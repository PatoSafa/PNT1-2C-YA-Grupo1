// ABM de usuarios para el SuperAdmin (D-22): crear, editar (rol, activo, contraseña) y eliminar.
using CenaApp.Web.Data;
using CenaApp.Web.Models;
using CenaApp.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CenaApp.Web.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public UsuarioService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UsuarioFilaViewModel>> ListarAsync(string? buscar = null, Rol? rol = null)
    {
        var consulta = _db.Usuarios.AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim().ToLower();
            consulta = consulta.Where(u => u.Email.Contains(texto) || u.Nombre.ToLower().Contains(texto));
        }
        if (rol is not null)
        {
            consulta = consulta.Where(u => u.Rol == rol);
        }

        return await consulta
            .OrderBy(u => u.Email)
            .Select(u => new UsuarioFilaViewModel
            {
                Id = u.Id,
                Email = u.Email,
                Nombre = u.Nombre,
                Rol = u.Rol,
                Activo = u.Activo,
                CreadoEn = u.CreadoEn,
                Favoritos = u.Favoritos.Count,
                EnDespensa = u.Despensa.Count,
            })
            .ToListAsync();
    }

    public async Task<Usuario?> ObtenerAsync(int id) => await _db.Usuarios.FindAsync(id);

    public async Task<Dictionary<string, string>> ValidarAsync(UsuarioFormularioViewModel formulario, int idActual)
    {
        var errores = new Dictionary<string, string>();

        // VN-01: email único, sin distinguir mayúsculas.
        var email = Normalizar(formulario.Email);
        if (await _db.Usuarios.AnyAsync(u => u.Email == email && u.Id != formulario.Id))
        {
            errores[nameof(formulario.Email)] = "Ya existe un usuario con ese email.";
        }

        if (formulario.Id is null && string.IsNullOrEmpty(formulario.Clave))
        {
            errores[nameof(formulario.Clave)] = "Al crear un usuario, la contraseña es obligatoria.";
        }

        // El SuperAdmin no puede dejarse sin acceso a sí mismo.
        if (formulario.Id == idActual && (formulario.Rol != Rol.SuperAdmin || !formulario.Activo))
        {
            errores[nameof(formulario.Rol)] = "No podés quitarte el rol de SuperAdmin ni desactivar tu propia cuenta.";
        }

        return errores;
    }

    public async Task<Usuario> CrearAsync(UsuarioFormularioViewModel formulario)
    {
        var usuario = new Usuario();
        Copiar(formulario, usuario);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return usuario;
    }

    public async Task<bool> ActualizarAsync(int id, UsuarioFormularioViewModel formulario)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return false;
        }
        Copiar(formulario, usuario);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string?> EliminarAsync(int id, int idActual)
    {
        if (id == idActual)
        {
            return "No podés eliminar tu propia cuenta.";
        }
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return $"No existe un usuario con id {id}.";
        }

        // Su perfil, favoritos y despensa se borran en cascada (tablas intermedias).
        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
        return null;
    }

    private void Copiar(UsuarioFormularioViewModel formulario, Usuario usuario)
    {
        usuario.Email = Normalizar(formulario.Email);
        usuario.Nombre = formulario.Nombre?.Trim() ?? "";
        usuario.Rol = formulario.Rol;
        usuario.Activo = formulario.Activo;
        // Contraseña: solo si se escribió una (al editar, vacía = no cambia).
        if (!string.IsNullOrEmpty(formulario.Clave))
        {
            usuario.PasswordHash = _hasher.HashPassword(usuario, formulario.Clave);
        }
    }

    private static string Normalizar(string email) => email.Trim().ToLowerInvariant();
}
