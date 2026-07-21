using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EvolutionTweaker.Services;
using EvolutionTweaker.ViewModels;

namespace EvolutionTweaker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.GetService<MainViewModel>();

        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.ActiveSection))
                    UpdateCurrentPage(vm.ActiveSection);
            };
            UpdateCurrentPage(vm.ActiveSection);
        }
    }

    private void UpdateCurrentPage(string section)
    {
        Page? page = section switch
        {
            "dashboard" => new Pages.DashboardPage(),
            "optimization" => new Pages.OptimizationPage(),
            "tools" => new Pages.ToolsPage(),
            "settings" => new Pages.SettingsPage(),
            _ => new Pages.DashboardPage()
        };

        MainFrame.Content = page;

        if (page?.DataContext is DashboardViewModel dashVm)
        {
            dashVm.NavigationRequested += s =>
            {
                if (DataContext is MainViewModel mainVm)
                    mainVm.NavigateCommand.Execute(s);
            };
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var menu = new ContextMenu
        {
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            HasDropShadow = true
        };

        // Создаём конвертер пакета FamFamFam
        var flagConverter = new FamFamFam.Flags.Wpf.CountryIdToFlagImageSourceConverter();

        // Массив: (название, код_страны_для_флага, код_языка_для_настроек)
        var languages = new (string Name, string CountryCode, string LangCode)[]
        {
            ("Русский", "ru", "ru"),
            ("English", "gb", "en"),
            ("Українська", "ua", "uk"),
            ("中文", "cn", "zh")
        };

        var settings = App.GetService<SettingsService>();

        foreach (var (name, countryCode, langCode) in languages)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            // Image с Binding через конвертер FamFamFam
            var flagImage = new Image
            {
                Width = 18,
                Height = 12,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform,
                DataContext = countryCode  // DataContext = код страны
            };

            // Binding с конвертером
            var binding = new System.Windows.Data.Binding
            {
                Converter = flagConverter,
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            flagImage.SetBinding(Image.SourceProperty, binding);

            stack.Children.Add(flagImage);

            // Название языка + галочка если текущий
            var displayText = settings.Settings.Language == langCode ? $"✓ {name}" : name;
            var textBlock = new TextBlock
            {
                Text = displayText,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = settings.Settings.Language == langCode
                    ? FontWeights.SemiBold
                    : FontWeights.Normal
            };
            stack.Children.Add(textBlock);

            // Создаём сам MenuItem
            var item = new MenuItem
            {
                Header = stack,
                Tag = langCode,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Padding = new Thickness(12, 8, 24, 8)
            };

            // Hover эффект
            item.MouseEnter += (s, args) =>
            {
                if (s is MenuItem mi)
                    mi.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            };
            item.MouseLeave += (s, args) =>
            {
                if (s is MenuItem mi)
                    mi.Background = Brushes.Transparent;
            };

            item.Click += (s, args) =>
            {
                if (s is MenuItem mi && mi.Tag is string code)
                {
                    settings.Settings.Language = code;
                    settings.Save();
                    MessageBox.Show(
                        "Язык будет изменён после перезапуска приложения.",
                        "Смена языка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    menu.IsOpen = false;
                }
            };

            menu.Items.Add(item);
        }

        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }
}