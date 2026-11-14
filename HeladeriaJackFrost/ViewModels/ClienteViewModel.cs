using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels
{
    public class ClienteViewModel
    {
        public int IdCliente { get; set; }

        public int IdPersona { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Apellido Paterno")]
        public string ApellidoPat { get; set; } = null!;

        [StringLength(50)]
        [Display(Name = "Apellido Materno")]
        public string? ApellidoMat { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = null!;

        [Display(Name = "Puntos de Cliente")]
        public int PuntosDeCliente { get; set; } = 0;
    }
}