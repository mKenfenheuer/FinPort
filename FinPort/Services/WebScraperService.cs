using FinPort.Data;
using FinPort.Models;
using HtmlAgilityPack;
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

            var feed = await FetchRssFeedAsync(url);
            if (feed == null) return;

            var contentClient = _httpClientFactory.CreateClient();
            contentClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FinPort/1.0)");
            contentClient.Timeout = TimeSpan.FromSeconds(15);

            foreach (var item in feed.Items.Take(10))
            {
                try
                {
                    var articleUrl = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                    if (string.IsNullOrEmpty(articleUrl)) continue;

                    var exists = await db.ScrapedArticles.AnyAsync(a => a.Url == articleUrl);
                    if (exists) continue;

                    var content = await FetchArticleContentAsync(contentClient, articleUrl);

                    db.ScrapedArticles.Add(new ScrapedArticle
                    {
                        PositionId = position.Id,
                        Title = item.Title?.Text ?? "",
                        Url = articleUrl,
                        Summary = item.Summary?.Text ?? "",
                        Content = content,
                        Source = "Google News",
                        ScrapedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to process article from Google News for {PositionName}", position.Name);
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to scrape Google News for position {PositionName}: {Message}", position.Name, ex.Message);
        }
    }

    private async Task ScrapeRssFeedAsync(DataBaseContext db, string feedUrl)
    {
        try
        {
            var feed = await FetchRssFeedAsync(feedUrl);
            if (feed == null) return;

            var contentClient = _httpClientFactory.CreateClient();
            contentClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FinPort/1.0)");
            contentClient.Timeout = TimeSpan.FromSeconds(15);

            foreach (var item in feed.Items.Take(20))
            {
                try
                {
                    var articleUrl = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                    if (string.IsNullOrEmpty(articleUrl)) continue;

                    var exists = await db.ScrapedArticles.AnyAsync(a => a.Url == articleUrl);
                    if (exists) continue;

                    var content = await FetchArticleContentAsync(contentClient, articleUrl);

                    db.ScrapedArticles.Add(new ScrapedArticle
                    {
                        Title = item.Title?.Text ?? "",
                        Url = articleUrl,
                        Summary = item.Summary?.Text ?? "",
                        Content = content,
                        Source = feedUrl,
                        ScrapedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to process article from feed {FeedUrl}", feedUrl);
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to scrape RSS feed {FeedUrl}: {Message}", feedUrl, ex.Message);
        }
    }

    private async Task<SyndicationFeed?> FetchRssFeedAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FinPort/1.0)");
        client.Timeout = TimeSpan.FromSeconds(30);

        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("RSS feed returned {StatusCode} for {Url}", response.StatusCode, url);
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = XmlReader.Create(stream);
        return SyndicationFeed.Load(reader);
    }

    private async Task<string?> FetchArticleContentAsync(HttpClient client, string url)
    {
        try
        {
            var html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script, style, nav, header, footer elements
            foreach (var node in doc.DocumentNode.SelectNodes("//script|//style|//nav|//header|//footer|//aside|//noscript") ?? Enumerable.Empty<HtmlNode>())
                node.Remove();

            // Try common article content selectors
            var articleNode = doc.DocumentNode.SelectSingleNode("//article")
                ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'article-body')]")
                ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'article-content')]")
                ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'story-body')]")
                ?? doc.DocumentNode.SelectSingleNode("//main")
                ?? doc.DocumentNode.SelectSingleNode("//body");

            if (articleNode == null) return null;

            var text = HtmlEntity.DeEntitize(articleNode.InnerText);
            // Normalize whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            // Cap at 5000 chars to keep storage reasonable
            if (text.Length > 5000)
                text = text.Substring(0, 5000);

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch article content from {Url}", url);
            return null;
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
