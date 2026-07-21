namespace EvolutionTweaker.Models;

public enum TweakCategory
{
    Basic,
    Security,
    Customization,
    NvidiaPanel,
    Power,
    Debloat,
    Cleaning,
    Privacy,
    Tweaks
}

public class TweakInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconKind { get; set; } = "Cog";
    public string ShortDescription { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string ImpactWarning { get; set; } = string.Empty;
    public TweakCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool OptimalStateIsEnabled { get; set; }
}