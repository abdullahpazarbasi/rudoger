namespace Rudoger.DatabaseDump;

public interface IDumpScriptSource
{
    IEnumerable<string> ReadBatches();
}
