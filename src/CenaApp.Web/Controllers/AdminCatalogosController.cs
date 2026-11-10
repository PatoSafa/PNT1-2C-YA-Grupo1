// Administración de catálogos (categorías, gustos, restricciones y alérgenos) y panel con números. Admin y SuperAdmin (D-22).
using CenaApp.Web.Models;
using CenaApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CenaApp.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Admin/Catalogos/[action]/{id?}")]
public class AdminCatalogosController : Controller
{
    private readonly ICatalogoService _catalogos;

    public AdminCatalogosController(ICatalogoService catalogos)
    {
        _catalogos = catalogos;
    }

    // GET /Admin/Panel → números de la app (usuarios, recetas, lo más usado).
    [HttpGet("/Admin")]
    [HttpGet("/Admin/Panel")]
    public async Task<IActionResult> Panel()
    {
        return View(await _catalogos.PanelAsync());
    }

    // GET /Admin/Catalogos
    [HttpGet("/Admin/Catalogos")]
    public async Task<IActionResult> Catalogos()
    {
        return View(await _catalogos.ListarAsync());
    }

    // POST /Admin/Catalogos/Guardar (id vacío = alta). Vuelve a la lista con un aviso.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(Catalogo catalogo, int? id, string? nombre, string? descripcion, TipoGusto tipo)
    {
        var error = await _catalogos.GuardarAsync(catalogo, id, nombre, descripcion, tipo);
        TempData[error is null ? "Exito" : "Error"] = error ?? $"Se guardó \"{nombre?.Trim()}\".";
        return RedirectToAction(nameof(Catalogos), null, catalogo.ToString().ToLower());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Catalogo catalogo, int id)
    {
        var error = await _catalogos.EliminarAsync(catalogo, id);
        TempData[error is null ? "Exito" : "Error"] = error ?? "Se eliminó.";
        return RedirectToAction(nameof(Catalogos), null, catalogo.ToString().ToLower());
    }
}
