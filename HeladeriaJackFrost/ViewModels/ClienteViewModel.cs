using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels
{
    public class ClienteViewModel
    {
        public int IdCliente { get; set; }

        public int IdPersona { get; set; }

        [Required(ErrorMessage = "¡Hola! Por favor, dinos cuál es tu nombre.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo debe contener letras y espacios, por favor evita números o símbolos.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido paterno es necesario para completar tu registro.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido paterno debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido paterno solo debe contener letras y espacios.")]
        [Display(Name = "Apellido Paterno")]
        public string ApellidoPat { get; set; } = null!;

        [StringLength(50, ErrorMessage = "El apellido materno no puede superar los 50 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]*$", ErrorMessage = "El apellido materno solo debe contener letras y espacios.")]
        [Display(Name = "Apellido Materno")]
        public string? ApellidoMat { get; set; }

        [Required(ErrorMessage = "Necesitamos un correo electrónico para enviarte notificaciones y ofertas.")]
        [EmailAddress(ErrorMessage = "Ups, el formato del correo no es válido. Asegúrate de incluir el '@' y un dominio correcto (ej. usuario@correo.com).")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = null!;

        [Display(Name = "Puntos de Cliente")]
        [Range(0, 1000000, ErrorMessage = "Los puntos de fidelidad no pueden ser negativos.")]
        public int PuntosDeCliente { get; set; } = 0;

        [Required(ErrorMessage = "Por favor, selecciona el estado en el que se encuentra la cuenta del cliente.")]
        [Range(1, 5, ErrorMessage = "El estado seleccionado no es válido.")]
        [Display(Name = "Estado")]
        public int Estado { get; set; } = 1;
    }
}