using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GymEcommerce.Models;

// Tabla AspNetUsers. Email, PhoneNumber (= Telefono del DER) y PasswordHash ya vienen en IdentityUser.
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Apellido { get; set; } = string.Empty;

    // Baja lógica: el usuario no se borra, se desactiva.
    public bool Activo { get; set; } = true;

    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    public ICollection<CarritoItem> CarritoItems { get; set; } = new List<CarritoItem>();
    public ICollection<ResenaProducto> ResenasProducto { get; set; } = new List<ResenaProducto>();
    public ICollection<ResenaPlataforma> ResenasPlataforma { get; set; } = new List<ResenaPlataforma>();
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}
