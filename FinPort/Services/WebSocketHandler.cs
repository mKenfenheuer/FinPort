using System.Net.WebSockets;
using System.Text.Json;

namespace FinPort.Services;

public class WebSocketHandler
{
    private readonly List<ClientWebSocketHandler> _handlers = new();

    public async Task HandleWebSocket(WebSocket webSocket)
    {
        var handler = new ClientWebSocketHandler(webSocket);
        _handlers.Add(handler);
        await handler.Handle();
    }

    public async Task SendMessage<T>(T message)
    {
        var tasks = new List<Task>();
        string msg = JsonSerializer.Serialize(message);
        foreach (var handler in _handlers)
        {
            tasks.Add(handler.SendMessage(msg));
        }

        await Task.WhenAll(tasks);
    }
}
