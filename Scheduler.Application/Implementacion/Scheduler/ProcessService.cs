using Microsoft.Extensions.Configuration;
using Scheduler.Application.Implementacion.Rabbit;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Scheduler.Application.Implementacion.Scheduler
{
    public class ProcessFilesService : IProcessFilesService
    {
        private readonly int MaxConcurrentExecutions = 10;
        private readonly SemaphoreSlim _semaphore;
        private readonly List<Task> _tasks = [];
        public ProcessFilesService(IConfiguration configuration)
        {
            MaxConcurrentExecutions = int.Parse(configuration.GetSection("PerformanceSettings").GetSection("MaxDegreeOfParallelism").Value ?? "10");
            _semaphore = new SemaphoreSlim(MaxConcurrentExecutions);
        }
        public static async Task CopyLargeFileAsync(string inputFilePath, string outputFilePath)
        {
            using FileStream inputStream = new(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using FileStream outputStream = new(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await inputStream.CopyToAsync(outputStream);
        }
        public async Task HeavyParallelProcessAsync(string filePath)
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = MaxConcurrentExecutions
            };
            ConcurrentBag<string> concurrentLines = [];
            var lines = File.ReadLinesAsync(filePath);

            await Parallel.ForEachAsync(lines, parallelOptions, async (line, ct) =>
            {
                // procesar la linea
                concurrentLines.Add(line);
            });
        }
        public async Task HeavyProcessingOfTheFileAsync(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                await _semaphore.WaitAsync();
                var line = await reader.ReadLineAsync();
                _tasks.Add(ProcessLineAsync(line ?? string.Empty));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.WhenAll(_tasks);
        }

        private async Task ProcessLineAsync(string line)
        {
            try
            {
                // procesar la linea
            }
            finally
            {
                _semaphore.Release();
            }
        }


        public async Task ProcessMessageAsync(RabbitMessage message, CancellationToken cancellationToken)
        {

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
    }
}
