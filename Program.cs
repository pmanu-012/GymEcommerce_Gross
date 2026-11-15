using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// --- Base de datos ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Identity (usuarios + roles) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccesoDenegado";
    // En pedidos AJAX no se redirige al login: se responde 401/403 y el JavaScript decide qué hacer
    options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Headers.XRequestedWith == "XMLHttpRequest") ctx.Response.StatusCode = 401;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        }
        ;
    options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Headers.XRequestedWith == "XMLHttpRequest") ctx.Response.StatusCode = 403;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        }
     ;
});

// Se usará en el hito de auditoría para saber qué usuario hizo cada cambio.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<ImagenService>();
builder.Services.AddScoped<CarritoService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<IngresoService>();
builder.Services.Configure<OpcionesTienda>(builder.Configuration.GetSection("Tienda"));

builder.Services.AddControllersWithViews(options =>
{
    // Sin esto, MVC exige las propiedades no nulas que no llegan en el formulario
    // (por ejemplo, la lista de casillas cuando no se marca ninguna).
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

var app = builder.Build();

// Migraciones + roles + admin inicial
await DbInitializer.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DemoDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// Precios y fechas con formato argentino ($ 1.250.000,00) sin importar la config del servidor
var es = new CultureInfo("es-AR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(es),
    SupportedCultures = new[] { es },
    SupportedUICultures = new[] { es }
});
app.UseRouting();

app.UseAuthentication(); // siempre antes de UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
