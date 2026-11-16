// Baja lógica y reactivación de clientes desde el panel de administración.
(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    document.querySelectorAll('.js-estado-cliente').forEach(boton => {
        boton.addEventListener('click', async () => {
            const estaActivo = boton.dataset.activo === 'true';

            const confirmado = await Alertas.confirmar({
                titulo: estaActivo ? '¿Dar de baja al cliente?' : '¿Reactivar al cliente?',
                texto: estaActivo
                    ? `${boton.dataset.nombre} no va a poder iniciar sesión y se cerrará su sesión abierta. Sus pedidos se conservan.`
                    : `${boton.dataset.nombre} va a poder volver a iniciar sesión.`,
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
})();
