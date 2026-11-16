// Configura la aplicación: servicios (inyección de dependencias), base de datos y rutas MVC.
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using CenaApp.Web.Data;
using CenaApp.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.WebEncoders;
using Microsoft.EntityFrameworkCore;

// Números con punto decimal en formularios y JSON, igual en cualquier máquina: un <input type="number">
// manda "0.5", y con la configuración regional en español se leería como 5. Los textos que se muestran
// con coma ("0,5 ud") se formatean explícitamente con es-AR.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Render indica en la variable PORT en qué puerto escuchar. Es lo mismo que server.port: ${PORT:8080} en Spring (Clase 6).
// En local no existe, y se usa el de launchSettings.json (5224).
var puerto = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(puerto))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");
}

// En Render la app está detrás de un proxy que atiende el HTTPS. Estos encabezados le avisan a la app que
// la petición original era https (si no, creería que es http y la redirección a HTTPS no terminaría nunca).
builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opciones.KnownIPNetworks.Clear();
    opciones.KnownProxies.Clear();
});

// Los enum viajan en el JSON como texto ("Facil") en vez de número (0).
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Las vistas escriben los acentos y la ñ tal cual en el HTML (por defecto los codifica como &#xF1;).
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Services: una instancia por petición HTTP (igual que el AppDbContext que usan).
builder.Services.AddScoped<IRecetaService, RecetaService>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IDespensaService, DespensaService>();
builder.Services.AddScoped<IIngredienteService, IngredienteService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
// Comercios: consulta OpenStreetMap (Overpass) por HTTP (D-20). AddHttpClient le da al servicio un HttpClient
// ya configurado; la caché en memoria guarda las respuestas para no repetir consultas.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IComercioService, ComercioService>(cliente =>
{
    cliente.Timeout = TimeSpan.FromSeconds(12);
    // Overpass pide identificar la app que consulta.
    cliente.DefaultRequestHeaders.UserAgent.ParseAdd("CenaAPP/1.0 (TP ORT PNT1)");
});

