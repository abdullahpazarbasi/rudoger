using Rudoger.DatabaseDump;

namespace Rudoger.UnitTests;

public sealed class SqlDumpWriterTests
{
    private static readonly DateTimeOffset GeneratedAtUtc =
        new(2026, 9, 17, 12, 34, 56, TimeSpan.Zero);

    [Fact]
    public void WritesSchemaAndDataBatchesAsUtf8SqlText()
    {
        string testDirectory = CreateTestDirectory();
        string outputPath = Path.Combine(testDirectory, "nested", "rudoger.sql");

        try
        {
            var source = new StubDumpScriptSource(
            [
                "CREATE TABLE [product].[Products] ([Id] uniqueidentifier NOT NULL);",
                "INSERT [product].[Products] ([Id]) VALUES ('0199f00d-0000-7000-8000-000000000001');",
            ]);
            var writer = new SqlDumpWriter(source);

            writer.Write(outputPath, force: false, GeneratedAtUtc);

            byte[] bytes = File.ReadAllBytes(outputPath);
            string sql = File.ReadAllText(outputPath);
            bool hasUtf8ByteOrderMark = bytes.Length >= 3
                && bytes[0] == 0xef
                && bytes[1] == 0xbb
                && bytes[2] == 0xbf;
            Assert.False(hasUtf8ByteOrderMark);
            Assert.Contains("-- Rudoger SQL database dump", sql, StringComparison.Ordinal);
            Assert.Contains("CREATE TABLE [product].[Products]", sql, StringComparison.Ordinal);
            Assert.Contains("INSERT [product].[Products]", sql, StringComparison.Ordinal);
            Assert.Equal(3, sql.Split('\n').Count(line => line.Trim().Equals("GO", StringComparison.Ordinal)));
            Assert.Contains("-- End of Rudoger SQL database dump.", sql, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void ExistingOutputIsOnlyReplacedWhenForced()
    {
        string testDirectory = CreateTestDirectory();
        string outputPath = Path.Combine(testDirectory, "rudoger.sql");

        try
        {
            File.WriteAllText(outputPath, "original");
            var writer = new SqlDumpWriter(new StubDumpScriptSource(["SELECT 1;\nGO"]));

            Assert.Throws<IOException>(() => writer.Write(outputPath, force: false, GeneratedAtUtc));
            Assert.Equal("original", File.ReadAllText(outputPath));

            writer.Write(outputPath, force: true, GeneratedAtUtc);
            Assert.Contains("SELECT 1;", File.ReadAllText(outputPath), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void FailedGenerationLeavesNoPartialDump()
    {
        string testDirectory = CreateTestDirectory();
        string outputPath = Path.Combine(testDirectory, "rudoger.sql");

        try
        {
            var writer = new SqlDumpWriter(new StubDumpScriptSource(FailingBatches()));

            Assert.Throws<InvalidOperationException>(() =>
                writer.Write(outputPath, force: false, GeneratedAtUtc));

            Assert.False(File.Exists(outputPath));
            Assert.Empty(Directory.EnumerateFiles(testDirectory));
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "rudoger-database-dump-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static IEnumerable<string> FailingBatches()
    {
        yield return "SELECT 1;\nGO";
        throw new InvalidOperationException("Scripting failed.");
    }
}
