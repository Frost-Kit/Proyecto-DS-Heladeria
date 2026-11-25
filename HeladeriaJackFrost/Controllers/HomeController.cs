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
    public async Task<IActionResult> DashboardDatos(DateTime? fecha, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("La fecha seleccionada no es válida.");
        }

        var dia = fecha?.Date ?? DateTime.Today;
        if (dia == DateTime.MaxValue.Date)
        {
            return BadRequest("La fecha seleccionada no es válida.");
        }

        var diaSiguiente = dia.AddDays(1);

        // 1. Inventario ordenado de menor a mayor stock (prioridad operativa)
        var productos = await _context.Producto
            .AsNoTracking()
            .Where(p => p.Estado != 5)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Nombre)
            .Select(p => new DashboardProductoViewModel
            {
                Nombre = p.Nombre,
                Categoria = p.IdCategoriaNavigation.Nombre,
                Stock = p.Stock
            })
            .ToListAsync(cancellationToken);

        // 2. Ventas del día seleccionado
        var ventasHoy = await _context.Venta
            .AsNoTracking()
            .Where(v => v.Fecha >= dia && v.Fecha < diaSiguiente)
            .Select(v => new
            {
                v.Total,
                v.Estado
            })
            .ToListAsync(cancellationToken);

        // Excluye anuladas y reembolsadas del total cobrado y de los descuentos.
        var ventasValidas = ventasHoy
            .Where(v => v.Estado != (int)EstadoVenta.Anulada && v.Estado != (int)EstadoVenta.Reembolsada)
            .ToList();

        var descuentosPorEmpleado = await _context.Detalle
            .AsNoTracking()
            .Where(d => d.IdVentaNavigation.Fecha >= dia
                     && d.IdVentaNavigation.Fecha < diaSiguiente
                     && d.IdVentaNavigation.Estado != (int)EstadoVenta.Anulada
                     && d.IdVentaNavigation.Estado != (int)EstadoVenta.Reembolsada
                     && d.Descuento > 0)
            .GroupBy(d => new
            {
                d.IdVentaNavigation.IdPersonal,
                d.IdVentaNavigation.IdPersonalNavigation.IdPersonaNavigation.Nombre,
                d.IdVentaNavigation.IdPersonalNavigation.IdPersonaNavigation.ApellidoPat,
                d.IdVentaNavigation.IdPersonalNavigation.IdPersonaNavigation.ApellidoMat
            })
            .Select(g => new
            {
                g.Key.IdPersonal,
                g.Key.Nombre,
                g.Key.ApellidoPat,
                g.Key.ApellidoMat,
                DescuentoTotal = g.Sum(d => d.Descuento)
            })
            .OrderByDescending(g => g.DescuentoTotal)
            .ThenBy(g => g.ApellidoPat)
            .ThenBy(g => g.Nombre)
            .ToListAsync(cancellationToken);

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

        // 3. Top 5 productos más vendidos en la fecha seleccionada
        var topProductosHoy = await _context.Detalle
            .AsNoTracking()
            .Where(d => d.IdVentaNavigation.Fecha >= dia
                     && d.IdVentaNavigation.Fecha < diaSiguiente
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
            DescuentoTotalDia = descuentosPorEmpleado.Sum(g => g.DescuentoTotal),
            Productos = productos,
            EstadosVentasHoy = estadosAgrupados,
            TopProductosHoy = topProductosHoy,
            DescuentosPorEmpleado = descuentosPorEmpleado
                .Select(g => new DashboardEmpleadoDescuentoViewModel
                {
                    IdPersonal = g.IdPersonal,
                    Nombre = string.Join(" ", new[] { g.Nombre, g.ApellidoPat, g.ApellidoMat }
                        .Where(nombre => !string.IsNullOrWhiteSpace(nombre))),
                    DescuentoTotal = g.DescuentoTotal
                })
                .ToList()
        };

        return Json(resultado);
    }

    public IActionResult GuiaEstilos()
    {
        return View();
    }
    
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        bool estaAutenticado = User.Identity?.IsAuthenticated == true;
        bool esCliente = User.IsInRole("Cliente");
        bool esPersonal = estaAutenticado && !esCliente; // cambiar segun los roles que tengamos

        ViewData["EsPersonal"] = esPersonal;
        return View();
    }
}