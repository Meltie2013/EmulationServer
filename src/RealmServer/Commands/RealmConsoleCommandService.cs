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

using EmulationServer.Database.Accounts;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/RealmServer/Commands/RealmConsoleCommandService.cs
  * This file belongs to the console command parsing and dispatch portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Commands;

/**
  * Represents the realm console command service component in the console command parsing and dispatch area.
  * It encapsulates a focused runtime behavior so callers can use a small public API instead of duplicating workflow code.
  */
public sealed class RealmConsoleCommandService
{
    /**
      * Stores the account repository dependency or runtime value for RealmConsoleCommandService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private readonly AccountRepository _accountRepository;
    /**
      * Stores the command task dependency or runtime value for RealmConsoleCommandService.
      * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
      */
    private Task? _commandTask;

    /**
      * Creates a new RealmConsoleCommandService instance and stores the dependencies required by the component.
      * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
      */
    public RealmConsoleCommandService(AccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    /**
      * Starts the component and prepares the runtime state required before it can accept work.
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public void Start(CancellationToken cancellationToken)
    {
        if (_commandTask is not null)
        {
            throw new InvalidOperationException("Realm command service has already been started.");
        }

        _commandTask = Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
    }

    /**
      * Runs the main loop for this component until cancellation or shutdown is requested.
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        Logger.Write(LogType.TRACE, "RealmServer console commands are available. Type 'account help' for account commands.", nameof(RealmConsoleCommandService));

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
                Logger.Write(LogType.FAILED, exception.Message, nameof(RealmConsoleCommandService));
            }
        }
    }

    /**
      * Executes the requested command after parsing and validation are complete.
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
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

        if (!string.Equals(parts[0], "account", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Write(LogType.WARNING, $"Unknown command '{parts[0]}'.", nameof(RealmConsoleCommandService));
            return;
        }

        if (parts.Length == 1 || string.Equals(parts[1], "help", StringComparison.OrdinalIgnoreCase))
        {
            WriteAccountHelp();
            return;
        }

        switch (parts[1].ToLowerInvariant())
        {
            case "add":
                await AddAccountAsync(parts, cancellationToken);
                break;

            case "remove":
            case "delete":
                await RemoveAccountAsync(parts, cancellationToken);
                break;

            default:
                Logger.Write(LogType.WARNING, $"Unknown account command '{parts[1]}'.", nameof(RealmConsoleCommandService));
                WriteAccountHelp();
                break;
        }
    }

    /**
      * Adds a new item to the managed collection while preserving internal invariants.
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task AddAccountAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 4)
        {
            Logger.Write(LogType.WARNING, "Usage: account add <username> <password> [email] [gmlevel]", nameof(RealmConsoleCommandService));
            return;
        }

        string username = parts[2];
        string password = parts[3];
        string email = parts.Length >= 5 ? parts[4] : string.Empty;
        byte gmLevel = 0;

        if (parts.Length >= 6 && !byte.TryParse(parts[5], out gmLevel))
        {
            Logger.Write(LogType.WARNING, "gmlevel must be a number between 0 and 3.", nameof(RealmConsoleCommandService));
            return;
        }

        AccountCommandResult result = await _accountRepository.CreateAccountAsync(username, password, email, gmLevel, cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SUCCESS : LogType.FAILED, result.Message, nameof(RealmConsoleCommandService));
    }

    /**
      * Removes an item from the managed collection and cleans up related state.
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    private async Task RemoveAccountAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3)
        {
            Logger.Write(LogType.WARNING, "Usage: account remove <username>", nameof(RealmConsoleCommandService));
            return;
        }

        AccountCommandResult result = await _accountRepository.RemoveAccountAsync(parts[2], cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SUCCESS : LogType.FAILED, result.Message, nameof(RealmConsoleCommandService));
    }

    /**
      * Writes the supplied data to the target destination using the project protocol or file format.
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
      */
    private static void WriteAccountHelp()
    {
        Logger.Write(LogType.TRACE, "Account commands:", nameof(RealmConsoleCommandService));
        Logger.Write(LogType.TRACE, "  account add <username> <password> [email] [gmlevel]", nameof(RealmConsoleCommandService));
        Logger.Write(LogType.TRACE, "  account remove <username>", nameof(RealmConsoleCommandService));
    }

    /**
      * Splits the supplied text into command parts while preserving quoted values.
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
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
      * The method is part of RealmConsoleCommandService and keeps this workflow isolated from the caller.
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
