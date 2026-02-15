using FinPort.Data;
using FinPort.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace FinPort.Services;

public class AiMonitoringService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NotificationService _notificationService;
    private readonly ILogger<AiMonitoringService> _logger;
    private Timer? _timer;

    public AiMonitoringService(
        IServiceScopeFactory serviceScopeFactory,
        IHttpClientFactory httpClientFactory,
        NotificationService notificationService,
        ILogger<AiMonitoringService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _httpClientFactory = httpClientFactory;
        _notificationService = notificationService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = ScheduleNextRun();
        return Task.CompletedTask;
    }

    private async Task ScheduleNextRun()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var enabled = await db.GetSettingAsync("AI:Enabled", false);
        var intervalMinutes = await db.GetSettingAsync("Scraper:IntervalMinutes", 360);

        if (enabled)
        {
            _timer?.Dispose();
            // Offset by 5 minutes after scraper to let it finish first
            _timer = new Timer(async _ => await RunAnalysisAsync(), null,
                TimeSpan.FromMinutes(6),
                TimeSpan.FromMinutes(intervalMinutes));
            _logger.LogInformation("AI monitoring scheduled every {Interval} minutes (offset by 5min from scraper)", intervalMinutes);
        }
        else
        {
            _logger.LogInformation("AI monitoring is disabled");
        }
    }

    public async Task RunAnalysisAsync()
    {
        try
        {
            _logger.LogInformation("Starting AI analysis...");

            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

            var enabled = await db.GetSettingAsync("AI:Enabled", false);
            if (!enabled) return;

            var baseUrl = await db.GetSettingAsync("AI:BaseUrl", "") ?? "";
            var apiKey = await db.GetSettingAsync("AI:ApiKey", "") ?? "";
            var model = await db.GetSettingAsync("AI:Model", "gpt-4o") ?? "gpt-4o";
            var customPrompt = await db.GetSettingAsync("AI:AnalysisPrompt", "") ?? "";

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("AI base URL or API key not configured");
                return;
            }

            var portfolios = await db.Portfolios.Include(p => p.Positions).ToListAsync();
            var recentArticles = await db.ScrapedArticles
                .Where(a => a.ScrapedAt > DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(a => a.ScrapedAt)
                .Take(50)
                .ToListAsync();

            if (!portfolios.Any()) return;

            var prompt = BuildAnalysisPrompt(portfolios, recentArticles, customPrompt);
            var response = await CallAiApiAsync(baseUrl, apiKey, model, prompt);

            if (response == null) return;

            var alerts = ParseAiResponse(response, portfolios, db);
            foreach (var alert in alerts)
            {
                db.AiAlerts.Add(alert);
                await db.SaveChangesAsync();
                await _notificationService.SendAlertAsync(alert);
            }

            _logger.LogInformation("AI analysis completed. Generated {Count} alerts", alerts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI analysis");
        }
    }

    private string BuildAnalysisPrompt(List<Portfolio> portfolios, List<ScrapedArticle> articles, string customPrompt)
    {
        var sb = new StringBuilder();

        var systemInstruction = string.IsNullOrWhiteSpace(customPrompt)
            ? "You are a financial portfolio risk analyst. Analyze the following portfolio data and recent news to identify positions at risk of losing value. For each risk identified, provide a JSON array of alerts."
            : customPrompt;

        sb.AppendLine(systemInstruction);
        sb.AppendLine();
        sb.AppendLine("## Portfolio Data");

        foreach (var portfolio in portfolios)
        {
            sb.AppendLine($"### Portfolio: {portfolio.Name}");
            if (portfolio.Positions != null)
            {
                foreach (var pos in portfolio.Positions)
                {
                    sb.AppendLine($"- {pos.Name} (ISIN: {pos.ISIN}): Qty={pos.Quantity}, PurchasePrice={pos.PurchasePrice:F2}, LastPrice={pos.LastPrice:F2}, Change={pos.Change:F2}%");
                }
            }
        }

        if (articles.Any())
        {
            sb.AppendLine();
            sb.AppendLine("## Recent News");
            foreach (var article in articles)
            {
                sb.AppendLine($"- [{article.Source}] {article.Title}");
                if (!string.IsNullOrWhiteSpace(article.Content))
                    sb.AppendLine($"  Content: {article.Content.Substring(0, Math.Min(article.Content.Length, 500))}");
                else if (!string.IsNullOrWhiteSpace(article.Summary))
                    sb.AppendLine($"  Summary: {article.Summary.Substring(0, Math.Min(article.Summary.Length, 200))}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Analyze the data and identify positions at risk. For each risk, provide the position name, severity, a short title, and detailed analysis. If no risks found, return an empty alerts array.");

        return sb.ToString();
    }

    private static object BuildJsonSchema()
    {
        return new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "portfolio_alerts",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["alerts"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["positionName"] = new { type = "string", description = "Name of the position at risk" },
                                    ["severity"] = new { type = "string", @enum = new[] { "Info", "Warning", "Critical" } },
                                    ["title"] = new { type = "string", description = "Short summary of the risk" },
                                    ["analysis"] = new { type = "string", description = "Detailed explanation of the risk" }
                                },
                                required = new[] { "positionName", "severity", "title", "analysis" },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[] { "alerts" },
                    additionalProperties = false
                }
            }
        };
    }

    private async Task<string?> CallAiApiAsync(string baseUrl, string apiKey, string model, string prompt)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var endpoint = baseUrl.TrimEnd('/') + "/v1/chat/completions";
            var requestBody = JsonSerializer.Serialize(new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                response_format = BuildJsonSchema()
            });

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return messageContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call AI API at {BaseUrl}", baseUrl);
            return null;
        }
    }

    private List<AiAlert> ParseAiResponse(string response, List<Portfolio> portfolios, DataBaseContext db)
    {
        var alerts = new List<AiAlert>();
        try
        {
            using var doc = JsonDocument.Parse(response);
            var alertsArray = doc.RootElement.GetProperty("alerts");

            foreach (var element in alertsArray.EnumerateArray())
            {
                var positionName = element.GetProperty("positionName").GetString() ?? "";
                var severityStr = element.GetProperty("severity").GetString() ?? "Info";
                var title = element.GetProperty("title").GetString() ?? "";
                var analysis = element.GetProperty("analysis").GetString() ?? "";

                if (!Enum.TryParse<AlertSeverity>(severityStr, true, out var severity))
                    severity = AlertSeverity.Info;

                PortfolioPosition? matchedPosition = null;
                Portfolio? matchedPortfolio = null;
                foreach (var portfolio in portfolios)
                {
                    matchedPosition = portfolio.Positions?.FirstOrDefault(p =>
                        p.Name != null && p.Name.Equals(positionName, StringComparison.OrdinalIgnoreCase));
                    if (matchedPosition != null)
                    {
                        matchedPortfolio = portfolio;
                        break;
                    }
                }

                alerts.Add(new AiAlert
                {
                    PortfolioId = matchedPortfolio?.Id,
                    Portfolio = matchedPortfolio,
                    PositionId = matchedPosition?.Id,
                    Position = matchedPosition,
                    Severity = severity,
                    Title = title,
                    Analysis = analysis,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsNotified = false
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response: {Response}", response.Substring(0, Math.Min(response.Length, 500)));
        }

        return alerts;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
