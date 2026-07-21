using CommunityToolkit.Mvvm.ComponentModel;

namespace EvolutionTweaker.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    public ToolItem[] Tools { get; } = new[]
    {
        new ToolItem("Store", "Магазин приложений", "🛒", true),
        new ToolItem("LatencyTest", "PC Latency Test", "📊", true),
        new ToolItem("InternetTest", "Тест интернета", "🌐", true),
        new ToolItem("AppsOptimization", "Apps Optimization", "🎯", true),
    };
}

public record ToolItem(string Id, string Name, string Icon, bool IsStub);