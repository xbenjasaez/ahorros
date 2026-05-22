using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Goals;

public partial class GoalsViewModel : ViewModelBase, ILoadable
{
    private const decimal DefaultMonthlyPace = 50_000m;
    private readonly ISavingsGoalService _goals;
    private readonly IBudgetService _budget;
    private readonly IExcelExportService _excel;
    private readonly IPdfExportService _pdf;
    private Guid? _editingGoalId;

    [ObservableProperty] private string _totalSaved = "$0";
    [ObservableProperty] private string _totalTargetDisplay = "$0";
    [ObservableProperty] private string _globalProgressText = "0%";
    [ObservableProperty] private string _globalProgressSubtitle = "Sin metas activas";
    [ObservableProperty] private string _activeGoalsLabel = "0 metas";
    [ObservableProperty] private string _portfolioProjection = string.Empty;
    [ObservableProperty] private string _totalTargetLabel = "$0 objetivo";
    [ObservableProperty] private string _totalRemainingLabel = "$0 por alcanzar";
    [ObservableProperty] private string _nextGoalTitle = "—";
    [ObservableProperty] private string _nextGoalSubtitle = "Sin fecha próxima";
    [ObservableProperty] private string _contributeAmount = "50000";
    [ObservableProperty] private bool _hasGoals;
    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private string _editorTitle = "Editar meta";
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editTargetInput = "0";
    [ObservableProperty] private string _editDateInput = string.Empty;
    [ObservableProperty] private LookupItem? _editCategory;
    [ObservableProperty] private GoalColorPreset? _editColor;
    [ObservableProperty] private GoalIconOption? _editIcon;
    [ObservableProperty] private bool _editAutoFromBudget;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatusMessage;
    [ObservableProperty] private GoalCardItem? _selectedGoal;
    [ObservableProperty] private GoalCardItem? _quickContributeGoal;
    [ObservableProperty] private string _detailName = string.Empty;
    [ObservableProperty] private string _detailStatus = string.Empty;
    [ObservableProperty] private string _detailProgressText = string.Empty;
    [ObservableProperty] private string _detailPaceLabel = string.Empty;
    [ObservableProperty] private string _detailExpectedVsActual = string.Empty;
    [ObservableProperty] private string _detailCompletionEstimate = string.Empty;
    [ObservableProperty] private string _detailCategory = string.Empty;
    [ObservableProperty] private double _detailProgress;
    [ObservableProperty] private bool _showDetailPanel;
    [ObservableProperty] private bool _showDetailPlaceholder = true;

    public ObservableCollection<GoalCardItem> GoalCards { get; } = [];
    public ObservableCollection<GoalContributionListItem> RecentContributions { get; } = [];
    public ObservableCollection<LookupItem> CategoryOptions { get; } = [];
    public ObservableCollection<GoalColorPreset> ColorPresets { get; } = [];
    public ObservableCollection<GoalIconOption> IconOptions { get; } = [];

    partial void OnStatusMessageChanged(string value) => HasStatusMessage = !string.IsNullOrWhiteSpace(value);

    partial void OnSelectedGoalChanged(GoalCardItem? value)
    {
        ShowDetailPanel = value != null && !IsEditorOpen;
        ShowDetailPlaceholder = value == null && HasGoals && !IsEditorOpen;
    }

    partial void OnIsEditorOpenChanged(bool value)
    {
        ShowDetailPanel = SelectedGoal != null && !value;
        ShowDetailPlaceholder = SelectedGoal == null && HasGoals && !value;
    }

    partial void OnHasGoalsChanged(bool value)
    {
        ShowDetailPlaceholder = SelectedGoal == null && value && !IsEditorOpen;
    }

    partial void OnQuickContributeGoalChanged(GoalCardItem? value)
    {
        if (value != null && SelectedGoal?.Id != value.Id)
            _ = SelectGoalAsync(value);
    }

