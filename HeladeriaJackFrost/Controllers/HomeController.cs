using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

[Authorize(Roles = "Administrador")]
public class HomeController : Controller
{
    private readonly HeladeriaDbContext _context;

    public HomeController(HeladeriaDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> DashboardDatos(CancellationToken cancellationToken)
    {
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);

        // 1. Inventario ordenado de menor a mayor stock (prioridad operativa)
        var productos = await _context.Producto
            .AsNoTracking()
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Nombre)
            .Select(p => new DashboardProductoViewModel
            {
                Nombre = p.Nombre,
                Categoria = p.IdCategoriaNavigation.Nombre,
                Stock = p.Stock
            })
            .ToListAsync(cancellationToken);

        // 2. Solo ventas del día actual
        var ventasHoy = await _context.Venta
            .AsNoTracking()
            .Where(v => v.Fecha >= hoy && v.Fecha < manana)
            .Select(v => new
            {
                v.Total,
                v.Estado
            })
            .ToListAsync(cancellationToken);

        // Total monetario excluyendo anuladas y reembolsadas
        var ventasValidas = ventasHoy
            .Where(v => v.Estado != (int)EstadoVenta.Anulada && v.Estado != (int)EstadoVenta.Reembolsada)
            .ToList();

        var estadosAgrupados = ventasHoy
            .GroupBy(v => v.Estado)
            .Select(g => new DashboardEstadoVentaViewModel
            {
                Estado = g.Key switch
                {
                    (int)EstadoVenta.Completada => "Completada",
                    (int)EstadoVenta.Pendiente => "Pendiente",
                    (int)EstadoVenta.EnPreparacion => "En Preparación",
                    (int)EstadoVenta.Anulada => "Anulada",
                    (int)EstadoVenta.Reembolsada => "Reembolsada",
                    _ => $"Estado {g.Key}"
                },
                Cantidad = g.Count()
            })
            .ToList();

        // 3. Top 5 productos más vendidos hoy (Ajusta 'DetalleVenta' y 'IdProductoNavigation' según el nombre exacto en tu DbContext)
        var topProductosHoy = await _context.Detalle
            .AsNoTracking()
            .Where(d => d.IdVentaNavigation.Fecha >= hoy
                     && d.IdVentaNavigation.Fecha < manana
                     && d.IdVentaNavigation.Estado != (int)EstadoVenta.Anulada
                     && d.IdVentaNavigation.Estado != (int)EstadoVenta.Reembolsada)
            .GroupBy(d => d.IdProductoNavigation.Nombre)
            .Select(g => new DashboardTopProductoViewModel
            {
                Nombre = g.Key,
                CantidadVendida = g.Sum(x => x.Cantidad)
            })
            .OrderByDescending(x => x.CantidadVendida)
            .Take(5)
            .ToListAsync(cancellationToken);

        var resultado = new DashboardViewModel
        {
            VentasHoyTotal = ventasValidas.Sum(v => v.Total),
            VentasHoyCantidad = ventasValidas.Count,
            PedidosEnCursoHoy = ventasHoy.Count(v => v.Estado == (int)EstadoVenta.Pendiente || v.Estado == (int)EstadoVenta.EnPreparacion),
            Productos = productos,
            EstadosVentasHoy = estadosAgrupados,
            TopProductosHoy = topProductosHoy
        };

        return Json(resultado);
    }

    public IActionResult GuiaEstilos()
    {
        return View();
    }
}