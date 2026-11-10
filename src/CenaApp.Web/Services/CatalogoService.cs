// Catálogos que el admin puede mantener (D-22): categorías, gustos, restricciones y alérgenos. También arma el panel.
// Borrar un gusto, una restricción o un alérgeno lo quita de recetas, ingredientes y perfiles (tablas intermedias en
// cascada). Una categoría con recetas no se puede borrar (DeleteBehavior.Restrict, igual que en la base).
using CenaApp.Web.Data;
using CenaApp.Web.Models;
using CenaApp.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CenaApp.Web.Services;

public class CatalogoService : ICatalogoService
{
    private const int LargoMaximo = 40;

    private readonly AppDbContext _db;

    public CatalogoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CatalogosViewModel> ListarAsync()
    {
        return new CatalogosViewModel
        {
            Categorias = await _db.Categorias.OrderBy(c => c.Nombre)
                .Select(c => new ItemCatalogoViewModel(c.Id, c.Nombre, null, null,
                    Cuantos(_db.Recetas.Count(r => r.CategoriaId == c.Id), "receta", "recetas")))
                .ToListAsync(),
            Gustos = await _db.Gustos.OrderBy(g => g.Tipo).ThenBy(g => g.Nombre)
                .Select(g => new ItemCatalogoViewModel(g.Id, g.Nombre, null, g.Tipo,
                    Cuantos(_db.Recetas.Count(r => r.Gustos.Any(x => x.Id == g.Id)), "receta", "recetas") + " · " + Cuantos(_db.Usuarios.Count(u => u.Gustos.Any(x => x.Id == g.Id)), "usuario", "usuarios")))
                .ToListAsync(),
            Restricciones = await _db.Restricciones.OrderBy(r => r.Nombre)
                .Select(r => new ItemCatalogoViewModel(r.Id, r.Nombre, r.Descripcion, null,
                    Cuantos(_db.Ingredientes.Count(i => i.RestriccionesIncompatibles.Any(x => x.Id == r.Id)), "ingrediente no apto", "ingredientes no aptos") + " · " + Cuantos(_db.Usuarios.Count(u => u.Restricciones.Any(x => x.Id == r.Id)), "usuario", "usuarios")))
                .ToListAsync(),
            Alergenos = await _db.Alergenos.OrderBy(a => a.Nombre)
                .Select(a => new ItemCatalogoViewModel(a.Id, a.Nombre, null, null,
                    Cuantos(_db.Ingredientes.Count(i => i.Alergenos.Any(x => x.Id == a.Id)), "ingrediente", "ingredientes") + " · " + Cuantos(_db.Usuarios.Count(u => u.Alergenos.Any(x => x.Id == a.Id)), "usuario", "usuarios")))
                .ToListAsync(),
        };
    }

    // "1 receta" / "3 recetas". Se usa en la proyección final de la consulta: EF Core trae los números y esto corre en C#.
    private static string Cuantos(int cantidad, string singular, string plural) =>
        $"{cantidad} {(cantidad == 1 ? singular : plural)}";

