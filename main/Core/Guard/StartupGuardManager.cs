using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GuvenlikDuvarim.Core.Services;
using GuvenlikDuvarim.Core.Startup;
using GuvenlikDuvarim.Core.Utils;

namespace GuvenlikDuvarim.Core.Guard
{
    public class GuardSnapshotRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class GuardEntryInfo
    {
        public string Id { get; set; } = string.Empty;
        public string EventTitle { get; set; } = "Yeni Başlangıç Öğesi Eklendi!";
        public string Type { get; set; } = "Başlangıç Uygulaması";
        public string Name { get; set; } = string.Empty;
        public string CommandOrPath { get; set; } = string.Empty;
        public string CleanExePath { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool WasNewlyAdded { get; set; } = true;
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public object? RawItem { get; set; }
    }

    public static class StartupGuardManager
    {
        private static System.Threading.Timer? _guardTimer;
        private static readonly object _syncLock = new();
        private static Dictionary<string, GuardSnapshotRecord> _snapshot = new(StringComparer.OrdinalIgnoreCase);

        public static string StorageDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        private static string StateFilePath => Path.Combine(StorageDirectory, "guard_state.json");
        private static string LegacyBaselinePath => Path.Combine(StorageDirectory, "guard_baseline.json");
        private static string GuardLockFilePath => Path.Combine(StorageDirectory, "guard_active.lock");
        private static FileStream? _guardLockStream;

        private static DateTime _lastDiskSyncUtc = DateTime.MinValue;
        private static bool _isInitialized = false;
        private static bool _isChecking = false;
        private static int _tickCounter = 0;

        private static readonly Queue<GuardEntryInfo> _pendingAlerts = new();
        private static readonly HashSet<string> _activeAlertIds = new(StringComparer.OrdinalIgnoreCase);
        private static bool _isAlertWindowOpen = false;

        public static bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Başlangıç veya servis listesinde harici/dahili bir değişiklik olduğunda açık pencereleri canlı yenilemek için tetiklenir.
        /// </summary>
        public static event Action? SystemStateChanged;

