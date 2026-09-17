namespace Rudoger.Api;

/// <summary>
/// The default accounts the seed command creates. Seed data is a deployment concern, so it lives
/// with the host commands rather than inside an application use case.
/// </summary>
public static class SeedUsers
{
    public static IReadOnlyList<(string Username, string Password)> Default { get; } =
    [
        ("abdullah", "12345678"),
        ("murat", "12345678"),
        ("gokhan", "12345678"),
    ];
}
