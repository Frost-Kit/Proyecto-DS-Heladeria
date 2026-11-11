using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Personal
{
    public int IdPersonal { get; set; }

    public int IdPersona { get; set; }

    public string Cargo { get; set; } = null!;

    public int Estado { get; set; }

    public virtual Persona IdPersonaNavigation { get; set; } = null!;

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
