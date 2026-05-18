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
  * This file belongs to the realm authentication, build validation, and realm list packet creation portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Defines the allowed realm client expansion values used to keep state and protocol decisions explicit.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public enum RealmClientExpansion : byte
{
    /**
      * Represents the classic value for RealmClientExpansion.
      */
    Classic = 0,
    /**
      * Represents the the burning crusade value for RealmClientExpansion.
      */
    TheBurningCrusade = 1,
    /**
      * Represents the wrath of the lich king value for RealmClientExpansion.
      */
    WrathOfTheLichKing = 2,
    /**
      * Represents the cataclysm value for RealmClientExpansion.
      */
    Cataclysm = 3,
}

/**
  * Represents the realm builds component in the realm authentication, build validation, and realm list packet creation area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
public static class RealmBuilds
{
    public const ushort Vanilla1121 = 5875;
    public const ushort Vanilla1122 = 6005;
    public const ushort Vanilla1123 = 6141;

    public const ushort TheBurningCrusade243 = 8606;

    public const ushort Wrath335a = 12340;

    public const ushort Cataclysm434 = 15595;

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
      * Performs the is supported operation for RealmBuilds.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool IsSupported(ushort build)
    {
        return SupportedBuilds.Contains(build);
    }

    /**
      * Performs the is vanilla operation for RealmBuilds.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool IsVanilla(ushort build)
    {
        return build is Vanilla1121 or Vanilla1122 or Vanilla1123;
    }

    /**
      * Performs the is the burning crusade operation for RealmBuilds.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool IsTheBurningCrusade(ushort build)
    {
        return build is TheBurningCrusade243;
    }

    /**
      * Performs the is wrath operation for RealmBuilds.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool IsWrath(ushort build)
    {
        return build is Wrath335a;
    }

    /**
      * Performs the is cataclysm operation for RealmBuilds.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool IsCataclysm(ushort build)
    {
        return build is Cataclysm434;
    }

    /**
      * Performs the uses modern proof response operation for RealmBuilds.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool UsesModernProofResponse(ushort build)
    {
        return !IsVanilla(build);
    }

    /**
      * Performs the uses modern realm list operation for RealmBuilds.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
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
