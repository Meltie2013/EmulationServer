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
// File: src/EmulationServer.Game/Chat/ChatCommandContext.cs
// Purpose: Contains chat command context code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Commands;

// Type: ChatCommandContext
// Purpose: Provides chat command context behavior for the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ChatCommandContext(
    IInGameCommandSession session,
    string rawText,
    string commandName,
    string arguments,
    InGameCommandRegistry registry,
    InGameCommandDependencies dependencies)
{

    // Property: Gets or sets the session value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: session value exposed by the owning type.
    public IInGameCommandSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

    // Property: Gets or sets the raw text value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: raw text value exposed by the owning type.
    public string RawText { get; } = rawText ?? string.Empty;

    // Property: Gets or sets the command name value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: command name value exposed by the owning type.
    public string CommandName { get; } = commandName ?? string.Empty;

    // Property: Gets or sets the arguments value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: arguments value exposed by the owning type.
    public string Arguments { get; } = arguments ?? string.Empty;

    // Property: Gets or sets the registry value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: registry value exposed by the owning type.
    public InGameCommandRegistry Registry { get; } = registry ?? throw new ArgumentNullException(nameof(registry));

    // Property: Gets or sets the dependencies value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: dependencies value exposed by the owning type.
    public InGameCommandDependencies Dependencies { get; } = dependencies ?? InGameCommandDependencies.Empty;
}
