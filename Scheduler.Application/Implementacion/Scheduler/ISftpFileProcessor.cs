using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Domain;

namespace Scheduler.Application.Implementacion.Scheduler
{
    public interface ISftpFileProcessor
    {
        Task<List<RegistroTransaccion>> LeerArchivoAsync(string remoteFilePath, string? connectionId = null, CancellationToken cancellationToken = default);
    }
}
