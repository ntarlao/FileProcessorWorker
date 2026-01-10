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
                var observacionFinal = string.Empty;
                if (campos.Length < 4)
                    return null; // no tiene los campos mínimos

                if (!int.TryParse(campos[0], out int id))
                    observacionFinal = "campo id invalido";
                if (!DateTime.TryParseExact(campos[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                    observacionFinal = "campo fecha invalido";
                string tipo = campos[2];
                if (!decimal.TryParse(campos[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importe))
                    observacionFinal = "campo importe invalido";

                var registro = new RegistroTransaccion
                {
                    Id = id,
                    Fecha = fecha,
                    TipoTransaccion = tipo,
                    Importe = importe,
                    Observacion = observacionFinal
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
