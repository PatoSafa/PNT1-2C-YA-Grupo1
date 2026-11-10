// ABM de usuarios: listado, alta, edición (rol, activo, contraseña) y baja. Solo para el SuperAdmin (D-22).
using CenaApp.Web.Models;
using CenaApp.Web.Services;
using CenaApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CenaApp.Web.Controllers;

[Authorize(Roles = nameof(Rol.SuperAdmin))]
[Route("Admin/Usuarios/[action]/{id?}")]
public class AdminUsuariosController : Controller
{
    private readonly IUsuarioService _usuarios;

    public AdminUsuariosController(IUsuarioService usuarios)
    {
        _usuarios = usuarios;
    }

    // GET /Admin/Usuarios?buscar=ana&rol=Admin
    [HttpGet("/Admin/Usuarios")]
    [HttpGet]
    public async Task<IActionResult> Index(string? buscar, Rol? rol)
    {
        ViewData["Buscar"] = buscar;
        ViewData["Rol"] = rol;
        return View(await _usuarios.ListarAsync(buscar, rol));
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View("Formulario", new UsuarioFormularioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioFormularioViewModel formulario)
    {
        formulario.Id = null;
        if (!await EsValidoAsync(formulario))
        {
            return View("Formulario", formulario);
        }

        var creado = await _usuarios.CrearAsync(formulario);
        TempData["Exito"] = $"Se creó el usuario {creado.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await _usuarios.ObtenerAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }
        return View("Formulario", new UsuarioFormularioViewModel
        {
            Id = usuario.Id,
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Rol = usuario.Rol,
            Activo = usuario.Activo,
            EsElActual = usuario.Id == UsuarioActual.Id(User),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, UsuarioFormularioViewModel formulario)
    {
        formulario.Id = id;
        formulario.EsElActual = id == UsuarioActual.Id(User);
        if (!await EsValidoAsync(formulario))
        {
            return View("Formulario", formulario);
        }

        if (!await _usuarios.ActualizarAsync(id, formulario))
        {
            return NotFound();
        }
        TempData["Exito"] = $"Se guardaron los cambios de {formulario.Email.Trim().ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var error = await _usuarios.EliminarAsync(id, UsuarioActual.Id(User));
        if (error is null)
        {
            TempData["Exito"] = "Se eliminó el usuario.";
        }
        else
        {
            TempData["Error"] = error;
        }
        return RedirectToAction(nameof(Index));
    }

    // Data Annotations (ModelState) + reglas del servicio, todo en el mismo ModelState para mostrarlo en el formulario.
    private async Task<bool> EsValidoAsync(UsuarioFormularioViewModel formulario)
    {
        if (ModelState.IsValid)
        {
            foreach (var (campo, mensaje) in await _usuarios.ValidarAsync(formulario, UsuarioActual.Id(User)))
            {
                ModelState.AddModelError(campo, mensaje);
            }
        }
        return ModelState.IsValid;
    }
}
