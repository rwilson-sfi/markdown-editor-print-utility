namespace MarkdownEditor.Core.Theming;

public sealed record ThemeDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Description { get; init; }

    public required bool IsDark { get; init; }

    public required bool MonospaceInterface { get; init; }

    public required string Background { get; init; }

    public required string Foreground { get; init; }

    public required string Card { get; init; }

    public required string Muted { get; init; }

    public required string MutedForeground { get; init; }

    public required string Popover { get; init; }

    public required string Primary { get; init; }

    public required string PrimaryForeground { get; init; }

    public required string Secondary { get; init; }

    public required string SecondaryForeground { get; init; }

    public required string Accent { get; init; }

    public required string AccentForeground { get; init; }

    public required string Border { get; init; }

    public required string Input { get; init; }

    public required string Ring { get; init; }

    public required string RingForeground { get; init; }

    public required string SidebarBackground { get; init; }

    public required string SidebarBorder { get; init; }

    public required string Selection { get; init; }

    public required string Destructive { get; init; }

    public required string DestructiveForeground { get; init; }

    public IEnumerable<string> Colors =>
    [
        Background,
        Foreground,
        Card,
        Muted,
        MutedForeground,
        Popover,
        Primary,
        PrimaryForeground,
        Secondary,
        SecondaryForeground,
        Accent,
        AccentForeground,
        Border,
        Input,
        Ring,
        RingForeground,
        SidebarBackground,
        SidebarBorder,
        Selection,
        Destructive,
        DestructiveForeground,
    ];
}
