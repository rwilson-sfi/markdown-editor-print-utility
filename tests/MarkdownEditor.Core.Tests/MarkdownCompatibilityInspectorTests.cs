using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.Core.Tests;

public sealed class MarkdownCompatibilityInspectorTests
{
    [Fact]
    public void FindRichEditorRisks_ReturnsNoRisksForSupportedGfm()
    {
        const string markdown = """
            # Heading

            - [x] Task

            | A | B |
            | - | - |
            | 1 | 2 |
            """;

        Assert.Empty(MarkdownCompatibilityInspector.FindRichEditorRisks(markdown));
    }

    [Fact]
    public void FindRichEditorRisks_IdentifiesOutOfContractSyntax()
    {
        const string markdown = """
            ```mermaid
            graph LR
            ```

            A reference[^note].

            $$
            x = 1
            $$

            <details>
            """;

        var risks = MarkdownCompatibilityInspector.FindRichEditorRisks(markdown);

        Assert.Contains("Mermaid or mathematical fenced blocks", risks);
        Assert.Contains("footnotes", risks);
        Assert.Contains("mathematical notation", risks);
        Assert.Contains("embedded HTML", risks);
    }
}
