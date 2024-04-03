using System.Net.WebSockets;
using System.Text.Json;
using FinPort.Models.WebSocket;

namespace FinPort.Services;

public class JustEtfWebSocketConnection
{
    private readonly ILogger<JustEtfWebSocketConnection>? _logger;
    private readonly IEnumerable<string> _isin;
    private readonly string _language;
    private readonly string _currency;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private ClientWebSocket _webSocket;
    private Task ReceiveTask { get; set; }
    public event EventHandler<MarketUpdate>? OnMarketUpdate;

    public JustEtfWebSocketConnection(IEnumerable<string> isin, string language = "de", string currency = "EUR", ILogger<JustEtfWebSocketConnection>? logger = null)
    {
        _isin = isin;
        _language = language;
        _currency = currency;
        _cancellationTokenSource = new CancellationTokenSource();
        _logger = logger;
        _webSocket = new ClientWebSocket();
        ReceiveTask = ReceiveAsync();
    }

    public async Task ReceiveAsync()
    {
        _logger?.LogInformation($"WebSocket started for ISINs: {String.Join(",", _isin)}");
        try
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Origin", "https://www.justetf.com");
            _webSocket.Options.SetRequestHeader("User-Agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0");
            await _webSocket.ConnectAsync(new Uri($"wss://api.mobile.stock-data-subscriptions.justetf.com/?subscription=trend&parameters=isins:{String.Join(",", _isin)}/currency:{_currency}/language:{_language}"), _cancellationTokenSource.Token);

            while (!_cancellationTokenSource.IsCancellationRequested && !_webSocket.CloseStatus.HasValue)
            {
                try
                {
                    var buffer = new byte[1024];
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            var marketUpdate = JsonSerializer.Deserialize<MarketUpdate>(message);
                            if (marketUpdate != null)
                                OnMarketUpdate?.Invoke(this, marketUpdate);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError("Error while receiving message: {0}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Error while receiving message: {0}", ex);
                }
            }

            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "WebSocket client stopped", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error while connecting to websocket: {0}", ex.Message);
        }

        _logger?.LogInformation($"WebSocket stopped for ISINs: {String.Join(",", _isin)}");

        await Task.Delay(1000);
        if (!_cancellationTokenSource.IsCancellationRequested)
            ReceiveTask = ReceiveAsync();
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
}