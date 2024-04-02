using System.Globalization;
using FinPort.Middleware;

namespace FinPort;

public static class Extensions
{
    public static IApplicationBuilder UseWebSocketMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<WebSocketMiddleware>();
    }

    public static string ToJsonNumberFormat(this double value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    public static string ToJsonNumberFormat(this double? value)
    {
        return (value ?? 0).ToJsonNumberFormat();
    }
}