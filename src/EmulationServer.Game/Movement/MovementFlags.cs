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
// File: src/EmulationServer.Game/Movement/MovementFlags.cs
// Purpose: Contains movement flags code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Movement;

[Flags]
// Type: MovementFlags
// Purpose: Defines the allowed movement flags values used by the game-domain data, player state, DBC, and world-template layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum MovementFlags : uint
{

    // Enum Value: Defines the none enum value.
    // Value: explicit expression 0x00000000.
    None = 0x00000000,

    // Enum Value: Defines the forward enum value.
    // Value: explicit expression 0x00000001.
    Forward = 0x00000001,

    // Enum Value: Defines the backward enum value.
    // Value: explicit expression 0x00000002.
    Backward = 0x00000002,

    // Enum Value: Defines the strafe left enum value.
    // Value: explicit expression 0x00000004.
    StrafeLeft = 0x00000004,

    // Enum Value: Defines the strafe right enum value.
    // Value: explicit expression 0x00000008.
    StrafeRight = 0x00000008,

    // Enum Value: Defines the turn left enum value.
    // Value: explicit expression 0x00000010.
    TurnLeft = 0x00000010,

    // Enum Value: Defines the turn right enum value.
    // Value: explicit expression 0x00000020.
    TurnRight = 0x00000020,

    // Enum Value: Defines the pitch up enum value.
    // Value: explicit expression 0x00000040.
    PitchUp = 0x00000040,

    // Enum Value: Defines the pitch down enum value.
    // Value: explicit expression 0x00000080.
    PitchDown = 0x00000080,

    // Enum Value: Defines the walk mode enum value.
    // Value: explicit expression 0x00000100.
    WalkMode = 0x00000100,

    // Enum Value: Defines the on transport enum value.
    // Value: explicit expression 0x00000200.
    OnTransport = 0x00000200,

    // Enum Value: Defines the levitate enum value.
    // Value: explicit expression 0x00000400.
    Levitate = 0x00000400,

    // Enum Value: Defines the root enum value.
    // Value: explicit expression 0x00000800.
    Root = 0x00000800,

    // Enum Value: Defines the falling enum value.
    // Value: explicit expression 0x00001000.
    Falling = 0x00001000,

    // Enum Value: Defines the falling far enum value.
    // Value: explicit expression 0x00002000.
    FallingFar = 0x00002000,

    // Enum Value: Defines the pending stop enum value.
    // Value: explicit expression 0x00004000.
    PendingStop = 0x00004000,

    // Enum Value: Defines the pending strafe stop enum value.
    // Value: explicit expression 0x00008000.
    PendingStrafeStop = 0x00008000,

    // Enum Value: Defines the pending forward enum value.
    // Value: explicit expression 0x00010000.
    PendingForward = 0x00010000,

    // Enum Value: Defines the pending backward enum value.
    // Value: explicit expression 0x00020000.
    PendingBackward = 0x00020000,

    // Enum Value: Defines the pending strafe left enum value.
    // Value: explicit expression 0x00040000.
    PendingStrafeLeft = 0x00040000,

    // Enum Value: Defines the pending strafe right enum value.
    // Value: explicit expression 0x00080000.
    PendingStrafeRight = 0x00080000,

    // Enum Value: Defines the pending root enum value.
    // Value: explicit expression 0x00100000.
    PendingRoot = 0x00100000,

    // Enum Value: Defines the swimming enum value.
    // Value: explicit expression 0x00200000.
    Swimming = 0x00200000,

    // Enum Value: Defines the ascending enum value.
    // Value: explicit expression 0x00400000.
    Ascending = 0x00400000,

    // Enum Value: Defines the descending enum value.
    // Value: explicit expression 0x00800000.
    Descending = 0x00800000,

    // Enum Value: Defines the can fly enum value.
    // Value: explicit expression 0x01000000.
    CanFly = 0x01000000,

    // Enum Value: Defines the flying enum value.
    // Value: explicit expression 0x02000000.
    Flying = 0x02000000,

    // Enum Value: Defines the spline elevation enum value.
    // Value: explicit expression 0x04000000.
    SplineElevation = 0x04000000,

    // Enum Value: Defines the spline enabled enum value.
    // Value: explicit expression 0x08000000.
    SplineEnabled = 0x08000000,

    // Enum Value: Defines the water walking enum value.
    // Value: explicit expression 0x10000000.
    WaterWalking = 0x10000000,

    // Enum Value: Defines the safe fall enum value.
    // Value: explicit expression 0x20000000.
    SafeFall = 0x20000000,

    // Enum Value: Defines the hover enum value.
    // Value: explicit expression 0x40000000.
    Hover = 0x40000000,
}
