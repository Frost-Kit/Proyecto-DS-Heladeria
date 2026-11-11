using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using Microsoft.AspNetCore.Mvc;

namespace HeladeriaJackFrost.Controllers;

public class ProveedorController : Controller
{
    private readonly HeladeriaJackFrostDbContext _context;

    public ProveedorController(HeladeriaJackFrostDbContext context)
    {
        _context = context;
    }

    // GET: /Proveedor/Crear
    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    // POST: /Proveedor/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Proveedor proveedor)
    {
        if (ModelState.IsValid)
        {
            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        return View(proveedor);
    }
}