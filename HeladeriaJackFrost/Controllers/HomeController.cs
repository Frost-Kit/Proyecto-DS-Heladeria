using System.Diagnostics;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

public class HomeController : Controller
{
    private readonly HeladeriaDbContext _context;

    public HomeController(HeladeriaDbContext context)
    {
        // Aqui igual a de mejorar para usar injeccion de dependencias
        _context = context;
    }
    
    public async Task<IActionResult> Index()
    {
        // prueba rapidona (solo para probar)
        var proveedores = await _context.Proveedor
            .ToListAsync();
        return View(proveedores);
    }
}