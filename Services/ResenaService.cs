using System.Linq.Expressions;
using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Services;

public record ResultadoResena(bool Ok, string Mensaje);

public class ResenaService
{
    public const int TamanoPagina = 5;

    private readonly ApplicationDbContext _db;

    public ResenaService(ApplicationDbContext db) => _db = db;

    // Proyecciones reutilizables (EF las traduce a SQL)
    private static readonly Expression<Func<ResenaProducto, ResenaItemViewModel>> ItemProducto = r => new ResenaItemViewModel
    {
        IdResena = r.IdResena,
        Calificacion = r.Calificacion,
        Comentario = r.Comentario,
        Fecha = r.Fecha,
        Nombre = r.Usuario.Nombre,
        Apellido = r.Usuario.Apellido
    };

    private static readonly Expression<Func<ResenaPlataforma, ResenaItemViewModel>> ItemPlataforma = r => new ResenaItemViewModel
    {
        IdResena = r.IdResena,
        Calificacion = r.Calificacion,
        Comentario = r.Comentario,
        Fecha = r.Fecha,
        Nombre = r.Usuario.Nombre,
        Apellido = r.Usuario.Apellido
    };

    // ---------- Consultas ----------

    // Solo opina sobre un producto quien lo recibió (pedido entregado): "compra verificada"
    public Task<bool> PuedeResenarProductoAsync(string idUsuario, int idProducto) =>
        _db.DetallesPedido.AnyAsync(d =>
            d.IdProducto == idProducto &&
            d.Pedido.IdUsuario == idUsuario &&
            d.Pedido.Estado == EstadoPedido.Entregado);

