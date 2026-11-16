using GymEcommerce.Data;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Services;

// Emails relacionados con los pedidos. Devuelven true si el email salió.
// Un error de envío nunca deshace la operación: el pedido ya quedó guardado.
public class NotificacionService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;

    public NotificacionService(ApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<bool> PedidoRecibidoAsync(int idPedido, string urlDetalle)
    {
        var datos = await _db.Pedidos.AsNoTracking()
            .Where(p => p.IdPedido == idPedido)
            .Select(p => new
            {
                p.Usuario.Email,
                p.Usuario.Nombre,
                p.CostoEnvio,
                p.Total,
                Items = p.Detalles
                    .OrderBy(d => d.IdDetalle)
                    .Select(d => new { d.Producto.Nombre, d.Cantidad, d.PrecioUnitario })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (datos?.Email is null)
            return false;

        var contenido = PlantillasEmail.PedidoRecibido(
            datos.Nombre, idPedido,
            datos.Items.Select(i => (i.Nombre, i.Cantidad, i.PrecioUnitario)),
            datos.CostoEnvio, datos.Total, urlDetalle);

        return (await _email.EnviarAsync(datos.Email, contenido)).Ok;
    }

    public async Task<bool> CambioEstadoAsync(int idPedido, string urlDetalle)
    {
        var datos = await _db.Pedidos.AsNoTracking()
            .Where(p => p.IdPedido == idPedido)
            .Select(p => new { p.Usuario.Email, p.Usuario.Nombre, p.Estado })
            .FirstOrDefaultAsync();

        if (datos?.Email is null)
            return false;

        var contenido = PlantillasEmail.CambioEstado(datos.Nombre, idPedido, datos.Estado, urlDetalle);
        return (await _email.EnviarAsync(datos.Email, contenido)).Ok;
    }
}
