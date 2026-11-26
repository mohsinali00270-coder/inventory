// Minimal stand-ins for the few MAUI types the linked logic files touch, so they compile in a plain net8.0 project.
public enum FlowDirection { MatchParent, LeftToRight, RightToLeft }

public static class Preferences
{
    private static readonly Dictionary<string, object> Store = new();
    public static T Get<T>(string key, T defaultValue) => Store.TryGetValue(key, out var v) ? (T)v : defaultValue;
    public static void Set<T>(string key, T value) => Store[key!] = value!;
}

public static class FileSystem
{
    public static string AppDataDirectory => Path.GetTempPath();
    public static string CacheDirectory => Path.GetTempPath();
}
