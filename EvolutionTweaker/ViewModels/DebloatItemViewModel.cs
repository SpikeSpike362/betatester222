using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolutionTweaker.Models;
using EvolutionTweaker.Services;

namespace EvolutionTweaker.ViewModels;

public partial class DebloatItemViewModel : ObservableObject
{
    private readonly DebloatItem _item;
    private readonly PendingTweaksService _pending;

    [ObservableProperty] private bool _isPendingUninstall;
    [ObservableProperty] private bool _isInstalled = true;
    [ObservableProperty] private string _toggleLabel = "Удалить";
    [ObservableProperty] private bool _canToggle = true;
    [ObservableProperty] private bool _toggleIsDanger;

    [ObservableProperty] private BitmapImage? _realIcon;
    [ObservableProperty] private bool _hasRealIcon;
    [ObservableProperty] private string _sizeText = "…";
    [ObservableProperty] private bool _hasSizeText;
    [ObservableProperty] private string? _displayName;
    [ObservableProperty] private bool _hasDisplayName;

    public DebloatItem Info => _item;

    public DebloatItemViewModel(DebloatItem item, PendingTweaksService pending, DebloatEngine? engine = null)
    {
        _item = item;
        _pending = pending;
        IsPendingUninstall = _pending.IsPending(_item.Id, out _);
        _pending.Changed += () => IsPendingUninstall = _pending.IsPending(_item.Id, out _);
        if (!_item.IsCurated && engine != null) _ = LoadDetailsAsync(engine);
        RefreshButton();
    }

    public void TogglePending()
    {
        if (!CanToggle) return;
        if (IsPendingUninstall) _pending.RemovePending(_item.Id);
        else _pending.SetPending(_item.Id, true);
    }

    partial void OnIsInstalledChanged(bool value) => RefreshButton();
    partial void OnIsPendingUninstallChanged(bool value) => RefreshButton();

    private void RefreshButton()
    {
        if (!IsInstalled)            { ToggleLabel = "Удалено"; CanToggle = false; ToggleIsDanger = true; }
        else if (IsPendingUninstall) { ToggleLabel = "Отмена";  CanToggle = true;  ToggleIsDanger = true; }
        else                         { ToggleLabel = "Удалить"; CanToggle = true;  ToggleIsDanger = false; }
    }

    private async Task LoadDetailsAsync(DebloatEngine engine)
    {
        var t = await Task.Run(() => (
            icon: engine.TryLoadIcon(_item.InstallLocation),
            size: engine.TryGetSize(_item.InstallLocation),
            name: engine.TryGetDisplayName(_item.InstallLocation, _item.UwpPackageName ?? "", _item.PackageFamilyName ?? "")));
        if (t.icon != null) { RealIcon = t.icon; HasRealIcon = true; }
        SizeText = t.size; HasSizeText = true;
        if (!string.IsNullOrWhiteSpace(t.name)) { DisplayName = t.name; HasDisplayName = true; }
    }
}