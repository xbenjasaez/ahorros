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
using Microsoft.Win32;

namespace Ahorro.ViewModels.Settings;

public partial class SettingsViewModel : ViewModelBase, ILoadable
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly ICurrentUserContext _user;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private int _cutoffDay = 25;
    [ObservableProperty] private EnumLookupItem<PeriodFrequency>? _selectedFrequency;
    [ObservableProperty] private int _attentionThreshold = 80;
    [ObservableProperty] private int _limitThreshold = 100;
    [ObservableProperty] private bool _alertsEnabled = true;
    [ObservableProperty] private string _currencyCode = "CLP";
    [ObservableProperty] private CurrencyOption? _selectedCurrency;
    [ObservableProperty] private ThemeVariantOption? _selectedTheme;
    [ObservableProperty] private AccentColorOption? _selectedAccent;
    [ObservableProperty] private string _goalMonthlyPaceInput = "50000";
    [ObservableProperty] private bool _goalShowProjections = true;
    [ObservableProperty] private bool _goalAutoCelebrate = true;
    [ObservableProperty] private bool _goalSuggestContributions = true;
    [ObservableProperty] private string _exportFolder = string.Empty;
    [ObservableProperty] private string _exportFilePrefix = "Ahorro";
    [ObservableProperty] private bool _exportIncludeNotes = true;
    [ObservableProperty] private bool _exportPdfCharts = true;
    [ObservableProperty] private bool _exportExcelAutoOpen;
    [ObservableProperty] private bool _multiUserEnabled;
    [ObservableProperty] private string _multiUserDescription = "Gestiona perfiles locales. Al cambiar de usuario se recargan presupuesto, metas y movimientos.";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatusMessage;
    [ObservableProperty] private bool _showInactiveCategories;
    [ObservableProperty] private SettingsCategoryItem? _selectedCategory;
    [ObservableProperty] private SettingsSubcategoryItem? _selectedSubcategory;
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _newSubcategoryName = string.Empty;
    [ObservableProperty] private BudgetGroupOption? _selectedNewGroup;
    [ObservableProperty] private BudgetGroupOption? _selectedEditGroup;
    [ObservableProperty] private GoalColorPreset? _selectedCategoryColor;
    [ObservableProperty] private bool _editCategoryRollover;
    [ObservableProperty] private SettingsPaymentMethodItem? _selectedPaymentMethod;
    [ObservableProperty] private string _paymentMethodName = string.Empty;
    [ObservableProperty] private EnumLookupItem<PaymentMethodType>? _selectedPaymentType;
    [ObservableProperty] private LookupItem? _selectedCreditCard;
    [ObservableProperty] private SettingsUserItem? _selectedUser;
    [ObservableProperty] private int _selectedTabIndex;

    public ObservableCollection<EnumLookupItem<PeriodFrequency>> FrequencyOptions { get; } = [];
    public ObservableCollection<SettingsCategoryItem> Categories { get; } = [];
    public ObservableCollection<SettingsSubcategoryItem> Subcategories { get; } = [];
    public ObservableCollection<SettingsPaymentMethodItem> PaymentMethods { get; } = [];
    public ObservableCollection<LookupItem> CreditCardOptions { get; } = [];
    public ObservableCollection<EnumLookupItem<PaymentMethodType>> PaymentTypeOptions { get; } = [];
    public ObservableCollection<BudgetGroupOption> GroupOptions { get; } = [];
    public ObservableCollection<GoalColorPreset> CategoryColorPresets { get; } = [];
    public ObservableCollection<ThemeVariantOption> ThemeOptions { get; } = [];
    public ObservableCollection<AccentColorOption> AccentOptions { get; } = [];
    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = [];
    public ObservableCollection<SettingsUserItem> UserProfiles { get; } = [];
    public ObservableCollection<SettingsExportHistoryItem> ExportHistory { get; } = [];

    public SettingsViewModel(ISettingsService settings, IThemeService theme, ICurrentUserContext user)
    {
        Title = "Configuración";
        _settings = settings;
        _theme = theme;
        _user = user;
        InitLookups();
    }

    partial void OnStatusMessageChanged(string value) => HasStatusMessage = !string.IsNullOrWhiteSpace(value);

    public string ActiveProfileDisplay => string.IsNullOrWhiteSpace(DisplayName) ? "Perfil local" : DisplayName.Trim();
    public string ActiveProfileInitial => string.IsNullOrWhiteSpace(DisplayName) ? "P" : char.ToUpperInvariant(DisplayName.Trim()[0]).ToString();
    public string ActiveCurrencyDisplay => SelectedCurrency?.Label ?? CurrencyCode;
    public string ActiveThemeDisplay => SelectedTheme?.Label ?? SettingsLabels.LabelThemeVariant("dark-premium");
    public string ActiveStorageDisplay => "Modo offline · almacenamiento local";
    public string ActiveCutoffDisplay => $"Día de corte {CutoffDay} · {SelectedFrequency?.Label ?? "Mensual"}";
    public string ActiveAlertsSummary => AlertsEnabled
        ? $"Activas · atención {AttentionThreshold}% · límite {LimitThreshold}%"
        : "Alertas desactivadas";
    public string ActiveAccentDisplay => SelectedAccent?.Hex ?? "#27D3FF";
    public string ActiveMultiUserSummary => MultiUserEnabled
        ? $"{UserProfiles.Count} perfiles · cambio habilitado"
        : "Un solo perfil en este equipo";

    partial void OnDisplayNameChanged(string value)
    {
        OnPropertyChanged(nameof(ActiveProfileDisplay));
        OnPropertyChanged(nameof(ActiveProfileInitial));
    }

    partial void OnSelectedCurrencyChanged(CurrencyOption? value) => OnPropertyChanged(nameof(ActiveCurrencyDisplay));
    partial void OnCurrencyCodeChanged(string value) => OnPropertyChanged(nameof(ActiveCurrencyDisplay));
    partial void OnSelectedThemeChanged(ThemeVariantOption? value) => OnPropertyChanged(nameof(ActiveThemeDisplay));
    partial void OnCutoffDayChanged(int value) => OnPropertyChanged(nameof(ActiveCutoffDisplay));
    partial void OnSelectedFrequencyChanged(EnumLookupItem<PeriodFrequency>? value) => OnPropertyChanged(nameof(ActiveCutoffDisplay));
    partial void OnAttentionThresholdChanged(int value) => OnPropertyChanged(nameof(ActiveAlertsSummary));
    partial void OnLimitThresholdChanged(int value) => OnPropertyChanged(nameof(ActiveAlertsSummary));
    partial void OnAlertsEnabledChanged(bool value) => OnPropertyChanged(nameof(ActiveAlertsSummary));
    partial void OnSelectedAccentChanged(AccentColorOption? value) => OnPropertyChanged(nameof(ActiveAccentDisplay));
    partial void OnMultiUserEnabledChanged(bool value) => OnPropertyChanged(nameof(ActiveMultiUserSummary));

    partial void OnSelectedCategoryChanged(SettingsCategoryItem? value)
    {
        if (value == null)
        {
            Subcategories.Clear();
            return;
        }
        EditCategoryRollover = value.AllowRollover;
        SelectedEditGroup = GroupOptions.FirstOrDefault(g => g.Group == value.Group) ?? GroupOptions[0];
        SelectedCategoryColor = CategoryColorPresets.FirstOrDefault(c => c.Hex.Equals(value.ColorHex, StringComparison.OrdinalIgnoreCase))
            ?? CategoryColorPresets[0];
        _ = LoadSubcategoriesAsync(value.Id);
    }

    partial void OnShowInactiveCategoriesChanged(bool value) => _ = ReloadCategoriesAsync();

    partial void OnSelectedPaymentMethodChanged(SettingsPaymentMethodItem? value)
    {
        if (value == null)
        {
            PaymentMethodName = string.Empty;
            SelectedPaymentType = PaymentTypeOptions.FirstOrDefault(o => o.Value == PaymentMethodType.Cash);
            SelectedCreditCard = CreditCardOptions.FirstOrDefault();
            return;
        }

        PaymentMethodName = value.Name;
        SelectedPaymentType = PaymentTypeOptions.FirstOrDefault(o => o.Value == value.Type) ?? PaymentTypeOptions[0];
        SelectedCreditCard = CreditCardOptions.FirstOrDefault(c => c.Id == value.CreditCardAccountId)
            ?? CreditCardOptions.FirstOrDefault();
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await _settings.GetProfileAsync();
            DisplayName = profile.DisplayName;
            Email = profile.Email;
            CutoffDay = profile.CutoffDay;
            SelectedFrequency = FrequencyOptions.FirstOrDefault(f => f.Value == profile.DefaultFrequency)
                ?? FrequencyOptions[0];

            var alert = await _settings.GetGlobalAlertRuleAsync();
            AttentionThreshold = alert.AttentionThreshold;
            LimitThreshold = alert.LimitThreshold;
            AlertsEnabled = alert.IsEnabled;

            var prefs = await _settings.GetPreferencesAsync();
            CurrencyCode = prefs.CurrencyCode;
            SelectedCurrency = CurrencyOptions.FirstOrDefault(c => c.Code == prefs.CurrencyCode) ?? CurrencyOptions[0];
            SelectedTheme = ThemeOptions.FirstOrDefault(t => t.Id == prefs.ThemeVariant) ?? ThemeOptions[0];
            SelectedAccent = AccentOptions.FirstOrDefault(a => a.Hex.Equals(prefs.AccentHex, StringComparison.OrdinalIgnoreCase))
                ?? AccentOptions[0];
            GoalMonthlyPaceInput = prefs.GoalDefaultMonthlyPace.ToString("0");
            GoalShowProjections = prefs.GoalShowProjections;
            GoalAutoCelebrate = prefs.GoalAutoCelebrate;
            GoalSuggestContributions = prefs.GoalSuggestContributions;
            ExportFolder = prefs.ExportDefaultFolder;
            ExportFilePrefix = prefs.ExportFileNamePrefix;
            ExportIncludeNotes = prefs.ExportIncludeNotes;
            ExportPdfCharts = prefs.ExportPdfCharts;
            ExportExcelAutoOpen = prefs.ExportExcelAutoOpen;
            MultiUserEnabled = prefs.MultiUserEnabled;

            await ReloadCategoriesAsync();
            await ReloadPaymentMethodsAsync();
            await ReloadUsersAsync();
            await ReloadExportHistoryAsync();
            RefreshSummaryDisplays();
            SetStatus("Configuración cargada.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfile()
    {
        try
        {
            var profile = await _settings.GetProfileAsync();
            profile.DisplayName = DisplayName;
            profile.Email = Email;
            await _settings.SaveProfileAsync(profile);
            SetStatus("Perfil guardado.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SavePeriod()
    {
        try
        {
            var profile = await _settings.GetProfileAsync();
            profile.CutoffDay = Math.Clamp(CutoffDay, 1, 28);
            profile.DefaultFrequency = SelectedFrequency?.Value ?? PeriodFrequency.Monthly;
            await _settings.SaveProfileAsync(profile);
            SetStatus("Periodo y corte guardados.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SaveAlerts()
    {
        try
        {
            await _settings.SaveGlobalAlertRuleAsync(AttentionThreshold, LimitThreshold, AlertsEnabled);
            SetStatus("Umbrales de alerta guardados.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SaveAppearance()
    {
        try
        {
            var prefs = await BuildPreferencesAsync();
            prefs.CurrencyCode = SelectedCurrency?.Code ?? CurrencyCode;
            prefs.ThemeVariant = SelectedTheme?.Id ?? "dark-premium";
            prefs.AccentHex = SelectedAccent?.Hex ?? "#27D3FF";
            await _settings.SavePreferencesAsync(prefs);
            CurrencyCode = prefs.CurrencyCode;
            _theme.Apply(prefs.ThemeVariant, prefs.AccentHex);
            SetStatus("Apariencia guardada.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SaveGoalsBehavior()
    {
        try
        {
            var prefs = await BuildPreferencesAsync();
            if (!decimal.TryParse(GoalMonthlyPaceInput, out var pace) || pace < 0)
                throw new ArgumentException("Ritmo mensual de referencia inválido.");
            prefs.GoalDefaultMonthlyPace = pace;
            prefs.GoalShowProjections = GoalShowProjections;
            prefs.GoalAutoCelebrate = GoalAutoCelebrate;
            prefs.GoalSuggestContributions = GoalSuggestContributions;
            await _settings.SavePreferencesAsync(prefs);
            SetStatus("Comportamiento de metas guardado.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SaveExportSettings()
    {
        try
        {
            var prefs = await BuildPreferencesAsync();
            prefs.ExportDefaultFolder = ExportFolder.Trim();
            prefs.ExportFileNamePrefix = string.IsNullOrWhiteSpace(ExportFilePrefix) ? "Ahorro" : ExportFilePrefix.Trim();
            prefs.ExportIncludeNotes = ExportIncludeNotes;
            prefs.ExportPdfCharts = ExportPdfCharts;
            prefs.ExportExcelAutoOpen = ExportExcelAutoOpen;
            if (!string.IsNullOrWhiteSpace(prefs.ExportDefaultFolder))
                Directory.CreateDirectory(prefs.ExportDefaultFolder);
            await _settings.SavePreferencesAsync(prefs);
            SetStatus("Exportación guardada.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SaveMultiUserSettings()
    {
        try
        {
            var prefs = await BuildPreferencesAsync();
            prefs.MultiUserEnabled = MultiUserEnabled;
            await _settings.SavePreferencesAsync(prefs);
            SetStatus(MultiUserEnabled ? "Multiusuario habilitado en este equipo." : "Modo un solo perfil activo.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private void BrowseExportFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Carpeta de exportación",
            InitialDirectory = Directory.Exists(ExportFolder) ? ExportFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog() == true)
            ExportFolder = dlg.FolderName;
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
                throw new ArgumentException("Ingresa un nombre de categoría.");

            var color = SelectedCategoryColor?.Hex ?? "#27D3FF";
            await _settings.AddCategoryAsync(new CategoryUpsert(
                NewCategoryName,
                SelectedNewGroup?.Group ?? BudgetGroup.Other,
                color,
                NewCategoryName.Trim().ToLowerInvariant(),
                false));

            NewCategoryName = string.Empty;
            await ReloadCategoriesAsync();
            SetStatus("Categoría creada.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SaveSelectedCategory()
    {
        if (SelectedCategory == null) return;
        try
        {
            await _settings.UpdateCategoryAsync(SelectedCategory.Id, new CategoryUpsert(
                SelectedCategory.Name,
                SelectedEditGroup?.Group ?? SelectedCategory.Group,
                SelectedCategoryColor?.Hex ?? SelectedCategory.ColorHex,
                SelectedCategory.IconKey,
                EditCategoryRollover,
                AllocationMode.Percentage));

            await ReloadCategoriesAsync();
            SetStatus("Categoría actualizada.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task ToggleCategoryActive()
    {
        if (SelectedCategory == null) return;
        try
        {
            await _settings.SetCategoryActiveAsync(SelectedCategory.Id, !SelectedCategory.IsActive);
            await ReloadCategoriesAsync();
            SetStatus(SelectedCategory.IsActive ? "Categoría desactivada." : "Categoría reactivada.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task MoveCategoryUp()
    {
        if (SelectedCategory == null) return;
        await _settings.ReorderCategoryAsync(SelectedCategory.Id, true);
        await ReloadCategoriesAsync();
    }

    [RelayCommand]
    private async Task MoveCategoryDown()
    {
        if (SelectedCategory == null) return;
        await _settings.ReorderCategoryAsync(SelectedCategory.Id, false);
        await ReloadCategoriesAsync();
    }

    [RelayCommand]
    private async Task AddSubcategory()
    {
        if (SelectedCategory == null) return;
        try
        {
            if (string.IsNullOrWhiteSpace(NewSubcategoryName))
                throw new ArgumentException("Ingresa un nombre de subcategoría.");

            await _settings.AddSubcategoryAsync(SelectedCategory.Id, NewSubcategoryName);
            NewSubcategoryName = string.Empty;
            await LoadSubcategoriesAsync(SelectedCategory.Id);
            await ReloadCategoriesAsync();
            SetStatus("Subcategoría creada.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SaveSelectedSubcategory()
    {
        if (SelectedSubcategory == null) return;
        try
        {
            await _settings.UpdateSubcategoryAsync(SelectedSubcategory.Id, SelectedSubcategory.Name);
            if (SelectedCategory != null)
                await LoadSubcategoriesAsync(SelectedCategory.Id);
            SetStatus("Subcategoría actualizada.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task ToggleSubcategoryActive()
    {
        if (SelectedSubcategory == null) return;
        try
        {
            await _settings.SetSubcategoryActiveAsync(SelectedSubcategory.Id, !SelectedSubcategory.IsActive);
            if (SelectedCategory != null)
                await LoadSubcategoriesAsync(SelectedCategory.Id);
            SetStatus("Estado de subcategoría actualizado.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SavePaymentMethod()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PaymentMethodName))
                throw new ArgumentException("Ingresa un nombre para el método de pago.");

            var type = SelectedPaymentType?.Value ?? PaymentMethodType.Cash;
            await _settings.SavePaymentMethodAsync(new PaymentMethodUpsert(
                SelectedPaymentMethod?.Id,
                PaymentMethodName,
                type,
                type == PaymentMethodType.Credit ? SelectedCreditCard?.Id : null,
                true));

            await ReloadPaymentMethodsAsync();
            SetStatus("Método de pago guardado.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private void NewPaymentMethod()
    {
        SelectedPaymentMethod = null;
        PaymentMethodName = string.Empty;
        SelectedPaymentType = PaymentTypeOptions.FirstOrDefault(o => o.Value == PaymentMethodType.Cash);
        SelectedCreditCard = CreditCardOptions.FirstOrDefault();
    }

    [RelayCommand]
    private async Task TogglePaymentMethodActive()
    {
        if (SelectedPaymentMethod == null) return;
        try
        {
            await _settings.SetPaymentMethodActiveAsync(SelectedPaymentMethod.Id, !SelectedPaymentMethod.IsActive);
            await ReloadPaymentMethodsAsync();
            SetStatus("Estado del método actualizado.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task SwitchUser()
    {
        if (SelectedUser == null || SelectedUser.IsCurrent) return;
        try
        {
            await _settings.SwitchProfileAsync(SelectedUser.Id);
            await LoadAsync();
            SetStatus($"Perfil activo: {SelectedUser.DisplayName}.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    [RelayCommand]
    private async Task RefreshExportHistory() => await ReloadExportHistoryAsync();

    [RelayCommand]
    private void OpenExportFolder()
    {
        if (!Directory.Exists(ExportFolder))
        {
            SetStatus("La carpeta de exportación no existe.", error: true);
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = ExportFolder,
            UseShellExecute = true
        });
    }

    private void InitLookups()
    {
        foreach (PeriodFrequency f in Enum.GetValues<PeriodFrequency>())
            FrequencyOptions.Add(new EnumLookupItem<PeriodFrequency> { Value = f, Label = SettingsLabels.LabelPeriodFrequency(f) });
        SelectedFrequency = FrequencyOptions[0];

        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Needs, Label = SettingsLabels.LabelBudgetGroup(BudgetGroup.Needs) });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Wants, Label = SettingsLabels.LabelBudgetGroup(BudgetGroup.Wants) });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Savings, Label = SettingsLabels.LabelBudgetGroup(BudgetGroup.Savings) });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Other, Label = SettingsLabels.LabelBudgetGroup(BudgetGroup.Other) });
        SelectedNewGroup = GroupOptions[0];
        SelectedEditGroup = GroupOptions[0];

        foreach (var hex in new[] { "#27D3FF", "#35E0A1", "#9B7AFF", "#FFB84D", "#FF6B6B", "#E8EDF5" })
            CategoryColorPresets.Add(new GoalColorPreset { Hex = hex, Swatch = BrushHelper.FromHex(hex) });
        SelectedCategoryColor = CategoryColorPresets[0];

        ThemeOptions.Add(new ThemeVariantOption { Id = "dark-premium", Label = SettingsLabels.LabelThemeVariant("dark-premium") });
        ThemeOptions.Add(new ThemeVariantOption { Id = "dark-midnight", Label = SettingsLabels.LabelThemeVariant("dark-midnight") });
        ThemeOptions.Add(new ThemeVariantOption { Id = "dark-emerald", Label = SettingsLabels.LabelThemeVariant("dark-emerald") });
        SelectedTheme = ThemeOptions[0];

        foreach (var hex in new[] { "#27D3FF", "#35E0A1", "#9B7AFF", "#FFB84D", "#FF6B6B" })
            AccentOptions.Add(new AccentColorOption { Hex = hex, Swatch = BrushHelper.FromHex(hex) });
        SelectedAccent = AccentOptions[0];

        CurrencyOptions.Add(new CurrencyOption { Code = "CLP", Label = "Peso chileno (CLP)" });
        CurrencyOptions.Add(new CurrencyOption { Code = "USD", Label = "Dólar (USD)" });
        CurrencyOptions.Add(new CurrencyOption { Code = "EUR", Label = "Euro (EUR)" });
        SelectedCurrency = CurrencyOptions[0];

        foreach (PaymentMethodType t in Enum.GetValues<PaymentMethodType>())
            PaymentTypeOptions.Add(new EnumLookupItem<PaymentMethodType> { Value = t, Label = SettingsLabels.LabelPaymentMethodType(t) });
        SelectedPaymentType = PaymentTypeOptions[0];
    }

    private async Task<UserPreferences> BuildPreferencesAsync()
    {
        var current = await _settings.GetPreferencesAsync();
        current.ExportDefaultFolder = ExportFolder;
        current.ExportFileNamePrefix = ExportFilePrefix;
        current.ExportIncludeNotes = ExportIncludeNotes;
        current.ExportPdfCharts = ExportPdfCharts;
        current.ExportExcelAutoOpen = ExportExcelAutoOpen;
        current.MultiUserEnabled = MultiUserEnabled;
        return current;
    }

    private async Task ReloadCategoriesAsync()
    {
        var list = await _settings.GetCategoriesAsync(ShowInactiveCategories);
        var selectedId = SelectedCategory?.Id;
        Categories.Clear();
        foreach (var c in list)
        {
            Categories.Add(new SettingsCategoryItem
            {
                Id = c.Id,
                Name = c.Name,
                Group = c.DefaultGroup,
                GroupLabel = SettingsLabels.LabelBudgetGroup(c.DefaultGroup),
                ColorHex = c.ColorHex,
                ColorBrush = BrushHelper.FromHex(c.ColorHex),
                IconKey = c.IconKey,
                AllowRollover = c.AllowRollover,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
                SubcategoryCount = c.Subcategories.Count(s => ShowInactiveCategories || s.IsActive)
            });
        }

        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId) ?? Categories.FirstOrDefault();
    }

    private async Task LoadSubcategoriesAsync(Guid categoryId)
    {
        var cats = await _settings.GetCategoriesAsync(includeInactive: true);
        var cat = cats.FirstOrDefault(c => c.Id == categoryId);
        Subcategories.Clear();
        if (cat == null) return;

        foreach (var s in cat.Subcategories.OrderBy(x => x.SortOrder))
        {
            if (!ShowInactiveCategories && !s.IsActive) continue;
            Subcategories.Add(new SettingsSubcategoryItem
            {
                Id = s.Id,
                Name = s.Name,
                IsActive = s.IsActive
            });
        }

        SelectedSubcategory = Subcategories.FirstOrDefault();
    }

    private async Task ReloadPaymentMethodsAsync()
    {
        var cards = await _settings.GetCreditCardsAsync();
        CreditCardOptions.Clear();
        CreditCardOptions.Add(new LookupItem { Id = null, Name = "Sin tarjeta vinculada" });
        foreach (var c in cards)
            CreditCardOptions.Add(new LookupItem { Id = c.Id, Name = c.Name });

        var methods = await _settings.GetPaymentMethodsAsync(includeInactive: true);
        var selectedId = SelectedPaymentMethod?.Id;
        PaymentMethods.Clear();
        foreach (var m in methods)
        {
            PaymentMethods.Add(new SettingsPaymentMethodItem
            {
                Id = m.Id,
                Name = m.Name,
                Type = m.Type,
                TypeLabel = SettingsLabels.LabelPaymentMethodType(m.Type),
                CreditCardAccountId = m.CreditCardAccountId,
                CreditCardName = m.CreditCardAccount?.Name ?? "—",
                IsActive = m.IsActive
            });
        }

        SelectedPaymentMethod = PaymentMethods.FirstOrDefault(p => p.Id == selectedId) ?? PaymentMethods.FirstOrDefault();
    }

    private async Task ReloadUsersAsync()
    {
        var profiles = await _settings.GetProfilesAsync();
        UserProfiles.Clear();
        foreach (var u in profiles)
        {
            UserProfiles.Add(new SettingsUserItem
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                IsCurrent = u.Id == _user.UserId,
                IsLocal = u.IsLocal
            });
        }

        SelectedUser = UserProfiles.FirstOrDefault(u => u.IsCurrent) ?? UserProfiles.FirstOrDefault();
        OnPropertyChanged(nameof(ActiveMultiUserSummary));
        MultiUserDescription = profiles.Count <= 1
            ? "Base preparada para varios perfiles. Crea perfiles adicionales en futuras versiones o importación."
            : $"{profiles.Count} perfiles locales registrados. Selecciona uno para cambiar el contexto activo.";
    }

    private async Task ReloadExportHistoryAsync()
    {
        var rows = await _settings.GetExportHistoryAsync();
        ExportHistory.Clear();
        foreach (var e in rows)
        {
            ExportHistory.Add(new SettingsExportHistoryItem
            {
                Id = e.Id,
                TypeLabel = e.ExportType switch
                {
                    ExportType.Transactions => "Movimientos",
                    ExportType.Budget => "Presupuesto",
                    ExportType.Report => "Reporte",
                    ExportType.Goals => "Metas",
                    _ => e.ExportType.ToString()
                },
                FileName = Path.GetFileName(e.FilePath),
                CreatedLabel = e.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                FilePath = e.FilePath
            });
        }
    }

    private void RefreshSummaryDisplays()
    {
        OnPropertyChanged(nameof(ActiveProfileDisplay));
        OnPropertyChanged(nameof(ActiveProfileInitial));
        OnPropertyChanged(nameof(ActiveCurrencyDisplay));
        OnPropertyChanged(nameof(ActiveThemeDisplay));
        OnPropertyChanged(nameof(ActiveCutoffDisplay));
        OnPropertyChanged(nameof(ActiveAlertsSummary));
        OnPropertyChanged(nameof(ActiveAccentDisplay));
        OnPropertyChanged(nameof(ActiveMultiUserSummary));
    }

    private void SetStatus(string message, bool error = false)
    {
        StatusMessage = message;
        HasStatusMessage = true;
    }
}
