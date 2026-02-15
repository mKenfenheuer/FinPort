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
            var model = new SettingViewModel
            {
                // Home Assistant
                PushPortfolioDetailsToHomeAssistant = await _context.GetSettingAsync("PushPortfolioDetailsToHomeAssistant", false),
                PushPositionDetailsToHomeAssistant = await _context.GetSettingAsync("PushPositionDetailsToHomeAssistant", false),

                // Scraper
                ScraperEnabled = await _context.GetSettingAsync("Scraper:Enabled", false),
                ScraperIntervalMinutes = await _context.GetSettingAsync("Scraper:IntervalMinutes", 360),
                ScraperRssFeeds = await _context.GetSettingAsync("Scraper:RssFeeds", ""),

                // AI
                AiEnabled = await _context.GetSettingAsync("AI:Enabled", false),
                AiBaseUrl = await _context.GetSettingAsync("AI:BaseUrl", ""),
                AiApiKey = await _context.GetSettingAsync("AI:ApiKey", ""),
                AiModel = await _context.GetSettingAsync("AI:Model", ""),
                AiAnalysisPrompt = await _context.GetSettingAsync("AI:AnalysisPrompt", ""),

                // Notifications - Email
                NotificationEmailEnabled = await _context.GetSettingAsync("Notification:Email:Enabled", false),
                NotificationEmailSmtpHost = await _context.GetSettingAsync("Notification:Email:SmtpHost", ""),
                NotificationEmailSmtpPort = await _context.GetSettingAsync("Notification:Email:SmtpPort", 587),
                NotificationEmailSmtpUser = await _context.GetSettingAsync("Notification:Email:SmtpUser", ""),
                NotificationEmailSmtpPassword = await _context.GetSettingAsync("Notification:Email:SmtpPassword", ""),
                NotificationEmailFromAddress = await _context.GetSettingAsync("Notification:Email:FromAddress", ""),
                NotificationEmailToAddress = await _context.GetSettingAsync("Notification:Email:ToAddress", ""),

                // Notifications - Webhook
                NotificationWebhookEnabled = await _context.GetSettingAsync("Notification:Webhook:Enabled", false),
                NotificationWebhookUrl = await _context.GetSettingAsync("Notification:Webhook:Url", ""),

                // Notifications - Home Assistant
                NotificationHomeAssistantEnabled = await _context.GetSettingAsync("Notification:HomeAssistant:Enabled", false)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Home Assistant
                await _context.SetSettingAsync("PushPortfolioDetailsToHomeAssistant", model.PushPortfolioDetailsToHomeAssistant);
                await _context.SetSettingAsync("PushPositionDetailsToHomeAssistant", model.PushPositionDetailsToHomeAssistant);

                // Scraper
                await _context.SetSettingAsync("Scraper:Enabled", model.ScraperEnabled);
                await _context.SetSettingAsync("Scraper:IntervalMinutes", model.ScraperIntervalMinutes);
                await _context.SetSettingAsync("Scraper:RssFeeds", model.ScraperRssFeeds ?? "");

                // AI
                await _context.SetSettingAsync("AI:Enabled", model.AiEnabled);
                await _context.SetSettingAsync("AI:BaseUrl", model.AiBaseUrl ?? "");
                await _context.SetSettingAsync("AI:ApiKey", model.AiApiKey ?? "");
                await _context.SetSettingAsync("AI:Model", model.AiModel ?? "");
                await _context.SetSettingAsync("AI:AnalysisPrompt", model.AiAnalysisPrompt ?? "");

                // Notifications - Email
                await _context.SetSettingAsync("Notification:Email:Enabled", model.NotificationEmailEnabled);
                await _context.SetSettingAsync("Notification:Email:SmtpHost", model.NotificationEmailSmtpHost ?? "");
                await _context.SetSettingAsync("Notification:Email:SmtpPort", model.NotificationEmailSmtpPort);
                await _context.SetSettingAsync("Notification:Email:SmtpUser", model.NotificationEmailSmtpUser ?? "");
                await _context.SetSettingAsync("Notification:Email:SmtpPassword", model.NotificationEmailSmtpPassword ?? "");
                await _context.SetSettingAsync("Notification:Email:FromAddress", model.NotificationEmailFromAddress ?? "");
                await _context.SetSettingAsync("Notification:Email:ToAddress", model.NotificationEmailToAddress ?? "");

                // Notifications - Webhook
                await _context.SetSettingAsync("Notification:Webhook:Enabled", model.NotificationWebhookEnabled);
                await _context.SetSettingAsync("Notification:Webhook:Url", model.NotificationWebhookUrl ?? "");

                // Notifications - Home Assistant
                await _context.SetSettingAsync("Notification:HomeAssistant:Enabled", model.NotificationHomeAssistantEnabled);

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}
