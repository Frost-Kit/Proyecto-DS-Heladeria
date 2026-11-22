using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Npgsql;

namespace HeladeriaJackFrost.Controllers;

[Authorize(Roles = "Administrador")]
public class VentaController : Controller
{
    private readonly HeladeriaDbContext _context;
    private readonly ILogger<VentaController> _logger;

    public VentaController(HeladeriaDbContext context, ILogger<VentaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Listado de transacciones realizadas (Protegido ante caída de BD)
    public async Task<IActionResult> Index(bool soloHoy = false)
    {
        List<Venta> ventas = new();
        InicializarListasVacias();

        try
        {
            ventas = await ObtenerVentasFiltradas(soloHoy);
            await CargarListasDesplegables();
        }
        catch (NpgsqlException exRed)
        {
            _logger.LogError(exRed, "Error de conexión Npgsql al cargar el listado de ventas (soloHoy: {SoloHoy}).", soloHoy);
            TempData["MensajeError"] = "No pudimos conectarnos a la base de datos para cargar las ventas. Verifica que el servicio esté activo e intenta nuevamente en unos momentos.";
        }
        catch (Exception ex) when (EsFalloConexion(ex))
        {
            _logger.LogError(ex, "Fallo de conexión a PostgreSQL al cargar Index de Ventas (soloHoy: {SoloHoy}).", soloHoy);
            TempData["MensajeError"] = "No pudimos conectarnos a la base de datos para cargar las ventas. Verifica que el servicio esté activo e intenta nuevamente en unos momentos.";
        }
        catch (Exception exGeneral)
        {
            _logger.LogError(exGeneral, "Error inesperado al cargar el listado de ventas.");
            TempData["MensajeError"] = "Ocurrió un inconveniente al cargar el listado de ventas. Intenta recargar la página o contacta a soporte.";
        }

        ViewBag.SoloHoy = soloHoy;
        ViewBag.VentaForm = new VentaViewModel
        {
            Fecha = DateTime.Now,
            EstadoInicial = (int)EstadoVenta.Completada,
            IdCliente = ViewBag.ClienteDefectoId ?? 0,
            PorcentajeDescuento = 0
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
        // 1. Cláusula de guarda: validar que llegó el modelo
        if (model is null)
        {
            _logger.LogWarning("Se recibió un modelo nulo al intentar registrar una venta.");
            TempData["MensajeError"] = "No se recibieron los datos de la venta. Por favor, completa el formulario e inténtalo nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        // Validación de política de descuento (0% a 20%)
        if (model.PorcentajeDescuento < 0 || model.PorcentajeDescuento > 20)
        {
            ModelState.AddModelError("PorcentajeDescuento", "El descuento permitido debe estar entre 0% y un máximo de 20%.");
        }

        // Validación de lista vacía en el detalle
        if (model.Detalles == null || !model.Detalles.Any())
        {
            ModelState.AddModelError("Detalles", "Debe agregar al menos un producto al detalle antes de confirmar la venta.");
        }

        IDbContextTransaction? transaccion = null;

        try
        {
            // Validar cada línea del detalle contra la BD (dentro del try por si se corta la conexión aquí)
            if (model.Detalles != null && model.Detalles.Any())
            {
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

                    var prodDb = await _context.Producto.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto && p.Estado != 5);

                    if (prodDb == null)
                    {
                        ModelState.AddModelError("Detalles", $"El producto '{item.NombreProducto}' ya no existe o fue retirado del catálogo.");
                        break;
                    }

                    if (prodDb.Stock < item.Cantidad)
                    {
                        ModelState.AddModelError("Detalles", $"No es posible comprar más de lo que hay en stock para '{prodDb.Nombre}' (disponible: {prodDb.Stock} u.).");
                        break;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return await VolverAIndexConModalAsync(model);
            }

            // 2. Persistencia Atómica Maestro-Detalle mediante Transacción Explícita
            transaccion = await _context.Database.BeginTransactionAsync();

            var detallesEntidad = new List<Detalle>();

            foreach (var item in model.Detalles!)
            {
                var producto = await _context.Producto.FindAsync(item.IdProducto);
                if (producto == null || producto.Estado == 5 || producto.Stock < item.Cantidad)
                {
                    throw new InvalidOperationException($"Stock insuficiente o producto no disponible: '{item.NombreProducto}'. Actualice la cantidad e intente nuevamente.");
                }

                decimal precioUnitario = producto.Precio;
                decimal subtotalLinea = precioUnitario * item.Cantidad;

                producto.Stock -= item.Cantidad;

                detallesEntidad.Add(new Detalle
                {
                    IdProducto = producto.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precioUnitario,
                    Descuento = 0,
                    Subtotal = subtotalLinea
                });
            }

            var nuevaVenta = new Venta
            {
                IdCliente = model.IdCliente,
                IdPersonal = model.IdPersonal,
                Fecha = DateTime.Now,
                Estado = Enum.IsDefined(typeof(EstadoVenta), model.EstadoInicial)
                    ? model.EstadoInicial
                    : (int)EstadoVenta.Completada,
                Detalle = detallesEntidad
            };

            // Regla de negocio CR-01 encapsulada en Models/Venta.cs
            nuevaVenta.AplicarDescuento(model.PorcentajeDescuento);

            _context.Venta.Add(nuevaVenta);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            _logger.LogInformation(
                "Venta registrada correctamente. IdVenta: {IdVenta}, IdPersonal: {IdPersonal}, Total: {Total}, Descuento: {Porcentaje}%",
                nuevaVenta.IdVenta,
                nuevaVenta.IdPersonal,
                nuevaVenta.Total,
                nuevaVenta.PorcentajeDescuento);

            TempData["MensajeExito"] = $"¡Venta #{nuevaVenta.IdVenta} registrada con éxito! Total: Bs. {nuevaVenta.Total:N2} (Descuento: {nuevaVenta.PorcentajeDescuento}%).";
            return RedirectToAction(nameof(Index));
        }
        // 3. Bloques CATCH ordenados de la excepción más específica a la más general
        catch (ArgumentOutOfRangeException exRango)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogWarning(exRango, "Intento de aplicar descuento fuera de rango ({Porcentaje}%).", model.PorcentajeDescuento);
            ModelState.AddModelError("PorcentajeDescuento", exRango.Message);
            return await VolverAIndexConModalAsync(model);
        }
        catch (DbUpdateException exDb) when (exDb.InnerException is PostgresException exPg)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(exDb, "Error de integridad PostgreSQL ({SqlState}) al registrar venta.", exPg.SqlState);

            string mensajeUx = exPg.SqlState switch
            {
                "23514" => "El descuento ingresado incumple la política permitida (0% a 20%). Por favor, corrige el porcentaje e inténtalo de nuevo.",
                "23503" => "El cliente, vendedor o producto seleccionado ya no se encuentra disponible. Recarga la página y vuelve a seleccionarlos.",
                "23505" => "Ya existe un registro con los mismos datos únicos. Verifica la información e intenta nuevamente.",
                _ => "No se pudo guardar la venta por una restricción de datos. Verifica la información ingresada e inténtalo de nuevo."
            };

            ModelState.AddModelError("Detalles", mensajeUx);
            return await VolverAIndexConModalAsync(model);
        }
        catch (NpgsqlException exRed)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(exRed, "Error de conexión Npgsql al intentar registrar una nueva venta.");
            TempData["MensajeError"] = "No fue posible conectar con la base de datos para registrar la venta. No se realizaron cobros ni cambios en el stock; verifica el servicio e intenta nuevamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (EsFalloConexion(ex))
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(ex, "Fallo de conexión a PostgreSQL durante el registro de venta.");
            TempData["MensajeError"] = "No fue posible conectar con la base de datos para registrar la venta. No se realizaron cobros ni cambios en el stock; verifica el servicio e intenta nuevamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exNegocio)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogWarning(exNegocio, "Validación de negocio al registrar venta.");
            ModelState.AddModelError("Detalles", exNegocio.Message);
            return await VolverAIndexConModalAsync(model);
        }
        catch (Exception exGeneral)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(exGeneral, "Error inesperado al registrar la venta.");
            TempData["MensajeError"] = "Ocurrió un inconveniente inesperado al procesar la venta. Tus datos y el inventario no se modificaron; por favor intenta de nuevo o contacta a soporte.";
            return RedirectToAction(nameof(Index));
        }
        finally
        {
            // 4. Bloque FINALLY: liberación garantizada de la transacción
            if (transaccion != null)
            {
                await transaccion.DisposeAsync();
            }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int idVenta, int nuevoEstado)
    {
        // Cláusulas de guarda
        if (idVenta <= 0)
        {
            _logger.LogWarning("Intento de cambiar estado con idVenta inválido: {IdVenta}", idVenta);
            TempData["MensajeError"] = "No se identificó una venta válida para actualizar. Recarga la página e inténtalo nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        if (!Enum.IsDefined(typeof(EstadoVenta), nuevoEstado))
        {
            _logger.LogWarning("Intento de asignar un estado inválido ({NuevoEstado}) a la venta #{IdVenta}.", nuevoEstado, idVenta);
            TempData["MensajeError"] = "El estado seleccionado no es válido. Por favor, elige una de las opciones disponibles.";
            return RedirectToAction(nameof(Index));
        }

        IDbContextTransaction? transaccion = null;

        try
        {
            transaccion = await _context.Database.BeginTransactionAsync();

            var venta = await _context.Venta
                .Include(v => v.Detalle)
                .ThenInclude(d => d.IdProductoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta);

            if (venta == null)
            {
                TempData["MensajeError"] = $"La venta #{idVenta} ya no existe o fue eliminada previamente.";
                return RedirectToAction(nameof(Index));
            }

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
                        if (det.IdProductoNavigation.Estado == 5)
                        {
                            await RollbackSeguroAsync(transaccion);
                            TempData["MensajeError"] = $"No se puede reactivar la venta #{idVenta}: el producto '{det.IdProductoNavigation.Nombre}' fue retirado del catálogo.";
                            return RedirectToAction(nameof(Index));
                        }

                        if (det.IdProductoNavigation.Stock < det.Cantidad)
                        {
                            await RollbackSeguroAsync(transaccion);
                            TempData["MensajeError"] = $"No se puede reactivar la venta #{idVenta}: stock insuficiente de '{det.IdProductoNavigation.Nombre}' (disponible: {det.IdProductoNavigation.Stock} u.).";
                            return RedirectToAction(nameof(Index));
                        }
                        det.IdProductoNavigation.Stock -= det.Cantidad;
                    }
                }
            }

