// Contrato del servicio de catálogos (admin, D-22): categorías, gustos, restricciones y alérgenos, y el panel con números.
using CenaApp.Web.Models;
using CenaApp.Web.ViewModels;

namespace CenaApp.Web.Services;

public enum Catalogo
{
    Categorias,
    Gustos,
    Restricciones,
    Alergenos
}

public interface ICatalogoService
{
    // Los cuatro catálogos, cada ítem con en cuántos lugares se usa.
    Task<CatalogosViewModel> ListarAsync();

    // Crea (id null) o renombra un ítem. Devuelve el error (nombre vacío, largo o repetido) o null si salió bien.
    // descripcion: solo para restricciones; tipo: solo para gustos.
    Task<string?> GuardarAsync(Catalogo catalogo, int? id, string? nombre, string? descripcion, TipoGusto tipo);

    // null si se borró; si no, el motivo (no existe, o es una categoría con recetas).
    Task<string?> EliminarAsync(Catalogo catalogo, int id);

    // Números para el panel de administración.
    Task<PanelViewModel> PanelAsync();
}
