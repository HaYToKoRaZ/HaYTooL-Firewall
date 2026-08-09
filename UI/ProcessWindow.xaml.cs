using GuvenlikDuvarim.Core.Firewall;
using GuvenlikDuvarim.Core.I18n;
using GuvenlikDuvarim.Core.Scanner;
using GuvenlikDuvarim.Core.Storage;
using GuvenlikDuvarim.Core.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GuvenlikDuvarim.UI
{
    public class ProcessItemModel : INotifyPropertyChanged
    {
        public int Pid { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public double RamMb { get; set; }
        public string RamText => $"{RamMb:F1} MB";

        public int NetworkConnections { get; set; }
        public string NetworkStatus => NetworkConnections > 0 ? string.Format(LanguageManager.Get("TaskMgrNetActive"), NetworkConnections) : LanguageManager.Get("TaskMgrNetNone");
        public string NetworkStatusColor => NetworkConnections > 0 ? "#0284C7" : "#9CA3AF";
        public string NetworkBadgeBackground => NetworkConnections > 0 ? "#E0F2FE" : "Transparent";

        private string _status = "⚪ Kural Yok";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private string _statusColor = "#9CA3AF";
        public string StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        private string _badgeBackground = "Transparent";
        public string BadgeBackground
        {
            get => _badgeBackground;
            set { _badgeBackground = value; OnPropertyChanged(); }
        }

        public CategoryModel? AssociatedProfile { get; set; }
        public AppItemModel? MatchedProfileItem { get; set; }

        private string _profileName = "⚪ Profil Dışı";
        public string ProfileName
        {
            get => _profileName;
            set { _profileName = value; OnPropertyChanged(); }
        }

        private string _profileColor = "#9CA3AF";
        public string ProfileColor
        {
            get => _profileColor;
            set { _profileColor = value; OnPropertyChanged(); }
        }

        private string _profileBadgeBackground = "Transparent";
        public string ProfileBadgeBackground
        {
            get => _profileBadgeBackground;
            set { _profileBadgeBackground = value; OnPropertyChanged(); }
        }

        public ImageSource? Icon => IconExtractor.GetIcon(FullPath, isFolder: false);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class ProcessWindow : Window
    {
        private List<ProcessItemModel> _allProcesses = new();
        private ObservableCollection<ProcessItemModel> _filteredProcesses = new();
        private List<FirewallRuleInfo> _activeFirewallRules = new();
        public CategoryModel? TargetCategory { get; set; }
        public bool AddedToProfile { get; private set; } = false;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        private readonly MainWindow? _mainWindow;

        public ProcessWindow(MainWindow mainWindow) : this()
        {
            _mainWindow = mainWindow;
            if (mainWindow != null) Owner = mainWindow;
        }

        public ProcessWindow()
        {
            InitializeComponent();
            ApplyLanguageText();
            Loaded += async (s, e) => await LoadProcessesAsync();
        }

        public void ApplyLanguageText()
        {
            Title = LanguageManager.Get("TaskMgrTitle");
            if (txtProcessTitle != null) txtProcessTitle.Text = LanguageManager.Get("TaskMgrHeader");
            if (btnRefreshList != null) btnRefreshList.Content = LanguageManager.Get("RefreshList");
            if (txtSearchPlaceholder != null) txtSearchPlaceholder.Text = LanguageManager.Get("TaskMgrSearchPlaceholder");

            if (chkFilterNetwork != null) chkFilterNetwork.Content = LanguageManager.Get("TaskMgrFilterNetwork");
            if (chkFilterBlocked != null) chkFilterBlocked.Content = LanguageManager.Get("TaskMgrFilterBlocked");
            if (chkFilterAllowed != null) chkFilterAllowed.Content = LanguageManager.Get("TaskMgrFilterAllowed");
            if (chkFilterNoRule != null) chkFilterNoRule.Content = LanguageManager.Get("TaskMgrFilterNoRule");

            if (colIcon != null) colIcon.Header = LanguageManager.Get("ColHeaderIcon");
            if (colName != null) colName.Header = LanguageManager.Get("ColHeaderName");
            if (colRam != null) colRam.Header = LanguageManager.Get("TaskMgrColRam");
            if (colNetwork != null) colNetwork.Header = LanguageManager.Get("TaskMgrColNetwork");
            if (colStatus != null) colStatus.Header = LanguageManager.Get("TaskMgrColStatus");
            if (colProfile != null) colProfile.Header = LanguageManager.Get("TaskMgrColProfile");
            if (colPath != null) colPath.Header = LanguageManager.Get("ColHeaderPath");

            if (btnBlockProcess != null) btnBlockProcess.Content = LanguageManager.Get("TaskMgrBtnBlock");
            if (btnAllowProcess != null) btnAllowProcess.Content = LanguageManager.Get("TaskMgrBtnAllow");
            if (btnDeleteRule != null) btnDeleteRule.Content = LanguageManager.Get("TaskMgrBtnDeleteRule");
            if (btnAddToProfile != null) btnAddToProfile.Content = LanguageManager.Get("TaskMgrBtnAddExeProfile");
            if (btnAddFolderToProfile != null) btnAddFolderToProfile.Content = LanguageManager.Get("TaskMgrBtnAddFolderProfile");
            if (btnOpenLocation != null) btnOpenLocation.Content = LanguageManager.Get("TaskMgrBtnOpenLoc");
            if (btnKillProcess != null) btnKillProcess.Content = LanguageManager.Get("TaskMgrBtnKill");

            if (ctxBlockProcess != null) ctxBlockProcess.Header = LanguageManager.Get("TaskMgrBtnBlock");
            if (ctxAllowProcess != null) ctxAllowProcess.Header = LanguageManager.Get("TaskMgrBtnAllow");
            if (ctxDeleteRule != null) ctxDeleteRule.Header = LanguageManager.Get("TaskMgrBtnDeleteRule");
            if (ctxAddToProfile != null) ctxAddToProfile.Header = LanguageManager.Get("TaskMgrBtnAddExeProfile");
            if (ctxAddFolderToProfile != null) ctxAddFolderToProfile.Header = LanguageManager.Get("TaskMgrBtnAddFolderProfile");
            if (ctxLocateProfile != null) ctxLocateProfile.Header = LanguageManager.Get("TaskMgrLocateProfile");
            if (ctxOpenLocation != null) ctxOpenLocation.Header = LanguageManager.Get("TaskMgrBtnOpenLoc");
            if (ctxKillProcess != null) ctxKillProcess.Header = LanguageManager.Get("TaskMgrBtnKill");

            if (_allProcesses != null && _allProcesses.Count > 0)
            {
                var categories = _mainWindow?.Categories?.ToList() ?? new List<CategoryModel>();
                foreach (var p in _allProcesses)
                {
                    EvaluateFirewallStatus(p);
                    EvaluateProfileMembership(p, categories);
                }
                FilterProcesses();
            }
        }

        private async Task LoadProcessesAsync()
        {
            _allProcesses.Clear();
            _filteredProcesses.Clear();
            dgProcesses.ItemsSource = null;

            txtProcessCount.Text = "⏳ Süreçler ve Ağ Bağlantıları taranıyor...";

            await Task.Run(() =>
            {
                try
                {
                    _activeFirewallRules = FirewallManager.GetActiveRules();
                }
                catch
                {
                    _activeFirewallRules = new List<FirewallRuleInfo>();
                }

                var netMap = NetworkHelper.GetActiveConnectionCounts();
                var processList = Process.GetProcesses();
                var categories = _mainWindow?.Categories?.ToList() ?? new List<CategoryModel>();
                var tempMap = new Dictionary<string, ProcessItemModel>(StringComparer.OrdinalIgnoreCase);

                foreach (var p in processList)
                {
                    try
                    {
                        if (p.Id <= 4) continue;

                        string exePath = GetProcessPath(p.Id);
                        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) continue;

                        double ramMb = 0;
                        try { ramMb = p.WorkingSet64 / (1024.0 * 1024.0); } catch { }

                        int netConns = netMap.GetValueOrDefault(p.Id, 0);

                        string pName = p.ProcessName;
                        if (string.IsNullOrWhiteSpace(pName)) pName = Path.GetFileNameWithoutExtension(exePath);

                        if (tempMap.TryGetValue(exePath, out var existing))
                        {
                            existing.RamMb += ramMb;
                            existing.NetworkConnections += netConns;
                        }
                        else
                        {
                            var item = new ProcessItemModel
                            {
                                Pid = p.Id,
                                ProcessName = pName,
                                FullPath = exePath,
                                RamMb = ramMb,
                                NetworkConnections = netConns
                            };

                            EvaluateFirewallStatus(item);
                            EvaluateProfileMembership(item, categories);
                            tempMap[exePath] = item;
                        }
                    }
                    catch { }
                }

                _allProcesses = tempMap.Values.OrderByDescending(v => v.NetworkConnections).ThenByDescending(v => v.RamMb).ToList();
            });

            FilterProcesses();
        }

        private string GetProcessPath(int pid)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    var sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                    {
                        return sb.ToString();
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }

            try
            {
                var proc = Process.GetProcessById(pid);
                return proc.MainModule?.FileName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void EvaluateFirewallStatus(ProcessItemModel item)
        {
            if (string.IsNullOrWhiteSpace(item.FullPath)) return;

            string lowerPath = item.FullPath.ToLowerInvariant();
            var matchingRules = _activeFirewallRules
                .Where(r => (r.ApplicationPath ?? "").Equals(lowerPath, StringComparison.OrdinalIgnoreCase) && r.IsEnabled)
                .ToList();

            if (matchingRules.Count > 0)
            {
                bool inBlocked = matchingRules.Any(r => (r.RawDirection == 1 || r.RawDirection == 3 || r.IsMerged) && r.RawAction == 0);
                bool outBlocked = matchingRules.Any(r => (r.RawDirection == 2 || r.RawDirection == 3 || r.IsMerged) && r.RawAction == 0);
                bool inAllowed = matchingRules.Any(r => (r.RawDirection == 1 || r.RawDirection == 3 || r.IsMerged) && r.RawAction == 1);
                bool outAllowed = matchingRules.Any(r => (r.RawDirection == 2 || r.RawDirection == 3 || r.IsMerged) && r.RawAction == 1);

                if (inBlocked && outBlocked)
                {
                    item.Status = LanguageManager.Get("TaskMgrStatusBlockedBoth");
                    item.StatusColor = "#DC2626";
                    item.BadgeBackground = "#FEE2E2";
                }
                else if (inBlocked)
                {
                    item.Status = LanguageManager.Get("TaskMgrStatusInboundBlocked");
                    item.StatusColor = "#EA580C";
                    item.BadgeBackground = "#FFEDD5";
                }
                else if (outBlocked)
                {
                    item.Status = LanguageManager.Get("TaskMgrStatusOutboundBlocked");
                    item.StatusColor = "#EA580C";
                    item.BadgeBackground = "#FFEDD5";
                }
                else if (inAllowed && outAllowed)
                {
                    item.Status = LanguageManager.Get("TaskMgrStatusAllowedBoth");
                    item.StatusColor = "#16A34A";
                    item.BadgeBackground = "#DCFCE7";
                }
                else if (inAllowed)
                {
                    item.Status = LanguageManager.Get("TaskMgrStatusInboundAllowed");
                    item.StatusColor = "#059669";
                    item.BadgeBackground = "#D1FAE5";
                }
                else if (outAllowed)
                {
                    item.Status = LanguageManager.Get("TaskMgrStatusOutboundAllowed");
                    item.StatusColor = "#059669";
                    item.BadgeBackground = "#D1FAE5";
                }
                else
                {
                    item.Status = LanguageManager.Get("TaskMgrNoRule");
                    item.StatusColor = "#9CA3AF";
                    item.BadgeBackground = "Transparent";
                }
            }
            else
            {
                item.Status = LanguageManager.Get("TaskMgrNoRule");
                item.StatusColor = "#9CA3AF";
                item.BadgeBackground = "Transparent";
            }
        }

        private void EvaluateProfileMembership(ProcessItemModel item, List<CategoryModel> categories)
        {
            if (string.IsNullOrWhiteSpace(item.FullPath) || categories == null || categories.Count == 0) return;

            string lowerPath = item.FullPath.ToLowerInvariant();
            CategoryModel? foundCat = null;
            AppItemModel? foundCatItem = null;

            foreach (var cat in categories)
            {
                foreach (var catItem in cat.Items)
                {
                    if (string.IsNullOrWhiteSpace(catItem.Path)) continue;

                    if (!catItem.IsFolder)
                    {
                        if (catItem.Path.Equals(lowerPath, StringComparison.OrdinalIgnoreCase))
                        {
                            foundCat = cat;
                            foundCatItem = catItem;
                            break;
                        }
                    }
                    else
                    {
                        string folderPath = catItem.Path.TrimEnd('\\', '/').ToLowerInvariant();
                        if (lowerPath.StartsWith(folderPath + "\\") || lowerPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase))
                        {
                            foundCat = cat;
                            foundCatItem = catItem;
                            break;
                        }
                    }
                }
                if (foundCat != null) break;
            }

            if (foundCat != null && foundCatItem != null)
            {
                item.AssociatedProfile = foundCat;
                item.MatchedProfileItem = foundCatItem;
                if (foundCatItem.IsFolder)
                {
                    string folderName = Path.GetFileName(foundCatItem.Path.TrimEnd('\\', '/'));
                    item.ProfileName = $"📁 {foundCat.Name} ({folderName})";
                }
                else
                {
                    item.ProfileName = $"📄 {foundCat.Name}";
                }
                item.ProfileColor = "#2563EB";
                item.ProfileBadgeBackground = "#DBEAFE";
            }
            else
            {
                item.AssociatedProfile = null;
                item.MatchedProfileItem = null;
                item.ProfileName = LanguageManager.Get("TaskMgrNoProfile");
                item.ProfileColor = "#9CA3AF";
                item.ProfileBadgeBackground = "Transparent";
            }
        }

        private void FilterProcesses()
        {
            if (dgProcesses == null || txtProcessCount == null) return;

            string query = (txtSearch?.Text ?? "").Trim().ToLowerInvariant();

            bool filterNet = chkFilterNetwork?.IsChecked == true;
            bool filterBlock = chkFilterBlocked?.IsChecked == true;
            bool filterAllow = chkFilterAllowed?.IsChecked == true;
            bool filterNoRule = chkFilterNoRule?.IsChecked == true;

            bool hasFilter = filterNet || filterBlock || filterAllow || filterNoRule;

            var filtered = _allProcesses.Where(p =>
            {
                if (p == null) return false;

                string pName = p.ProcessName ?? string.Empty;
                string pPath = p.FullPath ?? string.Empty;
                string pStatus = p.Status ?? string.Empty;

                bool matchesQuery = string.IsNullOrEmpty(query) ||
                                     pName.ToLowerInvariant().Contains(query) ||
                                     pPath.ToLowerInvariant().Contains(query) ||
                                     p.Pid.ToString().Contains(query);

                if (!matchesQuery) return false;

                if (!hasFilter) return true;

                bool matchesFilter = false;

                if (filterNet && p.NetworkConnections > 0) matchesFilter = true;
                if (filterBlock && (pStatus.Contains("Engellend", StringComparison.OrdinalIgnoreCase) || pStatus.Contains("Engelli", StringComparison.OrdinalIgnoreCase) || pStatus.Contains("Block", StringComparison.OrdinalIgnoreCase))) matchesFilter = true;
                if (filterAllow && (pStatus.Contains("İzin", StringComparison.OrdinalIgnoreCase) || pStatus.Contains("Allow", StringComparison.OrdinalIgnoreCase))) matchesFilter = true;
                if (filterNoRule && (pStatus.Contains("Kural Yok", StringComparison.OrdinalIgnoreCase) || pStatus.Contains("No Rule", StringComparison.OrdinalIgnoreCase))) matchesFilter = true;

                return matchesFilter;
            }).ToList();

            _filteredProcesses = new ObservableCollection<ProcessItemModel>(filtered);
            dgProcesses.ItemsSource = _filteredProcesses;
            txtProcessCount.Text = $"{filtered.Count} / {_allProcesses.Count} {LanguageManager.Get("TaskMgrRunningCount")}";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterProcesses();

        private void FilterCheckBox_Changed(object sender, RoutedEventArgs e) => FilterProcesses();

        private async void BtnRefreshList_Click(object sender, RoutedEventArgs e) => await LoadProcessesAsync();

        private async void BtnBlockProcess_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected)
            {
                MessageBox.Show(LanguageManager.Get("TaskMgrSelectWarning"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FirewallManager.IsProtectedSystemPath(selected.FullPath))
            {
                MessageBox.Show($"DİKKAT: '{selected.FullPath}' kritik bir sistem dosyasıdır ve engellenmesi ağ çökmesine yol açabilir.", "Sistem Koruması", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string appName = Path.GetFileNameWithoutExtension(selected.FullPath) + "_" + selected.FullPath.GetHashCode();
                FirewallManager.ApplyRule(appName, selected.FullPath, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: false);

                var targetCat = _mainWindow?.GetSelectedOrActiveCategory() ?? TargetCategory;
                if (targetCat != null)
                {
                    if (!targetCat.Items.Any(i => i.Path.Equals(selected.FullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        targetCat.Items.Add(new AppItemModel { Path = selected.FullPath, IsFolder = false, BlockInbound = true, BlockOutbound = true });
                        AddedToProfile = true;
                        _mainWindow?.NotifyProfileItemAdded(targetCat);
                    }
                    MessageBox.Show($"⛔ '{selected.ProcessName}' hem Windows Güvenlik Duvarı'nda engellendi hem de '{targetCat.Name}' profiline eklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"⛔ '{selected.ProcessName}' için Windows Güvenlik Duvarı Engelleme Kuralı Oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadProcessesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kural oluşturulurken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAllowProcess_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected)
            {
                MessageBox.Show(LanguageManager.Get("TaskMgrSelectWarning"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string appName = Path.GetFileNameWithoutExtension(selected.FullPath) + "_" + selected.FullPath.GetHashCode();
                FirewallManager.ApplyRule(appName, selected.FullPath, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: true);

                var targetCat = _mainWindow?.GetSelectedOrActiveCategory() ?? TargetCategory;
                if (targetCat != null)
                {
                    if (!targetCat.Items.Any(i => i.Path.Equals(selected.FullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        targetCat.Items.Add(new AppItemModel { Path = selected.FullPath, IsFolder = false, BlockInbound = false, BlockOutbound = false });
                        AddedToProfile = true;
                        _mainWindow?.NotifyProfileItemAdded(targetCat);
                    }
                    MessageBox.Show($"🟢 '{selected.ProcessName}' için Windows Güvenlik Duvarı İzin Kuralı Oluşturuldu ve '{targetCat.Name}' profiline eklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"🟢 '{selected.ProcessName}' için Windows Güvenlik Duvarı İzin Verme Kuralı Oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadProcessesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kural oluşturulurken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected)
            {
                MessageBox.Show(LanguageManager.Get("TaskMgrSelectWarning"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                FirewallManager.RemoveRulesByPath(selected.FullPath);

                if (selected.AssociatedProfile != null && selected.MatchedProfileItem != null)
                {
                    if (!selected.MatchedProfileItem.IsFolder)
                    {
                        selected.AssociatedProfile.Items.Remove(selected.MatchedProfileItem);
                        _mainWindow?.NotifyProfileItemAdded(selected.AssociatedProfile);
                    }
                    else
                    {
                        string folderName = Path.GetFileName(selected.MatchedProfileItem.Path.TrimEnd('\\', '/'));
                        var res = MessageBox.Show(
                            $"⚠️ '{selected.ProcessName}' süreci '{selected.AssociatedProfile.Name}' profilindeki '{folderName}' KLASÖR kuralı kapsamındadır.\n\n" +
                            $"'{folderName}' klasör kuralını da '{selected.AssociatedProfile.Name}' profilinden tamamen silmek istiyor musunuz?\n\n" +
                            "• [Evet]: Klasör kuralını profilden siler.\n" +
                            "• [Hayır]: Sadece bu EXE'ye ait özel Windows Güvenlik Duvarı kuralını kaldırır.",
                            "Klasör Kuralı Algılandı",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (res == MessageBoxResult.Yes)
                        {
                            selected.AssociatedProfile.Items.Remove(selected.MatchedProfileItem);
                            _mainWindow?.NotifyProfileItemAdded(selected.AssociatedProfile);
                        }
                    }
                }

                MessageBox.Show($"🗑️ '{selected.ProcessName}' sürecine ait Güvenlik Duvarı kuralı başarıyla kaldırıldı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadProcessesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kural kaldırılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAddToProfile_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected)
            {
                MessageBox.Show(LanguageManager.Get("TaskMgrSelectWarning"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var targetCat = _mainWindow?.GetSelectedOrActiveCategory() ?? TargetCategory;

            if (targetCat == null)
            {
                MessageBox.Show("Lütfen ana penceredeki profil listesinden bir profil ekleyin veya seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!targetCat.Items.Any(i => i.Path.Equals(selected.FullPath, StringComparison.OrdinalIgnoreCase)))
            {
                targetCat.Items.Add(new AppItemModel { Path = selected.FullPath, IsFolder = false, BlockInbound = true, BlockOutbound = true });
                AddedToProfile = true;

                string appName = Path.GetFileNameWithoutExtension(selected.FullPath) + "_" + selected.FullPath.GetHashCode();
                FirewallManager.ApplyRule(appName, selected.FullPath, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: false);

                _mainWindow?.NotifyProfileItemAdded(targetCat);

                MessageBox.Show($"⛔ '{selected.ProcessName}' hem '{targetCat.Name}' profiline eklendi hem de Güvenlik Duvarı'nda Gelen & Giden Engellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadProcessesAsync();
            }
            else
            {
                MessageBox.Show($"'{selected.ProcessName}' zaten '{targetCat.Name}' profilinde mevcut.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnAddFolderToProfile_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected)
            {
                MessageBox.Show(LanguageManager.Get("TaskMgrSelectWarning"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var targetCat = _mainWindow?.GetSelectedOrActiveCategory() ?? TargetCategory;

            if (targetCat == null)
            {
                MessageBox.Show("Lütfen ana penceredeki profil listesinden bir profil ekleyin veya seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? folderPath = Path.GetDirectoryName(selected.FullPath);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show("Sürecin bulunduğu klasör konumu tespit edilemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!targetCat.Items.Any(i => i.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
            {
                targetCat.Items.Add(new AppItemModel { Path = folderPath, IsFolder = true, BlockInbound = true, BlockOutbound = true });
                AddedToProfile = true;

                await Task.Run(() =>
                {
                    var exeFiles = FileScanner.FindExeFiles(folderPath, null);
                    foreach (var exe in exeFiles)
                    {
                        if (!FirewallManager.IsProtectedSystemPath(exe))
                        {
                            string appName = Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
                            FirewallManager.ApplyRule(appName, exe, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: false);
                        }
                    }
                });

                _mainWindow?.NotifyProfileItemAdded(targetCat);

                MessageBox.Show($"📁 '{Path.GetFileName(folderPath)}' klasörü (tüm EXE'leriyle birlikte) '{targetCat.Name}' profiline eklendi ve Güvenlik Duvarı'nda Engellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadProcessesAsync();
            }
            else
            {
                MessageBox.Show($"'{Path.GetFileName(folderPath)}' klasörü zaten '{targetCat.Name}' profilinde mevcut.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnKillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected)
            {
                MessageBox.Show(LanguageManager.Get("TaskMgrSelectWarning"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"⚠️ '{selected.ProcessName}' (PID: {selected.Pid}) sürecini zorla sonlandırmak istediğinizden emin misiniz?", "Görevi Sonlandır Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    var proc = Process.GetProcessById(selected.Pid);
                    proc.Kill();
                    _allProcesses.Remove(selected);
                    _filteredProcesses.Remove(selected);
                    MessageBox.Show($"❌ '{selected.ProcessName}' süreci sonlandırıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Süreç sonlandırılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected || string.IsNullOrEmpty(selected.FullPath)) return;

            try
            {
                if (File.Exists(selected.FullPath))
                {
                    Process.Start("explorer.exe", $"/select,\"{selected.FullPath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Konum açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLocateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcesses.SelectedItem is not ProcessItemModel selected)
            {
                MessageBox.Show(LanguageManager.Get("TaskMgrSelectWarning"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selected.AssociatedProfile == null)
            {
                MessageBox.Show($"'{selected.ProcessName}' henüz hiçbir HaYTooL Firewall profiline eklenmemiş.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _mainWindow?.SelectCategory(selected.AssociatedProfile);
            _mainWindow?.Focus();
            MessageBox.Show($"🎯 '{selected.ProcessName}' süreci '{selected.AssociatedProfile.Name}' profilinde bulundu ve ana pencerede seçildi!", "Profil Bulundu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DgProcesses_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnOpenLocation_Click(sender, e);
        }
    }
}
