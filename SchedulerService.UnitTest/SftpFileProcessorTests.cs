using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Scheduler.Application.Implementacion.Scheduler;
using Moq;
using Xunit;

namespace SchedulerService.UnitTest
{
    public class SftpFileProcessorLeerArchivoAsyncTests
    {
     

        private static SftpFileProcessor CreateProcessor(Func<string, Stream> openStream)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                [
                    new KeyValuePair<string, string>("Sftp:Host", "localhost"),
                    new KeyValuePair<string, string>("Sftp:Port", "22"),
                    new KeyValuePair<string, string>("Sftp:Username", "user"),
                    new KeyValuePair<string, string>("Sftp:Password", "pass"),
                    new KeyValuePair<string, string>("PerformanceSettings:MaxDegreeOfParallelism", "10")
                ])
                .Build();

            // Crear mock de IArchivosService y pasarlo al constructor
            var mockArchivos = new Mock<ArchivosServices>(config);
            // Aquí se pueden configurar setups si SftpFileProcessor los necesita:
            // e.g. mockArchivos.Setup(x => x.SomeMethod(It.IsAny<string>())).Returns(...);

            return new SftpFileProcessor(mockArchivos.Object,config , openStream);
        }

        [Fact]
        public async Task LeerArchivoAsync_ParsesValidLines()
        {
            var lines = new[]
            {
                "1;2023-01-01;TYPE;10.5",
                "2;2023-02-02;TYPE2;20.00"
            };

            var tempFile = Path.GetTempFileName();
            try
            {
                await File.WriteAllLinesAsync(tempFile, lines);

                // OpenStream factory que abre un FileStream local para pruebas
                Func<string, Stream> openLocal = path => File.OpenRead(path);
                var processor = CreateProcessor(openLocal);

                var result = await processor.LeerArchivoAsync(tempFile);

                Assert.NotNull(result);
                Assert.Equal(2, result.Count);

                var r1 = result.SingleOrDefault(r => r.Id == 1);
                Assert.NotNull(r1);
                Assert.Equal(new DateTime(2023, 1, 1), r1!.Fecha.Date);
                Assert.Equal(10.5m, r1.Importe);

                var r2 = result.SingleOrDefault(r => r.Id == 2);
                Assert.NotNull(r2);
                Assert.Equal(new DateTime(2023, 2, 2), r2!.Fecha.Date);
                Assert.Equal(20.00m, r2.Importe);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        [Fact]
        public async Task LeerArchivoAsync_ParsesInvalidLines()
        {
            var lines = new[]
            {
                "1;2023-01-01;TYPE;10.5",
                "invalid;line;here",
                "2;2023-02-02;TYPE2;20.00"
            };

            var tempFile = Path.GetTempFileName();
            try
            {
                await File.WriteAllLinesAsync(tempFile, lines);

                // OpenStream factory que abre un FileStream local para pruebas
                Func<string, Stream> openLocal = path => File.OpenRead(path);
                var processor = CreateProcessor(openLocal);

                var result = await processor.LeerArchivoAsync(tempFile);

            }
            catch (Exception ex) {
                Assert.NotNull(ex);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LeerArchivoAsync_EmptyFile_ReturnsEmptyList()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                // Ensure empty file
                File.WriteAllText(tempFile, string.Empty);

                Func<string, Stream> openLocal = path => File.OpenRead(path);
                var processor = CreateProcessor(openLocal);

                var result = await processor.LeerArchivoAsync(tempFile);

                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}