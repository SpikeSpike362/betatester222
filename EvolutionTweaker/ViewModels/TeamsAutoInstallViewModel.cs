using CommunityToolkit.Mvvm.ComponentModel;
using EvolutionTweaker.Services;

namespace EvolutionTweaker.ViewModels;

public partial class TeamsAutoInstallViewModel : ObservableObject
{
    private const string Key = "debloat:teams_autoinstall";
    private readonly DebloatEngine _engine;
    private readonly PendingTweaksService _pending;
    private bool _suppress;

    [ObservableProperty] private bool _currentState;
    [ObservableProperty] private bool _sliderValue;
    [ObservableProperty] private bool _isOptimal;
    public string StateText => SliderValue ? "Включено" : "Отключено";

    public TeamsAutoInstallViewModel(DebloatEngine engine, PendingTweaksService pending)
    {
        _engine = engine;
        _pending = pending;
        _pending.Changed += OnPendingChanged;
        Refresh();
    }

    private void Refresh()
    {
        CurrentState = _engine.GetTeamsAutoInstallEnabled();
        _suppress = true;
        SliderValue = _pending.IsPending(Key, out var d) ? d : CurrentState;
        _suppress = false;
        UpdateOptimal();
        OnPropertyChanged(nameof(StateText));
    }

    private void OnPendingChanged()
    {
        if (!_pending.IsPending(Key, out _) && SliderValue != CurrentState)
        {
            _suppress = true; SliderValue = CurrentState; _suppress = false;
            UpdateOptimal(); OnPropertyChanged(nameof(StateText));
        }
    }

    partial void OnSliderValueChanged(bool value)
    {
        OnPropertyChanged(nameof(StateText));
        if (_suppress) return;
        if (value == CurrentState) _pending.RemovePending(Key);
        else _pending.SetPending(Key, value);
        UpdateOptimal();
    }

    private void UpdateOptimal() => IsOptimal = !SliderValue;
}