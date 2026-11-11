using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal Precio { get; set; }

    public int Stock { get; set; }

    public virtual ICollection<Compra> Compra { get; set; } = new List<Compra>();

    public virtual ICollection<Detalle> Detalle { get; set; } = new List<Detalle>();

    public virtual Categoria IdCategoriaNavigation { get; set; } = null!;
}