// Límite de pedidos por dirección IP (D-28), con lo que ya trae ASP.NET Core. Frena a quien prueba contraseñas
// una tras otra o le pega a la API sin parar: pasado el límite, responde 429 hasta que termine el minuto.
// Cada política se aplica con [EnableRateLimiting("nombre")] arriba de la acción o del controller.
Func<HttpContext, string> ipDe = http => http.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.OnRejected = async (contexto, cancelacion) =>
    {
        contexto.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await contexto.HttpContext.Response.WriteAsync("Demasiados intentos. Esperá un minuto y probá de nuevo.", cancelacion);
    };
    // Ingresar y registrarse: 10 intentos por minuto.
    opciones.AddPolicy("cuenta", http => RateLimitPartition.GetFixedWindowLimiter(ipDe(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    // API: 60 pedidos por minuto.
    opciones.AddPolicy("api", http => RateLimitPartition.GetFixedWindowLimiter(ipDe(http),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
});

// Fuera de desarrollo, las cookies (sesión y antiforgery) viajan solo por HTTPS. En local se usa http://localhost.
var cookieSegura = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
builder.Services.AddAntiforgery(opciones => opciones.Cookie.SecurePolicy = cookieSegura);

// Login por cookie (sin ASP.NET Core Identity completo: son pocas clases propias, fáciles de explicar). Si alguien sin sesión entra a una
// página con [Authorize], lo manda a /Cuenta/Ingresar; si no tiene el rol, a /Cuenta/AccesoDenegado.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Ingresar";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        // La sesión vence tras 60 minutos sin uso; cada visita la renueva (RNF15).
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        opciones.SlidingExpiration = true;
        // La cookie no se puede leer desde JavaScript, no viaja en pedidos que arma otro sitio y, fuera de
        // desarrollo, solo va por HTTPS (D-28).
        opciones.Cookie.Name = "CenaApp.Sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.Cookie.SecurePolicy = cookieSegura;

        // En cada pedido se revisa que el usuario siga existiendo, activo y con el mismo rol (D-22). Así, si el
        // SuperAdmin lo desactiva o le cambia el rol, la sesión abierta se cierra y no sigue viendo lo de antes.
        opciones.Events.OnValidatePrincipal = async contexto =>
        {
            var db = contexto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            // Si la cookie no trae un id válido, se rechaza igual que la de un usuario que ya no existe.
            var idValido = int.TryParse(contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
            var usuario = idValido
                ? await db.Usuarios.Where(u => u.Id == id).Select(u => new { u.Activo, u.Rol }).FirstOrDefaultAsync()
                : null;
            if (usuario is null || !usuario.Activo || !contexto.Principal!.IsInRole(usuario.Rol.ToString()))
            {
                contexto.RejectPrincipal();
                await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// La cadena de conexión sale de appsettings.Development.json en local
// y de la variable de entorno ConnectionStrings__CenaApp en producción.
var cadenaConexion = builder.Configuration.GetConnectionString("CenaApp");
if (string.IsNullOrEmpty(cadenaConexion))
{
    throw new InvalidOperationException("Falta la cadena de conexión 'CenaApp'.");
}
// Render entrega la cadena como una URL (postgresql://usuario:clave@servidor/base). Npgsql espera el formato
// "Host=...;Database=...", así que se convierte.
if (cadenaConexion.StartsWith("postgres://") || cadenaConexion.StartsWith("postgresql://"))
{
    var url = new Uri(cadenaConexion);
    var usuarioYClave = url.UserInfo.Split(':', 2);
    cadenaConexion = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = url.Host,
        Port = url.Port > 0 ? url.Port : 5432,
        Database = url.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(usuarioYClave[0]),
        Password = usuarioYClave.Length > 1 ? Uri.UnescapeDataString(usuarioYClave[1]) : null,
    }.ConnectionString;
}
builder.Services.AddDbContext<AppDbContext>(opciones => opciones.UseNpgsql(cadenaConexion));

var app = builder.Build();

// Al arrancar: aplica las migraciones pendientes y carga los datos iniciales si la base está vacía.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DatosIniciales.Cargar(db, app.Configuration);
}

app.UseForwardedHeaders();

// Encabezados de seguridad en todas las respuestas (D-28). Le indican al navegador qué puede hacer la página:
// - Content-Security-Policy: de dónde puede cargar scripts, estilos, fuentes e imágenes. Solo de la propia app y
//   de los tres orígenes que usa (Bootstrap Icons en jsDelivr, Leaflet en unpkg y el mapa de OpenStreetMap).
//   Sin scripts en línea: si alguien lograra meter un <script> en una página, el navegador no lo ejecuta.
//   Al agregar un <script src> o un <link> de otro sitio, hay que sumarlo acá o el navegador lo bloquea.
// - X-Frame-Options / frame-ancestors: la app no se puede mostrar dentro de un <iframe> de otro sitio.
// - X-Content-Type-Options: el navegador respeta el tipo de archivo que declara el servidor.
// En desarrollo se permite además el WebSocket que usa "dotnet watch" para recargar la página.
var politicaDeContenido = string.Join("; ",
    "default-src 'self'",
    "script-src 'self' https://unpkg.com",
    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com",
    "font-src 'self' https://cdn.jsdelivr.net",
    "img-src 'self' data: blob: https://tile.openstreetmap.org https://unpkg.com",
    "connect-src 'self'" + (app.Environment.IsDevelopment() ? " ws: wss:" : ""),
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'none'");
app.Use(async (contexto, siguiente) =>
{
    var encabezados = contexto.Response.Headers;
    encabezados["Content-Security-Policy"] = politicaDeContenido;
    encabezados["X-Content-Type-Options"] = "nosniff";
    encabezados["X-Frame-Options"] = "DENY";
    encabezados["Referrer-Policy"] = "strict-origin-when-cross-origin";
    encabezados["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=()";
    await siguiente();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Primero se lee la cookie (quién es), después se controlan los permisos (qué puede hacer).
app.UseAuthentication();
app.UseAuthorization();

// Aplica los límites de pedidos definidos arriba (va después de UseRouting: necesita saber qué acción se pidió).
app.UseRateLimiter();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
