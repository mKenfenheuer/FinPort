using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinPort.Models;

namespace FinPort.Services;

public class HomeAssistantApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public HomeAssistantApiClient(IConfiguration configuration)
    {
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(configuration.GetValue<string>("HomeAssistant:Url") ?? "http://supervisor/core"),
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", configuration.GetValue<string>("HomeAssistant:Token") ?? Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN"))
            }
        };
        _configuration = configuration;
    }

    public async Task PushPortfolioDetailsAsync(Portfolio portfolio)
    {
        if (portfolio.Id == null)
            return;

        var changeSensor = new HomeAssistantSensor()
        {
            EntityId = $"sensor.finport_portfolio_{portfolio.Id.Replace("-", "")}_change",
            State = portfolio.Change.ToJsonNumberFormat(),
            Attributes = new Dictionary<string, string>
            {
                { "unit_of_measurement", "%" },
                { "friendly_name", $"{portfolio.Name} Change" },
                { "icon", "mdi:percent" }
            }
        };
        var valueSensor = new HomeAssistantSensor()
        {
            EntityId = $"sensor.finport_portfolio_{portfolio.Id.Replace("-", "")}_value",
            State = portfolio.Value?.ToJsonNumberFormat(),
            Attributes = new Dictionary<string, string>
            {
                { "unit_of_measurement", _configuration.GetValue<string>("CurrencySymbol") ?? "€" },
                { "friendly_name", $"{portfolio.Name} Value" },
                { "icon", "mdi:cash" }
            }
        };
        await PushHomeAssistantSensorAsync(changeSensor);
        await PushHomeAssistantSensorAsync(valueSensor);
    }

    public async Task PushHomeAssistantSensorAsync(HomeAssistantSensor sensor)
    {
        var data = JsonSerializer.Serialize(sensor);
        var content = new StringContent(data, Encoding.UTF8, "application/json");
        await _httpClient.PostAsync($"api/states/{sensor.EntityId}", content);
    }

    public async Task PushPositionDetailsAsync(PortfolioPosition position)
    {
        if (position.Id == null)
            return;

        var changeSensor = new HomeAssistantSensor()
        {
            EntityId = $"sensor.finport_position_{position.ISIN}_change",
            State = position.Change.ToJsonNumberFormat(),
            Attributes = new Dictionary<string, string>
            {
                { "unit_of_measurement", "%" },
                { "friendly_name", $"{position.Name} Change" },
                { "icon", "mdi:percent" }
            }
        };
        var valueSensor = new HomeAssistantSensor()
        {
            EntityId = $"sensor.finport_position_{position.ISIN}_value",
            State = position.LastPrice.ToJsonNumberFormat(),
            Attributes = new Dictionary<string, string>
            {
                { "unit_of_measurement", _configuration.GetValue<string>("CurrencySymbol") ?? "€" },
                { "friendly_name", $"{position.Name} Value" },
                { "icon", "mdi:cash" }
            }
        };
        await PushHomeAssistantSensorAsync(changeSensor);
        await PushHomeAssistantSensorAsync(valueSensor);
    }
}