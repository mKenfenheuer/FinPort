using System.Text.Json.Serialization;
public class HomeAssistantSensor
{
    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }

    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }

    [JsonPropertyName("last_changed")]
    public string? LastChanged { get; set; }

    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}