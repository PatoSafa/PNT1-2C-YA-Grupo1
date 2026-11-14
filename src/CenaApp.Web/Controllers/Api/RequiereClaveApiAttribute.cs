// Filtro que protege las acciones de la API que modifican datos (D-27): exige el encabezado X-Api-Key.
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CenaApp.Web.Controllers.Api;

// Se pone arriba de una acción: [RequiereClaveApi]. Es un filtro de autorización, así que corre antes de
// validar los datos y de ejecutar la acción: sin la clave correcta responde 401 y no se toca la base.
// La clave sale de la configuración (Api:Clave): en local, de appsettings.Development.json; en producción,
// de la variable de entorno Api__Clave.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiereClaveApiAttribute : Attribute, IAuthorizationFilter
{
    public const string Encabezado = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext contexto)
    {
        var config = contexto.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var claveEsperada = config["Api:Clave"];
        var claveRecibida = contexto.HttpContext.Request.Headers[Encabezado].ToString();

        // Si no hay clave configurada no entra nadie: es más seguro que dejar la API abierta por un olvido.
        if (string.IsNullOrEmpty(claveEsperada) || !SonIguales(claveRecibida, claveEsperada))
        {
            contexto.Result = new UnauthorizedObjectResult(new
            {
                mensaje = $"Falta el encabezado {Encabezado} o la clave no es correcta."
            });
        }
    }

    // Compara en tiempo constante: tarda lo mismo acierte o no, para que no se pueda adivinar la clave
    // letra por letra midiendo cuánto tarda la respuesta.
    private static bool SonIguales(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
