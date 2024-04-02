using System.Text.Json.Serialization;

namespace FinPort.Models.WebSocket;
public class Ask
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class Bid
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class DtdAmt
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class DtdDec
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class DtdPrc
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class Last
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class Mid
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class MarketUpdate
{
    [JsonPropertyName("isin")]
    public string? Isin { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("trend")]
    public string? Trend { get; set; }

    [JsonPropertyName("ask")]
    public Ask? Ask { get; set; }

    [JsonPropertyName("bid")]
    public Bid? Bid { get; set; }

    [JsonPropertyName("mid")]
    public Mid? Mid { get; set; }

    [JsonPropertyName("last")]
    public Last? Last { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("dtdDec")]
    public DtdDec? DtdDec { get; set; }

    [JsonPropertyName("dtdPrc")]
    public DtdPrc? DtdPrc { get; set; }

    [JsonPropertyName("dtdAmt")]
    public DtdAmt? DtdAmt { get; set; }

    [JsonPropertyName("spreadAmt")]
    public SpreadAmt? SpreadAmt { get; set; }

    [JsonPropertyName("spreadDec")]
    public SpreadDec? SpreadDec { get; set; }

    [JsonPropertyName("spreadPrc")]
    public SpreadPrc? SpreadPrc { get; set; }

    [JsonPropertyName("stockExchange")]
    public string? StockExchange { get; set; }

    [JsonPropertyName("quoteType")]
    public string? QuoteType { get; set; }
}

public class SpreadAmt
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class SpreadDec
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

public class SpreadPrc
{
    [JsonPropertyName("raw")]
    public double Value { get; set; }
}

