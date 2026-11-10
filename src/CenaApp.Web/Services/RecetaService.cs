// Reglas de negocio de las recetas: consultas, alta, baja y cálculo de alérgenos desde los ingredientes.
using CenaApp.Web.Data;
using CenaApp.Web.Models;
using CenaApp.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CenaApp.Web.Services;

public class RecetaService : IRecetaService
{
    private readonly AppDbContext _db;

    public RecetaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Receta>> ListarAsync(string? nombre = null)
    {
        var consulta = _db.Recetas.Include(r => r.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var buscado = nombre.Trim().ToLower();
            consulta = consulta.Where(r => r.Nombre.ToLower().Contains(buscado));
        }

        return await consulta.OrderBy(r => r.Nombre).ToListAsync();
    }

    public async Task<Receta?> ObtenerAsync(int id)
    {
        return await _db.Recetas
            .Include(r => r.Categoria)
            .Include(r => r.Gustos)
            .Include(r => r.Pasos)
            .Include(r => r.Ingredientes).ThenInclude(ri => ri.Ingrediente).ThenInclude(i => i.Alergenos)
            .Include(r => r.Ingredientes).ThenInclude(ri => ri.Ingrediente).ThenInclude(i => i.RestriccionesIncompatibles)
            .AsSplitQuery() // varias consultas chicas en vez de un JOIN enorme con filas repetidas
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Dictionary<string, string>> ValidarAsync(RecetaCrearDto datos, int? excluirId = null)
    {
        var errores = new Dictionary<string, string>();

        // VAL-15: nombre único (sin distinguir mayúsculas ni espacios de los costados).
        var nombre = datos.Nombre.Trim().ToLower();
        if (await _db.Recetas.AnyAsync(r => r.Nombre.ToLower() == nombre && r.Id != excluirId))
        {
            errores[nameof(datos.Nombre)] = "Ya existe una receta con ese nombre.";
        }

        if (!await _db.Categorias.AnyAsync(c => c.Id == datos.CategoriaId))
        {
            errores[nameof(datos.CategoriaId)] = $"No existe la categoría {datos.CategoriaId}.";
        }

        // VAL-18: sin ingredientes repetidos y todos del catálogo.
        var ids = datos.Ingredientes.Select(i => i.IngredienteId).ToList();
        if (ids.Count != ids.Distinct().Count())
        {
            errores[nameof(datos.Ingredientes)] = "Hay ingredientes repetidos.";
        }
        else
        {
            var existentes = await _db.Ingredientes.CountAsync(i => ids.Contains(i.Id));
            if (existentes != ids.Count)
            {
                errores[nameof(datos.Ingredientes)] = "Algún ingrediente no existe en el catálogo.";
            }
        }

        // VAL-19: cada paso con texto y hasta 500 caracteres.
        if (datos.Pasos.Any(p => string.IsNullOrWhiteSpace(p) || p.Length > 500))
        {
            errores[nameof(datos.Pasos)] = "Cada paso tiene que tener texto y hasta 500 caracteres.";
        }

        return errores;
    }

    public async Task<Receta> CrearAsync(RecetaCrearDto datos)
    {
        var receta = new Receta
        {
            Nombre = datos.Nombre.Trim(),
            Descripcion = datos.Descripcion?.Trim(),
            Emoji = string.IsNullOrWhiteSpace(datos.Emoji) ? "🍽️" : datos.Emoji.Trim(),
            TiempoMin = datos.TiempoMin,
            Porciones = datos.Porciones,
            Calorias = datos.Calorias,
            Dificultad = datos.Dificultad!.Value,
            CategoriaId = datos.CategoriaId,
            Ingredientes = datos.Ingredientes.Select(i => new RecetaIngrediente
            {
                IngredienteId = i.IngredienteId,
                Cantidad = i.Cantidad,
                Unidad = i.Unidad,
            }).ToList(),
            Pasos = datos.Pasos.Select((texto, i) => new Paso { Orden = i + 1, Texto = texto.Trim() }).ToList(),
            Gustos = await _db.Gustos.Where(g => datos.GustosIds.Contains(g.Id)).ToListAsync(),
        };

        _db.Recetas.Add(receta);
        await _db.SaveChangesAsync();
        return receta;
    }

    public async Task<bool> ActualizarAsync(int id, RecetaCrearDto datos)
    {
        var receta = await _db.Recetas
            .Include(r => r.Ingredientes)
            .Include(r => r.Pasos)
            .Include(r => r.Gustos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id);
        if (receta is null)
        {
            return false;
        }

        receta.Nombre = datos.Nombre.Trim();
        receta.Descripcion = datos.Descripcion?.Trim();
        receta.Emoji = string.IsNullOrWhiteSpace(datos.Emoji) ? "🍽️" : datos.Emoji.Trim();
        receta.TiempoMin = datos.TiempoMin;
        receta.Porciones = datos.Porciones;
        receta.Calorias = datos.Calorias;
        receta.Dificultad = datos.Dificultad!.Value;
        receta.CategoriaId = datos.CategoriaId;
        receta.Gustos = await _db.Gustos.Where(g => datos.GustosIds.Contains(g.Id)).ToListAsync();

        // Ingredientes: se actualizan en el lugar (la clave es receta + ingrediente, así que borrar y volver
        // a agregar el mismo en un solo guardado no se puede): se quitan los que ya no están, se actualizan
        // los que siguen y se agregan los nuevos.
        var nuevos = datos.Ingredientes.ToDictionary(i => i.IngredienteId);
        receta.Ingredientes.RemoveAll(ri => !nuevos.ContainsKey(ri.IngredienteId));
        foreach (var (ingredienteId, dato) in nuevos)
        {
            var existente = receta.Ingredientes.FirstOrDefault(ri => ri.IngredienteId == ingredienteId);
            if (existente is null)
            {
                receta.Ingredientes.Add(new RecetaIngrediente { IngredienteId = ingredienteId, Cantidad = dato.Cantidad, Unidad = dato.Unidad });
            }
            else
            {
                existente.Cantidad = dato.Cantidad;
                existente.Unidad = dato.Unidad;
            }
        }

        // Pasos: se reemplazan todos (cada paso tiene su propio Id, no hay choque de claves).
        receta.Pasos.Clear();
        receta.Pasos.AddRange(datos.Pasos.Select((texto, i) => new Paso { Orden = i + 1, Texto = texto.Trim() }));

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var receta = await _db.Recetas.FindAsync(id);
        if (receta is null)
        {
            return false;
        }

        // Pasos, ingredientes de la receta y favoritos se borran en cascada (VN-22).
        _db.Recetas.Remove(receta);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Receta>> ListarParaUsuarioAsync(int usuarioId, string? texto = null, int? gustoId = null)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Restricciones)
            .Include(u => u.Alergenos)
            .Include(u => u.Gustos)
            .AsSplitQuery()
            .FirstAsync(u => u.Id == usuarioId);

        var consulta = ConIngredientesYGustos(_db.Recetas);

        // RF11: búsqueda por texto en nombre o descripción.
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var buscado = texto.Trim().ToLower();
            consulta = consulta.Where(r => r.Nombre.ToLower().Contains(buscado)
                || (r.Descripcion != null && r.Descripcion.ToLower().Contains(buscado)));
        }

