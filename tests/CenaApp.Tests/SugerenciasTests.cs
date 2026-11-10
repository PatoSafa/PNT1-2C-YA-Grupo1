// Tests de "Te puede gustar" (D-26): parecido entre recetas y sugerencias a partir de las favoritas.
using CenaApp.Web.Models;
using CenaApp.Web.Services;

namespace CenaApp.Tests;

public class SugerenciasTests
{
    private static readonly Gusto Italiana = new() { Id = 1, Nombre = "Italiana" };
    private static readonly Gusto Rapido = new() { Id = 2, Nombre = "Rápido" };

    private static readonly Ingrediente Papa = new() { Id = 1, Nombre = "Papa" };
    private static readonly Ingrediente Tomate = new() { Id = 2, Nombre = "Tomate" };
    private static readonly Ingrediente Arroz = new() { Id = 3, Nombre = "Arroz" };
    private static readonly Ingrediente Sal = new() { Id = 4, Nombre = "Sal", CuentaEnDespensa = false };

    private static Receta Receta(int id, string nombre, int categoriaId, Gusto[] gustos, params Ingrediente[] ingredientes) => new()
    {
        Id = id,
        Nombre = nombre,
        CategoriaId = categoriaId,
        Gustos = gustos.ToList(),
        Ingredientes = ingredientes.Select(i => new RecetaIngrediente { Ingrediente = i, IngredienteId = i.Id }).ToList(),
    };

    [Fact]
    public void Parecido_SumaGustosCategoriaEIngredientesEnComun()
    {
        var noquis = Receta(1, "Ñoquis", 1, [Italiana, Rapido], Papa, Tomate, Sal);
        var pure = Receta(2, "Puré", 1, [Italiana], Papa, Sal);

        // 1 gusto (2 puntos) + misma categoría (1) + papa (1); la sal no cuenta.
        Assert.Equal(4, RecetaService.Parecido(noquis, pure));
    }

    [Fact]
    public void Sugerir_SinFavoritas_NoSugiereNada()
    {
        var recetas = new[] { Receta(1, "Ñoquis", 1, [Italiana], Papa) };

        Assert.Empty(RecetaService.Sugerir(recetas, [], 4));
    }

    [Fact]
    public void Sugerir_NoRepiteFavoritas_NiSugiereLasQueNoSeParecenEnNada()
    {
        var favorita = Receta(1, "Ñoquis", 1, [Italiana], Papa);
        var parecida = Receta(2, "Puré", 2, [], Papa);
        var distinta = Receta(3, "Arroz blanco", 3, [], Arroz);

        var sugerencias = RecetaService.Sugerir([favorita, parecida, distinta], [favorita], 4);

        Assert.Equal(["Puré"], sugerencias.Select(s => s.Receta.Nombre));
        Assert.Equal("Ñoquis", sugerencias[0].ParecidaA);
    }

    [Fact]
    public void Sugerir_PrimeroLasMasParecidas_YRespetaLaCantidad()
    {
        var favorita = Receta(1, "Ñoquis", 1, [Italiana, Rapido], Papa, Tomate);
        var muyParecida = Receta(2, "Gnocchi al pesto", 1, [Italiana, Rapido], Papa);  // 4 + 1 + 1 = 6
        var algoParecida = Receta(3, "Ensalada", 2, [Rapido], Tomate);                  // 2 + 0 + 1 = 3
        var pocoParecida = Receta(4, "Tortilla", 2, [], Papa);                          // 0 + 0 + 1 = 1

        var sugerencias = RecetaService.Sugerir([pocoParecida, algoParecida, muyParecida], [favorita], 2);

        Assert.Equal(["Gnocchi al pesto", "Ensalada"], sugerencias.Select(s => s.Receta.Nombre));
    }

    [Fact]
    public void Sugerir_SoloEntreLasCandidatas_QueYaVienenFiltradasParaElUsuario()
    {
        // Las candidatas son lo que dejó pasar FiltrarYOrdenar: si una no está (ej: por una alergia), no se sugiere.
        var favorita = Receta(1, "Ñoquis", 1, [Italiana], Papa);
        var noApta = Receta(2, "Ñoquis con queso", 1, [Italiana], Papa);

        Assert.Empty(RecetaService.Sugerir([favorita], [favorita, noApta], 4));
    }
}
