using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FinPort.Models;
using Microsoft.EntityFrameworkCore.Storage;
using FinPort.Data;
using Microsoft.EntityFrameworkCore;

namespace FinPort.Controllers;

public class HomeController : Controller
{
    private readonly DataBaseContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, DataBaseContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Portfolios.Include(p => p.Positions).ToListAsync());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
