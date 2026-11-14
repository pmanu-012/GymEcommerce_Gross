using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class CategoriasController : CatalogoAdminController
{
    private readonly ApplicationDbContext _db;

    public CategoriasController(ApplicationDbContext db) => _db = db;

    protected override string Titulo => "Categorías";
    protected override string Singular => "categoría";
    protected override string TextoNuevo => "Nueva categoría";
    protected override string TextoAsociados => "Subcategorías";

    protected override Task<List<CatalogoFilaViewModel>> ListarAsync() =>
        _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogoFilaViewModel
            {
                Id = c.IdCategoria,
                Nombre = c.Nombre,
                CantidadAsociados = c.Subcategorias.Count
            })
            .ToListAsync();

    protected override async Task<CatalogoFormViewModel?> ObtenerAsync(int id) =>
        await _db.Categorias.AsNoTracking()
            .Where(c => c.IdCategoria == id)
            .Select(c => new CatalogoFormViewModel { Id = c.IdCategoria, Nombre = c.Nombre })
            .FirstOrDefaultAsync();

    protected override async Task ValidarAsync(CatalogoFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre))
            return;

        var existe = await _db.Categorias.AnyAsync(c => c.Nombre == vm.Nombre && c.IdCategoria != vm.Id);
        if (existe)
            ModelState.AddModelError(nameof(vm.Nombre), "Ya existe una categoría con ese nombre.");
    }

    protected override async Task<bool> GuardarAsync(CatalogoFormViewModel vm)
    {
        if (vm.Id == 0)
        {
            _db.Categorias.Add(new Categoria { Nombre = vm.Nombre });
        }
        else
        {
            var categoria = await _db.Categorias.FindAsync(vm.Id);
            if (categoria is null)
                return false;
            categoria.Nombre = vm.Nombre;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    protected override async Task<string?> EliminarAsync(int id)
    {
        var categoria = await _db.Categorias.FindAsync(id);
        if (categoria is null)
            return "La categoría ya no existe.";

        var subcategorias = await _db.Subcategorias.CountAsync(s => s.IdCategoria == id);
        if (subcategorias > 0)
            return $"No se puede eliminar: tiene {Cantidad(subcategorias, "subcategoría asociada", "subcategorías asociadas")}.";

        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync();
        return null;
    }
}
