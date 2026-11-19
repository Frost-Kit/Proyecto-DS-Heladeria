using System;
using Xunit;
using HeladeriaJackFrost.Models;

namespace HeladeriaJackFrost.Tests;

public class VentaDescuentoTests
{
    [Fact] // 1. Caso Válido: Descuento dentro del límite (15%)
    public void AplicarDescuento_PorcentajeValido_CalculaTotalCorrectamente()
    {
        var venta = new Venta { IdPersonal = 1 };
        venta.Detalle.Add(new Detalle { Cantidad = 2, PrecioUnitario = 100m }); // Bruto: 200

        venta.AplicarDescuento(15m); // 15% de 200 = 30

        Assert.Equal(200m, venta.SubtotalBruto);
        Assert.Equal(30m, venta.MontoDescuento);
        Assert.Equal(170m, venta.Total);
    }

    [Fact] // 2. Caso Borde: Exactamente el límite máximo (20%)
    public void AplicarDescuento_LimiteMaximo20_EsAceptado()
    {
        var venta = new Venta { IdPersonal = 1 };
        venta.Detalle.Add(new Detalle { Cantidad = 1, PrecioUnitario = 100m }); // Bruto: 100

        venta.AplicarDescuento(20m); // 20% de 100 = 20

        Assert.Equal(20m, venta.MontoDescuento);
        Assert.Equal(80m, venta.Total);
    }

    [Fact] // 3. Caso Inválido: Supera el 20% (ej. 25%)
    public void AplicarDescuento_MayorAl20PorCiento_LanzaExcepcion()
    {
        var venta = new Venta { IdPersonal = 1 };
        venta.Detalle.Add(new Detalle { Cantidad = 1, PrecioUnitario = 100m });

        Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(25m));
    }

    [Fact] // 4. Caso Inválido: Descuento negativo (-5%)
    public void AplicarDescuento_Negativo_LanzaExcepcion()
    {
        var venta = new Venta { IdPersonal = 1 };
        venta.Detalle.Add(new Detalle { Cantidad = 1, PrecioUnitario = 100m });

        Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(-5m));
    }
}
