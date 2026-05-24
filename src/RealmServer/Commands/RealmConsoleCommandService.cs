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

using System.Globalization;

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
        _accountRepository = accountRepository ?? throw new ArgumentNullException();
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
        Logger.Write(LogType.TRACE, "RealmServer console commands are available. Type 'account help' for account commands.", "RealmConsoleCommandService");

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
                Logger.Write(LogType.FAILED, exception.Message, "RealmConsoleCommandService");
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
            Logger.Write(LogType.WARNING, $"Unknown command '{parts[0]}'.", "RealmConsoleCommandService");
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

            case "ban":
                await BanAccountAsync(parts, cancellationToken);
                break;

            case "unban":
                await UnbanAccountAsync(parts, cancellationToken);
                break;

            case "baninfo":
            case "banhistory":
                await WriteBanInfoAsync(parts, cancellationToken);
                break;

            case "banlist":
            case "bans":
                await WriteBanListAsync(parts, cancellationToken);
                break;

            default:
                Logger.Write(LogType.WARNING, $"Unknown account command '{parts[1]}'.", "RealmConsoleCommandService");
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
            Logger.Write(LogType.WARNING, "Usage: account add <username> <password> [email] [gmlevel]", "RealmConsoleCommandService");
            return;
        }

        string username = parts[2];
        string password = parts[3];
        string email = parts.Length >= 5 ? parts[4] : string.Empty;
        byte gmLevel = 0;

        if (parts.Length >= 6 && !byte.TryParse(parts[5], out gmLevel))
        {
            Logger.Write(LogType.WARNING, "gmlevel must be a number between 0 and 3.", "RealmConsoleCommandService");
            return;
        }

        AccountCommandResult result = await _accountRepository.CreateAccountAsync(username, password, email, gmLevel, cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SUCCESS : LogType.FAILED, result.Message, "RealmConsoleCommandService");
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
            Logger.Write(LogType.WARNING, "Usage: account remove <username>", "RealmConsoleCommandService");
            return;
        }

        AccountCommandResult result = await _accountRepository.RemoveAccountAsync(parts[2], cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SUCCESS : LogType.FAILED, result.Message, "RealmConsoleCommandService");
    }

    /**
      * Adds a permanent or temporary ban to an account while preserving previous ban history.
      * The duration argument accepts permanent, perm, forever, 0, plain seconds, or compound values like 30m, 2h, 7d, and 1d12h.
      */
    private async Task BanAccountAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 5)
        {
            Logger.Write(LogType.WARNING, "Usage: account ban <username> <duration|permanent> <reason...>", "RealmConsoleCommandService");
            return;
        }

        if (!TryParseBanDuration(parts[3], out ulong durationSeconds))
        {
            Logger.Write(LogType.WARNING, "Duration must be permanent, 0, seconds, or values using s/m/h/d/w such as 30m, 2h, 7d, or 1d12h.", "RealmConsoleCommandService");
            return;
        }

        string reason = string.Join(' ', parts.Skip(4));
        AccountCommandResult result = await _accountRepository.BanAccountAsync(parts[2], durationSeconds, "RealmConsole", reason, cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SUCCESS : LogType.FAILED, result.Message, "RealmConsoleCommandService");
    }

    /**
      * Removes the active ban from an account by marking the current account_banned row inactive.
      * Historic ban rows remain available through the baninfo command.
      */
    private async Task UnbanAccountAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3)
        {
            Logger.Write(LogType.WARNING, "Usage: account unban <username>", "RealmConsoleCommandService");
            return;
        }

        AccountCommandResult result = await _accountRepository.UnbanAccountAsync(parts[2], cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SUCCESS : LogType.FAILED, result.Message, "RealmConsoleCommandService");
    }

    /**
      * Writes the complete ban history for a single account to the realm console.
      * The output includes inactive rows so administrators can audit prior bans without querying MySQL directly.
      */
    private async Task WriteBanInfoAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3)
        {
            Logger.Write(LogType.WARNING, "Usage: account baninfo <username>", "RealmConsoleCommandService");
            return;
        }

        AccountBanHistoryResult result = await _accountRepository.GetAccountBanHistoryAsync(parts[2], cancellationToken);
        if (!result.AccountExists)
        {
            Logger.Write(LogType.FAILED, $"Account '{parts[2]}' was not found.", "RealmConsoleCommandService");
            return;
        }

        if (result.Bans.Count == 0)
        {
            Logger.Write(LogType.TRACE, $"Account '{result.Username}' has no ban history.", "RealmConsoleCommandService");
            return;
        }

        Logger.Write(LogType.TRACE, $"Ban history for account '{result.Username}':", "RealmConsoleCommandService");
        foreach (AccountBanRecord ban in result.Bans)
        {
            string activeState = GetBanStateText(ban);
            string banWindow = FormatBanWindow(ban);
            Logger.Write(LogType.TRACE, $"  [{activeState}] {banWindow}; by {ban.BannedBy}; reason: {ban.BanReason}", "RealmConsoleCommandService");
        }
    }

    /**
      * Writes active account bans to the realm console, optionally filtering by account name.
      * Expired temporary bans are cleaned up by the repository before this command receives data.
      */
    private async Task WriteBanListAsync(string[] parts, CancellationToken cancellationToken)
    {
        string usernameFilter = parts.Length >= 3 ? parts[2] : string.Empty;
        IReadOnlyList<AccountBanRecord> bans = await _accountRepository.GetActiveAccountBansAsync(usernameFilter, cancellationToken);

        if (bans.Count == 0)
        {
            string suffix = string.IsNullOrWhiteSpace(usernameFilter) ? string.Empty : $" matching '{usernameFilter}'";
            Logger.Write(LogType.TRACE, $"No active account bans{suffix}.", "RealmConsoleCommandService");
            return;
        }

        Logger.Write(LogType.TRACE, string.IsNullOrWhiteSpace(usernameFilter) ? "Active account bans:" : $"Active account bans matching '{usernameFilter}':", "RealmConsoleCommandService");
        foreach (AccountBanRecord ban in bans)
        {
            Logger.Write(LogType.TRACE, $"  {ban.Username} ({ban.AccountId}): {FormatBanWindow(ban)}; by {ban.BannedBy}; reason: {ban.BanReason}", "RealmConsoleCommandService");
        }
    }

    /**
      * Writes write account help data to the target packet, stream, or persistent store.
      * The method keeps binary layout and serialization rules centralized for easier packet review and compatibility fixes.
      */
    private static void WriteAccountHelp()
    {
        Logger.Write(LogType.TRACE, "Account commands:", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account add <username> <password> [email] [gmlevel]", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account remove <username>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account ban <username> <duration|permanent> <reason...>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account unban <username>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account baninfo <username>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account banlist [username-filter]", "RealmConsoleCommandService");
    }

    /**
      * Parses permanent and timed ban durations from console-friendly text.
      * Plain numbers are seconds; suffixes support seconds, minutes, hours, days, and weeks.
      */
    private static bool TryParseBanDuration(string value, out ulong durationSeconds)
    {
        durationSeconds = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized is "permanent" or "perm" or "forever" or "0")
        {
            return true;
        }

        if (ulong.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out durationSeconds))
        {
            return durationSeconds > 0;
        }

        ulong total = 0;
        ulong current = 0;
        bool hasUnit = false;
        bool hasDigits = false;

        foreach (char character in normalized)
        {
            if (char.IsDigit(character))
            {
                hasDigits = true;
                uint digit = (uint)(character - '0');
                if (current > (ulong.MaxValue - digit) / 10)
                {
                    return false;
                }

                current = (current * 10) + digit;
                continue;
            }

            ulong multiplier = character switch
            {
                's' => 1UL,
                'm' => 60UL,
                'h' => 60UL * 60UL,
                'd' => 60UL * 60UL * 24UL,
                'w' => 60UL * 60UL * 24UL * 7UL,
                _ => 0UL
            };

            if (multiplier == 0 || current == 0)
            {
                return false;
            }

            if (current > ulong.MaxValue / multiplier)
            {
                return false;
            }

            ulong component = current * multiplier;
            if (total > ulong.MaxValue - component)
            {
                return false;
            }

            total += component;
            current = 0;
            hasUnit = true;
        }

        if (!hasDigits || !hasUnit || current != 0 || total == 0)
        {
            return false;
        }

        durationSeconds = total;
        return true;
    }

    /**
      * Formats an account ban row into a readable lifetime description for console output.
      */
    private static string FormatBanWindow(AccountBanRecord ban)
    {
        string start = FormatUnixTime(ban.BanDate);
        if (ban.IsPermanent)
        {
            return $"permanent since {start}";
        }

        ulong durationSeconds = ban.UnbanDate >= ban.BanDate ? ban.UnbanDate - ban.BanDate : 0;
        return $"temporary from {start} until {FormatUnixTime(ban.UnbanDate)} ({FormatDuration(durationSeconds)})";
    }

    /**
      * Returns an active, inactive, or expired label for a ban history row.
      */
    private static string GetBanStateText(AccountBanRecord ban)
    {
        if (!ban.Active)
        {
            return "inactive";
        }

        ulong now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return !ban.IsPermanent && ban.UnbanDate <= now ? "expired" : "active";
    }

    /**
      * Formats a Unix timestamp in UTC so ban logs match the account_banned schema storage format.
      */
    private static string FormatUnixTime(ulong timestamp)
    {
        const ulong maximumDateTimeOffsetUnixSeconds = 253402300799UL;
        if (timestamp > maximumDateTimeOffsetUnixSeconds)
        {
            return $"{timestamp} seconds since Unix epoch";
        }

        return DateTimeOffset.FromUnixTimeSeconds((long)timestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
    }

    /**
      * Formats a duration for administrator-facing console output.
      */
    private static string FormatDuration(ulong durationSeconds)
    {
        if (durationSeconds > int.MaxValue)
        {
            return $"{durationSeconds} seconds";
        }

        TimeSpan duration = TimeSpan.FromSeconds((int)durationSeconds);
        List<string> parts = [];

        if (duration.Days > 0)
        {
            parts.Add($"{duration.Days} day{(duration.Days == 1 ? string.Empty : "s")}");
        }

        if (duration.Hours > 0)
        {
            parts.Add($"{duration.Hours} hour{(duration.Hours == 1 ? string.Empty : "s")}");
        }

        if (duration.Minutes > 0)
        {
            parts.Add($"{duration.Minutes} minute{(duration.Minutes == 1 ? string.Empty : "s")}");
        }

        if (duration.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{duration.Seconds} second{(duration.Seconds == 1 ? string.Empty : "s")}");
        }

        return string.Join(' ', parts);
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
