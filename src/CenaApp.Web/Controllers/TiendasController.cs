// Tiendas cercanas: comercios de OpenStreetMap cerca del usuario, con filtro por tipo y por ingredientes, y mapa (D-11, D-20).
// La página se dibuja sin la lista; js/tiendas.js pide la ubicación al navegador y trae la lista de Buscar con fetch.
using CenaApp.Web.Models;
using CenaApp.Web.Services;
using CenaApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CenaApp.Web.Controllers;

[Authorize]
public class TiendasController : Controller
{
    private readonly IComercioService _comercios;
    private readonly IDespensaService _despensa;

    public TiendasController(IComercioService comercios, IDespensaService despensa)
    {
        _comercios = comercios;
        _despensa = despensa;
    }

    // GET /Tiendas?tipo=Verdulería&ingredientes=3,8 (los ingredientes llegan desde "Ver tiendas" de la Despensa)
    [HttpGet]
    public async Task<IActionResult> Index(string? tipo, string? ingredientes)
    {
        var modelo = new TiendasViewModel
        {
            Tipo = tipo,
            Tipos = _comercios.Tipos(),
            Ingredientes = (await IngredientesAsync(ingredientes)).Select(i => (i.Id, i.Nombre)).ToList(),
            Centro = _comercios.Centro,
        };
        return View(modelo);
    }

    // GET /Tiendas/Buscar?lat=-34.6&lng=-58.4&tipo=...&ingredientes=3,8 → vista parcial con la lista.
    // Sin lat/lng (el usuario no compartió la ubicación), o si no son válidas, busca desde ORT Yatay.
    [HttpGet]
    public async Task<IActionResult> Buscar(double? lat, double? lng, string? tipo, string? ingredientes)
    {
        var ubicacionReal = lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
        var (latitud, longitud) = ubicacionReal ? (lat!.Value, lng!.Value) : (_comercios.Centro.Lat, _comercios.Centro.Lng);
        var buscados = await IngredientesAsync(ingredientes);

        var resultado = await _comercios.BuscarAsync(latitud, longitud, tipo, buscados);
        return PartialView("_Lista", new TiendasListaViewModel
        {
            Comercios = resultado.Comercios,
            SonDeEjemplo = resultado.SonDeEjemplo,
            UbicacionReal = ubicacionReal,
            NombreCentro = _comercios.Centro.Nombre,
            FiltraPorIngredientes = buscados.Count > 0,
        });
    }

    // "3,8,x" → los ingredientes 3 y 8 del catálogo (lo que no es un id válido se ignora).
    private async Task<List<Ingrediente>> IngredientesAsync(string? ids)
    {
        var buscados = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => int.TryParse(t, out var id) ? id : 0)
            .ToHashSet();
        if (buscados.Count == 0)
        {
            return [];
        }
        return (await _despensa.CatalogoAsync()).Where(i => buscados.Contains(i.Id)).ToList();
    }
}
