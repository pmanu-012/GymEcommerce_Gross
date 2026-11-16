// Gráfico de ventas del resumen del panel (Chart.js).
(() => {
    const canvas = document.getElementById('graficoVentas');
    const fuente = document.getElementById('datosVentas');
    if (!canvas || !fuente || !window.Chart) return;

    const datos = JSON.parse(fuente.textContent);

    // Naranja oscurecido de la marca: contraste suficiente (3.8:1) contra el fondo blanco
    const color = '#E84E0B';
    const moneda = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
    const compacto = new Intl.NumberFormat('es-AR', { notation: 'compact', maximumFractionDigits: 1 });

    Chart.defaults.font.family = "'Inter', system-ui, sans-serif";
    Chart.defaults.color = '#6B7280';

    new Chart(canvas, {
        // Por día: línea (se ve la tendencia). Por mes: barras (cada mes es un total).
        type: datos.mensual ? 'bar' : 'line',
        data: {
            labels: datos.etiquetas,
            datasets: [{
                label: 'Ventas',
                data: datos.valores,
                borderColor: color,
                backgroundColor: datos.mensual ? color : 'rgba(232, 78, 11, 0.08)',
                borderWidth: 2,
                fill: !datos.mensual,
                tension: 0.25,
                pointRadius: 0,
                pointHoverRadius: 5,
                pointHitRadius: 12,
                borderRadius: datos.mensual ? 4 : 0,
                maxBarThickness: 36
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { display: false }, // una sola serie: el título de la tarjeta ya la nombra
                tooltip: {
                    callbacks: { label: (contexto) => ` ${moneda.format(contexto.parsed.y)}` }
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { autoSkip: true, maxTicksLimit: 10, maxRotation: 0 }
                },
                y: {
                    beginAtZero: true,
                    border: { display: false },
                    grid: { color: '#EEF0F2' },
                    ticks: { callback: (valor) => '$ ' + compacto.format(valor) }
                }
            }
        }
    });
})();
