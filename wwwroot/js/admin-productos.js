// Comportamiento de las pantallas de productos del panel de administración.
// Cada bloque solo actúa si encuentra sus elementos en la página.
(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // Mensaje pendiente de una operación AJAX que recargó la página
    const pendiente = sessionStorage.getItem('alertaExito');
    if (pendiente) {
        sessionStorage.removeItem('alertaExito');
        Alertas.exito(pendiente);
    }

    // ----- Listado: dar de baja / reactivar con confirmación -----
    document.querySelectorAll('.js-estado').forEach(boton => {
        boton.addEventListener('click', async () => {
            const estaActivo = boton.dataset.activo === 'true';

            const confirmado = await Alertas.confirmar({
                titulo: estaActivo ? '¿Dar de baja el producto?' : '¿Reactivar el producto?',
                texto: estaActivo
                    ? `"${boton.dataset.nombre}" dejará de verse en la tienda.`
                    : `"${boton.dataset.nombre}" volverá a verse en la tienda.`,
                confirmar: estaActivo ? 'Dar de baja' : 'Reactivar'
            });
            if (!confirmado) return;

            try {
                const respuesta = await fetch(boton.dataset.url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: new URLSearchParams({ __RequestVerificationToken: token() })
                });
                const datos = await respuesta.json();

                if (!respuesta.ok || !datos.ok) {
                    Alertas.error(datos.mensaje ?? 'No se pudo completar la operación.');
                    return;
                }

                sessionStorage.setItem('alertaExito', datos.mensaje);
                location.reload();
            } catch {
                Alertas.error('No se pudo completar la operación. Intentá de nuevo.');
            }
        });
    });

    // ----- Formulario: subcategorías según la categoría elegida -----
    const categoria = document.getElementById('IdCategoria');
    const subcategoria = document.getElementById('IdSubcategoria');

    if (categoria && subcategoria) {
        const agregarOpcion = (valor, texto) => {
            const opcion = document.createElement('option');
            opcion.value = valor;
            opcion.textContent = texto;
            subcategoria.appendChild(opcion);
        };

        categoria.addEventListener('change', async () => {
            subcategoria.replaceChildren();
            agregarOpcion('', 'Elegí una subcategoría');
            if (!categoria.value) return;

            try {
                const url = `${categoria.dataset.urlSubcategorias}?idCategoria=${encodeURIComponent(categoria.value)}`;
                const respuesta = await fetch(url);
                const lista = await respuesta.json();
                lista.forEach(s => agregarOpcion(s.id, s.nombre));
            } catch {
                Alertas.error('No se pudieron cargar las subcategorías.');
            }
        });
    }

    // ----- Formulario: vista previa de la imagen -----
    const archivo = document.getElementById('Imagen');
    const vistaPrevia = document.getElementById('vistaPrevia');

    if (archivo && vistaPrevia) {
        archivo.addEventListener('change', () => {
            const imagen = archivo.files[0];
            if (!imagen) return;

            if (imagen.size > 2 * 1024 * 1024) {
                Alertas.error('La imagen no puede superar los 2 MB.');
                archivo.value = '';
                return;
            }
            vistaPrevia.src = URL.createObjectURL(imagen);
        });
    }
})();
