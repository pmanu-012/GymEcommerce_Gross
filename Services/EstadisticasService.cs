using System.Globalization;
using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Services;

public class EstadisticasService
{
    public const int UmbralStockBajo = 5;

    private static readonly CultureInfo Ar = new("es-AR");
    private readonly ApplicationDbContext _db;

    public EstadisticasService(ApplicationDbContext db) => _db = db;

    public async Task<EstadisticasViewModel> ObtenerAsync(int dias)
    {
        if (!EstadisticasViewModel.Periodos.Any(p => p.Dias == dias))
            dias = 30;

        var vm = new EstadisticasViewModel { Dias = dias, Mensual = dias == 365 };

        // Período en hora local (incluye hoy) y su equivalente en UTC para consultar la base
        var hoy = Fechas.ALocal(DateTime.UtcNow).Date;
        DateTime desdeLocal, desdeAnteriorLocal;
        if (vm.Mensual)
        {
            desdeLocal = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-11);
            desdeAnteriorLocal = desdeLocal.AddMonths(-12);
        }
        else
        {
            desdeLocal = hoy.AddDays(-(dias - 1));
            desdeAnteriorLocal = desdeLocal.AddDays(-dias);
        }
        vm.DesdeLocal = desdeLocal;

        var desde = Fechas.AUtc(desdeLocal);
        var desdeAnterior = Fechas.AUtc(desdeAnteriorLocal);

        // Ventas = pedidos no cancelados
        var validos = _db.Pedidos.AsNoTracking().Where(p => p.Estado != EstadoPedido.Cancelado);
        var enPeriodo = validos.Where(p => p.Fecha >= desde);

        vm.Ventas = await enPeriodo.SumAsync(p => (decimal?)p.Total) ?? 0;
        vm.Pedidos = await enPeriodo.CountAsync();

        var ventasAnteriores = await validos
            .Where(p => p.Fecha >= desdeAnterior && p.Fecha < desde)
            .SumAsync(p => (decimal?)p.Total) ?? 0;
        vm.VariacionVentas = ventasAnteriores == 0 ? null : Math.Round((vm.Ventas - ventasAnteriores) / ventasAnteriores * 100, 1);

        var detallesEnPeriodo = _db.DetallesPedido.AsNoTracking()
            .Where(d => d.Pedido.Estado != EstadoPedido.Cancelado && d.Pedido.Fecha >= desde);

        vm.Unidades = await detallesEnPeriodo.SumAsync(d => (int?)d.Cantidad) ?? 0;

        // Serie: se agrupa en C# porque los días se cuentan en hora local, no en UTC
        var ventas = await enPeriodo.Select(p => new { p.Fecha, p.Total }).ToListAsync();
        vm.Serie = vm.Mensual
            ? Enumerable.Range(0, 12).Select(i => desdeLocal.AddMonths(i)).Select(mes => new PuntoSerie
            {
                Etiqueta = mes.ToString("MMM yy", Ar),
                Valor = ventas.Where(v => EsMismoMes(Fechas.ALocal(v.Fecha), mes)).Sum(v => v.Total)
            }).ToList()
            : Enumerable.Range(0, dias).Select(i => desdeLocal.AddDays(i)).Select(dia => new PuntoSerie
            {
                Etiqueta = dia.ToString("dd/MM", Ar),
                Valor = ventas.Where(v => Fechas.ALocal(v.Fecha).Date == dia).Sum(v => v.Total)
            }).ToList();

        vm.TopProductos = await detallesEnPeriodo
            .GroupBy(d => new { d.IdProducto, d.Producto.Nombre })
            .Select(g => new RankingItem
            {
                Id = g.Key.IdProducto,
                Nombre = g.Key.Nombre,
                Unidades = g.Sum(d => d.Cantidad),
                Ingresos = g.Sum(d => d.Cantidad * d.PrecioUnitario)
            })
            .OrderByDescending(r => r.Unidades).ThenByDescending(r => r.Ingresos)
            .Take(5)
            .ToListAsync();

        vm.VentasPorCategoria = await detallesEnPeriodo
            .GroupBy(d => d.Producto.Subcategoria.Categoria.Nombre)
            .Select(g => new RankingItem
            {
                Nombre = g.Key,
                Unidades = g.Sum(d => d.Cantidad),
                Ingresos = g.Sum(d => d.Cantidad * d.PrecioUnitario)
            })
            .OrderByDescending(r => r.Ingresos)
            .ToListAsync();

        var porEstado = await _db.Pedidos.AsNoTracking()
            .Where(p => p.Fecha >= desde)
            .GroupBy(p => p.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();
        vm.PedidosPorEstado = Enum.GetValues<EstadoPedido>()
            .ToDictionary(e => e, e => porEstado.FirstOrDefault(x => x.Estado == e)?.Cantidad ?? 0);

        // Pendientes y stock bajo: lo que el administrador tiene que atender, sin importar el período
        var pendientes = _db.Pedidos.AsNoTracking().Where(p => p.Estado == EstadoPedido.Pendiente);
        vm.PendientesTotal = await pendientes.CountAsync();
        vm.UltimosPendientes = await pendientes
            .OrderBy(p => p.Fecha)
            .Take(5)
            .Select(p => new PedidoFilaAdminViewModel
            {
                IdPedido = p.IdPedido,
                Fecha = p.Fecha,
                Cliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                Email = p.Usuario.Email,
                Estado = p.Estado,
                Total = p.Total
            })
            .ToListAsync();

        vm.StockBajo = await _db.Productos.AsNoTracking()
            .Where(p => p.Activo && p.Stock <= UmbralStockBajo)
            .OrderBy(p => p.Stock).ThenBy(p => p.Nombre)
            .Take(10)
            .Select(p => new StockBajoItem { IdProducto = p.IdProducto, Nombre = p.Nombre, Stock = p.Stock })
            .ToListAsync();

        return vm;
    }

    private static bool EsMismoMes(DateTime fecha, DateTime mes) => fecha.Year == mes.Year && fecha.Month == mes.Month;
}
