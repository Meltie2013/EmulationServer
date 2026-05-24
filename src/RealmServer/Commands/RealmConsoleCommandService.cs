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
 * Documents the RealmConsoleCommandService source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.RealmServer.Commands;

/**
 * Owns the realm console command service behavior for the realm authentication, realm-list handling, and external client login services layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class RealmConsoleCommandService
{
    /**
     * Holds the private account repository state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private readonly AccountRepository _accountRepository;
    /**
     * Holds the private command task state used by the owning component.
     * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
     */
    private Task? _commandTask;

    /**
     * Initializes a new RealmConsoleCommandService instance with the dependencies required by the realm authentication, realm-list handling, and external client login services workflow.
     * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
     * Inputs used by this operation: accountRepository.
     */
    public RealmConsoleCommandService(AccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    /**
     * Starts the start workflow and prepares the component to accept runtime work.
     * Startup is ordered so validation and dependency setup finish before services are announced as available.
     * Inputs used by this operation: cancellationToken.
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
     * Writes write account help data to the target packet, stream, or persistent store.
     * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
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
