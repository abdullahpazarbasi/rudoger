namespace Rudoger.EndToEndTests;

[CollectionDefinition(Name)]
public sealed class ApiTestSuite : ICollectionFixture<RunningApiFixture>
{
    public const string Name = "Running API";
}
