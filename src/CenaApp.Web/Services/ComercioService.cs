// Comercios reales cerca del usuario, consultados en vivo a OpenStreetMap con la Overpass API (gratis, sin clave) (D-20).
// Cada grupo de ingrediente se vende en ciertos tipos de comercio: así se muestran solo los lugares donde se consigue
// lo que el usuario necesita. Si Overpass no responde, se usa la lista de ejemplo alrededor de ORT Yatay.
using System.Globalization;
using System.Text.Json;
using CenaApp.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CenaApp.Web.Services;

public class ComercioService : IComercioService
{
    // Radio de búsqueda alrededor del usuario.
    private const int RadioMetros = 1500;
    private const int MaximoResultados = 30;

    // Etiqueta "shop" de OpenStreetMap → tipo de comercio que ve el usuario.
    private static readonly Dictionary<string, (string Tipo, string Emoji)> TiposOsm = new()
    {
        ["supermarket"] = ("Supermercado", "🛒"),
        ["convenience"] = ("Almacén", "🏪"),
        ["greengrocer"] = ("Verdulería", "🥬"),
        ["butcher"] = ("Carnicería", "🥩"),
        ["seafood"] = ("Pescadería", "🐟"),
        ["bakery"] = ("Panadería", "🥖"),
        ["health_food"] = ("Dietética", "🌾"),
        ["deli"] = ("Fiambrería", "🧀"),
        ["cheese"] = ("Fiambrería", "🧀"),
        ["dairy"] = ("Fiambrería", "🧀"),
    };

    // Grupo del ingrediente (Ingrediente.Grupo) → tipos de comercio donde se compra.
    private static readonly Dictionary<string, string[]> DondeSeCompra = new()
    {
        ["Verdura"] = ["Verdulería", "Supermercado", "Almacén"],
        ["Fruta"] = ["Verdulería", "Supermercado", "Almacén"],
        ["Carne"] = ["Carnicería", "Supermercado"],
        ["Pescado"] = ["Pescadería", "Supermercado"],
        ["Huevo"] = ["Verdulería", "Almacén", "Supermercado"],
        ["Lácteo"] = ["Fiambrería", "Almacén", "Supermercado"],
        ["Cereal y harina"] = ["Dietética", "Almacén", "Supermercado"],
        ["Legumbre"] = ["Dietética", "Almacén", "Supermercado"],
        ["Fruto seco y semilla"] = ["Dietética", "Supermercado"],
    };
    private static readonly string[] DondeSeCompraLoDemas = ["Almacén", "Supermercado", "Dietética"];

    // Respaldo si Overpass no responde: comercios de ejemplo en direcciones reales de Almagro (D-15).
    private static readonly Lugar[] Ejemplos =
    [
        new("ej1", "Súper La Esquina", "Supermercado", "🛒", "Av. Rivadavia 4500", "Lun a Sáb 8 a 21 h · Dom 9 a 14 h", -34.61485, -58.42839),
        new("ej2", "Verdulería Don Ramón", "Verdulería", "🥬", "Yatay 420", "Lun a Sáb 7 a 20 h", -34.60689, -58.42927),
        new("ej3", "Carnicería El Novillo", "Carnicería", "🥩", "Av. Corrientes 4300", "Lun a Sáb 8 a 13 y 17 a 20:30 h", -34.60282, -58.42549),
        new("ej4", "Dietética Semillas", "Dietética", "🌾", "Bogado 4400", "Lun a Vie 9 a 19 h · Sáb 9 a 13 h", -34.60520, -58.42760),
        new("ej5", "Almacén Doña Rosa", "Almacén", "🏪", "Pringles 410", "Todos los días 8 a 22 h", -34.60706, -58.42775),
        new("ej6", "Panadería La Espiga", "Panadería", "🥖", "Yatay 150", "Todos los días 7 a 21 h", -34.61177, -58.42883),
        new("ej7", "Pescadería Puerto Sur", "Pescadería", "🐟", "Av. Rivadavia 4650", "Mar a Sáb 8 a 14 h", -34.61606, -58.43106),
        new("ej8", "Fiambrería La Quesera", "Fiambrería", "🧀", "Medrano 220", "Lun a Sáb 9 a 20 h", -34.60640, -58.42080),
    ];

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ComercioService> _log;
    private readonly string _urlOverpass;

    public ComercioService(HttpClient http, IMemoryCache cache, ILogger<ComercioService> log, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _log = log;
        _urlOverpass = config["Overpass:Url"] ?? "https://overpass-api.de/api/interpreter";
    }

    public (double Lat, double Lng, string Nombre) Centro => (-34.60995, -58.42914, "ORT Yatay"); // Yatay 240

    public List<string> Tipos() => TiposOsm.Values.Select(t => t.Tipo).Distinct().Order().ToList();

