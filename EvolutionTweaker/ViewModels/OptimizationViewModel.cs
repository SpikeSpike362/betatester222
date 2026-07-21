using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EvolutionTweaker.Models;
using EvolutionTweaker.Services;

namespace EvolutionTweaker.ViewModels;

public partial class TweakItemViewModel : ObservableObject
{
    private readonly ITweak _tweak;
    private readonly PendingTweaksService _pending;
    private bool _suppress;

    [ObservableProperty] private bool _currentState;
    [ObservableProperty] private bool _sliderValue;
    [ObservableProperty] private bool _isOptimal;
    [ObservableProperty] private bool _isExpanded;
    public TweakInfo Info => _tweak.Info;
    public string StateText => SliderValue ? "Включено" : "Отключено";

    public TweakItemViewModel(ITweak tweak, PendingTweaksService pending) { _tweak = tweak; _pending = pending; }

    public async Task RefreshStateAsync()
    {
        CurrentState = await _tweak.GetCurrentStateAsync();
        _suppress = true;
        SliderValue = _pending.IsPending(Info.Id, out var d) ? d : CurrentState;
        _suppress = false;
        UpdateOptimal(); OnPropertyChanged(nameof(StateText));
    }
    partial void OnSliderValueChanged(bool value)
    {
        OnPropertyChanged(nameof(StateText));
        if (_suppress) return;
        if (value == CurrentState) _pending.RemovePending(Info.Id);
        else _pending.SetPending(Info.Id, value);
        UpdateOptimal();
    }
    private void UpdateOptimal() => IsOptimal = Info.OptimalStateIsEnabled ? SliderValue : !SliderValue;
}

public partial class OptimizationViewModel : ObservableObject
{
    private const string TeamsKey = "debloat:teams_autoinstall";
    private readonly TweakEngine _engine;
    private readonly PendingTweaksService _pending;
    private readonly DebloatEngine _debloat;
    private readonly Dictionary<string, TweakItemViewModel> _all = new();

    public OptimizationViewModel(TweakEngine engine, PendingTweaksService pending, DebloatEngine debloat)
    {
        _engine = engine; _pending = pending; _debloat = debloat;
        Categories = new List<CategoryInfo>
        {
            new("basic","Базовое",TweakCategory.Basic), new("security","Безопасность",TweakCategory.Security),
            new("customization","Кастомизация",TweakCategory.Customization), new("nvidia","Панель Nvidia",TweakCategory.NvidiaPanel),
            new("power","Электропитание",TweakCategory.Power), new("debloat","Выпиливание",TweakCategory.Debloat),
            new("cleaning","Очистка",TweakCategory.Cleaning), new("privacy","Приватность",TweakCategory.Privacy),
            new("tweaks","Твики",TweakCategory.Tweaks),
        };
        foreach (var t in engine.GetAll()) _all[t.Info.Id] = new TweakItemViewModel(t, pending);
        SelectedCategory = Categories.First();
        _ = LoadCategoryAsync();
        _pending.Changed += () =>
        {
            OnPropertyChanged(nameof(TotalPendingCount));
            OnPropertyChanged(nameof(ApplyButtonText));
            OnPropertyChanged(nameof(CanApply));
        };
    }

    public List<CategoryInfo> Categories { get; }
    [ObservableProperty] private CategoryInfo? _selectedCategory;
    [ObservableProperty] private List<TweakItemViewModel> _currentTweaks = new();
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private bool _showApplyOverlay;
    [ObservableProperty] private DebloatViewModel? _debloatVm;
    public bool IsDebloatCategory => SelectedCategory?.Category == TweakCategory.Debloat;

    partial void OnSelectedCategoryChanged(CategoryInfo? value)
    {
        if (value == null) return;
        OnPropertyChanged(nameof(IsDebloatCategory));
        if (value.Category == TweakCategory.Debloat && DebloatVm == null)
            DebloatVm = new DebloatViewModel(_debloat, _pending);
        _ = LoadCategoryAsync();
    }
    private async Task LoadCategoryAsync()
    {
        if (SelectedCategory == null) return;
        if (SelectedCategory.Category == TweakCategory.Debloat) { CurrentTweaks = new(); return; }
        var tw = _engine.GetByCategory(SelectedCategory.Category).Select(t => _all[t.Info.Id]).ToList();
        foreach (var vm in tw) await vm.RefreshStateAsync();
        CurrentTweaks = tw;
    }

