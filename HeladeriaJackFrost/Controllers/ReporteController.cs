using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeladeriaJackFrost.Controllers;

[Authorize(Roles = "Administrador")]
public class ReporteController : Controller
{
    private readonly ReporteDAO _reporteDao;
    private readonly ILogger<ReporteController> _logger;

    public ReporteController(ReporteDAO reporteDao, ILogger<ReporteController> logger)
    {
        _reporteDao = reporteDao;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Ventas(DateTime? fechaInicio, DateTime? fechaFin, string granularidad = "Diario")
    {
        var inicio = (fechaInicio ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var fin = (fechaFin ?? DateTime.Today).Date;
        var modo = string.Equals(granularidad, "Mensual", StringComparison.OrdinalIgnoreCase)
            ? "Mensual"
            : "Diario";

        var viewModel = new ReporteVentasViewModel
        {
            FechaInicio = inicio,
            FechaFin = fin,
            Granularidad = modo,
            FechaEmision = DateTime.Now,
            UsuarioGenerador = User.Identity?.Name ?? "Administrador"
        };

        if (inicio > fin)
        {
            TempData["MensajeError"] = "La fecha de inicio no puede ser mayor que la fecha final. Ajusta el rango e intenta nuevamente.";
            return View(viewModel);
        }

        try
        {
            viewModel.Grupos = await _reporteDao.ObtenerReporteVentasAsync(inicio, fin, modo);
        }
        catch (InvalidOperationException ex)
        {
            TempData["MensajeError"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al generar el reporte de ventas ({Inicio} - {Fin}, {Modo}).", inicio, fin, modo);
            TempData["MensajeError"] = "Ocurrió un inconveniente inesperado al generar el reporte. Por favor, intenta nuevamente.";
        }

        return View(viewModel);
    }
}