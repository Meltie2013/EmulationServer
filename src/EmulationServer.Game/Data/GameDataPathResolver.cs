namespace EmulationServer.Game.Data;

public static class GameDataPathResolver
{
    public static string ResolveDirectory(string dataDirectory, string childDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new ArgumentException("Data directory is required.", nameof(dataDirectory));
        }

        if (string.IsNullOrWhiteSpace(childDirectory))
        {
            throw new ArgumentException("Child directory is required.", nameof(childDirectory));
        }

        return Path.GetFullPath(Path.IsPathRooted(childDirectory)
            ? childDirectory
            : Path.Combine(dataDirectory, childDirectory));
    }
}
