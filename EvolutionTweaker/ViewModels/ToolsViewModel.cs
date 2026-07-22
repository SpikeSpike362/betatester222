using CommunityToolkit.Mvvm.ComponentModel;

namespace EvolutionTweaker.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    public ToolItem[] Tools { get; } = new[]
    {
        new ToolItem("Store", "Магазин приложений", "Cart", true),
        new ToolItem("LatencyTest", "PC Latency Test", "Timer", true),
        new ToolItem("InternetTest", "Тест интернета", "Web", true),
        new ToolItem("AppsOptimization", "Apps Optimization", "Target", true),
    };
}

public record ToolItem(string Id, string Name, string Icon, bool IsStub);