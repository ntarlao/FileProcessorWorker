using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Application.Implementacion.Scheduler;

namespace SchedulerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IRabbitMqSubscriber _subscriber;
        private readonly ISftpProcessor _sftpProcessor;
        public Worker(ILogger<Worker> logger, IRabbitMqSubscriber subscriber, ISftpProcessor sftpProcessor)
        {
            _logger = logger;
            _subscriber = subscriber;
            _sftpProcessor = sftpProcessor;
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
                //await _processService.ProcessMessageAsync(request, CancellationToken.None);
                if (!string.IsNullOrEmpty(request.Path)) { 
                    var registros_validos = await _sftpProcessor.LeerArchivoAsync(request.Path, CancellationToken.None);

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