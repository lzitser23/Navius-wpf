using System.Linq;

namespace Navius.Wpf.Captures;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string? outDir = null;
        List<string>? pageFilter = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out" when i + 1 < args.Length:
                    outDir = args[++i];
                    break;
                case "--pages" when i + 1 < args.Length:
                    pageFilter = args[++i]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(outDir))
        {
            Console.Error.WriteLine("Usage: Navius.Wpf.Captures --out <dir> [--pages <label,label,...>]");
            return 2;
        }

        return CaptureRunner.Run(outDir, pageFilter);
    }
}
