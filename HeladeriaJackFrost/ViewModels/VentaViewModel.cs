using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class VentaViewModel
{
    [Required(ErrorMessage = "Por favor, seleccione el cliente que realiza la compra.")]
    [Range(1, int.MaxValue, ErrorMessage = "Por favor, seleccione un cliente válido de la lista.")]
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "Por favor, seleccione el cajero que atiende el turno.")]
    [Range(1, int.MaxValue, ErrorMessage = "Por favor, seleccione un cajero válido.")]
    public int IdPersonal { get; set; }

    [Required(ErrorMessage = "Por favor, seleccione al menos un producto disponible.")]
    [Range(1, int.MaxValue, ErrorMessage = "Por favor, elija un producto válido de la lista.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "Por favor, indique la cantidad de unidades (mínimo 1).")]
    [Range(1, 1000, ErrorMessage = "La cantidad debe ser un número mayor a cero (entre 1 y 1000).")]
    public int Cantidad { get; set; } = 1;

    [Required(ErrorMessage = "Por favor, ingrese el descuento (escriba 0 si no aplica).")]
    [Range(0, 10000, ErrorMessage = "El descuento no puede ser negativo; ingrese 0 o un monto válido.")]
    public decimal Descuento { get; set; } = 0;

    public int EstadoInicial { get; set; } = (int)HeladeriaJackFrost.Models.EstadoVenta.Completada;
}