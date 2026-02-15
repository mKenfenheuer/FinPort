using FinPort.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinPort.Controllers
{
    public class AlertsController : Controller
    {
        private readonly DataBaseContext _context;

        public AlertsController(DataBaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var alerts = await _context.AiAlerts
                .Include(a => a.Portfolio)
                .Include(a => a.Position)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(alerts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(string id)
        {
            var alert = await _context.AiAlerts.FindAsync(id);
            if (alert != null)
            {
                alert.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(string id)
        {
            var alert = await _context.AiAlerts.FindAsync(id);
            if (alert != null)
            {
                _context.AiAlerts.Remove(alert);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
