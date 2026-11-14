using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Por favor, ingresa tu correo electrónico o nombre de usuario para continuar.")]
    [Display(Name = "Correo o Usuario")]
    public string EmailOrUser { get; set; } = null!;

    [Required(ErrorMessage = "Ingresa tu contraseña para acceder al sistema.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = null!;
    
    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }
}