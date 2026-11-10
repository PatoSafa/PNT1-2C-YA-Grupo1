// Imágenes de los ingredientes (D-21): las devuelve desde la base para usarlas en <img src="/Ingredientes/Imagen/5">.
using System.Security.Cryptography;
using CenaApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CenaApp.Web.Controllers;

[Authorize]
public class IngredientesController : Controller
{
    private readonly IIngredienteService _ingredientes;

    public IngredientesController(IIngredienteService ingredientes)
    {
        _ingredientes = ingredientes;
    }

    // GET /Ingredientes/Imagen/5 → el archivo de la imagen. Sin imagen, un ícono genérico.
    // ETag: una "huella" del archivo. Si el navegador ya la tiene, responde 304 (sin volver a mandarla);
    // si el admin cambia la imagen, cambia la huella y el navegador baja la nueva.
    [HttpGet]
    public async Task<IActionResult> Imagen(int id)
    {
        var imagen = await _ingredientes.ImagenAsync(id);
        if (imagen is null)
        {
            return Redirect("~/img/ingrediente.svg");
        }

        var huella = $"\"{Convert.ToHexString(SHA256.HashData(imagen.Value.Datos))[..16]}\"";
        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.ETag = huella;
        if (Request.Headers.IfNoneMatch == huella)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }
        return File(imagen.Value.Datos, imagen.Value.Tipo);
    }
}
