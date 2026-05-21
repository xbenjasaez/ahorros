namespace Ahorro.Helpers;

public static class GoalIconHelper
{
    private static readonly Dictionary<string, string> Glyphs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["target"] = "◎",
        ["home"] = "⌂",
        ["engine"] = "⚙",
        ["shield"] = "⛨",
        ["car"] = "◈",
        ["travel"] = "✈",
        ["gift"] = "✦",
        ["education"] = "◆",
        ["health"] = "♥",
        ["tech"] = "⬡",
        ["star"] = "★",
        ["rocket"] = "▲"
    };

    public static string GetGlyph(string? iconKey) =>
        !string.IsNullOrWhiteSpace(iconKey) && Glyphs.TryGetValue(iconKey, out var g) ? g : Glyphs["target"];

    public static IReadOnlyList<GoalIconOption> AllOptions { get; } =
    [
        new("target", "◎ Objetivo"),
        new("home", "⌂ Hogar"),
        new("car", "◈ Vehículo"),
        new("engine", "⚙ Proyecto"),
        new("shield", "⛨ Emergencia"),
        new("travel", "✈ Viaje"),
        new("education", "◆ Estudio"),
        new("health", "♥ Salud"),
        new("tech", "⬡ Tecnología"),
        new("gift", "✦ Regalo"),
        new("rocket", "▲ Lanzamiento"),
        new("star", "★ Especial")
    ];
}

public record GoalIconOption(string Key, string Label);
