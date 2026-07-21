using System.Windows.Controls;
using EvolutionTweaker.ViewModels;

namespace EvolutionTweaker.Pages;

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        DataContext = App.GetService<DashboardViewModel>();
    }
}