    public async Task<string?> GuardarAsync(Catalogo catalogo, int? id, string? nombre, string? descripcion, TipoGusto tipo)
    {
        nombre = nombre?.Trim() ?? "";
        if (nombre.Length == 0)
        {
            return "El nombre es obligatorio.";
        }
        if (nombre.Length > LargoMaximo)
        {
            return $"El nombre puede tener hasta {LargoMaximo} caracteres.";
        }
        descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        if (descripcion?.Length > 200)
        {
            return "La descripción puede tener hasta 200 caracteres.";
        }

        // Nombre único dentro del catálogo, sin distinguir mayúsculas.
        var buscado = nombre.ToLower();
        var repetido = catalogo switch
        {
            Catalogo.Categorias => await _db.Categorias.AnyAsync(c => c.Nombre.ToLower() == buscado && c.Id != id),
            Catalogo.Gustos => await _db.Gustos.AnyAsync(g => g.Nombre.ToLower() == buscado && g.Id != id),
            Catalogo.Restricciones => await _db.Restricciones.AnyAsync(r => r.Nombre.ToLower() == buscado && r.Id != id),
            _ => await _db.Alergenos.AnyAsync(a => a.Nombre.ToLower() == buscado && a.Id != id),
        };
        if (repetido)
        {
            return $"Ya existe \"{nombre}\".";
        }

        switch (catalogo)
        {
            case Catalogo.Categorias:
                var categoria = id is null ? _db.Categorias.Add(new Categoria()).Entity : await _db.Categorias.FindAsync(id);
                if (categoria is null) return "Ya no existe.";
                categoria.Nombre = nombre;
                break;
            case Catalogo.Gustos:
                var gusto = id is null ? _db.Gustos.Add(new Gusto()).Entity : await _db.Gustos.FindAsync(id);
                if (gusto is null) return "Ya no existe.";
                gusto.Nombre = nombre;
                gusto.Tipo = tipo;
                break;
            case Catalogo.Restricciones:
                var restriccion = id is null ? _db.Restricciones.Add(new Restriccion()).Entity : await _db.Restricciones.FindAsync(id);
                if (restriccion is null) return "Ya no existe.";
                restriccion.Nombre = nombre;
                restriccion.Descripcion = descripcion;
                break;
            default:
                var alergeno = id is null ? _db.Alergenos.Add(new Alergeno()).Entity : await _db.Alergenos.FindAsync(id);
                if (alergeno is null) return "Ya no existe.";
                alergeno.Nombre = nombre;
                break;
        }
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> EliminarAsync(Catalogo catalogo, int id)
    {
        object? item = catalogo switch
        {
            Catalogo.Categorias => await _db.Categorias.FindAsync(id),
            Catalogo.Gustos => await _db.Gustos.FindAsync(id),
            Catalogo.Restricciones => await _db.Restricciones.FindAsync(id),
            _ => await _db.Alergenos.FindAsync(id),
        };
        if (item is null)
        {
            return "Ya no existe.";
        }

        if (catalogo == Catalogo.Categorias)
        {
            var recetas = await _db.Recetas.CountAsync(r => r.CategoriaId == id);
            if (recetas > 0)
            {
                return $"No se puede eliminar: la usan {recetas} {(recetas == 1 ? "receta" : "recetas")}. Cambiales la categoría primero.";
            }
        }

        _db.Remove(item);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<PanelViewModel> PanelAsync()
    {
        return new PanelViewModel
        {
            Usuarios = await _db.Usuarios.CountAsync(),
            UsuariosActivos = await _db.Usuarios.CountAsync(u => u.Activo),
            Admins = await _db.Usuarios.CountAsync(u => u.Rol != Rol.Usuario),
            Recetas = await _db.Recetas.CountAsync(),
            Ingredientes = await _db.Ingredientes.CountAsync(),
            IngredientesSinImagen = await _db.Ingredientes.CountAsync(i => i.Imagen == null),
            IngredientesSinUso = await _db.Ingredientes.CountAsync(i => !_db.RecetaIngredientes.Any(ri => ri.IngredienteId == i.Id)),
            MasFavoritas = await _db.Recetas
                .Select(r => new { r.Nombre, r.Emoji, Veces = _db.Usuarios.Count(u => u.Favoritos.Any(f => f.Id == r.Id)) })
                .Where(x => x.Veces > 0)
                .OrderByDescending(x => x.Veces).ThenBy(x => x.Nombre)
                .Take(5)
                .Select(x => new ValorPanel($"{x.Emoji} {x.Nombre}", x.Veces))
                .ToListAsync(),
            MasEnDespensas = await _db.Despensa
                .GroupBy(d => d.Ingrediente.Nombre)
                .Select(g => new { Nombre = g.Key, Veces = g.Count() })
                .OrderByDescending(x => x.Veces).ThenBy(x => x.Nombre)
                .Take(5)
                .Select(x => new ValorPanel(x.Nombre, x.Veces))
                .ToListAsync(),
        };
    }
}
