using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Payments;

public partial class PaymentsViewModel : ViewModelBase, ILoadable
{
    private readonly IScheduledPaymentService _payments;
    private readonly IBudgetService _budget;
    private readonly ITransactionService _transactions;
    private Guid? _editingId;
    private List<PaymentListItem> _allItems = [];
    private DateTime? _selectedCalendarDate;

    [ObservableProperty] private string _activeCountLabel = "0 pagos activos";
    [ObservableProperty] private string _calendarMonthLabel = string.Empty;
    [ObservableProperty] private string _calendarSummaryLabel = string.Empty;
    [ObservableProperty] private string _calendarDayFilterLabel = string.Empty;
    [ObservableProperty] private bool _hasCalendarDayFilter;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatusMessage;
    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private bool _hasUpcomingHighlights;
    [ObservableProperty] private bool _isListEmpty;
    [ObservableProperty] private string _editorTitle = "Nuevo pago programado";
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editAmountInput = "0";
    [ObservableProperty] private string _editDueDateInput = string.Empty;
    [ObservableProperty] private string _editReminderDays = "3";
    [ObservableProperty] private LookupItem? _editCategory;
    [ObservableProperty] private LookupItem? _editPaymentMethod;
    [ObservableProperty] private EnumLookupItem<IncomeFrequency>? _editFrequency;
    [ObservableProperty] private PaymentStatusFilterItem? _selectedStatusFilter;
    [ObservableProperty] private DateTime? _filterDateFrom;
    [ObservableProperty] private DateTime? _filterDateTo;
    [ObservableProperty] private PaymentListItem? _selectedPayment;

    public ObservableCollection<KpiCardModel> SummaryKpis { get; } = [];
    public ObservableCollection<PaymentListItem> UpcomingHighlights { get; } = [];
    public ObservableCollection<PaymentListItem> FilteredItems { get; } = [];
    public ObservableCollection<PaymentCalendarDayItem> CalendarDays { get; } = [];
    public ObservableCollection<PaymentStatusFilterItem> StatusFilterOptions { get; } = [];
    public ObservableCollection<EnumLookupItem<IncomeFrequency>> FrequencyOptions { get; } = [];
    public ObservableCollection<LookupItem> CategoryOptions { get; } = [];
    public ObservableCollection<LookupItem> PaymentMethodOptions { get; } = [];

    partial void OnStatusMessageChanged(string value) => HasStatusMessage = !string.IsNullOrWhiteSpace(value);
    partial void OnSelectedStatusFilterChanged(PaymentStatusFilterItem? value) => ApplyFilters();
    partial void OnFilterDateFromChanged(DateTime? value) => _ = ReloadPaymentsAsync();
    partial void OnFilterDateToChanged(DateTime? value) => _ = ReloadPaymentsAsync();

    public PaymentsViewModel(
        IScheduledPaymentService payments,
        IBudgetService budget,
        ITransactionService transactions)
    {
        Title = "Pagos y vencimientos";
        _payments = payments;
        _budget = budget;
        _transactions = transactions;
        InitFilters();
    }

