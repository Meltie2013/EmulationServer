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

/**
  * File overview: src/WorldServer/Commands/WorldConsoleCommandService.cs
  * This file belongs to the console command parsing and dispatch portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.WorldServer.Commands;

/**
  * Reads WorldServer console commands and forwards validated map service commands to the internal control layer.
  * It encapsulates a focused runtime behavior so callers can use a small public API instead of duplicating workflow code.
  */
public sealed class WorldConsoleCommandService
{
    private readonly Func<string, int, CancellationToken, Task> _executeMapCommandAsync;
    /**
      * Stores the command task dependency or runtime value for WorldConsoleCommandService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private Task? _commandTask;

    /**
      * Creates a new WorldConsoleCommandService instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public WorldConsoleCommandService(Func<string, int, CancellationToken, Task> executeMapCommandAsync)
    {
        _executeMapCommandAsync = executeMapCommandAsync ?? throw new ArgumentNullException(nameof(executeMapCommandAsync));
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of WorldConsoleCommandService and keeps this workflow isolated from the caller.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public void Start(CancellationToken cancellationToken)
    {
        if (_commandTask is not null)
        {
            throw new InvalidOperationException("World command service has already been started.");
        }

        _commandTask = Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of WorldConsoleCommandService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, "WorldServer console commands are available. Type 'map help' for map commands.", nameof(WorldConsoleCommandService));

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await Task.Run(Console.ReadLine, CancellationToken.None);
            if (line is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                await ExecuteAsync(line, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                Logger.Write(LogType.FAILED, exception.Message, nameof(WorldConsoleCommandService));
            }
        }
    }

    /**
      * Executes the requested command after parsing and validation are complete.
      * The method is part of WorldConsoleCommandService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task ExecuteAsync(string line, CancellationToken cancellationToken)
    {
        string[] parts = SplitCommandLine(line);
        if (parts.Length == 0)
        {
            return;
        }

        if (!string.Equals(parts[0], "map", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Unknown command '{parts[0]}'.", nameof(WorldConsoleCommandService));
            return;
        }

        if (parts.Length == 1 || string.Equals(parts[1], "help", StringComparison.OrdinalIgnoreCase))
        {
            WriteMapHelp();
            return;
        }

        string action = parts[1].ToLowerInvariant();
        if (action is not ("start" or "shutdown" or "restart" or "info"))
        {
            Logger.Write(LogType.WARNING, $"Unknown map command '{parts[1]}'.", nameof(WorldConsoleCommandService));
            WriteMapHelp();
            return;
        }

        if (parts.Length < 3)
        {
            Logger.Write(LogType.WARNING, $"Usage: map {action} #mapid", nameof(WorldConsoleCommandService));
            return;
        }

        string mapIdText = parts[2].StartsWith('#') ? parts[2][1..] : parts[2];
        if (!int.TryParse(mapIdText, out int mapId) || mapId < 0)
        {
            Logger.Write(LogType.WARNING, "Map ID must be a non-negative number. Example: map info #0", nameof(WorldConsoleCommandService));
            return;
        }

        await _executeMapCommandAsync(action, mapId, cancellationToken);
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of WorldConsoleCommandService and keeps this workflow isolated from the caller.
      */
    private static void WriteMapHelp()
    {
        Logger.Write(LogType.TRACE, "Map commands:", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map start #mapid", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map shutdown #mapid", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map restart #mapid", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map info #mapid", nameof(WorldConsoleCommandService));
    }

    /**
      * Splits the supplied text into command parts while preserving quoted values.
      * The method is part of WorldConsoleCommandService and keeps this workflow isolated from the caller.
      */
    private static string[] SplitCommandLine(string commandLine)
    {
        List<string> parts = [];
        bool inQuotes = false;
        List<char> current = [];

        foreach (char character in commandLine)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                AddPart(parts, current);
                continue;
            }

            current.Add(character);
        }

        AddPart(parts, current);
        return [.. parts];
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of WorldConsoleCommandService and keeps this workflow isolated from the caller.
      */
    private static void AddPart(List<string> parts, List<char> current)
    {
        if (current.Count == 0)
        {
            return;
        }

        parts.Add(new string(current.ToArray()));
        current.Clear();
    }
}
