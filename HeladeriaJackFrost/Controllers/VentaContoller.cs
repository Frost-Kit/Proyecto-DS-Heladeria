using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;

namespace HeladeriaJackFrost.Controllers;

[Authorize(Roles = "Administrador")]
public class VentaController : Controller
{
    private readonly HeladeriaDbContext _context;

    public VentaController(HeladeriaDbContext context)
    {
        _context = context;
    }

    // Listado de transacciones realizadas
    public async Task<IActionResult> Index(bool soloHoy = false)
    {
        var ventas = await ObtenerVentasFiltradas(soloHoy);
        await CargarListasDesplegables();

        ViewBag.SoloHoy = soloHoy;
        ViewBag.VentaForm = new VentaViewModel
        {
            Fecha = DateTime.Now,
            EstadoInicial = (int)EstadoVenta.Completada,
            IdCliente = ViewBag.ClienteDefectoId ?? 0
        };
        ViewBag.AbrirModalVenta = false;

        return View(ventas);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        TempData["AbrirModalAlCargar"] = true;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(VentaViewModel model)
    {
        // 1. Validación de lista vacía en el detalle (Prueba 3 - Validación de Borde)
        if (model.Detalles == null || !model.Detalles.Any())
        {
            ModelState.AddModelError("Detalles", "Debe agregar al menos un producto al detalle antes de confirmar la venta.");
        }
        else
        {
            // Validar cada línea del detalle
            for (int i = 0; i < model.Detalles.Count; i++)
            {
                var item = model.Detalles[i];
                if (item.IdProducto <= 0)
                {
                    ModelState.AddModelError("Detalles", $"El ítem #{i + 1} no tiene un producto válido.");
                    break;
                }
                if (item.Cantidad <= 0)
                {
                    ModelState.AddModelError("Detalles", $"La cantidad del producto '{item.NombreProducto}' debe ser mayor a cero.");
                    break;
                }
                if (item.Descuento < 0)
                {
                    ModelState.AddModelError("Detalles", $"El descuento de '{item.NombreProducto}' no puede ser negativo.");
                    break;
                }

                var prodDb = await _context.Producto.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto);

                if (prodDb == null)
                {
                    ModelState.AddModelError("Detalles", $"El producto '{item.NombreProducto}' ya no existe en el catálogo.");
                    break;
                }

                decimal subtotalLinea = (prodDb.Precio * item.Cantidad) - item.Descuento;
                if (subtotalLinea <= 0)
                {
                    ModelState.AddModelError("Detalles", $"El descuento aplicado en '{prodDb.Nombre}' no puede igualar o superar su importe.");
                    break;
                }

                if (prodDb.Stock < item.Cantidad)
                {
                    ModelState.AddModelError("Detalles", $"Stock insuficiente para '{prodDb.Nombre}': disponibles {prodDb.Stock} u., solicitadas {item.Cantidad} u.");
                    break;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            var ventas = await ObtenerVentasFiltradas(false);
            await CargarListasDesplegables();
            ViewBag.SoloHoy = false;
            ViewBag.VentaForm = model;
            ViewBag.AbrirModalVenta = true; // Reabre el modal conservando los ítems cargados
            return View("Index", ventas);
        }

        // 2. Persistencia Atómica Maestro-Detalle mediante Transacción Explícita (NpgsqlTransaction)
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            decimal totalVenta = 0m;
            var detallesEntidad = new List<Detalle>();

            foreach (var item in model.Detalles!)
            {
                var producto = await _context.Producto.FindAsync(item.IdProducto);
                if (producto == null || producto.Stock < item.Cantidad)
                {
                    throw new InvalidOperationException($"Stock insuficiente o producto no disponible: {item.NombreProducto}");
                }

                decimal precioUnitario = producto.Precio;
                decimal subtotal = (precioUnitario * item.Cantidad) - item.Descuento;
                totalVenta += subtotal;

                // Descontar inventario
                producto.Stock -= item.Cantidad;

                detallesEntidad.Add(new Detalle
                {
                    IdProducto = producto.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precioUnitario,
                    Descuento = item.Descuento,
                    Subtotal = subtotal
                });
            }

            // Insertar Cabecera (El Maestro)
            var nuevaVenta = new Venta
            {
                IdCliente = model.IdCliente,
                IdPersonal = model.IdPersonal,
                Fecha = DateTime.Now,
                Total = totalVenta,
                Estado = Enum.IsDefined(typeof(EstadoVenta), model.EstadoInicial)
                    ? model.EstadoInicial
                    : (int)EstadoVenta.Completada,
                Detalle = detallesEntidad
            };

            _context.Venta.Add(nuevaVenta);
            await _context.SaveChangesAsync();

            // Confirmar toda la operación atómica en PostgreSQL
            await transaccion.CommitAsync();

            TempData["MensajeExito"] = $"¡Venta #{nuevaVenta.IdVenta} registrada con éxito por un total de Bs. {nuevaVenta.Total:N2}!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // Revertir cabecera, detalles y stock si algo falla
            await transaccion.RollbackAsync();

            ModelState.AddModelError("Detalles", $"No se pudo completar la transacción: {ex.Message}");
            var ventas = await ObtenerVentasFiltradas(false);
            await CargarListasDesplegables();
            ViewBag.SoloHoy = false;
            ViewBag.VentaForm = model;
            ViewBag.AbrirModalVenta = true;
            return View("Index", ventas);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int idVenta, int nuevoEstado)
    {
        if (!Enum.IsDefined(typeof(EstadoVenta), nuevoEstado))
        {
            return BadRequest("Estado no válido.");
        }

        var venta = await _context.Venta
            .Include(v => v.Detalle)
            .ThenInclude(d => d.IdProductoNavigation)
            .FirstOrDefaultAsync(v => v.IdVenta == idVenta);

        if (venta == null) return NotFound();

        int estadoAnterior = venta.Estado;
        if (estadoAnterior == nuevoEstado)
        {
            return RedirectToAction(nameof(Index));
        }

        if (nuevoEstado == (int)EstadoVenta.Anulada && estadoAnterior != (int)EstadoVenta.Anulada)
        {
            foreach (var det in venta.Detalle)
            {
                if (det.IdProductoNavigation != null)
                {
                    det.IdProductoNavigation.Stock += det.Cantidad;
                }
            }
        }
        else if (estadoAnterior == (int)EstadoVenta.Anulada && nuevoEstado != (int)EstadoVenta.Anulada)
        {
            foreach (var det in venta.Detalle)
            {
                if (det.IdProductoNavigation != null)
                {
                    if (det.IdProductoNavigation.Stock < det.Cantidad)
                    {
                        TempData["MensajeError"] = $"No se puede reactivar la venta #{idVenta}: stock insuficiente de {det.IdProductoNavigation.Nombre}.";
                        return RedirectToAction(nameof(Index));
                    }
                    det.IdProductoNavigation.Stock -= det.Cantidad;
                }
            }
        }

        venta.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = $"Estado de la venta #{idVenta} actualizado a {(EstadoVenta)nuevoEstado}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasDesplegables()
    {
        var productosDisponibles = await _context.Producto
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        ViewBag.Productos = productosDisponibles;

        var clientes = await _context.Cliente
            .Include(c => c.IdPersonaNavigation)
            .Select(c => new
            {
                c.IdCliente,
                NombreCompleto = c.IdPersonaNavigation != null
                    ? $"{c.IdPersonaNavigation.Nombre} {c.IdPersonaNavigation.ApellidoPat}".Trim()
                    : $"Cliente #{c.IdCliente}"
            })
            .ToListAsync();

        var consumidorFinal = clientes.FirstOrDefault(c =>
            c.NombreCompleto.Contains("Consumidor Final", StringComparison.OrdinalIgnoreCase));
        ViewBag.ClienteDefectoId = consumidorFinal?.IdCliente;

        ViewBag.Clientes = new SelectList(clientes, "IdCliente", "NombreCompleto");

        var personal = await _context.Personal
            .Include(p => p.IdPersonaNavigation)
            .Where(p => p.Estado == 1)
            .Select(p => new
            {
                p.IdPersonal,
                NombreCargo = p.IdPersonaNavigation != null
                    ? $"{p.IdPersonaNavigation.Nombre} ({p.Cargo})"
                    : $"Personal #{p.IdPersonal} ({p.Cargo})"
            })
            .ToListAsync();

        ViewBag.Personal = new SelectList(personal, "IdPersonal", "NombreCargo");
    }

    private async Task<List<Venta>> ObtenerVentasFiltradas(bool soloHoy)
    {
        var query = _context.Venta
            .Include(v => v.IdClienteNavigation)
            .ThenInclude(c => c.IdPersonaNavigation)
            .Include(v => v.IdPersonalNavigation)
            .ThenInclude(p => p.IdPersonaNavigation)
            .Include(v => v.Detalle)
            .ThenInclude(d => d.IdProductoNavigation)
            .AsQueryable();

        if (soloHoy)
        {
            var hoy = DateTime.Today;
            query = query.Where(v => v.Fecha.Date == hoy);
        }

        return await query
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.IdVenta)
            .ToListAsync();
    }
}