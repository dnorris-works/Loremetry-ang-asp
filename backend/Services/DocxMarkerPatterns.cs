using System.Text.RegularExpressions;

namespace backend.Services;

internal static class DocxMarkerPatterns
{
    private static readonly Regex SceneMarkerExpression = new(
        @"^(?:\*{3,}|-{3,}|#{1,3}\s*scene\b.*|scene\s+(?:break|\d+)\b.*)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsSceneMarkerLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (SceneMarkerExpression.IsMatch(trimmed))
        {
            return true;
        }

        return trimmed is "***" or "* * *" or "# # #" or "~~~";
    }
}
