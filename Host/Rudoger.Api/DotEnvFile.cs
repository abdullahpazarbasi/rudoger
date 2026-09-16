namespace Rudoger.Api;

public static class DotEnvFile
{
    public static IReadOnlyDictionary<string, string?> Read(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string?>();
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new InvalidOperationException($"Invalid dotenv entry in '{path}': {rawLine}");
            }

            string key = line[..separator].Trim().Replace("__", ":", StringComparison.Ordinal);
            string value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            values[key] = value;
        }

        return values;
    }
}
