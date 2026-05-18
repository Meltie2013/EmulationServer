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

namespace EmulationServer.Tools.Extraction.Formats.Maps.Conversion;

public static partial class AdtFileNameParser
{
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

    [GeneratedRegex("^(?<map>.+)_(?<x>\\d{1,2})_(?<y>\\d{1,2})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex AdtFileNameRegex();
}
