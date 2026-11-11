using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Venta
{
    public int IdVenta { get; set; }

    public int IdCliente { get; set; }

    public int IdPersonal { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public int Estado { get; set; }

    public virtual ICollection<Detalle> Detalle { get; set; } = new List<Detalle>();

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual Personal IdPersonalNavigation { get; set; } = null!;
}
