using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Application.Implementacion.Scheduler;
using Scheduler.Application.Hubs.Interfaces;

namespace SchedulerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IRabbitMqSubscriber _subscriber;
        private readonly ISftpFileProcessor _sftpProcessor;
        private readonly IErrorNotifier _notifier;

        public Worker(ILogger<Worker> logger, IRabbitMqSubscriber subscriber, ISftpFileProcessor sftpProcessor, IErrorNotifier notifier)
        {
            _logger = logger;
            _subscriber = subscriber;
            _sftpProcessor = sftpProcessor;
            _notifier = notifier;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            try
            {
                await _subscriber.StartAsync(HandleMessageAsync, stoppingToken).ConfigureAwait(false);
                _logger.LogInformation("Worker iniciado y suscrito a RabbitMQ.");
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _subscriber.StopAsync(cancellationToken).ConfigureAwait(false);
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleMessageAsync(string message)
        {
            try
            {
                RabbitMessage? request = System.Text.Json.JsonSerializer.Deserialize<RabbitMessage>(message);
                if (request is null)
                {
                    _logger.LogError("No se puede deserializar el mensaje: {Message}", message);
                    return;
                }

                _logger.LogInformation("Procesando mensaje: {Message}", message);

                var connectionId = request.ConnectionId;

                //await _notifier.NotifyInfoAsync("Procesamiento iniciado", new { path = request.Path }, connectionId).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(request.Path))
                {
                    try
                    {
                        var registros_validos = await _sftpProcessor.LeerArchivoAsync(request.Path!, connectionId, CancellationToken.None).ConfigureAwait(false);

                        //await _notifier.NotifyInfoAsync("Procesamiento finalizado", new { path = request.Path, registros = registros_validos.Count }, connectionId).ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando archivo: {Path}", request.Path);
                        //await _notifier.NotifyErrorAsync("Error procesando archivo", new { path = request.Path, exception = ex.ToString() }, connectionId).ConfigureAwait(false);
                    }
                }
            }
            catch
            {
                _logger.LogError("No se puede procesar el mensaje: {Message}", message);
            }
            await Task.CompletedTask;
        }
    }
}