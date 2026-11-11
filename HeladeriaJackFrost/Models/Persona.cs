using System;
using System.Collections.Generic;

namespace HeladeriaJackFrost.Models;

public partial class Persona
{
    public int IdPersona { get; set; }

    public string Nombre { get; set; } = null!;

    public string ApellidoPat { get; set; } = null!;

    public string? ApellidoMat { get; set; }

    public string? Telefono { get; set; }

    public virtual Cliente? Cliente { get; set; }

    public virtual Personal? Personal { get; set; }
}
