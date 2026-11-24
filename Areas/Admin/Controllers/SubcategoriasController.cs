using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class SubcategoriasController : CatalogoAdminController
{
    private readonly ApplicationDbContext _db;

    public SubcategoriasController(ApplicationDbContext db) => _db = db;

    //Título principal que se muestra en la vista del listado.
    protected override string Titulo => "Subcategorías";
    // nombre en singular, usado para textos como "editar subcategoria"
    protected override string Singular => "subcategoría";
    // texto que se muestra en el botón de "nuevo" del listado
    protected override string TextoNuevo => "Nueva subcategoría";
    // Indica que el listado debe mostrar una columna adicional llamada Categoría. Su valor se obtiene de:
    // detalle = s.Categoria.Nombre, en el método ListarAsync.
    protected override string? ColumnaDetalle => "Categoría";
    // Indica que el formulario necesita seleccionar una categoría. En la clase base se utiliza para preparar el ViewModel:
    protected override bool PideCategoria => true;

    protected override Task<List<CatalogoFilaViewModel>> ListarAsync() =>
        _db.Subcategorias.AsNoTracking()
            .OrderBy(s => s.Categoria.Nombre).ThenBy(s => s.Nombre)
            .Select(s => new CatalogoFilaViewModel
            {
                Id = s.IdSubcategoria,
                Nombre = s.Nombre,
                Detalle = s.Categoria.Nombre,
                CantidadAsociados = s.Productos.Count
            })
            .ToListAsync();

    protected override async Task<CatalogoFormViewModel?> ObtenerAsync(int id) =>
        await _db.Subcategorias.AsNoTracking()
            .Where(s => s.IdSubcategoria == id)
            .Select(s => new CatalogoFormViewModel { Id = s.IdSubcategoria, Nombre = s.Nombre, IdCategoria = s.IdCategoria })
            .FirstOrDefaultAsync();

    protected override async Task CargarOpcionesAsync(CatalogoFormViewModel vm) =>
        vm.Categorias = await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem { Text = c.Nombre, Value = c.IdCategoria.ToString() })
            .ToListAsync();

    protected override async Task ValidarAsync(CatalogoFormViewModel vm)
    {
        if (!vm.IdCategoria.HasValue)
        {
            ModelState.AddModelError(nameof(vm.IdCategoria), "Elegí una categoría.");
            return;
        }

        if (!await _db.Categorias.AnyAsync(c => c.IdCategoria == vm.IdCategoria.Value))
        {
            ModelState.AddModelError(nameof(vm.IdCategoria), "La categoría elegida no existe.");
            return;
        }

        if (string.IsNullOrWhiteSpace(vm.Nombre))
            return;

        var existe = await _db.Subcategorias.AnyAsync(s =>
            s.IdCategoria == vm.IdCategoria.Value && s.Nombre == vm.Nombre && s.IdSubcategoria != vm.Id);
        if (existe)
            ModelState.AddModelError(nameof(vm.Nombre), "Ya existe una subcategoría con ese nombre en esa categoría.");
    }

    protected override async Task<bool> GuardarAsync(CatalogoFormViewModel vm)
    {
        if (vm.Id == 0)
        {
            _db.Subcategorias.Add(new Subcategoria { Nombre = vm.Nombre, IdCategoria = vm.IdCategoria!.Value });
        }
        else
        {
            var subcategoria = await _db.Subcategorias.FindAsync(vm.Id);
            if (subcategoria is null)
                return false;
            subcategoria.Nombre = vm.Nombre;
            subcategoria.IdCategoria = vm.IdCategoria!.Value;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    protected override async Task<string?> EliminarAsync(int id)
    {
        var subcategoria = await _db.Subcategorias.FindAsync(id);
        if (subcategoria is null)
            return "La subcategoría ya no existe.";

        var productos = await _db.Productos.CountAsync(p => p.IdSubcategoria == id);
        if (productos > 0)
            return $"No se puede eliminar: tiene {Cantidad(productos, "producto asociado", "productos asociados")}.";

        _db.Subcategorias.Remove(subcategoria);
        await _db.SaveChangesAsync();
        return null;
    }
}