            venta.Estado = nuevoEstado;
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            _logger.LogInformation("Estado de venta #{IdVenta} cambiado de {EstadoAnterior} a {NuevoEstado}.", idVenta, estadoAnterior, nuevoEstado);
            TempData["MensajeExito"] = $"Estado de la venta #{idVenta} actualizado a {(EstadoVenta)nuevoEstado}.";
        }
        catch (DbUpdateException exDb) when (exDb.InnerException is PostgresException exPg)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(exDb, "Error de integridad PostgreSQL ({SqlState}) al cambiar estado de venta #{IdVenta}.", exPg.SqlState, idVenta);
            TempData["MensajeError"] = $"No se pudo actualizar el estado de la venta #{idVenta} por una restricción en la base de datos. Verifica la información e inténtalo de nuevo.";
        }
        catch (NpgsqlException exRed)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(exRed, "Error de conexión Npgsql al cambiar el estado de la venta #{IdVenta}.", idVenta);
            TempData["MensajeError"] = $"No pudimos conectarnos a la base de datos para guardar el nuevo estado de la venta #{idVenta}. Verifica que el servicio esté activo e intenta nuevamente.";
        }
        catch (Exception ex) when (EsFalloConexion(ex))
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(ex, "Fallo de conexión a PostgreSQL al cambiar estado de la venta #{IdVenta}.", idVenta);
            TempData["MensajeError"] = $"No pudimos conectarnos a la base de datos para guardar el nuevo estado de la venta #{idVenta}. Verifica que el servicio esté activo e intenta nuevamente.";
        }
        catch (Exception exGeneral)
        {
            await RollbackSeguroAsync(transaccion);
            _logger.LogError(exGeneral, "Error inesperado al cambiar el estado de la venta #{IdVenta}.", idVenta);
            TempData["MensajeError"] = $"Ocurrió un inconveniente al actualizar el estado de la venta #{idVenta}. No se realizaron cambios; por favor intenta nuevamente.";
        }
        finally
        {
            if (transaccion != null)
            {
                await transaccion.DisposeAsync();
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------- Helpers ----------

    private async Task<IActionResult> VolverAIndexConModalAsync(VentaViewModel model)
    {
        List<Venta> ventas = new();
        InicializarListasVacias();

        try
        {
            ventas = await ObtenerVentasFiltradas(false);
            await CargarListasDesplegables();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudieron recargar los datos de la vista Index tras un error de validación.");
            TempData["MensajeError"] = "Se perdió la conexión con la base de datos al recargar el listado. Verifica el servicio e intenta nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SoloHoy = false;
        ViewBag.VentaForm = model;
        ViewBag.AbrirModalVenta = true;
        return View("Index", ventas);
    }

    private void InicializarListasVacias()
    {
        ViewBag.Productos = new List<Producto>();
        ViewBag.Clientes = new SelectList(Enumerable.Empty<object>());
        ViewBag.Personal = new SelectList(Enumerable.Empty<object>());
    }

    private async Task RollbackSeguroAsync(IDbContextTransaction? transaccion)
    {
        if (transaccion == null) return;
        try
        {
            await transaccion.RollbackAsync();
        }
        catch (Exception exRollback)
        {
            _logger.LogWarning(exRollback, "No fue posible ejecutar Rollback porque la conexión con PostgreSQL se interrumpió.");
        }
    }

    private static bool EsFalloConexion(Exception ex)
    {
        Exception? actual = ex;
        while (actual != null)
        {
            if (actual is NpgsqlException or TimeoutException or SocketException)
                return true;
            actual = actual.InnerException;
        }
        return false;
    }

    private async Task CargarListasDesplegables()
    {
        var productosDisponibles = await _context.Producto
            .Where(p => p.Estado != 5 && p.Stock > 0)
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