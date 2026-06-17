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
// File: src/RealmServer/Commands/RealmConsoleCommandService.cs
// Purpose: Contains realm console command service code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Globalization;

using EmulationServer.Database.Accounts;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Commands;

// Type: RealmConsoleCommandService
// Purpose: Provides realm console command service behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmConsoleCommandService
{

    // Field: Stores the account repository state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current account repository backing value maintained by the owning type.
    private readonly AccountRepository _accountRepository;

    // Field: Stores the command task state used by the realm server authentication, realm-list, and account connection layer.
    // Value: current command task backing value maintained by the owning type.
    private Task? _commandTask;

    // Constructor: RealmConsoleCommandService
    // Purpose: Initializes a new RealmConsoleCommandService instance with dependencies and values required by the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - accountRepository: Account repository value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    public RealmConsoleCommandService(AccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException();
    }

    // Method: Start
    // Purpose: Controls the start lifecycle step for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    public void Start(CancellationToken cancellationToken)
    {
        if (_commandTask is not null)
        {
            throw new InvalidOperationException("Realm command service has already been started.");
        }

        _commandTask = Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
    }

    // Method: RunAsync
    // Purpose: Controls the run lifecycle step for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: ExecuteAsync
    // Purpose: Controls the execute lifecycle step for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - line: Line value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: AddAccountAsync
    // Purpose: Applies add account changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task AddAccountAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 4)
        {
            Logger.Write(LogType.WARNING, "Usage: account add <username> <password> [email]", "RealmConsoleCommandService");
            return;
        }

        string username = parts[2];
        string password = parts[3];
        string email = parts.Length >= 5 ? parts[4] : string.Empty;

        AccountCommandResult result = await _accountRepository.CreateAccountAsync(username, password, email, cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SYSTEM : LogType.FAILED, result.Message, "RealmConsoleCommandService");
    }

    // Method: RemoveAccountAsync
    // Purpose: Applies remove account changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task RemoveAccountAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3)
        {
            Logger.Write(LogType.WARNING, "Usage: account remove <username>", "RealmConsoleCommandService");
            return;
        }

        AccountCommandResult result = await _accountRepository.RemoveAccountAsync(parts[2], cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SYSTEM : LogType.FAILED, result.Message, "RealmConsoleCommandService");
    }

    // Method: BanAccountAsync
    // Purpose: Executes the ban account operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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
        Logger.Write(result.Succeeded ? LogType.SYSTEM : LogType.FAILED, result.Message, "RealmConsoleCommandService");
    }

    // Method: UnbanAccountAsync
    // Purpose: Executes the unban account operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    private async Task UnbanAccountAsync(string[] parts, CancellationToken cancellationToken)
    {
        if (parts.Length < 3)
        {
            Logger.Write(LogType.WARNING, "Usage: account unban <username>", "RealmConsoleCommandService");
            return;
        }

        AccountCommandResult result = await _accountRepository.UnbanAccountAsync(parts[2], cancellationToken);
        Logger.Write(result.Succeeded ? LogType.SYSTEM : LogType.FAILED, result.Message, "RealmConsoleCommandService");
    }

    // Method: WriteBanInfoAsync
    // Purpose: Builds or writes write ban info output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: WriteBanListAsync
    // Purpose: Builds or writes write ban list output for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - stringparts: Stringparts value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Method: WriteAccountHelp
    // Purpose: Builds or writes write account help output for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    private static void WriteAccountHelp()
    {
        Logger.Write(LogType.TRACE, "Account commands:", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account add <username> <password> [email]", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account remove <username>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account ban <username> <duration|permanent> <reason...>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account unban <username>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account baninfo <username>", "RealmConsoleCommandService");
        Logger.Write(LogType.TRACE, "  account banlist [username-filter]", "RealmConsoleCommandService");
    }

    // Method: TryParseBanDuration
    // Purpose: Attempts to retrieve or parse try parse ban duration data without treating normal misses as failures.
    // Parameters:
    // - value: Value value supplied by the caller for this operation.
    // - durationSeconds: Duration seconds value supplied by the caller for this operation.
    // Returns: Returns true when try parse ban duration succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: FormatBanWindow
    // Purpose: Executes the format ban window operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - ban: Ban value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: GetBanStateText
    // Purpose: Retrieves get ban state text data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - ban: Ban value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    private static string GetBanStateText(AccountBanRecord ban)
    {
        if (!ban.Active)
        {
            return "inactive";
        }

        ulong now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return !ban.IsPermanent && ban.UnbanDate <= now ? "expired" : "active";
    }

    // Method: FormatUnixTime
    // Purpose: Executes the format unix time operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - timestamp: Timestamp value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
    private static string FormatUnixTime(ulong timestamp)
    {
        const ulong maximumDateTimeOffsetUnixSeconds = 253402300799UL;
        if (timestamp > maximumDateTimeOffsetUnixSeconds)
        {
            return $"{timestamp} seconds since Unix epoch";
        }

        return DateTimeOffset.FromUnixTimeSeconds((long)timestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
    }

    // Method: FormatDuration
    // Purpose: Executes the format duration operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - durationSeconds: Duration seconds value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: SplitCommandLine
    // Purpose: Executes the split command line operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - commandLine: Command line value supplied by the caller for this operation.
    // Returns: Returns the string[] value produced by this operation.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: AddPart
    // Purpose: Applies add part changes for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - parts: Parts value supplied by the caller for this operation.
    // - current: Current value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmConsoleCommandService so callers do not duplicate validation, protocol, or persistence rules.
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
