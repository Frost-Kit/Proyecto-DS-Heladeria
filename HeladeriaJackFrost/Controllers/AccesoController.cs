using System.Security.Claims;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using HeladeriaJackFrost.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HeladeriaJackFrost.Controllers;

public class AccesoController : Controller
{
    private readonly HeladeriaDbContext _context;
    private readonly ILogger<AccesoController> _logger;

    public AccesoController(HeladeriaDbContext context, ILogger<AccesoController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Cliente"))
            return RedirectToAction("Index", "Cliente");
        // pal Home si esta autenticado
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (model is null)
            return View(new LoginViewModel());
        
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Campos incorrectos. Intente nuevamente.");
            return View(model);
        }

        Cuenta? cuenta;
        Personal? personal;
        Cliente? cliente;

        try
        {
            cuenta = await _context.Cuenta
                .Include(c => c.IdPersonaNavigation)
                .FirstOrDefaultAsync(c => c.Email == model.EmailOrUser || c.Usuario == model.EmailOrUser);

            if (cuenta == null)
            {
                ModelState.AddModelError(string.Empty, "No encontramos una cuenta con esos datos. Verifica tu correo o usuario.");
                return View(model);
            }

            var passwordHasher = new PasswordHasher<string>();
            var resultado = passwordHasher.VerifyHashedPassword(null!, cuenta.Contra, model.Password);

            if (resultado == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "La contraseña ingresada no es correcta. Inténtalo nuevamente.");
                return View(model);
            }

            personal = await _context.Personal.FirstOrDefaultAsync(p => p.IdPersona == cuenta.IdPersona);
            cliente = await _context.Cliente.FirstOrDefaultAsync(c => c.IdPersona == cuenta.IdPersona);
        }
        catch (Exception ex)
        {
            var baseEx = ex.GetBaseException();
            bool esFalloConexionDb = baseEx is NpgsqlException or TimeoutException;

            if (esFalloConexionDb)
            {
                _logger.LogError(
                    ex,
                    "Fallo de conexión a base de datos durante Login para {EmailOrUser}. Causa raíz: {BaseExceptionType}",
                    model.EmailOrUser,
                    baseEx.GetType().Name);

                ModelState.AddModelError(
                    string.Empty,
                    "No es posible conectarse al servicio de datos en este momento. Verifica que esté activo o intenta en unos minutos.");
            }
            else
            {
                _logger.LogError(ex, "Error inesperado durante Login para {EmailOrUser}", model.EmailOrUser);
                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un problema al iniciar sesión. Intenta nuevamente; si persiste, contacta a soporte.");
            }

            return View(model);
        }
        
        // si existe uno almenos ta bien, si no pal login de nuevo
        if (personal == null && cliente == null)
        {
            ModelState.AddModelError(string.Empty, "La cuenta no está asociada a ningún perfil en el sistema.");
            return View();
        }

        bool esPersonalActivo = (personal != null && personal.Estado == 1);
        if (personal != null && !esPersonalActivo)
        {
            ModelState.AddModelError(string.Empty, "Tu cuenta de personal se encuentra inactiva.");
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, cuenta.IdPersonaNavigation.Nombre + " " + cuenta.IdPersonaNavigation.ApellidoPat),
            new Claim(ClaimTypes.Email, cuenta.Email),
            new Claim("IdPersona", cuenta.IdPersona.ToString())
        };

        if (esPersonalActivo)
        {
            claims.Add(new Claim(ClaimTypes.Role, personal.Cargo));
            claims.Add(new Claim("IdPersonal", personal.IdPersonal.ToString()));
        }

        if (cliente != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Cliente"));
            claims.Add(new Claim("IdCliente", cliente.IdCliente.ToString()));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null // duracion de la cookie persistente
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties // <--- propiedades
        );

        // una redireccion mas si algo salio mal
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        
        _logger.LogInformation("Login efectuado correctamente {ModelEmailOrUser}.", model.EmailOrUser);
    
        // aqui ha de arreglarlo mas adenlante para redireccionar al cliente a SU home o vista
        return esPersonalActivo ? RedirectToAction("Index", "Home") : RedirectToAction("PanelCliente", "Cliente");
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("Cerrando Sesion de {User.Identity.Name}.", User.Identity.Name);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Acceso");
    }
}