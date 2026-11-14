using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers
{
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly HeladeriaDbContext _context;

        public ClienteController(HeladeriaDbContext context)
        {
            _context = context;
        }
        
        [Authorize(Roles = "Administrador, Cliente")]
        public async Task<IActionResult> PanelCliente(int? id)
        {
            int idPersona;

            // si es Administrador y proporciona un ID de cliente por parámetro
            if (User.IsInRole("Administrador") && id.HasValue)
            {
                var clienteBuscado = await _context.Cliente
                    .FirstOrDefaultAsync(c => c.IdCliente == id.Value);

                if (clienteBuscado == null) return NotFound();
                idPersona = clienteBuscado.IdPersona;
            }
            else
            {
                // Si es un Cliente normal (o el admin entra a su propio perfil si tuviera)
                var idPersonaClaim = User.FindFirst("IdPersona")?.Value;
        
                if (idPersonaClaim == null || !int.TryParse(idPersonaClaim, out idPersona))
                {
                    return RedirectToAction("AccesoDenegado", "Acceso");
                }
            }

            // Buscamos los datos completos de la persona y cliente
            var clientePersona = await _context.Cliente
                .Include(c => c.IdPersonaNavigation)
                .FirstOrDefaultAsync(c => c.IdPersona == idPersona);

            if (clientePersona == null) return NotFound();

            var modelUnico = new ClienteViewModel
            {
                IdCliente = clientePersona.IdCliente,
                IdPersona = clientePersona.IdPersona,
                Nombre = clientePersona.IdPersonaNavigation.Nombre,
                ApellidoPat = clientePersona.IdPersonaNavigation.ApellidoPat,
                ApellidoMat = clientePersona.IdPersonaNavigation.ApellidoMat,
                Email = clientePersona.IdPersonaNavigation.Cuenta != null ? clientePersona.IdPersonaNavigation.Cuenta.Email : "Sin correo",
                PuntosDeCliente = clientePersona.PuntosDeCliente
            };

            return View(modelUnico);
        }

        // Vista de tabla general exclusiva para el Administrador
        [Authorize(Roles = "Administrador")] 
        public async Task<IActionResult> Index()
        {
            var clientesLista = await _context.Cliente
                .Include(c => c.IdPersonaNavigation)
                .Select(c => new ClienteViewModel
                {
                    IdCliente = c.IdCliente,
                    IdPersona = c.IdPersona,
                    Nombre = c.IdPersonaNavigation.Nombre,
                    ApellidoPat = c.IdPersonaNavigation.ApellidoPat,
                    ApellidoMat = c.IdPersonaNavigation.ApellidoMat,
                    Email = c.IdPersonaNavigation.Cuenta != null ? c.IdPersonaNavigation.Cuenta.Email : "Sin correo",
                    PuntosDeCliente = c.PuntosDeCliente
                })
                .ToListAsync();

            return View(clientesLista);
        }

        // GET: Cliente/Crear
        [Authorize(Roles = "Administrador")]
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Cliente/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Crear(ClienteViewModel model)
        {
            if (ModelState.IsValid)
            {
                var persona = new Persona
                {
                    Nombre = model.Nombre,
                    ApellidoPat = model.ApellidoPat,
                    ApellidoMat = model.ApellidoMat
                };

                _context.Persona.Add(persona);
                await _context.SaveChangesAsync();

                var cliente = new Cliente
                {
                    IdPersona = persona.IdPersona,
                    PuntosDeCliente = model.PuntosDeCliente
                };

                _context.Cliente.Add(cliente);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Cliente/Editar/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Cliente
                .Include(c => c.IdPersonaNavigation)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null) return NotFound();

            var model = new ClienteViewModel
            {
                IdCliente = cliente.IdCliente,
                IdPersona = cliente.IdPersona,
                Nombre = cliente.IdPersonaNavigation.Nombre,
                ApellidoPat = cliente.IdPersonaNavigation.ApellidoPat,
                ApellidoMat = cliente.IdPersonaNavigation.ApellidoMat,
                PuntosDeCliente = cliente.PuntosDeCliente
            };

            return View(model);
        }

        // POST: Cliente/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Editar(int id, ClienteViewModel model)
        {
            if (id != model.IdCliente) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var cliente = await _context.Cliente
                        .Include(c => c.IdPersonaNavigation)
                        .FirstOrDefaultAsync(c => c.IdCliente == id);

                    if (cliente == null) return NotFound();

                    cliente.IdPersonaNavigation.Nombre = model.Nombre;
                    cliente.IdPersonaNavigation.ApellidoPat = model.ApellidoPat;
                    cliente.IdPersonaNavigation.ApellidoMat = model.ApellidoMat;
                    cliente.PuntosDeCliente = model.PuntosDeCliente;

                    _context.Update(cliente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Cliente.Any(e => e.IdCliente == model.IdCliente))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}