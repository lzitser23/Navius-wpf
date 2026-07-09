using System.Text.RegularExpressions;

namespace Navius.Wpf.Captures;

/// <summary>Kebab-cases a PascalCase nav label or theme name, e.g. AlertDialog -&gt; alert-dialog.</summary>
internal static partial class Slug
{
    public static string From(string pascalCase) => WordBoundary().Replace(pascalCase, "-").ToLowerInvariant();

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}