    // idProducto null = opiniones sobre la tienda
    public async Task<SeccionResenasViewModel> ObtenerSeccionAsync(int? idProducto, int pagina, string? idUsuario, bool esCliente)
    {
        var vm = new SeccionResenasViewModel
        {
            IdProducto = idProducto,
            Autenticado = idUsuario is not null,
            EsCliente = esCliente
        };

        if (idProducto.HasValue)
        {
            var id = idProducto.Value;
            var query = _db.ResenasProducto.AsNoTracking().Where(r => r.IdProducto == id);
            vm.Resumen = await ResumirAsync(query.Select(r => r.Calificacion));

            if (idUsuario is not null)
            {
                // La reseña propia se muestra aparte, arriba de las demás
                vm.MiResena = await query.Where(r => r.IdUsuario == idUsuario).Select(ItemProducto).FirstOrDefaultAsync();
                query = query.Where(r => r.IdUsuario != idUsuario);
                vm.PuedeResenar = esCliente && (vm.MiResena is not null || await PuedeResenarProductoAsync(idUsuario, id));
            }

            (vm.Resenas, vm.Pagina, vm.TotalPaginas) = await PaginarAsync(
                query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.IdResena).Select(ItemProducto), pagina);
        }
        else
        {
            var query = _db.ResenasPlataforma.AsNoTracking().AsQueryable();
            vm.Resumen = await ResumirAsync(query.Select(r => r.Calificacion));

            if (idUsuario is not null)
            {
                vm.MiResena = await query.Where(r => r.IdUsuario == idUsuario).Select(ItemPlataforma).FirstOrDefaultAsync();
                query = query.Where(r => r.IdUsuario != idUsuario);
                vm.PuedeResenar = esCliente;
            }

            (vm.Resenas, vm.Pagina, vm.TotalPaginas) = await PaginarAsync(
                query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.IdResena).Select(ItemPlataforma), pagina);
        }

        vm.Form = new ResenaFormViewModel
        {
            IdProducto = idProducto,
            Calificacion = vm.MiResena?.Calificacion,
            Comentario = vm.MiResena?.Comentario,
            EsEdicion = vm.MiResena is not null
        };

        return vm;
    }

    // Para la portada: promedio de la tienda y las últimas opiniones con comentario
    public async Task<UltimasOpinionesViewModel> UltimasDePlataformaAsync(int cantidad) => new()
    {
        Resumen = await ResumirAsync(_db.ResenasPlataforma.Select(r => r.Calificacion)),
        Items = await _db.ResenasPlataforma.AsNoTracking()
            .Where(r => r.Comentario != null && r.Comentario != "")
            .OrderByDescending(r => r.Fecha)
            .Take(cantidad)
            .Select(ItemPlataforma)
            .ToListAsync()
    };

    // ---------- Guardar (alta o edición: una reseña por usuario) ----------

    public async Task<ResultadoResena> GuardarDeProductoAsync(string idUsuario, ResenaFormViewModel vm)
    {
        var idProducto = vm.IdProducto!.Value;

        if (!await _db.Productos.AnyAsync(p => p.IdProducto == idProducto && p.Activo))
            return new(false, "El producto no existe.");

        if (!await PuedeResenarProductoAsync(idUsuario, idProducto))
            return new(false, "Solo pueden opinar quienes ya recibieron este producto.");

        var resena = await _db.ResenasProducto.FirstOrDefaultAsync(r => r.IdUsuario == idUsuario && r.IdProducto == idProducto);
        var nueva = resena is null;
        if (resena is null)
        {
            resena = new ResenaProducto { IdUsuario = idUsuario, IdProducto = idProducto };
            _db.ResenasProducto.Add(resena);
        }

        resena.Calificacion = vm.Calificacion!.Value;
        resena.Comentario = Limpiar(vm.Comentario);
        resena.Fecha = DateTime.UtcNow;

        return await GuardarCambiosAsync(nueva);
    }

    public async Task<ResultadoResena> GuardarDePlataformaAsync(string idUsuario, ResenaFormViewModel vm)
    {
        var resena = await _db.ResenasPlataforma.FirstOrDefaultAsync(r => r.IdUsuario == idUsuario);
        var nueva = resena is null;
        if (resena is null)
        {
            resena = new ResenaPlataforma { IdUsuario = idUsuario };
            _db.ResenasPlataforma.Add(resena);
        }

        resena.Calificacion = vm.Calificacion!.Value;
        resena.Comentario = Limpiar(vm.Comentario);
        resena.Fecha = DateTime.UtcNow;

        return await GuardarCambiosAsync(nueva);
    }

    // ---------- Eliminar ----------

    // idUsuario null = lo elimina un administrador (moderación); con idUsuario, solo si es suya
    public async Task<ResultadoResena> EliminarAsync(string tipo, int idResena, string? idUsuario)
    {
        if (tipo == "producto")
        {
            var resena = await _db.ResenasProducto.FirstOrDefaultAsync(r =>
                r.IdResena == idResena && (idUsuario == null || r.IdUsuario == idUsuario));
            if (resena is null)
                return new(false, "La reseña no existe.");
            _db.ResenasProducto.Remove(resena);
        }
        else
        {
            var resena = await _db.ResenasPlataforma.FirstOrDefaultAsync(r =>
                r.IdResena == idResena && (idUsuario == null || r.IdUsuario == idUsuario));
            if (resena is null)
                return new(false, "La opinión no existe.");
            _db.ResenasPlataforma.Remove(resena);
        }

        await _db.SaveChangesAsync();
        return new(true, idUsuario is null ? "Reseña eliminada." : "Tu opinión se eliminó.");
    }

    // ---------- Auxiliares ----------

    private async Task<ResultadoResena> GuardarCambiosAsync(bool nueva)
    {
        try
        {
            await _db.SaveChangesAsync();
            return new(true, nueva ? "¡Gracias por tu opinión!" : "Tu opinión se actualizó.");
        }
        catch (DbUpdateException)
        {
            // Dos envíos casi simultáneos chocan con el índice único (una reseña por usuario)
            return new(false, "No se pudo guardar tu opinión. Recargá la página e intentá de nuevo.");
        }
    }

    private static async Task<ResumenCalificacion> ResumirAsync(IQueryable<int> calificaciones)
    {
        var conteo = await calificaciones
            .GroupBy(c => c)
            .Select(g => new { Estrellas = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var resumen = new ResumenCalificacion();
        foreach (var c in conteo.Where(c => c.Estrellas is >= 1 and <= 5))
            resumen.Distribucion[c.Estrellas - 1] = c.Cantidad;

        resumen.Cantidad = resumen.Distribucion.Sum();
        resumen.Promedio = resumen.Cantidad == 0
            ? 0
            : Math.Round((decimal)conteo.Sum(c => c.Estrellas * c.Cantidad) / resumen.Cantidad, 1);

        return resumen;
    }

    private static async Task<(List<T> Items, int Pagina, int TotalPaginas)> PaginarAsync<T>(IQueryable<T> query, int pagina)
    {
        var total = await query.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var items = await query.Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToListAsync();
        return (items, pagina, totalPaginas);
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
