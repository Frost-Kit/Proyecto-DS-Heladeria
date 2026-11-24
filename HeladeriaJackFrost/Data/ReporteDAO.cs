using System.Globalization;
using System.Net.Sockets;
using HeladeriaJackFrost.ViewModels;
using Npgsql;

namespace HeladeriaJackFrost.Data;

public class ReporteDAO
{
    private readonly string _cadenaConexion;
    private readonly ILogger<ReporteDAO> _logger;

    public ReporteDAO(IConfiguration configuration, ILogger<ReporteDAO> logger)
    {
        _cadenaConexion = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        _logger = logger;
    }

    public async Task<List<GrupoReporteVentaViewModel>> ObtenerReporteVentasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        string granularidad)
    {
        var filas = new List<FilaReporteVentaViewModel>();

        const string sql = @"
            SELECT
                v.id_venta,
                v.fecha,
                concat_ws(' ', pc.nombre, pc.apellido_pat, pc.apellido_mat) AS cliente,
                concat_ws(' ', pv.nombre, pv.apellido_pat, pv.apellido_mat) AS vendedor,
                v.subtotal_bruto,
                v.monto_descuento,
                v.total
            FROM venta v
            INNER JOIN cliente c ON v.id_cliente = c.id_cliente
            INNER JOIN persona pc ON c.id_persona = pc.id_persona
            INNER JOIN personal per ON v.id_personal = per.id_personal
            INNER JOIN persona pv ON per.id_persona = pv.id_persona
            WHERE v.fecha::date BETWEEN @fechaInicio::date AND @fechaFin::date
              AND v.estado NOT IN (4, 5)
            ORDER BY v.fecha ASC, v.id_venta ASC;";

        try
        {
            await using var conn = new NpgsqlConnection(_cadenaConexion);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("fechaInicio", fechaInicio.Date);
            cmd.Parameters.AddWithValue("fechaFin", fechaFin.Date);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                filas.Add(new FilaReporteVentaViewModel
                {
                    IdVenta = reader.GetInt32(0),
                    Fecha = reader.GetDateTime(1),
                    Cliente = reader.IsDBNull(2) ? "Sin cliente" : reader.GetString(2).Trim(),
                    Vendedor = reader.IsDBNull(3) ? "Sin vendedor" : reader.GetString(3).Trim(),
                    SubtotalBruto = reader.GetDecimal(4),
                    MontoDescuento = reader.GetDecimal(5),
                    TotalCobrado = reader.GetDecimal(6)
                });
            }
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Error de PostgreSQL al consultar el reporte de ventas ({Inicio} - {Fin}).", fechaInicio, fechaFin);
            throw new InvalidOperationException(
                "No pudimos consultar los datos para generar el reporte. Verifica que el servicio de base de datos esté activo e intenta nuevamente.",
                ex);
        }
        catch (Exception ex) when (ex is TimeoutException or SocketException || ex.InnerException is SocketException)
        {
            _logger.LogError(ex, "Error de conexión al generar el reporte de ventas ({Inicio} - {Fin}).", fechaInicio, fechaFin);
            throw new InvalidOperationException(
                "No pudimos conectarnos con la base de datos para generar el reporte. Verifica la conexión e intenta nuevamente.",
                ex);
        }

        return AgruparFilas(filas, granularidad);
    }

    private static List<GrupoReporteVentaViewModel> AgruparFilas(
        List<FilaReporteVentaViewModel> filas,
        string granularidad)
    {
        var culturaEs = new CultureInfo("es-BO");
        bool esMensual = string.Equals(granularidad, "Mensual", StringComparison.OrdinalIgnoreCase);

        return filas
            .GroupBy(f => esMensual
                ? f.Fecha.ToString("yyyy-MM")
                : f.Fecha.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var primeraFecha = g.First().Fecha;
                string etiqueta = esMensual
                    ? $"{primeraFecha.ToString("MMMM", culturaEs).ToUpper(culturaEs)} / {primeraFecha:yyyy}"
                    : primeraFecha.ToString("dd/MM/yyyy");

                return new GrupoReporteVentaViewModel
                {
                    ClaveGrupo = g.Key,
                    EtiquetaGrupo = etiqueta,
                    Filas = g.ToList()
                };
            })
            .ToList();
    }
}