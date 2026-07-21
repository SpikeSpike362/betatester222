namespace EvolutionTweaker.Models;

public enum DebloatSubcategory { Gaming, Advertising, Cloud, News, Telemetry, Other }

public class DebloatItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string IconKind { get; set; } = "Delete";
    public string Description { get; set; } = "";
    public string Warning { get; set; } = "";
    public DebloatSubcategory Subcategory { get; set; }
    public string? UwpPackageName { get; set; }
    public string? PackageFamilyName { get; set; }
    public string? InstallLocation { get; set; }
    public string? SpecialHandler { get; set; }
    public bool RequiresReboot { get; set; }
    public bool IsCurated { get; set; }
}