using FinPort.Data;
using FinPort.Models;
using FinPort.Models.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FinPort.Services
{
    public class JustEtfWebSocketClient : IHostedService
    {
        private readonly Dictionary<string, JustEtfWebSocketConnection> _webSockets;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly HomeAssistantApiClient _homeAssistantApiClient;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<JustEtfWebSocketClient> _logger;
        private readonly string _dbLock = "dblock";

        public JustEtfWebSocketClient(IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory, IConfiguration configuration, HomeAssistantApiClient homeAssistantApiClient, WebSocketHandler webSocketHandler, ILogger<JustEtfWebSocketClient> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _webSockets = new Dictionary<string, JustEtfWebSocketConnection>();
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _configuration = configuration;
            _homeAssistantApiClient = homeAssistantApiClient;
            _webSocketHandler = webSocketHandler;
            _logger = logger;

            using (var scope = _serviceScopeFactory.CreateScope())
            using (var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>())
            {
                var positions = db.PortfolioPositions.ToList();
                foreach (var position in positions)
                    if (position.ISIN != null)
                    {
                        AddISIN(position.ISIN, _configuration.GetValue<string>("Currency"), _configuration.GetValue<string>("Language"));
                    }
            }
        }

        public void AddISIN(string ISIN, string? currency = null, string? language = null)
        {
            if (currency == null)
                currency = _configuration.GetValue<string>("Currency");
            if (language == null)
                language = _configuration.GetValue<string>("Language");

            if (!_webSockets.ContainsKey(ISIN))
            {
                var webSocket = new JustEtfWebSocketConnection(new List<string> { ISIN }, language ?? "de", currency ?? "EUR", _loggerFactory.CreateLogger<JustEtfWebSocketConnection>());
                webSocket.OnMarketUpdate += (sender, update) => _ = OnMarketUpdate(sender, update);
                _webSockets.Add(ISIN, webSocket);
            }
        }

        public void AddISIN(IEnumerable<string> ISIN, string? currency = null, string? language = null)
        {
            if (currency == null)
                currency = _configuration.GetValue<string>("Currency");
            if (language == null)
                language = _configuration.GetValue<string>("Language");

            var isins = ISIN.Where(i => !_webSockets.ContainsKey(i)).ToList();

            if (isins.Count() > 0)
            {
                var webSocket = new JustEtfWebSocketConnection(isins, language ?? "de", currency ?? "EUR", _loggerFactory.CreateLogger<JustEtfWebSocketConnection>());
                webSocket.OnMarketUpdate += (sender, update) => _ = OnMarketUpdate(sender, update);
                foreach (var isin in isins)
                    _webSockets.Add(isin, webSocket);
            }
        }

        public void RemoveISIN(string ISIN)
        {
            if (_webSockets.ContainsKey(ISIN))
            {
                _webSockets[ISIN].Stop();
                _webSockets.Remove(ISIN);
            }
        }

        private async Task OnMarketUpdate(object? sender, MarketUpdate marketUpdate)
        {
            if (marketUpdate == null)
                return;

            if (marketUpdate?.Isin == null)
                return;

            var isin = marketUpdate.Isin ?? "";
            var value = marketUpdate?.Bid?.Value ?? 0;
            if (value == 0)
                return;

            _logger.LogDebug($"Received market update for ISIN {isin}: {value}");

            lock (_dbLock)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                using (var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>())
                {
                    var position = db.PortfolioPositions.Include(p => p.Portfolio).ThenInclude(p => p.Positions).FirstOrDefault(p => p.ISIN == isin);
                    if (position != null && position.LastPrice != value)
                    {
                        position.LastPrice = value;
                        position.LastPriceDate = DateTime.Now;
                        db.SaveChanges();

                        NotifyWebsocketsAsync(position).Wait();

                        var pushPortfolio = db.GetSetting("PushPortfolioDetailsToHomeAssistant", false);
                        if (position.Portfolio != null && pushPortfolio)
                            _ = _homeAssistantApiClient.PushPortfolioDetailsAsync(position.Portfolio);
                        var pushPosition = db.GetSetting("PushPositionDetailsToHomeAssistant", false);
                        if (position != null && pushPosition)
                            _ = _homeAssistantApiClient.PushPositionDetailsAsync(position);
                    }
                }
            }
        }

        private async Task NotifyWebsocketsAsync(PortfolioPosition position)
        {
            await _webSocketHandler.SendMessage(new UpdateMessage()
            {
                Id = position.Id,
                Type = "position",
                Field = "value",
                Value = position.Value
            });

            await _webSocketHandler.SendMessage(new UpdateMessage()
            {
                Id = position.Id,
                Type = "position",
                Field = "change",
                Value = position.Change
            });

            if (position.Portfolio?.Value != null)
                await _webSocketHandler.SendMessage(new UpdateMessage()
                {
                    Id = position.Id,
                    Type = "portfolio",
                    Field = "value",
                    Value = position.Portfolio.Value ?? 0
                });

            if (position.Portfolio?.Change != null)
                await _webSocketHandler.SendMessage(new UpdateMessage()
                {
                    Id = position.Id,
                    Type = "portfolio",
                    Field = "change",
                    Value = position.Portfolio.Change ?? 0
                });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}