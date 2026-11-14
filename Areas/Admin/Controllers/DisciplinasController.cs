using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class DisciplinasController : CatalogoAdminController
{
    private readonly ApplicationDbContext _db;

    public DisciplinasController(ApplicationDbContext db) => _db = db;

    protected override string Titulo => "Disciplinas";
    protected override string Singular => "disciplina";
    protected override string TextoNuevo => "Nueva disciplina";

    protected override Task<List<CatalogoFilaViewModel>> ListarAsync() =>
        _db.Disciplinas.AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new CatalogoFilaViewModel
            {
                Id = d.IdDisciplina,
                Nombre = d.Nombre,
                CantidadAsociados = d.ProductoDisciplinas.Count
            })
            .ToListAsync();

    protected override async Task<CatalogoFormViewModel?> ObtenerAsync(int id) =>
        await _db.Disciplinas.AsNoTracking()
            .Where(d => d.IdDisciplina == id)
            .Select(d => new CatalogoFormViewModel { Id = d.IdDisciplina, Nombre = d.Nombre })
            .FirstOrDefaultAsync();

    protected override async Task ValidarAsync(CatalogoFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nombre))
            return;

        var existe = await _db.Disciplinas.AnyAsync(d => d.Nombre == vm.Nombre && d.IdDisciplina != vm.Id);
        if (existe)
            ModelState.AddModelError(nameof(vm.Nombre), "Ya existe una disciplina con ese nombre.");
    }

    protected override async Task<bool> GuardarAsync(CatalogoFormViewModel vm)
    {
        if (vm.Id == 0)
        {
            _db.Disciplinas.Add(new Disciplina { Nombre = vm.Nombre });
        }
        else
        {
            var disciplina = await _db.Disciplinas.FindAsync(vm.Id);
            if (disciplina is null)
                return false;
            disciplina.Nombre = vm.Nombre;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    protected override async Task<string?> EliminarAsync(int id)
    {
        var disciplina = await _db.Disciplinas.FindAsync(id);
        if (disciplina is null)
            return "La disciplina ya no existe.";

        var productos = await _db.ProductoDisciplinas.CountAsync(x => x.IdDisciplina == id);
        if (productos > 0)
            return $"No se puede eliminar: tiene {Cantidad(productos, "producto asociado", "productos asociados")}.";

        _db.Disciplinas.Remove(disciplina);
        await _db.SaveChangesAsync();
        return null;
    }
}
