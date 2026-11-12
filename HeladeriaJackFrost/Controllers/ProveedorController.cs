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
        var proveedor = await _context.Proveedor.FirstOrDefaultAsync(x => x.IdProveedor == viewModel.IdProveedor);
        
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
    
}