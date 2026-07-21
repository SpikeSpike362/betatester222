using System;
using System.IO;
using System.Text.Json;

namespace EvolutionTweaker.Services;

public class AppSettings
{
    public string Language { get; set; } = "ru";
    public string Theme { get; set; } = "dark";
    public bool NotificationsEnabled { get; set; } = true;
    public DateTime? FirstRunDate { get; set; }
    public string? BackupPath { get; set; }
}

public class SettingsService
{
    private static readonly string _appDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EvolutionTweaker");

    private static readonly string SettingsFilePath = Path.Combine(_appDataFolder, "settings.json");
    private AppSettings _settings;

    public SettingsService()
    {
        _settings = Load();
    }

    public AppSettings Settings => _settings;
    public string AppDataFolder => _appDataFolder;

    private AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) return new AppSettings();
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_appDataFolder);
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}"); }
    }
}