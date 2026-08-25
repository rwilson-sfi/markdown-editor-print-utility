using MarkdownEditor.Core.Theming;

namespace MarkdownEditor.Core.Tests;

public sealed class ThemeCatalogTests
{
    private static readonly string[] ExpectedBuiltInIds =
    [
        "nous",
        "github",
        "catppuccin",
        "everforest",
        "solarized",
        "nous-alt",
        "midnight",
        "ember",
        "mono",
        "slate",
        "cyberpunk",
    ];

    [Fact]
    public void CatalogContainsEveryHermesBuiltInThemeOnce()
    {
        Assert.Equal(ExpectedBuiltInIds, ThemeCatalog.All.Select(theme => theme.Id));
        Assert.Equal(
            ThemeCatalog.All.Count,
            ThemeCatalog.All.Select(theme => theme.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void CatalogOffersBothLightAndDarkPalettes()
    {
        Assert.Equal(6, ThemeCatalog.Search(null, isDark: false).Count);
        Assert.Equal(5, ThemeCatalog.Search(null, isDark: true).Count);
    }

    [Theory]
    [InlineData("mauve", "catppuccin")]
    [InlineData("mission", "nous-alt")]
    [InlineData("matrix", "cyberpunk")]
    [InlineData("SLATE", "slate")]
    public void SearchFindsPalettesByNameOrMood(string query, string expectedId)
    {
        var result = Assert.Single(ThemeCatalog.Search(query));

        Assert.Equal(expectedId, result.Id);
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
