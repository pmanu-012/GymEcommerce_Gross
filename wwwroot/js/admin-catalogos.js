// Modales y AJAX de los catálogos del panel (categorías, subcategorías, disciplinas y grupos musculares).
(() => {
    const elementoModal = document.getElementById('modalCatalogo');
    if (!elementoModal) return;

    const contenido = document.getElementById('modalCatalogoContenido');
    const modal = bootstrap.Modal.getOrCreateInstance(elementoModal);
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // Pone el formulario dentro del modal y activa su validación de cliente (jQuery Unobtrusive)
    const mostrarFormulario = (html) => {
        contenido.innerHTML = html;
        const formulario = document.getElementById('formCatalogo');
        $.validator.unobtrusive.parse(formulario);
        formulario.addEventListener('submit', enviar);
    };

    const abrirFormulario = async (url) => {
        try {
            const respuesta = await fetch(url);
            if (!respuesta.ok) {
                Alertas.error('No se pudo abrir el formulario.');
                return;
            }
            mostrarFormulario(await respuesta.text());
            modal.show();
        } catch {
            Alertas.error('No se pudo abrir el formulario.');
        }
    };

    const enviar = async (evento) => {
        evento.preventDefault();
        const formulario = evento.target;

        // Validación de cliente: si hay errores, se muestran sin ir al servidor
        if (!$(formulario).valid()) return;

        const boton = formulario.querySelector('[type="submit"]');
        boton.disabled = true;

        try {
            const respuesta = await fetch(formulario.action, { method: 'POST', body: new FormData(formulario) });

            // 422: el servidor encontró errores y devolvió el formulario con sus mensajes
            if (respuesta.status === 422) {
                mostrarFormulario(await respuesta.text());
                return;
            }

            const datos = await respuesta.json();
            if (!respuesta.ok || !datos.ok) {
                Alertas.error(datos.mensaje ?? 'No se pudo guardar.');
                boton.disabled = false;
                return;
            }

            sessionStorage.setItem('alertaExito', datos.mensaje);
            location.reload();
        } catch {
            Alertas.error('No se pudo guardar. Intentá de nuevo.');
            boton.disabled = false;
        }
    };

    // Al abrirse el modal, el cursor va al campo Nombre
    elementoModal.addEventListener('shown.bs.modal', () => {
        document.getElementById('Nombre')?.focus();
    });

    document.querySelectorAll('.js-catalogo-nuevo, .js-catalogo-editar').forEach(boton => {
        boton.addEventListener('click', () => abrirFormulario(boton.dataset.url));
    });

    document.querySelectorAll('.js-catalogo-eliminar').forEach(boton => {
        boton.addEventListener('click', async () => {
            const confirmado = await Alertas.confirmar({
                titulo: `¿Eliminar "${boton.dataset.nombre}"?`,
                texto: 'Esta acción no se puede deshacer.',
                confirmar: 'Eliminar'
            });
            if (!confirmado) return;

            try {
                const respuesta = await fetch(boton.dataset.url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: new URLSearchParams({ __RequestVerificationToken: token() })
                });
                const datos = await respuesta.json();

                if (!datos.ok) {
                    // Mensaje largo (por ejemplo, "tiene 3 productos asociados"): se muestra como diálogo
                    Alertas.aviso('No se pudo eliminar', datos.mensaje);
                    return;
                }

                sessionStorage.setItem('alertaExito', datos.mensaje);
                location.reload();
            } catch {
                Alertas.error('No se pudo completar la operación. Intentá de nuevo.');
            }
        });
    });
})();
