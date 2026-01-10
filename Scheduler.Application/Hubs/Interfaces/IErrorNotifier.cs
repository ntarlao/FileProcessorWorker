using System.Threading;
using System.Threading.Tasks;

namespace Scheduler.Application.Hubs.Interfaces
{
    public interface IErrorNotifier
    {
        Task NotifyErrorAsync(string message, object? details = null, string? connectionId = null, CancellationToken cancellationToken = default);
        Task NotifyInfoAsync(string message, object? details = null, string? connectionId = null, CancellationToken cancellationToken = default);
    }
}