//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Threading;

/**
  * Configures process-wide runtime concurrency for server executables.
  * .NET already schedules async socket, database, and timer continuations on the ThreadPool, but the default worker ramp-up can be conservative for a game server where many small tasks become active at once.
  * Raising the minimum worker and IO completion thread counts does not force busy spinning; it only allows the runtime to make more workers available immediately when load appears.
  */
public static class RuntimeConcurrencyConfigurator
{
    private static int _configured;

    /**
      * Applies a safe process-wide ThreadPool baseline based on the host CPU count.
      * The optional EMULATIONSERVER_MIN_WORKER_THREADS and EMULATIONSERVER_MIN_IO_THREADS environment variables can override the automatic values for local tuning.
      */
    public static void ConfigureForServer(string serverName)
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1)
        {
            return;
        }

        int processorCount = Math.Max(1, Environment.ProcessorCount);
        int targetWorkerThreads = ReadPositiveEnvironmentOverride("EMULATIONSERVER_MIN_WORKER_THREADS") ?? Math.Max(16, processorCount * 4);
        int targetCompletionPortThreads = ReadPositiveEnvironmentOverride("EMULATIONSERVER_MIN_IO_THREADS") ?? Math.Max(16, processorCount * 2);

        ThreadPool.GetMinThreads(out int currentWorkerThreads, out int currentCompletionPortThreads);

        int workerThreads = Math.Max(currentWorkerThreads, targetWorkerThreads);
        int completionPortThreads = Math.Max(currentCompletionPortThreads, targetCompletionPortThreads);

        if (!ThreadPool.SetMinThreads(workerThreads, completionPortThreads))
        {
            Logger.Write(LogType.WARNING, $"{serverName} could not update ThreadPool minimum threads. Current worker={currentWorkerThreads}, io={currentCompletionPortThreads}.", "RuntimeConcurrency");
            return;
        }

        Logger.Write(LogType.THREAD, $"{serverName} concurrency baseline: processors={processorCount}, min worker threads={workerThreads}, min IO threads={completionPortThreads}.", "RuntimeConcurrency");
    }

    /**
      * Reads a positive integer environment override without making startup fail because of a mistyped local tuning value.
      */
    private static int? ReadPositiveEnvironmentOverride(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : null;
    }
}
