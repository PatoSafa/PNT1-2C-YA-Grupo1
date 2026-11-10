// Vista previa de una imagen elegida, antes de guardar (avatar del perfil e imagen de ingrediente).
// Uso: <input type="file" data-vista-previa="id-del-elemento">. Si el elemento es un <img> le cambia la imagen;
// si es otro (ej: el círculo del avatar), pone una <img> adentro.

document.querySelectorAll("input[type=file][data-vista-previa]").forEach((entrada) => {
    entrada.addEventListener("change", () => {
        const archivo = entrada.files[0];
        if (!archivo) return;
        const url = URL.createObjectURL(archivo); // dirección temporal del archivo, solo en este navegador
        const destino = document.getElementById(entrada.dataset.vistaPrevia);
        if (destino.tagName === "IMG") {
            destino.src = url;
        } else {
            const imagen = document.createElement("img");
            imagen.alt = "Vista previa";
            imagen.src = url;
            destino.replaceChildren(imagen);
        }
    });
});
