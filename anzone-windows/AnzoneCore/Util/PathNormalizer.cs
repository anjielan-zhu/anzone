namespace AnzoneCore.Util;

public static class PathNormalizer
{
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        return path.Trim().Replace('/', '\\').ToLowerInvariant();
    }

    public static bool IsUnderWindows(string? path)
    {
        var n = Normalize(path);
        return n.StartsWith(@"c:\windows\");
    }
}
