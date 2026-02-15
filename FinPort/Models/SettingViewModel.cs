using System.ComponentModel.DataAnnotations;

namespace FinPort.Models;

public class SettingViewModel
{
    // Home Assistant
    [Display(Name = "Push Portfolio Details to Home Assistant")]
    public bool PushPortfolioDetailsToHomeAssistant { get; set; }
    [Display(Name = "Push Portfolio Position Details to Home Assistant")]
    public bool PushPositionDetailsToHomeAssistant { get; set; }

    // Web Scraper
    [Display(Name = "Enable Web Scraper")]
    public bool ScraperEnabled { get; set; }
    [Display(Name = "Scrape Interval (minutes)")]
    public int ScraperIntervalMinutes { get; set; } = 360;
    [Display(Name = "Custom RSS Feed URLs (one per line)")]
    public string? ScraperRssFeeds { get; set; }

    // AI Monitoring
    [Display(Name = "Enable AI Monitoring")]
    public bool AiEnabled { get; set; }
    [Display(Name = "API Base URL")]
    public string? AiBaseUrl { get; set; }
    [Display(Name = "API Key")]
    public string? AiApiKey { get; set; }
    [Display(Name = "Model")]
    public string? AiModel { get; set; }
    [Display(Name = "Custom Analysis Prompt")]
    public string? AiAnalysisPrompt { get; set; }

    // Notifications - Email
    [Display(Name = "Enable Email Notifications")]
    public bool NotificationEmailEnabled { get; set; }
    [Display(Name = "SMTP Host")]
    public string? NotificationEmailSmtpHost { get; set; }
    [Display(Name = "SMTP Port")]
    public int NotificationEmailSmtpPort { get; set; } = 587;
    [Display(Name = "SMTP Username")]
    public string? NotificationEmailSmtpUser { get; set; }
    [Display(Name = "SMTP Password")]
    public string? NotificationEmailSmtpPassword { get; set; }
    [Display(Name = "From Address")]
    public string? NotificationEmailFromAddress { get; set; }
    [Display(Name = "To Address")]
    public string? NotificationEmailToAddress { get; set; }

    // Notifications - Webhook
    [Display(Name = "Enable Webhook Notifications")]
    public bool NotificationWebhookEnabled { get; set; }
    [Display(Name = "Webhook URL")]
    public string? NotificationWebhookUrl { get; set; }

    // Notifications - Home Assistant
    [Display(Name = "Enable Home Assistant Notifications")]
    public bool NotificationHomeAssistantEnabled { get; set; }
}
