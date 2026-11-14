using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeladeriaJackFrost.Controllers;

public class ClienteController : Controller
{
    [Authorize(Roles = "Cliente, Administrador")]
    public IActionResult Index()
    {
        return View();
    }
}