namespace HeladeriaJackFrost.ViewModels;

public class ReporteVentasViewModel
{
    // Filtros seleccionados por el usuario
    public DateTime FechaInicio { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime FechaFin { get; set; } = DateTime.Today;
    public string Granularidad { get; set; } = "Diario"; // "Diario" o "Mensual"

    // Metadatos del Encabezado del Reporte (Header)
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    public string UsuarioGenerador { get; set; } = string.Empty;

    // Bandas de Grupo (Día o Mes/Año)
    public List<GrupoReporteVentaViewModel> Grupos { get; set; } = new();

    // Acumulados del Pie del Reporte (Report Footer)
    public int TotalOperaciones => Grupos.Sum(g => g.CantidadOperaciones);
    public decimal GranTotalBruto => Grupos.Sum(g => g.TotalBruto);
    public decimal GranTotalDescuentos => Grupos.Sum(g => g.TotalDescuentos);
    public decimal GranTotalCobrado => Grupos.Sum(g => g.TotalCobrado);

    public bool TieneDatos => Grupos.Any(g => g.Filas.Any());
}

public class GrupoReporteVentaViewModel
{
    public string ClaveGrupo { get; set; } = string.Empty;     // Ej: "2026-01-15" o "2026-01" (para ordenar)
    public string EtiquetaGrupo { get; set; } = string.Empty;  // Ej: "15/01/2026" o "ENERO / 2026"

    // Filas del Detalle (Detail)
    public List<FilaReporteVentaViewModel> Filas { get; set; } = new();

    // Acumulados del Pie de Grupo (Group Footer)
    public int CantidadOperaciones => Filas.Count;
    public decimal TotalBruto => Filas.Sum(f => f.SubtotalBruto);
    public decimal TotalDescuentos => Filas.Sum(f => f.MontoDescuento);
    public decimal TotalCobrado => Filas.Sum(f => f.TotalCobrado);
}

public class FilaReporteVentaViewModel
{
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public decimal SubtotalBruto { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal TotalCobrado { get; set; }
}