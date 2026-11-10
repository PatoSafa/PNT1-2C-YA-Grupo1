// Contrato del servicio de usuarios: el ABM que usa el SuperAdmin (D-22).
using CenaApp.Web.Models;
using CenaApp.Web.ViewModels;

namespace CenaApp.Web.Services;

public interface IUsuarioService
{
    // Usuarios por email; filtra por texto (email o nombre) y por rol si vienen.
    Task<List<UsuarioFilaViewModel>> ListarAsync(string? buscar = null, Rol? rol = null);

    // null si no existe.
    Task<Usuario?> ObtenerAsync(int id);

    // Reglas que las Data Annotations no pueden ver: email repetido, contraseña obligatoria al crear y que el
    // SuperAdmin no se quite a sí mismo el rol ni se desactive (si no, la app podría quedar sin SuperAdmin).
    // Devuelve los errores por campo; vacío = todo bien.
    Task<Dictionary<string, string>> ValidarAsync(UsuarioFormularioViewModel formulario, int idActual);

    Task<Usuario> CrearAsync(UsuarioFormularioViewModel formulario);

    // false si no existe.
    Task<bool> ActualizarAsync(int id, UsuarioFormularioViewModel formulario);

    // null si se borró; si no, el motivo (no existe, o es el propio SuperAdmin).
    Task<string?> EliminarAsync(int id, int idActual);
}
