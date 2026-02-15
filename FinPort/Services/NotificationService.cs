using FinPort.Data;
using FinPort.Models;
using MailKit.Net.Smtp;
using MimeKit;
using System.Text;
using System.Text.Json;

namespace FinPort.Services;

public class NotificationService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HomeAssistantApiClient _homeAssistantApiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IServiceScopeFactory serviceScopeFactory,
        HomeAssistantApiClient homeAssistantApiClient,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _homeAssistantApiClient = homeAssistantApiClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAlertAsync(AiAlert alert)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var emailEnabled = await db.GetSettingAsync("Notification:Email:Enabled", false);
        if (emailEnabled)
            await SendEmailAsync(db, alert);

        var webhookEnabled = await db.GetSettingAsync("Notification:Webhook:Enabled", false);
        if (webhookEnabled)
            await SendWebhookAsync(db, alert);

        var haEnabled = await db.GetSettingAsync("Notification:HomeAssistant:Enabled", false);
        if (haEnabled)
            await SendHomeAssistantAsync(alert);

        alert.IsNotified = true;
        db.AiAlerts.Update(alert);
        await db.SaveChangesAsync();
    }

    private async Task SendEmailAsync(DataBaseContext db, AiAlert alert)
    {
        try
        {
            var host = await db.GetSettingAsync("Notification:Email:SmtpHost", "");
            var port = await db.GetSettingAsync("Notification:Email:SmtpPort", 587);
            var user = await db.GetSettingAsync("Notification:Email:SmtpUser", "");
            var password = await db.GetSettingAsync("Notification:Email:SmtpPassword", "");
            var from = await db.GetSettingAsync("Notification:Email:FromAddress", "");
            var to = await db.GetSettingAsync("Notification:Email:ToAddress", "");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(to))
                return;

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = $"[FinPort {alert.Severity}] {alert.Title}";
            message.Body = new TextPart("plain")
            {
                Text = $"Severity: {alert.Severity}\n" +
                       $"Position: {alert.Position?.Name ?? "N/A"}\n" +
                       $"Portfolio: {alert.Portfolio?.Name ?? "N/A"}\n\n" +
                       $"{alert.Analysis}"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.Auto);
            if (!string.IsNullOrEmpty(user))
                await client.AuthenticateAsync(user, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email notification sent for alert {AlertId}", alert.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification for alert {AlertId}", alert.Id);
        }
    }

    private async Task SendWebhookAsync(DataBaseContext db, AiAlert alert)
    {
        try
        {
            var url = await db.GetSettingAsync("Notification:Webhook:Url", "") ?? "";
            if (string.IsNullOrEmpty(url))
                return;

            var client = _httpClientFactory.CreateClient();
            var payload = JsonSerializer.Serialize(new
            {
                severity = alert.Severity.ToString(),
                title = alert.Title,
                analysis = alert.Analysis,
                position = alert.Position?.Name,
                portfolio = alert.Portfolio?.Name,
                createdAt = alert.CreatedAt
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Webhook notification sent for alert {AlertId}", alert.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook notification for alert {AlertId}", alert.Id);
        }
    }

    private async Task SendHomeAssistantAsync(AiAlert alert)
    {
        try
        {
            await _homeAssistantApiClient.SendNotificationAsync(
                $"[{alert.Severity}] {alert.Title}",
                alert.Analysis ?? "");

            _logger.LogInformation("Home Assistant notification sent for alert {AlertId}", alert.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Home Assistant notification for alert {AlertId}", alert.Id);
        }
    }
}
