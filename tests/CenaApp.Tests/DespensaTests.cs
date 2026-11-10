// Tests de las reglas de la despensa: porcentaje, "Podés cocinar" / "Casi listas" y búsqueda por sinónimo.
using CenaApp.Web.Models;
using CenaApp.Web.Services;

namespace CenaApp.Tests;

public class DespensaTests
{
    private static readonly Ingrediente Papa = new() { Id = 1, Nombre = "Papa", Sinonimos = "patata" };
    private static readonly Ingrediente Huevo = new() { Id = 2, Nombre = "Huevo" };
    private static readonly Ingrediente Cebolla = new() { Id = 3, Nombre = "Cebolla" };
    private static readonly Ingrediente Queso = new() { Id = 4, Nombre = "Queso" };
    private static readonly Ingrediente Palta = new() { Id = 5, Nombre = "Palta", Sinonimos = "aguacate" };
    private static readonly Ingrediente Limon = new() { Id = 6, Nombre = "Limón", Sinonimos = "lima" };
    private static readonly Ingrediente Aceite = new() { Id = 7, Nombre = "Aceite", CuentaEnDespensa = false };
    private static readonly Ingrediente Sal = new() { Id = 8, Nombre = "Sal", CuentaEnDespensa = false };

    private static Receta Receta(string nombre, params Ingrediente[] ingredientes) => new()
    {
        Nombre = nombre,
        Ingredientes = ingredientes.Select(i => new RecetaIngrediente { Ingrediente = i, IngredienteId = i.Id }).ToList(),
    };

    // Despensa sin cantidades (= alcanza para cualquier receta).
    private static List<DespensaItem> Tengo(params Ingrediente[] ingredientes) =>
        ingredientes.Select(i => new DespensaItem { IngredienteId = i.Id, Ingrediente = i }).ToList();

    private static RecetaIngrediente Necesito(Ingrediente i, decimal? cantidad, string? unidad) =>
        new() { Ingrediente = i, IngredienteId = i.Id, Cantidad = cantidad, Unidad = unidad };

    // ---------- Porcentaje (VN-10) ----------

    [Fact]
    public void Analizar_CuentaLosQueTieneYListaLosQueFaltan()
    {
        var tortilla = Receta("Tortilla", Papa, Huevo, Cebolla, Queso);

        var analisis = DespensaService.Analizar(tortilla, Tengo(Papa, Huevo, Cebolla));

        Assert.Equal(75, analisis.Porcentaje);
        Assert.Equal(["Queso"], analisis.Faltantes.Select(f => f.Ingrediente.Nombre));
    }

    [Fact]
    public void Analizar_NoCuentaLosBasicosComoAceiteOSal()
    {
        // Sin contar aceite y sal, tiene los 2 que cuentan: 100 %.
        var tortilla = Receta("Tortilla", Papa, Huevo, Aceite, Sal);

        var analisis = DespensaService.Analizar(tortilla, Tengo(Papa, Huevo));

        Assert.Equal(100, analisis.Porcentaje);
        Assert.Empty(analisis.Faltantes);
    }

    [Fact]
    public void Analizar_DespensaVacia_EsCeroPorCiento()
    {
        var tortilla = Receta("Tortilla", Papa, Huevo);

        Assert.Equal(0, DespensaService.Analizar(tortilla, Tengo()).Porcentaje);
    }

    // ---------- Clasificación (VN-11 a VN-13) ----------

    [Fact]
    public void Clasificar_SeparaPodesCocinarYCasiListas_YDescartaLasDeMenosDel50()
    {
        var recetas = new[]
        {
            Receta("Puré", Papa),                          // 100 %
            Receta("Tortilla", Papa, Huevo, Cebolla),      // 66 %
            Receta("Papas con queso", Papa, Queso),        // 50 %
            Receta("Guacamole", Palta, Limon, Cebolla),    // 0 %
        };

        var resultado = DespensaService.Clasificar(recetas, Tengo(Papa, Huevo));

        Assert.Equal(["Puré"], resultado.PodesCocinar.Select(a => a.Receta.Nombre));
        Assert.Equal(["Tortilla", "Papas con queso"], resultado.CasiListas.Select(a => a.Receta.Nombre));
    }

