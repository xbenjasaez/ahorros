using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Settings;

public partial class SettingsViewModel : ViewModelBase, ILoadable
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private int _cutoffDay = 25;
    [ObservableProperty] private PeriodFrequency _frequency = PeriodFrequency.Monthly;
    [ObservableProperty] private int _attentionThreshold = 80;
    [ObservableProperty] private int _limitThreshold = 100;
    [ObservableProperty] private string _currency = "CLP";
    [ObservableProperty] private string _themeNote = "Tema oscuro premium (fijo)";
    [ObservableProperty] private string _multiUserNote = "Preparado para múltiples perfiles — disponible en versión futura";

    public SettingsViewModel(ISettingsService settings)
    {
        Title = "Configuración";
        _settings = settings;
    }

    public async Task LoadAsync()
    {
        var profile = await _settings.GetProfileAsync();
        DisplayName = profile.DisplayName;
        Email = profile.Email;
        CutoffDay = profile.CutoffDay;
        Frequency = profile.DefaultFrequency;
    }

    [RelayCommand]
    private async Task Save()
    {
        var profile = await _settings.GetProfileAsync();
        profile.DisplayName = DisplayName;
        profile.Email = Email;
        profile.CutoffDay = Math.Clamp(CutoffDay, 1, 28);
        profile.DefaultFrequency = Frequency;
        await _settings.SaveProfileAsync(profile);
    }
}
