using Rudoger.DatabaseDump;

namespace Rudoger.UnitTests;

public sealed class StubDumpScriptSource(IEnumerable<string> batches) : IDumpScriptSource
{
    public IEnumerable<string> ReadBatches()
    {
        return batches;
    }
}
