using System.Diagnostics;
using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}