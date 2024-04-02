
using FinPort.Services;

namespace FinPort.Middleware;

public class WebSocketMiddleware : IMiddleware
{
    private readonly WebSocketHandler _webSocketHandler;

    public WebSocketMiddleware(WebSocketHandler webSocketHandler)
    {
        _webSocketHandler = webSocketHandler;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            if(!context.Request.Path.ToString().EndsWith("/api/websocket"))
            {
                context.Response.StatusCode = 400;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.HandleWebSocket(webSocket);
        }
        else
        {
            await next(context);
        }
    }
}