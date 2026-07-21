using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolutionTweaker.Helpers;
using EvolutionTweaker.Services;

namespace EvolutionTweaker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly PendingTweaksService _pending;

    [ObservableProperty] private string _greeting = string.Empty;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private string _activeSection = "dashboard";

    public MainViewModel(
        SettingsService settings,
        PendingTweaksService pending)
    {
        _settings = settings;
        _pending = pending;

        Greeting = $"Привет, {Models.SystemInfo.UserName}!";

        _pending.Changed += () =>
        {
            PendingCount = _pending.TotalCount;
            OnPropertyChanged(nameof(ApplyButtonText));
        };
    }

    public string ApplyButtonText => PendingCount > 0 ? $"✓ Применить {PendingCount}" : "✓ Применить";

    public ICommand NavigateCommand => new RelayCommand<string>(section =>
    {
        ActiveSection = section ?? "dashboard";
    });
}