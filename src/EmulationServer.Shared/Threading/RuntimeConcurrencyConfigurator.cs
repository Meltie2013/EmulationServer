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
// File: src/EmulationServer.Shared/Threading/RuntimeConcurrencyConfigurator.cs
// Purpose: Contains runtime concurrency configurator code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Shared.Threading;

// Type: RuntimeConcurrencyConfigurator
// Purpose: Provides runtime concurrency configurator behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RuntimeConcurrencyConfigurator
{
    // Field: Stores the configured state used by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Value: current configured backing value maintained by the owning type.
    private static int _configured;

    // Method: ConfigureForServer
    // Purpose: Executes the configure for server operation for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - serverName: Server name value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RuntimeConcurrencyConfigurator so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: ReadPositiveEnvironmentOverride
    // Purpose: Retrieves read positive environment override data for the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - name: Name value supplied by the caller for this operation.
    // Returns: Returns the int? value produced by this operation.
    // Notes: This keeps the operation scoped to RuntimeConcurrencyConfigurator so callers do not duplicate validation, protocol, or persistence rules.
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
