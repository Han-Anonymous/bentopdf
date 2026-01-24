using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace PDFKawankasi.Services;

/// <summary>
/// Manages Windows startup registration for PDF Kawankasi
/// </summary>
public static class StartupManager
{
    private const string AppName = "PDFKawankasi";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    
    /// <summary>
    /// Checks if the application is set to run at Windows startup
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            if (key == null) return false;
            
            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Enables or disables the application from running at Windows startup
    /// </summary>
    public static bool SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null) return false;
            
            if (enabled)
            {
                // Get the application executable path
                var exePath = Assembly.GetExecutingAssembly().Location;
                
                // For .NET 8+, the location points to .dll, we need .exe in the same directory
                if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    var directory = Path.GetDirectoryName(exePath);
                    var exeName = Path.GetFileNameWithoutExtension(exePath) + ".exe";
                    exePath = Path.Combine(directory ?? "", exeName);
                }
                
                // Verify the .exe exists
                if (!File.Exists(exePath))
                {
                    return false;
                }
                
                // Add --minimized argument to start minimized
                var startupCommand = $"\"{exePath}\" --minimized";
                
                key.SetValue(AppName, startupCommand);
            }
            else
            {
                // Remove the startup entry
                key.DeleteValue(AppName, false);
            }
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Toggles the Windows startup setting
    /// </summary>
    public static bool ToggleStartup()
    {
        var currentState = IsStartupEnabled();
        return SetStartupEnabled(!currentState);
    }
}
