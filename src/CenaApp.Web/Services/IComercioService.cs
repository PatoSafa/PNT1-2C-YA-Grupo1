// Contrato del servicio de comercios: dónde comprar cerca del usuario (pantalla Tiendas, D-20).
using CenaApp.Web.Models;

namespace CenaApp.Web.Services;

public interface IComercioService
{
    // Punto por defecto si el usuario no comparte su ubicación (ORT Yatay).
    (double Lat, double Lng, string Nombre) Centro { get; }

    // Tipos de comercio, para los filtros.
    List<string> Tipos();

    // Comercios de alimentos cerca de (lat, lng), del tipo pedido (o todos). Si se pasan ingredientes, solo los
    // comercios que venden al menos uno, primero los que venden más y después los más cercanos (RF20).
    Task<ResultadoComercios> BuscarAsync(double lat, double lng, string? tipo, List<Ingrediente> ingredientes);
}

// Un comercio. No es una entidad: no se guarda en la base, sale de OpenStreetMap (o de la lista de ejemplo).
// Consigue: cuáles de los ingredientes buscados se pueden comprar ahí.
public record Comercio(
    string Id,
    string Nombre,
    string Tipo,
    string Emoji,
    string Direccion,
    string Horario,
    double Lat,
    double Lng,
    double DistanciaKm,
    List<string> Consigue);

// SonDeEjemplo: true si OpenStreetMap no respondió y se muestran los comercios fijos de ejemplo.
public record ResultadoComercios(List<Comercio> Comercios, bool SonDeEjemplo);
