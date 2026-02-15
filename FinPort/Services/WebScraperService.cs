using FinPort.Data;
using FinPort.Models;
using Microsoft.EntityFrameworkCore;
using System.ServiceModel.Syndication;
using System.Xml;

namespace FinPort.Services;

public class WebScraperService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebScraperService> _logger;
    private Timer? _timer;

    public WebScraperService(
        IServiceScopeFactory serviceScopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebScraperService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _httpClientFactory = httpClientFactory;
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
        var enabled = await db.GetSettingAsync("Scraper:Enabled", false);
        var intervalMinutes = await db.GetSettingAsync("Scraper:IntervalMinutes", 360);

        if (enabled)
        {
            _timer?.Dispose();
            _timer = new Timer(async _ => await RunScrapeAsync(), null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(intervalMinutes));
            _logger.LogInformation("Web scraper scheduled every {Interval} minutes", intervalMinutes);
        }
        else
        {
            _logger.LogInformation("Web scraper is disabled");
        }
    }

    public async Task RunScrapeAsync()
    {
        try
        {
            _logger.LogInformation("Starting web scrape...");

            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

            var enabled = await db.GetSettingAsync("Scraper:Enabled", false);
            if (!enabled) return;

            var positions = await db.PortfolioPositions.ToListAsync();
            var rssFeedsRaw = await db.GetSettingAsync("Scraper:RssFeeds", "");

            foreach (var position in positions)
            {
                await ScrapeGoogleNewsAsync(db, position);
            }

            if (!string.IsNullOrWhiteSpace(rssFeedsRaw))
            {
                var feeds = rssFeedsRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var feedUrl in feeds)
                {
                    await ScrapeRssFeedAsync(db, feedUrl);
                }
            }

            _logger.LogInformation("Web scrape completed. Processed {Count} positions", positions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during web scrape");
        }
    }

    private async Task ScrapeGoogleNewsAsync(DataBaseContext db, PortfolioPosition position)
    {
        try
        {
            var searchTerm = Uri.EscapeDataString($"{position.Name} {position.ISIN}");
            var url = $"https://news.google.com/rss/search?q={searchTerm}&hl=en";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FinPort/1.0)");

            using var stream = await client.GetStreamAsync(url);
            using var reader = XmlReader.Create(stream);
            var feed = SyndicationFeed.Load(reader);

            if (feed == null) return;

            foreach (var item in feed.Items.Take(10))
            {
                var articleUrl = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                if (string.IsNullOrEmpty(articleUrl)) continue;

                var exists = await db.ScrapedArticles.AnyAsync(a => a.Url == articleUrl);
                if (exists) continue;

                db.ScrapedArticles.Add(new ScrapedArticle
                {
                    PositionId = position.Id,
                    Title = item.Title?.Text ?? "",
                    Url = articleUrl,
                    Summary = item.Summary?.Text ?? "",
                    Source = "Google News",
                    ScrapedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scrape Google News for position {PositionName}", position.Name);
        }
    }

    private async Task ScrapeRssFeedAsync(DataBaseContext db, string feedUrl)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FinPort/1.0)");

            using var stream = await client.GetStreamAsync(feedUrl);
            using var reader = XmlReader.Create(stream);
            var feed = SyndicationFeed.Load(reader);

            if (feed == null) return;

            foreach (var item in feed.Items.Take(20))
            {
                var articleUrl = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                if (string.IsNullOrEmpty(articleUrl)) continue;

                var exists = await db.ScrapedArticles.AnyAsync(a => a.Url == articleUrl);
                if (exists) continue;

                db.ScrapedArticles.Add(new ScrapedArticle
                {
                    Title = item.Title?.Text ?? "",
                    Url = articleUrl,
                    Summary = item.Summary?.Text ?? "",
                    Source = feedUrl,
                    ScrapedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scrape RSS feed {FeedUrl}", feedUrl);
        }
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
