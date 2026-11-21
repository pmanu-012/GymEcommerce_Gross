# GymStore

E-commerce de equipamiento deportivo y de gimnasio desarrollado como **Trabajo Final Integrador** de la materia Programación Web.

**Sitio publicado:** _[agregar la URL del despliegue]_
**Autor:** _[nombre y apellido]_

> Proyecto académico: el pago es simulado y no se procesan transacciones reales.

---

## Funcionalidades

### Visitantes
- Catálogo con filtros por categoría y subcategoría, búsqueda, orden por precio o descuento, y paginación.
- Página de ofertas y detalle de producto con disciplinas, grupos musculares y stock.
- Reseñas de productos y opiniones sobre la tienda.
- **Asistente virtual de compras con IA** (Gemini) que recomienda productos del catálogo según presupuesto, objetivo o gustos.
- Registro con confirmación de cuenta por email.

### Clientes
- Carrito con actualización sin recargar (AJAX) y validación de stock.
- Checkout con dirección de envío, costo de envío y envío gratis desde un monto.
- "Mis pedidos" con seguimiento de estado, cancelación de pedidos pendientes y comprobante en PDF.
- Avisos por email: pedido recibido y cada cambio de estado.
- Reseñas de productos con compra verificada (solo quien recibió el producto) y opinión sobre la tienda.
- Perfil: datos personales, cambio de contraseña y recuperación de contraseña por email.

### Administrador
- **Resumen** con indicadores (ventas, pedidos, ticket promedio), gráfico de ventas, productos más vendidos, ventas por categoría, pedidos pendientes y stock bajo.
- ABM de productos con imágenes, oferta (porcentaje de descuento) y baja lógica.
- ABM de categorías, subcategorías, disciplinas y grupos musculares en ventanas modales.
- Gestión de pedidos con transiciones de estado controladas (al cancelar se devuelve el stock).
- **Ingresos de mercadería**: el stock solo sube con ingresos y baja con ventas; los ingresos se pueden anular.
- Gestión de clientes: consulta, edición, baja y reactivación (la baja cierra la sesión abierta).
- Moderación de reseñas.
- **Auditoría** de todas las operaciones por usuario, con filtros y exportación a PDF.

---

## Tecnologías

| Área | Tecnología |
|---|---|
| Backend | ASP.NET Core MVC (.NET 10), C# |
| Datos | SQL Server, Entity Framework Core (Code First con migraciones) |
| Seguridad | ASP.NET Core Identity con roles `Cliente` y `Administrador` |
| Frontend | Razor, Bootstrap 5, Bootstrap Icons, jQuery Validation Unobtrusive, SweetAlert2, Chart.js |
| Emails | MailKit (SMTP de Gmail con contraseña de aplicación) |
| PDF | QuestPDF (licencia Community) |
| IA | API de Google Gemini |
| Hosting | MonsterASP.NET |

---

## Requisitos del trabajo y dónde se cumplen

| Requisito | Implementación |
|---|---|
| ASP.NET Core MVC | `Controllers/`, `Views/` y área de administración en `Areas/Admin/` |
| Identity con roles | `Models/ApplicationUser.cs`, roles creados por `Data/DbInitializer.cs`, controladores protegidos con `[Authorize(Roles = ...)]` |
| Entity Framework Core | `Data/ApplicationDbContext.cs` y `Migrations/`; las migraciones se aplican al iniciar la aplicación |
| Validaciones cliente y servidor | DataAnnotations en `ViewModels/` + jQuery Validation Unobtrusive; `wwwroot/js/validacion-decimal.js` adapta los números al formato argentino |
| Modales, AJAX y SweetAlert2 | ABM de catálogos en modales, carrito, reseñas, cambios de estado y confirmaciones de baja |
| Manejo de imágenes | `Services/ImagenService.cs`: valida tamaño, extensión y el contenido real del archivo; guarda con nombre generado |
| Bootstrap 5 responsive | Todas las vistas; identidad visual en `wwwroot/css/site.css` |
| Auditoría por usuario | `Data/ApplicationDbContext.Auditoria.cs` registra automáticamente cada cambio (antes y después); consulta en `Admin/Auditoria` |
| Despliegue | MonsterASP.NET |
| Módulo de IA | Asistente virtual de compras: `Services/AsistenteService.cs` |

