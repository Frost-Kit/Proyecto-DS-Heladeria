using System.Diagnostics;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using Microsoft.AspNetCore.Mvc;
using HeladeriaJackFrost.Models;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

public class HomeController : Controller
{
    private readonly HeladeriaJackFrostDbContext _context;

    public HomeController(HeladeriaJackFrostDbContext context)
    {
        // Aqui igual a de mejorar para usar injeccion de dependencias
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // prueba rapidona (solo para probar)
        var proveedores = await _context.Proveedores
            .ToListAsync();

        return View(proveedores);
    }
}