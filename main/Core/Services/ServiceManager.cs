using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace GuvenlikDuvarim.Core.Services
{
    public class ServiceItemModel
    {
        public string ServiceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = "Durduruldu";
        public bool IsRunning => Status == "Çalışıyor";
        public string StatusColor => IsRunning ? "#10B981" : "#EF4444";
        public string StatusBackground => IsRunning ? "#2210B981" : "#22EF4444";
        
        public string StartupType { get; set; } = "Manuel";
        public string StartupTypeColor => StartupType switch
        {
            "Otomatik" => "#06B6D4",
            "Devre Dışı" => "#9CA3AF",
            _ => "#F59E0B"
        };

        public int ProcessId { get; set; } = 0;
        public string ImagePath { get; set; } = string.Empty;
        public string CleanExePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsMicrosoftService { get; set; } = false;
    }

    public static class ServiceManager
    {
        #region Win32 SCM P/Invoke
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint dwAccess);

        [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", EntryPoint = "EnumServicesStatusExW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EnumServicesStatusEx(
            IntPtr hSCManager,
            int infoLevel,
            uint dwServiceType,
            uint dwServiceState,
            IntPtr lpServices,
            uint cbBufSize,
            out uint pcbBytesNeeded,
            out uint lpServicesReturned,
            ref uint lpResumeHandle,
            string? pszGroupName);

        private const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
        private const int SC_ENUM_PROCESS_INFO = 0;
        private const uint SERVICE_WIN32 = 0x00000030; // Own process & Share process
        private const uint SERVICE_STATE_ALL = 0x00000003;

        private const int SERVICE_STOPPED = 1;
        private const int SERVICE_START_PENDING = 2;
        private const int SERVICE_STOP_PENDING = 3;
        private const int SERVICE_RUNNING = 4;
        private const int SERVICE_CONTINUE_PENDING = 5;
        private const int SERVICE_PAUSE_PENDING = 6;
        private const int SERVICE_PAUSED = 7;

        [StructLayout(LayoutKind.Sequential)]
        private struct ENUM_SERVICE_STATUS_PROCESS
        {
            public IntPtr lpServiceName;
            public IntPtr lpDisplayName;
            public SERVICE_STATUS_PROCESS ServiceStatusProcess;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_STATUS_PROCESS
        {
            public int dwServiceType;
            public int dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
            public int dwProcessId;
            public int dwServiceFlags;
        }
        #endregion

        public static List<ServiceItemModel> GetAllServices()
        {
            var list = new List<ServiceItemModel>();

            try
            {
                // 1. Win32 SCM üzerinden tüm servislerin canlı durumunu ve PID'sini al
                var liveStatusDict = GetLiveServicesFromScm();

                using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
                if (servicesKey == null) return list;

                foreach (string subKeyName in servicesKey.GetSubKeyNames())
                {
                    try
                    {
                        using var serviceKey = servicesKey.OpenSubKey(subKeyName);
                        if (serviceKey == null) continue;

                        object? typeObj = serviceKey.GetValue("Type");
                        if (typeObj is int typeVal)
                        {
                            // 1 = Kernel Driver, 2 = File System Driver (Sürücüleri filtrele)
                            if (typeVal == 1 || typeVal == 2) continue;
                        }

                        string imagePath = serviceKey.GetValue("ImagePath")?.ToString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(imagePath)) continue;

                        string displayName = serviceKey.GetValue("DisplayName")?.ToString() ?? subKeyName;
                        string description = serviceKey.GetValue("Description")?.ToString() ?? string.Empty;

                        if (description.StartsWith("@")) description = string.Empty;
                        if (displayName.StartsWith("@")) displayName = subKeyName;

                        int startTypeVal = (serviceKey.GetValue("Start") as int?) ?? 3;
                        string startupType = startTypeVal switch
                        {
                            2 => "Otomatik",
                            3 => "Manuel",
                            4 => "Devre Dışı",
                            _ => "Manuel"
                        };

                        string status = "Durduruldu";
                        int pid = 0;
                        if (liveStatusDict.TryGetValue(subKeyName, out var info))
                        {
                            status = info.Status;
                            pid = info.Pid;
                        }

                        string cleanExe = ExtractCleanExePath(imagePath);
                        bool isMs = cleanExe.Contains(@"\Windows\", StringComparison.OrdinalIgnoreCase) ||
                                    cleanExe.Contains(@"\System32\", StringComparison.OrdinalIgnoreCase) ||
                                    displayName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                    displayName.Contains("Windows", StringComparison.OrdinalIgnoreCase);

                        list.Add(new ServiceItemModel
                        {
                            ServiceName = subKeyName,
                            DisplayName = displayName,
                            Status = status,
                            StartupType = startupType,
                            ProcessId = pid,
                            ImagePath = imagePath,
                            CleanExePath = cleanExe,
                            Description = description,
                            IsMicrosoftService = isMs
                        });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"Hizmetler taranırken hata: {ex}");
            }

            return list.OrderBy(s => s.DisplayName).ToList();
        }

        private static Dictionary<string, (string Status, int Pid)> GetLiveServicesFromScm()
        {
            var dict = new Dictionary<string, (string Status, int Pid)>(StringComparer.OrdinalIgnoreCase);

            IntPtr hSCM = OpenSCManager(null, null, SC_MANAGER_ENUMERATE_SERVICE);
            if (hSCM == IntPtr.Zero) return dict;

            try
            {
                uint resume = 0;
                int structSize = Marshal.SizeOf(typeof(ENUM_SERVICE_STATUS_PROCESS));

                while (true)
                {
                    uint bytesNeeded = 0;
                    uint count = 0;

                    EnumServicesStatusEx(hSCM, SC_ENUM_PROCESS_INFO, SERVICE_WIN32, SERVICE_STATE_ALL, IntPtr.Zero, 0, out bytesNeeded, out count, ref resume, null);
                    if (bytesNeeded == 0) break;

                    uint allocSize = bytesNeeded + 8192;
                    IntPtr buffer = Marshal.AllocHGlobal((int)allocSize);
                    try
                    {
                        bool ok = EnumServicesStatusEx(hSCM, SC_ENUM_PROCESS_INFO, SERVICE_WIN32, SERVICE_STATE_ALL, buffer, allocSize, out bytesNeeded, out count, ref resume, null);
                        int err = Marshal.GetLastWin32Error();

                        for (int i = 0; i < count; i++)
                        {
                            IntPtr ptr = IntPtr.Add(buffer, i * structSize);
                            var item = Marshal.PtrToStructure<ENUM_SERVICE_STATUS_PROCESS>(ptr);
                            string? sName = Marshal.PtrToStringUni(item.lpServiceName);
                            if (string.IsNullOrEmpty(sName)) continue;

                            int state = item.ServiceStatusProcess.dwCurrentState;
                            int pid = item.ServiceStatusProcess.dwProcessId;

                            string statusStr = state switch
                            {
                                SERVICE_RUNNING => "Çalışıyor",
                                SERVICE_PAUSED => "Duraklatıldı",
                                SERVICE_START_PENDING => "Başlatılıyor...",
                                SERVICE_STOP_PENDING => "Durduruluyor...",
                                _ => "Durduruldu"
                            };

                            dict[sName] = (statusStr, pid);
                        }

                        if (ok || err != 234) break; // 234 = ERROR_MORE_DATA
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"Win32 SCM servis tarama hatası: {ex}");
            }
            finally
            {
                CloseServiceHandle(hSCM);
            }

            return dict;
        }

        private static string ExtractCleanExePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return string.Empty;
            string clean = rawPath.Trim();

            if (clean.StartsWith("\""))
            {
                int endQuote = clean.IndexOf('\"', 1);
                if (endQuote > 1) clean = clean.Substring(1, endQuote - 1);
            }
            else
            {
                int exeIdx = clean.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (exeIdx > 0)
                {
                    clean = clean.Substring(0, exeIdx + 4);
                }
            }

            clean = Environment.ExpandEnvironmentVariables(clean).Trim();
            return clean;
        }

        public static bool StartService(string serviceName)
        {
            return ExecuteScCommand($"start \"{serviceName}\"");
        }

        public static bool StopService(string serviceName)
        {
            return ExecuteScCommand($"stop \"{serviceName}\"");
        }

        public static bool RestartService(string serviceName)
        {
            StopService(serviceName);
            System.Threading.Thread.Sleep(1000);
            return StartService(serviceName);
        }

        public static bool SetStartupType(string serviceName, string type)
        {
            string scType = type.ToLowerInvariant() switch
            {
                "otomatik" or "auto" => "auto",
                "manuel" or "demand" => "demand",
                "devre dışı" or "disabled" => "disabled",
                _ => "demand"
            };
            return ExecuteScCommand($"config \"{serviceName}\" start= {scType}");
        }

        public static bool DisableService(string serviceName)
        {
            return SetStartupType(serviceName, "disabled");
        }

        public static bool DeleteService(string serviceName)
        {
            return ExecuteScCommand($"delete \"{serviceName}\"");
        }

        private static bool ExecuteScCommand(string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(5000);
                return proc?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"sc.exe komut hatası ({args}): {ex}");
                return false;
            }
        }
    }
}
