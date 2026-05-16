
using EmulationServer.Database.Accounts;
using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.RealmServer.Commands;

public sealed class RealmConsoleCommandService
{
    private readonly AccountRepository _accountRepository;
    private Task? _commandTask;

    public RealmConsoleCommandService(AccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    public void Start(CancellationToken cancellationToken)
    {
        if (_commandTask is not null)
        {
            throw new InvalidOperationException("Realm command service has already been started.");
        }

        _commandTask = Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
    }

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

    private static void WriteAccountHelp()
    {
        Logger.Write(LogType.TRACE, "Account commands:", nameof(RealmConsoleCommandService));
        Logger.Write(LogType.TRACE, "  account add <username> <password> [email] [gmlevel]", nameof(RealmConsoleCommandService));
        Logger.Write(LogType.TRACE, "  account remove <username>", nameof(RealmConsoleCommandService));
    }

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
