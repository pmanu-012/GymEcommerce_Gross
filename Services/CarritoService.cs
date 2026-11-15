using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymEcommerce.Services;

// Ok = la operación se hizo. Mensaje = texto para mostrar (en un Ok puede ser un aviso, por ejemplo "se ajustó la cantidad").
public record ResultadoCarrito(bool Ok, string? Mensaje = null);

public class CarritoService
{
    public const int MaximoPorProducto = 99;

    private readonly ApplicationDbContext _db;
    private readonly OpcionesTienda _opciones;

    public CarritoService(ApplicationDbContext db, IOptions<OpcionesTienda> opciones)
    {
        _db = db;
        _opciones = opciones.Value;
    }

    public async Task<CarritoViewModel> ObtenerAsync(string idUsuario)
    {
        var filas = await _db.CarritoItems.AsNoTracking()
            .Where(c => c.IdUsuario == idUsuario)
            .OrderBy(c => c.IdCarritoItem)
            .Select(c => new { c.IdCarritoItem, c.Cantidad, c.Producto })
            .ToListAsync();

        var items = filas.Select(f => new CarritoItemViewModel
        {
            IdCarritoItem = f.IdCarritoItem,
            IdProducto = f.Producto.IdProducto,
            Nombre = f.Producto.Nombre,
            ImagenUrl = f.Producto.ImagenUrl,
            PrecioUnitario = f.Producto.PrecioFinal,
            PrecioOriginal = f.Producto.Precio,
            EnOferta = f.Producto.EnOferta,
            Cantidad = f.Cantidad,
            Stock = f.Producto.Stock,
            Activo = f.Producto.Activo
        }).ToList();

        var subtotal = items.Sum(i => i.Subtotal);

        return new CarritoViewModel
        {
            Items = items,
            Subtotal = subtotal,
            CostoEnvio = _opciones.CalcularEnvio(subtotal),
            EnvioGratisDesde = _opciones.EnvioGratisDesde
        };
    }

    public Task<int> ContarAsync(string idUsuario) =>
        _db.CarritoItems.Where(c => c.IdUsuario == idUsuario).SumAsync(c => c.Cantidad);

    public async Task<ResultadoCarrito> AgregarAsync(string idUsuario, int idProducto, int cantidad)
    {
        cantidad = Math.Max(1, cantidad);

        var producto = await _db.Productos.AsNoTracking()
            .Where(p => p.IdProducto == idProducto && p.Activo)
            .Select(p => new { p.Stock })
            .FirstOrDefaultAsync();

        if (producto is null)
            return new(false, "El producto no está disponible.");

        var limite = Math.Min(producto.Stock, MaximoPorProducto);
        if (limite <= 0)
            return new(false, "El producto no tiene stock.");

        var item = await _db.CarritoItems
            .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario && c.IdProducto == idProducto);

        var actual = item?.Cantidad ?? 0;
        if (actual >= limite)
            return new(false, $"Ya tenés en el carrito todas las unidades disponibles ({limite}).");

        var nueva = Math.Min(actual + cantidad, limite);

        if (item is null)
            _db.CarritoItems.Add(new CarritoItem { IdUsuario = idUsuario, IdProducto = idProducto, Cantidad = nueva });
        else
            item.Cantidad = nueva;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos clics casi simultáneos chocan con el índice único (usuario, producto)
            return new(false, "No se pudo agregar el producto. Intentá de nuevo.");
        }

        var agregadas = nueva - actual;
        return agregadas < cantidad
            ? new(true, $"Se agregaron {agregadas} (es el stock disponible).")
            : new(true, "Producto agregado al carrito.");
    }

    public async Task<ResultadoCarrito> ActualizarCantidadAsync(string idUsuario, int idCarritoItem, int cantidad)
    {
        var item = await _db.CarritoItems
            .Include(c => c.Producto)
            .FirstOrDefaultAsync(c => c.IdCarritoItem == idCarritoItem && c.IdUsuario == idUsuario);

        if (item is null)
            return new(false, "El producto ya no está en tu carrito.");

        var limite = Math.Min(item.Producto.Stock, MaximoPorProducto);
        if (!item.Producto.Activo || limite <= 0)
            return new(false, "Este producto no está disponible. Quitalo del carrito.");

        cantidad = Math.Max(1, cantidad);
        string? aviso = null;
        if (cantidad > limite)
        {
            cantidad = limite;
            aviso = $"Solo hay {limite} unidades disponibles de \"{item.Producto.Nombre}\".";
        }

        item.Cantidad = cantidad;
        await _db.SaveChangesAsync();
        return new(true, aviso);
    }

    public async Task<ResultadoCarrito> QuitarAsync(string idUsuario, int idCarritoItem)
    {
        var item = await _db.CarritoItems
            .FirstOrDefaultAsync(c => c.IdCarritoItem == idCarritoItem && c.IdUsuario == idUsuario);

        if (item is not null)
        {
            _db.CarritoItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        return new(true);
    }
}
