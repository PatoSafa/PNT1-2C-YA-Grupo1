// Un ingrediente que el usuario tiene en casa, con la cantidad opcional (D-19).
// Tiene clase propia (y no es una N:M implícita) porque guarda datos de la relación: cantidad y unidad.
using System.ComponentModel.DataAnnotations;

namespace CenaApp.Web.Models;

public class DespensaItem
{
    public int UsuarioId { get; set; }

    public int IngredienteId { get; set; }
    public Ingrediente Ingrediente { get; set; } = null!;

    // null = no la indicó: se toma como que alcanza para cualquier receta.
    public decimal? Cantidad { get; set; }

    // La misma unidad que usan las recetas para ese ingrediente ("g", "ml", "ud").
    [MaxLength(20)]
    public string? Unidad { get; set; }
}
