using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EvolutionTweaker.Services;

public class FirstRunService
{
    private readonly SettingsService _settings;

    public FirstRunService(SettingsService settings)
    {
        _settings = settings;
    }

    public bool IsFirstRun()
    {
        return _settings.Settings.FirstRunDate == null;
    }

    public async Task CreateInitialBackupAsync()
    {
        var backupFolder = Path.Combine(_settings.AppDataFolder, "Backups", "Initial");
        Directory.CreateDirectory(backupFolder);

        var backupPath = Path.Combine(backupFolder, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        // Тут собираем важные ключи реестра перед любыми изменениями.
        // Пока заглушка — потом расширим списком ключей, которые реально трогаем.
        var backup = new
        {
            CreatedAt = DateTime.UtcNow,
            Type = "InitialBackup",
            MachineName = Environment.MachineName,
            Keys = new object[]
            {
                // Placeholder — сюда потом добавим реальные значения ключей
                new { Path = "HKCU\\Control Panel\\Mouse", Note = "Initial state captured" }
            }
        };

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(backupPath, json);

        _settings.Settings.FirstRunDate = DateTime.UtcNow;
        _settings.Settings.BackupPath = backupPath;
        _settings.Save();
    }
}