---

## Modelo de datos

Base normalizada (3FN). Tablas del dominio:

`Categorias`, `Subcategorias`, `Productos`, `Disciplinas`, `ProductoDisciplinas`, `GruposMusculares`, `ProductoGruposMusculares`, `CarritoItems`, `Pedidos`, `DetallesPedido`, `IngresosMercaderia`, `DetallesIngreso`, `ResenasProducto`, `ResenasPlataforma` y `Auditorias`.

Usuarios y roles se manejan con las tablas de Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.). `DataProtectionKeys` es una tabla técnica de ASP.NET.

Decisiones de diseño:
- La categoría de un producto se deduce de su subcategoría (no se guarda dos veces).
- El precio con descuento se calcula a partir del porcentaje; no se almacena.
- `Pedidos` y `DetallesPedido` guardan la dirección, el total y el precio unitario **del momento de la compra** (desnormalización intencional para conservar el historial).
- Bajas lógicas (`Activo`) en productos y usuarios.
- `RowVersion` en productos para evitar vender stock que ya no existe cuando dos compras ocurren al mismo tiempo.

El diagrama entidad-relación está en `docs/DER.png`.

---

## Estructura del proyecto

```
GymEcommerce/
├── Areas/Admin/        Panel de administración (controladores y vistas propios)
├── Controllers/        Controladores de la tienda pública
├── Data/               DbContext, auditoría, seed de roles y datos de ejemplo
├── Migrations/         Migraciones de EF Core
├── Models/             Entidades del dominio
├── Services/           Lógica de negocio (pedidos, carrito, ingresos, emails, PDF, IA...)
├── ViewComponents/     Piezas de página con datos propios (contador del carrito, reseñas)
├── ViewModels/         Modelos de las vistas y sus validaciones
├── Views/              Vistas Razor de la tienda
└── wwwroot/            CSS, JavaScript e imágenes
```

---

## Cómo ejecutarlo localmente

### Requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, incluido con Visual Studio, alcanza)
- Visual Studio 2026 o VS Code

### Pasos

1. Clonar el repositorio.
2. Configurar los secretos (no se guardan en el repositorio). Desde la carpeta del proyecto:
   ```bash
   dotnet user-secrets set "SeedAdmin:Email" "admin@ejemplo.com"
   dotnet user-secrets set "SeedAdmin:Password" "Admin12345"
   dotnet user-secrets set "Email:Usuario" "tucuenta@gmail.com"
   dotnet user-secrets set "Email:Contrasena" "contraseña de aplicación"
   dotnet user-secrets set "Gemini:ApiKey" "tu-clave"
   ```
   - Sin `Email:*`, los emails se guardan como archivos `.html` en `emails-dev/` (se abren con el navegador y los enlaces funcionan).
   - Sin `Gemini:ApiKey`, el asistente muestra que no está disponible.
3. Ejecutar:
   ```bash
   dotnet run
   ```
   Al iniciar se crea la base, se aplican las migraciones, se crean los roles y el administrador, y en desarrollo se cargan productos de ejemplo.

### Configuración

| Clave | Descripción |
|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server |
| `SeedAdmin:Email` / `SeedAdmin:Password` | Administrador que se crea al iniciar (si no existe) |
| `Email:Usuario` / `Email:Contrasena` | Cuenta de Gmail y contraseña de aplicación para enviar emails |
| `Gemini:ApiKey` / `Gemini:Modelo` | Clave y modelo de la API de Gemini |
| `Tienda:CostoEnvio` / `Tienda:EnvioGratisDesde` | Costo de envío y monto a partir del cual es gratis |
| `Demo:SembrarDatos` | `true` para cargar los productos de ejemplo fuera de desarrollo |

---

## Despliegue

Publicado en MonsterASP.NET con Web Deploy desde Visual Studio. La configuración de producción va en `appsettings.Production.json`, que **no** se sube al repositorio (hay un modelo en `appsettings.Production.example.json`).

---
