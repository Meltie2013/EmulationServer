
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
