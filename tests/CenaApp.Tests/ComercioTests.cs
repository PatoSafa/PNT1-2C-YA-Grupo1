// Tests de Tiendas (D-20): lectura de la respuesta de OpenStreetMap, filtro por ingredientes y respaldo si Overpass falla.
// En lugar de llamar a internet, el HttpClient usa un manejador falso que devuelve una respuesta fija.
using System.Net;
using CenaApp.Web.Models;
using CenaApp.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CenaApp.Tests;

public class ComercioTests
{
    // Respuesta real de Overpass (recortada) para la zona de ORT Yatay.
    private const string RespuestaOverpass = """
        { "elements": [
          { "type": "node", "id": 1, "lat": -34.6148, "lon": -58.4268,
            "tags": { "name": "Supermercado Luis", "shop": "supermarket" } },
          { "type": "node", "id": 2, "lat": -34.6087, "lon": -58.4229,
            "tags": { "name": "Andrés", "shop": "butcher", "addr:street": "Yatay", "addr:housenumber": "100", "opening_hours": "Mo-Sa 08:00-13:00" } },
          { "type": "way", "id": 3, "center": { "lat": -34.6100, "lon": -58.4290 },
            "tags": { "name": "Verdu Pepe", "shop": "greengrocer" } },
          { "type": "node", "id": 4, "lat": -34.6100, "lon": -58.4290,
            "tags": { "name": "Ferretería", "shop": "hardware" } }
        ] }
        """;

    private static readonly Ingrediente Carne = new() { Id = 1, Nombre = "Carne vacuna", Grupo = "Carne" };
    private static readonly Ingrediente Tomate = new() { Id = 2, Nombre = "Tomate", Grupo = "Verdura" };

    // Manejador HTTP falso: responde siempre lo mismo (o falla) sin salir a internet.
    private class ManejadorFalso(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken cancelacion) =>
            Task.FromResult(responder());
    }

    private static ComercioService Servicio(Func<HttpResponseMessage> responder) => new(
        new HttpClient(new ManejadorFalso(responder)),
        new MemoryCache(new MemoryCacheOptions()),
        NullLogger<ComercioService>.Instance,
        new ConfigurationBuilder().Build());

    private static HttpResponseMessage Ok() => new(HttpStatusCode.OK) { Content = new StringContent(RespuestaOverpass) };

    [Fact]
    public async Task Buscar_LeeLosComerciosDeOpenStreetMap_EIgnoraLosQueNoSonDeAlimentos()
    {
        var resultado = await Servicio(Ok).BuscarAsync(-34.61, -58.429, null, []);

        Assert.False(resultado.SonDeEjemplo);
        Assert.Equal(new[] { "Andrés", "Supermercado Luis", "Verdu Pepe" }, resultado.Comercios.Select(c => c.Nombre).Order());
        var carniceria = resultado.Comercios.Single(c => c.Nombre == "Andrés");
        Assert.Equal("Carnicería", carniceria.Tipo);
        Assert.Equal("Yatay 100", carniceria.Direccion);
        Assert.Equal("Lun-Sáb 08:00-13:00", carniceria.Horario);
    }

    [Fact]
    public async Task Buscar_ConIngredientes_SoloDondeSeConsiguen_PrimeroLosQueTienenMas()
    {
        var resultado = await Servicio(Ok).BuscarAsync(-34.61, -58.429, null, [Carne, Tomate]);

        // El súper tiene las dos cosas; la carnicería solo carne y la verdulería solo tomate.
        Assert.Equal("Supermercado Luis", resultado.Comercios[0].Nombre);
        Assert.Equal(["Carne vacuna", "Tomate"], resultado.Comercios[0].Consigue);
        Assert.Equal(3, resultado.Comercios.Count);
    }

    [Fact]
    public async Task Buscar_PorTipo_DevuelveSoloEseTipo()
    {
        var resultado = await Servicio(Ok).BuscarAsync(-34.61, -58.429, "Verdulería", []);

        Assert.Equal(["Verdu Pepe"], resultado.Comercios.Select(c => c.Nombre));
    }

    [Fact]
    public async Task Buscar_SiOverpassFalla_UsaLosComerciosDeEjemplo()
    {
        var resultado = await Servicio(() => throw new HttpRequestException("sin conexión")).BuscarAsync(-34.61, -58.429, null, []);

        Assert.True(resultado.SonDeEjemplo);
        Assert.NotEmpty(resultado.Comercios);
    }

    [Theory]
    [InlineData("Carne", "Carnicería", true)]
    [InlineData("Carne", "Verdulería", false)]
    [InlineData("Verdura", "Supermercado", true)]
    [InlineData("Básico", "Almacén", true)]
    public void SeVendeEn_SegunElGrupoDelIngrediente(string grupo, string tipo, bool esperado)
    {
        Assert.Equal(esperado, ComercioService.SeVendeEn(grupo, tipo));
    }
}
