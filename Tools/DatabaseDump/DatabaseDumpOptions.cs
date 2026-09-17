using System.Globalization;

namespace Rudoger.DatabaseDump;

public sealed record DatabaseDumpOptions(string OutputPath, bool Force, bool ShowHelp)
{
    public static DatabaseDumpOptions Parse(
        IReadOnlyList<string> arguments,
        string workingDirectory,
        DateTimeOffset generatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        string? outputPath = null;
        bool force = false;
        bool showHelp = false;

        for (int index = 0; index < arguments.Count; index++)
        {
            switch (arguments[index])
            {
                case "-o":
                case "--output":
                    if (index + 1 >= arguments.Count || string.IsNullOrWhiteSpace(arguments[index + 1]))
                    {
                        throw new ArgumentException($"{arguments[index]} requires a path.");
                    }

                    outputPath = arguments[++index];
                    break;
                case "-f":
                case "--force":
                    force = true;
                    break;
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arguments[index]}");
            }
        }

        outputPath ??= Path.Combine(
            "artifacts",
            "database",
            $"rudoger-{generatedAtUtc.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)}.sql");

        string fullOutputPath = Path.GetFullPath(outputPath, workingDirectory);
        if (!Path.GetExtension(fullOutputPath).Equals(".sql", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Output path must use the .sql extension.");
        }

        return new DatabaseDumpOptions(fullOutputPath, force, showHelp);
    }
}
