using GymEcommerce.Data;
using GymEcommerce.Models;
using GymEcommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using QuestPDF.Infrastructure;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Licencia gratuita de QuestPDF (individuos, proyectos académicos y empresas chicas)
QuestPDF.Settings.License = LicenseType.Community;

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
        options.SignIn.RequireConfirmedEmail = true;
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
builder.Services.Configure<OpcionesEmail>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<ResenaService>();
builder.Services.AddScoped<EstadisticasService>();
builder.Services.Configure<OpcionesGemini>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<AsistenteService>(c => c.Timeout = TimeSpan.FromSeconds(30));

// Límite de consultas al asistente: 10 por minuto por usuario (o por IP si no inició sesión)
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy("asistente", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    
    o.OnRejected = async (contexto, ct) =>
    {
        contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        contexto.HttpContext.Response.ContentType = "application/json";
        await contexto.HttpContext.Response.WriteAsync(
            "{\"ok\":false,\"mensaje\":\"Hiciste muchas consultas seguidas. Esperá un minuto y probá de nuevo.\"}", ct);
    };
});

// Cada cuánto se revisa que la sesión siga siendo válida (por defecto, 30 minutos).
// Con 1 minuto, un cliente dado de baja pierde la sesión casi enseguida.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

// Las claves que firman las cookies y los enlaces de los emails se guardan en la base:
// así siguen valiendo aunque el servidor se reinicie o se vuelva a publicar
builder.Services.AddDataProtection()
    .SetApplicationName("GymEcommerce")
    .PersistKeysToDbContext<ApplicationDbContext>();
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

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Demo:SembrarDatos"))
{
    using var scope = app.Services.CreateScope();
    await DemoDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}
// Páginas propias para 404 y otros códigos sin contenido (también en desarrollo, para poder verlas)
app.UseStatusCodePagesWithReExecute("/Error/{0}");

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
app.UseRateLimiter();    // después de la autenticación, para limitar por usuario

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
