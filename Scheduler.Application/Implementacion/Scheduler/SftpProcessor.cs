using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using Scheduler.Domain;
using Scheduler.Infrastructure;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace Scheduler.Application.Implementacion.Scheduler
{

    public class SftpFileProcessor : ISftpProcessor
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly Func<string, Stream> _openRemoteStream;
        private readonly IArchivosService _archivosService;
        public SftpFileProcessor(IArchivosService archivosService, IConfiguration configuration, Func<string, Stream>? openRemoteStream = null)
        {
            _archivosService = archivosService;
            var sftpconfig = configuration.GetSection("Sftp");
            _host = sftpconfig["Host"]?.Trim() ?? string.Empty;
            _username = sftpconfig["Username"]?.Trim() ?? string.Empty;
            _password = sftpconfig["Password"]?.Trim() ?? string.Empty;

            var portText = sftpconfig["Port"]?.Trim();
            if (!int.TryParse(portText, out _port))
                _port = 22;

            // Si se proporciona un factory para abrir streams remotos se usa (para tests).
            // En caso contrario se usa un implementation por defecto que abre con SftpClient.
            _openRemoteStream = openRemoteStream ?? DefaultOpenRemoteStream;
        }

        private Stream DefaultOpenRemoteStream(string remotePath)
        {
            var client = new SftpClient(_host, _port, _username, _password);
            client.Connect();
            var stream = client.OpenRead(remotePath);
            
            return stream is null ? 
                throw new FileNotFoundException("No existe el archivo para procesar") : 
                (Stream)new OwnedStream(stream, client);
        }

        // Stream que encapsula el stream real y libera también el propietario (SftpClient).
        private sealed class OwnedStream : Stream
        {
            private readonly Stream _inner;
            private readonly IDisposable? _owner;

            public OwnedStream(Stream inner, IDisposable? owner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _owner = owner;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }

            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default)
                => await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
                => await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

            public override async Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
                => await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        _inner.Dispose();
                        _owner?.Dispose();
                    }
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

       

        public async Task<List<RegistroTransaccion>> LeerArchivoAsync(string remoteFilePath, CancellationToken cancellationToken = default)
        {
            var registrosValidos = new ConcurrentBag<RegistroTransaccion>();
            var total = MemoryScanner.GetMemoryLimitBytes();
            var reservedMemoryBytes = MemoryScanner.CalculateReservedMemoryBytes(total, reserveFraction: 0.40);
            // Estimación de chars por línea
            int avgChars = 2000;
            var channelCapacity = MemoryScanner.CalculateChannelCapacity(avgChars, reservedMemoryBytes);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ctsToken = linkedCts.Token;

            ExceptionDispatchInfo? firstException = null;
            var exceptionLock = new object();

            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(channelCapacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            var processorCount = MemoryScanner.GetProcessorCount();
            var degreeOfParallelism = MemoryScanner.GetMaxDegreeOfParallelism();
            var consumers = new List<Task>(degreeOfParallelism);
            for (int i = 0; i < degreeOfParallelism; i++)
            {
                consumers.Add(Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var linea in channel.Reader.ReadAllAsync(ctsToken))
                        {
                            if (ctsToken.IsCancellationRequested) break;

                            var registro = _archivosService.ProcesarLinea(linea);
                            if (registro != null)
                                registrosValidos.Add(registro);
                            else
                                throw new Exception("Archivo inválido");
                        }
                    }
                    catch (OperationCanceledException) when (ctsToken.IsCancellationRequested)
                    {
                        // Cancelación esperada: salir silenciosamente
                    }
                    catch (Exception ex)
                    {
                        // Capturar la primera excepción y propagar la cancelación.
                        lock (exceptionLock)
                        {
                            firstException ??= ExceptionDispatchInfo.Capture(ex);
                        }
                        // Intentar completar el writer con la excepción para desbloquear al productor/otros consumidores.
                        registrosValidos.Clear();
                        channel.Writer.TryComplete(ex);
                        linkedCts.Cancel();
                    }
                }, CancellationToken.None));
            }

            // Abrir stream remoto mediante el factory inyectado (por defecto usa SftpClient)
            try
            {
                using var stream = _openRemoteStream(remoteFilePath);
                using var reader = new StreamReader(stream);
                string? linea;
                while (!ctsToken.IsCancellationRequested)
                {
                    // StreamReader.ReadLineAsync no acepta token; salimos si se cancela antes de intentar leer.
                    linea = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (linea == null) break;

                    try
                    {
                        await channel.Writer.WriteAsync(linea, ctsToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ctsToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Error al escribir en el canal: capturar, cancelar y completar para detener todo.
                        lock (exceptionLock)
                        {
                            firstException ??= ExceptionDispatchInfo.Capture(ex);
                        }
                        channel.Writer.TryComplete(ex);
                        linkedCts.Cancel();
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (ctsToken.IsCancellationRequested)
            {
                // cancelación esperada
            }
            catch (Exception ex)
            {
                // Excepción al abrir/leer el stream remoto.
                lock (exceptionLock)
                {
                    firstException ??= ExceptionDispatchInfo.Capture(ex);
                }
                channel.Writer.TryComplete(ex);
                linkedCts.Cancel();
            }
            finally
            {
                // Indicar fin de escritura si aún no se hizo por un error.
                channel.Writer.TryComplete();
            }

            // Esperar consumidores
            await Task.WhenAll(consumers).ConfigureAwait(false);

            // Si hubo excepción, relanzarla preservando la pila.
            if (firstException != null)
            {
                firstException.Throw();
            }

            return [.. registrosValidos];
        }

        
    }
}