// Despensa: ingredientes que el usuario tiene en casa y cálculo de qué recetas puede cocinar (RF16 a RF18).
using System.Globalization;
using System.Text;
using CenaApp.Web.Data;
using CenaApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CenaApp.Web.Services;

public class DespensaService : IDespensaService
{
    // VN-11 / VN-12: desde qué porcentaje una receta aparece en "Casi listas" (D-03, punto 8).
    public const int MinimoCasiListas = 50;

    private readonly AppDbContext _db;

    public DespensaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DespensaItem>> ObtenerAsync(int usuarioId)
    {
        return await _db.Despensa
            .Where(d => d.UsuarioId == usuarioId)
            .Include(d => d.Ingrediente)
            .OrderBy(d => d.Ingrediente.Nombre)
            .ToListAsync();
    }

    public async Task<List<Ingrediente>> CatalogoAsync()
    {
        return await _db.Ingredientes.OrderBy(i => i.Nombre).ToListAsync();
    }

    public async Task<Dictionary<int, List<string>>> UnidadesAsync()
    {
        var usadas = await _db.RecetaIngredientes
            .Where(ri => ri.Unidad != null)
            .Select(ri => new { ri.IngredienteId, ri.Unidad })
            .Distinct()
            .ToListAsync();
        return usadas
            .GroupBy(u => u.IngredienteId)
            .ToDictionary(g => g.Key, g => g.Select(u => u.Unidad!).Order().ToList());
    }

