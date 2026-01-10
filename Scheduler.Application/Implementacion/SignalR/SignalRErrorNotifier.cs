using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Scheduler.Application.Hubs;
using Scheduler.Application.Hubs.Interfaces;

namespace Scheduler.Application.Implementacion.Scheduler
{
    public class SignalRErrorNotifier : IErrorNotifier
    {
        private readonly IHubContext<ErrorHub> _hubContext;
        private readonly ILogger<SignalRErrorNotifier> _logger;

        public SignalRErrorNotifier(IHubContext<ErrorHub> hubContext, ILogger<SignalRErrorNotifier> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyErrorAsync(string message, object? details = null, string? connectionId = null, CancellationToken cancellationToken = default)
        {
            await SendAsync("ErrorOccured", new { message, details, timestamp = DateTimeOffset.UtcNow }, connectionId, cancellationToken).ConfigureAwait(false);
        }

        public async Task NotifyInfoAsync(string message, object? details = null, string? connectionId = null, CancellationToken cancellationToken = default)
        {
            await SendAsync("Info", new { message, details, timestamp = DateTimeOffset.UtcNow }, connectionId, cancellationToken).ConfigureAwait(false);
        }

        private async Task SendAsync(string eventName, object payload, string? connectionId, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync(eventName, payload, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync(eventName, payload, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación externa: no propagar
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando notificación SignalR ({Event})", eventName);
            }
        }
    }
}