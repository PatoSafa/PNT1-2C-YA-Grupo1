// Datos que reciben la pantalla de Despensa y su vista parcial de resultados.
using CenaApp.Web.Models;

namespace CenaApp.Web.ViewModels;

// Página completa: buscador y el bloque que se reemplaza con fetch.
public class DespensaViewModel
{
    // Para el <datalist> del buscador: nombre, sinónimos y unidades que usan las recetas para ese ingrediente.
    public List<IngredienteCatalogoViewModel> Catalogo { get; set; } = [];
    public DespensaContenidoViewModel Contenido { get; set; } = new();
}

public record IngredienteCatalogoViewModel(string Nombre, string? Sinonimos, List<string> Unidades);

// Lo que se actualiza con fetch (vista parcial _Contenido): la despensa y las recetas sugeridas.
public class DespensaContenidoViewModel
{
    // Aviso de la última acción (verde si salió bien, amarillo si no). null si no hay.
    public string? Mensaje { get; set; }
    public bool MensajeOk { get; set; }

    public List<FilaDespensaViewModel> Ingredientes { get; set; } = [];
    // Accesos rápidos: ingredientes comunes que todavía no están en la despensa (RF17).
    public List<Ingrediente> Rapidos { get; set; } = [];
    public List<RecetaDespensaViewModel> PodesCocinar { get; set; } = [];
    public List<RecetaDespensaViewModel> CasiListas { get; set; } = [];

    // Lista desplegable "¿Qué me falta para...?" con las favoritas del usuario, y la elegida analizada.
    public List<(int Id, string Nombre)> Favoritas { get; set; } = [];
    public int? FavoritaElegida { get; set; }
    public RecetaDespensaViewModel? AnalisisFavorita { get; set; }
}

// Una fila de la tabla "Tengo en casa".
public class FilaDespensaViewModel
{
    public int IngredienteId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? Cantidad { get; set; }
    public string? Unidad { get; set; }
    // Unidades para el <select>: las que usan las recetas para este ingrediente.
    public List<string> Unidades { get; set; } = [];
}

public class RecetaDespensaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Emoji { get; set; } = "";
    public int TiempoMin { get; set; }
    public Dificultad Dificultad { get; set; }
    public int Porcentaje { get; set; }
    public List<FaltanteViewModel> Faltantes { get; set; } = [];
}

// Un ingrediente a comprar, con lo necesario para agregarlo con el botón "+".
public class FaltanteViewModel
{
    public int IngredienteId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal? Cantidad { get; set; }
    public string? Unidad { get; set; }
    // "Queso fresco (30 g)" o "Huevo (falta 1 ud, tenés 1)".
    public string Texto { get; set; } = "";
}
