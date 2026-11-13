using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class ProveedorViewModel
{
    public int IdProveedor { get; set; }
    
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "El nombre debe tener entre 8 y 100 caracteres.")]
    [RegularExpression(@"^(?!\d+$)[a-zA-ZáéíóúÁÉÍÓÚñÑ0-9\s\.\,\-]+$", ErrorMessage = "El nombre de la empresa no puede ser solo números.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El nombre del contacto es obligatorio.")]
    [StringLength(100, ErrorMessage = "El contacto no puede superar los 100 caracteres.")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre del contacto solo debe contener letras y espacios, sin números ni caracteres especiales.")]
    public string NombreContacto { get; set; } = null!;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres.")]
    [RegularExpression(@"^[\+\-\s\(\)]*[0-9](?:[\+\-\s\(\)]*[0-9]){7,}[0-9\+\-\s\(\)]*$", ErrorMessage = "El formato del teléfono no es válido y debe contener al menos 8 dígitos.")]
    public string Telefono { get; set; } = null!;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(150, MinimumLength = 10, ErrorMessage = "La dirección debe tener entre 10 y 150 caracteres.")]
    [RegularExpression(@"^(?!\d+$).*$", ErrorMessage = "La dirección no puede consistir únicamente de números.")]
    public string Direccion { get; set; } = null!;
    
    [Required(ErrorMessage = "El Estado es obligatorio.")]
    [Range(1, 5, ErrorMessage = "El estado debe ser un valor entre 1 y 5.")]
    public int Estado { get; set; }
}