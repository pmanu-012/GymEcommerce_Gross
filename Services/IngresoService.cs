using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Services;

public record ResultadoIngreso(bool Ok, string Mensaje, int? IdIngreso = null);

public class IngresoService
{
    // Igual que en los pedidos: si una venta cambia el stock al mismo tiempo, se reintenta con los datos nuevos
    private const int MaxIntentos = 3;

    private readonly ApplicationDbContext _db;

    public IngresoService(ApplicationDbContext db) => _db = db;

    // Crea el ingreso y suma el stock de cada producto, todo en una misma transacción.
    // Se asume que el controlador ya validó los datos (sin filas vacías ni productos repetidos).
    public async Task<ResultadoIngreso> RegistrarAsync(string idUsuario, IngresoFormViewModel vm)
    {
        var lineas = vm.Detalles
            .Select(d => (IdProducto: d.IdProducto!.Value, Cantidad: d.Cantidad!.Value, Costo: d.CostoUnitario!.Value))
            .ToList();
        var ids = lineas.Select(l => l.IdProducto).ToList();

        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            _db.ChangeTracker.Clear();

            var productos = await _db.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .ToDictionaryAsync(p => p.IdProducto);

            if (productos.Count != ids.Count)
                return new(false, "Uno de los productos elegidos ya no existe.");

            var ingreso = new IngresoMercaderia
            {
                Fecha = DateTime.UtcNow,
                Proveedor = vm.Proveedor.Trim(),
                NumeroComprobante = string.IsNullOrWhiteSpace(vm.NumeroComprobante) ? null : vm.NumeroComprobante.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(vm.Observaciones) ? null : vm.Observaciones.Trim(),
                IdUsuario = idUsuario
            };

            foreach (var linea in lineas)
            {
                ingreso.Detalles.Add(new DetalleIngreso
                {
                    IdProducto = linea.IdProducto,
                    Cantidad = linea.Cantidad,
                    CostoUnitario = linea.Costo
                });
                productos[linea.IdProducto].Stock += linea.Cantidad;
            }

            _db.IngresosMercaderia.Add(ingreso);

            try
            {
                await _db.SaveChangesAsync();
                var unidades = lineas.Sum(l => l.Cantidad);
                return new(true, $"Ingreso #{ingreso.IdIngreso} registrado: {unidades} unidades sumadas al stock.", ingreso.IdIngreso);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Otro proceso modificó el stock de algún producto. Se reintenta.
            }
        }

        return new(false, "No se pudo registrar el ingreso por cambios simultáneos en el stock. Intentá de nuevo.");
    }

    // Anula el ingreso y descuenta del stock lo que había sumado.
    // No se permite si parte de esa mercadería ya se vendió (el stock quedaría negativo).
    public async Task<ResultadoIngreso> AnularAsync(int idIngreso)
    {
        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            _db.ChangeTracker.Clear();

            var ingreso = await _db.IngresosMercaderia
                .Include(i => i.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(i => i.IdIngreso == idIngreso);

            if (ingreso is null)
                return new(false, "El ingreso no existe.");

            if (ingreso.Anulado)
                return new(false, "Este ingreso ya estaba anulado.");

            var insuficiente = ingreso.Detalles.FirstOrDefault(d => d.Producto.Stock < d.Cantidad);
            if (insuficiente is not null)
                return new(false,
                    $"No se puede anular: \"{insuficiente.Producto.Nombre}\" tiene {insuficiente.Producto.Stock} unidades " +
                    $"y este ingreso sumó {insuficiente.Cantidad}. Parte de esa mercadería ya se vendió.");

            foreach (var detalle in ingreso.Detalles)
                detalle.Producto.Stock -= detalle.Cantidad;

            ingreso.Anulado = true;
            ingreso.FechaAnulacion = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
                return new(true, $"Ingreso #{ingreso.IdIngreso} anulado. Se descontó el stock.", ingreso.IdIngreso);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Una venta cambió el stock al mismo tiempo. Se reintenta con los datos actuales.
            }
        }

        return new(false, "No se pudo anular el ingreso por cambios simultáneos en el stock. Intentá de nuevo.");
    }
}
