namespace Rudoger.DatabaseDump;

public static class Program
{
    public static int Main(string[] args)
    {
        return DatabaseDumpApplication.Run(args, Console.Out, Console.Error);
    }
}
