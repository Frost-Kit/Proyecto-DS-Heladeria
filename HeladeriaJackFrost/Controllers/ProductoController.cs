using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace HeladeriaJackFrost.Controllers;

[Authorize(Roles = "Administrador")]
public class ProductoController : Controller
{
    private const int EstadoActivo = 1;
    private const int EstadoEliminado = 5;

    private readonly HeladeriaDbContext _context;
    private readonly ILogger<ProductoController> _logger;

    public ProductoController(HeladeriaDbContext context, ILogger<ProductoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? idCategoria, bool mostrarEliminados = false)
    {
        var categorias = new List<Categoria>();
        var productos = new List<ProductoViewModel>();

        try
        {
            categorias = await _context.Categoria
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var consulta = _context.Producto
                .Include(p => p.IdCategoriaNavigation)
                .Where(p => mostrarEliminados
                    ? p.Estado == EstadoEliminado
                    : p.Estado != EstadoEliminado)
                .AsQueryable();

            if (idCategoria.HasValue
                && categorias.Any(c => c.IdCategoria == idCategoria.Value))
            {
                consulta = consulta.Where(p => p.IdCategoria == idCategoria.Value);
                ViewBag.IdCategoriaSeleccionada = idCategoria.Value;
            }

            productos = await consulta
                .OrderBy(p => p.IdProducto)
                .Select(p => new ProductoViewModel
                {
                    ID_Producto = p.IdProducto,
                    Categoria = p.IdCategoriaNavigation.Nombre,
                    Nombre = p.Nombre,
                    Precio = p.Precio,
                    Stock = p.Stock,
                    Estado = p.Estado
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo cargar la lista de productos.");
            ViewBag.MensajeError =
                "No se pudieron cargar los productos desde la base de datos. Verifica la conexión e inténtalo nuevamente.";
        }

        ViewBag.ListaCategorias = new SelectList(
            categorias,
            nameof(Categoria.IdCategoria),
            nameof(Categoria.Nombre),
            ViewBag.IdCategoriaSeleccionada);
        ViewBag.MostrarEliminados = mostrarEliminados;

        return View(productos);
    }

    [HttpGet]
    public async Task<IActionResult> Formulario(int? id)
    {
        ProductoViewModel viewModel;

        if (id is null)
        {
            viewModel = new ProductoViewModel();
        }
        else
        {
            var producto = await _context.Producto
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProducto == id && p.Estado != EstadoEliminado);

            if (producto == null)
                return NotFound();

            viewModel = new ProductoViewModel
            {
                ID_Producto = producto.IdProducto,
                IdCategoria = producto.IdCategoria,
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                Stock = producto.Stock
            };
        }

        await CargarCategorias(viewModel.IdCategoria);
        return PartialView("_ProductoForm", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProductoViewModel viewModel)
    {
        if (!NombreValido(viewModel.Nombre))
            ModelState.AddModelError(nameof(viewModel.Nombre),
                "El nombre debe incluir al menos una letra; puede contener números y caracteres especiales.");

        await ValidarCategoria(viewModel.IdCategoria);

        if (!ModelState.IsValid)
        {
            await CargarCategorias(viewModel.IdCategoria);
            return FormularioProductoInvalido(viewModel);
        }

        var producto = new Producto
        {
            Nombre = viewModel.Nombre,
            Precio = viewModel.Precio,
            Stock = viewModel.Stock,
            IdCategoria = viewModel.IdCategoria,
            Estado = EstadoActivo
        };

        try
        {
            _context.Producto.Add(producto);
            await _context.SaveChangesAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { ok = true });
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo guardar el producto. Verifique que el nombre no esté duplicado y vuelva a intentarlo.");
            await CargarCategorias(viewModel.IdCategoria);
            return FormularioProductoInvalido(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(ProductoViewModel viewModel)
    {
        var producto = await _context.Producto.FindAsync(viewModel.ID_Producto);

        if (producto == null || producto.Estado == EstadoEliminado)
            return NotFound();

        if (!string.Equals(viewModel.Nombre, producto.Nombre, StringComparison.Ordinal)
            && !NombreValido(viewModel.Nombre))
        {
            ModelState.AddModelError(nameof(viewModel.Nombre),
                "El nombre debe incluir al menos una letra; puede contener números y caracteres especiales.");
        }

        await ValidarCategoria(viewModel.IdCategoria);

        if (!ModelState.IsValid)
        {
            await CargarCategorias(viewModel.IdCategoria);
            return FormularioProductoInvalido(viewModel);
        }

        producto.Nombre = viewModel.Nombre;
        producto.Precio = viewModel.Precio;
        producto.Stock = viewModel.Stock;
        producto.IdCategoria = viewModel.IdCategoria;

        try
        {
            await _context.SaveChangesAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { ok = true });
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo actualizar el producto. Verifique que el nombre no esté duplicado y vuelva a intentarlo.");
            await CargarCategorias(viewModel.IdCategoria);
            return FormularioProductoInvalido(viewModel);
        }
    }

    [HttpPost, ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var producto = await _context.Producto.FindAsync(id);

        if (producto == null || producto.Estado == EstadoEliminado)
            return NotFound();

        try
        {
            producto.Estado = EstadoEliminado;
            await _context.SaveChangesAsync();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { ok = true });
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo retirar el producto del catálogo. Inténtelo nuevamente.");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                return Json(new
                {
                    error = "No se pudo retirar el producto del catálogo. Inténtelo nuevamente."
                });
            }
            TempData["MensajeError"] = "No se pudo retirar el producto del catálogo. Inténtelo nuevamente.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var producto = await _context.Producto
            .FirstOrDefaultAsync(p =>
                p.IdProducto == id && p.Estado == EstadoEliminado);

        if (producto == null)
            return NotFound();

        try
        {
            producto.Estado = EstadoActivo;
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"El producto \"{producto.Nombre}\" volvió al catálogo.";
        }
        catch (DbUpdateException)
        {
            TempData["MensajeError"] =
                "No se pudo reactivar el producto. Inténtelo nuevamente.";
        }

        return RedirectToAction(nameof(Index), new { mostrarEliminados = true });
    }

    private async Task ValidarCategoria(int idCategoria)
    {
        if (!await _context.Categoria.AnyAsync(c => c.IdCategoria == idCategoria))
            ModelState.AddModelError(nameof(ProductoViewModel.IdCategoria),
                "Seleccione una categoría válida.");
    }

    private static bool NombreValido(string? nombre)
    {
        return !string.IsNullOrWhiteSpace(nombre)
            && Regex.IsMatch(nombre, @"\p{L}");
    }

    private async Task CargarCategorias(int? seleccionada = null)
    {
        var categorias = await _context.Categoria
            .OrderBy(c => c.IdCategoria)
            .ToListAsync();

        ViewBag.ListaCategorias = new SelectList(
            categorias,
            nameof(Categoria.IdCategoria),
            nameof(Categoria.Nombre),
            seleccionada);
    }

    private IActionResult FormularioProductoInvalido(ProductoViewModel viewModel)
    {
        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return PartialView("_ProductoForm", viewModel);
    }

}
