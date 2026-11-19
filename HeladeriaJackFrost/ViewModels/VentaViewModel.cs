using System.ComponentModel.DataAnnotations;
using HeladeriaJackFrost.Models;

namespace HeladeriaJackFrost.ViewModels;

public class DetalleItemViewModel
{
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }

    // Propiedad calculada automáticamente por línea
    public decimal Subtotal => Math.Max(0, (PrecioUnitario * Cantidad) - Descuento);
}

public class VentaViewModel
{
    [Required(ErrorMessage = "Por favor, seleccione el cliente que realiza la compra.")]
    [Range(1, int.MaxValue, ErrorMessage = "Por favor, seleccione un cliente válido de la lista.")]
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "Por favor, seleccione el cajero que atiende el turno.")]
    [Range(1, int.MaxValue, ErrorMessage = "Por favor, seleccione un cajero válido.")]
    public int IdPersonal { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public int EstadoInicial { get; set; } = (int)EstadoVenta.Completada;

    // Lista dinámica del detalle de venta (Maestro-Detalle)
    public List<DetalleItemViewModel> Detalles { get; set; } = new();

    // Cálculo automático del Total General usando LINQ
    public decimal Total => Detalles.Sum(d => d.Subtotal);
    
    //Cambio para el rango de descuento
    [Range(0, 20, ErrorMessage = "El descuento permitido debe estar entre 0% y un máximo de 20%.")]
    public decimal PorcentajeDescuento { get; set; } = 0;
}