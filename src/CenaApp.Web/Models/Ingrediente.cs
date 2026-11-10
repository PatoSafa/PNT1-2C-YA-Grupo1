// Ingrediente del catálogo: de acá salen los alérgenos y las restricciones de cada receta.
using System.ComponentModel.DataAnnotations;

namespace CenaApp.Web.Models;

public class Ingrediente
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Nombre { get; set; } = "";

    // Ej: "Verdura", "Lácteo", "Carne".
    [Required, MaxLength(40)]
    public string Grupo { get; set; } = "";

    // Otros nombres separados por coma, para buscarlo en la despensa (ej: "aguacate" para palta).
    [MaxLength(200)]
    public string? Sinonimos { get; set; }

    // false para básicos como sal o agua: no cuentan en el porcentaje de la despensa (VN-10).
    public bool CuentaEnDespensa { get; set; } = true;

    // Imagen guardada en la base (D-21). ImagenTipo: "image/png", "image/jpeg" o "image/webp".
    // Se muestra con /Ingredientes/Imagen/{id}; si no tiene, la vista usa el ícono de su grupo.
    public byte[]? Imagen { get; set; }

    [MaxLength(20)]
    public string? ImagenTipo { get; set; }

    public List<Alergeno> Alergenos { get; set; } = [];

    // Restricciones con las que este ingrediente NO es compatible (ej: pollo → Vegetariana).
    public List<Restriccion> RestriccionesIncompatibles { get; set; } = [];
}
