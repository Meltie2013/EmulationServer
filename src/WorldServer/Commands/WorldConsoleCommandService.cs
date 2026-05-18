using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.WorldServer.Commands;

public sealed class WorldConsoleCommandService
{
    private readonly Func<string, int, CancellationToken, Task> _executeMapCommandAsync;
    private Task? _commandTask;

    public WorldConsoleCommandService(Func<string, int, CancellationToken, Task> executeMapCommandAsync)
    {
        _executeMapCommandAsync = executeMapCommandAsync ?? throw new ArgumentNullException(nameof(executeMapCommandAsync));
    }

    public void Start(CancellationToken cancellationToken)
    {
        if (_commandTask is not null)
        {
            throw new InvalidOperationException("World command service has already been started.");
        }

        _commandTask = Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
    }

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

    private static void WriteMapHelp()
    {
        Logger.Write(LogType.TRACE, "Map commands:", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map start #mapid", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map shutdown #mapid", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map restart #mapid", nameof(WorldConsoleCommandService));
        Logger.Write(LogType.TRACE, "  map info #mapid", nameof(WorldConsoleCommandService));
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
