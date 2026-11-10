// Formulario de perfil: muestra la nota de lo que deja afuera cada restricción o alergia al tildarla.

document.querySelectorAll("input[data-nota]").forEach((casilla) => {
    casilla.addEventListener("change", () => {
        document.getElementById(casilla.dataset.nota).hidden = !casilla.checked;
    });
});
