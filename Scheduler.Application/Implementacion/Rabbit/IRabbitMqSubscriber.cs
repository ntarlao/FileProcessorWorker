namespace Scheduler.Application.Implementacion.Rabbit
{
    public interface IRabbitMqSubscriber
    {
        Task StartAsync(Func<string, Task> onMessage, CancellationToken stoppingToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
