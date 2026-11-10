// Despensa con AJAX (Parte 15 del enunciado): agregar, cambiar cantidad, quitar, vaciar y elegir una favorita
// sin recargar la página.
// Recorrido: formulario → fetch → DespensaController → DespensaService → PostgreSQL
//            → el servidor responde el HTML de la vista parcial _Contenido → se reemplaza en la página.

const contenido = document.getElementById("contenido-despensa");
const formAgregar = document.getElementById("form-agregar");
const inputTexto = document.getElementById("texto");
const selectUnidad = document.getElementById("unidad");

// Favorita elegida en la lista desplegable (vive dentro del bloque que se reemplaza).
const favoritaElegida = () => contenido.querySelector("#favorita")?.value ?? "";

// Pone el HTML que respondió el servidor y muestra su aviso, si trae uno.
async function mostrar(respuesta) {
    if (!respuesta.ok) {
        throw new Error(`El servidor respondió ${respuesta.status}`);
    }
    contenido.innerHTML = await respuesta.text();
    const aviso = contenido.querySelector(".aviso-temporal");
    if (aviso) {
        avisar(aviso.textContent.trim(), aviso.classList.contains("alert-success"));
        aviso.remove();
    }
}

// Manda un formulario con fetch. Devuelve true si salió bien.
async function enviar(formulario) {
    const datos = new FormData(formulario);
    // El token antiforgery está dentro del bloque reemplazable (_Contenido); se toma el más reciente.
    datos.set("__RequestVerificationToken", contenido.querySelector('input[name="__RequestVerificationToken"]').value);
    datos.set("favorita", favoritaElegida());

    try {
        await mostrar(await fetch(formulario.action, {
            method: "POST",
            body: datos,
            headers: { "X-Requested-With": "fetch" }, // así el servidor sabe que tiene que responder solo el bloque
        }));
        return true;
    } catch (error) {
        console.error(error);
        avisar("No se pudo actualizar la despensa. Probá de nuevo.", false);
        return false;
    }
}

// Al elegir un ingrediente, la lista de unidades pasa a ser la que usan las recetas para ese ingrediente.
inputTexto.addEventListener("input", () => {
    const opcion = [...document.querySelectorAll("#catalogo-ingredientes option")]
        .find(o => o.value.toLowerCase() === inputTexto.value.trim().toLowerCase());
    const unidades = (opcion?.dataset.unidades || "g,ml,ud").split(",");
    selectUnidad.replaceChildren(...unidades.map(u => new Option(u, u)));
});

// Formulario principal (ingrediente + cantidad opcional + unidad).
formAgregar.addEventListener("submit", async (evento) => {
    evento.preventDefault();
    if (await enviar(formAgregar)) {
        formAgregar.reset();
        inputTexto.focus();
    }
});

// Formularios dentro del bloque (accesos rápidos, cantidades, "+" de faltantes, quitar, limpiar todo).
// Como el bloque se reemplaza en cada respuesta, se escucha en el contenedor en vez de en cada formulario.
contenido.addEventListener("submit", async (evento) => {
    const formulario = evento.target.closest(".form-despensa");
    if (!formulario) {
        return;
    }
    evento.preventDefault();
    if (formulario.dataset.confirmar && !(await confirmar(formulario.dataset.confirmar))) {
        return;
    }
    await enviar(formulario);
});

// Lista desplegable de favoritas: pide el bloque de nuevo con la receta elegida (GET).
contenido.addEventListener("change", async (evento) => {
    if (evento.target.id !== "favorita") {
        return;
    }
    try {
        await mostrar(await fetch(`/Despensa/Contenido?favorita=${favoritaElegida()}`, { headers: { "X-Requested-With": "fetch" } }));
    } catch (error) {
        console.error(error);
        avisar("No se pudo cargar la receta. Probá de nuevo.", false);
    }
});
