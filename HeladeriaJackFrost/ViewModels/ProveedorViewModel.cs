using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class ProveedorViewModel
{
    public int IdProveedor { get; set; }
    
    [Required(ErrorMessage = "El nombre de la empresa proveedora es obligatorio.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "El nombre de la empresa debe tener entre 5 y 100 caracteres.")]
    [RegularExpression(@"^(?!\d+$)[a-zA-ZáéíóúÁÉÍÓÚñÑ0-9\s\.\,\-]+$", ErrorMessage = "El nombre de la empresa no puede consistir únicamente de números.")]
    [Display(Name = "Nombre de la Empresa")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "Necesitamos saber el nombre de la persona de contacto.")]
    [StringLength(100, ErrorMessage = "El nombre del contacto no puede superar los 100 caracteres.")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre del contacto solo debe contener letras y espacios (sin números ni símbolos raros).")]
    [Display(Name = "Nombre del Contacto")]
    public string NombreContacto { get; set; } = null!;

    [Required(ErrorMessage = "El número de teléfono es indispensable para comunicarnos con el proveedor.")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "El teléfono debe tener entre 8 y 20 caracteres.")]
    [RegularExpression(@"^[\+\-\s\(\)]*[0-9](?:[\+\-\s\(\)]*[0-9]){7,}[0-9\+\-\s\(\)]*$", ErrorMessage = "Ingresa un número de teléfono válido que contenga al menos 8 dígitos.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = null!;

    [Required(ErrorMessage = "La dirección física del proveedor es obligatoria.")]
    [StringLength(150, MinimumLength = 10, ErrorMessage = "Por favor, detalla un poco más la dirección (entre 10 y 150 caracteres).")]
    [RegularExpression(@"^(?!\d+$).*$", ErrorMessage = "La dirección no puede estar compuesta únicamente por números.")]
    [Display(Name = "Dirección")]
    public string Direccion { get; set; } = null!;
    
    [Required(ErrorMessage = "Debes seleccionar el estado del proveedor.")]
    [Range(1, 5, ErrorMessage = "Selecciona un estado válido que se encuentre dentro del rango permitido.")]
    [Display(Name = "Estado")]
    public int Estado { get; set; }
}