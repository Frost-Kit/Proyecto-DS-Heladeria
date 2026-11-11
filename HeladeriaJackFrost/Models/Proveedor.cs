namespace HeladeriaJackFrost.Models;

public class Proveedor
{
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
}