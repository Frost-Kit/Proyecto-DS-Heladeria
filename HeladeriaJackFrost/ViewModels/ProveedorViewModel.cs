using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class ProveedorViewModel
{
    public int IdProveedor { get; set; }
    
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [RegularExpression(@"^(?!\d+$)[a-zA-ZáéíóúÁÉÍÓÚñÑ0-9\s\.\,\-]+$", ErrorMessage = "El nombre de la empresa no puede ser solo números.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El nombre del contacto es obligatorio.")]
    [StringLength(100, ErrorMessage = "El contacto no puede superar los 100 caracteres.")]
    [RegularExpression(@"^(?!\d+$)(?=(?:[^a-zA-ZáéíóúÁÉÍÓÚñÑ]*[a-zA-ZáéíóúÁÉÍÓÚñÑ]){2,}).*$", ErrorMessage = "El nombre del contacto debe contener al menos 2 letras y no puede ser solo números.")]
    public string NombreContacto { get; set; } = null!;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [RegularExpression(@"^[0-9\+\-\s\(\)]+$", ErrorMessage = "El formato del teléfono no es válido.")]
    public string Telefono { get; set; } = null!;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
    [RegularExpression(@"^(?!\d+$).*$", ErrorMessage = "La dirección no puede consistir únicamente de números.")]
    public string Direccion { get; set; } = null!;
    
    [Required(ErrorMessage = "El Estado es obligatorio.")]
    [Range(1, 5, ErrorMessage = "El estado debe ser un valor entre 1 y 5.")]
    public int Estado { get; set; } = 1;
}