using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

public class ProveedorController : Controller
{
    private readonly HeladeriaDbContext _context;

    public ProveedorController(HeladeriaDbContext context)
    {
        _context = context;
    }
    
    public async Task<IActionResult> Index()
    {
        // prueba rapidona (solo para probar)
        var proveedores = await _context.Proveedor
            .ToListAsync();
        return View(proveedores);
    }
    
    // GET: /Proveedor/Crear
    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }
    
    // POST: /Proveedor/Crear - HAY QUE USAR VIEWMODELS!!!!
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProveedorViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var proveedor = new Proveedor
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