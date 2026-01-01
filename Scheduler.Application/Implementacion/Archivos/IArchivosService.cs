using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Domain;

namespace Scheduler.Application.Implementacion.Scheduler
{
    public interface IArchivosService
    {
        RegistroTransaccion? ProcesarLinea(string linea);
    }
}
