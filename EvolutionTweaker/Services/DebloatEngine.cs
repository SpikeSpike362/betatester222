using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using EvolutionTweaker.Models;
using Microsoft.Win32;

namespace EvolutionTweaker.Services;

public class DebloatEngine
{
    private readonly List<DebloatItem> _curated = new();

    private static readonly HashSet<string> BlockedExact = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.WindowsStore","Microsoft.StorePurchaseApp","Microsoft.DesktopAppInstaller",
        "Microsoft.WebMediaExtensions","Microsoft.HEVCVideoExtension","Microsoft.VP9VideoExtensions",
        "Microsoft.AV1VideoExtension","Microsoft.MPEG2VideoExtension","Microsoft.WebpImageExtension",
        "Microsoft.RawImageExtension","Microsoft.HEIFImageExtension","Microsoft.AVCEncoderVideoExtension",
        "Microsoft.Windows.CloudExperienceHost","Microsoft.Windows.ShellExperienceHost",
        "Microsoft.Windows.StartMenuExperienceHost","Microsoft.Windows.SecureAssessmentBrowser",
        "Microsoft.Windows.PeopleExperienceHost","Microsoft.Windows.AugLoop.CBS",
        "Microsoft.LockApp","Microsoft.AAD.BrokerPlugin","Microsoft.AccountsControl",
        "Microsoft.BioEnrollment","Microsoft.CredDialogHost","Microsoft.ECApp",
        "Microsoft.Windows.AssignedAccessLockApp","Microsoft.Windows.ParentalControls",
        "Microsoft.Windows.OOBENetworkCaptivePortal","Microsoft.Windows.OOBENetworkConnectionFlow",
        "Microsoft.Windows.PinningConfirmationDialog","Microsoft.Windows.XGpuEjectDialog",
        "Microsoft.Windows.NarratorQuickStart","Microsoft.Windows.CapturePicker",
        "Microsoft.Windows.PrintQueueActionCenter","Microsoft.Windows.Apprep.ChxApp",
        "Microsoft.XboxGameCallableUI","Microsoft.Xbox.TCUI","Microsoft.XboxIdentityProvider",
        "Microsoft.XboxSpeechToTextOverlay","Microsoft.GamingApp",
        "windows.immersivecontrolpanel","Windows.CBSPreview","Windows.PrintDialog"
    };
    private static readonly string[] BlockedPrefixes =
    {
        "Microsoft.VCLibs","Microsoft.NET.Native","Microsoft.UI.Xaml",
        "Microsoft.WindowsAppRuntime","MicrosoftWindows.",
        "Microsoft.AIFabric","Microsoft.ApplicationCompatibility","Microsoft.AsyncTextService"
    };

    public DebloatEngine() => RegisterCurated();

    private void Add(string id, string n, string i, string d, DebloatSubcategory s, string p, string w = "")
        => _curated.Add(new DebloatItem { Id = id, Name = n, IconKind = i, Description = d, Warning = w, Subcategory = s, UwpPackageName = p, IsCurated = true });

    private void RegisterCurated()
    {
        Add("debloat:xbox_win10","Xbox (Windows 10)","Xbox","Старое приложение Xbox для Windows 10.",DebloatSubcategory.Gaming,"Microsoft.XboxApp","Некоторые игры из Store могут перестать работать.");
        Add("debloat:xbox_win11","Xbox (Gaming App)","Xbox","Новое приложение Xbox для Windows 11.",DebloatSubcategory.Gaming,"Microsoft.GamingApp","Некоторые игры из Store могут перестать работать.");
        Add("debloat:xbox_gamebar","Xbox Game Bar","Controller","Оверлей для записи игр и стриминга (Win+G).",DebloatSubcategory.Gaming,"Microsoft.XboxGamingOverlay");
        Add("debloat:mixedreality","Mixed Reality Portal","VirtualReality","Портал смешанной реальности (VR).",DebloatSubcategory.Gaming,"Microsoft.MixedReality.Portal");
        Add("debloat:candycrush","Candy Crush Saga","Candy","Предустановленная игра.",DebloatSubcategory.Advertising,"king.com.CandyCrushSaga");
        Add("debloat:tiktok","TikTok","MusicNote","Предустановленное приложение TikTok.",DebloatSubcategory.Advertising,"BytedancePte.Ltd.TikTok");
        Add("debloat:disney","Disney+","Movie","Предустановленное приложение Disney+.",DebloatSubcategory.Advertising,"Disney.37853FC22B2CE");
        Add("debloat:solitaire","Microsoft Solitaire Collection","CardsPlayingOutline","Коллекция карточных игр с рекламой.",DebloatSubcategory.Advertising,"Microsoft.MicrosoftSolitaireCollection","Пасьянсы Windows станут недоступны.");
        Add("debloat:officehub","Office (Hub)","MicrosoftOffice","Ярлык-хаб Microsoft Office (не сам Office).",DebloatSubcategory.Advertising,"Microsoft.MicrosoftOfficeHub");
        Add("debloat:3dbuilder","3D Builder","Cube","Редактор 3D-моделей Microsoft.",DebloatSubcategory.Advertising,"Microsoft.3DBuilder");
        Add("debloat:print3d","Print 3D","Printer3d","Подготовка моделей для 3D-печати.",DebloatSubcategory.Advertising,"Microsoft.Print3D");
        Add("debloat:clipchamp","Clipchamp","Video","Предустановленный видеоредактор Microsoft.",DebloatSubcategory.Advertising,"Clipchamp.Clipchamp");
        Add("debloat:paint","Paint (UWP)","Brush","UWP-версия Paint. Если пользуешься — не удаляй.",DebloatSubcategory.Advertising,"Microsoft.Paint");
        Add("debloat:soundrecorder","Диктофон","MicrophoneVariant","Приложение «Диктофон» Windows.",DebloatSubcategory.Advertising,"Microsoft.WindowsSoundRecorder");
        Add("debloat:screensketch","Ножницы (Snipping Tool)","ContentCut","Инструмент для скриншотов.",DebloatSubcategory.Advertising,"Microsoft.ScreenSketch");
        Add("debloat:zunemusic","Музыка Groove","Music","Старое приложение для музыки.",DebloatSubcategory.Advertising,"Microsoft.ZuneMusic");
        Add("debloat:zunevideo","Кино и ТВ","Television","Приложение для фильмов Microsoft.",DebloatSubcategory.Advertising,"Microsoft.ZuneVideo");
        Add("debloat:skype","Skype","Phone","Классический мессенджер Microsoft.",DebloatSubcategory.Cloud,"Microsoft.SkypeApp");
        Add("debloat:bingsearch","Bing Search","Magnify","Поиск Bing, встроенный в систему.",DebloatSubcategory.Telemetry,"Microsoft.BingSearch");
        Add("debloat:stickynotes","Microsoft Sticky Notes","NoteOutline","Приложение заметок. Если не пользуетесь — можно удалить.",DebloatSubcategory.Advertising,"Microsoft.MicrosoftStickyNotes");
        Add("debloat:cortana","Cortana","Microphone","Голосовой помощник (почти не используется).",DebloatSubcategory.Cloud,"Microsoft.549981C3F5F10");
        Add("debloat:people","Люди (People)","AccountMultiple","Контакты Microsoft (редко нужны).",DebloatSubcategory.Cloud,"Microsoft.People");
        Add("debloat:oneconnect","Платные Wi-Fi сети","Wifi","Поиск платных хотспотов.",DebloatSubcategory.Cloud,"Microsoft.OneConnect");
        Add("debloat:outlooknew","Outlook (new)","Email","Новый Outlook for Windows.",DebloatSubcategory.Cloud,"Microsoft.OutlookForWindows");
        Add("debloat:news","Новости","Newspaper","Приложение Microsoft News.",DebloatSubcategory.News,"Microsoft.BingNews");
        Add("debloat:weather","Погода","WeatherPartlyCloudy","Приложение Microsoft Weather.",DebloatSubcategory.News,"Microsoft.BingWeather");
        Add("debloat:maps","Карты","Map","Приложение Microsoft Maps.",DebloatSubcategory.News,"Microsoft.WindowsMaps");
        Add("debloat:phonelink","Связь с телефоном","CellphoneLink","Синхронизация с Android-телефоном.",DebloatSubcategory.News,"Microsoft.YourPhone");
        Add("debloat:sports","Спорт","Basketball","Приложение MSN Спорт.",DebloatSubcategory.News,"Microsoft.BingSports");
        Add("debloat:finance","Финансы","Cash","Приложение MSN Финансы.",DebloatSubcategory.News,"Microsoft.BingFinance");
        Add("debloat:getstarted","Советы (Get Started)","Lightbulb","Приложение «Советы» с рекламой функций.",DebloatSubcategory.News,"Microsoft.Getstarted");
        Add("debloat:feedback","Feedback Hub","MessageAlert","Центр отправки отзывов в Microsoft.",DebloatSubcategory.Telemetry,"Microsoft.WindowsFeedbackHub");
        Add("debloat:gethelp","Get Help","Lifebuoy","Справочное приложение Microsoft.",DebloatSubcategory.Telemetry,"Microsoft.GetHelp");
        Add("debloat:quickassist","Quick Assist","Headset","Удалённая помощь Microsoft.",DebloatSubcategory.Telemetry,"MicrosoftCorporationII.QuickAssist");
        Add("debloat:devhome","Dev Home","CodeBraces","Центр разработчика Windows.",DebloatSubcategory.Telemetry,"Microsoft.Windows.DevHome");
        Add("debloat:contentdelivery","Content Delivery Manager","Broadcast","Реклама в меню Пуск + автоустановка Candy Crush/TikTok.",DebloatSubcategory.Telemetry,"Microsoft.Windows.ContentDeliveryManager","Реклама из Пуска исчезнет навсегда. ОЧЕНЬ полезно!");
    }

    public IReadOnlyList<DebloatItem> GetCurated() => _curated;

    public bool IsBlocked(string name)
    {
        if (BlockedExact.Contains(name)) return true;
        foreach (var p in BlockedPrefixes)
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // ---------- батч: один запрос на все пакеты ----------
    public async Task<List<InstalledUwp>> GetAllInstalledUwpAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"Get-AppxPackage -AllUsers | Select-Object Name,PackageFamilyName,InstallLocation | ConvertTo-Json -Compress -Depth 3\"",
                    RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return new List<InstalledUwp>();
                var json = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                if (string.IsNullOrWhiteSpace(json)) return new List<InstalledUwp>();
                var list = new List<InstalledUwp>();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    foreach (var el in doc.RootElement.EnumerateArray()) AddOne(el, list);
                else AddOne(doc.RootElement, list);
                return list;
            }
            catch (Exception ex) { Debug.WriteLine($"[Debloat] batch: {ex.Message}"); return new List<InstalledUwp>(); }
        });
    }
    private static void AddOne(JsonElement el, List<InstalledUwp> list)
    {
        var n = el.TryGetProperty("Name", out var a) ? a.GetString() ?? "" : "";
        var p = el.TryGetProperty("PackageFamilyName", out var b) ? b.GetString() ?? "" : "";
        var l = el.TryGetProperty("InstallLocation", out var c) ? c.GetString() ?? "" : "";
        if (!string.IsNullOrEmpty(n)) list.Add(new InstalledUwp(n, p, l));
    }

    // ---------- реальная иконка из манифеста ----------
    public BitmapImage? TryLoadIcon(string? installLocation)
    {
        if (string.IsNullOrEmpty(installLocation)) return null;
        try
        {
            var logo = ReadLogoRelative(installLocation);
            if (logo == null) return null;
            var resolved = ResolveLogoFile(installLocation, logo);
            if (resolved == null) return null;
            var bytes = File.ReadAllBytes(resolved);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(bytes);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 40;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }
    private static string? ReadLogoRelative(string installLocation)
    {
        var manifest = Path.Combine(installLocation, "AppxManifest.xml");
        if (!File.Exists(manifest)) return null;
        var xdoc = XDocument.Load(manifest);
        var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        string? logo = null;
        foreach (var ve in xdoc.Descendants(ns + "VisualElements"))
        {
            logo = ve.Attribute("Square44x44Logo")?.Value ?? ve.Attribute("Logo")?.Value;
            if (!string.IsNullOrEmpty(logo)) break;
        }
        if (string.IsNullOrEmpty(logo)) logo = xdoc.Descendants(ns + "Logo").FirstOrDefault()?.Value;
        return string.IsNullOrEmpty(logo) ? null : logo;
    }
    private static string? ResolveLogoFile(string installLocation, string relativeLogo)
    {
        var exact = Path.Combine(installLocation, relativeLogo.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(exact)) return exact;
        var dir = Path.GetDirectoryName(exact);
        var baseName = Path.GetFileNameWithoutExtension(exact);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(baseName)) return null;
        try
        {
            return Directory.EnumerateFiles(dir, baseName + "*")
                .OrderBy(f => f.IndexOf("altform", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0)
                .FirstOrDefault();
        }
        catch { return null; }
    }

    // ---------- читаемое имя (DisplayName) ----------
    public string? TryGetDisplayName(string? installLocation, string packageName, string packageFamilyName)
    {
        if (string.IsNullOrEmpty(installLocation)) return null;
        try
        {
            var manifest = Path.Combine(installLocation, "AppxManifest.xml");
            if (!File.Exists(manifest)) return null;
            var xdoc = XDocument.Load(manifest);
            var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            var raw = xdoc.Descendants(ns + "DisplayName").FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (!raw.Contains("ms-resource", StringComparison.OrdinalIgnoreCase)) return raw;

            var key = raw.Split(':', 2).Last().TrimStart('/', ':').Trim();
            string[] fmts =
            {
                $"ms-resource://{packageName}/Resources/{key}",
                $"ms-resource://{packageName}/{key}",
                $"@{{{packageFamilyName}}}?ms-resource://{packageName}/Resources/{key}",
                $"@{{{packageFamilyName}}}?ms-resource://{packageName}/{key}"
            };
            foreach (var f in fmts)
            {
                var sb = new StringBuilder(512);
                if (SHLoadIndirectString(f, sb, sb.Capacity, IntPtr.Zero) == 0)
                {
                    var s = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(s) && !s.StartsWith("ms-resource", StringComparison.OrdinalIgnoreCase) && s != f)
                        return s;
                }
            }
            return null;
        }
        catch { return null; }
    }
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

    // ---------- размер папки ----------
    public string TryGetSize(string? installLocation)
    {
        if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation)) return "—";
        try
        {
            long b = 0;
            foreach (var f in Directory.EnumerateFiles(installLocation, "*", SearchOption.AllDirectories))
                try { b += new FileInfo(f).Length; } catch { }
            return b <= 0 ? "—" : (b / 1048576.0).ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("ru-RU")) + " МБ";
        }
        catch { return "—"; }
    }

    // ---------- чистка остатков ----------
    public async Task CleanResidualsAsync(string? pfn)
    {
        if (string.IsNullOrEmpty(pfn)) return;
        await Task.Run(() =>
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", pfn);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch (Exception ex) { Debug.WriteLine($"[Debloat] clean: {ex.Message}"); }
        });
    }

    // ---------- удаление UWP ----------
    public async Task<UninstallResult> UninstallUwpAsync(string? pfn, string? name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return new UninstallResult(false, "Не указано имя пакета");

            // Попытка 1: для всех пользователей (нужен админ)
            var (code, _, err) = await RunPowerShellAsync(
                $"-NoProfile -Command \"[Console]::OutputEncoding=[Text.Encoding]::UTF8; Get-AppxPackage -Name '{name}' -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction Stop\"");
            if (code == 0) return new UninstallResult(true, "Удалено успешно");

            // Попытка 2: только текущий пользователь
            var (c2, _, e2) = await RunPowerShellAsync(
                $"-NoProfile -Command \"[Console]::OutputEncoding=[Text.Encoding]::UTF8; Get-AppxPackage -Name '{name}' | Remove-AppxPackage -ErrorAction Stop\"");
            if (c2 == 0) return new UninstallResult(true, "Удалено успешно");

            var msg = string.IsNullOrWhiteSpace(err) ? e2 : err;
            return new UninstallResult(false, string.IsNullOrWhiteSpace(msg) ? $"код выхода {code}/{c2}" : msg.Trim());
        }
        catch (Exception ex) { return new UninstallResult(false, ex.Message); }
    }

    private static async Task<(int code, string stdout, string stderr)> RunPowerShellAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("PowerShell не запустился");
        var outTask = proc.StandardOutput.ReadToEndAsync();
        var errTask = proc.StandardError.ReadToEndAsync();
        await Task.WhenAll(outTask, errTask);   // читаем ОБА потока до ожидания — иначе deadlock
        await proc.WaitForExitAsync();
        return (proc.ExitCode, outTask.Result, errTask.Result);
    }

    // ---------- OneDrive ----------
    public bool IsOneDriveInstalled()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidates = new[]
        {
            Path.Combine(local, "Microsoft\\OneDrive\\OneDrive.exe"),
            Path.Combine(pf, "Microsoft OneDrive\\OneDrive.exe"),
            Path.Combine(pfx86, "Microsoft OneDrive\\OneDrive.exe")
        };
        return candidates.Any(File.Exists);
    }
