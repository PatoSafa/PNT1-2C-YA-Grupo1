// Contrato del servicio de ingredientes (administración): alta, baja, modificación y validaciones del catálogo.
using CenaApp.Web.Models;
using CenaApp.Web.ViewModels;

namespace CenaApp.Web.Services;

public interface IIngredienteService
{
    // Ingredientes con sus alérgenos y en cuántas recetas se usan; filtra por nombre o sinónimo si viene "buscar".
    Task<List<IngredienteFilaViewModel>> ListarAsync(string? buscar = null);

    // El ingrediente con sus alérgenos y restricciones incompatibles. null si no existe.
    Task<Ingrediente?> ObtenerAsync(int id);

    Task CargarOpcionesAsync(IngredienteFormularioViewModel formulario);

    // Reglas que las Data Annotations no pueden ver (nombre o sinónimo repetido, sinónimos largos).
    Task<Dictionary<string, string>> ValidarAsync(IngredienteFormularioViewModel formulario);

    // La imagen guardada del ingrediente y su tipo MIME. null si no existe o no tiene imagen (D-21).
    Task<(byte[] Datos, string Tipo)?> ImagenAsync(int id);

    // imagen: la nueva, ya validada con Imagenes.LeerAsync (null = no cambia, salvo que marque "Quitar imagen").
    Task<Ingrediente> CrearAsync(IngredienteFormularioViewModel formulario, byte[]? imagen = null, string? imagenTipo = null);

    // false si el ingrediente no existe.
    Task<bool> ActualizarAsync(int id, IngredienteFormularioViewModel formulario, byte[]? imagen = null, string? imagenTipo = null);

    // null si se borró; si no, el motivo (no existe, o se usa en recetas: VN-21).
    Task<string?> EliminarAsync(int id);
}
