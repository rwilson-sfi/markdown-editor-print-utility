using System.IO;
using System.Text.Json;
using System.Windows;
using MarkdownEditor.Core.Theming;

namespace MarkdownEditor.App;

public sealed class ThemeManager
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MarkdownEditor",
        "preferences.json");

    public ThemeDefinition Current { get; private set; } = ThemeCatalog.Get(ThemeCatalog.DefaultThemeId);

    public ThemeDefinition ApplySavedTheme()
    {
        var savedTheme = LoadSavedThemeId();
        return Apply(savedTheme, persist: false);
    }

    public ThemeDefinition Apply(string? themeId, bool persist = true)
    {
        Current = ThemeCatalog.Get(themeId);
        var resources = System.Windows.Application.Current.Resources;
        resources["AppFontFamily"] = new System.Windows.Media.FontFamily(Current.MonospaceInterface
            ? "Cascadia Mono, Consolas"
            : "Segoe UI Variable Text, Segoe UI");
        resources["WindowBackgroundBrush"] = Brush(Current.Background);
        resources["SurfaceBrush"] = Brush(Current.Card);
        resources["SurfaceMutedBrush"] = Brush(Current.Muted);
        resources["PopoverBrush"] = Brush(Current.Popover);
        resources["SidebarBrush"] = Brush(Current.SidebarBackground);
        resources["BorderBrush"] = Brush(Current.Border);
        resources["SidebarBorderBrush"] = Brush(Current.SidebarBorder);
        resources["TextPrimaryBrush"] = Brush(Current.Foreground);
        resources["TextSecondaryBrush"] = Brush(Current.MutedForeground);
        resources["PrimaryBrush"] = Brush(Current.Primary);
        resources["PrimaryTextBrush"] = Brush(Current.PrimaryForeground);
        resources["AccentBrush"] = Brush(Current.Ring);
        resources["AccentTextBrush"] = Brush(Current.RingForeground);
        resources["SecondaryBrush"] = Brush(Current.Secondary);
        resources["SecondaryTextBrush"] = Brush(Current.SecondaryForeground);
        resources["HoverBrush"] = Brush(Current.Accent);
        resources["HoverTextBrush"] = Brush(Current.AccentForeground);
        resources["InputBrush"] = Brush(Current.Input);
        resources["SelectionBrush"] = Brush(Current.Selection);
        resources["DangerBrush"] = Brush(Current.Destructive);
        resources["DangerTextBrush"] = Brush(Current.DestructiveForeground);

        if (persist)
        {
            SaveThemeId(Current.Id);
        }

        return Current;
    }

    private string? LoadSavedThemeId()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return null;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            return document.RootElement.TryGetProperty("theme", out var theme)
                ? theme.GetString()
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private void SaveThemeId(string themeId)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory, $"preferences.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new { theme = themeId }));
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Theme persistence is optional; the active in-memory theme remains usable.
        }
    }

    private static System.Windows.Media.SolidColorBrush Brush(string value)
    {
        var brush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value));
        brush.Freeze();
        return brush;
    }
}
