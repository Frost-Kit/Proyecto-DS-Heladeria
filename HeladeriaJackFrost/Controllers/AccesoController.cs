using System.Security.Claims;
using HeladeriaJackFrost.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

public class AccesoController : Controller
{
    private readonly HeladeriaDbContext _context;

    public AccesoController(HeladeriaDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
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
    public async Task<IActionResult> Login(string emailOrUser, string password, bool rememberMe, string? returnUrl = null)
    {
        // buscar la cuenta
        var cuenta = await _context.Cuenta
            .Include(c => c.IdPersonaNavigation)
            .FirstOrDefaultAsync(c => c.Email == emailOrUser || c.Usuario == emailOrUser);

        // si existe ta bien, si no pal login de nuevo
        if (cuenta == null)
        {
            ModelState.AddModelError(string.Empty, "Credenciales incorrectas. Intente nuevamente.");
            return View();
        }

        // checkar la contraseña encriptada con PasswordHasher
        var passwordHasher = new PasswordHasher<string>();
        var resultado = passwordHasher.VerifyHashedPassword(null, cuenta.Contra, password);

        if (resultado == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Credenciales incorrectas.");
            return View();
        }

        // validar perfiles (Personal y/o Cliente) y asignar roles dinámicamente
        var personal = await _context.Personal.FirstOrDefaultAsync(p => p.IdPersona == cuenta.IdPersona);
        var cliente = await _context.Cliente.FirstOrDefaultAsync(c => c.IdPersona == cuenta.IdPersona);

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
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null // duracion de la cookie persistente
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
    
        // aqui ha de arreglarlo mas adenlante para redireccionar al cliente a SU home o vista
        return esPersonalActivo ? RedirectToAction("Index", "Home") : RedirectToAction("Index", "Home");
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Acceso");
    }
}