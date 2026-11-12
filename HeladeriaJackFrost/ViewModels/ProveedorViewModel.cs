using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class ProveedorViewModel
{
    public int IdProveedor { get; set; }
    
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El nombre del contacto es obligatorio.")]
    [StringLength(100, ErrorMessage = "El contacto no puede superar los 100 caracteres.")]
    public string NombreContacto { get; set; } = null!;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string Telefono { get; set; } = null!;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
    public string Direccion { get; set; } = null!;
    
    [Required(ErrorMessage = "Estado es obligatorio.")]
    public int Estado { get; set; } = 1;
}