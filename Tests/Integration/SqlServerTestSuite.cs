namespace Rudoger.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class SqlServerTestSuite : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server integration";
}
