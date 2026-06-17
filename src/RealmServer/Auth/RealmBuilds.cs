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
// File: src/RealmServer/Auth/RealmBuilds.cs
// Purpose: Contains realm builds code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Auth;

// Type: RealmBuildVersionInfo
// Purpose: Represents realm build version info data passed through the realm server authentication, realm-list, and account connection layer.
// Constructor values:
// - MajorVersion: Major version value supplied by the caller for this operation.
// - MinorVersion: Minor version value supplied by the caller for this operation.
// - PatchVersion: Patch version value supplied by the caller for this operation.
// - Build: Build value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public readonly record struct RealmBuildVersionInfo(byte MajorVersion, byte MinorVersion, byte PatchVersion, ushort Build);

// Type: RealmClientExpansion
// Purpose: Defines the allowed realm client expansion values used by the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public enum RealmClientExpansion : byte
{

    // Enum Value: Defines the classic enum value.
    // Value: explicit expression 0.
    Classic = 0,

    // Enum Value: Defines the the burning crusade enum value.
    // Value: explicit expression 1.
    TheBurningCrusade = 1,

    // Enum Value: Defines the wrath of the lich king enum value.
    // Value: explicit expression 2.
    WrathOfTheLichKing = 2,

    // Enum Value: Defines the cataclysm enum value.
    // Value: explicit expression 3.
    Cataclysm = 3,
}

// Type: RealmBuilds
// Purpose: Provides realm builds behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RealmBuilds
{

    // Constant: Defines the vanilla1121 constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed vanilla1121 value used anywhere this rule or protocol value is needed.
    public const ushort Vanilla1121 = 5875;

    // Constant: Defines the vanilla1122 constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed vanilla1122 value used anywhere this rule or protocol value is needed.
    public const ushort Vanilla1122 = 6005;

    // Constant: Defines the vanilla1123 constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed vanilla1123 value used anywhere this rule or protocol value is needed.
    public const ushort Vanilla1123 = 6141;

    // Constant: Defines the the burning crusade243 constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed the burning crusade243 value used anywhere this rule or protocol value is needed.
    public const ushort TheBurningCrusade243 = 8606;

    // Constant: Defines the wrath335a constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed wrath335a value used anywhere this rule or protocol value is needed.
    public const ushort Wrath335a = 12340;

    // Constant: Defines the cataclysm434 constant used by the realm server authentication, realm-list, and account connection layer.
    // Value: fixed cataclysm434 value used anywhere this rule or protocol value is needed.
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

    // Method: IsSupported
    // Purpose: Validates or evaluates is supported rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns true when is supported succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsSupported(ushort build)
    {
        return SupportedBuilds.Contains(build);
    }

    // Method: IsVanilla
    // Purpose: Validates or evaluates is vanilla rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns true when is vanilla succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsVanilla(ushort build)
    {
        return build is Vanilla1121 or Vanilla1122 or Vanilla1123;
    }

    // Method: IsTheBurningCrusade
    // Purpose: Validates or evaluates is the burning crusade rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns true when is the burning crusade succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsTheBurningCrusade(ushort build)
    {
        return build is TheBurningCrusade243;
    }

    // Method: IsWrath
    // Purpose: Validates or evaluates is wrath rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns true when is wrath succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsWrath(ushort build)
    {
        return build is Wrath335a;
    }

    // Method: IsCataclysm
    // Purpose: Validates or evaluates is cataclysm rules for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns true when is cataclysm succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool IsCataclysm(ushort build)
    {
        return build is Cataclysm434;
    }

    // Method: UsesModernProofResponse
    // Purpose: Executes the uses modern proof response operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns true when uses modern proof response succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool UsesModernProofResponse(ushort build)
    {
        return !IsVanilla(build);
    }

    // Method: UsesModernRealmList
    // Purpose: Executes the uses modern realm list operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns true when uses modern realm list succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool UsesModernRealmList(ushort build)
    {
        return !IsVanilla(build);
    }

    // Method: GetExpansion
    // Purpose: Retrieves get expansion data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns the realm client expansion value produced by this operation.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
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

    // Method: TryGetVersionInfo
    // Purpose: Attempts to retrieve or parse try get version info data without treating normal misses as failures.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // - versionInfo: Version info value supplied by the caller for this operation.
    // Returns: Returns true when try get version info succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
    public static bool TryGetVersionInfo(ushort build, out RealmBuildVersionInfo versionInfo)
    {
        versionInfo = build switch
        {
            Vanilla1121 => new RealmBuildVersionInfo(1, 12, 1, Vanilla1121),
            Vanilla1122 => new RealmBuildVersionInfo(1, 12, 2, Vanilla1122),
            Vanilla1123 => new RealmBuildVersionInfo(1, 12, 3, Vanilla1123),
            TheBurningCrusade243 => new RealmBuildVersionInfo(2, 4, 3, TheBurningCrusade243),
            Wrath335a => new RealmBuildVersionInfo(3, 3, 5, Wrath335a),
            Cataclysm434 => new RealmBuildVersionInfo(4, 3, 4, Cataclysm434),
            _ => default,
        };

        return versionInfo.Build != 0;
    }

    // Method: GetDisplayName
    // Purpose: Retrieves get display name data for the realm server authentication, realm-list, and account connection layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmBuilds so callers do not duplicate validation, protocol, or persistence rules.
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
