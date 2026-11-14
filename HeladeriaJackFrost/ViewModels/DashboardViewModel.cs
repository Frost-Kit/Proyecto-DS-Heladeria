namespace HeladeriaJackFrost.ViewModels;

public class DashboardViewModel
{
    public decimal VentasHoyTotal { get; set; }
    public int VentasHoyCantidad { get; set; }
    public int PedidosEnCursoHoy { get; set; }
    public List<DashboardProductoViewModel> Productos { get; set; } = [];
    public List<DashboardEstadoVentaViewModel> EstadosVentasHoy { get; set; } = [];
    public List<DashboardTopProductoViewModel> TopProductosHoy { get; set; } = [];
}

public class DashboardProductoViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Stock { get; set; }
}

public class DashboardEstadoVentaViewModel
{
    public string Estado { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class DashboardTopProductoViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public int CantidadVendida { get; set; }
}