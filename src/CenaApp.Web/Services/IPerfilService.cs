// Contrato del servicio de perfil: leer y guardar los datos personales y las preferencias del usuario.
using CenaApp.Web.Models;
using CenaApp.Web.ViewModels;

namespace CenaApp.Web.Services;

public interface IPerfilService
{
    // El usuario con sus restricciones, alergias y gustos. null si no existe.
    Task<Usuario?> ObtenerAsync(int usuarioId);

    // Carga las opciones de los catálogos en el formulario (restricciones, alérgenos, gustos).
    Task CargarOpcionesAsync(PerfilFormularioViewModel formulario);

    // Qué ingredientes del catálogo tiene cada alérgeno (id → "Almendras, Nueces"), para explicar qué deja afuera.
    Task<Dictionary<int, string>> IngredientesPorAlergenoAsync();

    // Guarda el perfil. avatar: la foto nueva ya validada (null = no cambia, salvo que marque "Quitar foto").
    Task GuardarAsync(int usuarioId, PerfilFormularioViewModel formulario, byte[]? avatar, string? avatarTipo);
}
