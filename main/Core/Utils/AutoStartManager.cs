using System;
using System.IO;
using Microsoft.Win32;

namespace GuvenlikDuvarim.Core.Utils
{
    public static class AutoStartManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "HaYTooL Firewall";

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetAutoStart(bool enable, bool startMinimized = false)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return false;

                if (enable)
                {
                    string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "HaYTooL Firewall.exe");
                    string runCommand = startMinimized ? $"\"{exePath}\" --tray" : $"\"{exePath}\"";
                    key.SetValue(AppName, runCommand);

                    // StartupApproved içinde devre dışı kalmışsa etkinleştir ve Koruma Kalkanı'na bildir
                    using (var appKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true))
                    {
                        byte[] enabledBytes = new byte[12];
                        enabledBytes[0] = 0x02;
                        appKey?.SetValue(AppName, enabledBytes, RegistryValueKind.Binary);
                    }

                    Guard.StartupGuardManager.RecordUserAuthorizedStartup(new Startup.StartupItemModel
                    {
                        UniqueKey = Startup.StartupManager.BuildRunUniqueKey(AppName, allUsers: false),
                        Name = AppName,
                        Command = runCommand,
                        CleanExePath = exePath,
                        IsEnabled = true
                    });
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                    Guard.StartupGuardManager.RecordUserRemovedStartup(Startup.StartupManager.BuildRunUniqueKey(AppName, allUsers: false));
                }
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Log($"AutoStart ayarlama hatası: {ex.Message}");
                return false;
            }
        }
    }
}
