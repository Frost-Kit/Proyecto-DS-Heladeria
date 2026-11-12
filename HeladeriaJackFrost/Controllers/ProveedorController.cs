using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        var proveedores = await _context.Proveedor
            .Where(p => p.Estado != 5)
            .OrderBy(p => p.IdProveedor)
            .ToListAsync();
        
        return View(proveedores);
    }
    
    // GET: /Proveedor/Crear
    [HttpGet]
    public IActionResult Crear()
    {
        ViewBag.ListaEstados = new SelectList(new List<object>
        {
            new { Id = 1, Texto = "1 - Activo" },
            new { Id = 2, Texto = "2 - Inactivo" },
            new { Id = 3, Texto = "3 - En Vacaciones" },
            new { Id = 4, Texto = "4 - Suspendido" }
        }, "Id", "Texto", 1);
        return View();
    }
    
    // POST: /Proveedor/Crear
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
    
    // GET: Actualizar datos del proveedor
    public async Task<IActionResult> Actualizar(int idProveedor)
    {
        var proveedorObejtivo = await  _context.Proveedor
            .FindAsync(idProveedor);
        
        if (proveedorObejtivo == null)
            return NotFound();
        
        ViewBag.ListaEstados = new SelectList(new List<object>
        {
            new { Id = 1, Texto = "1 - Activo" },
            new { Id = 2, Texto = "2 - Inactivo" },
            new { Id = 3, Texto = "3 - En Vacaciones" },
            new { Id = 4, Texto = "4 - Suspendido" }
        }, "Id", "Texto", proveedorObejtivo.Estado);
        
        var viewModel = new ProveedorViewModel
        {
            IdProveedor = proveedorObejtivo.IdProveedor,
            Nombre = proveedorObejtivo.Nombre,
            NombreContacto = proveedorObejtivo.NombreContacto,
            Telefono = proveedorObejtivo.Telefono,
            Direccion = proveedorObejtivo.Direccion,
            Estado = proveedorObejtivo.Estado
        };
        return View(viewModel);
    }
    
    // Post: Actualizar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(ProveedorViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var proveedor = await _context.Proveedor
            .FindAsync(viewModel.IdProveedor);
        
        if (proveedor == null)
            return NotFound();
        
        proveedor.Nombre = viewModel.Nombre;
        proveedor.NombreContacto = viewModel.NombreContacto;
        proveedor.Telefono = viewModel.Telefono;
        proveedor.Direccion = viewModel.Direccion;
        proveedor.Estado = viewModel.Estado;

        try
        {
            _context.Proveedor.Update(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    // POST: Proveedor/Eliminar
    [HttpPost, ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmed(int id)
    {
        var proveedor = await _context.Proveedor
            .FindAsync(id);
    
        if (proveedor != null)
        {
            proveedor.Estado = 5;
        
            _context.Proveedor.Update(proveedor);
            await _context.SaveChangesAsync();
        }
    
        return RedirectToAction(nameof(Index));
    }
}