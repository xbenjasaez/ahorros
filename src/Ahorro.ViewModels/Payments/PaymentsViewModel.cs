using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Payments;

public partial class PaymentsViewModel : ViewModelBase, ILoadable
{
    private readonly IScheduledPaymentService _payments;

    [ObservableProperty] private string? _statusFilter;
    public ObservableCollection<string> StatusFilters { get; } = ["Todos", "Pending", "Upcoming", "Overdue", "Paid"];
    public ObservableCollection<PaymentListItem> PaymentItems { get; } = [];
    public ObservableCollection<PaymentListItem> FilteredItems { get; } = [];

    public PaymentsViewModel(IScheduledPaymentService payments)
    {
        Title = "Pagos programados";
        _payments = payments;
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        var list = await _payments.GetUpcomingAsync(60);
        PaymentItems.Clear();
        foreach (var p in list)
        {
            PaymentItems.Add(new PaymentListItem
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category?.Name ?? "—",
                Amount = ClpFormatter.Format(p.EstimatedAmount),
                DueDate = p.DueDate.ToString("dd MMM yyyy"),
                Status = p.Status,
                StatusLabel = StatusToLabel(p.Status)
            });
        }
        ApplyStatusFilter();
        IsBusy = false;
    }

    partial void OnStatusFilterChanged(string? value) => ApplyStatusFilter();

    private void ApplyStatusFilter()
    {
        FilteredItems.Clear();
        var query = PaymentItems.AsEnumerable();
        if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "Todos" && Enum.TryParse<ScheduledPaymentStatus>(StatusFilter, out var st))
            query = query.Where(p => p.Status == st);
        foreach (var p in query)
            FilteredItems.Add(p);
    }

    [RelayCommand]
    private async Task RegisterPayment(PaymentListItem? item)
    {
        if (item == null) return;
        await _payments.RegisterPaymentAsync(item.Id);
        await LoadAsync();
    }

    private static string StatusToLabel(ScheduledPaymentStatus s) => s switch
    {
        ScheduledPaymentStatus.Pending => "Pendiente",
        ScheduledPaymentStatus.Upcoming => "Próximo",
        ScheduledPaymentStatus.Paid => "Pagado",
        ScheduledPaymentStatus.Overdue => "Vencido",
        _ => s.ToString()
    };
}