    public int TotalPendingCount => _pending.TotalCount;
    public string ApplyButtonText => TotalPendingCount > 0 ? $"Применить {TotalPendingCount}" : "Применить";
    public bool CanApply => TotalPendingCount > 0;

    private DebloatItem? ResolveDebloat(string id)
    {
        var c = _debloat.GetCurated().FirstOrDefault(x => x.Id == id);
        return c ?? DebloatVm?.GetItemInfo(id);
    }

    public List<PendingTweakPreview> PendingPreview
    {
        get
        {
            var r = new List<PendingTweakPreview>();
            foreach (var kv in _pending.GetAll())
            {
                if (kv.Key == TeamsKey) { r.Add(new PendingTweakPreview("AccountGroup","Автоустановка MS Teams","Изменяет реестр автоустановки Teams.", kv.Value ? "Включить" : "Отключить")); continue; }
                if (kv.Key.StartsWith("debloat:")) { var di = ResolveDebloat(kv.Key); if (di != null) r.Add(new PendingTweakPreview(di.IconKind, di.Name, di.Warning, "Удалить")); continue; }
                if (_all.TryGetValue(kv.Key, out var vm)) r.Add(new PendingTweakPreview(vm.Info.IconKind, vm.Info.Name, vm.Info.ImpactWarning, kv.Value ? "Включить" : "Отключить"));
            }
            return r;
        }
    }

    [RelayCommand] private void SelectCategory(CategoryInfo? c) { if (c != null) SelectedCategory = c; }
    [RelayCommand] private void ToggleExpand(TweakItemViewModel? i) { if (i != null) i.IsExpanded = !i.IsExpanded; }
    [RelayCommand] private void TogglePending(DebloatItemViewModel? i) { i?.TogglePending(); }
    [RelayCommand] private void OpenApplyOverlay() { if (TotalPendingCount == 0) return; OnPropertyChanged(nameof(PendingPreview)); ShowApplyOverlay = true; }
    [RelayCommand] private void CancelApply() { _pending.Clear(); ShowApplyOverlay = false; _ = RefreshAllAsync(); }

    [RelayCommand]
    private async Task ConfirmApplyAsync()
    {
        IsApplying = true; ShowApplyOverlay = false;
        var failures = new List<string>();
        try
        {
            foreach (var kv in _pending.GetAll().ToList())
            {
                if (kv.Key == TeamsKey) { _debloat.SetTeamsAutoInstall(kv.Value); continue; }
                if (kv.Key.StartsWith("debloat:"))
                {
                    var di = ResolveDebloat(kv.Key);
                    if (di == null) continue;
                    try
                    {
                        var res = di.SpecialHandler == "onedrive"
                            ? await _debloat.UninstallOneDriveAsync()
                            : await _debloat.UninstallUwpAsync(di.PackageFamilyName, di.UwpPackageName);
                        if (res.Success) await _debloat.CleanResidualsAsync(di.PackageFamilyName);
                        else failures.Add($"{di.Name}: {res.Message}");
                    }
                    catch (Exception ex) { Debug.WriteLine($"[Debloat] ex {kv.Key}: {ex.Message}"); }
                    continue;
                }
                var t = _engine.Get(kv.Key);
                if (t != null) try { await t.ApplyAsync(kv.Value); } catch (Exception ex) { Debug.WriteLine($"apply {kv.Key}: {ex.Message}"); }
            }
            _pending.Clear();
            await RefreshAllAsync();
            if (DebloatVm != null) DebloatVm = new DebloatViewModel(_debloat, _pending);
            if (failures.Count > 0)
                System.Windows.MessageBox.Show(
                    "Не удалось удалить некоторые приложения:\n\n" + string.Join("\n", failures) +
                    "\n\nВозможно, нужен запуск от имени администратора.",
                    "Evolution Tweaker", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        finally { IsApplying = false; }
    }
    private async Task RefreshAllAsync() { foreach (var vm in _all.Values) await vm.RefreshStateAsync(); }
}

public record CategoryInfo(string Id, string Name, TweakCategory Category);
public record PendingTweakPreview(string Icon, string Name, string Warning, string Action);