using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using Microsoft.AspNetCore.Mvc;

namespace HeladeriaJackFrost.Controllers;

public class ProveedorController : Controller
{
    private readonly HeladeriaDbContext _context;

    public ProveedorController(HeladeriaDbContext context)
    {
        _context = context;
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
    public async Task<IActionResult> Crear(Proveedor proveedor)
    {
        proveedor.Estado = 1;
        if (ModelState.IsValid)
        {
            _context.Proveedor.Add(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        return View(proveedor);
    }
    
}