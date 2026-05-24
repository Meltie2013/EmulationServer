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
 * File overview: src/RealmServer/Auth/RealmBuilds.cs
 * Documents the RealmBuilds source file in the realm authentication, realm-list handling, and external client login services area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.RealmServer.Auth;

/**
 * Lists the supported realm client expansion values used by the realm authentication, realm-list handling, and external client login services layer.
 * Numeric values are part of the project contract and should only be changed when the related client packet, DBC value, or database schema is updated as well.
 */
public enum RealmClientExpansion : byte
{
    /**
     * Represents the classic value for realm client expansion handling.
     */
    Classic = 0,
    /**
     * Represents the the burning crusade value for realm client expansion handling.
     */
    TheBurningCrusade = 1,
    /**
     * Represents the wrath of the lich king value for realm client expansion handling.
     */
    WrathOfTheLichKing = 2,
    /**
     * Represents the cataclysm value for realm client expansion handling.
     */
    Cataclysm = 3,
}

/**
 * Owns the realm builds behavior for the realm authentication, realm-list handling, and external client login services layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class RealmBuilds
{
    /**
     * Defines the constant value for vanilla 1121.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Vanilla1121 = 5875;
    /**
     * Defines the constant value for vanilla 1122.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Vanilla1122 = 6005;
    /**
     * Defines the constant value for vanilla 1123.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Vanilla1123 = 6141;

    /**
     * Defines the constant value for the burning crusade 243.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort TheBurningCrusade243 = 8606;

    /**
     * Defines the constant value for wrath 335 a.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Wrath335a = 12340;

    /**
     * Defines the constant value for cataclysm 434.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Cataclysm434 = 15595;

    /**
     * Stores the default supported builds value used when the caller does not supply an override.
     * Centralizing the default keeps configuration and packet behavior consistent across the server process.
     */
    private static readonly HashSet<ushort> SupportedBuilds =
    [
        Vanilla1121,
        Vanilla1122,
        Vanilla1123,
        TheBurningCrusade243,
        Wrath335a,
        Cataclysm434,
    ];

    /**
     * Determines whether supported for the realm authentication, realm-list handling, and external client login services workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool IsSupported(ushort build)
    {
        return SupportedBuilds.Contains(build);
    }

    /**
     * Determines whether vanilla for the realm authentication, realm-list handling, and external client login services workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool IsVanilla(ushort build)
    {
        return build is Vanilla1121 or Vanilla1122 or Vanilla1123;
    }

    /**
     * Determines whether the burning crusade for the realm authentication, realm-list handling, and external client login services workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool IsTheBurningCrusade(ushort build)
    {
        return build is TheBurningCrusade243;
    }

    /**
     * Determines whether wrath for the realm authentication, realm-list handling, and external client login services workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool IsWrath(ushort build)
    {
        return build is Wrath335a;
    }

    /**
     * Determines whether cataclysm for the realm authentication, realm-list handling, and external client login services workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool IsCataclysm(ushort build)
    {
        return build is Cataclysm434;
    }

    /**
     * Performs the uses modern proof response operation for the realm authentication, realm-list handling, and external client login services workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool UsesModernProofResponse(ushort build)
    {
        return !IsVanilla(build);
    }

    /**
     * Performs the uses modern realm list operation for the realm authentication, realm-list handling, and external client login services workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool UsesModernRealmList(ushort build)
    {
        return !IsVanilla(build);
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmBuilds and keeps this workflow isolated from the caller.
      */
    public static RealmClientExpansion GetExpansion(ushort build)
    {
        return build switch
        {
            Vanilla1121 or Vanilla1122 or Vanilla1123 => RealmClientExpansion.Classic,
            TheBurningCrusade243 => RealmClientExpansion.TheBurningCrusade,
            Wrath335a => RealmClientExpansion.WrathOfTheLichKing,
            Cataclysm434 => RealmClientExpansion.Cataclysm,
            _ => RealmClientExpansion.Classic,
        };
    }

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmBuilds and keeps this workflow isolated from the caller.
      */
    public static string GetDisplayName(ushort build)
    {
        return build switch
        {
            Vanilla1121 => "1.12.1",
            Vanilla1122 => "1.12.2",
            Vanilla1123 => "1.12.3",
            TheBurningCrusade243 => "2.4.3",
            Wrath335a => "3.3.5a",
            Cataclysm434 => "4.3.4",
            _ => $"Unknown ({build})",
        };
    }
}
