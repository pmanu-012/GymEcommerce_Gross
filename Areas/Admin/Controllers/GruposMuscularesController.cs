using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class GruposMuscularesController : CatalogoAdminController
{
    private readonly ApplicationDbContext _db;

    public GruposMuscularesController(ApplicationDbContext db) => _db = db;

    protected override string Titulo => "Grupos musculares";
    protected override string Singular => "grupo muscular";
    protected override string TextoNuevo => "Nuevo grupo muscular";

    protected override Task<List<CatalogoFilaViewModel>> ListarAsync() =>
        _db.GruposMusculares.AsNoTracking()
            .OrderBy(g => g.Nombre)
            .Select(g => new CatalogoFilaViewModel
            {
                Id = g.IdGrupoMuscular,
                Nombre = g.Nombre,
                CantidadAsociados = g.ProductoGruposMusculares.Count
            })
            .ToListAsync();

    protected override async Task<CatalogoFormViewModel?> ObtenerAsync(int id) =>
        await _db.GruposMusculares.AsNoTracking()
            .Where(g => g.IdGrupoMuscular == id)
            .Select(g => new CatalogoFormViewModel { Id = g.IdGrupoMuscular, Nombre = g.Nombre })
            .FirstOrDefaultAsync();

    protected override async Task ValidarAsync(CatalogoFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre))
            return;

        var existe = await _db.GruposMusculares.AnyAsync(g => g.Nombre == vm.Nombre && g.IdGrupoMuscular != vm.Id);
        if (existe)
            ModelState.AddModelError(nameof(vm.Nombre), "Ya existe un grupo muscular con ese nombre.");
    }

    protected override async Task<bool> GuardarAsync(CatalogoFormViewModel vm)
    {
        if (vm.Id == 0)
        {
            _db.GruposMusculares.Add(new GrupoMuscular { Nombre = vm.Nombre });
        }
        else
        {
            var grupo = await _db.GruposMusculares.FindAsync(vm.Id);
            if (grupo is null)
                return false;
            grupo.Nombre = vm.Nombre;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    protected override async Task<string?> EliminarAsync(int id)
    {
        var grupo = await _db.GruposMusculares.FindAsync(id);
        if (grupo is null)
            return "El grupo muscular ya no existe.";

        var productos = await _db.ProductoGruposMusculares.CountAsync(x => x.IdGrupoMuscular == id);
        if (productos > 0)
            return $"No se puede eliminar: tiene {Cantidad(productos, "producto asociado", "productos asociados")}.";

        _db.GruposMusculares.Remove(grupo);
        await _db.SaveChangesAsync();
        return null;
    }
}
