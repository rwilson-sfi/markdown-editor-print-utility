using MarkdownEditor.Core.Theming;

namespace MarkdownEditor.Core.Tests;

public sealed class ThemeCatalogTests
{
    [Fact]
    public void CatalogContainsFiveDistinctThemes()
    {
        Assert.Equal(5, ThemeCatalog.All.Count);
        Assert.Equal(5, ThemeCatalog.All.Select(theme => theme.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void EveryThemeHasValidRgbHexColors()
    {
        foreach (var theme in ThemeCatalog.All)
        {
            Assert.All(theme.Colors, color =>
            {
                Assert.Matches("^#[0-9A-Fa-f]{6}$", color);
            });
        }
    }

    [Fact]
    public void UnknownThemeFallsBackToNous()
    {
        Assert.Equal(ThemeCatalog.DefaultThemeId, ThemeCatalog.Get("not-a-theme").Id);
    }
}
