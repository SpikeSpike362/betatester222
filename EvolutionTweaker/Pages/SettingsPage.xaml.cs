// Pages/SettingsPage.xaml.cs
using System.Windows.Controls;
using EvolutionTweaker.ViewModels;

namespace EvolutionTweaker.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.GetService<SettingsViewModel>();
    }
}