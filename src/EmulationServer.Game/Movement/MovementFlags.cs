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

/**
 * File overview: src/EmulationServer.Game/Movement/MovementFlags.cs
 * Documents the MovementFlags source file in the movement packet state and client coordinate tracking area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Game.Movement;

/**
 * Lists the supported movement flags values used by the movement packet state and client coordinate tracking layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
[Flags]
public enum MovementFlags : uint
{
    /**
     * Represents the none value for movement flags handling.
     */
    None = 0x00000000,
    /**
     * Represents the forward value for movement flags handling.
     */
    Forward = 0x00000001,
    /**
     * Represents the backward value for movement flags handling.
     */
    Backward = 0x00000002,
    /**
     * Represents the strafe left value for movement flags handling.
     */
    StrafeLeft = 0x00000004,
    /**
     * Represents the strafe right value for movement flags handling.
     */
    StrafeRight = 0x00000008,
    /**
     * Represents the turn left value for movement flags handling.
     */
    TurnLeft = 0x00000010,
    /**
     * Represents the turn right value for movement flags handling.
     */
    TurnRight = 0x00000020,
    /**
     * Represents the pitch up value for movement flags handling.
     */
    PitchUp = 0x00000040,
    /**
     * Represents the pitch down value for movement flags handling.
     */
    PitchDown = 0x00000080,
    /**
     * Represents the walk mode value for movement flags handling.
     */
    WalkMode = 0x00000100,
    /**
     * Represents the on transport value for movement flags handling.
     */
    OnTransport = 0x00000200,
    /**
     * Represents the levitate value for movement flags handling.
     */
    Levitate = 0x00000400,
    /**
     * Represents the root value for movement flags handling.
     */
    Root = 0x00000800,
    /**
     * Represents the falling value for movement flags handling.
     */
    Falling = 0x00001000,
    /**
     * Represents the falling far value for movement flags handling.
     */
    FallingFar = 0x00002000,
    /**
     * Represents the pending stop value for movement flags handling.
     */
    PendingStop = 0x00004000,
    /**
     * Represents the pending strafe stop value for movement flags handling.
     */
    PendingStrafeStop = 0x00008000,
    /**
     * Represents the pending forward value for movement flags handling.
     */
    PendingForward = 0x00010000,
    /**
     * Represents the pending backward value for movement flags handling.
     */
    PendingBackward = 0x00020000,
    /**
     * Represents the pending strafe left value for movement flags handling.
     */
    PendingStrafeLeft = 0x00040000,
    /**
     * Represents the pending strafe right value for movement flags handling.
     */
    PendingStrafeRight = 0x00080000,
    /**
     * Represents the pending root value for movement flags handling.
     */
    PendingRoot = 0x00100000,
    /**
     * Represents the swimming value for movement flags handling.
     */
    Swimming = 0x00200000,
    /**
     * Represents the ascending value for movement flags handling.
     */
    Ascending = 0x00400000,
    /**
     * Represents the descending value for movement flags handling.
     */
    Descending = 0x00800000,
    /**
     * Represents the can fly value for movement flags handling.
     */
    CanFly = 0x01000000,
    /**
     * Represents the flying value for movement flags handling.
     */
    Flying = 0x02000000,
    /**
     * Represents the spline elevation value for movement flags handling.
     */
    SplineElevation = 0x04000000,
    /**
     * Represents the spline enabled value for movement flags handling.
     */
    SplineEnabled = 0x08000000,
    /**
     * Represents the water walking value for movement flags handling.
     */
    WaterWalking = 0x10000000,
    /**
     * Represents the safe fall value for movement flags handling.
     */
    SafeFall = 0x20000000,
    /**
     * Represents the hover value for movement flags handling.
     */
    Hover = 0x40000000,
}
