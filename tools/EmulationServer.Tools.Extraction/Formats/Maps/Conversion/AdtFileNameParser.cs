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

using System.Text.RegularExpressions;


/**
 * File overview: tools/EmulationServer.Tools.Extraction/Formats/Maps/Conversion/AdtFileNameParser.cs
 * Documents the AdtFileNameParser source file in the client data extraction and conversion tooling area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

/**
 * Owns the adt file name parser behavior for the client data extraction and conversion tooling layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public static partial class AdtFileNameParser
{
    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of AdtFileNameParser and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public static bool TryParse(string path, out string mapDirectoryName, out int tileX, out int tileY)
    {
        mapDirectoryName = string.Empty;
        tileX = 0;
        tileY = 0;

        string fileName = Path.GetFileNameWithoutExtension(path);
        Match match = AdtFileNameRegex().Match(fileName);

        if (!match.Success)
        {
            return false;
        }

        mapDirectoryName = match.Groups["map"].Value;
        return int.TryParse(match.Groups["x"].Value, out tileX) && int.TryParse(match.Groups["y"].Value, out tileY);
    }

    /**
     * Performs the adt file name regex operation for the client data extraction and conversion tooling workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
    [GeneratedRegex("^(?<map>.+)_(?<x>\\d{1,2})_(?<y>\\d{1,2})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex AdtFileNameRegex();
}
