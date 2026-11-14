namespace GymEcommerce.ViewModels;

public class PortadaViewModel
{
    public List<CategoriaMenuItem> Categorias { get; set; } = new();
    public List<ProductoCardViewModel> Ofertas { get; set; } = new();
}