    private void InitFilters()
    {
        StatusFilterOptions.Add(new PaymentStatusFilterItem { Status = null, Label = "Todos" });
        foreach (var st in Enum.GetValues<ScheduledPaymentStatus>())
            StatusFilterOptions.Add(new PaymentStatusFilterItem { Status = st, Label = ScheduledPaymentLabels.Status(st) });

        foreach (var f in Enum.GetValues<IncomeFrequency>())
            FrequencyOptions.Add(new EnumLookupItem<IncomeFrequency> { Value = f, Label = ScheduledPaymentLabels.Frequency(f) });

        SelectedStatusFilter = StatusFilterOptions[0];
        EditFrequency = FrequencyOptions[0];
        FilterDateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        FilterDateTo = FilterDateFrom.Value.AddMonths(2).AddDays(-1);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await ReloadLookupsAsync();
            var summary = await _payments.GetSummaryAsync();
            ActiveCountLabel = summary.TotalActive == 1 ? "1 pago activo" : $"{summary.TotalActive} pagos activos";
            PopulateSummaryKpis(summary);

            var list = await _payments.GetAllAsync(FilterDateFrom, FilterDateTo);
            _allItems = list.Select(MapItem).ToList();
            BuildCalendar();
            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PopulateSummaryKpis(ScheduledPaymentSummary summary)
    {
        SummaryKpis.Clear();
        SummaryKpis.Add(CreateKpi("VENCIDOS", summary.OverdueCount.ToString(),
            summary.OverdueCount == 1 ? "requiere acción" : "requieren acción", "#FF6B6B"));
        SummaryKpis.Add(CreateKpi("PRÓXIMOS", summary.UpcomingCount.ToString(),
            "en ventana de recordatorio", "#27D3FF"));
        SummaryKpis.Add(CreateKpi("PENDIENTES", summary.PendingCount.ToString(),
            "fuera de alerta inmediata", "#93A4BD"));
        SummaryKpis.Add(CreateKpi("POR PAGAR ESTE MES", ClpFormatter.Format(summary.TotalDueThisMonth),
            "monto estimado restante", "#35E0A1"));
    }

    private static KpiCardModel CreateKpi(string title, string value, string subtitle, string accentHex) =>
        new()
        {
            Title = title,
            Value = value,
            Subtitle = subtitle,
            AccentColor = accentHex,
            AccentBrush = BrushHelper.FromHex(accentHex)
        };

    private async Task ReloadLookupsAsync()
    {
        var cats = await _budget.GetCategoriesAsync();
        CategoryOptions.Clear();
        foreach (var c in cats.OrderBy(c => c.Name))
            CategoryOptions.Add(new LookupItem { Id = c.Id, Name = c.Name });

        var methods = await _transactions.GetPaymentMethodsAsync();
        PaymentMethodOptions.Clear();
        foreach (var m in methods.OrderBy(m => m.Name))
            PaymentMethodOptions.Add(new LookupItem { Id = m.Id, Name = m.Name });

        if (EditCategory == null)
            EditCategory = CategoryOptions.FirstOrDefault();
        if (EditPaymentMethod == null)
            EditPaymentMethod = PaymentMethodOptions.FirstOrDefault();
    }

    private void ApplyFilters()
    {
        var query = _allItems.AsEnumerable();
        var status = SelectedStatusFilter?.Status;
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (_selectedCalendarDate.HasValue)
            query = query.Where(p => p.DueDateValue.Date == _selectedCalendarDate.Value.Date);

        var items = query.OrderBy(p => p.DueDateValue).ToList();
        FilteredItems.Clear();
        foreach (var p in items)
            FilteredItems.Add(p);

        IsListEmpty = FilteredItems.Count == 0;

        UpcomingHighlights.Clear();
        foreach (var p in _allItems
                     .Where(p => p.Status is ScheduledPaymentStatus.Upcoming or ScheduledPaymentStatus.Overdue)
                     .OrderBy(p => p.Status == ScheduledPaymentStatus.Overdue ? 0 : 1)
                     .ThenBy(p => p.DueDateValue)
                     .Take(3))
            UpcomingHighlights.Add(p);

        HasUpcomingHighlights = UpcomingHighlights.Count > 0;
        RefreshCalendarSelection();
    }

    private void BuildCalendar()
    {
        CalendarDays.Clear();
        var anchor = FilterDateFrom ?? DateTime.Today;
        var first = new DateTime(anchor.Year, anchor.Month, 1);
        var monthEnd = first.AddMonths(1).AddDays(-1);
        CalendarMonthLabel = first.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-CL"));

        var inMonth = _allItems
            .Where(p => p.DueDateValue >= first && p.DueDateValue <= monthEnd)
            .ToList();
        var overdue = inMonth.Count(p => p.Status == ScheduledPaymentStatus.Overdue);
        var upcoming = inMonth.Count(p => p.Status == ScheduledPaymentStatus.Upcoming);
        var pending = inMonth.Count(p => p.Status == ScheduledPaymentStatus.Pending);
        CalendarSummaryLabel = inMonth.Count == 0
            ? "Sin vencimientos en este mes"
            : $"{inMonth.Count} en el mes · {overdue} vencidos · {upcoming} próximos · {pending} pendientes";

        var offset = first.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)first.DayOfWeek - 1;
        var start = first.AddDays(-offset);
        var byDate = _allItems.GroupBy(p => p.DueDateValue.Date).ToDictionary(g => g.Key, g => g.ToList());

        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            byDate.TryGetValue(date.Date, out var dayPayments);
            var count = dayPayments?.Count ?? 0;
            var worst = dayPayments?
                .OrderByDescending(p => p.Status == ScheduledPaymentStatus.Overdue)
                .ThenByDescending(p => p.Status == ScheduledPaymentStatus.Upcoming)
                .ThenByDescending(p => p.Status == ScheduledPaymentStatus.Pending)
                .FirstOrDefault();

            var accent = worst != null
                ? BrushHelper.FromHex(ScheduledPaymentLabels.StatusColor(worst.Status))
                : BrushHelper.FromHex("#3A4A5C");

            CalendarDays.Add(new PaymentCalendarDayItem
            {
                Date = date,
                DayNumber = date.Day,
                WeekdayShort = date.ToString("ddd", new System.Globalization.CultureInfo("es-CL")),
                IsToday = date.Date == DateTime.Today,
                IsCurrentMonth = date.Month == first.Month,
                HasPayments = count > 0,
                PaymentCount = count,
                PaymentCountLabel = count > 1 ? count.ToString() : string.Empty,
                DominantStatus = worst?.Status,
                Tooltip = count > 0
                    ? string.Join(Environment.NewLine, dayPayments!.Select(p => $"{p.Name} · {p.Amount} · {p.StatusLabel}"))
                    : date.ToString("dddd d MMMM", new System.Globalization.CultureInfo("es-CL")),
                AccentBrush = accent,
                CellBackground = count > 0 && worst != null
                    ? BrushHelper.FromHex(TintHex(ScheduledPaymentLabels.StatusColor(worst.Status), 0.12))
                    : BrushHelper.FromHex("#141C26"),
                IsSelected = _selectedCalendarDate.HasValue && _selectedCalendarDate.Value.Date == date.Date
            });
        }
    }

    private void RefreshCalendarSelection()
    {
        if (CalendarDays.Count == 0) return;

        var days = CalendarDays.ToList();
        CalendarDays.Clear();
        foreach (var day in days)
        {
            CalendarDays.Add(new PaymentCalendarDayItem
            {
                Date = day.Date,
                DayNumber = day.DayNumber,
                WeekdayShort = day.WeekdayShort,
                IsToday = day.IsToday,
                IsCurrentMonth = day.IsCurrentMonth,
                HasPayments = day.HasPayments,
                PaymentCount = day.PaymentCount,
                PaymentCountLabel = day.PaymentCountLabel,
                DominantStatus = day.DominantStatus,
                Tooltip = day.Tooltip,
                AccentBrush = day.AccentBrush,
                CellBackground = day.CellBackground,
                IsSelected = _selectedCalendarDate.HasValue && _selectedCalendarDate.Value.Date == day.Date.Date
            });
        }

        if (_selectedCalendarDate.HasValue)
        {
            HasCalendarDayFilter = true;
            CalendarDayFilterLabel = _selectedCalendarDate.Value.ToString(
                "dddd d MMM", new System.Globalization.CultureInfo("es-CL"));
        }
        else
        {
            HasCalendarDayFilter = false;
            CalendarDayFilterLabel = string.Empty;
        }
    }

    private static string TintHex(string hex, double alpha)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "#141C26";
        var a = (int)(alpha * 255);
        return $"#{a:X2}{hex}";
    }

    private static PaymentListItem MapItem(ScheduledPayment p)
    {
        var days = (p.DueDate.Date - DateTime.Today).Days;
        var daysLabel = days switch
        {
            < 0 => $"Hace {Math.Abs(days)} días",
            0 => "Vence hoy",
            1 => "Mañana",
            _ => $"En {days} días"
        };
        var color = p.Category?.ColorHex ?? "#27D3FF";
        var statusColor = ScheduledPaymentLabels.StatusColor(p.Status);

        return new PaymentListItem
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category?.Name ?? "—",
            PaymentMethod = p.PaymentMethod?.Name ?? "—",
            Amount = ClpFormatter.Format(p.EstimatedAmount),
            DueDateValue = p.DueDate.Date,
            DueDate = p.DueDate.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("es-CL")),
            DaysUntilDue = days,
            DaysLabel = daysLabel,
            FrequencyLabel = ScheduledPaymentLabels.Frequency(p.Frequency),
            ReminderLabel = p.ReminderDaysBefore > 0
                ? $"{p.ReminderDaysBefore} días antes"
                : "Sin recordatorio",
            LastPaidLabel = p.LastPaidDate.HasValue
                ? $"Último pago {p.LastPaidDate.Value:dd MMM yyyy}"
                : "Sin pagos registrados",
            IsRecurring = p.Frequency != IncomeFrequency.OneTime,
            Status = p.Status,
            StatusLabel = ScheduledPaymentLabels.Status(p.Status),
            StatusColor = statusColor,
            StatusBrush = BrushHelper.FromHex(statusColor),
            StatusBadgeBackground = BrushHelper.FromHex(TintHex(statusColor, 0.18)),
            CategoryBrush = BrushHelper.FromHex(color),
            CanRegister = p.Status != ScheduledPaymentStatus.Paid
        };
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void OpenNewPayment()
    {
        _editingId = null;
        EditorTitle = "Nuevo pago programado";
        IsEditorOpen = true;
        EditName = "Nuevo servicio";
        EditAmountInput = "25000";
        EditDueDateInput = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
        EditReminderDays = "3";
        EditCategory = CategoryOptions.FirstOrDefault();
        EditPaymentMethod = PaymentMethodOptions.FirstOrDefault();
        EditFrequency = FrequencyOptions.FirstOrDefault();
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task OpenEditPayment(PaymentListItem? item)
    {
        if (item == null) return;
        var entity = await _payments.GetByIdAsync(item.Id);
        if (entity == null) return;

        _editingId = entity.Id;
        EditorTitle = "Editar pago programado";
        IsEditorOpen = true;
        EditName = entity.Name;
        EditAmountInput = ((long)entity.EstimatedAmount).ToString();
        EditDueDateInput = entity.DueDate.ToString("yyyy-MM-dd");
        EditReminderDays = entity.ReminderDaysBefore.ToString();
        EditCategory = CategoryOptions.FirstOrDefault(c => c.Id == entity.CategoryId) ?? CategoryOptions.FirstOrDefault();
        EditPaymentMethod = PaymentMethodOptions.FirstOrDefault(m => m.Id == entity.PaymentMethodId) ?? PaymentMethodOptions.FirstOrDefault();
        EditFrequency = FrequencyOptions.FirstOrDefault(f => f.Value == entity.Frequency) ?? FrequencyOptions.FirstOrDefault();
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditorOpen = false;
        _editingId = null;
    }

    [RelayCommand]
    private async Task SavePayment()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            StatusMessage = "El nombre es obligatorio.";
            return;
        }

        if (!decimal.TryParse(EditAmountInput, out var amount) || amount <= 0)
        {
            StatusMessage = "Indica un monto válido.";
            return;
        }

        if (!DateTime.TryParse(EditDueDateInput, out var due))
        {
            StatusMessage = "Fecha de vencimiento inválida (yyyy-MM-dd).";
            return;
        }

        if (!int.TryParse(EditReminderDays, out var reminder) || reminder < 0)
            reminder = 3;

        if (EditCategory?.Id == null || EditPaymentMethod?.Id == null)
        {
            StatusMessage = "Selecciona categoría y método de pago.";
            return;
        }

        var data = new ScheduledPaymentUpsert(
            EditName.Trim(),
            EditCategory.Id.Value,
            amount,
            due.Date,
            EditFrequency?.Value ?? IncomeFrequency.Monthly,
            reminder,
            EditPaymentMethod.Id.Value);

        if (_editingId.HasValue)
            await _payments.UpdateAsync(_editingId.Value, data);
        else
            await _payments.CreateAsync(data);

        IsEditorOpen = false;
        _editingId = null;
        StatusMessage = "Pago programado guardado.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RegisterPayment(PaymentListItem? item)
    {
        if (item == null || !item.CanRegister) return;
        await _payments.RegisterPaymentAsync(item.Id);
        StatusMessage = $"Pago “{item.Name}” registrado.";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearDateFilters()
    {
        _selectedCalendarDate = null;
        FilterDateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        FilterDateTo = FilterDateFrom.Value.AddMonths(2).AddDays(-1);
        await LoadAsync();
    }

    [RelayCommand]
    private void SelectCalendarDay(PaymentCalendarDayItem? day)
    {
        if (day == null || !day.IsCurrentMonth) return;

        if (_selectedCalendarDate.HasValue && _selectedCalendarDate.Value.Date == day.Date.Date)
            _selectedCalendarDate = null;
        else
            _selectedCalendarDate = day.Date.Date;

        ApplyFilters();
    }

    [RelayCommand]
    private void ClearCalendarDayFilter()
    {
        _selectedCalendarDate = null;
        ApplyFilters();
    }

    private async Task ReloadPaymentsAsync()
    {
        if (IsBusy) return;
        _selectedCalendarDate = null;
        var list = await _payments.GetAllAsync(FilterDateFrom, FilterDateTo);
        _allItems = list.Select(MapItem).ToList();
        BuildCalendar();
        ApplyFilters();
    }
}
