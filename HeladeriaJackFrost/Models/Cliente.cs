using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public int IdPersona { get; set; }

    public int PuntosDeCliente { get; set; }

    public virtual Persona IdPersonaNavigation { get; set; } = null!;

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
