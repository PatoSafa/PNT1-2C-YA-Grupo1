// Tiendas (D-11, D-20): pide la ubicación al navegador, trae la lista de comercios con fetch y los dibuja en el mapa
// (Leaflet + OpenStreetMap). Si el usuario no comparte la ubicación, busca desde ORT Yatay.
// Recorrido: ubicación → fetch GET /Tiendas/Buscar → TiendasController → ComercioService → Overpass (OpenStreetMap)
//            → el servidor responde el HTML de la lista (_Lista) → se pone en la página y se marcan los comercios.

const datos = JSON.parse(document.getElementById("datos-tiendas").textContent);
const lista = document.getElementById("lista-comercios");
const estado = document.getElementById("estado-ubicacion");
const contenedorMapa = document.getElementById("mapa");

// Filtros actuales y ubicación (null = no la compartió).
const filtros = { tipo: datos.tipo, ingredientes: [...datos.ingredientes] };
let ubicacion = null;

// ---------- Mapa ----------
let mapa = null;
let capaComercios = null;
let marcadorUsuario = null;
const marcadores = {};

if (typeof L === "undefined") {
    contenedorMapa.hidden = true;
    document.getElementById("mapa-error").hidden = false;
} else {
    mapa = L.map(contenedorMapa).setView([datos.centro.lat, datos.centro.lng], 15);
    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(mapa);
    capaComercios = L.layerGroup().addTo(mapa);
}

// Punto naranja: el usuario (o ORT Yatay si no compartió la ubicación).
function marcarPuntoDePartida() {
    if (!mapa) return;
    const punto = ubicacion ?? datos.centro;
    marcadorUsuario?.remove();
    marcadorUsuario = L.circleMarker([punto.lat, punto.lng], { radius: 8, color: "#F97316", fillOpacity: 1 })
        .addTo(mapa)
        .bindPopup(`<strong>${ubicacion ? "Estás acá" : datos.centro.nombre}</strong>`);
    mapa.setView([punto.lat, punto.lng], 15);
}

// Un marcador verde por comercio, con los datos que dejó el servidor en cada tarjeta.
function marcarComercios() {
    if (!mapa) return;
    capaComercios.clearLayers();
    for (const tarjeta of lista.querySelectorAll(".tarjeta-comercio")) {
        const d = tarjeta.dataset;
        const globito = document.createElement("div");
        globito.innerHTML = "<strong></strong><br><span></span><br><span></span>";
        globito.children[0].textContent = `${d.emoji} ${d.nombre}`;
        globito.children[2].textContent = d.direccion;
        globito.children[4].textContent = `⏰ ${d.horario}`;
        marcadores[d.comercio] = L.circleMarker([Number(d.lat), Number(d.lng)], { radius: 10, color: "#16A34A", fillColor: "#22C55E", fillOpacity: .9 })
            .addTo(capaComercios)
            .bindPopup(globito);
    }
}

// ---------- Lista ----------
async function buscar() {
    const parametros = new URLSearchParams({ tipo: filtros.tipo, ingredientes: filtros.ingredientes.join(",") });
    if (ubicacion) {
        parametros.set("lat", ubicacion.lat);
        parametros.set("lng", ubicacion.lng);
    }
    lista.setAttribute("aria-busy", "true");
    try {
        const respuesta = await fetch(`/Tiendas/Buscar?${parametros}`);
        if (!respuesta.ok) {
            throw new Error(`El servidor respondió ${respuesta.status}`);
        }
        lista.innerHTML = await respuesta.text();
        marcarComercios();
    } catch (error) {
        console.error(error);
        lista.innerHTML = '<div class="alert alert-warning">No se pudieron cargar los comercios. Probá de nuevo.</div>';
    } finally {
        lista.removeAttribute("aria-busy");
    }
}

// Deja los filtros en la URL, así recargar o compartir el link muestra lo mismo.
function actualizarUrl() {
    const url = new URL(window.location);
    url.searchParams.set("tipo", filtros.tipo);
    url.searchParams.set("ingredientes", filtros.ingredientes.join(","));
    history.replaceState(null, "", url);
}

// ---------- Ubicación ----------
// Geolocation API del navegador. Resuelve con { lat, lng } o con null si no la da (rechazo, error o 8 s sin respuesta).
function pedirUbicacion() {
    return new Promise((resolver) => {
        if (!navigator.geolocation) {
            resolver(null);
            return;
        }
        navigator.geolocation.getCurrentPosition(
            (posicion) => resolver({ lat: posicion.coords.latitude, lng: posicion.coords.longitude }),
            () => resolver(null),
            { timeout: 8000, maximumAge: 300000 });
    });
}

// ---------- Eventos ----------
document.getElementById("filtro-tipos").addEventListener("click", (evento) => {
    const chip = evento.target.closest("[data-tipo]");
    if (!chip) return;
    filtros.tipo = chip.dataset.tipo;
    document.querySelectorAll("#filtro-tipos .chip").forEach(c => c.classList.toggle("activo", c === chip));
    actualizarUrl();
    buscar();
});

document.getElementById("filtro-ingredientes")?.addEventListener("click", (evento) => {
    const boton = evento.target.closest(".quitar-ingrediente");
    if (!boton) return;
    const etiqueta = boton.closest("[data-ingrediente]");
    filtros.ingredientes = filtros.ingredientes.filter(id => String(id) !== etiqueta.dataset.ingrediente);
    etiqueta.remove();
    actualizarUrl();
    buscar();
});

// "Ver en el mapa": centra el mapa en el comercio, abre su globito y marca la tarjeta.
lista.addEventListener("click", (evento) => {
    const boton = evento.target.closest(".ver-en-mapa");
    if (!boton || !mapa) return;
    const id = boton.dataset.comercio;
    const marcador = marcadores[id];
    mapa.flyTo(marcador.getLatLng(), 17);
    marcador.openPopup();
    lista.querySelectorAll(".tarjeta-comercio").forEach(t => t.classList.toggle("elegido", t.dataset.comercio === id));
    contenedorMapa.scrollIntoView({ behavior: "smooth", block: "center" });
});

// ---------- Inicio ----------
(async () => {
    ubicacion = await pedirUbicacion();
    estado.innerHTML = ubicacion
        ? '<i class="bi bi-geo-alt-fill text-success"></i> Usando tu ubicación actual.'
        : `<i class="bi bi-geo-alt"></i> No compartiste tu ubicación: buscamos cerca de ${datos.centro.nombre}.`;
    marcarPuntoDePartida();
    await buscar();
})();
