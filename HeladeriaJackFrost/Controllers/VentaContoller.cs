using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Authorization;

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
        ViewBag.VentaForm = new VentaViewModel { Cantidad = 1, Descuento = 0, EstadoInicial = 1 };
        ViewBag.AbrirModalVenta = false;

        return View(ventas);
    }
    
    [HttpGet]
    public IActionResult Crear()
    {
        // Si alguien entra directo por URL a /Venta/Crear, lo llevamos a Index y abrimos el modal
        TempData["AbrirModalAlCargar"] = true;
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(VentaViewModel model)
    {
        if (model.IdProducto <= 0)
        {
            ModelState.AddModelError("IdProducto", "Por favor, seleccione al menos un producto disponible.");
        }

        if (model.Cantidad <= 0)
        {
            ModelState.AddModelError("Cantidad", "La cantidad debe ser un número válido mayor a cero.");
        }

        var producto = await _context.Producto.FindAsync(model.IdProducto);
        if (producto == null)
        {
            ModelState.AddModelError("IdProducto", "El producto seleccionado no existe o ya no está disponible.");
        }
        else if (producto.Stock < model.Cantidad)
        {
            ModelState.AddModelError("Cantidad", $"Stock insuficiente: solo quedan {producto.Stock} unidades de {producto.Nombre}.");
        }

        if (!ModelState.IsValid)
        {
            var ventas = await ObtenerVentasFiltradas(false);
            await CargarListasDesplegables();
            ViewBag.SoloHoy = false;
            ViewBag.VentaForm = model;
            ViewBag.AbrirModalVenta = true; // Reabre el modal automáticamente con los errores
            return View("Index", ventas);
        }

        decimal precioUnitario = producto!.Precio;
        decimal subtotal = (precioUnitario * model.Cantidad) - model.Descuento;

        if (subtotal < 0)
        {
            ModelState.AddModelError("Descuento", "El descuento no puede superar el monto total de la venta.");
            var ventas = await ObtenerVentasFiltradas(false);
            await CargarListasDesplegables();
            ViewBag.SoloHoy = false;
            ViewBag.VentaForm = model;
            ViewBag.AbrirModalVenta = true;
            return View("Index", ventas);
        }

        var nuevaVenta = new Venta
        {
            IdCliente = model.IdCliente,
            IdPersonal = model.IdPersonal,
            Fecha = DateTime.Now,
            Total = subtotal,
            Estado = Enum.IsDefined(typeof(EstadoVenta), model.EstadoInicial)
                ? model.EstadoInicial
                : (int)EstadoVenta.Completada
        };

        var nuevoDetalle = new Detalle
        {
            IdVentaNavigation = nuevaVenta,
            IdProducto = producto.IdProducto,
            Cantidad = model.Cantidad,
            PrecioUnitario = precioUnitario,
            Descuento = model.Descuento,
            Subtotal = subtotal
        };

        producto.Stock -= model.Cantidad;

        _context.Venta.Add(nuevaVenta);
        _context.Detalle.Add(nuevoDetalle);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = $"Venta registrada exitosamente. Total cobrado: Bs. {subtotal:F2}";
        return RedirectToAction(nameof(Index));
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

        // Regla de negocio: Si pasa a Anulada (4) desde otro estado, el stock regresa al inventario
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
        // Si estaba Anulada (4) y la reactivan a otro estado, vuelve a comprometer el stock
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
        // Solo productos con stock mayor a 0
        var productosDisponibles = await _context.Producto
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        ViewBag.Productos = productosDisponibles;

        // Lista de Clientes (mostrando el nombre desde Persona si existe relación, o su ID)
        var clientes = await _context.Cliente
            .Include(c => c.IdPersonaNavigation)
            .Select(c => new
            {
                c.IdCliente,
                NombreCompleto = c.IdPersonaNavigation != null 
                    ? c.IdPersonaNavigation.Nombre 
                    : $"Cliente #{c.IdCliente}"
            })
            .ToListAsync();

        ViewBag.Clientes = new SelectList(clientes, "IdCliente", "NombreCompleto");

        // Lista de Personal activo (Estado == 1)
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