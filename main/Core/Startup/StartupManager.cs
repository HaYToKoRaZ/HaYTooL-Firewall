using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Win32;

namespace GuvenlikDuvarim.Core.Startup
{
    public enum StartupSourceKind
    {
        RegistryRun,       // Windows Explorer\StartupApproved ile yönetilen HKCU/HKLM Run anahtarları
        RegistryOther,     // RunOnce ve Policies\Explorer\Run (*dis yedek anahtarı ile yönetilir)
        StartupFolder,     // Başlangıç klasörleri (Explorer\StartupApproved\StartupFolder ile yönetilir)
        UwpApp,            // Modern UWP StartupTask (SystemAppData\...\State ile yönetilir)
        TaskScheduler      // Görev Zamanlayıcı (Schedule.Service COM ile yönetilir)
    }

    public class StartupItemModel : INotifyPropertyChanged
    {
        public string UniqueKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string CleanExePath { get; set; } = string.Empty;
        public string CategoryGroup { get; set; } = StartupManager.GroupCurrentUser;
        public string LocationDetail { get; set; } = string.Empty;

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusBackground));
                }
            }
        }

        public string StatusText => IsEnabled ? "🟢 Etkin" : "⚪ Devre Dışı";
        public string StatusColor => IsEnabled ? "#10B981" : "#9CA3AF";
        public string StatusBackground => IsEnabled ? "#2210B981" : "#229CA3AF";

        public StartupSourceKind SourceKind { get; set; } = StartupSourceKind.RegistryRun;
        public bool IsLocalMachine { get; set; } = false;
        public string ActiveRegistryKeyPath { get; set; } = string.Empty;
        public string DisabledRegistryKeyPath { get; set; } = string.Empty;
        public string StartupApprovedKeyPath { get; set; } = string.Empty;
        public string StartupApprovedValueName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public bool IsRegistry => SourceKind == StartupSourceKind.RegistryRun || SourceKind == StartupSourceKind.RegistryOther;
        public bool IsTaskScheduler => SourceKind == StartupSourceKind.TaskScheduler;
        public bool IsUwp => SourceKind == StartupSourceKind.UwpApp;

        public string UwpPackageFamily { get; set; } = string.Empty;
        public string UwpTaskSubKey { get; set; } = string.Empty;
        public string TaskFullPath { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public static class StartupManager
    {
        private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunOncePath = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string RunWowPath = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
        private const string RunOnceWowPath = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string PoliciesRunPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run";

        private const string ApprovedRunPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string ApprovedRun32Path = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
        private const string ApprovedFolderPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

        private const string UwpRepoPackagesPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";
        private const string UwpSystemAppDataPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData";

        private const string LegacyDisabledIniSection = "HaYTooL_Disabled_Startup";
        private static bool _legacyMigrated = false;

        public const string GroupAllUsers = "All Users - Registry Entries";
        public const string GroupCurrentUser = "Current User - Registry Entries";
        public const string GroupUwp = "UWP App";
        public const string GroupTaskScheduler = "Task Scheduler";

        private static readonly HashSet<string> IgnoredUwpSubKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "PersistedStorageItemTable", "PSR", "Schemas", "SplashScreen", "ApplicationFrame", "HAM"
        };

        public static List<StartupItemModel> GetAllStartupItems(bool includeTaskScheduler = true)
        {
            if (!_legacyMigrated)
            {
                _legacyMigrated = true;
                MigrateLegacyIniDisabledEntries();
            }

            var list = new List<StartupItemModel>();

            // 1. All Users - Registry Entries & Common Startup Folder
            ScanRegistryRunWithApproved(true, RunPath, ApprovedRunPath, GroupAllUsers, @"HKLM\Run", list);
            ScanRegistryRunWithApproved(true, RunWowPath, ApprovedRun32Path, GroupAllUsers, @"HKLM (32-bit)\Run", list);
            ScanRegistryOtherPair(true, RunOncePath, RunOncePath + "dis", GroupAllUsers, @"HKLM\RunOnce", list);
            ScanRegistryOtherPair(true, RunOnceWowPath, RunOnceWowPath + "dis", GroupAllUsers, @"HKLM (32-bit)\RunOnce", list);
            ScanRegistryOtherPair(true, PoliciesRunPath, PoliciesRunPath + "dis", GroupAllUsers, @"HKLM\Policies\Run", list);
            try
            {
                string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                ScanFolderStartup(true, commonStartup, ApprovedFolderPath, GroupAllUsers, "Ortak Başlangıç Klasörü", list);
            }
            catch { }

            // 2. Current User - Registry Entries & User Startup Folder
            ScanRegistryRunWithApproved(false, RunPath, ApprovedRunPath, GroupCurrentUser, @"HKCU\Run", list);
            ScanRegistryRunWithApproved(false, RunWowPath, ApprovedRun32Path, GroupCurrentUser, @"HKCU (32-bit)\Run", list);
            ScanRegistryOtherPair(false, RunOncePath, RunOncePath + "dis", GroupCurrentUser, @"HKCU\RunOnce", list);
            ScanRegistryOtherPair(false, RunOnceWowPath, RunOnceWowPath + "dis", GroupCurrentUser, @"HKCU (32-bit)\RunOnce", list);
            ScanRegistryOtherPair(false, PoliciesRunPath, PoliciesRunPath + "dis", GroupCurrentUser, @"HKCU\Policies\Run", list);
            try
            {
                string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                ScanFolderStartup(false, userStartup, ApprovedFolderPath, GroupCurrentUser, "Kullanıcı Başlangıç Klasörü", list);
            }
            catch { }

            // 3. UWP Apps (AppxManifest windows.startupTask + SystemAppData State)
            ScanUwpStartupApps(list);

            // 4. Task Scheduler
            if (includeTaskScheduler)
            {
                ScanTaskSchedulerTasks(list);
            }

            return list.OrderBy(x => x.CategoryGroup).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        #region Tarama Motorları (Scan Engines)

        private static RegistryKey GetRootHive(bool isLocalMachine) =>
            isLocalMachine ? Registry.LocalMachine : Registry.CurrentUser;

        private static bool IsStartupApprovedEnabled(bool isLocalMachine, string approvedKeyPath, string valueName)
        {
            try
            {
                using var key = GetRootHive(isLocalMachine).OpenSubKey(approvedKeyPath, false);
                if (key == null) return true;

                if (key.GetValue(valueName) is byte[] bytes && bytes.Length > 0)
                {
                    // Windows StartupApproved standardı:
                    // Çift sayılar (0x00, 0x02, 0x06) => Etkin (Enabled)
                    // Tek sayılar (0x01, 0x03, 0x07) => Devre Dışı (Disabled)
                    return (bytes[0] & 1) == 0;
                }
            }
            catch { }
            return true;
        }

        private static bool SetStartupApprovedState(bool isLocalMachine, string approvedKeyPath, string valueName, bool enable)
        {
            try
            {
                using var key = GetRootHive(isLocalMachine).CreateSubKey(approvedKeyPath, true);
                if (key == null) return false;

                byte[] data = new byte[12];
                if (enable)
                {
                    data[0] = 0x02; // Enabled
                }
                else
                {
                    data[0] = 0x03; // Disabled by user
                    byte[] ft = BitConverter.GetBytes(DateTime.UtcNow.ToFileTimeUtc());
                    Buffer.BlockCopy(ft, 0, data, 4, 8);
                }

                key.SetValue(valueName, data, RegistryValueKind.Binary);
                return true;
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"StartupApproved güncellenemedi ({approvedKeyPath}\\{valueName}): {ex.Message}");
                return false;
            }
        }

        private static void DeleteStartupApprovedEntry(bool isLocalMachine, string approvedKeyPath, string valueName)
        {
            try
            {
                using var key = GetRootHive(isLocalMachine).OpenSubKey(approvedKeyPath, true);
                key?.DeleteValue(valueName, false);
            }
            catch { }
        }

        private static void ScanRegistryRunWithApproved(
            bool isLocalMachine,
            string regKeyPath,
            string approvedKeyPath,
            string group,
            string detail,
            List<StartupItemModel> list)
        {
            try
            {
                using var key = GetRootHive(isLocalMachine).OpenSubKey(regKeyPath, false);
                if (key == null) return;

                foreach (string valName in key.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valName)) continue;
                    string cmd = key.GetValue(valName)?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(cmd)) continue;

                    bool isEnabled = IsStartupApprovedEnabled(isLocalMachine, approvedKeyPath, valName);

                    list.Add(new StartupItemModel
                    {
                        UniqueKey = $"REG|{isLocalMachine}|{regKeyPath.ToLowerInvariant()}|{valName.ToLowerInvariant()}",
                        Name = valName,
                        Command = cmd,
                        CleanExePath = ExtractCleanExe(cmd),
                        CategoryGroup = group,
                        LocationDetail = detail,
                        SourceKind = StartupSourceKind.RegistryRun,
                        IsLocalMachine = isLocalMachine,
                        ActiveRegistryKeyPath = regKeyPath,
                        StartupApprovedKeyPath = approvedKeyPath,
                        StartupApprovedValueName = valName,
                        IsEnabled = isEnabled
                    });
                }
            }
            catch { }
        }

        private static void ScanRegistryOtherPair(
            bool isLocalMachine,
            string activeKeyPath,
            string disabledKeyPath,
            string group,
            string detail,
            List<StartupItemModel> list)
        {
            ScanSingleOtherKey(isLocalMachine, activeKeyPath, activeKeyPath, disabledKeyPath, group, detail, isEnabled: true, list);
            ScanSingleOtherKey(isLocalMachine, disabledKeyPath, activeKeyPath, disabledKeyPath, group, detail, isEnabled: false, list);
        }

        private static void ScanSingleOtherKey(
            bool isLocalMachine,
            string currentPath,
            string activeKeyPath,
            string disabledKeyPath,
            string group,
            string detail,
            bool isEnabled,
            List<StartupItemModel> list)
        {
            try
            {
                using var key = GetRootHive(isLocalMachine).OpenSubKey(currentPath, false);
                if (key == null) return;

                foreach (string valName in key.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(valName)) continue;
                    string cmd = key.GetValue(valName)?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(cmd)) continue;

                    string uniqueKey = $"REG|{isLocalMachine}|{activeKeyPath.ToLowerInvariant()}|{valName.ToLowerInvariant()}";
                    if (list.Any(x => x.UniqueKey == uniqueKey)) continue;

                    list.Add(new StartupItemModel
                    {
                        UniqueKey = uniqueKey,
                        Name = valName,
                        Command = cmd,
                        CleanExePath = ExtractCleanExe(cmd),
                        CategoryGroup = group,
                        LocationDetail = detail,
                        SourceKind = StartupSourceKind.RegistryOther,
                        IsLocalMachine = isLocalMachine,
                        ActiveRegistryKeyPath = activeKeyPath,
                        DisabledRegistryKeyPath = disabledKeyPath,
                        IsEnabled = isEnabled
                    });
                }
            }
            catch { }
        }

        private static void ScanFolderStartup(
            bool isLocalMachine,
            string folderPath,
            string approvedKeyPath,
            string group,
            string detail,
            List<StartupItemModel> list)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return;

            try
            {
                foreach (string file in Directory.EnumerateFiles(folderPath))
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;

                    bool isLegacyDisabledFile = fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                    string baseFileName = isLegacyDisabledFile
                        ? fileName.Substring(0, fileName.Length - ".disabled".Length)
                        : fileName;

                    string ext = Path.GetExtension(baseFileName).ToLowerInvariant();
                    if (ext != ".lnk" && ext != ".exe" && ext != ".bat" && ext != ".cmd" && ext != ".vbs" && ext != ".url" && !isLegacyDisabledFile)
                    {
                        continue;
                    }

                    string displayName = Path.GetFileNameWithoutExtension(baseFileName);
                    bool isEnabled = !isLegacyDisabledFile && IsStartupApprovedEnabled(isLocalMachine, approvedKeyPath, baseFileName);

                    string targetExe = string.Empty;
                    string commandDisplay = file;
                    if (!isLegacyDisabledFile && ext == ".lnk")
                    {
                        string resolved = ResolveShortcutTarget(file);
                        if (!string.IsNullOrWhiteSpace(resolved))
                        {
                            targetExe = resolved;
                            commandDisplay = resolved;
                        }
                    }
                    else if (!isLegacyDisabledFile)
                    {
                        targetExe = file;
                    }

                    list.Add(new StartupItemModel
                    {
                        UniqueKey = $"FOLDER|{isLocalMachine}|{baseFileName.ToLowerInvariant()}",
                        Name = displayName,
                        Command = commandDisplay,
                        CleanExePath = targetExe,
                        FilePath = file,
                        CategoryGroup = group,
                        LocationDetail = detail,
                        SourceKind = StartupSourceKind.StartupFolder,
                        IsLocalMachine = isLocalMachine,
                        StartupApprovedKeyPath = approvedKeyPath,
                        StartupApprovedValueName = baseFileName,
                        IsEnabled = isEnabled
                    });
                }
            }
            catch { }
        }

        private static void ScanUwpStartupApps(List<StartupItemModel> list)
        {
            var handledFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Aşama 1: AppxManifest.xml üzerinden resmi windows.startupTask uzantılarını tara
            try
            {
                using var repoKey = Registry.CurrentUser.OpenSubKey(UwpRepoPackagesPath, false);
                if (repoKey != null)
                {
                    foreach (string pkgFullName in repoKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var pkgKey = repoKey.OpenSubKey(pkgFullName, false);
                            string? rootFolder = pkgKey?.GetValue("PackageRootFolder")?.ToString();
                            if (string.IsNullOrWhiteSpace(rootFolder)) continue;

                            string manifestPath = Path.Combine(rootFolder, "AppxManifest.xml");
                            if (!File.Exists(manifestPath)) continue;

                            string xmlText = File.ReadAllText(manifestPath, Encoding.UTF8);
                            if (!xmlText.Contains("windows.startupTask", StringComparison.OrdinalIgnoreCase) &&
                                !xmlText.Contains("StartupTask", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            string pkgFamily = ConvertPackageFullNameToFamilyName(pkgFullName);
                            if (string.IsNullOrWhiteSpace(pkgFamily)) continue;

                            var doc = XDocument.Parse(xmlText);
                            string pkgDisplayName = doc.Root?
                                .Elements().FirstOrDefault(e => e.Name.LocalName == "Properties")?
                                .Elements().FirstOrDefault(e => e.Name.LocalName == "DisplayName")?.Value ?? string.Empty;

                            var applications = doc.Descendants().Where(e => e.Name.LocalName == "Application");
                            bool foundAnyInPkg = false;

                            foreach (var appElem in applications)
                            {
                                string appExeRel = appElem.Attribute("Executable")?.Value ?? string.Empty;
                                string fullExePath = !string.IsNullOrWhiteSpace(appExeRel)
                                    ? Path.Combine(rootFolder, appExeRel.Replace('/', '\\'))
                                    : string.Empty;

                                var startupTasks = appElem.Descendants().Where(e => e.Name.LocalName == "StartupTask");
                                foreach (var st in startupTasks)
                                {
                                    string taskId = st.Attribute("TaskId")?.Value ?? string.Empty;
                                    if (string.IsNullOrWhiteSpace(taskId)) continue;

                                    string taskDisp = st.Attribute("DisplayName")?.Value ?? string.Empty;
                                    bool defaultEnabled = !string.Equals(st.Attribute("Enabled")?.Value, "false", StringComparison.OrdinalIgnoreCase);

                                    bool isEnabled = ReadUwpStartupTaskState(pkgFamily, taskId, defaultEnabled);
                                    string friendlyName = ResolveUwpDisplayName(pkgFamily, taskDisp, pkgDisplayName);

                                    string uniqueKey = $"UWP|{pkgFamily.ToLowerInvariant()}|{taskId.ToLowerInvariant()}";
                                    if (list.Any(x => x.UniqueKey == uniqueKey)) continue;

                                    list.Add(new StartupItemModel
                                    {
                                        UniqueKey = uniqueKey,
                                        Name = friendlyName,
                                        Command = !string.IsNullOrWhiteSpace(fullExePath) ? fullExePath : $"{pkgFamily} [{taskId}]",
                                        CleanExePath = File.Exists(fullExePath) ? fullExePath : string.Empty,
                                        CategoryGroup = GroupUwp,
                                        LocationDetail = $"UWP ({pkgFamily})",
                                        SourceKind = StartupSourceKind.UwpApp,
                                        UwpPackageFamily = pkgFamily,
                                        UwpTaskSubKey = taskId,
                                        IsEnabled = isEnabled
                                    });
                                    foundAnyInPkg = true;
                                }
                            }

                            if (foundAnyInPkg)
                            {
                                handledFamilies.Add(pkgFamily);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // Aşama 2: Manifestosu okunamayan UWP paketleri için SystemAppData yedeği (State + UserEnabledStartupOnce kontrolü ile)
            try
            {
                using var baseKey = Registry.CurrentUser.OpenSubKey(UwpSystemAppDataPath, false);
                if (baseKey == null) return;

                foreach (string pkgFamily in baseKey.GetSubKeyNames())
                {
                    if (handledFamilies.Contains(pkgFamily)) continue;

                    try
                    {
                        using var pkgKey = baseKey.OpenSubKey(pkgFamily, false);
                        if (pkgKey == null) continue;

                        foreach (string sub in pkgKey.GetSubKeyNames())
                        {
                            if (IgnoredUwpSubKeys.Contains(sub)) continue;

                            using var subKey = pkgKey.OpenSubKey(sub, false);
                            if (subKey == null) continue;

                            object? stateObj = subKey.GetValue("State");
                            object? onceObj = subKey.GetValue("UserEnabledStartupOnce");
                            if (stateObj is int stateVal && onceObj != null)
                            {
                                string uniqueKey = $"UWP|{pkgFamily.ToLowerInvariant()}|{sub.ToLowerInvariant()}";
                                if (list.Any(x => x.UniqueKey == uniqueKey)) continue;

                                list.Add(new StartupItemModel
                                {
                                    UniqueKey = uniqueKey,
                                    Name = ResolveUwpDisplayName(pkgFamily, string.Empty, string.Empty),
                                    Command = $"{pkgFamily} [{sub}]",
                                    CleanExePath = string.Empty,
                                    CategoryGroup = GroupUwp,
                                    LocationDetail = $"UWP ({pkgFamily})",
                                    SourceKind = StartupSourceKind.UwpApp,
                                    UwpPackageFamily = pkgFamily,
                                    UwpTaskSubKey = sub,
                                    IsEnabled = (stateVal == 2 || stateVal == 4)
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static bool ReadUwpStartupTaskState(string pkgFamily, string taskId, bool defaultEnabled)
        {
            try
            {
                string keyPath = $@"{UwpSystemAppDataPath}\{pkgFamily}\{taskId}";
                using var key = Registry.CurrentUser.OpenSubKey(keyPath, false);
                if (key != null && key.GetValue("State") is int stateVal)
                {
                    // StartupTaskState: 0=Disabled, 1=DisabledByUser, 2=Enabled, 3=DisabledByPolicy, 4=EnabledByPolicy
                    return stateVal == 2 || stateVal == 4;
                }
            }
            catch { }
            return defaultEnabled;
        }

        private static string ConvertPackageFullNameToFamilyName(string pkgFullName)
        {
            // Örn: 5319275A.WhatsAppDesktop_2.2639.100.0_x64__cv1g1gvanyjgm -> 5319275A.WhatsAppDesktop_cv1g1gvanyjgm
            string[] parts = pkgFullName.Split('_');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}_{parts[^1]}";
            }
            return pkgFullName;
        }

        private static string ResolveUwpDisplayName(string pkgFamily, string taskDisplay, string pkgDisplay)
        {
            if (!string.IsNullOrWhiteSpace(taskDisplay) && !taskDisplay.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
            {
                return taskDisplay.Trim();
            }

            if (!string.IsNullOrWhiteSpace(pkgDisplay) && !pkgDisplay.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
            {
                return pkgDisplay.Trim();
            }

            string name = pkgFamily;
            int under = name.IndexOf('_');
            if (under > 0) name = name.Substring(0, under);
            int dot = name.LastIndexOf('.');
            if (dot >= 0 && dot < name.Length - 1) name = name.Substring(dot + 1);

            if (name.Equals("WhatsAppDesktop", StringComparison.OrdinalIgnoreCase)) return "WhatsApp";
            if (name.Equals("WindowsTerminal", StringComparison.OrdinalIgnoreCase)) return "Windows Terminal";

            return Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        }

        private static void ScanTaskSchedulerTasks(List<StartupItemModel> list)
        {
            try
            {
                Type? schedType = Type.GetTypeFromProgID("Schedule.Service");
                if (schedType != null)
                {
                    dynamic service = Activator.CreateInstance(schedType)!;
                    service.Connect();
                    dynamic rootFolder = service.GetFolder(@"\");
                    ScanTaskFolderRecursive(rootFolder, list);
                    return;
                }
            }
            catch
            {
                FallbackSchtasksQuery(list);
            }
        }

        private static void ScanTaskFolderRecursive(dynamic folder, List<StartupItemModel> list)
        {
            try
            {
                // 1 = TASK_ENUM_HIDDEN (Gizli başlangıç görevlerini de dahil et!)
                dynamic tasks = folder.GetTasks(1);
                foreach (dynamic task in tasks)
                {
                    try
                    {
                        string name = task.Name;
                        string path = task.Path;
                        bool enabled = task.Enabled;

                        // Dahili Microsoft sistem bakım görevlerini atla
                        if (path.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith(@"\SoftLanding\", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        dynamic definition = task.Definition;
                        string actionPath = string.Empty;
                        string actionArgs = string.Empty;

                        try
                        {
                            dynamic actions = definition.Actions;
                            foreach (dynamic act in actions)
                            {
                                if ((int)act.Type == 0) // TASK_ACTION_EXEC
                                {
                                    actionPath = act.Path?.ToString() ?? string.Empty;
                                    actionArgs = act.Arguments?.ToString() ?? string.Empty;
                                    break;
                                }
                            }
                        }
                        catch { }

                        if (string.IsNullOrWhiteSpace(actionPath)) continue;

                        bool isLogonOrBoot = false;
                        try
                        {
                            dynamic triggers = definition.Triggers;
                            foreach (dynamic trig in triggers)
                            {
                                int trigType = (int)trig.Type;
                                // 8=Boot, 9=Logon, 11=SessionStateChange, 7=Registration
                                if (trigType == 8 || trigType == 9 || trigType == 11 || trigType == 7)
                                {
                                    isLogonOrBoot = true;
                                    break;
                                }
                            }
                        }
                        catch { }

                        string fullCmd = string.IsNullOrWhiteSpace(actionArgs)
                            ? actionPath
                            : $"{actionPath} {actionArgs}";

                        string folderLabel = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "/";
                        string triggerTag = isLogonOrBoot ? "Açılış Görevi" : "Zamanlanmış Görev";

                        list.Add(new StartupItemModel
                        {
                            UniqueKey = $"TASK|{path.ToLowerInvariant()}",
                            Name = name,
                            Command = fullCmd,
                            CleanExePath = ExtractCleanExe(actionPath),
                            CategoryGroup = GroupTaskScheduler,
                            LocationDetail = $"{triggerTag} ({path})",
                            SourceKind = StartupSourceKind.TaskScheduler,
                            TaskFullPath = path,
                            IsEnabled = enabled
                        });
                    }
                    catch { }
                }

                dynamic subFolders = folder.GetFolders(0);
                foreach (dynamic sub in subFolders)
                {
                    string subPath = sub.Path;
                    if (subPath.Equals(@"\Microsoft", StringComparison.OrdinalIgnoreCase) ||
                        subPath.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase) ||
                        subPath.StartsWith(@"\SoftLanding", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    ScanTaskFolderRecursive(sub, list);
                }
            }
            catch { }
        }

        private static void FallbackSchtasksQuery(List<StartupItemModel> list)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/query /fo CSV /v",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return;

                proc.StandardOutput.ReadLine();
                while (!proc.StandardOutput.EndOfStream)
                {
                    string? line = proc.StandardOutput.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length > 8)
                    {
                        string taskPath = parts[0].Trim('\"');
                        if (string.IsNullOrWhiteSpace(taskPath) ||
                            taskPath.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string uniqueKey = $"TASK|{taskPath.ToLowerInvariant()}";
                        if (list.Any(x => x.UniqueKey == uniqueKey)) continue;

                        bool isDisabled = line.Contains("Disabled", StringComparison.OrdinalIgnoreCase) ||
                                          line.Contains("Devre", StringComparison.OrdinalIgnoreCase);

                        list.Add(new StartupItemModel
                        {
                            UniqueKey = uniqueKey,
                            Name = Path.GetFileName(taskPath),
                            Command = taskPath,
                            CleanExePath = string.Empty,
                            CategoryGroup = GroupTaskScheduler,
                            LocationDetail = $"Task ({taskPath})",
                            SourceKind = StartupSourceKind.TaskScheduler,
                            TaskFullPath = taskPath,
                            IsEnabled = !isDisabled
                        });
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Yönetim İşlemleri (Enable / Disable / Delete / Add)

        public static bool DisableStartupItem(StartupItemModel item)
        {
            try
            {
                switch (item.SourceKind)
                {
                    case StartupSourceKind.RegistryRun:
                    {
                        bool ok = SetStartupApprovedState(item.IsLocalMachine, item.StartupApprovedKeyPath, item.StartupApprovedValueName, enable: false);
                        if (ok) item.IsEnabled = false;
                        return ok;
                    }

                    case StartupSourceKind.StartupFolder:
                    {
                        bool ok = SetStartupApprovedState(item.IsLocalMachine, item.StartupApprovedKeyPath, item.StartupApprovedValueName, enable: false);
                        if (ok) item.IsEnabled = false;
                        return ok;
                    }

                    case StartupSourceKind.RegistryOther:
                    {
                        var hive = GetRootHive(item.IsLocalMachine);
                        using var srcKey = hive.OpenSubKey(item.ActiveRegistryKeyPath, true);
                        object? rawVal = srcKey?.GetValue(item.Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        RegistryValueKind kind = srcKey != null && srcKey.GetValueNames().Contains(item.Name)
                            ? srcKey.GetValueKind(item.Name)
                            : RegistryValueKind.String;

                        using var dstKey = hive.CreateSubKey(item.DisabledRegistryKeyPath, true);
                        if (dstKey == null) return false;

                        dstKey.SetValue(item.Name, rawVal ?? item.Command, kind);
                        srcKey?.DeleteValue(item.Name, false);
                        item.IsEnabled = false;
                        return true;
                    }

                    case StartupSourceKind.UwpApp:
                    {
                        if (string.IsNullOrWhiteSpace(item.UwpPackageFamily) || string.IsNullOrWhiteSpace(item.UwpTaskSubKey))
                            return false;

                        string taskPath = $@"{UwpSystemAppDataPath}\{item.UwpPackageFamily}\{item.UwpTaskSubKey}";
                        using var key = Registry.CurrentUser.CreateSubKey(taskPath, true);
                        if (key == null) return false;

                        key.SetValue("State", 1, RegistryValueKind.DWord); // 1 = DisabledByUser
                        key.SetValue("UserEnabledStartupOnce", 1, RegistryValueKind.DWord);
                        item.IsEnabled = false;
                        return true;
                    }

                    case StartupSourceKind.TaskScheduler:
                    {
                        bool ok = SetScheduledTaskEnabled(item.TaskFullPath, enable: false);
                        if (ok) item.IsEnabled = false;
                        return ok;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"Başlangıç öğesi devre dışı bırakılırken hata ({item.Name}): {ex.Message}");
            }
            return false;
        }

        public static bool EnableStartupItem(StartupItemModel item)
        {
            try
            {
                switch (item.SourceKind)
                {
                    case StartupSourceKind.RegistryRun:
                    {
                        bool ok = SetStartupApprovedState(item.IsLocalMachine, item.StartupApprovedKeyPath, item.StartupApprovedValueName, enable: true);
                        if (ok) item.IsEnabled = true;
                        return ok;
                    }

                    case StartupSourceKind.StartupFolder:
                    {
                        // Eski sürümden kalma .disabled uzantılı dosya varsa orijinal adına geri döndür
                        if (!string.IsNullOrWhiteSpace(item.FilePath) &&
                            item.FilePath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) &&
                            File.Exists(item.FilePath))
                        {
                            string origPath = item.FilePath.Substring(0, item.FilePath.Length - ".disabled".Length);
                            File.Move(item.FilePath, origPath, overwrite: true);
                            item.FilePath = origPath;
                        }

                        bool ok = SetStartupApprovedState(item.IsLocalMachine, item.StartupApprovedKeyPath, item.StartupApprovedValueName, enable: true);
                        if (ok) item.IsEnabled = true;
                        return ok;
                    }

                    case StartupSourceKind.RegistryOther:
                    {
                        var hive = GetRootHive(item.IsLocalMachine);
                        using var srcKey = hive.OpenSubKey(item.DisabledRegistryKeyPath, true);
                        object? rawVal = srcKey?.GetValue(item.Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        RegistryValueKind kind = srcKey != null && srcKey.GetValueNames().Contains(item.Name)
                            ? srcKey.GetValueKind(item.Name)
                            : RegistryValueKind.String;

                        using var dstKey = hive.CreateSubKey(item.ActiveRegistryKeyPath, true);
                        if (dstKey == null) return false;

                        dstKey.SetValue(item.Name, rawVal ?? item.Command, kind);
                        srcKey?.DeleteValue(item.Name, false);
                        item.IsEnabled = true;
                        return true;
                    }

                    case StartupSourceKind.UwpApp:
                    {
                        if (string.IsNullOrWhiteSpace(item.UwpPackageFamily) || string.IsNullOrWhiteSpace(item.UwpTaskSubKey))
                            return false;

                        string taskPath = $@"{UwpSystemAppDataPath}\{item.UwpPackageFamily}\{item.UwpTaskSubKey}";
                        using var key = Registry.CurrentUser.CreateSubKey(taskPath, true);
                        if (key == null) return false;

                        key.SetValue("State", 2, RegistryValueKind.DWord); // 2 = Enabled
                        key.SetValue("UserEnabledStartupOnce", 1, RegistryValueKind.DWord);
                        item.IsEnabled = true;
                        return true;
                    }

                    case StartupSourceKind.TaskScheduler:
                    {
                        bool ok = SetScheduledTaskEnabled(item.TaskFullPath, enable: true);
                        if (ok) item.IsEnabled = true;
                        return ok;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"Başlangıç öğesi etkinleştirilirken hata ({item.Name}): {ex.Message}");
            }
            return false;
        }

        public static bool DeleteStartupItem(StartupItemModel item)
        {
            try
            {
                switch (item.SourceKind)
                {
                    case StartupSourceKind.RegistryRun:
                    {
                        using var key = GetRootHive(item.IsLocalMachine).OpenSubKey(item.ActiveRegistryKeyPath, true);
                        key?.DeleteValue(item.Name, false);
                        DeleteStartupApprovedEntry(item.IsLocalMachine, item.StartupApprovedKeyPath, item.StartupApprovedValueName);
                        return true;
                    }

                    case StartupSourceKind.RegistryOther:
                    {
                        var hive = GetRootHive(item.IsLocalMachine);
                        using (var k1 = hive.OpenSubKey(item.ActiveRegistryKeyPath, true))
                            k1?.DeleteValue(item.Name, false);
                        using (var k2 = hive.OpenSubKey(item.DisabledRegistryKeyPath, true))
                            k2?.DeleteValue(item.Name, false);
                        return true;
                    }

                    case StartupSourceKind.StartupFolder:
                    {
                        string targetFile = !string.IsNullOrWhiteSpace(item.FilePath) ? item.FilePath : item.Command;
                        if (File.Exists(targetFile))
                        {
                            File.Delete(targetFile);
                        }
                        DeleteStartupApprovedEntry(item.IsLocalMachine, item.StartupApprovedKeyPath, item.StartupApprovedValueName);
                        return true;
                    }

                    case StartupSourceKind.UwpApp:
                    {
                        // UWP paket manifestosundaki StartupTask silinemez, güvenli şekilde devre dışı bırakılır
                        return DisableStartupItem(item);
                    }

                    case StartupSourceKind.TaskScheduler:
                    {
                        return DeleteScheduledTask(item.TaskFullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"Başlangıç öğesi silinirken hata ({item.Name}): {ex.Message}");
            }
            return false;
        }

        public static string BuildRunUniqueKey(string name, bool allUsers = false) =>
            $"REG|{allUsers}|{RunPath.ToLowerInvariant()}|{name.ToLowerInvariant()}";

        public static bool AddStartupItem(string name, string exePath, bool allUsers = false)
        {
            try
            {
                var hive = GetRootHive(allUsers);
                using var key = hive.CreateSubKey(RunPath, true);
                if (key == null) return false;

                string cmd = $"\"{exePath}\"";
                key.SetValue(name, cmd, RegistryValueKind.String);
                // Eğer daha önceden StartupApproved içinde kalmış devre dışı kaydı varsa Etkin olarak sıfırla
                SetStartupApprovedState(allUsers, ApprovedRunPath, name, enable: true);

                Guard.StartupGuardManager.RecordUserAuthorizedStartup(new StartupItemModel
                {
                    UniqueKey = BuildRunUniqueKey(name, allUsers),
                    Name = name,
                    Command = cmd,
                    CleanExePath = exePath,
                    CategoryGroup = allUsers ? GroupAllUsers : GroupCurrentUser,
                    LocationDetail = allUsers ? @"HKLM\Run" : @"HKCU\Run",
                    SourceKind = StartupSourceKind.RegistryRun,
                    IsLocalMachine = allUsers,
                    IsEnabled = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Utils.LogManager.Log($"Yeni başlangıç öğesi eklenirken hata: {ex.Message}");
                return false;
            }
        }

        private static bool SetScheduledTaskEnabled(string taskFullPath, bool enable)
        {
            if (string.IsNullOrWhiteSpace(taskFullPath)) return false;

            try
            {
                Type? schedType = Type.GetTypeFromProgID("Schedule.Service");
                if (schedType != null)
                {
                    dynamic service = Activator.CreateInstance(schedType)!;
                    service.Connect();

                    SplitTaskPath(taskFullPath, out string folderPath, out string taskName);
                    dynamic folder = service.GetFolder(folderPath);
                    dynamic task = folder.GetTask(taskName);
                    task.Enabled = enable;
                    return true;
                }
            }
            catch { }

            string flag = enable ? "/enable" : "/disable";
            return ExecuteSchtasksCommand($"/change /tn \"{taskFullPath}\" {flag}");
        }

        private static bool DeleteScheduledTask(string taskFullPath)
        {
            if (string.IsNullOrWhiteSpace(taskFullPath)) return false;

            try
            {
                Type? schedType = Type.GetTypeFromProgID("Schedule.Service");
                if (schedType != null)
                {
                    dynamic service = Activator.CreateInstance(schedType)!;
                    service.Connect();

                    SplitTaskPath(taskFullPath, out string folderPath, out string taskName);
                    dynamic folder = service.GetFolder(folderPath);
                    folder.DeleteTask(taskName, 0);
                    return true;
                }
            }
            catch { }

            return ExecuteSchtasksCommand($"/delete /tn \"{taskFullPath}\" /f");
        }

        private static void SplitTaskPath(string fullPath, out string folderPath, out string taskName)
        {
            int idx = fullPath.LastIndexOf('\\');
            if (idx <= 0)
            {
                folderPath = @"\";
                taskName = idx == 0 ? fullPath.Substring(1) : fullPath;
            }
            else
            {
                folderPath = fullPath.Substring(0, idx);
                taskName = fullPath.Substring(idx + 1);
            }
        }

        private static bool ExecuteSchtasksCommand(string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(4000);
                return proc?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Yardımcılar & Eski INI Göç Temizliği

        private static string ResolveShortcutTarget(string lnkPath)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType)!;
                    dynamic shortcut = shell.CreateShortcut(lnkPath);
                    string? target = shortcut.TargetPath?.ToString();
                    if (!string.IsNullOrWhiteSpace(target))
                    {
                        return Environment.ExpandEnvironmentVariables(target);
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public static string ExtractCleanExe(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string expanded = Environment.ExpandEnvironmentVariables(raw.Trim());

            if (expanded.StartsWith("\""))
            {
                int end = expanded.IndexOf('\"', 1);
                if (end > 1) return expanded.Substring(1, end - 1).Trim();
            }

            int exeIdx = expanded.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIdx > 0)
            {
                return expanded.Substring(0, exeIdx + 4).Trim();
            }

            return expanded;
        }

        private static void MigrateLegacyIniDisabledEntries()
        {
            try
            {
                string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HaYTooL_Firewall.ini");
                if (!File.Exists(iniPath)) return;

                string[] lines = File.ReadAllLines(iniPath, Encoding.UTF8);
                bool hasLegacySection = lines.Any(l => l.Trim().Equals($"[{LegacyDisabledIniSection}]", StringComparison.OrdinalIgnoreCase));
                if (!hasLegacySection) return;

                var cleanedLines = new List<string>();
                bool inLegacy = false;

                foreach (string line in lines)
                {
                    string t = line.Trim();
                    if (t.StartsWith("[") && t.EndsWith("]"))
                    {
                        inLegacy = t.Equals($"[{LegacyDisabledIniSection}]", StringComparison.OrdinalIgnoreCase);
                        if (inLegacy) continue;
                    }

                    if (inLegacy && (t.StartsWith(";") || t.StartsWith("#")))
                    {
                        inLegacy = false;
                    }

                    if (inLegacy)
                    {
                        // Gerçek yedek varsa Registry + StartupApproved(Disabled) olarak geri yükle
                        if (!string.IsNullOrWhiteSpace(t) && t.Contains("="))
                        {
                            int eq = t.IndexOf('=');
                            string name = t.Substring(0, eq).Trim();
                            string val = t.Substring(eq + 1).Trim();
                            string[] parts = val.Split('|');
                            if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(parts[2]))
                            {
                                bool isAllUsers = parts[0].Contains("All Users", StringComparison.OrdinalIgnoreCase);
                                string regSub = parts[1].Contains("32-bit", StringComparison.OrdinalIgnoreCase) ? RunWowPath : RunPath;
                                string appSub = parts[1].Contains("32-bit", StringComparison.OrdinalIgnoreCase) ? ApprovedRun32Path : ApprovedRunPath;

                                try
                                {
                                    using var rk = GetRootHive(isAllUsers).CreateSubKey(regSub, true);
                                    if (rk != null && rk.GetValue(name) == null)
                                    {
                                        rk.SetValue(name, parts[2], RegistryValueKind.String);
                                        SetStartupApprovedState(isAllUsers, appSub, name, enable: false);
                                    }
                                }
                                catch { }
                            }
                        }
                        continue;
                    }

                    // Bozuk ";===...|HKCU|" satırını düzelt
                    if (t.StartsWith(";===") && t.Contains("|HKCU|"))
                    {
                        cleanedLines.Add("; =============================================================================");
                        continue;
                    }

                    cleanedLines.Add(line);
                }

                File.WriteAllLines(iniPath, cleanedLines, Encoding.UTF8);
            }
            catch { }
        }

        #endregion
    }
}