        public static void RaiseSystemStateChanged()
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    SystemStateChanged?.Invoke();
                }));
            }
            catch { }
        }

        public static void Start()
        {
            if (_guardTimer != null) return;

            CleanupLegacyBaselineFile();

            // Baseline'ı gecikmesiz hemen arka planda hazırla, ardından her 2.5 saniyede bir canlı izle
            _ = Task.Run(() =>
            {
                if (TryAcquireGuardOwnership())
                {
                    EnsureInitialized();
                }
                _guardTimer = new System.Threading.Timer(OnTimerTick, null, 2000, 2500);
            });

            LogManager.Log("🛡️ Başlangıç & Servis Koruma Kalkanı (Canlı İzleme) başlatıldı.");
        }

        public static void Stop()
        {
            _guardTimer?.Dispose();
            _guardTimer = null;
            try
            {
                _guardLockStream?.Dispose();
                _guardLockStream = null;
            }
            catch { }
        }

        private static bool TryAcquireGuardOwnership()
        {
            if (_guardLockStream != null) return true;
            try
            {
                _guardLockStream = new FileStream(
                    GuardLockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    1,
                    FileOptions.DeleteOnClose);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CleanupLegacyBaselineFile()
        {
            try
            {
                if (File.Exists(LegacyBaselinePath))
                {
                    File.Delete(LegacyBaselinePath);
                }
            }
            catch { }
        }

        /// <summary>
        /// HaYTooL arayüzü içerisinden (StartupWindow / ServicesWindow) bizzat kullanıcı tarafından yapılan değişiklikleri
        /// güvenli olarak işler ve diğer HaYTooL süreçleriyle (tray <-> standalone) senkronize eder.
        /// </summary>
        public static void RecordUserAuthorizedStartup(StartupItemModel item)
        {
            EnsureInitialized();
            lock (_syncLock)
            {
                EnsureDiskSyncedUnsafe();
                _snapshot[item.UniqueKey] = new GuardSnapshotRecord
                {
                    Id = item.UniqueKey,
                    Name = item.Name,
                    Command = item.Command,
                    IsEnabled = item.IsEnabled
                };
                SaveSnapshotUnsafe();
            }
        }

        public static void RecordUserRemovedStartup(string uniqueKey)
        {
            if (string.IsNullOrWhiteSpace(uniqueKey)) return;
            EnsureInitialized();
            lock (_syncLock)
            {
                EnsureDiskSyncedUnsafe();
                if (_snapshot.Remove(uniqueKey))
                {
                    SaveSnapshotUnsafe();
                }
            }
        }

        public static void RecordUserAuthorizedService(ServiceItemModel svc, bool isEnabled)
        {
            EnsureInitialized();
            string key = GetServiceKey(svc.ServiceName);
            lock (_syncLock)
            {
                EnsureDiskSyncedUnsafe();
                _snapshot[key] = new GuardSnapshotRecord
                {
                    Id = key,
                    Name = svc.DisplayName,
                    Command = svc.ImagePath,
                    IsEnabled = isEnabled
                };
                SaveSnapshotUnsafe();
            }
        }

        public static void RecordUserRemovedService(string serviceName)
        {
            EnsureInitialized();
            string key = GetServiceKey(serviceName);
            lock (_syncLock)
            {
                EnsureDiskSyncedUnsafe();
                if (_snapshot.Remove(key))
                {
                    SaveSnapshotUnsafe();
                }
            }
        }

        public static string GetServiceKey(string serviceName) =>
            $"SVC|{serviceName.ToLowerInvariant()}";

        private static async void OnTimerTick(object? state)
        {
            if (_isChecking) return;

            // Aynı anda birden fazla HaYTooL süreci (örn. Tepsi + bağımsız Startup.cmd) açıksa
            // yalnızca kilit sahibi süreç uyarı penceresi açar; kilit sahibi kapanırsa diğeri otomatik devralır.
            if (!TryAcquireGuardOwnership())
            {
                return;
            }

            _isChecking = true;

            try
            {
                await CheckSystemChangesAsync();
            }
            catch (Exception ex)
            {
                LogManager.Log($"StartupGuard kontrol hatası: {ex.Message}");
            }
            finally
            {
                _isChecking = false;
            }
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            lock (_syncLock)
            {
                if (_isInitialized) return;

                if (File.Exists(StateFilePath))
                {
                    LoadSnapshotUnsafe();
                }

                if (_snapshot.Count == 0)
                {
                    BuildInitialBaselineUnsafe();
                    SaveSnapshotUnsafe();
                }

                _isInitialized = true;
            }
        }

        private static void BuildInitialBaselineUnsafe()
        {
            try
            {
                var startups = StartupManager.GetAllStartupItems(includeTaskScheduler: true);
                foreach (var s in startups)
                {
                    _snapshot[s.UniqueKey] = new GuardSnapshotRecord
                    {
                        Id = s.UniqueKey,
                        Name = s.Name,
                        Command = s.Command,
                        IsEnabled = s.IsEnabled
                    };
                }

                var services = ServiceManager.GetAllServices();
                var allSvcNames = new HashSet<string>(services.Select(x => x.ServiceName), StringComparer.OrdinalIgnoreCase);

                foreach (var svc in services)
                {
                    if (IsDynamicSessionCloneService(svc.ServiceName, allSvcNames)) continue;

                    string key = GetServiceKey(svc.ServiceName);
                    _snapshot[key] = new GuardSnapshotRecord
                    {
                        Id = key,
                        Name = svc.DisplayName,
                        Command = svc.ImagePath,
                        IsEnabled = svc.StartupType != "Devre Dışı"
                    };
                }
            }
            catch (Exception ex)
            {
                LogManager.Log($"StartupGuard ilk baseline oluşturma hatası: {ex.Message}");
            }
        }

        private static bool IsDynamicSessionCloneService(string serviceName, HashSet<string> allServiceNames)
        {
            // Örn: WpnUserService_138403 -> şablonu WpnUserService zaten mevcutsa dinamik oturum klonudur
            int idx = serviceName.LastIndexOf('_');
            if (idx > 0 && idx < serviceName.Length - 3)
            {
                string suffix = serviceName.Substring(idx + 1);
                if (Regex.IsMatch(suffix, "^[0-9a-fA-F]{4,10}$"))
                {
                    string baseName = serviceName.Substring(0, idx);
                    if (allServiceNames.Contains(baseName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void EnsureDiskSyncedUnsafe()
        {
            try
            {
                string guardVal = Storage.IniStorage.ReadValue("Settings", "EnableStartupGuard", "True");
                IsEnabled = !string.Equals(guardVal, "False", StringComparison.OrdinalIgnoreCase);
            }
            catch { }

            try
            {
                if (!File.Exists(StateFilePath)) return;
                DateTime diskUtc = File.GetLastWriteTimeUtc(StateFilePath);
                if (diskUtc > _lastDiskSyncUtc.AddMilliseconds(50))
                {
                    LoadSnapshotUnsafe();
                }
            }
            catch { }
        }

        private static void LoadSnapshotUnsafe()
        {
            try
            {
                if (!File.Exists(StateFilePath)) return;
                string json = File.ReadAllText(StateFilePath);
                var list = JsonSerializer.Deserialize<List<GuardSnapshotRecord>>(json);
                if (list != null)
                {
                    _snapshot = new Dictionary<string, GuardSnapshotRecord>(StringComparer.OrdinalIgnoreCase);
                    foreach (var rec in list)
                    {
                        if (!string.IsNullOrWhiteSpace(rec.Id))
                        {
                            _snapshot[rec.Id] = rec;
                        }
                    }
                }
                _lastDiskSyncUtc = File.GetLastWriteTimeUtc(StateFilePath);
            }
            catch { }
        }

        private static void SaveSnapshotUnsafe()
        {
            try
            {
                string json = JsonSerializer.Serialize(_snapshot.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFilePath, json);
                _lastDiskSyncUtc = File.GetLastWriteTimeUtc(StateFilePath);
            }
            catch { }
        }

        public static async Task CheckSystemChangesAsync()
        {
            EnsureInitialized();

            int tick = Interlocked.Increment(ref _tickCounter);
            // Her 2 tikte bir (5 sn) Görev Zamanlayıcı dahil tam tarama; ara tiklerde hızlı Kayıt Defteri + Klasör + UWP + Servis taraması
            bool includeTasks = (tick % 2 == 1);

            var currentStartup = await Task.Run(() => StartupManager.GetAllStartupItems(includeTaskScheduler: includeTasks));
            var currentServices = await Task.Run(() => ServiceManager.GetAllServices());
            var allSvcNames = new HashSet<string>(currentServices.Select(x => x.ServiceName), StringComparer.OrdinalIgnoreCase);

            var alertsToFire = new List<GuardEntryInfo>();
            bool stateChanged = false;

            lock (_syncLock)
            {
                EnsureDiskSyncedUnsafe();

                // --- 1. BAŞLANGIÇ ÖĞELERİ KARŞILAŞTIRMASI ---
                var seenStartupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in currentStartup)
                {
                    seenStartupIds.Add(item.UniqueKey);

                    if (_activeAlertIds.Contains(item.UniqueKey))
                        continue;

                    if (!_snapshot.TryGetValue(item.UniqueKey, out var prev))
                    {
                        // Yepyeni bir başlangıç öğesi algılandı!
                        // (UWP paketlerinde varsayılan olarak kapalı gelen sistem uygulamalarını sessizce kaydet, etkinse uyar)
                        bool shouldAlert = IsEnabled && (!item.IsUwp || item.IsEnabled);

                        _snapshot[item.UniqueKey] = new GuardSnapshotRecord
                        {
                            Id = item.UniqueKey,
                            Name = item.Name,
                            Command = item.Command,
                            IsEnabled = item.IsEnabled
                        };
                        stateChanged = true;

                        if (shouldAlert)
                        {
                            _activeAlertIds.Add(item.UniqueKey);
                            alertsToFire.Add(new GuardEntryInfo
                            {
                                Id = item.UniqueKey,
                                EventTitle = "Yeni Başlangıç Uygulaması Eklendi!",
                                Type = $"Başlangıç ({item.CategoryGroup})",
                                Name = item.Name,
                                CommandOrPath = item.Command,
                                CleanExePath = item.CleanExePath,
                                Location = $"{item.CategoryGroup} — {item.LocationDetail}",
                                WasNewlyAdded = true,
                                RawItem = item
                            });
                        }
                    }
                    else
                    {
                        // Daha önceden bilinen öğe: Devre Dışı -> Etkin geçişi veya Komut değişikliği kontrolü
                        bool enabledTurnedOn = !prev.IsEnabled && item.IsEnabled;
                        bool commandChanged = !string.Equals(prev.Command, item.Command, StringComparison.OrdinalIgnoreCase) && item.IsEnabled;

                        if (prev.IsEnabled != item.IsEnabled || !string.Equals(prev.Command, item.Command, StringComparison.Ordinal))
                        {
                            prev.IsEnabled = item.IsEnabled;
                            prev.Command = item.Command;
                            prev.Name = item.Name;
                            stateChanged = true;
                        }

                        if (IsEnabled && (enabledTurnedOn || commandChanged))
                        {
                            _activeAlertIds.Add(item.UniqueKey);
                            alertsToFire.Add(new GuardEntryInfo
                            {
                                Id = item.UniqueKey,
                                EventTitle = commandChanged
                                    ? "Başlangıç Öğesi Komutu Değiştirildi!"
                                    : "Başlangıç Uygulaması Etkinleştirildi!",
                                Type = $"Başlangıç ({item.CategoryGroup})",
                                Name = item.Name,
                                CommandOrPath = item.Command,
                                CleanExePath = item.CleanExePath,
                                Location = $"{item.CategoryGroup} — {item.LocationDetail}",
                                WasNewlyAdded = false,
                                RawItem = item
                            });
                        }
                    }
                }

                // Sistemden silinen başlangıç öğelerini snapshot'tan kaldır (tekrar eklenirse yeniden uyarabilsin!)
                var removedStartupKeys = _snapshot.Keys
                    .Where(k => !k.StartsWith("SVC|", StringComparison.OrdinalIgnoreCase))
                    .Where(k => includeTasks || !k.StartsWith("TASK|", StringComparison.OrdinalIgnoreCase))
                    .Where(k => !seenStartupIds.Contains(k) && !_activeAlertIds.Contains(k))
                    .ToList();

                foreach (string rk in removedStartupKeys)
                {
                    _snapshot.Remove(rk);
                    stateChanged = true;
                }

                // --- 2. WINDOWS HİZMETLERİ KARŞILAŞTIRMASI ---
                var seenServiceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var svc in currentServices)
                {
                    if (IsDynamicSessionCloneService(svc.ServiceName, allSvcNames)) continue;

                    string svcKey = GetServiceKey(svc.ServiceName);
                    seenServiceIds.Add(svcKey);

                    if (_activeAlertIds.Contains(svcKey))
                        continue;

                    bool svcEnabled = svc.StartupType != "Devre Dışı";

                    if (!_snapshot.TryGetValue(svcKey, out var prevSvc))
                    {
                        _snapshot[svcKey] = new GuardSnapshotRecord
                        {
                            Id = svcKey,
                            Name = svc.DisplayName,
                            Command = svc.ImagePath,
                            IsEnabled = svcEnabled
                        };
                        stateChanged = true;

                        if (IsEnabled && svcEnabled)
                        {
                            _activeAlertIds.Add(svcKey);
                            alertsToFire.Add(new GuardEntryInfo
                            {
                                Id = svcKey,
                                EventTitle = "Yeni Windows Hizmeti (Servis) Eklendi!",
                                Type = "Windows Hizmeti (Service)",
                                Name = $"{svc.DisplayName} ({svc.ServiceName})",
                                CommandOrPath = svc.ImagePath,
                                CleanExePath = svc.CleanExePath,
                                Location = $"SYSTEM\\CurrentControlSet\\Services\\{svc.ServiceName} ({svc.StartupType})",
                                WasNewlyAdded = true,
                                RawItem = svc
                            });
                        }
                    }
                    else
                    {
                        bool reEnabled = !prevSvc.IsEnabled && svcEnabled && !svc.IsMicrosoftService;
                        if (prevSvc.IsEnabled != svcEnabled || !string.Equals(prevSvc.Command, svc.ImagePath, StringComparison.Ordinal))
                        {
                            prevSvc.IsEnabled = svcEnabled;
                            prevSvc.Command = svc.ImagePath;
                            stateChanged = true;
                        }

                        if (IsEnabled && reEnabled)
                        {
                            _activeAlertIds.Add(svcKey);
                            alertsToFire.Add(new GuardEntryInfo
                            {
                                Id = svcKey,
                                EventTitle = "Devre Dışı Windows Hizmeti Etkinleştirildi!",
                                Type = "Windows Hizmeti (Service)",
                                Name = $"{svc.DisplayName} ({svc.ServiceName})",
                                CommandOrPath = svc.ImagePath,
                                CleanExePath = svc.CleanExePath,
                                Location = $"SYSTEM\\CurrentControlSet\\Services\\{svc.ServiceName} ({svc.StartupType})",
                                WasNewlyAdded = false,
                                RawItem = svc
                            });
                        }
                    }
                }

                var removedSvcKeys = _snapshot.Keys
                    .Where(k => k.StartsWith("SVC|", StringComparison.OrdinalIgnoreCase))
                    .Where(k => !seenServiceIds.Contains(k) && !_activeAlertIds.Contains(k))
                    .ToList();

                foreach (string rk in removedSvcKeys)
                {
                    _snapshot.Remove(rk);
                    stateChanged = true;
                }

                if (stateChanged)
                {
                    SaveSnapshotUnsafe();
                }
            }

            if (stateChanged)
            {
                RaiseSystemStateChanged();
            }

            foreach (var alert in alertsToFire)
            {
                TriggerAlert(alert);
            }
        }

        public static void CompleteAlertDecision(GuardEntryInfo info, bool existsAfterDecision, bool isEnabledAfterDecision)
        {
            lock (_syncLock)
            {
                _activeAlertIds.Remove(info.Id);
                if (!existsAfterDecision)
                {
                    _snapshot.Remove(info.Id);
                }
                else
                {
                    _snapshot[info.Id] = new GuardSnapshotRecord
                    {
                        Id = info.Id,
                        Name = info.Name,
                        Command = info.CommandOrPath,
                        IsEnabled = isEnabledAfterDecision
                    };
                }
                SaveSnapshotUnsafe();
            }

            RaiseSystemStateChanged();
        }

        private static void TriggerAlert(GuardEntryInfo info)
        {
            LogManager.Log($"⚠️ KORUMA KALKANI UYARISI: [{info.EventTitle}] {info.Name} -> {info.CommandOrPath}");

            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                lock (_pendingAlerts)
                {
                    _pendingAlerts.Enqueue(info);
                }
                ShowNextAlertWindow();
            }));
        }

        public static void ShowNextAlertWindow()
        {
            if (_isAlertWindowOpen) return;

            GuardEntryInfo? next = null;
            lock (_pendingAlerts)
            {
                if (_pendingAlerts.Count > 0)
                {
                    next = _pendingAlerts.Dequeue();
                }
            }

            if (next == null) return;

            _isAlertWindowOpen = true;
            try
            {
                System.Media.SystemSounds.Exclamation.Play();
                var win = new UI.StartupGuardAlertWindow(next);
                win.Closed += (s, e) =>
                {
                    lock (_syncLock)
                    {
                        _activeAlertIds.Remove(next.Id);
                    }
                    _isAlertWindowOpen = false;
                    ShowNextAlertWindow();
                };
                win.Show();
                win.Activate();
            }
            catch (Exception ex)
            {
                LogManager.Log($"Uyarı penceresi açılamadı: {ex.Message}");
                lock (_syncLock)
                {
                    _activeAlertIds.Remove(next.Id);
                }
                _isAlertWindowOpen = false;
            }
        }
    }
}
