using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using GymEcommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GymEcommerce.Controllers;

[AllowAnonymous]
public class AsistenteController : Controller
{
    private readonly AsistenteService _asistente;
    private readonly ApplicationDbContext _db;

    public AsistenteController(AsistenteService asistente, ApplicationDbContext db)
    {
        _asistente = asistente;
        _db = db;
    }

    // AJAX (JSON). El token antifalsificación viaja en el encabezado "RequestVerificationToken".
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("asistente")]
    public async Task<IActionResult> Preguntar([FromBody] PreguntaAsistenteDto pregunta, CancellationToken ct)
    {
        if (!_asistente.Configurado)
            return Json(new { ok = false, mensaje = "El asistente no está disponible en este momento." });

        if (!ModelState.IsValid)
            return Json(new { ok = false, mensaje = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage });

        var resultado = await _asistente.PreguntarAsync(pregunta.Mensaje.Trim(), pregunta.Historial, ct);
        if (!resultado.Ok)
            return Json(new { ok = false, mensaje = resultado.Error });

        // Los datos de las tarjetas salen de la base, no de la IA
        var encontrados = await _db.Productos.AsNoTracking()
            .Where(p => resultado.IdsProductos.Contains(p.IdProducto) && p.Activo)
            .ToListAsync(ct);

        var productos = resultado.IdsProductos
            .Select(id => encontrados.FirstOrDefault(p => p.IdProducto == id))
            .Where(p => p is not null)
            .Select(p => new
            {
                idProducto = p!.IdProducto,
                nombre = p.Nombre,
                precio = p.Precio,
                precioFinal = p.PrecioFinal,
                enOferta = p.EnOferta,
                imagenUrl = Url.Content(p.ImagenUrl ?? "~/img/sin-imagen.svg"),
                url = Url.Action("Detalle", "Catalogo", new { area = "", id = p.IdProducto })
            })
            .ToList();

        return Json(new
        {
            ok = true,
            respuesta = resultado.Texto,
            productos,
            puedeComprar = User.IsInRole(Roles.Cliente),
            urlAgregar = Url.Action("Agregar", "Carrito", new { area = "" })
        });
    }
}
