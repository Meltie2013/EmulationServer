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
// File: src/EmulationServer.Game/Chat/InGameCommandRegistry.cs
// Purpose: Contains in game command registry code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Commands;

// Type: InGameCommandRegistry
// Purpose: Provides in game command registry behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class InGameCommandRegistry
{
    // Field: Stores the string state used by the game-domain data, player state, DBC, and world-template layer.
    // Value: current string backing value maintained by the owning type.
    private readonly Dictionary<string, IChatCommand> _commandsByToken;

    // Constructor: InGameCommandRegistry
    // Purpose: Initializes a new InGameCommandRegistry instance with dependencies and values required by the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - commands: Commands value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InGameCommandRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public InGameCommandRegistry(IEnumerable<IChatCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        Dictionary<string, IChatCommand> commandsByToken = new(StringComparer.OrdinalIgnoreCase);
        List<IChatCommand> commandList = [];

        foreach (IChatCommand command in commands)
        {
            RegisterCommand(command, commandsByToken, commandList);
        }

        _commandsByToken = commandsByToken;
        Commands = [.. commandList.OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)];
    }

    // Method: CreateDefault
    // Purpose: Applies create default changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters: none.
    // Returns: Returns the in game command registry value produced by this operation.
    // Notes: This keeps the operation scoped to InGameCommandRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public static InGameCommandRegistry CreateDefault()
    {
        return new InGameCommandRegistry(
        [
            new AccountCommand(),
            new BanCommand(),
            new BankCommand(),
            new HelpCommand(),
            new MapCommand(),
            new ReloadCommand(),
            new ServerCommand(),
        ]);
    }

    // Method: TryGetCommand
    // Purpose: Attempts to retrieve or parse try get command data without treating normal misses as failures.
    // Parameters:
    // - token: Token value supplied by the caller for this operation.
    // - command: Database command used to execute this operation without opening unnecessary additional state.
    // Returns: Returns true when try get command succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to InGameCommandRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryGetCommand(string token, out IChatCommand command)
    {
        if (_commandsByToken.TryGetValue(token, out IChatCommand? resolved))
        {
            command = resolved;
            return true;
        }

        command = null!;
        return false;
    }

    // Property: Gets or sets the commands value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: commands value exposed by the owning type.
    public IReadOnlyList<IChatCommand> Commands { get; }

    // Method: GetAvailableCommands
    // Purpose: Retrieves get available commands data for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // Returns: Returns the I read only list value produced by this operation.
    // Notes: This keeps the operation scoped to InGameCommandRegistry so callers do not duplicate validation, protocol, or persistence rules.
    public IReadOnlyList<IChatCommand> GetAvailableCommands(IInGameCommandSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return [.. Commands.Where(command => session.HasPermission(command.RequiredPermission))];
    }

    // Method: RegisterCommand
    // Purpose: Executes the register command operation for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - command: Database command used to execute this operation without opening unnecessary additional state.
    // - commandsByToken: Commands by token value supplied by the caller for this operation.
    // - commandList: Command list value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InGameCommandRegistry so callers do not duplicate validation, protocol, or persistence rules.
    private static void RegisterCommand(
        IChatCommand command,
        Dictionary<string, IChatCommand> commandsByToken,
        List<IChatCommand> commandList)
    {
        ArgumentNullException.ThrowIfNull(command);

        string name = NormalizeToken(command.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Chat command name cannot be empty.");
        }

        AddToken(name, command, commandsByToken);
        foreach (string alias in command.Aliases)
        {
            string token = NormalizeToken(alias);
            if (!string.IsNullOrWhiteSpace(token))
            {
                AddToken(token, command, commandsByToken);
            }
        }

        commandList.Add(command);
    }

    // Method: AddToken
    // Purpose: Applies add token changes for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - token: Token value supplied by the caller for this operation.
    // - command: Database command used to execute this operation without opening unnecessary additional state.
    // - commandsByToken: Commands by token value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to InGameCommandRegistry so callers do not duplicate validation, protocol, or persistence rules.
    private static void AddToken(string token, IChatCommand command, Dictionary<string, IChatCommand> commandsByToken)
    {
        if (commandsByToken.TryGetValue(token, out IChatCommand? existing))
        {
            throw new InvalidOperationException($"Chat command token '{token}' is already registered by '{existing.Name}'.");
        }

        commandsByToken[token] = command;
    }

    // Method: NormalizeToken
    // Purpose: Converts incoming data into normalize token form for the game-domain data, player state, DBC, and world-template layer.
    // Parameters:
    // - token: Token value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to InGameCommandRegistry so callers do not duplicate validation, protocol, or persistence rules.
    private static string NormalizeToken(string token)
    {
        token = token.Trim();
        return token.StartsWith('.') ? token[1..].Trim() : token;
    }
}
