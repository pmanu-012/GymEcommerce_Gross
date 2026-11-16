using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrador)]
public class AuditoriaController : Controller
{
    private const int TamanoPagina = 20;
    private const int MaximoPdf = 2000;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditoriaController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(AuditoriaListadoViewModel vm)
    {
        var query = Filtrar(vm);

        vm.Total = await query.CountAsync();
        vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(vm.Total / (double)TamanoPagina));
        vm.Pagina = Math.Clamp(vm.Pagina, 1, vm.TotalPaginas);

        vm.Filas = await ProyectarAsync(query
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.IdAuditoria)
            .Skip((vm.Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina));

        vm.Entidades = await _db.Auditorias.Select(a => a.Entidad).Distinct().OrderBy(e => e).ToListAsync();

        return View(vm);
    }

    // Mismos filtros que el listado; se abre en una pestaña nueva
    public async Task<IActionResult> ExportarPdf(AuditoriaFiltroViewModel filtro)
    {
        var query = Filtrar(filtro);
        var total = await query.CountAsync();

        var filas = await ProyectarAsync(query
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.IdAuditoria)
            .Take(MaximoPdf));

        var admin = await _userManager.GetUserAsync(User);
        var generadoPor = admin is null ? "Administrador" : $"{admin.Nombre} {admin.Apellido}";

        var pdf = GeneradorPdf.Auditoria(filas, DescribirFiltros(filtro), generadoPor, total);

        Response.Headers.ContentDisposition = $"inline; filename=auditoria-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
        return File(pdf, "application/pdf");
    }

    // ---------- Auxiliares ----------

    private IQueryable<Auditoria> Filtrar(AuditoriaFiltroViewModel f)
    {
        var query = _db.Auditorias.AsNoTracking().AsQueryable();

        // Las fechas del filtro son días locales; se convierten a UTC para comparar con la base
        if (f.Desde.HasValue)
        {
            var desde = Fechas.AUtc(f.Desde.Value.Date);
            query = query.Where(a => a.Fecha >= desde);
        }
        if (f.Hasta.HasValue)
        {
            var hasta = Fechas.AUtc(f.Hasta.Value.Date.AddDays(1));
            query = query.Where(a => a.Fecha < hasta);
        }

        if (!string.IsNullOrWhiteSpace(f.Entidad))
            query = query.Where(a => a.Entidad == f.Entidad);

        if (!string.IsNullOrWhiteSpace(f.Accion))
            query = query.Where(a => a.Accion == f.Accion);

        var usuario = f.Usuario?.Trim();
        if (!string.IsNullOrEmpty(usuario))
        {
            query = usuario.Equals("sistema", StringComparison.OrdinalIgnoreCase)
                ? query.Where(a => a.IdUsuario == null)
                : query.Where(a => a.Usuario != null &&
                                   ((a.Usuario.Nombre + " " + a.Usuario.Apellido).Contains(usuario) ||
                                    (a.Usuario.Email != null && a.Usuario.Email.Contains(usuario))));
        }

        return query;
    }

    private static async Task<List<AuditoriaFilaViewModel>> ProyectarAsync(IQueryable<Auditoria> query)
    {
        var datos = await query
            .Select(a => new
            {
                a.IdAuditoria,
                a.Fecha,
                Usuario = a.Usuario == null ? null : a.Usuario.Nombre + " " + a.Usuario.Apellido,
                Email = a.Usuario == null ? null : a.Usuario.Email,
                a.Accion,
                a.Entidad,
                a.IdEntidad,
                a.ValorAnterior,
                a.ValorNuevo
            })
            .ToListAsync();

        // Los JSON se interpretan en C#, después de traer los datos
        return datos.Select(d => new AuditoriaFilaViewModel
        {
            IdAuditoria = d.IdAuditoria,
            Fecha = d.Fecha,
            Usuario = d.Usuario,
            Email = d.Email,
            Accion = d.Accion,
            Entidad = d.Entidad,
            IdEntidad = d.IdEntidad,
            Cambios = AuditoriaFormato.Cambios(d.ValorAnterior, d.ValorNuevo)
        }).ToList();
    }

    private static string DescribirFiltros(AuditoriaFiltroViewModel f)
    {
        var partes = new List<string>();
        if (f.Desde.HasValue) partes.Add($"desde {f.Desde:dd/MM/yyyy}");
        if (f.Hasta.HasValue) partes.Add($"hasta {f.Hasta:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(f.Usuario)) partes.Add($"usuario \"{f.Usuario.Trim()}\"");
        if (!string.IsNullOrWhiteSpace(f.Entidad)) partes.Add($"entidad {f.Entidad}");
        if (!string.IsNullOrWhiteSpace(f.Accion)) partes.Add($"acción {f.Accion}");
        return partes.Count == 0 ? "ninguno (todos los registros)" : string.Join(", ", partes);
    }
}
