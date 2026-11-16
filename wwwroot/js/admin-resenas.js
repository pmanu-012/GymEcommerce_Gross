// Moderación de reseñas desde el panel de administración.
(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    document.querySelectorAll('.js-eliminar-resena-admin').forEach(boton => {
        boton.addEventListener('click', async () => {
            const confirmado = await Alertas.confirmar({
                titulo: '¿Eliminar la reseña?',
                texto: `La reseña de ${boton.dataset.autor} se eliminará de la tienda.`,
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
                    Alertas.aviso('No se pudo eliminar', datos.mensaje);
                    return;
                }
                sessionStorage.setItem('alertaExito', datos.mensaje);
                location.reload();
            } catch {
                Alertas.error('No se pudo eliminar la reseña. Intentá de nuevo.');
            }
        });
    });
})();
