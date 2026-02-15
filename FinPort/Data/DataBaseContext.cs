using System.Text.Json;
using FinPort.Models;
using Microsoft.EntityFrameworkCore;

namespace FinPort.Data;
public class DataBaseContext : DbContext
{
    public DataBaseContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<PortfolioPosition> PortfolioPositions { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<ScrapedArticle> ScrapedArticles { get; set; }
    public DbSet<AiAlert> AiAlerts { get; set; }

    public T? GetSetting<T>(string key, T defaultValue)
    {
        var setting = Settings.FirstOrDefault(s => s.Key == key);
        if (setting == null)
            return default;
        if (setting.Value == null)
            return default;
        return JsonSerializer.Deserialize<T>(setting.Value);
    }

    public async Task<T?> GetSettingAsync<T>(string key, T defaultValue)
    {
        var setting = await Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
            return default;
        if (setting.Value == null)
            return default;
        return JsonSerializer.Deserialize<T>(setting.Value);
    }

    public void SetSetting(string key, object value)
    {
        var setting = Settings.FirstOrDefault(s => s.Key == key);
        if (setting == null)
        {
            setting = new Setting { Key = key, Value = JsonSerializer.Serialize(value) };
            Settings.Add(setting);
        }
        else
        {
            setting.Value = JsonSerializer.Serialize(value);
        }
        SaveChanges();
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        var setting = await Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new Setting { Key = key, Value = JsonSerializer.Serialize(value) };
            Settings.Add(setting);
        }
        else
        {
            setting.Value = JsonSerializer.Serialize(value);
        }
        await SaveChangesAsync();
    }
}
