using System.Windows.Controls;
using EvolutionTweaker.ViewModels;

namespace EvolutionTweaker.Pages;

public partial class OptimizationPage : Page
{
    public OptimizationPage()
    {
        InitializeComponent();
        DataContext = App.GetService<OptimizationViewModel>();
    }
}