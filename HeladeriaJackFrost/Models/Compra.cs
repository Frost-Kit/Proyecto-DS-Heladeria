using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Compra
{
    public int IdCompra { get; set; }

    public int IdProveedor { get; set; }

    public int IdProducto { get; set; }

    public DateTime Fecha { get; set; }

    public int Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal Total { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Proveedor IdProveedorNavigation { get; set; } = null!;
}
