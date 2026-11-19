using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HeladeriaJackFrost.Models;

public partial class Venta
{
    public int IdVenta { get; set; }

    public int IdCliente { get; set; }

    public int IdPersonal { get; set; }

    public DateTime Fecha { get; set; }

    [Column("subtotal_bruto")]
    public decimal SubtotalBruto { get; set; }

    [Column("porcentaje_descuento")]
    public decimal PorcentajeDescuento { get; private set; }

    [Column("monto_descuento")]
    public decimal MontoDescuento { get; set; }

    public decimal Total { get; set; }

    public int Estado { get; set; }

    public virtual ICollection<Detalle> Detalle { get; set; } = new List<Detalle>();

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual Personal IdPersonalNavigation { get; set; } = null!;

    // Regla de negocio CR-01 encapsulada en el dominio (0% a 20%)
    public void AplicarDescuento(decimal porcentaje)
    {
        if (porcentaje < 0 || porcentaje > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(porcentaje),
                "El descuento permitido debe estar entre 0% y un máximo de 20%."
            );
        }

        // Calcula el bruto sumando los detalles (Cantidad * PrecioUnitario)
        SubtotalBruto = Detalle.Sum(d => d.Cantidad * d.PrecioUnitario);
        PorcentajeDescuento = porcentaje;
        MontoDescuento = Math.Round(SubtotalBruto * (PorcentajeDescuento / 100m), 2);
        Total = SubtotalBruto - MontoDescuento;
    }
}