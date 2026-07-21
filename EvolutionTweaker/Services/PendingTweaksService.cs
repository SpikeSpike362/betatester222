using System.Collections.Generic;
using System.Linq;
using EvolutionTweaker.Models;

namespace EvolutionTweaker.Services;

/// <summary>
/// Хранит очередь твиков, которые ждут применения.
/// Сбрасывается при закрытии программы.
/// </summary>
public class PendingTweaksService
{
    // Key = TweakId, Value = desired state (true = включить, false = отключить)
    private readonly Dictionary<string, bool> _pending = new();

    public event System.Action? Changed;

    public int TotalCount => _pending.Count;

    public int CountForCategory(TweakCategory category, IReadOnlyList<TweakInfo> allTweaks)
    {
        return allTweaks
            .Where(t => t.Category == category && _pending.ContainsKey(t.Id))
            .Count();
    }

    public bool IsPending(string tweakId, out bool desiredState)
    {
        return _pending.TryGetValue(tweakId, out desiredState);
    }

    public void SetPending(string tweakId, bool desiredState)
    {
        _pending[tweakId] = desiredState;
        Changed?.Invoke();
    }

    public void RemovePending(string tweakId)
    {
        _pending.Remove(tweakId);
        Changed?.Invoke();
    }

    public void Clear()
    {
        _pending.Clear();
        Changed?.Invoke();
    }

    public IReadOnlyDictionary<string, bool> GetAll() => _pending;
}