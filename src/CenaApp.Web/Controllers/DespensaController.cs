// Mi Despensa: ingredientes que el usuario tiene en casa (con cantidad opcional) y qué puede cocinar con ellos.
// Agregar, cambiar cantidad, quitar, vaciar y elegir una favorita se llaman con fetch (js/despensa.js) y
// responden la vista parcial _Contenido, que el JavaScript reemplaza en la página (D-07, Parte 15 del enunciado).
using System.Globalization;
using CenaApp.Web.Services;
using CenaApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CenaApp.Web.Controllers;

[Authorize]
public class DespensaController : Controller
{
    // Accesos rápidos de la pantalla (RF17): se muestran los que el usuario todavía no tiene.
    private static readonly string[] IngredientesComunes =
        ["Huevo", "Papa", "Cebolla", "Tomate", "Arroz", "Zanahoria", "Ajo", "Limón", "Leche", "Garbanzos", "Lentejas", "Palta"];

    // Si un ingrediente no aparece en ninguna receta, se ofrecen estas unidades.
    private static readonly List<string> UnidadesGenerales = ["g", "ml", "ud"];

    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

    private readonly IDespensaService _despensa;
    private readonly IRecetaService _recetas;

    public DespensaController(IDespensaService despensa, IRecetaService recetas)
    {
        _despensa = despensa;
        _recetas = recetas;
    }

    // GET /Despensa?favorita=6
    [HttpGet]
    public async Task<IActionResult> Index(int? favorita)
    {
        var catalogo = await _despensa.CatalogoAsync();
        var unidades = await _despensa.UnidadesAsync();
        var modelo = new DespensaViewModel
        {
            Catalogo = catalogo
                .Select(i => new IngredienteCatalogoViewModel(i.Nombre, i.Sinonimos, unidades.GetValueOrDefault(i.Id, UnidadesGenerales)))
                .ToList(),
            // Los avisos de una acción sin JavaScript (TempData) los muestra _Layout.
            Contenido = await ArmarContenidoAsync(null, favorita),
        };
        return View(modelo);
    }

    // GET /Despensa/Contenido?favorita=6 → solo el bloque (cuando cambia la lista desplegable de favoritas).
    [HttpGet]
    public async Task<IActionResult> Contenido(int? favorita)
    {
        return PartialView("_Contenido", await ArmarContenidoAsync(null, favorita));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(string? texto, decimal? cantidad, string? unidad, int? favorita)
    {
        var resultado = await _despensa.AgregarAsync(UsuarioActual.Id(User), texto ?? "", cantidad, unidad);
        return await ResponderAsync(resultado, favorita);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cambiar(int id, decimal? cantidad, string? unidad, int? favorita)
    {
        var resultado = await _despensa.CambiarCantidadAsync(UsuarioActual.Id(User), id, cantidad, unidad);
        return await ResponderAsync(resultado, favorita);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(int id, int? favorita)
    {
        var resultado = await _despensa.QuitarAsync(UsuarioActual.Id(User), id);
        return await ResponderAsync(resultado, favorita);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vaciar(int? favorita)
    {
        var resultado = await _despensa.VaciarAsync(UsuarioActual.Id(User));
        return await ResponderAsync(resultado, favorita);
    }

    // Si la petición viene de fetch, devuelve solo el bloque a reemplazar.
    // Si vino de un formulario común (JavaScript desactivado), vuelve a la página completa.
    private async Task<IActionResult> ResponderAsync(ResultadoOperacion resultado, int? favorita)
    {
        if (Request.Headers["X-Requested-With"] == "fetch")
        {
            return PartialView("_Contenido", await ArmarContenidoAsync(resultado, favorita));
        }
        TempData[resultado.Ok ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { favorita });
    }

    private async Task<DespensaContenidoViewModel> ArmarContenidoAsync(ResultadoOperacion? aviso, int? favorita)
    {
        var usuarioId = UsuarioActual.Id(User);
        var items = await _despensa.ObtenerAsync(usuarioId);
        var catalogo = await _despensa.CatalogoAsync();
        var unidades = await _despensa.UnidadesAsync();
        var sugerencias = await _despensa.SugerenciasAsync(usuarioId);
        var favoritas = await _recetas.ListarFavoritosAsync(usuarioId);

        // Solo se analiza la favorita si sigue siendo favorita del usuario.
        var elegida = favoritas.Any(f => f.Id == favorita) ? favorita : null;
        var analisisFavorita = elegida is null ? null : await _despensa.AnalizarRecetaAsync(usuarioId, elegida.Value);

        return new DespensaContenidoViewModel
        {
            Mensaje = aviso?.Mensaje,
            MensajeOk = aviso?.Ok ?? false,
            Ingredientes = items.Select(d => new FilaDespensaViewModel
            {
                IngredienteId = d.IngredienteId,
                Nombre = d.Ingrediente.Nombre,
                Cantidad = d.Cantidad,
                Unidad = d.Unidad,
                // Las de las recetas, más la que tenga guardada si es otra (para no perderla al editar).
                Unidades = unidades.GetValueOrDefault(d.IngredienteId, UnidadesGenerales)
                    .Append(d.Unidad).OfType<string>().Distinct().ToList(),
            }).ToList(),
            Rapidos = IngredientesComunes
                .Select(n => catalogo.FirstOrDefault(i => i.Nombre == n))
                .Where(i => i is not null && items.All(t => t.IngredienteId != i.Id))
                .Take(6)
                .ToList()!,
            PodesCocinar = sugerencias.PodesCocinar.Select(AVista).ToList(),
            CasiListas = sugerencias.CasiListas.Select(AVista).ToList(),
            Favoritas = favoritas.Select(f => (f.Id, f.Nombre)).ToList(),
            FavoritaElegida = elegida,
            AnalisisFavorita = analisisFavorita is null ? null : AVista(analisisFavorita),
        };
    }

    private static RecetaDespensaViewModel AVista(AnalisisReceta a) => new()
    {
        Id = a.Receta.Id,
        Nombre = a.Receta.Nombre,
        Emoji = a.Receta.Emoji,
        TiempoMin = a.Receta.TiempoMin,
        Dificultad = a.Receta.Dificultad,
        Porcentaje = a.Porcentaje,
        Faltantes = a.Faltantes.Select(f => new FaltanteViewModel
        {
            IngredienteId = f.Ingrediente.Id,
            Nombre = f.Ingrediente.Nombre,
            Cantidad = f.Cantidad,
            Unidad = f.Unidad,
            Texto = TextoFaltante(f),
        }).ToList(),
    };

    // "Queso fresco (30 g)", "Huevo (falta 1 ud, tenés 1)" o solo el nombre si es "a gusto".
    // Coma decimal siempre (es-AR), sin depender del servidor.
    private static string TextoFaltante(Faltante f)
    {
        if (f.Cantidad is null)
        {
            return f.Ingrediente.Nombre;
        }
        var cantidad = $"{f.Cantidad.Value.ToString("0.##", Ar)} {f.Unidad}".Trim();
        return f.EnCasa is null
            ? $"{f.Ingrediente.Nombre} ({cantidad})"
            : $"{f.Ingrediente.Nombre} (falta {cantidad}, tenés {f.EnCasa.Value.ToString("0.##", Ar)})";
    }
}
