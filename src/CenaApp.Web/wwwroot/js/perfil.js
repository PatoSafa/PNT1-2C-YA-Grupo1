// Formulario de perfil: muestra la nota de lo que deja afuera cada restricción o alergia al tildarla,
// y una vista previa de la foto elegida antes de guardar.

document.querySelectorAll("input[data-nota]").forEach((casilla) => {
    casilla.addEventListener("change", () => {
        document.getElementById(casilla.dataset.nota).hidden = !casilla.checked;
    });
});

const archivoAvatar = document.getElementById("Avatar");
archivoAvatar?.addEventListener("change", () => {
    const archivo = archivoAvatar.files[0];
    if (!archivo) return;
    const vista = document.getElementById("vista-avatar");
    const imagen = document.createElement("img");
    imagen.alt = "Vista previa de tu foto";
    imagen.src = URL.createObjectURL(archivo); // dirección temporal del archivo, solo en este navegador
    vista.replaceChildren(imagen);
});
