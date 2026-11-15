// Ingresos de mercadería: filas dinámicas del formulario y anulación.
(() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // "1.250,50" → 1250.5
    const aNumero = (valor) => {
        const numero = parseFloat(String(valor ?? '').replace(/\./g, '').replace(',', '.'));
        return isNaN(numero) ? 0 : numero;
    };
    const moneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

    // ---------- Formulario de registro ----------
    const form = document.getElementById('formIngreso');
    if (form) {
        const contenedor = document.getElementById('filasIngreso');
        const plantilla = document.getElementById('plantillaFila');
        const filas = () => [...contenedor.querySelectorAll('.fila-ingreso')];

        // jQuery Unobtrusive solo lee los campos al cargar la página: con filas nuevas hay que volver a leerlos
        const reactivarValidacion = () => {
            const $form = $(form);
            $form.removeData('validator').removeData('unobtrusiveValidation');
            $.validator.unobtrusive.parse(form);
        };

        // Renumera Detalles[0], Detalles[1]... para que MVC reciba una lista sin huecos
        const reindexar = () => {
            filas().forEach((fila, i) => {
                fila.querySelectorAll('[name], [id], [for], [data-valmsg-for]').forEach(el => {
                    for (const atributo of ['name', 'id', 'for', 'data-valmsg-for']) {
                        const valor = el.getAttribute(atributo);
                        if (!valor) continue;
                        el.setAttribute(atributo, valor
                            .replace(/Detalles\[(\d+|__i__)\]/, `Detalles[${i}]`)
                            .replace(/Detalles_(\d+|__i__)__/, `Detalles_${i}__`));
                    }
                });
                fila.querySelector('.js-numero-fila').textContent = i + 1;
            });
        };

        const actualizarTotal = () => {
            let unidades = 0;
            let total = 0;
            filas().forEach(fila => {
                const cantidad = parseInt(fila.querySelector('.js-cantidad').value, 10) || 0;
                unidades += cantidad;
                total += cantidad * aNumero(fila.querySelector('.js-costo').value);
            });
            document.getElementById('totalUnidades').textContent = unidades;
            document.getElementById('totalIngreso').textContent = moneda.format(total);
        };

        document.getElementById('btnAgregarFila').addEventListener('click', () => {
            contenedor.insertAdjacentHTML('beforeend', plantilla.innerHTML.replaceAll('__i__', filas().length));
            reindexar();
            reactivarValidacion();
            filas().at(-1).querySelector('select').focus();
        });

        contenedor.addEventListener('click', (evento) => {
            const boton = evento.target.closest('.js-quitar-fila');
            if (!boton) return;

            if (filas().length === 1) {
                Alertas.error('El ingreso necesita al menos un producto.');
                return;
            }
            boton.closest('.fila-ingreso').remove();
            reindexar();
            reactivarValidacion();
            actualizarTotal();
        });

        contenedor.addEventListener('input', actualizarTotal);

        form.addEventListener('submit', (evento) => {
            if (!$(form).valid()) return; // jQuery Validation ya muestra los errores y frena el envío

            const elegidos = filas().map(f => f.querySelector('.js-producto').value).filter(Boolean);
            if (new Set(elegidos).size !== elegidos.length) {
                evento.preventDefault();
                Alertas.aviso('Producto repetido', 'Un mismo producto aparece en más de una fila. Unificá las cantidades en una sola.');
                return;
            }

            const boton = form.querySelector('[type="submit"]');
            boton.disabled = true;
            boton.textContent = 'Registrando...';
        });

        actualizarTotal();
    }

    // ---------- Anular ----------
    document.querySelectorAll('.js-anular-ingreso').forEach(boton => {
        boton.addEventListener('click', async () => {
            const confirmado = await Alertas.confirmar({
                titulo: `¿Anular el ingreso #${boton.dataset.id}?`,
                texto: 'Las unidades de este ingreso se van a descontar del stock. Esta acción no se puede deshacer.',
                confirmar: 'Anular ingreso'
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
                    Alertas.aviso('No se pudo anular', datos.mensaje);
                    return;
                }
                sessionStorage.setItem('alertaExito', datos.mensaje);
                location.reload();
            } catch {
                Alertas.error('No se pudo anular el ingreso. Intentá de nuevo.');
            }
        });
    });
})();
