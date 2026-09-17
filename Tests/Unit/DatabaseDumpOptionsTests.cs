using Rudoger.DatabaseDump;

namespace Rudoger.UnitTests;

public sealed class DatabaseDumpOptionsTests
{
    private static readonly DateTimeOffset GeneratedAtUtc =
        new(2026, 9, 17, 12, 34, 56, TimeSpan.Zero);

    [Fact]
    public void DefaultOutputIsTimestampedSqlFileUnderArtifacts()
    {
        string workingDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "rudoger-repository"));

        DatabaseDumpOptions options = DatabaseDumpOptions.Parse([], workingDirectory, GeneratedAtUtc);

        Assert.Equal(
            Path.Combine(
                workingDirectory,
                "artifacts",
                "database",
                "rudoger-20260917T123456Z.sql"),
            options.OutputPath);
        Assert.False(options.Force);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void ExplicitRelativeOutputAndForceAreParsed()
    {
        string workingDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "rudoger-repository"));

        DatabaseDumpOptions options = DatabaseDumpOptions.Parse(
            ["--output", "backups/rudoger.sql", "--force"],
            workingDirectory,
            GeneratedAtUtc);

        Assert.Equal(Path.Combine(workingDirectory, "backups", "rudoger.sql"), options.OutputPath);
        Assert.True(options.Force);
    }

    [Theory]
    [InlineData("--output")]
    [InlineData("--unknown")]
    public void InvalidArgumentsAreRejected(string argument)
    {
        Assert.Throws<ArgumentException>(() => DatabaseDumpOptions.Parse(
            [argument],
            Environment.CurrentDirectory,
            GeneratedAtUtc));
    }

    [Fact]
    public void NonSqlOutputIsRejected()
    {
        Assert.Throws<ArgumentException>(() => DatabaseDumpOptions.Parse(
            ["--output", "rudoger.dump"],
            Environment.CurrentDirectory,
            GeneratedAtUtc));
    }
}
