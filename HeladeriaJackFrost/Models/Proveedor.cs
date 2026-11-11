using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Proveedor
{
    public int IdProveedor { get; set; }

    public string Nombre { get; set; } = null!;

    public string NombreContacto { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public int Estado { get; set; }

    public virtual ICollection<Compra> Compra { get; set; } = new List<Compra>();
}
