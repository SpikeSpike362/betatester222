using System;
using System.Security.Principal;

namespace EvolutionTweaker.Models;

public static class SystemInfo
{
    public static string UserName => Environment.UserName;
    public static string MachineName => Environment.MachineName;
    public static string OsVersion => Environment.OSVersion.ToString();

    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}