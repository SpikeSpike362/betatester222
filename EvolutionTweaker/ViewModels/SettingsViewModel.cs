using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolutionTweaker.Services;

namespace EvolutionTweaker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        _selectedLanguage = settings.Settings.Language;
    }

    public List<string> Languages { get; } = new() { "ru", "en", "uk", "zh" };

    [ObservableProperty] private string _selectedLanguage;

    partial void OnSelectedLanguageChanged(string value)
    {
        _settings.Settings.Language = value;
        _settings.Save();
    }
}