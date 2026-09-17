namespace Rudoger.DatabaseDump;

public static class DatabaseDumpApplication
{
    public static int Run(IReadOnlyList<string> arguments, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            DateTimeOffset generatedAtUtc = DateTimeOffset.UtcNow;
            DatabaseDumpOptions options = DatabaseDumpOptions.Parse(
                arguments,
                Environment.CurrentDirectory,
                generatedAtUtc);

            if (options.ShowHelp)
            {
                WriteUsage(output);
                return 0;
            }

            string? connectionString = Environment.GetEnvironmentVariable(
                DatabaseDumpEnvironment.ConnectionStringVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"{DatabaseDumpEnvironment.ConnectionStringVariable} is missing.");
            }

            var source = new SqlServerDumpScriptSource(connectionString);
            var writer = new SqlDumpWriter(source);
            writer.Write(options.OutputPath, options.Force, generatedAtUtc);

            output.WriteLine($"Database dump created: {options.OutputPath}");
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            error.WriteLine($"Database dump failed: {exception.Message}");
            return 1;
        }
    }

    public static void WriteUsage(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        output.WriteLine("Usage: Rudoger.DatabaseDump [--output PATH] [--force]");
        output.WriteLine();
        output.WriteLine("Generates a UTF-8 T-SQL dump containing database schema and table data.");
        output.WriteLine("The default output is artifacts/database/rudoger-<UTC timestamp>.sql.");
    }
}
