using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymEcommerce.Services;

public record ResultadoPedido(bool Ok, string Mensaje, int? IdPedido = null);

public class PedidoService
{
    // Si otra compra modificó el stock al mismo tiempo, se vuelve a intentar con los datos actualizados
    private const int MaxIntentos = 3;

    private readonly ApplicationDbContext _db;
    private readonly OpcionesTienda _opciones;

    public PedidoService(ApplicationDbContext db, IOptions<OpcionesTienda> opciones)
    {
        _db = db;
        _opciones = opciones.Value;
    }

    // ---------- Crear pedido (checkout) ----------
    // Todo se guarda en un único SaveChangesAsync, que es una transacción:
    // se crea el pedido, se descuenta el stock y se vacía el carrito, o no pasa nada.
    public async Task<ResultadoPedido> CrearAsync(string idUsuario, CheckoutViewModel datos)
    {
        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            _db.ChangeTracker.Clear(); // en cada intento se leen los datos actuales de la base

            var items = await _db.CarritoItems
                .Include(c => c.Producto)
                .Where(c => c.IdUsuario == idUsuario)
                .ToListAsync();

            if (items.Count == 0)
                return new(false, "Tu carrito está vacío.");

            var sinStock = items.FirstOrDefault(i => !i.Producto.Activo || i.Producto.Stock < i.Cantidad);
            if (sinStock is not null)
                return new(false, $"\"{sinStock.Producto.Nombre}\" ya no tiene stock suficiente. Revisá tu carrito.");

            var pedido = new Pedido
            {
                IdUsuario = idUsuario,
                Fecha = DateTime.UtcNow,
                Estado = EstadoPedido.Pendiente,
                Calle = datos.Calle.Trim(),
                Numero = datos.Numero.Trim(),
                Ciudad = datos.Ciudad.Trim(),
                Provincia = datos.Provincia,
                CodigoPostal = datos.CodigoPostal.Trim().ToUpperInvariant()
            };

            decimal subtotal = 0;
            foreach (var item in items)
            {
                var precio = item.Producto.PrecioFinal; // foto del precio en el momento de la compra
                pedido.Detalles.Add(new DetallePedido
                {
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precio
                });
                subtotal += precio * item.Cantidad;

                item.Producto.Stock -= item.Cantidad; // RowVersion protege esta resta
            }

            pedido.CostoEnvio = _opciones.CalcularEnvio(subtotal);
            pedido.Total = subtotal + pedido.CostoEnvio;

            _db.Pedidos.Add(pedido);
            _db.CarritoItems.RemoveRange(items);

            try
            {
                await _db.SaveChangesAsync();
                return new(true, "¡Gracias por tu compra!", pedido.IdPedido);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Otro pedido cambió el stock de algún producto entre la lectura y el guardado. Se reintenta.
            }
        }

        return new(false, "Hubo mucha actividad sobre estos productos. Intentá confirmar la compra de nuevo.");
    }

    // ---------- Cambios de estado ----------

    public Task<ResultadoPedido> CambiarEstadoAsync(int idPedido, EstadoPedido nuevo) =>
        CambiarEstadoInternoAsync(idPedido, nuevo, idCliente: null);

    // El cliente solo puede cancelar sus propios pedidos y solo mientras estén pendientes
    public Task<ResultadoPedido> CancelarPorClienteAsync(int idPedido, string idUsuario) =>
        CambiarEstadoInternoAsync(idPedido, EstadoPedido.Cancelado, idCliente: idUsuario);

    private async Task<ResultadoPedido> CambiarEstadoInternoAsync(int idPedido, EstadoPedido nuevo, string? idCliente)
    {
        for (var intento = 1; intento <= MaxIntentos; intento++)
        {
            _db.ChangeTracker.Clear();

            var pedido = await _db.Pedidos
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.IdPedido == idPedido);

            if (pedido is null || (idCliente is not null && pedido.IdUsuario != idCliente))
                return new(false, "El pedido no existe.");

            if (idCliente is not null && pedido.Estado != EstadoPedido.Pendiente)
                return new(false, "Solo podés cancelar pedidos pendientes. Si necesitás ayuda, contactanos.");

            if (!TransicionesPedido.EsValida(pedido.Estado, nuevo))
                return new(false, $"Un pedido {pedido.Estado.Texto().ToLower()} no puede pasar a {nuevo.Texto().ToLower()}.");

            // Al cancelar, el stock vuelve a los productos
            if (nuevo == EstadoPedido.Cancelado)
            {
                foreach (var detalle in pedido.Detalles)
                    detalle.Producto.Stock += detalle.Cantidad;
            }

            pedido.Estado = nuevo;

            try
            {
                await _db.SaveChangesAsync();
                return new(true, $"Pedido #{pedido.IdPedido}: {nuevo.Texto().ToLower()}.", pedido.IdPedido);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Un producto del pedido cambió al mismo tiempo. Se reintenta con los datos nuevos.
            }
        }

        return new(false, "No se pudo actualizar el pedido. Intentá de nuevo.");
    }

    // ---------- Consultas ----------

    public Task<List<PedidoResumenViewModel>> ListarDelUsuarioAsync(string idUsuario) =>
        _db.Pedidos.AsNoTracking()
            .Where(p => p.IdUsuario == idUsuario)
            .OrderByDescending(p => p.Fecha)
            .Select(p => new PedidoResumenViewModel
            {
                IdPedido = p.IdPedido,
                Fecha = p.Fecha,
                Estado = p.Estado,
                Total = p.Total,
                CantidadProductos = p.Detalles.Sum(d => d.Cantidad)
            })
            .ToListAsync();

    // Con idUsuario: solo si el pedido es de ese usuario. Sin idUsuario: cualquier pedido (administración).
    public async Task<PedidoDetalleViewModel?> ObtenerDetalleAsync(int idPedido, string? idUsuario = null)
    {
        var vm = await _db.Pedidos.AsNoTracking()
            .Where(p => p.IdPedido == idPedido && (idUsuario == null || p.IdUsuario == idUsuario))
            .Select(p => new PedidoDetalleViewModel
            {
                IdPedido = p.IdPedido,
                Fecha = p.Fecha,
                Estado = p.Estado,
                CostoEnvio = p.CostoEnvio,
                Total = p.Total,
                Calle = p.Calle,
                Numero = p.Numero,
                Ciudad = p.Ciudad,
                Provincia = p.Provincia,
                CodigoPostal = p.CodigoPostal,
                ClienteNombre = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                ClienteEmail = p.Usuario.Email,
                ClienteTelefono = p.Usuario.PhoneNumber,
                Items = p.Detalles
                    .OrderBy(d => d.IdDetalle)
                    .Select(d => new PedidoDetalleItemViewModel
                    {
                        IdProducto = d.IdProducto,
                        Nombre = d.Producto.Nombre,
                        ImagenUrl = d.Producto.ImagenUrl,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (vm is not null)
            vm.EstadosSiguientes = TransicionesPedido.Siguientes(vm.Estado).ToList();

        return vm;
    }

    // Para precargar el formulario de envío con la dirección del último pedido
    public Task<CheckoutViewModel?> UltimaDireccionAsync(string idUsuario) =>
        _db.Pedidos.AsNoTracking()
            .Where(p => p.IdUsuario == idUsuario)
            .OrderByDescending(p => p.Fecha)
            .Select(p => new CheckoutViewModel
            {
                Calle = p.Calle,
                Numero = p.Numero,
                Ciudad = p.Ciudad,
                Provincia = p.Provincia,
                CodigoPostal = p.CodigoPostal
            })
            .FirstOrDefaultAsync();
}
