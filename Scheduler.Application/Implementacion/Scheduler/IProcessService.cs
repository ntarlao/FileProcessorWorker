using Scheduler.Application.Implementacion.Rabbit;

namespace Scheduler.Application.Implementacion.Scheduler
{
    public interface IProcessFilesService
    {
        Task ProcessMessageAsync(RabbitMessage message, CancellationToken cancellationToken);
    }
}
