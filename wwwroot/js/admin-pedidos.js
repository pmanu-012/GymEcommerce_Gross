// Cambio de estado de pedidos desde el panel de administración.
(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    document.querySelectorAll('.js-cambiar-estado').forEach(boton => {
        boton.addEventListener('click', async () => {
            const esCancelar = boton.dataset.estado === 'Cancelado';

            const confirmado = await Alertas.confirmar({
                titulo: `¿${boton.dataset.texto}?`,
                texto: esCancelar
                    ? 'El stock de los productos del pedido se va a devolver.'
                    : `El pedido pasará a "${boton.dataset.etiqueta}".`,
                confirmar: boton.dataset.texto
            });
            if (!confirmado) return;

            try {
                const respuesta = await fetch(boton.dataset.url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: new URLSearchParams({ estado: boton.dataset.estado, __RequestVerificationToken: token() })
                });
                const datos = await respuesta.json();

                if (!datos.ok) {
                    Alertas.aviso('No se pudo cambiar el estado', datos.mensaje);
                    return;
                }
                sessionStorage.setItem('alertaExito', datos.mensaje);
                location.reload();
            } catch {
                Alertas.error('No se pudo cambiar el estado. Intentá de nuevo.');
            }
        });
    });
})();