    public async Task<ResultadoComercios> BuscarAsync(double lat, double lng, string? tipo, List<Ingrediente> ingredientes)
    {
        var lugares = await LugaresCercanosAsync(lat, lng);
        var sonDeEjemplo = lugares is null;
        lugares ??= [.. Ejemplos];

        var comercios = lugares
            .Where(l => string.IsNullOrEmpty(tipo) || l.Tipo == tipo)
            .Select(l => new Comercio(l.Id, l.Nombre, l.Tipo, l.Emoji, l.Direccion, l.Horario, l.Lat, l.Lng,
                DistanciaKm(lat, lng, l.Lat, l.Lng),
                ingredientes.Where(i => SeVendeEn(i.Grupo, l.Tipo)).Select(i => i.Nombre).ToList()))
            .Where(c => ingredientes.Count == 0 || c.Consigue.Count > 0)
            .OrderByDescending(c => c.Consigue.Count)
            .ThenBy(c => c.DistanciaKm)
            .Take(MaximoResultados)
            .ToList();

        return new ResultadoComercios(comercios, sonDeEjemplo);
    }

    // true si un ingrediente de ese grupo se consigue en ese tipo de comercio.
    public static bool SeVendeEn(string grupo, string tipoComercio) =>
        DondeSeCompra.GetValueOrDefault(grupo, DondeSeCompraLoDemas).Contains(tipoComercio);

    // Comercios de alimentos en el radio, desde OpenStreetMap. Se guardan 30 minutos en memoria por zona
    // (~100 m) para no repetir la consulta. null si Overpass no respondió.
    private async Task<List<Lugar>?> LugaresCercanosAsync(double lat, double lng)
    {
        var clave = $"osm:{lat:F3}:{lng:F3}";
        if (_cache.TryGetValue(clave, out List<Lugar>? guardados))
        {
            return guardados;
        }

        var punto = $"{lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}";
        var consulta = $"""
            [out:json][timeout:15];
            nwr["shop"~"^({string.Join('|', TiposOsm.Keys)})$"]["name"](around:{RadioMetros},{punto});
            out center tags 300;
            """;
        try
        {
            using var respuesta = await _http.PostAsync(_urlOverpass, new FormUrlEncodedContent([new("data", consulta)]));
            respuesta.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

            var lugares = json.RootElement.GetProperty("elements").EnumerateArray()
                .Select(ALugar)
                .OfType<Lugar>()
                .ToList();
            _cache.Set(clave, lugares, TimeSpan.FromMinutes(30));
            return lugares;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _log.LogWarning(error, "Overpass no respondió; se usan los comercios de ejemplo.");
            return null;
        }
    }

    // Un elemento de OpenStreetMap → Lugar. Los nodos traen lat/lon; los edificios (way), un "center".
    private static Lugar? ALugar(JsonElement e)
    {
        var etiquetas = e.GetProperty("tags");
        var shop = Texto(etiquetas, "shop");
        if (shop is null || !TiposOsm.TryGetValue(shop, out var tipo))
        {
            return null;
        }
        var ubicacion = e.TryGetProperty("center", out var centro) ? centro : e;
        if (!ubicacion.TryGetProperty("lat", out var lat) || !ubicacion.TryGetProperty("lon", out var lon))
        {
            return null;
        }

        var calle = Texto(etiquetas, "addr:street");
        var direccion = calle is null ? "Dirección no informada" : $"{calle} {Texto(etiquetas, "addr:housenumber")}".Trim();
        var horario = Texto(etiquetas, "opening_hours") is { } h ? TraducirHorario(h) : "Horario no informado";

        return new Lugar($"{e.GetProperty("type").GetString()}/{e.GetProperty("id").GetInt64()}",
            Texto(etiquetas, "name")!, tipo.Tipo, tipo.Emoji, direccion, horario, lat.GetDouble(), lon.GetDouble());
    }

    private static string? Texto(JsonElement etiquetas, string nombre) =>
        etiquetas.TryGetProperty(nombre, out var valor) ? valor.GetString() : null;

    // "Mo-Sa 08:00-21:00; Su off" → "Lun-Sáb 08:00-21:00 · Dom cerrado" (formato opening_hours de OpenStreetMap).
    public static string TraducirHorario(string horario)
    {
        var dias = new Dictionary<string, string>
        {
            ["Mo"] = "Lun", ["Tu"] = "Mar", ["We"] = "Mié", ["Th"] = "Jue", ["Fr"] = "Vie", ["Sa"] = "Sáb", ["Su"] = "Dom",
            ["PH"] = "Feriados", ["off"] = "cerrado", ["24/7"] = "Abierto las 24 h",
        };
        var traducido = dias.Aggregate(horario, (texto, d) => texto.Replace(d.Key, d.Value));
        return traducido.Replace("; ", " · ").Replace(";", " · ");
    }

    // Distancia en línea recta entre dos puntos (fórmula del haversine), redondeada a 0,1 km.
    public static double DistanciaKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double radioTierraKm = 6371;
        double ARadianes(double grados) => grados * Math.PI / 180;

        var dLat = ARadianes(lat2 - lat1);
        var dLng = ARadianes(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ARadianes(lat1)) * Math.Cos(ARadianes(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var distancia = radioTierraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(distancia, 1);
    }

    // Un comercio tal como viene de OpenStreetMap (o de la lista de ejemplo), sin distancia.
    private record Lugar(string Id, string Nombre, string Tipo, string Emoji, string Direccion, string Horario, double Lat, double Lng);
}
