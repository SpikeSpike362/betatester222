using System.Windows;
using EvolutionTweaker.Models;
using EvolutionTweaker.Services;
using EvolutionTweaker.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EvolutionTweaker;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;
    public static T GetService<T>() where T : class => Host.Services.GetRequiredService<T>();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!SystemInfo.IsAdministrator())
        {
            var result = MessageBox.Show(
                "Evolution Tweaker требует прав администратора для применения твиков.\nПерезапустить от имени администратора?",
                "Требуются права администратора",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var psi = new System.Diagnostics.ProcessStartInfo(exe)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                try { System.Diagnostics.Process.Start(psi); }
                catch { MessageBox.Show("Не удалось получить права администратора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            Shutdown();
            return;
        }

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<SettingsService>();
                services.AddSingleton<FirstRunService>();
                services.AddSingleton<TweakEngine>();
                services.AddSingleton<DebloatEngine>();
                services.AddSingleton<PendingTweaksService>();

                services.AddSingleton<ViewModels.MainViewModel>();
                services.AddTransient<ViewModels.DashboardViewModel>();
                services.AddTransient<ViewModels.OptimizationViewModel>();
                services.AddTransient<ViewModels.ToolsViewModel>();
                services.AddTransient<ViewModels.SettingsViewModel>();
            })
            .Build();

        await Host.StartAsync();

        var firstRun = GetService<FirstRunService>();
        if (firstRun.IsFirstRun())
        {
            await firstRun.CreateInitialBackupAsync();
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (Host != null) await Host.StopAsync();
        base.OnExit(e);
    }
}