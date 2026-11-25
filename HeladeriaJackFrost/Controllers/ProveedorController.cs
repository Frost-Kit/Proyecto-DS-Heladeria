using System.Globalization;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HeladeriaJackFrost.Controllers;

[Authorize(Roles = "Administrador")]
public class ProveedorController : Controller
{
    private const int EstadoActivo = 1;
    private const int EstadoEliminado = 5;
    private TextInfo _textInfo = CultureInfo.CurrentCulture.TextInfo;

    private readonly HeladeriaDbContext _context;
    private readonly ILogger<ProveedorController> _logger;

    public ProveedorController(HeladeriaDbContext context, ILogger<ProveedorController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /Proveedor
    public async Task<IActionResult> Index(string? buscar)
    {
        List<Proveedor> proveedores = new();

        try
        {
            var query = _context.Proveedor
                .AsNoTracking()
                .Where(p => p.Estado != EstadoEliminado);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim().ToLower();

                query = query.Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.NombreContacto.ToLower().Contains(texto) ||
                    p.Telefono.ToLower().Contains(texto) ||
                    p.Direccion.ToLower().Contains(texto));
            }

            proveedores = await query
                .OrderBy(p => p.IdProveedor)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            var baseEx = ex.GetBaseException();
            bool esFalloConexion = baseEx is NpgsqlException or TimeoutException;

            _logger.LogError(
                ex,
                "Error al cargar la lista de proveedores en Index (Filtro: {Buscar}). Causa raíz: {BaseExceptionType}",
                buscar,
                baseEx.GetType().Name);

            TempData["MensajeError"] = esFalloConexion
                ? "El sistema no está disponible en este momento para mostrar los proveedores. Intenta nuevamente en unos minutos o avisa al encargado."
                : "No pudimos cargar la lista de proveedores en este momento. Intenta actualizar la página o vuelve a intentarlo más tarde.";
        }

