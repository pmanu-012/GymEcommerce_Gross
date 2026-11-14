using System.Linq.Expressions;
using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Services;

public class CatalogoService
{
    public const int TamanoPagina = 12;

    private readonly ApplicationDbContext _db;

    public CatalogoService(ApplicationDbContext db) => _db = db;

    // Precio final como expresión que EF traduce a SQL (la propiedad Producto.PrecioFinal solo existe en C#).
    private static readonly Expression<Func<Producto, decimal>> PrecioFinalSql = p =>
        p.PorcentajeDescuento > 0
            ? p.Precio * (1 - p.PorcentajeDescuento.Value / 100m)
            : p.Precio;

    public async Task<CatalogoViewModel> ObtenerListadoAsync(CatalogoFiltro filtro, string accion)
    {
        var query = _db.Productos.AsNoTracking().Where(p => p.Activo);

        // La subcategoría tiene prioridad sobre la categoría
        if (filtro.SubcategoriaId.HasValue)
            query = query.Where(p => p.IdSubcategoria == filtro.SubcategoriaId.Value);
        else if (filtro.CategoriaId.HasValue)
            query = query.Where(p => p.Subcategoria.IdCategoria == filtro.CategoriaId.Value);

        if (filtro.SoloOfertas)
            query = query.Where(p => p.PorcentajeDescuento > 0);

        var q = filtro.Q?.Trim();
        if (!string.IsNullOrEmpty(q))
            query = query.Where(p => p.Nombre.Contains(q) || (p.Descripcion != null && p.Descripcion.Contains(q)));

        // ThenBy por Id para que la paginación sea estable cuando hay empates
        IOrderedQueryable<Producto> ordenada = filtro.Orden switch
        {
            OrdenCatalogo.PrecioMenor => query.OrderBy(PrecioFinalSql),
            OrdenCatalogo.PrecioMayor => query.OrderByDescending(PrecioFinalSql),
            OrdenCatalogo.Descuento => query.OrderByDescending(p => p.PorcentajeDescuento ?? 0m),
            _ => query.OrderBy(p => p.Nombre)
        };
        ordenada = ordenada.ThenBy(p => p.IdProducto);

        var total = await query.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));
        var pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);
        filtro.Pagina = pagina;

        var productos = await ordenada
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .ToListAsync();

        var categorias = await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaMenuItem
            {
                Id = c.IdCategoria,
                Nombre = c.Nombre,
                Subcategorias = c.Subcategorias
                    .OrderBy(s => s.Nombre)
                    .Select(s => new SubcategoriaMenuItem { Id = s.IdSubcategoria, Nombre = s.Nombre })
                    .ToList()
            })
            .ToListAsync();

        return new CatalogoViewModel
        {
            Accion = accion,
            Filtro = filtro,
            Productos = productos.Select(ProductoCardViewModel.Desde).ToList(),
            Categorias = categorias,
            TotalProductos = total,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas
        };
    }

    public async Task<ProductoDetalleViewModel?> ObtenerDetalleAsync(int id)
    {
        var datos = await _db.Productos.AsNoTracking()
            .Where(p => p.IdProducto == id && p.Activo)
            .Select(p => new
            {
                Producto = p,
                IdCategoria = p.Subcategoria.IdCategoria,
                Categoria = p.Subcategoria.Categoria.Nombre,
                Subcategoria = p.Subcategoria.Nombre,
                Disciplinas = p.ProductoDisciplinas
                    .Select(pd => pd.Disciplina.Nombre).OrderBy(n => n).ToList(),
                Grupos = p.ProductoGruposMusculares
                    .Select(pg => pg.GrupoMuscular.Nombre).OrderBy(n => n).ToList()
            })
            .FirstOrDefaultAsync();

        if (datos is null)
            return null;

        var p = datos.Producto;
        return new ProductoDetalleViewModel
        {
            IdProducto = p.IdProducto,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            ImagenUrl = p.ImagenUrl,
            Precio = p.Precio,
            PrecioFinal = p.PrecioFinal,
            PorcentajeDescuento = p.PorcentajeDescuento,
            EnOferta = p.EnOferta,
            Stock = p.Stock,
            Uso = p.Uso,
            Material = p.Material,
            IdCategoria = datos.IdCategoria,
            Categoria = datos.Categoria,
            IdSubcategoria = p.IdSubcategoria,
            Subcategoria = datos.Subcategoria,
            Disciplinas = datos.Disciplinas,
            GruposMusculares = datos.Grupos
        };
    }
    public async Task<PortadaViewModel> ObtenerPortadaAsync()
    {
        var categorias = await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaMenuItem { Id = c.IdCategoria, Nombre = c.Nombre })
            .ToListAsync();

        var ofertas = await _db.Productos.AsNoTracking()
            .Where(p => p.Activo && p.Stock > 0 && p.PorcentajeDescuento > 0)
            .OrderByDescending(p => p.PorcentajeDescuento)
            .ThenBy(p => p.IdProducto)
            .Take(4)
            .ToListAsync();

        return new PortadaViewModel
        {
            Categorias = categorias,
            Ofertas = ofertas.Select(ProductoCardViewModel.Desde).ToList()
        };
    }
}
