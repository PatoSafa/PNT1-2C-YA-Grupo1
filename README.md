# CenaAPP · PNT1 - 2C - YA - ORT · Grupo 1

**CenaAPP** es una app web, pensada para el celular, que recomienda recetas según el perfil de cada persona.

## Qué problema resuelve

A quien tiene restricciones alimentarias (vegetariana, vegana, sin gluten, sin lactosa, baja en sodio) o alergias, encontrar una receta le exige revisar los ingredientes uno por uno. CenaAPP lo hace por la persona:

- **Muestra solo las recetas aptas para su perfil.** Los alérgenos y las restricciones de cada receta se calculan a partir de sus ingredientes, no se cargan a mano.
- **Las ordena según sus gustos** (italiana, rápida, saludable…) y su nivel de cocina, y sugiere recetas parecidas a sus favoritas.
- **Despensa:** muestra qué se puede cocinar con lo que ya hay en casa (con cantidades opcionales) y qué falta comprar.
- **Tiendas:** dónde comprar lo que falta, cerca de su ubicación, con comercios reales de OpenStreetMap.

## Integrantes

- Patricio Safa
- Eric Balbachan
- Maria Cecilia Ayala

## Qué se puede hacer

| Pantalla | Ruta | Qué hace |
|---|---|---|
| Registro e inicio de sesión | `/Cuenta/Registrarse`, `/Cuenta/Ingresar` | Cuenta con email y contraseña (hasheada). El mismo login sirve para usuarios y admin. |
| Onboarding y Perfil | `/Perfil`, `/Perfil/Editar` | Nombre, foto, restricciones y alergias (con lo que deja afuera cada una), gustos y nivel de cocina (con lo que cambia cada nivel). |
| Inicio | `/Recetas` | Recetas aptas para el perfil, ordenadas por gustos y nivel, con buscador, filtro por gusto y "Te puede gustar" (parecidas a las favoritas). |
| Detalle | `/Recetas/Detalle/{id}` | Ingredientes con imagen, pasos que se pueden marcar, alérgenos calculados y para qué restricciones es apta. Si la receta no existe, 404. |
| Mis favoritos | `/Recetas/Favoritos` | Recetas guardadas con el corazón (se marca con `fetch`, sin recargar). |
| Mi Despensa | `/Despensa` | Tabla "Tengo en casa" con cantidad opcional, "Podés cocinar" (100 %) y "Casi listas" (≥ 50 %) con lo que falta comprar ("+" para sumarlo), "¿Qué me falta para una favorita?" y "Ver tiendas". Se actualiza con `fetch`. |
| Tiendas | `/Tiendas` | Comercios de OpenStreetMap cerca de la ubicación del usuario (o de ORT Yatay), filtrados por tipo y por los ingredientes a comprar, en un mapa (Leaflet) con ruta a pie. |
| Administración | `/Admin`, `/Admin/Recetas`, `/Admin/Ingredientes`, `/Admin/Catalogos` | Panel con números, ABM de recetas, ingredientes (con imagen) y catálogos. Roles Admin y SuperAdmin. |
| Usuarios | `/Admin/Usuarios` | ABM de usuarios: rol (Usuario, Admin, SuperAdmin) y cuenta activa o desactivada. Solo SuperAdmin. |

Todas las acciones de guardar, eliminar o cancelar piden confirmación, y los resultados se muestran con un aviso.

## Tecnologías

