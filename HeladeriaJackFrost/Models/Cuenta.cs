using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Cuenta
{
    public int IdCuenta { get; set; }

    public int IdPersona { get; set; }

    public string Email { get; set; } = null!;

    public string Usuario { get; set; } = null!;

    public string? Contra { get; set; }

    public virtual Persona IdPersonaNavigation { get; set; } = null!;
}
