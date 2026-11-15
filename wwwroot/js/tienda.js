// Comportamiento de la tienda pública: agregar al carrito, página del carrito,
// cancelar pedidos y evitar el doble envío del checkout.
(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // El header X-Requested-With hace que el servidor responda 401/403 en vez de redirigir al login
    const post = (url, datos) => fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: new URLSearchParams({ ...datos, __RequestVerificationToken: token() })
    });

    // true si hubo que redirigir o avisar por falta de permisos
    const sinPermiso = (respuesta) => {
        if (respuesta.status === 401) {
            location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
            return true;
        }
        if (respuesta.status === 403) {
            Alertas.error('Esta acción es solo para clientes.');
            return true;
        }
        return false;
    };

    const actualizarContador = (cantidad) => {
        const contador = document.getElementById('contadorCarrito');
        if (!contador || cantidad == null) return;
        contador.textContent = cantidad;
        contador.classList.toggle('d-none', Number(cantidad) === 0);
    };

    // Aviso pendiente de una operación que recargó la página
    const pendiente = sessionStorage.getItem('alertaExito');
    if (pendiente) {
        sessionStorage.removeItem('alertaExito');
        Alertas.exito(pendiente);
    }

    // ----- Agregar al carrito (tarjetas y detalle) -----
    document.addEventListener('click', async (evento) => {
        const boton = evento.target.closest('.js-agregar');
        if (!boton) return;

        const selector = boton.dataset.cantidadInput;
        const cantidad = selector ? (document.querySelector(selector)?.value ?? 1) : 1;

        boton.disabled = true;
        try {
            const respuesta = await post(boton.dataset.url, { idProducto: boton.dataset.id, cantidad });
            if (sinPermiso(respuesta)) return;

            const datos = await respuesta.json();
            actualizarContador(datos.cantidad);
            datos.ok ? Alertas.exito(datos.mensaje) : Alertas.error(datos.mensaje);
        } catch {
            Alertas.error('No se pudo agregar el producto. Intentá de nuevo.');
        } finally {
            boton.disabled = false;
        }
    });

    // ----- Página del carrito -----
    const contenedor = document.getElementById('carritoContenido');
    if (contenedor) {
        const enviar = async (url, datos) => {
            try {
                const respuesta = await post(url, datos);
                if (sinPermiso(respuesta)) return;

                if (!respuesta.ok) {
                    const error = await respuesta.json().catch(() => ({}));
                    Alertas.error(error.mensaje ?? 'No se pudo actualizar el carrito.');
                    return;
                }

                // El servidor devuelve el HTML del carrito ya actualizado
                contenedor.innerHTML = await respuesta.text();
                actualizarContador(respuesta.headers.get('X-Carrito-Cantidad'));

                const aviso = respuesta.headers.get('X-Carrito-Aviso');
                if (aviso) Alertas.aviso('Atención', decodeURIComponent(aviso));
            } catch {
                Alertas.error('No se pudo actualizar el carrito. Intentá de nuevo.');
            }
        };

        contenedor.addEventListener('click', async (evento) => {
            const botonCantidad = evento.target.closest('.js-cantidad');
            if (botonCantidad) {
                const input = contenedor.querySelector(`.js-cantidad-input[data-id="${botonCantidad.dataset.id}"]`);
                const nueva = Number(input.value) + Number(botonCantidad.dataset.delta);
                if (nueva >= 1) await enviar(contenedor.dataset.urlActualizar, { idCarritoItem: botonCantidad.dataset.id, cantidad: nueva });
                return;
            }

            const botonQuitar = evento.target.closest('.js-quitar');
            if (botonQuitar) {
                const confirmado = await Alertas.confirmar({
                    titulo: '¿Quitar el producto?',
                    texto: `"${botonQuitar.dataset.nombre}" se quitará de tu carrito.`,
                    confirmar: 'Quitar'
                });
                if (confirmado) await enviar(contenedor.dataset.urlQuitar, { idCarritoItem: botonQuitar.dataset.id });
            }
        });

        // Cantidad escrita a mano: se envía al salir del campo o al presionar Enter
        contenedor.addEventListener('change', (evento) => {
            const input = evento.target.closest('.js-cantidad-input');
            if (!input) return;
            const cantidad = Math.max(1, parseInt(input.value, 10) || 1);
            enviar(contenedor.dataset.urlActualizar, { idCarritoItem: input.dataset.id, cantidad });
        });
    }

    // ----- Cancelar pedido (cliente) -----
    document.querySelectorAll('.js-cancelar-pedido').forEach(boton => {
        boton.addEventListener('click', async () => {
            const confirmado = await Alertas.confirmar({
                titulo: `¿Cancelar el pedido #${boton.dataset.id}?`,
                texto: 'Esta acción no se puede deshacer.',
                confirmar: 'Cancelar pedido'
            });
            if (!confirmado) return;

            try {
                const respuesta = await post(boton.dataset.url, {});
                if (sinPermiso(respuesta)) return;

                const datos = await respuesta.json();
                if (!datos.ok) {
                    Alertas.aviso('No se pudo cancelar', datos.mensaje);
                    return;
                }
                sessionStorage.setItem('alertaExito', datos.mensaje);
                location.reload();
            } catch {
                Alertas.error('No se pudo cancelar el pedido. Intentá de nuevo.');
            }
        });
    });

    // ----- Checkout: evita confirmar dos veces la misma compra -----
    const formCheckout = document.getElementById('formCheckout');
    if (formCheckout) {
        formCheckout.addEventListener('submit', () => {
            if (window.jQuery && jQuery.fn.valid && !jQuery(formCheckout).valid()) return;
            const boton = formCheckout.querySelector('[type="submit"]');
            boton.disabled = true;
            boton.textContent = 'Procesando...';
        });
    }
})();
