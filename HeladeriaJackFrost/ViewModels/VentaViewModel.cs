using System.ComponentModel.DataAnnotations;

namespace HeladeriaJackFrost.ViewModels;

public class VentaViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un cliente.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un cliente válido.")]
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el cajero que atiende.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un cajero válido.")]
    public int IdPersonal { get; set; }

    // Escenario 2: Validación de producto no seleccionado
    [Required(ErrorMessage = "Debe seleccionar al menos un producto para registrar la venta.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un producto válido.")]
    public int IdProducto { get; set; }

    // Escenarios 2 y 3: Bloqueo de cantidad en cero, vacía o negativa
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 1000, ErrorMessage = "La cantidad debe ser un número válido mayor a cero.")]
    public int Cantidad { get; set; }

    [Range(0, 10000, ErrorMessage = "El descuento no puede ser negativo.")]
    public decimal Descuento { get; set; } = 0;
    public int EstadoInicial { get; set; } = (int)HeladeriaJackFrost.Models.EstadoVenta.Completada;
}