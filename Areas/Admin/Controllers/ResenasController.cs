using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

// Moderación: el administrador puede ver y eliminar reseñas inapropiadas
[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class ResenasController : Controller
{
    private const int TamanoPagina = 15;

    private readonly ApplicationDbContext _db;
    private readonly ResenaService _resenas;

    public ResenasController(ApplicationDbContext db, ResenaService resenas)
    {
        _db = db;
        _resenas = resenas;
    }

    public async Task<IActionResult> Index(ResenasAdminViewModel vm)
    {
        vm.Tipo = vm.Tipo == "plataforma" ? "plataforma" : "producto";

        IQueryable<ResenaAdminFilaViewModel> query;
        if (vm.Tipo == "producto")
        {
            var resenas = _db.ResenasProducto.AsNoTracking().AsQueryable();
            if (vm.Calificacion.HasValue)
                resenas = resenas.Where(r => r.Calificacion == vm.Calificacion.Value);

            query = resenas.Select(r => new ResenaAdminFilaViewModel
            {
                IdResena = r.IdResena,
                Fecha = r.Fecha,
                Calificacion = r.Calificacion,
                Comentario = r.Comentario,
                Autor = r.Usuario.Nombre + " " + r.Usuario.Apellido,
                Email = r.Usuario.Email,
                IdProducto = r.IdProducto,
                Producto = r.Producto.Nombre
            });
        }
        else
        {
            var resenas = _db.ResenasPlataforma.AsNoTracking().AsQueryable();
            if (vm.Calificacion.HasValue)
                resenas = resenas.Where(r => r.Calificacion == vm.Calificacion.Value);

            query = resenas.Select(r => new ResenaAdminFilaViewModel
            {
                IdResena = r.IdResena,
                Fecha = r.Fecha,
                Calificacion = r.Calificacion,
                Comentario = r.Comentario,
                Autor = r.Usuario.Nombre + " " + r.Usuario.Apellido,
                Email = r.Usuario.Email
            });
        }

        vm.Total = await query.CountAsync();
        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(vm.Total / (double)TamanoPagina));
        vm.Pagina = Math.Clamp(vm.Pagina, 1, vm.TotalPaginas);

        vm.Resenas = await query
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.IdResena)
            .Skip((vm.Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .ToListAsync();

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string tipo, int id)
    {
        var resultado = await _resenas.EliminarAsync(tipo, id, idUsuario: null);
        return Json(new { ok = resultado.Ok, mensaje = resultado.Mensaje });
    }
}
