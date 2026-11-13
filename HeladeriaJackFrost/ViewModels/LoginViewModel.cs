using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo o nombre de usuario es obligatorio.")]
    [Display(Name = "Correo o Usuario")]
    public string EmailOrUser { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    
    // talvez borrarlo...
    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}