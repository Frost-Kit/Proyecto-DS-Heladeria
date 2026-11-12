using Microsoft.AspNetCore.Mvc;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

public class ProductoController : Controller
{
    private readonly HeladeriaDbContext _context;

    public ProductoController(HeladeriaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // prueba rapidona (solo para probar)
        var productos = await _context.Producto
            .Include(p => p.IdCategoriaNavigation)
            .Select(p => new ProductoViewModel
            {
                ID_Producto = p.IdProducto.ToString(),
                Categoria = p.IdCategoriaNavigation.Nombre,
                Nombre = p.Nombre,
                Precio = p.Precio.ToString("F2"),
                Stock = p.Stock.ToString()
            })
            .ToListAsync();
        return View(productos);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProductoViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var producto = new Producto
            {
                Nombre = viewModel.Nombre,
                NombreContacto = viewModel.NombreContacto,
                Telefono = viewModel.Telefono,
                Direccion = viewModel.Direccion,
                Estado = 1
            };

            try
            {
                _context.Proveedor.Add(proveedor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
            }
        }

        return View(viewModel);
    }
}