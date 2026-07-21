using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolutionTweaker.Helpers;

namespace EvolutionTweaker.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _versionStatus = "актуальная 1.0 Beta";
    [ObservableProperty] private string _latestNewsDate = "31.01.2026";
    [ObservableProperty] private string _latestNewsTitle = "Developer HOTFIX";

    public string[] NewsItems { get; } = new[]
    {
        "Тест. ыыыыыыыы 6767676767",
        "я пыль я пыль",
        "квадробика угу"
    };

    public string CurrentTip { get; } =
        "На странице\n" +
        "Применить.";

    public ToolCard[] QuickTools { get; } = new[]
    {
        new ToolCard("🎮", "Тест", "PC Latency Test"),
        new ToolCard("🛒", "Store", "Магазин приложений"),
        new ToolCard("🌐", "Internet", "Тест интернета"),
        new ToolCard("🎯", "Apps", "Оптимизация программ")
    };

    public event Action<string>? NavigationRequested;

    public DashboardViewModel()
    {
        UserName = Models.SystemInfo.UserName;
    }

    public ICommand NavigateToOptimizationCommand => new RelayCommand(() =>
    {
        NavigationRequested?.Invoke("optimization");
    });

    public ICommand NavigateToToolsCommand => new RelayCommand(() =>
    {
        NavigationRequested?.Invoke("tools");
    });
}

public record ToolCard(string Icon, string Title, string Subtitle);