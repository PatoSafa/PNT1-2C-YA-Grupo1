// Contrato del servicio de despensa: los ingredientes que el usuario tiene en casa y qué puede cocinar con ellos.
using CenaApp.Web.Models;

namespace CenaApp.Web.Services;

public interface IDespensaService
{
    // Lo que hay en la despensa del usuario (con el ingrediente cargado), por nombre.
    Task<List<DespensaItem>> ObtenerAsync(int usuarioId);

    // Todo el catálogo de ingredientes (para las sugerencias del buscador).
    Task<List<Ingrediente>> CatalogoAsync();

    // Unidades que usan las recetas para cada ingrediente (id → ["g"]), para ofrecer las mismas en la despensa.
    Task<Dictionary<int, List<string>>> UnidadesAsync();

    // Agrega un ingrediente buscándolo por nombre o sinónimo, con cantidad opcional. Falla si no existe en el
    // catálogo (VAL-13) o si ya estaba sin cantidad (VAL-12). Si ya estaba con cantidad, la suma.
    Task<ResultadoOperacion> AgregarAsync(int usuarioId, string texto, decimal? cantidad = null, string? unidad = null);

    // Cambia (o borra, con null) la cantidad de un ingrediente que ya está en la despensa.
    Task<ResultadoOperacion> CambiarCantidadAsync(int usuarioId, int ingredienteId, decimal? cantidad, string? unidad);

    Task<ResultadoOperacion> QuitarAsync(int usuarioId, int ingredienteId);

    Task<ResultadoOperacion> VaciarAsync(int usuarioId);

    // "Podés cocinar" (100 %) y "Casi listas" (50–99 %), respetando las alergias y restricciones del usuario.
    Task<ResultadoDespensa> SugerenciasAsync(int usuarioId);

    // Qué tiene y qué le falta para una receta puntual (la favorita que elige en la lista). null si no existe.
    Task<AnalisisReceta?> AnalizarRecetaAsync(int usuarioId, int recetaId);
}

// Resultado de una acción para mostrarle al usuario: si salió bien y el mensaje (aviso verde o amarillo).
public record ResultadoOperacion(bool Ok, string Mensaje);

// Lo que falta comprar de un ingrediente. Cantidad null = la receta dice "a gusto".
// EnCasa: lo que ya hay, cuando hay algo pero no alcanza.
public record Faltante(Ingrediente Ingrediente, decimal? Cantidad, string? Unidad, decimal? EnCasa);

// Una receta analizada contra la despensa: qué porcentaje de sus ingredientes alcanza y qué falta comprar.
public record AnalisisReceta(Receta Receta, int Porcentaje, List<Faltante> Faltantes);

public record ResultadoDespensa(List<AnalisisReceta> PodesCocinar, List<AnalisisReceta> CasiListas);
