// Datos que reciben la pantalla de Tiendas y su lista de comercios (que se pide con fetch).
using CenaApp.Web.Services;

namespace CenaApp.Web.ViewModels;

// Página: filtros (tipo e ingredientes a comprar) y el punto por defecto del mapa.
public class TiendasViewModel
{
    public string? Tipo { get; set; }
    public List<string> Tipos { get; set; } = [];
    // Ingredientes que llegan desde "Ver tiendas" de la Despensa.
    public List<(int Id, string Nombre)> Ingredientes { get; set; } = [];
    public (double Lat, double Lng, string Nombre) Centro { get; set; }
}

// Lista de comercios (vista parcial _Lista).
public class TiendasListaViewModel
{
    public List<Comercio> Comercios { get; set; } = [];
    // OpenStreetMap no respondió: son los comercios de ejemplo.
    public bool SonDeEjemplo { get; set; }
    // false si el usuario no compartió su ubicación y se busca desde ORT Yatay.
    public bool UbicacionReal { get; set; }
    public string NombreCentro { get; set; } = "";
    public bool FiltraPorIngredientes { get; set; }
}