        // RF11: filtro por un gusto.
        if (gustoId is not null)
        {
            consulta = consulta.Where(r => r.Gustos.Any(g => g.Id == gustoId));
        }

        // El filtro de aptitud se hace en memoria con las funciones de abajo (son las que tienen tests).
        var recetas = await consulta.ToListAsync();
        return FiltrarYOrdenar(recetas,
            usuario.Restricciones.Select(r => r.Id).ToList(),
            usuario.Alergenos.Select(a => a.Id).ToList(),
            usuario.Gustos.Select(g => g.Id).ToList(),
            usuario.Nivel);
    }

    // ---------- Sugerencias según los favoritos (D-26) ----------

    public async Task<List<Sugerencia>> SugerenciasAsync(int usuarioId, int cantidad = 4)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Restricciones)
            .Include(u => u.Alergenos)
            .Include(u => u.Gustos)
            .Include(u => u.Favoritos)
            .AsSplitQuery()
            .FirstAsync(u => u.Id == usuarioId);
        if (usuario.Favoritos.Count == 0)
        {
            return [];
        }

        var recetas = await ConIngredientesYGustos(_db.Recetas).ToListAsync();
        var idsFavoritas = usuario.Favoritos.Select(f => f.Id).ToHashSet();

        // Las candidatas salen de la misma lista filtrada del Inicio: respetan restricciones, alergias y nivel.
        var aptas = FiltrarYOrdenar(recetas,
            usuario.Restricciones.Select(r => r.Id).ToList(),
            usuario.Alergenos.Select(a => a.Id).ToList(),
            usuario.Gustos.Select(g => g.Id).ToList(),
            usuario.Nivel);
        return Sugerir(aptas, recetas.Where(r => idsFavoritas.Contains(r.Id)).ToList(), cantidad);
    }

    // ---------- Favoritos (RF12) ----------

    public async Task<bool?> AlternarFavoritoAsync(int usuarioId, int recetaId)
    {
        var receta = await _db.Recetas.FindAsync(recetaId);
        if (receta is null)
        {
            return null;
        }

        var usuario = await _db.Usuarios.Include(u => u.Favoritos).FirstAsync(u => u.Id == usuarioId);
        var yaEstaba = usuario.Favoritos.Any(f => f.Id == recetaId);
        if (yaEstaba)
        {
            usuario.Favoritos.RemoveAll(f => f.Id == recetaId);
        }
        else
        {
            usuario.Favoritos.Add(receta);
        }

        // EF Core agrega o borra la fila (UsuarioId, RecetaId) de la tabla Favoritos.
        await _db.SaveChangesAsync();
        return !yaEstaba;
    }

    public async Task<HashSet<int>> IdsFavoritosAsync(int usuarioId)
    {
        var ids = await _db.Usuarios
            .Where(u => u.Id == usuarioId)
            .SelectMany(u => u.Favoritos.Select(f => f.Id))
            .ToListAsync();
        return ids.ToHashSet();
    }

    public async Task<List<Receta>> ListarFavoritosAsync(int usuarioId)
    {
        return await _db.Usuarios
            .Where(u => u.Id == usuarioId)
            .SelectMany(u => u.Favoritos)
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    // Include de todo lo que necesitan los cálculos de aptitud.
    private static IQueryable<Receta> ConIngredientesYGustos(IQueryable<Receta> recetas) =>
        recetas
            .Include(r => r.Categoria)
            .Include(r => r.Gustos)
            .Include(r => r.Ingredientes).ThenInclude(ri => ri.Ingrediente).ThenInclude(i => i.Alergenos)
            .Include(r => r.Ingredientes).ThenInclude(ri => ri.Ingrediente).ThenInclude(i => i.RestriccionesIncompatibles)
            .AsSplitQuery();

    // ---------------------------------------------------------------------------------------------
    // Reglas de aptitud (VN-04 a VN-08). Son funciones puras: reciben las recetas ya cargadas con sus
    // ingredientes y devuelven un resultado, sin tocar la base. Por eso se pueden testear (CenaApp.Tests).
    // ---------------------------------------------------------------------------------------------

    // Alérgenos de una receta = todos los alérgenos de sus ingredientes, sin repetir (VN-04).
    public static List<string> CalcularAlergenos(Receta receta)
    {
        return receta.Ingredientes
            .SelectMany(ri => ri.Ingrediente.Alergenos)
            .Select(a => a.Nombre)
            .Distinct()
            .Order()
            .ToList();
    }

    // Una receta es apta para una restricción si NINGUNO de sus ingredientes es incompatible con ella.
    // Con varias restricciones tiene que ser apta para todas (filtro estricto, VN-06).
    public static bool EsApta(Receta receta, ICollection<int> restriccionesIds)
    {
        return !receta.Ingredientes.Any(ri =>
            ri.Ingrediente.RestriccionesIncompatibles.Any(r => restriccionesIds.Contains(r.Id)));
    }

    // De un catálogo de restricciones, las que la receta cumple (para mostrar "Apta para: ...").
    public static List<string> RestriccionesAptas(Receta receta, IEnumerable<Restriccion> catalogo)
    {
        return catalogo
            .Where(r => EsApta(receta, [r.Id]))
            .Select(r => r.Nombre)
            .Order()
            .ToList();
    }

    // true si algún ingrediente tiene alguno de esos alérgenos (VN-05).
    public static bool ContieneAlergeno(Receta receta, ICollection<int> alergenosIds)
    {
        return receta.Ingredientes.Any(ri =>
            ri.Ingrediente.Alergenos.Any(a => alergenosIds.Contains(a.Id)));
    }

    // Cuántos de los gustos del usuario tiene la receta (para ordenar, VN-08).
    public static int GustosEnComun(Receta receta, ICollection<int> gustosIds)
    {
        return receta.Gustos.Count(g => gustosIds.Contains(g.Id));
    }

    // Nivel de cocina (D-23): un Principiante no ve recetas Difíciles.
    public static bool PuedeVer(Receta receta, Nivel nivel) =>
        !(nivel == Nivel.Principiante && receta.Dificultad == Dificultad.Dificil);

    // Orden por nivel (menor = antes): al Principiante, primero las Fáciles; al Avanzado, primero las Difíciles;
    // al Intermedio le da igual.
    public static int OrdenPorNivel(Receta receta, Nivel nivel) => nivel switch
    {
        Nivel.Principiante => (int)receta.Dificultad,
        Nivel.Avanzado => (int)Dificultad.Dificil - (int)receta.Dificultad,
        _ => 0,
    };

    // Cuánto se parecen dos recetas: 2 puntos por gusto en común, 1 por ser de la misma categoría
    // y 1 por cada ingrediente en común (sin contar básicos como sal o aceite).
    public static int Parecido(Receta a, Receta b)
    {
        var gustos = a.Gustos.Count(g => b.Gustos.Any(o => o.Id == g.Id));
        var categoria = a.CategoriaId == b.CategoriaId ? 1 : 0;
        var ingredientes = a.Ingredientes.Count(ri => ri.Ingrediente.CuentaEnDespensa
            && b.Ingredientes.Any(o => o.IngredienteId == ri.IngredienteId));
        return gustos * 2 + categoria + ingredientes;
    }

    // "Te puede gustar" (D-26): de las candidatas (ya filtradas para el usuario), las que no son favoritas,
    // ordenadas por cuánto se parecen a sus favoritas en total. Cada una dice a qué favorita se parece más.
    public static List<Sugerencia> Sugerir(IEnumerable<Receta> candidatas, List<Receta> favoritas, int cantidad)
    {
        if (favoritas.Count == 0)
        {
            return [];
        }
        return candidatas
            .Where(r => favoritas.All(f => f.Id != r.Id))
            .Select(r =>
            {
                var masParecida = favoritas.MaxBy(f => Parecido(r, f))!;
                return new Sugerencia(r, favoritas.Sum(f => Parecido(r, f)), masParecida.Nombre);
            })
            .Where(s => s.Puntaje > 0)
            .OrderByDescending(s => s.Puntaje)
            .ThenBy(s => s.Receta.Nombre)
            .Take(cantidad)
            .ToList();
    }

    // Lo que ve el usuario en el Inicio: sin sus alérgenos, aptas para todas sus restricciones y para su nivel,
    // primero las que más coinciden con sus gustos, después según su nivel y, al final, por nombre.
    public static List<Receta> FiltrarYOrdenar(IEnumerable<Receta> recetas,
        ICollection<int> restriccionesIds, ICollection<int> alergenosIds, ICollection<int> gustosIds,
        Nivel nivel = Nivel.Intermedio)
    {
        return recetas
            .Where(r => !ContieneAlergeno(r, alergenosIds))
            .Where(r => EsApta(r, restriccionesIds))
            .Where(r => PuedeVer(r, nivel))
            .OrderByDescending(r => GustosEnComun(r, gustosIds))
            .ThenBy(r => OrdenPorNivel(r, nivel))
            .ThenBy(r => r.Nombre)
            .ToList();
    }
}
