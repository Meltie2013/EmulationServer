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
// File: src/EmulationServer.Game/Movement/TransportMovementInfo.cs
// Purpose: Contains transport movement info code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Movement;

// Type: TransportMovementInfo
// Purpose: Represents transport movement info data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Guid: GUID identifier used to select the exact record, object, or runtime owner.
// - X: X value supplied by the caller for this operation.
// - Y: Y value supplied by the caller for this operation.
// - Z: Z value supplied by the caller for this operation.
// - Orientation: Orientation value supplied by the caller for this operation.
// - Time: Time value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record TransportMovementInfo(
    ulong Guid,
    float X,
    float Y,
    float Z,
    float Orientation,
    uint Time)
{

    // Property: Gets or sets the is finite value used by the game-domain data, player state, DBC, and world-template layer.
    // Value: is finite value exposed by the owning type.
    public bool IsFinite =>
        float.IsFinite(X) &&
        float.IsFinite(Y) &&
        float.IsFinite(Z) &&
        float.IsFinite(Orientation);
}
