namespace Scheduler.Infrastructure
{
    public static class MemoryScanner
    {
        public static int GetProcessorCount()
        {
           return Environment.ProcessorCount > 1 ? Environment.ProcessorCount : 2;
        }
        public static int GetMaxDegreeOfParallelism()
        {
            return Math.Max(1, GetProcessorCount());
        }
        public static long GetMemoryLimitBytes()
        {
            // Intentar detectar cgroup (Linux containers)
            try
            {
                // cgroup v2
                const string cgroupV2Path = "/sys/fs/cgroup/memory.max";
                if (File.Exists(cgroupV2Path))
                {
                    var content = File.ReadAllText(cgroupV2Path).Trim();
                    if (!string.Equals(content, "max", StringComparison.OrdinalIgnoreCase) &&
                        long.TryParse(content, out var v2Limit))
                    {
                        return v2Limit;
                    }
                }

                // cgroup v1
                const string cgroupV1Path = "/sys/fs/cgroup/memory/memory.limit_in_bytes";
                if (File.Exists(cgroupV1Path))
                {
                    var content = File.ReadAllText(cgroupV1Path).Trim();
                    if (long.TryParse(content, out var v1Limit))
                    {
                        return v1Limit;
                    }
                }
            }
            catch
            {
            }

            // Si es windows: usar la estimación del GC
            try
            {
                var available = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                if (available > 0) return available;
            }
            catch
            {
            }

            // Último recurso: usar WorkingSet como aproximación
            try
            {
                return Environment.WorkingSet;
            }
            catch
            {
                // memoria estimada a 512MB si todo falla
                return 512L * 1024 * 1024;
            }
        }

        public static long CalculateReservedMemoryBytes(long totalMemoryBytes, double reserveFraction = 0.40, long minBytes = 50L * 1024 * 1024, long maxBytes = long.MaxValue)
        {
            var reserved = (long)(totalMemoryBytes * reserveFraction);

            // límites sensatos: no menos de minBytes y no más de maxBytes
            if (reserved < minBytes) reserved = minBytes;
            if (maxBytes > 0 && reserved > maxBytes) reserved = maxBytes;

            return reserved;
        }
        public static int CalculateChannelCapacity(int avgChars, long reservedMemoryBytes = 200L * 1024 * 1024)
        {
            const int overhead = 72;
            long estimatedBytesPerLine = (long)2 * avgChars + overhead;
            if (estimatedBytesPerLine <= 0) return 1000;
            long cap = reservedMemoryBytes / estimatedBytesPerLine;
            int minCap = 200;
            int maxCap = 50_000;
            return (int)Math.Clamp(cap, minCap, maxCap);
        }
    }
}
