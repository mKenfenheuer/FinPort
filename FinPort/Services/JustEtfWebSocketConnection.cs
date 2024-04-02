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
    private readonly ClientWebSocket _webSocket = new ClientWebSocket();
    private Task ReceiveTask { get; set; }
    public event EventHandler<MarketUpdate>? OnMarketUpdate;

    public JustEtfWebSocketConnection(IEnumerable<string> isin, string language = "de", string currency = "EUR", ILogger<JustEtfWebSocketConnection>? logger = null)
    {
        _isin = isin;
        _language = language;
        _currency = currency;
        _cancellationTokenSource = new CancellationTokenSource();
        _logger = logger;
        ReceiveTask = ReceiveAsync();
    }

    public async Task ReceiveAsync()
    {
        try
        {
            await _webSocket.ConnectAsync(new Uri($"wss://api.mobile.stock-data-subscriptions.justetf.com/?subscription=trend&parameters=isins:{String.Join(",", _isin)}/currency:{_currency}/language:{_language}"), _cancellationTokenSource.Token);

            while (!_cancellationTokenSource.IsCancellationRequested)
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
                            _logger?.LogError("Error while receiving message: {0}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Error while receiving message: {0}", ex.Message);
                }
            }

            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "WebSocket client stopped", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error while connecting to websocket: {0}", ex.Message);
        }

        await Task.Delay(1000);
        if (!_cancellationTokenSource.IsCancellationRequested)
            ReceiveTask = ReceiveAsync();
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
}