// Datos de las pantallas de administración (ingredientes y recetas).
using System.ComponentModel.DataAnnotations;
using CenaApp.Web.Models;

namespace CenaApp.Web.ViewModels;

// ---------- Ingredientes ----------

public class IngredienteFilaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Grupo { get; set; } = "";
    public string? Sinonimos { get; set; }
    public List<string> Alergenos { get; set; } = [];
    public int RecetasQueLoUsan { get; set; }
}

public class IngredienteFormularioViewModel
{
    // null = alta; con valor = edición.
    public int? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, ErrorMessage = "El nombre puede tener hasta 60 caracteres.")]
    public string Nombre { get; set; } = "";

    // VAL-23
    [Required(ErrorMessage = "El grupo es obligatorio.")]
    [StringLength(40, ErrorMessage = "El grupo puede tener hasta 40 caracteres.")]
    public string Grupo { get; set; } = "";

    // VAL-24: separados por coma (cada uno de hasta 30 caracteres, se controla en el servicio).
    [StringLength(200, ErrorMessage = "Los sinónimos pueden tener hasta 200 caracteres en total.")]
    [Display(Name = "Sinónimos")]
    public string? Sinonimos { get; set; }

    [Display(Name = "Cuenta en el porcentaje de la despensa")]
    public bool CuentaEnDespensa { get; set; } = true;

    public List<int> AlergenosIds { get; set; } = [];
    public List<int> RestriccionesIncompatiblesIds { get; set; } = [];

    // Imagen nueva (opcional) y si se quiere sacar la que tiene (D-21).
    [Display(Name = "Imagen")]
    public IFormFile? Imagen { get; set; }
    [Display(Name = "Quitar la imagen")]
    public bool QuitarImagen { get; set; }

    // Opciones para dibujar el formulario (las carga el controller).
    public List<Alergeno> Alergenos { get; set; } = [];
    public List<Restriccion> Restricciones { get; set; } = [];
    public List<string> Grupos { get; set; } = [];
}

// ---------- Usuarios (D-22) ----------

public class UsuarioFilaViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public Rol Rol { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
    public int Favoritos { get; set; }
    public int EnDespensa { get; set; }
}

public class UsuarioFormularioViewModel
{
    // null = alta; con valor = edición.
    public int? Id { get; set; }

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(254, ErrorMessage = "El email puede tener hasta 254 caracteres.")]
    public string Email { get; set; } = "";

    // Opcional: si queda vacío, el usuario lo completa en el onboarding.
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    public string? Nombre { get; set; }

    public Rol Rol { get; set; } = Rol.Usuario;

    [Display(Name = "Cuenta activa (puede iniciar sesión)")]
    public bool Activo { get; set; } = true;

    // Obligatoria al crear; al editar, vacía = no se cambia.
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña tiene que tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Clave { get; set; }

    // true si el SuperAdmin se está editando a sí mismo (no puede cambiarse el rol ni desactivarse).
    public bool EsElActual { get; set; }
}

// Qué puede hacer cada rol (D-22). Se muestra en la pantalla de usuarios.
public static class PermisosPorRol
{
    public static readonly (Rol Rol, string Puede)[] Todos =
    [
        (Rol.Usuario, "Recetas, favoritos, despensa, tiendas y su perfil."),
        (Rol.Admin, "Lo anterior, más administrar recetas, ingredientes y catálogos."),
        (Rol.SuperAdmin, "Lo anterior, más administrar usuarios: crearlos, cambiarles el rol y activarlos o desactivarlos."),
    ];
}

// ---------- Recetas ----------

public class RecetaFilaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Emoji { get; set; } = "";
    public string Categoria { get; set; } = "";
    public int TiempoMin { get; set; }
}

// Formulario de alta y edición. Los datos son el mismo DTO que recibe la API (POST /api/recetas),
// así las validaciones y el alta están en un solo lugar (RecetaService).
public class RecetaFormularioViewModel
{
    // Unidades permitidas para las cantidades (VAL-18).
    public static readonly string[] Unidades = ["g", "kg", "ml", "l", "ud", "cda", "cdita", "taza", "pizca"];

    // null = alta; con valor = edición.
    public int? Id { get; set; }

    public RecetaCrearDto Datos { get; set; } = new();

    // Los pasos se escriben en un textarea, uno por línea; el controller los separa en Datos.Pasos.
    [Display(Name = "Pasos (uno por línea)")]
    public string PasosTexto { get; set; } = "";

    // Solo en edición: alérgenos que resultan de los ingredientes guardados (vista previa, RF8).
    public List<string> AlergenosCalculados { get; set; } = [];

    // Opciones para dibujar el formulario (las carga el controller).
    public List<Categoria> Categorias { get; set; } = [];
    public List<Gusto> Gustos { get; set; } = [];
    public List<Ingrediente> Ingredientes { get; set; } = [];
}


// ---------- Catálogos y panel (D-22) ----------

// Un ítem de un catálogo. Uso: dónde se usa ("12 recetas · 3 usuarios"), para decidir antes de borrar.
public record ItemCatalogoViewModel(int Id, string Nombre, string? Descripcion, TipoGusto? Tipo, string Uso);

public class CatalogosViewModel
{
    public List<ItemCatalogoViewModel> Categorias { get; set; } = [];
    public List<ItemCatalogoViewModel> Gustos { get; set; } = [];
    public List<ItemCatalogoViewModel> Restricciones { get; set; } = [];
    public List<ItemCatalogoViewModel> Alergenos { get; set; } = [];
}

public record ValorPanel(string Nombre, int Cantidad);

public class PanelViewModel
{
    public int Usuarios { get; set; }
    public int UsuariosActivos { get; set; }
    public int Admins { get; set; }
    public int Recetas { get; set; }
    public int Ingredientes { get; set; }
    public int IngredientesSinImagen { get; set; }
    public int IngredientesSinUso { get; set; }
    public List<ValorPanel> MasFavoritas { get; set; } = [];
    public List<ValorPanel> MasEnDespensas { get; set; } = [];
}
