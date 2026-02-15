using FinPort.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinPort.Controllers
{
    public class NewsController : Controller
    {
        private readonly DataBaseContext _context;

        public NewsController(DataBaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var articles = await _context.ScrapedArticles
                .Include(a => a.Position)
                .OrderByDescending(a => a.ScrapedAt)
                .Take(100)
                .ToListAsync();
            return View(articles);
        }
    }
}