| Para qué | Tecnología |
|---|---|
| Lenguaje y framework | C# / .NET 10, ASP.NET Core MVC con vistas Razor |
| Base de datos | PostgreSQL 17 en Docker, con Entity Framework Core (code-first y migraciones) |
| Estilos | Bootstrap + `wwwroot/css/estilos.css`; íconos con Bootstrap Icons |
| Mapa y comercios | Leaflet + OpenStreetMap; comercios con la Overpass API (gratis, sin clave) |
| Íconos de ingredientes | Microsoft Fluent Emoji (licencia MIT), guardados en la base |
| Tests | xUnit |
| Contenedores | Docker: `compose.yaml` (PostgreSQL) y `Dockerfile` (la app) |
| Versionado | Git y GitHub |

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Visual Studio Code con la extensión C# Dev Kit (opcional, para editar y usar `recetas.http`)
- [Postman](https://www.postman.com/downloads/) (opcional, para probar la API)

## Cómo ejecutarlo

```bash
# 1. Levantar PostgreSQL (la primera vez descarga la imagen)
docker compose up -d

# 2. Restaurar la herramienta de migraciones (solo la primera vez)
dotnet tool restore

# 3. Aplicar las migraciones (opcional: la app también las aplica al arrancar)
dotnet ef database update --project src/CenaApp.Web

# 4. Correr la app
dotnet run --project src/CenaApp.Web
```

Abrir http://localhost:5224.

Al arrancar por primera vez, la app carga los datos iniciales: 32 recetas, 81 ingredientes con imagen y los catálogos. También crea tres usuarios de prueba, que existen **solo en desarrollo**:

| Usuario | Contraseña | Rol |
|---|---|---|
| `superadmin@cenaapp.com` | `super1234` | SuperAdmin (además administra usuarios) |
| `admin@cenaapp.com` | `admin1234` | Administrador |
| `demo@cenaapp.com` | `demo1234` | Usuario (vegetariana, alérgica a frutos secos, con despensa cargada) |

> **Tiendas en Windows 10:** el servidor principal de Overpass pide TLS 1.3 y Windows 10 no lo tiene. La app prueba entonces con un segundo servidor (`overpass.openstreetmap.fr`), que tiene los mismos datos, así que los comercios reales se ven igual. Solo si no responde ninguno se muestran los comercios de ejemplo con un aviso.

- Tests: `dotnet test`
- Apagar la base: `docker compose down`. Los datos se conservan en el volumen `cenaapp-datos`; para borrarlos, `docker compose down -v`.

## Correr la app en un contenedor (Docker)

Además de la base, la app también puede correr en Docker con el [`Dockerfile`](Dockerfile) de la raíz. Sirve para probarla igual que en un servidor:

```bash
docker build -t cenaapp .
docker run --rm -p 10000:10000 -e PORT=10000 -e Api__Clave=clave_local \
  -e "ConnectionStrings__CenaApp=Host=host.docker.internal;Port=5432;Database=cenaapp;Username=cenaapp;Password=cenaapp_dev" \
  cenaapp
# http://localhost:10000
```

La imagen corre en modo Producción, así que todo lo que cambia entre ambientes llega por variables de entorno (nunca desde el repositorio):

| Variable | Qué es |
|---|---|
| `ConnectionStrings__CenaApp` | Cadena de conexión a PostgreSQL |
| `PORT` | Puerto en el que escucha la app. `Program.cs` lo lee al arrancar, igual que `server.port: ${PORT:8080}` en Spring |
| `Api__Clave` | Clave para crear y eliminar por la API (`X-Api-Key`). Si falta, esos dos endpoints responden siempre 401 |
| `DatosIniciales__ClaveSuperAdmin`, `DatosIniciales__ClaveAdmin`, `DatosIniciales__ClaveDemo` | Contraseñas de los tres usuarios iniciales. Si faltan, esos usuarios no se crean |

## Desplegar en Render

El archivo [`render.yaml`](render.yaml) crea todo junto: la app (con el `Dockerfile`) y una base PostgreSQL, las dos en el plan gratis.

1. En [Render](https://dashboard.render.com), **New > Blueprint** y elegir este repositorio.
2. Completar las tres contraseñas que pide (`DatosIniciales__Clave...`): son las de los usuarios `superadmin@cenaapp.com`, `admin@cenaapp.com` y `demo@cenaapp.com`.
3. **Apply**. A los pocos minutos la app queda en `https://cenaapp.onrender.com` (o la dirección que muestre Render).

La cadena de conexión y la clave de la API las completa Render. En el plan gratis la app se duerme tras 15 minutos sin uso (la primera visita tarda cerca de un minuto) y la base se borra a los 30 días.

## API REST

Base: `http://localhost:5224/api/recetas`. Todas las pruebas están listas para ejecutar en [`src/CenaApp.Web/recetas.http`](src/CenaApp.Web/recetas.http) (VS Code), y más abajo se explica cómo hacerlas con Postman.

| Método | Ruta | Qué hace | Respuestas |
|---|---|---|---|
| `GET` | `/api/recetas` | Lista todas las recetas | `200` |
| `GET` | `/api/recetas?nombre=papa` | Lista las que contienen ese texto en el nombre | `200` |
| `GET` | `/api/recetas/{id}` | Una receta con ingredientes, pasos y alérgenos calculados | `200` · `404` si no existe |
| `POST` | `/api/recetas` | Crea una receta (pide clave) | `201` · `400` con los errores de validación · `401` sin la clave |
| `DELETE` | `/api/recetas/{id}` | Elimina una receta (pide clave) | `204` · `404` si no existe · `401` sin la clave |

Los `GET` son públicos. `POST` y `DELETE` piden el encabezado `X-Api-Key` con la clave de la API. En local es `clave_dev` (`appsettings.Development.json`); en producción, la de la variable de entorno `Api__Clave`. Sin la clave, o con una incorrecta, responden `401 Unauthorized`:
```json
{ "mensaje": "Falta el encabezado X-Api-Key o la clave no es correcta." }
```

### Ejemplo: crear una receta

```http
POST /api/recetas
Content-Type: application/json
X-Api-Key: clave_dev
```
```json
{
  "nombre": "Ensalada de prueba",
  "descripcion": "Tomate y pepino",
  "emoji": "🥗",
  "tiempoMin": 10,
  "porciones": 2,
  "calorias": 150,
  "dificultad": "Facil",
  "categoriaId": 2,
  "ingredientes": [
    { "ingredienteId": 10, "cantidad": 100, "unidad": "g" },
    { "ingredienteId": 12, "cantidad": 50, "unidad": "g" }
  ],
  "pasos": ["Lavar y cortar las verduras.", "Mezclar y servir."]
}
```

`gustosIds` es opcional (por ejemplo `"gustosIds": [5, 9]`). Respuesta `201 Created`, con el encabezado `Location: /api/recetas/33`:
```json
{
  "id": 33,
  "nombre": "Ensalada de prueba",
  "categoria": "Ensalada",
  "dificultad": "Facil",
  "alergenos": [],
  "ingredientes": [
    { "nombre": "Tomate", "cantidad": 100, "unidad": "g" },
    { "nombre": "Pepino", "cantidad": 50, "unidad": "g" }
  ],
  "pasos": ["Lavar y cortar las verduras.", "Mezclar y servir."]
}
```

### Ejemplo: ID inexistente

`GET /api/recetas/9999` → `404 Not Found`
```json
{ "mensaje": "No existe una receta con id 9999." }
```

### Ejemplo: datos inválidos

Si el `POST` llega con el nombre vacío y el tiempo en 0, responde `400 Bad Request` con un error por campo:
```json
{
  "status": 400,
  "errors": {
    "Nombre": ["El nombre es obligatorio."],
    "TiempoMin": ["El tiempo tiene que estar entre 1 y 600 minutos."]
  }
}
```

### Probar la API con Postman

Con la app corriendo, cada consulta se arma así en Postman:

| # | Método y URL | Encabezados | Respuesta esperada |
|---|---|---|---|
| 1 | `GET http://localhost:5224/api/recetas` | — | `200` con la lista |
| 2 | `GET http://localhost:5224/api/recetas?nombre=papa` | — | `200` con las que contienen "papa" |
| 3 | `GET http://localhost:5224/api/recetas/1` | — | `200` con ingredientes, pasos y alérgenos |
| 4 | `GET http://localhost:5224/api/recetas/9999` | — | `404` con mensaje |
| 5 | `POST http://localhost:5224/api/recetas` | `X-Api-Key: clave_dev` | `201` con la receta creada |
| 6 | `POST` con el nombre vacío y el tiempo en 0 | `X-Api-Key: clave_dev` | `400` con un error por campo |
| 7 | `POST` con un nombre que ya existe ("Risotto de hongos") | `X-Api-Key: clave_dev` | `400` |
| 8 | `DELETE http://localhost:5224/api/recetas/{id}` (el id que devolvió la 5) | `X-Api-Key: clave_dev` | `204` |
| 9 | `DELETE http://localhost:5224/api/recetas/9999` | `X-Api-Key: clave_dev` | `404` |
| 10 | `POST` o `DELETE` sin el encabezado, o con otra clave | — | `401` |

Para las de `POST`:

- Pestaña *Headers*: agregar `X-Api-Key` con el valor `clave_dev`.
- Pestaña *Body* → *raw* → *JSON*: pegar el JSON del ejemplo "crear una receta" de arriba.

Lo mismo desde la terminal, con `curl`:

```bash
curl http://localhost:5224/api/recetas/1
curl -X DELETE http://localhost:5224/api/recetas/33 -H "X-Api-Key: clave_dev"
```

## Seguridad

Resumen de lo que protege a la app:

- Contraseñas con hash (PBKDF2), sesión por cookie `HttpOnly` y token antiforgery en todos los formularios.
- Roles con `[Authorize]`; la sesión se corta si la cuenta se desactiva o cambia de rol.
- `POST` y `DELETE` de la API piden clave (`X-Api-Key`).
- Límite de intentos por IP: 10 por minuto para ingresar o registrarse y 60 por minuto en la API (`429` al pasarse).
- Encabezados de seguridad, con una Content-Security-Policy que no permite scripts en línea ni de sitios no listados.
- Imágenes subidas: hasta 300 KB y verificadas por contenido, no por extensión.
- Ninguna contraseña real en el repositorio: en producción llegan por variables de entorno. `appsettings.Development.json` tiene solo valores de desarrollo.
- El contenedor corre sin privilegios de administrador.

## Estructura del repositorio

```
CenaApp.sln                 solución (equivale a build.gradle / settings.gradle)
compose.yaml                PostgreSQL para desarrollo
Dockerfile                  imagen de la app
render.yaml                 despliegue en Render (app + base)
docs/funcional/             documentación funcional (un solo documento con todo)
src/CenaApp.Web/
  Controllers/              reciben la petición y eligen la vista (Api/ = controllers JSON)
  Services/                 reglas de negocio (aptitud de recetas, despensa, cuentas...)
  Models/                   entidades (tablas)
  ViewModels/               lo que recibe cada vista y los DTO de la API
  Data/                     AppDbContext, datos iniciales, Imagenes/ (íconos) y Migrations/
  Views/                    vistas Razor, una carpeta por controller
  wwwroot/                  estilos.css y los JS (favoritos, despensa, tiendas...)
  Program.cs                configuración: servicios, base de datos, seguridad y rutas
  recetas.http              pruebas de la API para VS Code
tests/CenaApp.Tests/        tests xUnit: aptitud y nivel, despensa, sugerencias y tiendas (43)
```

Recorrido de una petición: Vista Razor → Controller → Service (reglas) → `AppDbContext` (EF Core) → PostgreSQL.

## Documentación

Toda la documentación está en un solo archivo: [`docs/funcional/CenaAPP_Documentacion_Funcional.docx`](docs/funcional/CenaAPP_Documentacion_Funcional.docx) (Documentación Funcional de CenaAPP, versión 4). Las secciones 1 a 10 son el diseño funcional: alcance, actores, casos de uso, pantallas, validaciones, requisitos y modelo de datos. La sección 11 describe el sistema construido: qué cambió respecto del diseño, tecnología y arquitectura, API, seguridad, decisiones, errores encontrados y cómo ejecutarlo.
