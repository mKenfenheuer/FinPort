using FinPort.Data;
using FinPort.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinPort.Controllers
{
    public class SettingsController : Controller
    {
        private readonly DataBaseContext _context;

        public SettingsController(DataBaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(new SettingViewModel()
            {
                PushPortfolioDetailsToHomeAssistant = await _context.GetSettingAsync("PushPortfolioDetailsToHomeAssistant", false),
                PushPositionDetailsToHomeAssistant = await _context.GetSettingAsync("PushPositionDetailsToHomeAssistant", false)
            });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("PushPortfolioDetailsToHomeAssistant,PushPositionDetailsToHomeAssistant")] SettingViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _context.SetSettingAsync("PushPortfolioDetailsToHomeAssistant", model.PushPortfolioDetailsToHomeAssistant);
                await _context.SetSettingAsync("PushPositionDetailsToHomeAssistant", model.PushPositionDetailsToHomeAssistant);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}