// Reseñas de productos y de la tienda: publicar, editar, eliminar y paginar sin recargar la página.
// La sección se vuelve a pedir al servidor después de cada cambio, así el promedio y la lista quedan al día.
(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const encabezadosAjax = { 'X-Requested-With': 'XMLHttpRequest' };

    const irAlLogin = () => {
        location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search + '#resenas');
    };

    // Reemplaza la sección por la versión nueva que devuelve el servidor
    const recargar = async (seccion, url) => {
        const respuesta = await fetch(url ?? seccion.dataset.urlRecargar, { headers: encabezadosAjax });
        if (!respuesta.ok) throw new Error('No se pudo recargar la sección');

        const plantilla = document.createElement('template');
        plantilla.innerHTML = (await respuesta.text()).trim();
        const nueva = plantilla.content.firstElementChild;
        seccion.replaceWith(nueva);

        // El formulario nuevo necesita que jQuery Unobtrusive lea sus reglas de validación
        if (window.jQuery?.validator) $.validator.unobtrusive.parse(nueva);
        return nueva;
    };

    // ----- Publicar o editar -----
    // Si el formulario no es válido, jQuery Validation frena el evento antes de que llegue acá
    document.addEventListener('submit', async (evento) => {
        const form = evento.target.closest('.js-form-resena');
        if (!form) return;
        evento.preventDefault();
        if (window.jQuery && !$(form).valid()) return;

        const boton = form.querySelector('[type="submit"]');
        boton.disabled = true;

        try {
            const respuesta = await fetch(form.action, { method: 'POST', headers: encabezadosAjax, body: new FormData(form) });
            if (respuesta.status === 401) return irAlLogin();

            const datos = await respuesta.json();
            if (!datos.ok) {
                Alertas.aviso('No se pudo guardar', datos.mensaje);
                boton.disabled = false;
                return;
            }

            await recargar(form.closest('.js-resenas'));
            Alertas.exito(datos.mensaje);
        } catch {
            Alertas.error('No se pudo guardar tu opinión. Intentá de nuevo.');
            boton.disabled = false;
        }
    });

    document.addEventListener('click', async (evento) => {
        // ----- Eliminar la propia -----
        const eliminar = evento.target.closest('.js-eliminar-resena');
        if (eliminar) {
            const confirmado = await Alertas.confirmar({
                titulo: '¿Eliminar tu opinión?',
                texto: 'Esta acción no se puede deshacer.',
                confirmar: 'Eliminar'
            });
            if (!confirmado) return;

            try {
                const respuesta = await fetch(eliminar.dataset.url, {
                    method: 'POST',
                    headers: { ...encabezadosAjax, 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: new URLSearchParams({ __RequestVerificationToken: token() })
                });
                if (respuesta.status === 401) return irAlLogin();

                const datos = await respuesta.json();
                if (!datos.ok) {
                    Alertas.aviso('No se pudo eliminar', datos.mensaje);
                    return;
                }
                await recargar(eliminar.closest('.js-resenas'));
                Alertas.exito(datos.mensaje);
            } catch {
                Alertas.error('No se pudo eliminar tu opinión. Intentá de nuevo.');
            }
            return;
        }

        // ----- Paginación -----
        const enlace = evento.target.closest('.js-pagina-resenas');
        if (enlace) {
            evento.preventDefault();
            try {
                const nueva = await recargar(enlace.closest('.js-resenas'), enlace.href);
                nueva.scrollIntoView({ behavior: 'smooth', block: 'start' });
            } catch {
                Alertas.error('No se pudieron cargar las opiniones.');
            }
        }
    });
})();
