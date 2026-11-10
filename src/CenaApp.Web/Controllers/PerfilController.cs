// Perfil del usuario: verlo, completarlo por primera vez (onboarding) y editarlo.
using CenaApp.Web.Services;
using CenaApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CenaApp.Web.Controllers;

[Authorize]
public class PerfilController : Controller
{
    private readonly IPerfilService _perfiles;

    public PerfilController(IPerfilService perfiles)
    {
        _perfiles = perfiles;
    }

    // GET /Perfil → perfil en modo lectura; si todavía no lo completó, al onboarding.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuario = await _perfiles.ObtenerAsync(UsuarioActual.Id(User));
        if (usuario is null)
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }
        if (string.IsNullOrEmpty(usuario.Nombre))
        {
            return RedirectToAction(nameof(Editar));
        }

        var ingredientesPorAlergeno = await _perfiles.IngredientesPorAlergenoAsync();
        var modelo = new PerfilViewModel
        {
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Nivel = usuario.Nivel,
            AvatarUrl = Imagenes.DataUri(usuario.Avatar, usuario.AvatarTipo),
            Restricciones = usuario.Restricciones.OrderBy(r => r.Nombre).Select(r => (r.Nombre, r.Descripcion)).ToList(),
            Alergenos = usuario.Alergenos.OrderBy(a => a.Nombre)
                .Select(a => (a.Nombre, ingredientesPorAlergeno.TryGetValue(a.Id, out var lista) ? $"Sin recetas con: {lista}." : null))
                .ToList(),
            Gustos = usuario.Gustos.Select(g => g.Nombre).Order().ToList(),
        };
        return View(modelo);
    }

    // GET /Perfil/Editar → formulario con lo que el usuario ya tiene marcado.
    [HttpGet]
    public async Task<IActionResult> Editar()
    {
        var usuario = await _perfiles.ObtenerAsync(UsuarioActual.Id(User));
        if (usuario is null)
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        var formulario = new PerfilFormularioViewModel
        {
            Nombre = usuario.Nombre,
            Nivel = usuario.Nivel,
            RestriccionesIds = usuario.Restricciones.Select(r => r.Id).ToList(),
            AlergenosIds = usuario.Alergenos.Select(a => a.Id).ToList(),
            GustosIds = usuario.Gustos.Select(g => g.Id).ToList(),
            EsOnboarding = string.IsNullOrEmpty(usuario.Nombre),
            AvatarActual = Imagenes.DataUri(usuario.Avatar, usuario.AvatarTipo),
        };
        await _perfiles.CargarOpcionesAsync(formulario);
        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(PerfilFormularioViewModel formulario)
    {
        // La foto se valida acá (tamaño y que sea una imagen de verdad), además de las Data Annotations.
        byte[]? avatar = null;
        string? avatarTipo = null;
        if (formulario.Avatar is not null)
        {
            (avatar, avatarTipo, var error) = await Imagenes.LeerAsync(formulario.Avatar);
            if (error is not null)
            {
                ModelState.AddModelError(nameof(formulario.Avatar), error);
            }
        }

        if (!ModelState.IsValid)
        {
            // Las opciones (y la foto actual) no viajan en el POST: se vuelven a cargar para redibujar el formulario.
            await _perfiles.CargarOpcionesAsync(formulario);
            var actual = await _perfiles.ObtenerAsync(UsuarioActual.Id(User));
            formulario.AvatarActual = Imagenes.DataUri(actual?.Avatar, actual?.AvatarTipo);
            return View(formulario);
        }

        await _perfiles.GuardarAsync(UsuarioActual.Id(User), formulario, avatar, avatarTipo);
        TempData["Exito"] = formulario.EsOnboarding ? "¡Listo! Armamos tus recetas según tu perfil." : "Guardaste los cambios de tu perfil.";

        // Después del onboarding va directo a las recetas; después de editar, vuelve al perfil.
        return formulario.EsOnboarding
            ? RedirectToAction("Index", "Home")
            : RedirectToAction(nameof(Index));
    }
}
