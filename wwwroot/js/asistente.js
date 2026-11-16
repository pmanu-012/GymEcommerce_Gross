// Asistente virtual de compras: envía la consulta al servidor, muestra la respuesta
// y las tarjetas de los productos recomendados. La conversación se guarda en sessionStorage
// (dura mientras la pestaña esté abierta), así no se pierde al cambiar de página.
(() => {
    const panel = document.getElementById('asistente');
    if (!panel) return;

    const lista = document.getElementById('asistenteMensajes');
    const form = document.getElementById('asistenteForm');
    const campo = document.getElementById('asistenteTexto');
    const contenidoInicial = lista.innerHTML;

    const CLAVE = 'asistenteConversacion';
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const moneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });

    let historial = [];
    let enviando = false;

    const guardar = () => { try { sessionStorage.setItem(CLAVE, JSON.stringify(historial.slice(-20))); } catch { /* sin almacenamiento */ } };
    const leer = () => { try { return JSON.parse(sessionStorage.getItem(CLAVE) ?? '[]'); } catch { return []; } };

    // Todo el texto se inserta con textContent: lo que escriba el cliente o la IA nunca se interpreta como HTML
    const crear = (etiqueta, clase, texto) => {
        const el = document.createElement(etiqueta);
        if (clase) el.className = clase;
        if (texto != null) el.textContent = texto;
        return el;
    };

    const bajar = () => { lista.scrollTop = lista.scrollHeight; };

    const tarjeta = (p, puedeComprar, urlAgregar) => {
        const item = crear('div', 'gs-asistente-producto');

        const imagen = document.createElement('img');
        imagen.src = p.imagenUrl;
        imagen.alt = '';
        imagen.loading = 'lazy';

        const info = crear('div', 'flex-grow-1 min-w-0');
        const nombre = crear('a', 'fw-medium d-block text-truncate', p.nombre);
        nombre.href = p.url;
        const precio = crear('div', 'small');
        if (p.enOferta) precio.append(crear('del', 'text-muted me-1', moneda.format(p.precio)));
        precio.append(crear('span', 'fw-semibold', moneda.format(p.precioFinal)));
        info.append(nombre, precio);

        item.append(imagen, info);

        // Reutiliza el "Agregar al carrito" de tienda.js (escucha los clics en .js-agregar)
        if (puedeComprar) {
            const boton = crear('button', 'btn btn-sm btn-outline-primary js-agregar');
            boton.type = 'button';
            boton.dataset.url = urlAgregar;
            boton.dataset.id = p.idProducto;
            boton.setAttribute('aria-label', `Agregar ${p.nombre} al carrito`);
            boton.innerHTML = '<i class="bi bi-cart-plus" aria-hidden="true"></i>';
            item.append(boton);
        }
        return item;
    };

    const mostrar = (mensaje) => {
        lista.append(crear('div', mensaje.rol === 'usuario' ? 'gs-msg gs-msg-usuario' : 'gs-msg gs-msg-asistente', mensaje.texto));
        if (mensaje.productos?.length) {
            const contenedor = crear('div', 'gs-asistente-productos');
            mensaje.productos.forEach(p => contenedor.append(tarjeta(p, mensaje.puedeComprar, mensaje.urlAgregar)));
            lista.append(contenedor);
        }
        bajar();
    };

    const mostrarError = (texto) => {
        lista.append(crear('div', 'gs-msg gs-msg-error', texto));
        bajar();
    };

    const ajustarAlto = () => {
        campo.style.height = 'auto';
        campo.style.height = Math.min(campo.scrollHeight, 120) + 'px';
    };

    const enviar = async (consulta) => {
        consulta = consulta.trim();
        if (!consulta || enviando) return;
        enviando = true;

        document.getElementById('asistenteSugerencias')?.remove();

        const anteriores = historial.map(m => ({ rol: m.rol, texto: m.texto }));
        const delUsuario = { rol: 'usuario', texto: consulta };
        mostrar(delUsuario);
        campo.value = '';
        ajustarAlto();

        const pensando = crear('div', 'gs-msg gs-msg-asistente gs-pensando', 'Pensando...');
        lista.append(pensando);
        bajar();

        try {
            const respuesta = await fetch(panel.dataset.url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token(),
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({ mensaje: consulta, historial: anteriores })
            });

            let datos = null;
            try { datos = await respuesta.json(); } catch { /* la respuesta no era JSON */ }
            pensando.remove();

            if (!datos?.ok) {
                mostrarError(datos?.mensaje ?? 'No pude responder en este momento. Probá de nuevo.');
                return;
            }

            const delAsistente = {
                rol: 'asistente',
                texto: datos.respuesta,
                productos: datos.productos,
                puedeComprar: datos.puedeComprar,
                urlAgregar: datos.urlAgregar
            };
            historial.push(delUsuario, delAsistente);
            guardar();
            mostrar(delAsistente);
        } catch {
            pensando.remove();
            mostrarError('No pude conectarme. Revisá tu conexión e intentá de nuevo.');
        } finally {
            enviando = false;
            campo.focus();
        }
    };

    // ----- Eventos -----

    form.addEventListener('submit', (evento) => {
        evento.preventDefault();
        enviar(campo.value);
    });

    // Enter envía; Shift + Enter hace un salto de línea
    campo.addEventListener('keydown', (evento) => {
        if (evento.key === 'Enter' && !evento.shiftKey) {
            evento.preventDefault();
            enviar(campo.value);
        }
    });
    campo.addEventListener('input', ajustarAlto);

    lista.addEventListener('click', (evento) => {
        const sugerencia = evento.target.closest('.js-sugerencia');
        if (sugerencia) enviar(sugerencia.textContent);
    });

    document.getElementById('asistenteReiniciar').addEventListener('click', () => {
        historial = [];
        guardar();
        lista.innerHTML = contenidoInicial;
        campo.focus();
    });

    panel.addEventListener('shown.bs.offcanvas', () => campo.focus());

    // ----- Conversación guardada -----
    historial = leer();
    if (historial.length) {
        document.getElementById('asistenteSugerencias')?.remove();
        historial.forEach(mostrar);
    }
})();
