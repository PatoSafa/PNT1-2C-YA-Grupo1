// Valores fijos del dominio. En la base se guardan como texto (ver AppDbContext.ConfigureConventions).
using System.ComponentModel.DataAnnotations;

namespace CenaApp.Web.Models;

// D-22: Usuario usa la app; Admin además administra recetas, ingredientes y catálogos;
// SuperAdmin además administra los usuarios.
public enum Rol
{
    Usuario,
    Admin,
    SuperAdmin
}

public enum Nivel
{
    Principiante,
    Intermedio,
    Avanzado
}

// El orden importa: de más fácil a más difícil (lo usa RecetaService.OrdenPorNivel).
public enum Dificultad
{
    // Display: cómo se muestra en los <select> (el valor guardado sigue siendo "Facil").
    [Display(Name = "Fácil")]
    Facil,
    Media,
    [Display(Name = "Difícil")]
    Dificil
}

public enum TipoGusto
{
    Cocina,
    Estilo
}
