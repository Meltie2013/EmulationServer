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

using System.Diagnostics.CodeAnalysis;


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Client/ClientBuilds.cs
 * Documents the ClientBuilds source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Client;

/**
 * Owns the client builds behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static class ClientBuilds
{
    /**
     * Defines the constant value for classic 1121.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Classic1121 = 5875;
    /**
     * Defines the constant value for classic 1122.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Classic1122 = 6005;
    /**
     * Defines the constant value for classic 1123.
     * Keeping this value named avoids duplicated magic strings or numbers in packet, configuration, and data-loading code.
     */
    public const ushort Classic1123 = 6141;
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

    private static readonly IReadOnlyDictionary<ushort, ClientBuildInfo> BuildsByNumber =
        new Dictionary<ushort, ClientBuildInfo>
        {
            [Classic1121] = new(Classic1121, "1.12.1", SupportedClientExpansion.Classic, "Mangos Zero"),
            [Classic1122] = new(Classic1122, "1.12.2", SupportedClientExpansion.Classic, "Mangos Zero"),
            [Classic1123] = new(Classic1123, "1.12.3", SupportedClientExpansion.Classic, "Mangos Zero"),
            [TheBurningCrusade243] = new(TheBurningCrusade243, "2.4.3", SupportedClientExpansion.TheBurningCrusade, "Mangos One"),
            [Wrath335a] = new(Wrath335a, "3.3.5a", SupportedClientExpansion.WrathOfTheLichKing, "Mangos Two"),
        };

    /**
      * Gets or stores the all value used by ClientBuilds.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public static IReadOnlyCollection<ClientBuildInfo> All => BuildsByNumber.Values.ToArray();

    /**
     * Determines whether supported for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static bool IsSupported(ushort build)
    {
        return BuildsByNumber.ContainsKey(build);
    }

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of ClientBuilds and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool TryGet(ushort build, [NotNullWhen(true)] out ClientBuildInfo? buildInfo)
    {
        return BuildsByNumber.TryGetValue(build, out buildInfo);
    }

    /**
     * Performs the require operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    public static ClientBuildInfo Require(ushort build)
    {
        if (TryGet(build, out ClientBuildInfo? buildInfo))
        {
            return buildInfo;
        }

        throw new NotSupportedException($"Client build {build} is not supported by these extraction tools.");
    }
}
