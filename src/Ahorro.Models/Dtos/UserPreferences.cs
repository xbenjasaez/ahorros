namespace Ahorro.Models.Dtos;

public class UserPreferences
{
    public string CurrencyCode { get; set; } = "CLP";
    public string ThemeVariant { get; set; } = "dark-premium";
    public string AccentHex { get; set; } = "#27D3FF";
    public decimal GoalDefaultMonthlyPace { get; set; } = 50_000m;
    public bool GoalShowProjections { get; set; } = true;
    public bool GoalAutoCelebrate { get; set; } = true;
    public bool GoalSuggestContributions { get; set; } = true;
    public string ExportDefaultFolder { get; set; } = string.Empty;
    public bool ExportIncludeNotes { get; set; } = true;
    public bool ExportPdfCharts { get; set; } = true;
    public bool ExportExcelAutoOpen { get; set; }
    public string ExportFileNamePrefix { get; set; } = "Ahorro";
    public bool MultiUserEnabled { get; set; }
}
