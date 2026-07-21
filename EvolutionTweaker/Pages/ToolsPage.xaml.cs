// Pages/ToolsPage.xaml.cs
using System.Windows.Controls;
using EvolutionTweaker.ViewModels;

namespace EvolutionTweaker.Pages;

public partial class ToolsPage : Page
{
    public ToolsPage()
    {
        InitializeComponent();
        DataContext = App.GetService<ToolsViewModel>();
    }
}