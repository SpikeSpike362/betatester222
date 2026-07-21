using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolutionTweaker.Models;
using EvolutionTweaker.Services;

namespace EvolutionTweaker.ViewModels;

public partial class DebloatViewModel : ObservableObject
{
    private readonly DebloatEngine _engine;
    private readonly PendingTweaksService _pending;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _hasNoInstalledItems;
    [ObservableProperty] private List<DebloatGroup> _groups = new();
    [ObservableProperty] private DebloatItemViewModel? _oneDriveItem;
    [ObservableProperty] private TeamsAutoInstallViewModel _teamsVm = null!;

    public DebloatViewModel(DebloatEngine engine, PendingTweaksService pending)
    {
        _engine = engine;
        _pending = pending;
        _teamsVm = new TeamsAutoInstallViewModel(engine, pending);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        var installed = await _engine.GetAllInstalledUwpAsync();

        var byName = new Dictionary<string, InstalledUwp>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in installed) byName[u.Name] = u;
        InstalledUwp? Find(string pkg) =>
            byName.TryGetValue(pkg, out var e) ? e :
            installed.FirstOrDefault(u => u.Name.IndexOf(pkg, StringComparison.OrdinalIgnoreCase) >= 0);

        var curated = new List<DebloatItemViewModel>();
        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in _engine.GetCurated())
        {
            if (string.IsNullOrEmpty(item.UwpPackageName)) continue;
            var m = Find(item.UwpPackageName);
            if (m == null) continue;
            item.PackageFamilyName = m.PackageFamilyName;
            item.InstallLocation = m.InstallLocation;
            curated.Add(new DebloatItemViewModel(item, _pending));
            matched.Add(m.Name);
        }

        // OneDrive — карточка ВСЕГДА; факт установки кладём в IsInstalled
        var odInstalled = _engine.IsOneDriveInstalled();
        var od = new DebloatItem
        {
            Id = "debloat:onedrive", Name = "OneDrive", IconKind = "Cloud",
            Description = "Облачное хранилище Microsoft. Синхронизирует файлы и встроено в проводник.",
            Warning = "Файлы в облаке останутся, синхронизация прекратится. Папка на диске не удалится.",
            Subcategory = DebloatSubcategory.Other, SpecialHandler = "onedrive",
            RequiresReboot = true, IsCurated = true
        };
        OneDriveItem = new DebloatItemViewModel(od, _pending) { IsInstalled = odInstalled };

        var others = new List<DebloatItemViewModel>();
        foreach (var u in installed)
        {
            if (matched.Contains(u.Name)) continue;
            if (_engine.IsBlocked(u.Name)) continue;
            if (Guid.TryParse(u.Name, out _)) continue;
            var item = new DebloatItem
            {
                Id = "debloat:other:" + u.PackageFamilyName, Name = u.Name, IconKind = "Puzzle",
                Description = "Приложение не из нашего списка. Удаляйте только если уверены.",
                Warning = "Опасно: назначение неизвестно. Может оказаться нужным системе.",
                Subcategory = DebloatSubcategory.Other, UwpPackageName = u.Name,
                PackageFamilyName = u.PackageFamilyName, InstallLocation = u.InstallLocation, IsCurated = false
            };
            others.Add(new DebloatItemViewModel(item, _pending, _engine));
        }

        Groups = BuildGroups(curated, others);
        HasNoInstalledItems = curated.Count == 0 && others.Count == 0;
        IsLoading = false;
    }

    private static List<DebloatGroup> BuildGroups(List<DebloatItemViewModel> curated, List<DebloatItemViewModel> others)
    {
        var r = new List<DebloatGroup>();
        (DebloatSubcategory, string)[] g =
        {
            (DebloatSubcategory.Gaming, "🎮 Игровые сервисы"),
            (DebloatSubcategory.Advertising, "📢 Реклама и Bloatware"),
            (DebloatSubcategory.Cloud, "☁️ Облако и коммуникации"),
            (DebloatSubcategory.News, "📰 Новости и развлечения"),
            (DebloatSubcategory.Telemetry, "🔧 Телеметрия")
        };
        foreach (var (sub, title) in g)
        {
            var x = curated.Where(v => v.Info.Subcategory == sub).ToList();
            if (x.Count > 0) r.Add(new DebloatGroup(title, x, false, false));
        }
        if (others.Count > 0) r.Add(new DebloatGroup("📦 Другие", others, false, true));
        return r;
    }

    public DebloatItem? GetItemInfo(string id)
    {
        if (OneDriveItem != null && OneDriveItem.Info.Id == id) return OneDriveItem.Info;
        foreach (var grp in Groups)
        {
            var f = grp.Items.FirstOrDefault(x => x.Info.Id == id);
            if (f != null) return f.Info;
        }
        return null;
    }
}

public record DebloatGroup(string Title, List<DebloatItemViewModel> Items, bool IsOneDrive, bool IsOther);