        ViewData["CurrentFilter"] = buscar;
        return View(proveedores);
    }

    // GET: /Proveedor/Formulario          -> formulario vacío (crear)
    // GET: /Proveedor/Formulario?id=5     -> formulario con datos (editar)
    // Devuelve solo el parcial que se inyecta dentro del modal.
    [HttpGet]
    public async Task<IActionResult> Formulario(int? id)
    {
        ProveedorViewModel viewModel;

        if (id is null)
        {
            viewModel = new ProveedorViewModel { Estado = EstadoActivo };
        }
        else
        {
            var proveedor = await _context.Proveedor
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProveedor == id && p.Estado != EstadoEliminado);

            if (proveedor == null)
                return NotFound();

            viewModel = new ProveedorViewModel
            {
                IdProveedor = proveedor.IdProveedor,
                Nombre = proveedor.Nombre,
                NombreContacto = proveedor.NombreContacto,
                Telefono = proveedor.Telefono,
                Direccion = proveedor.Direccion,
                Estado = proveedor.Estado
            };
        }

        CargarEstados();
        return PartialView("_ProveedorForm", viewModel);
    }

    // POST: /Proveedor/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProveedorViewModel viewModel)
    {
        // Cláusula de guarda
        if (viewModel is null)
            return BadRequest("No se recibieron los datos del proveedor.");

        if (!ModelState.IsValid)
            return FormularioInvalido(viewModel);

        var proveedor = new Proveedor
        {
            Nombre = _textInfo.ToTitleCase(viewModel.Nombre.Trim().ToLower()),
            NombreContacto = _textInfo.ToTitleCase(viewModel.NombreContacto.Trim().ToLower()),
            Telefono = viewModel.Telefono.Trim(),
            Direccion = viewModel.Direccion.Trim(),
            Estado = viewModel.Estado
        };

        try
        {
            _context.Proveedor.Add(proveedor);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Proveedor creado correctamente. Id: {IdProveedor}, Nombre: {Nombre}",
                proveedor.IdProveedor,
                proveedor.Nombre);
            TempData["MensajeExito"] = $"El proveedor '{proveedor.Nombre}' se registró correctamente.";
            return Json(new { ok = true });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error de base de datos al crear el proveedor {Nombre}", viewModel.Nombre);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo registrar el proveedor. Revisa que los datos estén correctos e inténtalo de nuevo.");
            return FormularioInvalido(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear el proveedor {Nombre}", viewModel.Nombre);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo guardar el nuevo proveedor en este momento. Inténtalo nuevamente en unos instantes.");
            return FormularioInvalido(viewModel);
        }
    }

    // POST: /Proveedor/Actualizar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(ProveedorViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return FormularioInvalido(viewModel);

        var proveedor = await _context.Proveedor
            .FirstOrDefaultAsync(p => p.IdProveedor == viewModel.IdProveedor && p.Estado != EstadoEliminado);

        if (proveedor == null)
            return NotFound();

        proveedor.Nombre = _textInfo.ToTitleCase(viewModel.Nombre.Trim().ToLower());
        proveedor.NombreContacto = _textInfo.ToTitleCase(viewModel.NombreContacto.Trim().ToLower());
        proveedor.Telefono = viewModel.Telefono;
        proveedor.Direccion = viewModel.Direccion;
        proveedor.Estado = viewModel.Estado;

        try
        {
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Proveedor actualizado correctamente. Id: {IdProveedor}, Nombre: {Nombre}",
                proveedor.IdProveedor,
                proveedor.Nombre);
            TempData["MensajeExito"] = $"Los datos del proveedor '{proveedor.Nombre}' se actualizaron correctamente.";
            return Json(new { ok = true });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error de base de datos al actualizar el proveedor {IdProveedor}", viewModel.IdProveedor);
            ModelState.AddModelError(
                string.Empty,
                "No se pudieron guardar los cambios del proveedor. Revisa la información e inténtalo nuevamente.");
            return FormularioInvalido(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar el proveedor {IdProveedor}", viewModel.IdProveedor);
            ModelState.AddModelError(
                string.Empty,
                "No pudimos actualizar los datos en este momento. Vuelve a intentarlo en unos minutos.");
            return FormularioInvalido(viewModel);
        }
    }

    // POST: /Proveedor/Eliminar  (eliminación lógica)
    [HttpPost, ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, string? buscar)
    {
        if (id <= 0)
        {
            TempData["MensajeError"] = "No pudimos identificar el proveedor que deseas quitar. Vuelve a intentarlo.";
            return RedirectToAction(nameof(Index), new { buscar });
        }

        try
        {
            var proveedor = await _context.Proveedor.FindAsync(id);
            if (proveedor == null)
            {
                TempData["MensajeError"] = "Este proveedor ya no está disponible o fue eliminado anteriormente.";
                return RedirectToAction(nameof(Index), new { buscar });
            }

            var nombreProveedor = proveedor.Nombre;
            proveedor.Estado = EstadoEliminado;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Proveedor eliminado lógicamente. Id: {IdProveedor}, Nombre: {Nombre}",
                id,
                nombreProveedor);
            TempData["MensajeExito"] = $"El proveedor '{nombreProveedor}' se eliminó correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el proveedor {IdProveedor}", id);
            TempData["MensajeError"] = "No se pudo eliminar el proveedor en este momento. Inténtalo nuevamente más tarde.";
        }

        return RedirectToAction(nameof(Index), new { buscar });
    }

    // ---------- Helpers ----------

    // Devuelve el formulario con los errores (HTTP 422) para que el JS lo vuelva a pintar en el modal.
    private IActionResult FormularioInvalido(ProveedorViewModel viewModel)
    {
        CargarEstados();
        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return PartialView("_ProveedorForm", viewModel);
    }

    // El estado 5 (Eliminado) no se ofrece: solo se asigna desde "Eliminar".
    private void CargarEstados()
    {
        ViewBag.ListaEstados = new List<SelectListItem>
        {
            new("1 - Activo", "1"),
            new("2 - Inactivo", "2"),
            new("3 - En Vacaciones", "3"),
            new("4 - Suspendido", "4")
        };
    }
}
