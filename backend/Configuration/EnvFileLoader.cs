namespace backend.Configuration;

public static class EnvFileLoader
{
    public static void LoadFromRepoRoot(string contentRootPath)
    {
        var candidates = new[]
        {
            Path.Combine(contentRootPath, ".env"),
            Path.GetFullPath(Path.Combine(contentRootPath, "..", ".env")),
        };

        foreach (var path in candidates.Distinct(StringComparer.Ordinal))
        {
            LoadFile(path);
        }
    }

    private static void LoadFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2
                && ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
