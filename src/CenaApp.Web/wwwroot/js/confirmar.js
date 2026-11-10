// Confirmaciones y avisos de toda la app (se carga en _Layout).
// - confirmar(pregunta): abre el modal #modal-confirmar y devuelve una promesa con true (Aceptar) o false (Cancelar).
// - Formularios con data-confirmar="pregunta" (guardar, eliminar, cerrar sesión) y links con data-confirmar
//   (cancelar una edición) piden confirmación antes de seguir.
// - avisar(mensaje, ok): muestra un aviso (toast) arriba de la pantalla, que se cierra solo.

const modalElemento = document.getElementById("modal-confirmar");
const modal = bootstrap.Modal.getOrCreateInstance(modalElemento);

window.confirmar = (pregunta) => new Promise((resolver) => {
    modalElemento.querySelector(".modal-body p").textContent = pregunta;
    let aceptado = false;
    const botonAceptar = modalElemento.querySelector(".aceptar");
    botonAceptar.onclick = () => { aceptado = true; modal.hide(); };
    // Se resuelve al terminar de cerrarse, sea por Aceptar, Cancelar, la ✕, Escape o un clic afuera.
    modalElemento.addEventListener("hidden.bs.modal", () => resolver(aceptado), { once: true });
    modal.show();
});

window.avisar = (mensaje, ok = true) => {
    const plantilla = document.getElementById("plantilla-aviso");
    const aviso = plantilla.content.firstElementChild.cloneNode(true);
    aviso.classList.add(ok ? "text-bg-success" : "text-bg-warning");
    aviso.querySelector(".toast-body").textContent = mensaje;
    document.getElementById("avisos").append(aviso);
    mostrarAviso(aviso);
};

function mostrarAviso(aviso) {
    const toast = bootstrap.Toast.getOrCreateInstance(aviso, { delay: 4000 });
    aviso.addEventListener("hidden.bs.toast", () => aviso.remove(), { once: true });
    toast.show();
}

// Los avisos que dejó el servidor (TempData) ya vienen en la página: se les activa el cierre automático.
document.querySelectorAll("#avisos .toast").forEach(mostrarAviso);

// Formularios: se frena el envío, se pregunta y, si acepta, se vuelve a enviar marcado como confirmado.
// Los de la despensa (.form-despensa) los maneja despensa.js, que además los manda con fetch.
document.addEventListener("submit", async (evento) => {
    const formulario = evento.target;
    if (!formulario.dataset.confirmar || formulario.dataset.confirmado || formulario.classList.contains("form-despensa")) {
        return;
    }
    evento.preventDefault();
    // Si tiene validación del cliente (jQuery Validation) y hay errores, primero se muestran esos.
    if (window.jQuery && jQuery(formulario).data("validator") && !jQuery(formulario).valid()) {
        return;
    }
    if (await confirmar(formulario.dataset.confirmar)) {
        formulario.dataset.confirmado = "si";
        formulario.requestSubmit(evento.submitter);
    }
});

// Links que descartan cambios ("Cancelar").
document.addEventListener("click", async (evento) => {
    const link = evento.target.closest("a[data-confirmar]");
    if (!link) {
        return;
    }
    evento.preventDefault();
    if (await confirmar(link.dataset.confirmar)) {
        window.location.href = link.href;
    }
});