    [Fact]
    public void Clasificar_CasiListasVanDeMayorAMenorPorcentaje()
    {
        var recetas = new[]
        {
            Receta("A - mitad", Papa, Queso),                 // 50 %
            Receta("B - casi todo", Papa, Huevo, Cebolla, Queso), // 75 %
        };

        var resultado = DespensaService.Clasificar(recetas, Tengo(Papa, Huevo, Cebolla));

        Assert.Equal(["B - casi todo", "A - mitad"], resultado.CasiListas.Select(a => a.Receta.Nombre));
    }

    // ---------- Cantidades opcionales (D-19) ----------

    [Fact]
    public void CalcularFaltante_SinCantidadEnCasa_Alcanza()
    {
        var enCasa = new DespensaItem { IngredienteId = Papa.Id, Ingrediente = Papa };

        Assert.Null(DespensaService.CalcularFaltante(Necesito(Papa, 500, "g"), enCasa));
    }

    [Fact]
    public void CalcularFaltante_ConCantidadQueAlcanza_NoFaltaNada()
    {
        var enCasa = new DespensaItem { IngredienteId = Papa.Id, Ingrediente = Papa, Cantidad = 1000, Unidad = "g" };

        Assert.Null(DespensaService.CalcularFaltante(Necesito(Papa, 500, "g"), enCasa));
    }

    [Fact]
    public void CalcularFaltante_ConCantidadQueNoAlcanza_FaltaLaDiferencia()
    {
        var enCasa = new DespensaItem { IngredienteId = Huevo.Id, Ingrediente = Huevo, Cantidad = 1, Unidad = "ud" };

        var faltante = DespensaService.CalcularFaltante(Necesito(Huevo, 3, "ud"), enCasa);

        Assert.Equal(2, faltante?.Cantidad);
        Assert.Equal(1, faltante?.EnCasa);
    }

    [Fact]
    public void CalcularFaltante_SiNoEstaEnCasa_FaltaTodo()
    {
        var faltante = DespensaService.CalcularFaltante(Necesito(Queso, 30, "g"), null);

        Assert.Equal(30, faltante?.Cantidad);
        Assert.Null(faltante?.EnCasa);
    }

    [Fact]
    public void Analizar_UnIngredienteQueNoAlcanza_CuentaComoFaltante()
    {
        var tortilla = new Receta { Nombre = "Tortilla", Ingredientes = [Necesito(Papa, 200, "g"), Necesito(Huevo, 2, "ud")] };
        var despensa = new List<DespensaItem>
        {
            new() { IngredienteId = Papa.Id, Ingrediente = Papa },                               // sin cantidad: alcanza
            new() { IngredienteId = Huevo.Id, Ingrediente = Huevo, Cantidad = 1, Unidad = "ud" }, // falta 1
        };

        var analisis = DespensaService.Analizar(tortilla, despensa);

        Assert.Equal(50, analisis.Porcentaje);
        Assert.Equal(["Huevo"], analisis.Faltantes.Select(f => f.Ingrediente.Nombre));
    }

    // ---------- Búsqueda por nombre o sinónimo (RF17) ----------

    [Theory]
    [InlineData("Palta")]
    [InlineData("palta")]
    [InlineData("  AGUACATE ")]
    public void Buscar_EncuentraPorNombreOSinonimoSinImportarMayusculas(string texto)
    {
        var encontrado = DespensaService.Buscar([Papa, Palta, Limon], texto);

        Assert.Equal("Palta", encontrado?.Nombre);
    }

    [Fact]
    public void Buscar_IgnoraLasTildes()
    {
        Assert.Equal("Limón", DespensaService.Buscar([Limon], "limon")?.Nombre);
    }

    [Fact]
    public void Buscar_SiNoEstaEnElCatalogo_DevuelveNull()
    {
        Assert.Null(DespensaService.Buscar([Papa, Palta], "kiwi"));
    }
}
