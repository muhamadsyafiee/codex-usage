using Microsoft.Win32;
using System.Reflection;

namespace CodexUsage;

public static class StartupService
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "CodexUsageWidget";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string command && !string.IsNullOrWhiteSpace(command);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executable = Environment.ProcessPath ?? Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrWhiteSpace(executable)) throw new InvalidOperationException("Application path is unavailable.");
        key.SetValue(ValueName, $"\"{executable}\"");
    }
}
