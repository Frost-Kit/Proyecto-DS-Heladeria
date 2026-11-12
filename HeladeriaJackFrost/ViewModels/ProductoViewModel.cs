using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class ProductoViewModel
{
    public string ID_Producto { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Precio { get; set; } = null!;

    public string Stock { get; set; } = null!;
}