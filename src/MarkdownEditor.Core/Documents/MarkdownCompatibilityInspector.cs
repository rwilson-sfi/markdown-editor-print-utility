using System.Text.RegularExpressions;

namespace MarkdownEditor.Core.Documents;

public static partial class MarkdownCompatibilityInspector
{
    public static IReadOnlyList<string> FindRichEditorRisks(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var risks = new HashSet<string>(StringComparer.Ordinal);

        if (MathematicalFencePattern().IsMatch(markdown))
        {
            risks.Add("mathematical fenced blocks");
        }

        if (FootnotePattern().IsMatch(markdown))
        {
            risks.Add("footnotes");
        }

        if (MathPattern().IsMatch(markdown))
        {
            risks.Add("mathematical notation");
        }

        if (RawHtmlPattern().IsMatch(markdown))
        {
            risks.Add("embedded HTML");
        }

        return risks.ToArray();
    }

    [GeneratedRegex(@"(?im)^\s*`{3,}\s*(?:math|latex)\b")]
    private static partial Regex MathematicalFencePattern();

    [GeneratedRegex(@"(?m)\[\^[^\]\r\n]+\](?::)?")]
    private static partial Regex FootnotePattern();

    [GeneratedRegex(@"(?m)(?:^|\s)\$\$|\\\(|\\\[")]
    private static partial Regex MathPattern();

    [GeneratedRegex(@"(?im)^\s*(?:<!--[\s\S]*?-->|</?[a-z][a-z0-9-]*(?:\s+[^>]*)?/?>)\s*$")]
    private static partial Regex RawHtmlPattern();
}