    public async Task<ResultadoOperacion> AgregarAsync(int usuarioId, string texto, decimal? cantidad = null, string? unidad = null)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return new(false, "Escribí un ingrediente.");
        }
        if (cantidad is <= 0)
        {
            return new(false, "La cantidad tiene que ser mayor a 0 (o dejala vacía).");
        }

        var ingrediente = Buscar(await CatalogoAsync(), texto);
        if (ingrediente is null)
        {
            return new(false, $"\"{texto.Trim()}\" no está en el catálogo de ingredientes.");
        }

        unidad = cantidad is null || string.IsNullOrWhiteSpace(unidad) ? null : unidad.Trim();
        var existente = await _db.Despensa.FindAsync(usuarioId, ingrediente.Id);
        if (existente is null)
        {
            _db.Despensa.Add(new DespensaItem { UsuarioId = usuarioId, IngredienteId = ingrediente.Id, Cantidad = cantidad, Unidad = unidad });
            await _db.SaveChangesAsync();
            return new(true, $"{ingrediente.Nombre} agregado a tu despensa.");
        }

        // VAL-12: sin cantidad no tiene sentido agregarlo dos veces.
        if (cantidad is null)
        {
            return new(false, $"{ingrediente.Nombre} ya está en tu despensa.");
        }

        // Ya estaba: con cantidad en la misma unidad se suma (ej: compró lo que le faltaba); si no tenía
        // cantidad o estaba en otra unidad, queda la nueva.
        existente.Cantidad = existente.Cantidad is not null && existente.Unidad == unidad
            ? existente.Cantidad + cantidad
            : cantidad;
        existente.Unidad = unidad;
        await _db.SaveChangesAsync();
        return new(true, $"Sumaste {ingrediente.Nombre} a tu despensa.");
    }

    public async Task<ResultadoOperacion> CambiarCantidadAsync(int usuarioId, int ingredienteId, decimal? cantidad, string? unidad)
    {
        if (cantidad is <= 0)
        {
            return new(false, "La cantidad tiene que ser mayor a 0 (o dejala vacía).");
        }
        var item = await BuscarItemAsync(usuarioId, ingredienteId);
        if (item is null)
        {
            return new(false, "Ese ingrediente ya no está en tu despensa.");
        }

        item.Cantidad = cantidad;
        item.Unidad = cantidad is null || string.IsNullOrWhiteSpace(unidad) ? null : unidad.Trim();
        await _db.SaveChangesAsync();
        return new(true, $"Guardaste la cantidad de {item.Ingrediente.Nombre}.");
    }

    public async Task<ResultadoOperacion> QuitarAsync(int usuarioId, int ingredienteId)
    {
        var item = await BuscarItemAsync(usuarioId, ingredienteId);
        if (item is null)
        {
            return new(false, "Ese ingrediente ya no está en tu despensa.");
        }
        _db.Despensa.Remove(item);
        await _db.SaveChangesAsync();
        return new(true, $"Quitaste {item.Ingrediente.Nombre} de tu despensa.");
    }

    public async Task<ResultadoOperacion> VaciarAsync(int usuarioId)
    {
        await _db.Despensa.Where(d => d.UsuarioId == usuarioId).ExecuteDeleteAsync();
        return new(true, "Vaciaste tu despensa.");
    }

    public async Task<ResultadoDespensa> SugerenciasAsync(int usuarioId)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Restricciones)
            .Include(u => u.Alergenos)
            .AsSplitQuery()
            .FirstAsync(u => u.Id == usuarioId);

        var recetas = await ConIngredientes(_db.Recetas).ToListAsync();

        // Las mismas reglas que el Inicio: no se sugiere nada que el usuario no pueda comer.
        var aptas = RecetaService.FiltrarYOrdenar(recetas,
            usuario.Restricciones.Select(r => r.Id).ToList(),
            usuario.Alergenos.Select(a => a.Id).ToList(),
            []);

        return Clasificar(aptas, await ObtenerAsync(usuarioId));
    }

    public async Task<AnalisisReceta?> AnalizarRecetaAsync(int usuarioId, int recetaId)
    {
        var receta = await ConIngredientes(_db.Recetas).FirstOrDefaultAsync(r => r.Id == recetaId);
        return receta is null ? null : Analizar(receta, await ObtenerAsync(usuarioId));
    }

    private Task<DespensaItem?> BuscarItemAsync(int usuarioId, int ingredienteId) =>
        _db.Despensa.Include(d => d.Ingrediente)
            .FirstOrDefaultAsync(d => d.UsuarioId == usuarioId && d.IngredienteId == ingredienteId);

    private static IQueryable<Receta> ConIngredientes(IQueryable<Receta> recetas) =>
        recetas
            .Include(r => r.Ingredientes).ThenInclude(ri => ri.Ingrediente).ThenInclude(i => i.Alergenos)
            .Include(r => r.Ingredientes).ThenInclude(ri => ri.Ingrediente).ThenInclude(i => i.RestriccionesIncompatibles)
            .AsSplitQuery();

    // ---------------------------------------------------------------------------------------------
    // Reglas de la despensa. Funciones puras (sin base de datos), con tests en CenaApp.Tests.
    // ---------------------------------------------------------------------------------------------

    // Qué falta comprar de un ingrediente de la receta según lo que hay en casa (D-19). null = alcanza.
    // - No está en la despensa → falta todo lo que pide la receta.
    // - Está sin cantidad, la receta dice "a gusto" o está en otra unidad → alcanza.
    // - Está con cantidad en la misma unidad → falta la diferencia, si la hay.
    public static Faltante? CalcularFaltante(RecetaIngrediente necesario, DespensaItem? enCasa)
    {
        if (enCasa is null)
        {
            return new Faltante(necesario.Ingrediente, necesario.Cantidad, necesario.Unidad, null);
        }
        if (enCasa.Cantidad is null || necesario.Cantidad is null || enCasa.Unidad != necesario.Unidad)
        {
            return null;
        }
        var falta = necesario.Cantidad.Value - enCasa.Cantidad.Value;
        return falta > 0 ? new Faltante(necesario.Ingrediente, falta, necesario.Unidad, enCasa.Cantidad) : null;
    }

    // Porcentaje de ingredientes que alcanzan, sin contar los básicos como sal o agua (VN-10),
    // y la lista de lo que falta comprar. Una receta hecha solo de básicos cuenta como 100 %.
    public static AnalisisReceta Analizar(Receta receta, IEnumerable<DespensaItem> despensa)
    {
        var enCasa = despensa.ToDictionary(d => d.IngredienteId);
        var queCuentan = receta.Ingredientes.Where(ri => ri.Ingrediente.CuentaEnDespensa).ToList();
        var faltantes = queCuentan
            .Select(ri => CalcularFaltante(ri, enCasa.GetValueOrDefault(ri.IngredienteId)))
            .OfType<Faltante>()
            .ToList();

        var porcentaje = queCuentan.Count == 0
            ? 100
            : (queCuentan.Count - faltantes.Count) * 100 / queCuentan.Count;

        return new AnalisisReceta(receta, porcentaje, faltantes);
    }

    // 100 % → "Podés cocinar", por nombre. 50–99 % → "Casi listas", de mayor a menor porcentaje (VN-11 a VN-13).
    public static ResultadoDespensa Clasificar(IEnumerable<Receta> recetas, IEnumerable<DespensaItem> despensa)
    {
        var items = despensa.ToList();
        var analisis = recetas.Select(r => Analizar(r, items)).ToList();

        var podesCocinar = analisis
            .Where(a => a.Porcentaje == 100)
            .OrderBy(a => a.Receta.Nombre)
            .ToList();

        var casiListas = analisis
            .Where(a => a.Porcentaje >= MinimoCasiListas && a.Porcentaje < 100)
            .OrderByDescending(a => a.Porcentaje)
            .ThenBy(a => a.Receta.Nombre)
            .ToList();

        return new ResultadoDespensa(podesCocinar, casiListas);
    }

    // Busca un ingrediente del catálogo por nombre o por sinónimo, sin distinguir mayúsculas ni tildes
    // ("palta", "Aguacate" y "AGUACATE" encuentran la Palta) (RF17).
    public static Ingrediente? Buscar(IEnumerable<Ingrediente> catalogo, string texto)
    {
        var buscado = Normalizar(texto);
        return catalogo.FirstOrDefault(i =>
            Normalizar(i.Nombre) == buscado
            || (i.Sinonimos ?? "").Split(',').Any(s => Normalizar(s) == buscado));
    }

    // "  Limón " → "limon": sin espacios de más, en minúsculas y sin tildes.
    public static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sinTildes = descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(sinTildes.ToArray()).Normalize(NormalizationForm.FormC);
    }
}