public async Task<UninstallResult> UninstallOneDriveAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("OneDrive")) try { p.Kill(); } catch { }
                var setup = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "OneDriveSetup.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OneDriveSetup.exe")
                }.FirstOrDefault(File.Exists);
                if (setup == null) return new UninstallResult(false, "OneDriveSetup.exe не найден");
                Process.Start(new ProcessStartInfo(setup, "/uninstall") { UseShellExecute = true, Verb = "runas" });
                return new UninstallResult(true, "Удаление запущено. Требуется перезагрузка.");
            }
            catch (Exception ex) { return new UninstallResult(false, ex.Message); }
        });
    }    

    // ---------- автоустановка Teams (реестр) ----------
    private const string TeamsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Communications";
    private const string TeamsValue = "ConfigureChatAutoInstall";
    public bool GetTeamsAutoInstallEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(TeamsKey);
            var v = key?.GetValue(TeamsValue);
            if (v == null) return true;
            if (v is int i) return i != 0;
            return true;
        }
        catch { return true; }
    }
    public void SetTeamsAutoInstall(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(TeamsKey, true) ?? Registry.LocalMachine.CreateSubKey(TeamsKey, true);
            if (enable) try { key.DeleteValue(TeamsValue, false); } catch { }
            else key.SetValue(TeamsValue, 0, RegistryValueKind.DWord);
        }
        catch (Exception ex) { Debug.WriteLine($"[Debloat] teams: {ex.Message}"); }
    }
}

public record InstalledUwp(string Name, string PackageFamilyName, string InstallLocation);
public record UninstallResult(bool Success, string Message);