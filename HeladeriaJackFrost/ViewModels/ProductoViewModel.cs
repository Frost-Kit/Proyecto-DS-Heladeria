using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class ProductoViewModel
{
    public int ID_Producto { get; set; }

    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Range(typeof(decimal), "0.01", "99999999.99",
        ErrorMessage = "El precio debe estar entre 0.01 y 99,999,999.99.",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal Precio { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int IdCategoria { get; set; } = 0;
}