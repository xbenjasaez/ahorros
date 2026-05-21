using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Dtos;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Transactions;

public partial class TransactionsViewModel : ViewModelBase, ILoadable
{
    private readonly ITransactionService _transactions;
    private readonly ICurrentUserContext _user;
    private readonly IFilterPresetService _presets;
    private readonly FilterCriteria _criteria = new();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _filterSummary = "0 movimientos · $0";
    [ObservableProperty] private TransactionRowItem? _selectedTransaction;
    [ObservableProperty] private bool _showAdvancedFilters = true;
    [ObservableProperty] private DateTime? _filterDateFrom;
    [ObservableProperty] private DateTime? _filterDateTo;
    [ObservableProperty] private decimal? _filterMinAmount;
    [ObservableProperty] private decimal? _filterMaxAmount;

    public ObservableCollection<TransactionRowItem> Items { get; } = [];
    public ObservableCollection<FilterChipItem> QuickFilters { get; } = [];
    public ObservableCollection<string> ActiveFilterChips { get; } = [];

    public TransactionsViewModel(ITransactionService transactions, ICurrentUserContext user, IFilterPresetService presets)
    {
        Title = "Movimientos";
        _transactions = transactions;
        _user = user;
        _presets = presets;
        InitQuickFilters();
    }

    private void InitQuickFilters()
    {
        QuickFilters.Add(new FilterChipItem { Key = "month", Label = "Este mes" });
        QuickFilters.Add(new FilterChipItem { Key = "expense", Label = "Gastos" });
        QuickFilters.Add(new FilterChipItem { Key = "pending", Label = "Pendientes" });
        QuickFilters.Add(new FilterChipItem { Key = "recurring", Label = "Recurrentes" });
        QuickFilters.Add(new FilterChipItem { Key = "goal", Label = "Con meta" });
    }

    public async Task LoadAsync() => await ApplyFiltersAsync();

    partial void OnSearchTextChanged(string value)
    {
        _criteria.SearchText = value;
        _ = ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ToggleQuickFilter(FilterChipItem chip)
    {
        chip.IsActive = !chip.IsActive;
        switch (chip.Key)
        {
            case "month":
                if (chip.IsActive) { _criteria.DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); _criteria.DateTo = DateTime.Today; }
                else { _criteria.DateFrom = null; _criteria.DateTo = null; }
                break;
            case "expense":
                _criteria.Type = chip.IsActive ? TransactionType.Expense : null;
                break;
            case "pending":
                _criteria.Status = chip.IsActive ? TransactionStatus.Pending : null;
                break;
            case "recurring":
                _criteria.IsRecurring = chip.IsActive ? true : null;
                break;
            case "goal":
                _criteria.HasGoal = chip.IsActive ? true : null;
                break;
        }
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ApplyAdvancedFilters()
    {
        _criteria.DateFrom = FilterDateFrom;
        _criteria.DateTo = FilterDateTo;
        _criteria.MinAmount = FilterMinAmount;
        _criteria.MaxAmount = FilterMaxAmount;
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        _criteria.DateFrom = _criteria.DateTo = null;
        _criteria.Type = _criteria.Status = null;
        _criteria.IsRecurring = _criteria.HasGoal = null;
        _criteria.MinAmount = _criteria.MaxAmount = null;
        _criteria.SearchText = SearchText = string.Empty;
        foreach (var c in QuickFilters) c.IsActive = false;
        ActiveFilterChips.Clear();
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task SaveFilterPreset()
    {
        await _presets.SavePresetAsync($"Vista {DateTime.Now:HHmm}", _criteria);
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (SelectedTransaction == null) return;
        await _transactions.DeleteAsync(SelectedTransaction.Id);
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task DuplicateSelected()
    {
        if (SelectedTransaction == null) return;
        await _transactions.DuplicateAsync(SelectedTransaction.Id);
        await ApplyFiltersAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        _criteria.BudgetPeriodId = _user.ActivePeriodId;
        var list = await _transactions.GetFilteredAsync(_criteria);
        Items.Clear();
        decimal sum = 0;
        foreach (var t in list)
        {
            sum += t.Amount;
            Items.Add(new TransactionRowItem
            {
                Id = t.Id,
                Date = t.Date.ToString("dd/MM/yyyy"),
                Type = t.Type.ToString(),
                Description = t.Description,
                Category = t.Category?.Name ?? "—",
                Subcategory = t.Subcategory?.Name ?? "—",
                PaymentMethod = t.PaymentMethod?.Name ?? "—",
                Amount = ClpFormatter.Format(t.Amount),
                Status = t.Status.ToString(),
                Tags = string.Join(", ", new[] { t.Tag, t.IsRecurring ? "recurrente" : null }.Where(x => x != null)!)
            });
        }
        FilterSummary = $"{list.Count} movimientos · {ClpFormatter.Format(sum)}";
        UpdateActiveChips();
    }

    private void UpdateActiveChips()
    {
        ActiveFilterChips.Clear();
        if (_criteria.Type.HasValue) ActiveFilterChips.Add($"Tipo: {_criteria.Type}");
        if (_criteria.Status.HasValue) ActiveFilterChips.Add($"Estado: {_criteria.Status}");
        if (_criteria.IsRecurring == true) ActiveFilterChips.Add("Recurrente");
        if (_criteria.HasGoal == true) ActiveFilterChips.Add("Con meta");
        if (!string.IsNullOrWhiteSpace(_criteria.SearchText)) ActiveFilterChips.Add($"Texto: {_criteria.SearchText}");
    }
}
