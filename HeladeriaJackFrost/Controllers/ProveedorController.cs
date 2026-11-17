using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

[Authorize(Roles = "Administrador")]
public class ProveedorController : Controller
{
    private const int EstadoActivo = 1;
    private const int EstadoEliminado = 5;

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

        var proveedores = await query
            .OrderBy(p => p.IdProveedor)
            .ToListAsync();

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
        if (!ModelState.IsValid)
            return FormularioInvalido(viewModel);

        var proveedor = new Proveedor
        {
            Nombre = viewModel.Nombre,
            NombreContacto = viewModel.NombreContacto,
            Telefono = viewModel.Telefono,
            Direccion = viewModel.Direccion,
            Estado = viewModel.Estado
        };

        try
        {
            _context.Proveedor.Add(proveedor);
            await _context.SaveChangesAsync();
            return Json(new { ok = true });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al crear el proveedor {Nombre}", viewModel.Nombre);
            ModelState.AddModelError(string.Empty, "No se pudo guardar el proveedor. Inténtalo nuevamente.");
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

        proveedor.Nombre = viewModel.Nombre;
        proveedor.NombreContacto = viewModel.NombreContacto;
        proveedor.Telefono = viewModel.Telefono;
        proveedor.Direccion = viewModel.Direccion;
        proveedor.Estado = viewModel.Estado;

        try
        {
            await _context.SaveChangesAsync();
            return Json(new { ok = true });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al actualizar el proveedor {Id}", viewModel.IdProveedor);
            ModelState.AddModelError(string.Empty, "No se pudo guardar los cambios. Inténtalo nuevamente.");
            return FormularioInvalido(viewModel);
        }
    }

    // POST: /Proveedor/Eliminar  (eliminación lógica)
    [HttpPost, ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmed(int id, string? buscar)
    {
        var proveedor = await _context.Proveedor.FindAsync(id);

        if (proveedor == null) return RedirectToAction(nameof(Index), new { buscar });
        
        proveedor.Estado = EstadoEliminado;
        await _context.SaveChangesAsync();

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