    public GoalsViewModel(ISavingsGoalService goals, IBudgetService budget, IExcelExportService excel, IPdfExportService pdf)
    {
        Title = "Metas de ahorro";
        _goals = goals;
        _budget = budget;
        _excel = excel;
        _pdf = pdf;
        SeedColorPresets();
        foreach (var icon in GoalIconHelper.AllOptions)
            IconOptions.Add(icon);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await ReloadCategoriesAsync();
            var summary = await _goals.GetSummaryAsync();
            var list = await _goals.GetActiveGoalsAsync();

            TotalSaved = ClpFormatter.Format(summary.TotalSaved);
            TotalTargetDisplay = ClpFormatter.Format(summary.TotalTarget);
            ActiveGoalsLabel = summary.ActiveGoalsCount == 1 ? "1 meta activa" : $"{summary.ActiveGoalsCount} metas activas";
            PortfolioProjection = summary.ProjectionLabel;
            TotalTargetLabel = $"{ClpFormatter.FormatCompact(summary.TotalTarget)} objetivo total";
            TotalRemainingLabel = $"{ClpFormatter.FormatCompact(summary.TotalRemaining)} por alcanzar";

            var globalPct = summary.TotalTarget > 0
                ? (double)(summary.TotalSaved / summary.TotalTarget * 100)
                : 0;
            GlobalProgressText = $"{globalPct:0.#}%";
            GlobalProgressSubtitle = summary.ActiveGoalsCount == 0
                ? "Crea tu primera meta"
                : $"de {ClpFormatter.FormatCompact(summary.TotalTarget)} en juego";

            ApplyNextGoalHighlight(list);

            var previousId = SelectedGoal?.Id ?? QuickContributeGoal?.Id;
            GoalCards.Clear();
            foreach (var g in list)
                GoalCards.Add(MapCard(g));

            HasGoals = GoalCards.Count > 0;

            if (HasGoals)
            {
                var pick = previousId.HasValue
                    ? GoalCards.FirstOrDefault(g => g.Id == previousId.Value)
                    : null;
                pick ??= GoalCards[0];
                await SelectGoalAsync(pick);
            }
            else
            {
                SelectedGoal = null;
                QuickContributeGoal = null;
                ClearDetail();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyNextGoalHighlight(IReadOnlyList<SavingsGoal> list)
    {
        var next = list
            .Where(g => g.TargetAmount > g.AccumulatedAmount && g.TargetDate.HasValue)
            .OrderBy(g => g.TargetDate)
            .FirstOrDefault();

        if (next == null)
        {
            NextGoalTitle = list.Count == 0 ? "Sin metas" : "Sin fecha límite";
            NextGoalSubtitle = list.Count == 0 ? "Agrega una meta para comenzar" : "Todas abiertas o cumplidas";
            return;
        }

        var days = (next.TargetDate!.Value.Date - DateTime.Today).Days;
        NextGoalTitle = next.Name;
        NextGoalSubtitle = days < 0
            ? $"Venció hace {Math.Abs(days)} días · faltan {ClpFormatter.FormatCompact(next.TargetAmount - next.AccumulatedAmount)}"
            : days == 0
                ? "Fecha objetivo hoy"
                : $"En {days} días · {ClpFormatter.FormatCompact(next.TargetAmount - next.AccumulatedAmount)} restantes";
    }

    private void SeedColorPresets()
    {
        foreach (var hex in new[] { "#27D3FF", "#35E0A1", "#9B7AFF", "#FFB84D", "#FF6B6B", "#E8EDF5" })
            ColorPresets.Add(new GoalColorPreset { Hex = hex, Swatch = BrushHelper.FromHex(hex) });
        EditColor = ColorPresets[1];
    }

    private async Task ReloadCategoriesAsync()
    {
        var cats = await _budget.GetCategoriesAsync();
        CategoryOptions.Clear();
        CategoryOptions.Add(new LookupItem { Id = null, Name = "Sin vínculo a presupuesto" });
        foreach (var c in cats.OrderBy(c => c.Name))
            CategoryOptions.Add(new LookupItem { Id = c.Id, Name = c.Name });
    }

    private static GoalCardItem MapCard(SavingsGoal g)
    {
        var remaining = Math.Max(0, g.TargetAmount - g.AccumulatedAmount);
        var pct = g.TargetAmount > 0 ? (double)(g.AccumulatedAmount / g.TargetAmount * 100) : 0;
        var progress = Math.Min(1, pct / 100);
        var isCompleted = g.TargetAmount > 0 && remaining <= 0;
        var (statusLabel, statusBrush) = BuildStatus(g, pct, isCompleted);

        return new GoalCardItem
        {
            Id = g.Id,
            Name = g.Name,
            IconKey = g.IconKey,
            IconGlyph = GoalIconHelper.GetGlyph(g.IconKey),
            Accumulated = ClpFormatter.Format(g.AccumulatedAmount),
            Target = ClpFormatter.Format(g.TargetAmount),
            Remaining = ClpFormatter.Format(remaining),
            RemainingLabel = isCompleted ? "Completada" : "Te faltan",
            Progress = progress,
            PercentText = $"{pct:0.#}%",
            TargetDate = g.TargetDate?.ToString("dd MMM yyyy") ?? "Sin fecha límite",
            DaysLeftLabel = BuildDaysLeftLabel(g.TargetDate),
            Projection = BuildGoalProjection(g, remaining),
            CategoryName = g.Category?.Name ?? string.Empty,
            HasCategoryLink = g.CategoryId.HasValue,
            IsCompleted = isCompleted,
            StatusLabel = statusLabel,
            StatusBrush = statusBrush,
            ColorHex = g.ColorHex,
            AccentBrush = BrushHelper.FromHex(g.ColorHex),
            GlowBrush = GoalGlowHelper.GlowFromHex(g.ColorHex),
            TrackBrush = GoalGlowHelper.TrackBrush()
        };
    }

    private static (string Label, System.Windows.Media.Brush Brush) BuildStatus(SavingsGoal g, double pct, bool isCompleted)
    {
        if (isCompleted)
            return ("Completada", BrushHelper.FromHex("#35E0A1"));

        if (g.TargetDate.HasValue)
        {
            var days = (g.TargetDate.Value.Date - DateTime.Today).Days;
            if (days < 0)
                return ("Retrasada", BrushHelper.FromHex("#FF6B6B"));
        }

        if (pct >= 75)
            return ("Avanzada", BrushHelper.FromHex("#27D3FF"));

        return ("En curso", BrushHelper.FromHex("#FFB84D"));
    }

    private static string BuildDaysLeftLabel(DateTime? targetDate)
    {
        if (!targetDate.HasValue) return "Tiempo abierto";
        var days = (targetDate.Value.Date - DateTime.Today).Days;
        if (days < 0) return $"{Math.Abs(days)} días de retraso";
        if (days == 0) return "Hoy es el día";
        if (days == 1) return "1 día restante";
        if (days < 60) return $"{days} días restantes";
        var months = (int)Math.Ceiling(days / 30.0);
        return $"~{months} meses en el calendario";
    }

    private static string BuildGoalProjection(SavingsGoal g, decimal remaining)
    {
        if (remaining <= 0) return "Meta cumplida — celebra el hito";

        if (g.TargetDate.HasValue)
        {
            var months = Math.Max(1, (g.TargetDate.Value.Year - DateTime.Today.Year) * 12
                + g.TargetDate.Value.Month - DateTime.Today.Month);
            var pace = remaining / months;
            return $"Necesitas {ClpFormatter.FormatCompact(pace)}/mes para la fecha";
        }

        var fallbackMonths = (int)Math.Ceiling(remaining / DefaultMonthlyPace);
        return $"~{fallbackMonths} meses a {ClpFormatter.FormatCompact(DefaultMonthlyPace)}/mes";
    }

    private async Task SelectGoalAsync(GoalCardItem goal)
    {
        SelectedGoal = goal;
        if (QuickContributeGoal?.Id != goal.Id)
            QuickContributeGoal = goal;
        await LoadDetailAsync(goal.Id);
    }

    private async Task LoadDetailAsync(Guid goalId)
    {
        var entity = await _goals.GetByIdAsync(goalId);
        if (entity == null)
        {
            ClearDetail();
            return;
        }

        var remaining = Math.Max(0, entity.TargetAmount - entity.AccumulatedAmount);
        var pct = entity.TargetAmount > 0 ? (double)(entity.AccumulatedAmount / entity.TargetAmount * 100) : 0;
        var (statusLabel, _) = BuildStatus(entity, pct, remaining <= 0);

        DetailName = entity.Name;
        DetailStatus = statusLabel;
        DetailProgress = Math.Min(1, pct / 100);
        DetailProgressText = $"{pct:0.#}% · {ClpFormatter.Format(entity.AccumulatedAmount)} de {ClpFormatter.Format(entity.TargetAmount)}";
        DetailPaceLabel = BuildGoalProjection(entity, remaining);
        DetailExpectedVsActual = BuildExpectedVsActual(entity, pct);
        DetailCompletionEstimate = BuildCompletionEstimate(entity, remaining);
        DetailCategory = entity.Category?.Name ?? "Sin categoría vinculada";

        RecentContributions.Clear();
        var contributions = await _goals.GetRecentContributionsAsync(goalId);
        foreach (var c in contributions)
        {
            RecentContributions.Add(new GoalContributionListItem
            {
                Date = c.Date.ToString("dd MMM yyyy"),
                Amount = ClpFormatter.Format(c.Amount),
                Note = string.IsNullOrWhiteSpace(c.Note) ? (c.IsAutomatic ? "Aporte automático" : "Aporte manual") : c.Note!,
                TypeLabel = c.IsAutomatic ? "Auto" : "Manual",
                AmountBrush = BrushHelper.FromHex(entity.ColorHex)
            });
        }
    }

    private static string BuildExpectedVsActual(SavingsGoal g, double actualPct)
    {
        if (g.TargetDate == null || g.TargetAmount <= 0)
            return $"Avance real {actualPct:0.#}% · sin fecha para comparar ritmo";

        var totalDays = (g.TargetDate.Value.Date - DateTime.Today).Days;
        if (totalDays <= 0)
            return $"Avance real {actualPct:0.#}% · fecha objetivo vencida";

        var horizonDays = Math.Max(totalDays, 30);
        var expectedPct = Math.Min(100, Math.Max(0, 100 - totalDays / (double)horizonDays * 100));
        var delta = actualPct - expectedPct;
        var trend = delta >= 5 ? "por encima del ritmo esperado" : delta <= -5 ? "por debajo del ritmo esperado" : "alineada al ritmo esperado";
        return $"Real {actualPct:0.#}% vs esperado ~{expectedPct:0.#}% · {trend}";
    }

    private static string BuildCompletionEstimate(SavingsGoal g, decimal remaining)
    {
        if (remaining <= 0)
            return "Meta cumplida";

        if (g.TargetDate.HasValue)
            return $"Fecha objetivo {g.TargetDate.Value:dd MMM yyyy}";

        var months = (int)Math.Ceiling(remaining / DefaultMonthlyPace);
        return $"Estimación ~{months} meses a {ClpFormatter.FormatCompact(DefaultMonthlyPace)}/mes";
    }

    private void ClearDetail()
    {
        DetailName = string.Empty;
        DetailStatus = string.Empty;
        DetailProgressText = string.Empty;
        DetailPaceLabel = string.Empty;
        DetailExpectedVsActual = string.Empty;
        DetailCompletionEstimate = string.Empty;
        DetailCategory = string.Empty;
        DetailProgress = 0;
        RecentContributions.Clear();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task SelectGoal(GoalCardItem? goal)
    {
        if (goal == null) return;
        IsEditorOpen = false;
        await SelectGoalAsync(goal);
    }

    [RelayCommand]
    private void OpenContribute(GoalCardItem? goal)
    {
        if (goal == null) return;
        QuickContributeGoal = goal;
        _ = SelectGoalAsync(goal);
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task Contribute()
    {
        var target = QuickContributeGoal ?? SelectedGoal;
        if (target == null)
        {
            StatusMessage = "Selecciona una meta para aportar.";
            return;
        }

        if (!decimal.TryParse(ContributeAmount, out var amount) || amount <= 0)
            amount = DefaultMonthlyPace;

        await _goals.ContributeAsync(target.Id, amount);
        StatusMessage = $"Aporte de {ClpFormatter.Format(amount)} a «{target.Name}» registrado.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenEdit(GoalCardItem? goal)
    {
        if (goal == null) return;
        _editingGoalId = goal.Id;
        EditorTitle = "Editar meta";
        IsEditorOpen = true;

        var entity = await _goals.GetByIdAsync(goal.Id);
        if (entity == null) return;

        EditName = entity.Name;
        EditTargetInput = ((long)entity.TargetAmount).ToString();
        EditDateInput = entity.TargetDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        EditCategory = CategoryOptions.FirstOrDefault(c => c.Id == entity.CategoryId)
            ?? CategoryOptions.FirstOrDefault();
        EditColor = ColorPresets.FirstOrDefault(c => c.Hex.Equals(entity.ColorHex, StringComparison.OrdinalIgnoreCase))
            ?? ColorPresets.FirstOrDefault();
        EditIcon = IconOptions.FirstOrDefault(i => i.Key.Equals(entity.IconKey, StringComparison.OrdinalIgnoreCase))
            ?? IconOptions.FirstOrDefault();
        EditAutoFromBudget = entity.AutoContributeFromBudget;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void OpenNewGoal()
    {
        _editingGoalId = null;
        EditorTitle = "Nueva meta";
        IsEditorOpen = true;
        EditName = "Mi nueva meta";
        EditTargetInput = "1000000";
        EditDateInput = DateTime.Today.AddMonths(12).ToString("yyyy-MM-dd");
        EditCategory = CategoryOptions.FirstOrDefault();
        EditColor = ColorPresets[0];
        EditIcon = IconOptions[0];
        EditAutoFromBudget = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SelectColor(GoalColorPreset? preset)
    {
        if (preset != null)
            EditColor = preset;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditorOpen = false;
        _editingGoalId = null;
    }

    [RelayCommand]
    private async Task SaveGoal()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            StatusMessage = "El nombre es obligatorio.";
            return;
        }

        if (!decimal.TryParse(EditTargetInput, out var target) || target <= 0)
        {
            StatusMessage = "Indica un monto objetivo válido.";
            return;
        }

        DateTime? targetDate = null;
        if (!string.IsNullOrWhiteSpace(EditDateInput) && DateTime.TryParse(EditDateInput, out var parsed))
            targetDate = parsed.Date;

        var update = new SavingsGoalUpdate(
            EditName.Trim(),
            target,
            targetDate,
            EditCategory?.Id,
            EditColor?.Hex ?? "#35E0A1",
            EditIcon?.Key ?? "target",
            EditAutoFromBudget);

        if (_editingGoalId.HasValue)
            await _goals.UpdateAsync(_editingGoalId.Value, update);
        else
            await _goals.CreateAsync(update);

        IsEditorOpen = false;
        _editingGoalId = null;
        StatusMessage = "Meta guardada.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Archive(GoalCardItem? goal)
    {
        if (goal == null) return;
        await _goals.ArchiveAsync(goal.Id);
        if (SelectedGoal?.Id == goal.Id)
        {
            SelectedGoal = null;
            QuickContributeGoal = null;
            ClearDetail();
        }
        if (_editingGoalId == goal.Id)
        {
            IsEditorOpen = false;
            _editingGoalId = null;
        }
        StatusMessage = $"«{goal.Name}» archivada.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        var list = await _goals.GetActiveGoalsAsync();
        var path = await _excel.ExportGoalsAsync(list, ExportPaths.DefaultFolder);
        StatusMessage = $"Metas exportadas: {path}";
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        var list = await _goals.GetActiveGoalsAsync();
        var path = await _pdf.ExportGoalsAsync(list, ExportPaths.DefaultFolder);
        StatusMessage = $"Metas PDF: {path}";
    }
}
