using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Domain;

namespace Scheduler.Application.Implementacion.Scheduler
{
    public interface ISftpProcessor
    {
        Task<List<RegistroTransaccion>> LeerArchivoAsync(string remoteFilePath, CancellationToken cancellationToken);
    }
}
