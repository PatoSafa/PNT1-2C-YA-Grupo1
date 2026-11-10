// Imágenes guardadas en la base (avatar del usuario, D-25, e imagen de cada ingrediente, D-21):
// validar el archivo que se sube y armar el data: URI para mostrarla en un <img>.
namespace CenaApp.Web.Services;

public static class Imagenes
{
    public const int TamanioMaximoKb = 300;

    // Tipos aceptados y cómo empieza cada archivo (su "firma"): se mira el contenido, no solo la extensión,
    // para que no se pueda subir otra cosa renombrada como .png.
    private static readonly (string Tipo, byte[] Firma)[] Aceptadas =
    [
        ("image/png", [0x89, 0x50, 0x4E, 0x47]),
        ("image/jpeg", [0xFF, 0xD8, 0xFF]),
        ("image/webp", "RIFF"u8.ToArray()),
    ];

    // Lee un archivo subido. Devuelve los bytes y el tipo, o un mensaje de error (VAL de imagen).
    public static async Task<(byte[]? Datos, string? Tipo, string? Error)> LeerAsync(IFormFile archivo)
    {
        if (archivo.Length == 0)
        {
            return (null, null, "El archivo está vacío.");
        }
        if (archivo.Length > TamanioMaximoKb * 1024)
        {
            return (null, null, $"La imagen puede pesar hasta {TamanioMaximoKb} KB.");
        }

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);
        var datos = memoria.ToArray();

        var tipo = TipoSegunContenido(datos);
        return tipo is null
            ? (null, null, "Tiene que ser una imagen PNG, JPG o WebP.")
            : (datos, tipo, null);
    }

    // "image/png" si los primeros bytes son los de un PNG, etc. null si no es ninguna aceptada.
    public static string? TipoSegunContenido(byte[] datos)
    {
        foreach (var (tipo, firma) in Aceptadas)
        {
            if (datos.Length >= 12 && datos.AsSpan(0, firma.Length).SequenceEqual(firma)
                && (tipo != "image/webp" || datos.AsSpan(8, 4).SequenceEqual("WEBP"u8)))
            {
                return tipo;
            }
        }
        return null;
    }

    // data:image/png;base64,iVBOR... → el navegador la muestra sin pedir otro archivo al servidor.
    public static string? DataUri(byte[]? datos, string? tipo) =>
        datos is null || tipo is null ? null : $"data:{tipo};base64,{Convert.ToBase64String(datos)}";
}
