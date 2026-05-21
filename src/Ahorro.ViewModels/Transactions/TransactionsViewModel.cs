using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Dtos;
using Ahorro.Models.Entities;
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
    private readonly IBudgetPeriodService _periods;
    private readonly ISettingsService _settings;
    private readonly ISavingsGoalService _goals;
    private readonly IExcelExportService _export;
    private readonly FilterCriteria _criteria = new();
    private List<BudgetCategory> _allCategories = [];
    private List<MoneyTransaction> _lastResult = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _filterSummary = "0 movimientos";
    [ObservableProperty] private string _summaryIncome = "$0";
    [ObservableProperty] private string _summaryExpense = "$0";
    [ObservableProperty] private string _summaryNet = "$0";
    [ObservableProperty] private TransactionRowItem? _selectedTransaction;
    [ObservableProperty] private bool _showAdvancedFilters;
    public GridLength AdvancedColumnWidth => ShowAdvancedFilters ? new GridLength(300) : new GridLength(0);
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isAddingNew;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private PeriodOption? _selectedPeriodOption;
    [ObservableProperty] private DateTime? _filterDateFrom;
    [ObservableProperty] private DateTime? _filterDateTo;
    [ObservableProperty] private string? _filterMinAmountText;
    [ObservableProperty] private string? _filterMaxAmountText;
    [ObservableProperty] private LookupItem? _selectedCategoryFilter;
    [ObservableProperty] private LookupItem? _selectedSubcategoryFilter;
    [ObservableProperty] private LookupItem? _selectedPaymentMethodFilter;
    [ObservableProperty] private LookupItem? _selectedGoalFilter;
    [ObservableProperty] private EnumLookupItem<TransactionType>? _selectedTypeFilter;
    [ObservableProperty] private EnumLookupItem<TransactionStatus>? _selectedStatusFilter;
    [ObservableProperty] private bool? _filterRecurringOnly;
    [ObservableProperty] private DateTime _editDate = DateTime.Today;
    [ObservableProperty] private string _editDescription = string.Empty;
    [ObservableProperty] private string _editAmountText = "0";
    [ObservableProperty] private string? _editNote;
    [ObservableProperty] private string? _editTag;
    [ObservableProperty] private bool _editIsRecurring;
    [ObservableProperty] private CategoryPickerItem? _editCategory;
    [ObservableProperty] private CategoryPickerItem? _editSubcategory;
    [ObservableProperty] private LookupItem? _editPaymentMethod;
    [ObservableProperty] private EnumLookupItem<TransactionType>? _editType;
    [ObservableProperty] private EnumLookupItem<TransactionStatus>? _editStatus;
    [ObservableProperty] private LookupItem? _editGoal;

    public ObservableCollection<TransactionRowItem> Items { get; } = [];
    public ObservableCollection<FilterChipItem> QuickFilters { get; } = [];
    public ObservableCollection<ActiveFilterChipItem> ActiveFilterChips { get; } = [];
    public ObservableCollection<PeriodOption> PeriodOptions { get; } = [];
    public ObservableCollection<LookupItem> CategoryFilterOptions { get; } = [];
    public ObservableCollection<LookupItem> SubcategoryFilterOptions { get; } = [];
    public ObservableCollection<LookupItem> PaymentMethodFilterOptions { get; } = [];
    public ObservableCollection<LookupItem> GoalFilterOptions { get; } = [];
    public ObservableCollection<EnumLookupItem<TransactionType>> TypeFilterOptions { get; } = [];
    public ObservableCollection<EnumLookupItem<TransactionStatus>> StatusFilterOptions { get; } = [];
    public ObservableCollection<CategoryPickerItem> EditCategories { get; } = [];
    public ObservableCollection<CategoryPickerItem> EditSubcategories { get; } = [];
    public ObservableCollection<LookupItem> EditPaymentMethods { get; } = [];
    public ObservableCollection<LookupItem> EditGoals { get; } = [];
    public ObservableCollection<EnumLookupItem<TransactionType>> EditTypeOptions { get; } = [];
    public ObservableCollection<EnumLookupItem<TransactionStatus>> EditStatusOptions { get; } = [];

    public TransactionsViewModel(
        ITransactionService transactions,
        ICurrentUserContext user,
        IFilterPresetService presets,
        IBudgetPeriodService periods,
        ISettingsService settings,
        ISavingsGoalService goals,
        IExcelExportService export)
    {
        Title = "Movimientos";
        _transactions = transactions;
        _user = user;
        _presets = presets;
        _periods = periods;
        _settings = settings;
        _goals = goals;
        _export = export;
        InitQuickFilters();
        InitEnumFilters();
    }

    private void InitQuickFilters()
    {
        QuickFilters.Add(new FilterChipItem { Key = "month", Label = "Este mes" });
        QuickFilters.Add(new FilterChipItem { Key = "expense", Label = "Gastos" });
        QuickFilters.Add(new FilterChipItem { Key = "income", Label = "Ingresos" });
        QuickFilters.Add(new FilterChipItem { Key = "pending", Label = "Pendientes" });
        QuickFilters.Add(new FilterChipItem { Key = "recurring", Label = "Recurrentes" });
        QuickFilters.Add(new FilterChipItem { Key = "goal", Label = "Con meta" });
    }

    private void InitEnumFilters()
    {
        TypeFilterOptions.Add(new EnumLookupItem<TransactionType> { Label = "Todos los tipos" });
        foreach (TransactionType t in Enum.GetValues<TransactionType>())
            TypeFilterOptions.Add(new EnumLookupItem<TransactionType> { Value = t, Label = TransactionLabels.Type(t) });
        SelectedTypeFilter = TypeFilterOptions[0];

        StatusFilterOptions.Add(new EnumLookupItem<TransactionStatus> { Label = "Todos los estados" });
        foreach (TransactionStatus s in Enum.GetValues<TransactionStatus>())
            StatusFilterOptions.Add(new EnumLookupItem<TransactionStatus> { Value = s, Label = TransactionLabels.Status(s) });
        SelectedStatusFilter = StatusFilterOptions[0];

        foreach (TransactionType t in Enum.GetValues<TransactionType>())
            EditTypeOptions.Add(new EnumLookupItem<TransactionType> { Value = t, Label = TransactionLabels.Type(t) });
        foreach (TransactionStatus s in Enum.GetValues<TransactionStatus>())
            EditStatusOptions.Add(new EnumLookupItem<TransactionStatus> { Value = s, Label = TransactionLabels.Status(s) });
        EditType = EditTypeOptions.FirstOrDefault(o => o.Value == TransactionType.Expense);
        EditStatus = EditStatusOptions.FirstOrDefault(o => o.Value == TransactionStatus.Paid);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await _periods.EnsureActivePeriodAsync();
            await LoadLookupsAsync();
            var allPeriods = await _periods.GetPeriodsAsync();
            PeriodOptions.Clear();
            foreach (var p in allPeriods)
                PeriodOptions.Add(new PeriodOption { Id = p.Id, Label = $"{p.StartDate:dd MMM} – {p.EndDate:dd MMM yyyy}" });

            if (SelectedPeriodOption == null || !PeriodOptions.Any(o => o.Id == SelectedPeriodOption.Id))
                SelectedPeriodOption = PeriodOptions.FirstOrDefault(o => o.Id == _user.ActivePeriodId)
                    ?? PeriodOptions.FirstOrDefault();

            if (SelectedPeriodOption != null)
                _criteria.BudgetPeriodId = SelectedPeriodOption.Id;

            await ApplyFiltersAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        _allCategories = await _settings.GetCategoriesAsync();
        var methods = await _transactions.GetPaymentMethodsAsync();
        var goals = await _goals.GetActiveGoalsAsync();

        CategoryFilterOptions.Clear();
        CategoryFilterOptions.Add(new LookupItem { Name = "Todas las categorías" });
        EditCategories.Clear();
        foreach (var c in _allCategories)
        {
            CategoryFilterOptions.Add(new LookupItem { Id = c.Id, Name = c.Name });
            EditCategories.Add(new CategoryPickerItem { Id = c.Id, Name = c.Name });
        }
        SelectedCategoryFilter = CategoryFilterOptions[0];
        RefreshSubcategoryFilterOptions(null);

        PaymentMethodFilterOptions.Clear();
        PaymentMethodFilterOptions.Add(new LookupItem { Name = "Todos los métodos" });
        EditPaymentMethods.Clear();
        foreach (var m in methods)
        {
            PaymentMethodFilterOptions.Add(new LookupItem { Id = m.Id, Name = m.Name });
            EditPaymentMethods.Add(new LookupItem { Id = m.Id, Name = m.Name });
        }
        SelectedPaymentMethodFilter = PaymentMethodFilterOptions[0];
        EditPaymentMethod = EditPaymentMethods.FirstOrDefault();

        GoalFilterOptions.Clear();
        GoalFilterOptions.Add(new LookupItem { Name = "Todas las metas" });
        EditGoals.Clear();
        EditGoals.Add(new LookupItem { Name = "Sin meta" });
        foreach (var g in goals)
        {
            GoalFilterOptions.Add(new LookupItem { Id = g.Id, Name = g.Name });
            EditGoals.Add(new LookupItem { Id = g.Id, Name = g.Name });
        }
        SelectedGoalFilter = GoalFilterOptions[0];
    }

    partial void OnSelectedPeriodOptionChanged(PeriodOption? value)
    {
        if (value == null) return;
        _criteria.BudgetPeriodId = value.Id;
        _user.ActivePeriodId = value.Id;
        _ = ApplyFiltersAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _criteria.SearchText = value;
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedCategoryFilterChanged(LookupItem? value)
    {
        RefreshSubcategoryFilterOptions(value?.Id);
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedSubcategoryFilterChanged(LookupItem? value) => _ = ApplyFiltersAsync();
    partial void OnSelectedPaymentMethodFilterChanged(LookupItem? value) => _ = ApplyFiltersAsync();
    partial void OnSelectedGoalFilterChanged(LookupItem? value) => _ = ApplyFiltersAsync();
    partial void OnSelectedTypeFilterChanged(EnumLookupItem<TransactionType>? value) => _ = ApplyFiltersAsync();
    partial void OnSelectedStatusFilterChanged(EnumLookupItem<TransactionStatus>? value) => _ = ApplyFiltersAsync();
    partial void OnFilterRecurringOnlyChanged(bool? value) => _ = ApplyFiltersAsync();

    partial void OnSelectedTransactionChanged(TransactionRowItem? value)
    {
        if (value == null || IsAddingNew) return;
        LoadEditFromRow(value);
        IsEditing = false;
        IsAddingNew = false;
    }

    partial void OnEditCategoryChanged(CategoryPickerItem? value) => RefreshEditSubcategories(value?.Id);

    private void RefreshSubcategoryFilterOptions(Guid? categoryId)
    {
        SubcategoryFilterOptions.Clear();
        SubcategoryFilterOptions.Add(new LookupItem { Name = "Todas las subcategorías" });
        if (categoryId.HasValue)
        {
            var cat = _allCategories.FirstOrDefault(c => c.Id == categoryId);
            if (cat != null)
                foreach (var s in cat.Subcategories.OrderBy(x => x.SortOrder))
                    SubcategoryFilterOptions.Add(new LookupItem { Id = s.Id, Name = s.Name });
        }
        SelectedSubcategoryFilter = SubcategoryFilterOptions[0];
    }

    private void RefreshEditSubcategories(Guid? categoryId)
    {
        EditSubcategories.Clear();
        if (!categoryId.HasValue) return;
        var cat = _allCategories.FirstOrDefault(c => c.Id == categoryId);
        if (cat == null) return;
        foreach (var s in cat.Subcategories.OrderBy(x => x.SortOrder))
            EditSubcategories.Add(new CategoryPickerItem { Id = s.Id, Name = s.Name });
        EditSubcategory = EditSubcategories.FirstOrDefault();
    }

    private void SyncCriteriaFromUi()
    {
        _criteria.CategoryId = SelectedCategoryFilter?.Id;
        _criteria.SubcategoryId = SelectedSubcategoryFilter?.Id;
        _criteria.PaymentMethodId = SelectedPaymentMethodFilter?.Id;
        _criteria.SavingsGoalId = SelectedGoalFilter?.Id;
        _criteria.Type = SelectedTypeFilter?.Value;
        _criteria.Status = SelectedStatusFilter?.Value;
        _criteria.IsRecurring = FilterRecurringOnly;
        _criteria.DateFrom = FilterDateFrom;
        _criteria.DateTo = FilterDateTo;
        _criteria.MinAmount = decimal.TryParse(FilterMinAmountText, out var min) ? min : null;
        _criteria.MaxAmount = decimal.TryParse(FilterMaxAmountText, out var max) ? max : null;
        _criteria.SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
    }

    [RelayCommand]
    private void ToggleAdvancedFilters()
    {
        ShowAdvancedFilters = !ShowAdvancedFilters;
        OnPropertyChanged(nameof(AdvancedColumnWidth));
    }

    partial void OnShowAdvancedFiltersChanged(bool value) => OnPropertyChanged(nameof(AdvancedColumnWidth));

    [RelayCommand]
    private async Task ToggleQuickFilter(FilterChipItem chip)
    {
        chip.IsActive = !chip.IsActive;
        switch (chip.Key)
        {
            case "month":
                if (chip.IsActive)
                {
                    _criteria.DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    _criteria.DateTo = DateTime.Today;
                    FilterDateFrom = _criteria.DateFrom;
                    FilterDateTo = _criteria.DateTo;
                }
                else
                {
                    _criteria.DateFrom = _criteria.DateTo = null;
                    FilterDateFrom = FilterDateTo = null;
                }
                break;
            case "expense":
                _criteria.Type = chip.IsActive ? TransactionType.Expense : null;
                SelectedTypeFilter = chip.IsActive
                    ? TypeFilterOptions.FirstOrDefault(o => o.Value == TransactionType.Expense) ?? TypeFilterOptions[0]
                    : TypeFilterOptions[0];
                break;
            case "income":
                _criteria.Type = chip.IsActive ? TransactionType.Income : null;
                SelectedTypeFilter = chip.IsActive
                    ? TypeFilterOptions.FirstOrDefault(o => o.Value == TransactionType.Income) ?? TypeFilterOptions[0]
                    : TypeFilterOptions[0];
                break;
            case "pending":
                _criteria.Status = chip.IsActive ? TransactionStatus.Pending : null;
                SelectedStatusFilter = chip.IsActive
                    ? StatusFilterOptions.FirstOrDefault(o => o.Value == TransactionStatus.Pending) ?? StatusFilterOptions[0]
                    : StatusFilterOptions[0];
                break;
            case "recurring":
                _criteria.IsRecurring = chip.IsActive ? true : null;
                FilterRecurringOnly = chip.IsActive ? true : null;
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
        SyncCriteriaFromUi();
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        _criteria.DateFrom = _criteria.DateTo = null;
        _criteria.Type = null;
        _criteria.Status = null;
        _criteria.IsRecurring = null;
        _criteria.HasGoal = null;
        _criteria.SavingsGoalId = null;
        _criteria.MinAmount = _criteria.MaxAmount = null;
        _criteria.CategoryId = _criteria.SubcategoryId = _criteria.PaymentMethodId = null;
        _criteria.SearchText = null;
        SearchText = string.Empty;
        FilterDateFrom = FilterDateTo = null;
        FilterMinAmountText = FilterMaxAmountText = null;
        FilterRecurringOnly = null;
        foreach (var c in QuickFilters) c.IsActive = false;
        SelectedCategoryFilter = CategoryFilterOptions.FirstOrDefault();
        SelectedPaymentMethodFilter = PaymentMethodFilterOptions.FirstOrDefault();
        SelectedGoalFilter = GoalFilterOptions.FirstOrDefault();
        SelectedTypeFilter = TypeFilterOptions.FirstOrDefault();
        SelectedStatusFilter = StatusFilterOptions.FirstOrDefault();
        ActiveFilterChips.Clear();
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task RemoveFilterChip(ActiveFilterChipItem chip)
    {
        switch (chip.Key)
        {
            case "type":
                _criteria.Type = null;
                SelectedTypeFilter = TypeFilterOptions[0];
                QuickFilters.FirstOrDefault(f => f.Key is "expense" or "income")!.IsActive = false;
                break;
            case "status":
                _criteria.Status = null;
                SelectedStatusFilter = StatusFilterOptions[0];
                QuickFilters.FirstOrDefault(f => f.Key == "pending")!.IsActive = false;
                break;
            case "recurring":
                _criteria.IsRecurring = null;
                FilterRecurringOnly = null;
                QuickFilters.FirstOrDefault(f => f.Key == "recurring")!.IsActive = false;
                break;
            case "goal":
                _criteria.HasGoal = null;
                _criteria.SavingsGoalId = null;
                SelectedGoalFilter = GoalFilterOptions[0];
                QuickFilters.FirstOrDefault(f => f.Key == "goal")!.IsActive = false;
                break;
            case "category":
                _criteria.CategoryId = null;
                SelectedCategoryFilter = CategoryFilterOptions[0];
                break;
            case "subcategory":
                _criteria.SubcategoryId = null;
                SelectedSubcategoryFilter = SubcategoryFilterOptions[0];
                break;
            case "payment":
                _criteria.PaymentMethodId = null;
                SelectedPaymentMethodFilter = PaymentMethodFilterOptions[0];
                break;
            case "sgoal":
                _criteria.SavingsGoalId = null;
                SelectedGoalFilter = GoalFilterOptions[0];
                break;
            case "dates":
                _criteria.DateFrom = _criteria.DateTo = null;
                FilterDateFrom = FilterDateTo = null;
                QuickFilters.FirstOrDefault(f => f.Key == "month")!.IsActive = false;
                break;
            case "amount":
                _criteria.MinAmount = _criteria.MaxAmount = null;
                FilterMinAmountText = FilterMaxAmountText = null;
                break;
            case "text":
                _criteria.SearchText = null;
                SearchText = string.Empty;
                break;
        }
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task SaveFilterPreset()
    {
        SyncCriteriaFromUi();
        await _presets.SavePresetAsync($"Vista {DateTime.Now:HHmm}", _criteria);
        StatusMessage = "Vista de filtros guardada.";
    }

    [RelayCommand]
    private async Task Export()
    {
        SyncCriteriaFromUi();
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ahorro");
        var path = await _export.ExportTransactionsAsync(_lastResult, folder);
        StatusMessage = $"Exportado: {path}";
    }

    [RelayCommand]
    private void StartAdd()
    {
        IsAddingNew = true;
        IsEditing = true;
        SelectedTransaction = null;
        EditDate = DateTime.Today;
        EditDescription = string.Empty;
        EditAmountText = "0";
        EditNote = EditTag = null;
        EditIsRecurring = false;
        EditCategory = EditCategories.FirstOrDefault();
        EditType = EditTypeOptions.FirstOrDefault(o => o.Value == TransactionType.Expense);
        EditStatus = EditStatusOptions.FirstOrDefault(o => o.Value == TransactionStatus.Paid);
        EditGoal = EditGoals.FirstOrDefault();
        RefreshEditSubcategories(EditCategory?.Id);
    }

    [RelayCommand]
    private void StartEdit(TransactionRowItem? row)
    {
        if (row == null) return;
        SelectedTransaction = row;
        LoadEditFromRow(row);
        IsEditing = true;
        IsAddingNew = false;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        IsAddingNew = false;
        if (SelectedTransaction != null)
            LoadEditFromRow(SelectedTransaction);
    }

    [RelayCommand]
    private async Task SaveEdit()
    {
        if (EditCategory == null || EditPaymentMethod?.Id == null ||
            EditType?.Value is not TransactionType txType ||
            EditStatus?.Value is not TransactionStatus txStatus)
        {
            StatusMessage = "Completa categoría, método y tipo.";
            return;
        }
        if (!decimal.TryParse(EditAmountText, out var amount) || amount <= 0)
        {
            StatusMessage = "Monto inválido.";
            return;
        }
        var periodId = SelectedPeriodOption?.Id ?? _user.ActivePeriodId ?? Guid.Empty;
        if (IsAddingNew)
        {
            await _transactions.AddAsync(new MoneyTransaction
            {
                BudgetPeriodId = periodId,
                Date = EditDate,
                Type = txType,
                Description = EditDescription.Trim(),
                CategoryId = EditCategory.Id,
                SubcategoryId = EditSubcategory?.Id,
                Amount = amount,
                PaymentMethodId = EditPaymentMethod.Id.Value,
                Status = txStatus,
                Note = EditNote,
                Tag = EditTag,
                IsRecurring = EditIsRecurring,
                SavingsGoalId = EditGoal?.Id
            });
            StatusMessage = "Movimiento creado.";
        }
        else if (SelectedTransaction != null)
        {
            var entity = await _transactions.GetByIdAsync(SelectedTransaction.Id);
            if (entity == null) return;
            entity.Date = EditDate;
            entity.Type = txType;
            entity.Description = EditDescription.Trim();
            entity.CategoryId = EditCategory.Id;
            entity.SubcategoryId = EditSubcategory?.Id;
            entity.Amount = amount;
            entity.PaymentMethodId = EditPaymentMethod.Id.Value;
            entity.Status = txStatus;
            entity.Note = EditNote;
            entity.Tag = EditTag;
            entity.IsRecurring = EditIsRecurring;
            entity.SavingsGoalId = EditGoal?.Id;
            await _transactions.UpdateAsync(entity);
            StatusMessage = "Movimiento actualizado.";
        }

        IsEditing = false;
        IsAddingNew = false;
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task DeleteRow(TransactionRowItem? row)
    {
        if (row == null) return;
        await _transactions.DeleteAsync(row.Id);
        if (SelectedTransaction?.Id == row.Id)
            SelectedTransaction = null;
        StatusMessage = "Movimiento eliminado.";
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task DuplicateRow(TransactionRowItem? row)
    {
        if (row == null) return;
        await _transactions.DuplicateAsync(row.Id);
        StatusMessage = "Movimiento duplicado.";
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task MarkPaidRow(TransactionRowItem? row)
    {
        if (row == null) return;
        await _transactions.MarkPaidAsync(row.Id);
        StatusMessage = "Marcado como pagado.";
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ToggleRecurringRow(TransactionRowItem? row)
    {
        if (row == null) return;
        await _transactions.SetRecurringAsync(row.Id, !row.IsRecurring);
        StatusMessage = row.IsRecurring ? "Ya no es recurrente." : "Marcado como recurrente.";
        await ApplyFiltersAsync();
    }

    private void LoadEditFromRow(TransactionRowItem row)
    {
        EditDate = row.DateValue;
        EditDescription = row.Description;
        EditAmountText = row.AmountValue.ToString("0");
        EditNote = row.Note;
        EditTag = row.Tag;
        EditIsRecurring = row.IsRecurring;
        EditCategory = EditCategories.FirstOrDefault(c => c.Id == row.CategoryId);
        RefreshEditSubcategories(row.CategoryId);
        EditSubcategory = row.SubcategoryId.HasValue
            ? EditSubcategories.FirstOrDefault(s => s.Id == row.SubcategoryId)
            : null;
        EditPaymentMethod = EditPaymentMethods.FirstOrDefault(m => m.Id == row.PaymentMethodId);
        EditType = EditTypeOptions.FirstOrDefault(t => t.Value == row.TypeValue);
        EditStatus = EditStatusOptions.FirstOrDefault(s => s.Value == row.StatusValue);
        EditGoal = row.SavingsGoalId.HasValue
            ? EditGoals.FirstOrDefault(g => g.Id == row.SavingsGoalId)
            : EditGoals.FirstOrDefault(g => g.Id == null);
    }

    private async Task ApplyFiltersAsync()
    {
        SyncCriteriaFromUi();
        if (SelectedPeriodOption != null)
            _criteria.BudgetPeriodId = SelectedPeriodOption.Id;

        var list = await _transactions.GetFilteredAsync(_criteria);
        _lastResult = list;
        Items.Clear();
        decimal income = 0, expense = 0;
        foreach (var t in list)
        {
            if (t.Type == TransactionType.Income) income += t.Amount;
            else if (t.Type == TransactionType.Expense) expense += t.Amount;

            var tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(t.Tag)) tags.Add(t.Tag);
            if (t.IsRecurring) tags.Add("recurrente");
            if (t.SavingsGoal != null) tags.Add(t.SavingsGoal.Name);

            Items.Add(new TransactionRowItem
            {
                Id = t.Id,
                BudgetPeriodId = t.BudgetPeriodId,
                DateValue = t.Date,
                Date = t.Date.ToString("dd/MM/yyyy"),
                TypeValue = t.Type,
                Type = TransactionLabels.Type(t.Type),
                TypeColor = TransactionLabels.TypeColor(t.Type),
                Description = t.Description,
                CategoryId = t.CategoryId,
                Category = t.Category?.Name ?? "—",
                SubcategoryId = t.SubcategoryId,
                Subcategory = t.Subcategory?.Name ?? "—",
                PaymentMethodId = t.PaymentMethodId,
                PaymentMethod = t.PaymentMethod?.Name ?? "—",
                AmountValue = t.Amount,
                Amount = ClpFormatter.Format(t.Amount),
                AmountColor = t.Type == TransactionType.Income ? "#35E0A1" : "#E8EDF5",
                StatusValue = t.Status,
                Status = TransactionLabels.Status(t.Status),
                StatusColor = TransactionLabels.StatusColor(t.Status),
                Tags = tags.Count > 0 ? string.Join(" · ", tags) : "—",
                IsRecurring = t.IsRecurring,
                SavingsGoalId = t.SavingsGoalId,
                GoalName = t.SavingsGoal?.Name,
                Note = t.Note,
                Tag = t.Tag
            });
        }

        FilterSummary = $"{list.Count} movimientos";
        SummaryIncome = ClpFormatter.Format(income);
        SummaryExpense = ClpFormatter.Format(expense);
        SummaryNet = ClpFormatter.Format(income - expense);
        UpdateActiveChips();

        if (SelectedTransaction != null)
        {
            var refreshed = Items.FirstOrDefault(i => i.Id == SelectedTransaction.Id);
            if (refreshed != null) SelectedTransaction = refreshed;
        }
    }

    private void UpdateActiveChips()
    {
        ActiveFilterChips.Clear();
        if (_criteria.Type.HasValue)
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "type", Label = $"Tipo: {TransactionLabels.Type(_criteria.Type.Value)}" });
        if (_criteria.Status.HasValue)
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "status", Label = $"Estado: {TransactionLabels.Status(_criteria.Status.Value)}" });
        if (_criteria.IsRecurring == true)
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "recurring", Label = "Recurrente" });
        if (_criteria.HasGoal == true)
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "goal", Label = "Con meta" });
        if (_criteria.SavingsGoalId.HasValue)
        {
            var name = GoalFilterOptions.FirstOrDefault(g => g.Id == _criteria.SavingsGoalId)?.Name ?? "Meta";
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "sgoal", Label = $"Meta: {name}" });
        }
        if (_criteria.CategoryId.HasValue)
        {
            var name = CategoryFilterOptions.FirstOrDefault(c => c.Id == _criteria.CategoryId)?.Name ?? "Categoría";
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "category", Label = $"Cat.: {name}" });
        }
        if (_criteria.SubcategoryId.HasValue)
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "subcategory", Label = "Subcategoría" });
        if (_criteria.PaymentMethodId.HasValue)
        {
            var name = PaymentMethodFilterOptions.FirstOrDefault(p => p.Id == _criteria.PaymentMethodId)?.Name ?? "Método";
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "payment", Label = $"Pago: {name}" });
        }
        if (_criteria.DateFrom.HasValue || _criteria.DateTo.HasValue)
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "dates", Label = $"Fechas: {_criteria.DateFrom:dd/MM} – {_criteria.DateTo:dd/MM}" });
        if (_criteria.MinAmount.HasValue || _criteria.MaxAmount.HasValue)
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "amount", Label = $"Monto: {_criteria.MinAmount} – {_criteria.MaxAmount}" });
        if (!string.IsNullOrWhiteSpace(_criteria.SearchText))
            ActiveFilterChips.Add(new ActiveFilterChipItem { Key = "text", Label = $"Texto: {_criteria.SearchText}" });
    }
}
