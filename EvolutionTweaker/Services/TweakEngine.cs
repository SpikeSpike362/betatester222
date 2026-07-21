using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EvolutionTweaker.Models;
using Microsoft.Win32;

namespace EvolutionTweaker.Services;

public interface ITweak
{
    TweakInfo Info { get; }
    Task<bool> GetCurrentStateAsync();
    Task ApplyAsync(bool enable);
}

public class TweakEngine
{
    private readonly List<ITweak> _tweaks = new();

    public TweakEngine()
    {
        RegisterTweak(new MouseAccelerationTweak());
        RegisterTweak(new HagsTweak());
        RegisterTweak(new GameDvrTweak());
        RegisterTweak(new NagleAlgorithmTweak());
    }

    private void RegisterTweak(ITweak tweak) => _tweaks.Add(tweak);

    public IReadOnlyList<ITweak> GetAll() => _tweaks;

    public IReadOnlyList<ITweak> GetByCategory(TweakCategory category)
        => _tweaks.Where(t => t.Info.Category == category).ToList();

    public ITweak? Get(string id) => _tweaks.FirstOrDefault(t => t.Info.Id == id);

    public IReadOnlyList<TweakInfo> GetAllInfos() => _tweaks.Select(t => t.Info).ToList();
}

// ========= ПРИМЕРЫ ТВИКОВ =========

public class MouseAccelerationTweak : ITweak
{
    public TweakInfo Info { get; } = new TweakInfo
    {
        Id = "mouse_acceleration",
        IconKind = "Mouse",
        Name = "Акселерация мыши",
        ShortDescription = "Убирает программное ускорение курсора.",
        DetailedDescription = "Изменяет три параметра MouseThreshold1, MouseThreshold2 и MouseSpeed в ключе HKCU\\Control Panel\\Mouse.",
        Recommendation = "Отключайте для любых соревновательных игр.",
        ImpactWarning = "Курсор мыши будет двигаться линейно 1-к-1. Может быть непривычно.",
        Category = TweakCategory.Basic,
        CategoryName = "Базовое",
        OptimalStateIsEnabled = false
    };

    private const string KeyPath = @"Control Panel\Mouse";
    private static readonly string[] ValueNames = { "MouseThreshold1", "MouseThreshold2", "MouseSpeed" };

    public Task<bool> GetCurrentStateAsync()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (key == null) return Task.FromResult(true);

        foreach (var name in ValueNames)
        {
            var val = key.GetValue(name)?.ToString();
            if (val != "0") return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task ApplyAsync(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);

        if (enable)
        {
            key.SetValue("MouseThreshold1", "6", RegistryValueKind.String);
            key.SetValue("MouseThreshold2", "10", RegistryValueKind.String);
            key.SetValue("MouseSpeed", "10", RegistryValueKind.String);
        }
        else
        {
            foreach (var n in ValueNames)
                key.SetValue(n, "0", RegistryValueKind.String);
        }
        return Task.CompletedTask;
    }
}

public class HagsTweak : ITweak
{
    public TweakInfo Info { get; } = new TweakInfo
    {
        Id = "hags",
        IconKind = "Monitor",
        Name = "Hardware-Accelerated GPU Scheduling (HAGS)",
        ShortDescription = "Аппаратное планирование GPU для снижения задержки.",
        DetailedDescription = "Изменяет параметр HwSchMode в HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers. 2 = включено, 1 = выключено.",
        Recommendation = "Включайте для снижения задержки в играх. Требуется перезагрузка.",
        ImpactWarning = "Может вызвать нестабильность на некоторых старых драйверах. Требуется перезагрузка системы.",
        Category = TweakCategory.Basic,
        CategoryName = "Базовое",
        OptimalStateIsEnabled = true
    };

    private const string KeyPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string ValueName = "HwSchMode";

    public Task<bool> GetCurrentStateAsync()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            if (key == null) return Task.FromResult(false);
            var val = key.GetValue(ValueName);
            if (val == null) return Task.FromResult(false);
            // Поддерживаем и DWORD и string
            if (val is int i) return Task.FromResult(i == 2);
            if (int.TryParse(val.ToString(), out var parsed)) return Task.FromResult(parsed == 2);
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task ApplyAsync(bool enable)
    {
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                        ?? Registry.LocalMachine.CreateSubKey(KeyPath, writable: true);
        key.SetValue(ValueName, enable ? 2 : 1, RegistryValueKind.DWord);
        return Task.CompletedTask;
    }
}

public class GameDvrTweak : ITweak
{
    public TweakInfo Info { get; } = new TweakInfo
    {
        Id = "game_dvr",
        IconKind = "VideoOff",
        Name = "Отключение Game DVR (Xbox Game Bar)",
        ShortDescription = "Отключает запись игр и Xbox Game Bar.",
        DetailedDescription = "Изменяет параметры GameDVR_Enabled и AppCaptureEnabled в реестре.",
        Recommendation = "Отключайте, если не используете Xbox Game Bar.",
        ImpactWarning = "Xbox Game Bar и запись клипов перестанут работать.",
        Category = TweakCategory.Basic,
        CategoryName = "Базовое",
        OptimalStateIsEnabled = false
    };

    public Task<bool> GetCurrentStateAsync()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore");
        var val = key?.GetValue("GameDVR_Enabled");
        return Task.FromResult(val is int i && i == 1);
    }

    public Task ApplyAsync(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore", writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore", writable: true);
        key.SetValue("GameDVR_Enabled", enable ? 1 : 0, RegistryValueKind.DWord);
        return Task.CompletedTask;
    }
}

public class NagleAlgorithmTweak : ITweak
{
    public TweakInfo Info { get; } = new TweakInfo
    {
        Id = "nagle",
        IconKind = "Network",
        Name = "Отключение алгоритма Нейгла",
        ShortDescription = "Снижает задержку в онлайн-играх.",
        DetailedDescription = "Отключает алгоритм Нейгла для сетевых адаптеров, что уменьшает задержку пакетов.",
        Recommendation = "Включайте для онлайн-игр.",
        ImpactWarning = "Может незначительно увеличить нагрузку на сеть.",
        Category = TweakCategory.Basic,
        CategoryName = "Базовое",
        OptimalStateIsEnabled = true
    };

    public Task<bool> GetCurrentStateAsync() => Task.FromResult(false); // заглушка
    public Task ApplyAsync(bool enable) => Task.CompletedTask; // заглушка
}