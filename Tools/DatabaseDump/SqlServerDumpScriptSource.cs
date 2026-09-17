using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Rudoger.DatabaseDump;

public sealed class SqlServerDumpScriptSource : IDumpScriptSource
{
    private readonly string connectionString;
    private readonly string databaseName;

    public SqlServerDumpScriptSource(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = "Rudoger.DatabaseDump",
        };
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new ArgumentException(
                "The database dump connection string must contain Database or Initial Catalog.",
                nameof(connectionString));
        }

        this.connectionString = builder.ConnectionString;
        databaseName = builder.InitialCatalog;
    }

    public IEnumerable<string> ReadBatches()
    {
        using var sqlConnection = new SqlConnection(connectionString);
        var serverConnection = new ServerConnection(sqlConnection)
        {
            StatementTimeout = 0,
        };
        var server = new Server(serverConnection);

        try
        {
            Database database = server.Databases[databaseName]
                ?? throw new InvalidOperationException($"Database '{databaseName}' was not found.");
            Transfer transfer = CreateTransfer(database);

            foreach (string batch in transfer.EnumScriptTransfer())
            {
                yield return batch;
            }
        }
        finally
        {
            if (serverConnection.IsOpen)
            {
                serverConnection.Disconnect();
            }
        }
    }

    private static Transfer CreateTransfer(Database database)
    {
        var options = new ScriptingOptions
        {
            AnsiPadding = true,
            DriAll = true,
            FullTextIndexes = true,
            IncludeHeaders = true,
            Indexes = true,
            Permissions = true,
            SchemaQualify = true,
            ScriptBatchTerminator = true,
            ScriptData = true,
            ScriptSchema = true,
            Triggers = true,
            WithDependencies = true,
        };

        return new Transfer(database)
        {
            CopyAllLogins = false,
            CopyAllObjects = true,
            CopyAllUsers = false,
            CopyData = true,
            CopySchema = true,
            Options = options,
            PreserveDbo = true,
        };
    }
}
