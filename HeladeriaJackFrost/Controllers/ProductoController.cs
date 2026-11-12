using Microsoft.AspNetCore.Mvc;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;


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
        ViewBag.ListaCategorias = new SelectList(new List<object>
        {
            new { Id = 1, Texto = "1 - Cremas Clásicas" },
            new { Id = 2, Texto = "2 - Cremas Especiales" },
            new { Id = 3, Texto = "3 - Frutales al Agua" },
            new { Id = 4, Texto = "4 - Postres Helados" },
            new { Id = 5, Texto = "5 - Potes Familiares 2L" },
            new { Id = 6, Texto = "6 - Línea Sin Azúcar / Keto" }
        }, "Id", "Texto", 1);
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
                Precio = decimal.Parse(viewModel.Precio),
                Stock = int.Parse(viewModel.Stock),
                IdCategoria = viewModel.IdCategoria
            };

            try
            {
                _context.Producto.Add(producto);
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