using System.Text;

namespace Rudoger.DatabaseDump;

public sealed class SqlDumpWriter(IDumpScriptSource source)
{
    private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new(false);

    public void Write(string outputPath, bool force, DateTimeOffset generatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        string fullOutputPath = Path.GetFullPath(outputPath);
        string? outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output path must include a directory.", nameof(outputPath));
        }

        Directory.CreateDirectory(outputDirectory);
        if (File.Exists(fullOutputPath) && !force)
        {
            throw new IOException($"Output already exists: {fullOutputPath}. Use --force to replace it.");
        }

        string temporaryPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(fullOutputPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            using (var writer = new StreamWriter(stream, Utf8WithoutByteOrderMark))
            {
                WriteHeader(writer, generatedAtUtc);
                foreach (string batch in source.ReadBatches())
                {
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }

                    string trimmedBatch = batch.TrimEnd();
                    writer.WriteLine(trimmedBatch);
                    if (!EndsWithBatchTerminator(trimmedBatch))
                    {
                        writer.WriteLine("GO");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("-- End of Rudoger SQL database dump.");
            }

            File.Move(temporaryPath, fullOutputPath, force);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static void WriteHeader(TextWriter writer, DateTimeOffset generatedAtUtc)
    {
        writer.WriteLine("-- Rudoger SQL database dump");
        writer.WriteLine($"-- Generated at {generatedAtUtc.UtcDateTime:O}");
        writer.WriteLine("-- Run this file against an existing empty database with sqlcmd.");
        writer.WriteLine();
        writer.WriteLine("SET XACT_ABORT ON;");
        writer.WriteLine("GO");
        writer.WriteLine();
    }

    private static bool EndsWithBatchTerminator(string batch)
    {
        int lastNewLine = Math.Max(batch.LastIndexOf('\r'), batch.LastIndexOf('\n'));
        ReadOnlySpan<char> lastLine = batch.AsSpan(lastNewLine + 1).Trim();
        return lastLine.Equals("GO", StringComparison.OrdinalIgnoreCase);
    }
}
