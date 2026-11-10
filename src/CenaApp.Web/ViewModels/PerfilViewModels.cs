// Datos de las pantallas de perfil: el formulario (onboarding y edición) y la vista de solo lectura.
using System.ComponentModel.DataAnnotations;
using CenaApp.Web.Models;

namespace CenaApp.Web.ViewModels;

// Formulario de perfil. Se usa igual para el onboarding (cuenta nueva) y para editar.
public class PerfilFormularioViewModel
{
    // VAL-06 / VAL-07
    [Required(ErrorMessage = "Contanos cómo te llamás.")]
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    [Display(Name = "¿Cómo te llamás?")]
    public string Nombre { get; set; } = "";

    // VAL-08: un nivel, Intermedio por defecto.
    public Nivel Nivel { get; set; } = Nivel.Intermedio;

    // Lo que el usuario marcó (VAL-25: todo opcional).
    public List<int> RestriccionesIds { get; set; } = [];
    public List<int> AlergenosIds { get; set; } = [];
    public List<int> GustosIds { get; set; } = [];

    // true si es la primera vez (cambia títulos y oculta la barra inferior).
    public bool EsOnboarding { get; set; }

    // Foto de perfil nueva (opcional) y si quiere sacar la que tiene (D-25).
    [Display(Name = "Foto de perfil")]
    public IFormFile? Avatar { get; set; }
    public bool QuitarAvatar { get; set; }
    // La foto actual como data: URI, para mostrarla (no viene del POST).
    public string? AvatarActual { get; set; }

    // Qué deja afuera cada alergia: id del alérgeno → "Almendras, Nueces..." (no viene del POST).
    public Dictionary<int, string> IngredientesPorAlergeno { get; set; } = [];

    // Opciones para dibujar el formulario (no vienen del POST: las vuelve a cargar el controller).
    public List<Restriccion> Restricciones { get; set; } = [];
    public List<Alergeno> Alergenos { get; set; } = [];
    public List<Gusto> Gustos { get; set; } = [];
}

// Perfil en modo lectura.
public class PerfilViewModel
{
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public Nivel Nivel { get; set; }
    public string? AvatarUrl { get; set; }
    // (nombre, qué no se va a incluir)
    public List<(string Nombre, string? Nota)> Restricciones { get; set; } = [];
    public List<(string Nombre, string? Nota)> Alergenos { get; set; } = [];
    public List<string> Gustos { get; set; } = [];
}

// Qué significa cada nivel de cocina y qué le cambia al usuario (D-23). Lo usan el formulario y el perfil.
public static class NivelesDeCocina
{
    public static readonly (Nivel Valor, string Descripcion, string QueCambia)[] Todos =
    [
        (Nivel.Principiante, "Recién empiezo",
            "No te mostramos recetas difíciles y primero ves las fáciles."),
        (Nivel.Intermedio, "Tengo experiencia básica",
            "Ves todas las recetas, ordenadas solo por tus gustos."),
        (Nivel.Avanzado, "Soy experimentado",
            "Ves todas las recetas y primero las más desafiantes (difíciles)."),
    ];

    public static string QueCambia(Nivel nivel) => Todos.First(n => n.Valor == nivel).QueCambia;
}
