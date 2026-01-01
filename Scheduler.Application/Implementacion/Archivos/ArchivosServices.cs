using Microsoft.Extensions.Configuration;
using Scheduler.Domain;
using System.Globalization;

namespace Scheduler.Application.Implementacion.Scheduler
{
    public class ArchivosServices : IArchivosService
    {
        public ArchivosServices(IConfiguration configuration)
        {
        }
        public RegistroTransaccion? ProcesarLinea(string linea)
        {
            try
            {
                var campos = linea.Split(';');

                if (campos.Length < 4)
                    return null; // no tiene los campos mínimos

                if (!int.TryParse(campos[0], out int id)) return null;
                if (!DateTime.TryParseExact(campos[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime fecha)) return null;
                string tipo = campos[2];
                if (!decimal.TryParse(campos[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importe)) return null;

                var registro = new RegistroTransaccion
                {
                    Id = id,
                    Fecha = fecha,
                    TipoTransaccion = tipo,
                    Importe = importe
                };

                return registro;
            }
            catch
            {
                //return null;
                throw new OperationCanceledException();
            }
        }
    }
}
