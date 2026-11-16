using GymEcommerce.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymEcommerce.ViewComponents;

// Bloque de la portada con el promedio de la tienda y las últimas opiniones
public class UltimasOpinionesViewComponent : ViewComponent
{
    private readonly ResenaService _resenas;

    public UltimasOpinionesViewComponent(ResenaService resenas) => _resenas = resenas;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vm = await _resenas.UltimasDePlataformaAsync(3);
        return vm.Resumen.Cantidad == 0 ? Content(string.Empty) : View(vm);
    }
}
