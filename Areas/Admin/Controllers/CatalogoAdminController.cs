using GymEcommerce.Models;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

// Controlador base de los cuatro catálogos simples (categorías, subcategorías, disciplinas y grupos musculares).
// Acá está todo lo que se repite: listar, abrir el formulario del modal, guardar y eliminar.
// Cada catálogo concreto solo escribe su acceso a datos (los métodos abstractos de abajo).
[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public abstract class CatalogoAdminController : Controller
{
    // ----- Lo que define cada catálogo -----
    protected abstract string Titulo { get; }       // "Categorías"
    protected abstract string Singular { get; }     // "categoría"
    protected abstract string TextoNuevo { get; }   // "Nueva categoría"
    protected virtual string TextoAsociados => "Productos";
    protected virtual string? ColumnaDetalle => null;
    protected virtual bool PideCategoria => false;

    protected abstract Task<List<CatalogoFilaViewModel>> ListarAsync();
    protected abstract Task<CatalogoFormViewModel?> ObtenerAsync(int id);
    protected abstract Task ValidarAsync(CatalogoFormViewModel vm);      // agrega errores a ModelState
    protected abstract Task<bool> GuardarAsync(CatalogoFormViewModel vm); // false si el registro ya no existe
    protected abstract Task<string?> EliminarAsync(int id);              // null = eliminado; texto = motivo del rechazo
    protected virtual Task CargarOpcionesAsync(CatalogoFormViewModel vm) => Task.CompletedTask;

    protected static string Cantidad(int n, string uno, string varios) => n == 1 ? $"1 {uno}" : $"{n} {varios}";

    // ----- Listado -----

    public async Task<IActionResult> Index()
    {
        var vm = new CatalogoListadoViewModel
        {
            Titulo = Titulo,
            TextoNuevo = TextoNuevo,
            TextoAsociados = TextoAsociados,
            ColumnaDetalle = ColumnaDetalle,
            Filas = await ListarAsync()
        };
        return View("CatalogoIndex", vm);
    }

    // ----- Formulario del modal (se pide por AJAX y devuelve solo el HTML del formulario) -----

    [HttpGet]
    public async Task<IActionResult> Form(int id = 0)
    {
        var vm = id == 0 ? new CatalogoFormViewModel() : await ObtenerAsync(id);
        if (vm is null)
            return NotFound();

        Preparar(vm);
        await CargarOpcionesAsync(vm);
        return PartialView("_CatalogoForm", vm);
    }

    // ----- Guardar (alta y edición) -----

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(CatalogoFormViewModel vm)
    {
        vm.Nombre = vm.Nombre?.Trim() ?? string.Empty;

        await ValidarAsync(vm);

        if (!ModelState.IsValid)
        {
            // 422: el cliente reemplaza el contenido del modal con el formulario y sus mensajes de error
            Preparar(vm);
            await CargarOpcionesAsync(vm);
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView("_CatalogoForm", vm);
        }

        try
        {
            if (!await GuardarAsync(vm))
                return NotFound(new { ok = false, mensaje = "El registro ya no existe." });
        }
        catch (DbUpdateException)
        {
            // Respaldo del índice único de la base, por si dos personas guardan el mismo nombre a la vez
            return Json(new { ok = false, mensaje = "No se pudo guardar. Puede que ya exista un registro con ese nombre." });
        }

        return Json(new { ok = true, mensaje = "Guardado correctamente." });
    }

    // ----- Eliminar (AJAX) -----

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var motivo = await EliminarAsync(id);
            if (motivo is not null)
                return Json(new { ok = false, mensaje = motivo });
        }
        catch (DbUpdateException)
        {
            return Json(new { ok = false, mensaje = "No se puede eliminar porque tiene datos asociados." });
        }

        return Json(new { ok = true, mensaje = "Eliminado correctamente." });
    }

    private void Preparar(CatalogoFormViewModel vm)
    {
        vm.PideCategoria = PideCategoria;
        vm.TituloModal = vm.Id == 0 ? TextoNuevo : $"Editar {Singular}";
    }
}
