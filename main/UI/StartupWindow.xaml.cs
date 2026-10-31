using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GuvenlikDuvarim.Core.Firewall;
using GuvenlikDuvarim.Core.Guard;
using GuvenlikDuvarim.Core.Startup;
using GuvenlikDuvarim.Core.Storage;
using Microsoft.Win32;

namespace GuvenlikDuvarim.UI
{
    public partial class StartupWindow : Window
    {
        private List<StartupItemModel> _allItems = new();
        private List<StartupItemModel> _filteredItems = new();
        private string _lastStateSignature = string.Empty;
        private readonly DispatcherTimer _liveSyncTimer;
        private bool _isLoading = false;
        private bool _suppressGuardCheckEvent = false;

        public StartupWindow()
        {
            InitializeComponent();

            _liveSyncTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };
            _liveSyncTimer.Tick += LiveSyncTimer_Tick;

            Loaded += StartupWindow_Loaded;
            Closed += StartupWindow_Closed;
        }

        private async void StartupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SyncGuardToggleFromIni();
            StartupGuardManager.SystemStateChanged += OnGuardSystemStateChanged;
            await LoadStartupItemsAsync(showScanningText: true);
            _liveSyncTimer.Start();
        }

        private void StartupWindow_Closed(object? sender, EventArgs e)
        {
            _liveSyncTimer.Stop();
            StartupGuardManager.SystemStateChanged -= OnGuardSystemStateChanged;
        }

        private void SyncGuardToggleFromIni()
        {
            _suppressGuardCheckEvent = true;
            try
            {
                string val = IniStorage.ReadValue("Settings", "EnableStartupGuard", "True");
                bool enabled = !string.Equals(val, "False", StringComparison.OrdinalIgnoreCase);
                StartupGuardManager.IsEnabled = enabled;
                chkGuardToggle.IsChecked = enabled;
                chkGuardToggle.Content = enabled ? "🛡️ Başlangıç Koruması Aktif" : "⚪ Başlangıç Koruması Pasif";
            }
            catch { }
            finally
            {
                _suppressGuardCheckEvent = false;
            }
        }

        private void ChkGuardToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressGuardCheckEvent) return;
            bool enabled = chkGuardToggle.IsChecked == true;
            StartupGuardManager.IsEnabled = enabled;
            chkGuardToggle.Content = enabled ? "🛡️ Başlangıç Koruması Aktif" : "⚪ Başlangıç Koruması Pasif";
            try
            {
                IniStorage.SaveValue("Settings", "EnableStartupGuard", enabled ? "True" : "False");
            }
            catch { }
        }

        private async void OnGuardSystemStateChanged()
        {
            await LoadStartupItemsAsync(showScanningText: false);
        }

        private async void LiveSyncTimer_Tick(object? sender, EventArgs e)
        {
            if (_isLoading || !IsVisible) return;
            await LoadStartupItemsAsync(showScanningText: false, onlyUpdateIfChanged: true);
        }

        private static string ComputeSignature(List<StartupItemModel> items)
        {
            return string.Join("\n", items.Select(x => $"{x.UniqueKey}|{x.IsEnabled}|{x.Command}"));
        }

        private async Task LoadStartupItemsAsync(bool showScanningText = true, bool onlyUpdateIfChanged = false)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                if (showScanningText && txtStartupCount != null)
                {
                    txtStartupCount.Text = "Taranıyor...";
                }

                string? previousSelectedKey = (dgStartup?.SelectedItem as StartupItemModel)?.UniqueKey;

                var freshList = await Task.Run(() => StartupManager.GetAllStartupItems(includeTaskScheduler: true));
                string freshSig = ComputeSignature(freshList);

                if (onlyUpdateIfChanged && string.Equals(freshSig, _lastStateSignature, StringComparison.Ordinal))
                {
                    return;
                }

                _lastStateSignature = freshSig;
                _allItems = freshList;
                UpdateGroupCounts();
                FilterItems(previousSelectedKey);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateGroupCounts()
        {
            int allCount = _allItems.Count;
            int allUsersCount = _allItems.Count(x => x.CategoryGroup == StartupManager.GroupAllUsers);
            int currentUserCount = _allItems.Count(x => x.CategoryGroup == StartupManager.GroupCurrentUser);
            int uwpCount = _allItems.Count(x => x.CategoryGroup == StartupManager.GroupUwp);
            int taskSchedCount = _allItems.Count(x => x.CategoryGroup == StartupManager.GroupTaskScheduler);

            rbGroupAll.Content = $"🌐 Tümü ({allCount})";
            rbGroupAllUsers.Content = $"👥 All Users - Registry Entries ({allUsersCount})";
            rbGroupCurrentUser.Content = $"👤 Current User - Registry Entries ({currentUserCount})";
            rbGroupUwp.Content = $"📦 UWP App ({uwpCount})";
            rbGroupTaskScheduler.Content = $"⏰ Task Scheduler ({taskSchedCount})";
        }

        private void FilterItems(string? restoreUniqueKey = null)
        {
            string? selectedKey = restoreUniqueKey ?? (dgStartup?.SelectedItem as StartupItemModel)?.UniqueKey;

            string query = txtSearch?.Text?.Trim() ?? string.Empty;
            bool enabledOnly = chkEnabledOnly?.IsChecked == true;
            bool disabledOnly = chkDisabledOnly?.IsChecked == true;

            var q = _allItems.AsEnumerable();

            // Kategori Grubu Filtresi
            if (rbGroupAllUsers?.IsChecked == true)
            {
                q = q.Where(x => x.CategoryGroup == StartupManager.GroupAllUsers);
            }
            else if (rbGroupCurrentUser?.IsChecked == true)
            {
                q = q.Where(x => x.CategoryGroup == StartupManager.GroupCurrentUser);
            }
            else if (rbGroupUwp?.IsChecked == true)
            {
                q = q.Where(x => x.CategoryGroup == StartupManager.GroupUwp);
            }
            else if (rbGroupTaskScheduler?.IsChecked == true)
            {
                q = q.Where(x => x.CategoryGroup == StartupManager.GroupTaskScheduler);
            }

            // Durum Filtresi
            if (enabledOnly) q = q.Where(x => x.IsEnabled);
            if (disabledOnly) q = q.Where(x => !x.IsEnabled);

            // Arama Filtresi
            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(x => (x.Name != null && x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                 (x.Command != null && x.Command.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                 (x.LocationDetail != null && x.LocationDetail.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                                 (x.CategoryGroup != null && x.CategoryGroup.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

            _filteredItems = q.ToList();
            if (dgStartup != null)
            {
                dgStartup.ItemsSource = _filteredItems;
                if (!string.IsNullOrWhiteSpace(selectedKey))
                {
                    var match = _filteredItems.FirstOrDefault(x => string.Equals(x.UniqueKey, selectedKey, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        dgStartup.SelectedItem = match;
                    }
                }
            }

            if (txtStartupCount != null)
            {
                int enabledTotal = _allItems.Count(x => x.IsEnabled);
                int disabledTotal = _allItems.Count - enabledTotal;
                txtStartupCount.Text = $"{_filteredItems.Count} Öğe ({enabledTotal} Etkin, {disabledTotal} Devre Dışı)";
            }
        }

        private void ChkEnabledOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (chkDisabledOnly != null && chkDisabledOnly.IsChecked == true)
            {
                chkDisabledOnly.IsChecked = false;
            }
            FilterItems();
        }

        private void ChkDisabledOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (chkEnabledOnly != null && chkEnabledOnly.IsChecked == true)
            {
                chkEnabledOnly.IsChecked = false;
            }
            FilterItems();
        }

        private void GroupRadio_Checked(object sender, RoutedEventArgs e) => FilterItems();
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterItems();
        private void Filter_Changed(object sender, RoutedEventArgs e) => FilterItems();
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadStartupItemsAsync(showScanningText: true);

        private StartupItemModel? GetSelectedItem()
        {
            if (dgStartup.SelectedItem is StartupItemModel selected) return selected;
            MessageBox.Show("Lütfen önce listeden bir başlangıç öğesi seçin.", "Seçim Gerekli", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        private async void MenuEnable_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null) return;

            bool ok = await Task.Run(() => StartupManager.EnableStartupItem(item));
            if (ok)
            {
                StartupGuardManager.RecordUserAuthorizedStartup(item);
                _lastStateSignature = ComputeSignature(_allItems);
                FilterItems(item.UniqueKey);
                await LoadStartupItemsAsync(showScanningText: false);
            }
            else
            {
                MessageBox.Show("Başlangıç öğesi etkinleştirilemedi. Yönetici izinlerini kontrol edin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void MenuDisable_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null) return;

            bool ok = await Task.Run(() => StartupManager.DisableStartupItem(item));
            if (ok)
            {
                StartupGuardManager.RecordUserAuthorizedStartup(item);
                _lastStateSignature = ComputeSignature(_allItems);
                FilterItems(item.UniqueKey);
                await LoadStartupItemsAsync(showScanningText: false);
            }
            else
            {
                MessageBox.Show("Başlangıç öğesi devre dışı bırakılamadı. Yönetici izinlerini kontrol edin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null) return;

            if (item.IsUwp)
            {
                if (MessageBox.Show(
                    $"'{item.Name}' bir Microsoft Store / UWP uygulamasıdır.\n\nUWP paketlerinin dahili manifestosu silinemediği için başlangıç görevi güvenli şekilde DEVRE DIŞI bırakılacaktır. Devam edilsin mi?",
                    "UWP Başlangıç Görevi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                bool uwpOk = await Task.Run(() => StartupManager.DisableStartupItem(item));
                if (uwpOk)
                {
                    StartupGuardManager.RecordUserAuthorizedStartup(item);
                    await LoadStartupItemsAsync(showScanningText: false);
                }
                else
                {
                    MessageBox.Show("UWP başlangıç görevi devre dışı bırakılamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            if (MessageBox.Show($"'{item.Name}' başlangıç kaydını kalıcı olarak silmek istediğinize emin misiniz?", "Kayıt Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            bool ok = await Task.Run(() => StartupManager.DeleteStartupItem(item));
            if (ok)
            {
                StartupGuardManager.RecordUserRemovedStartup(item.UniqueKey);
                await LoadStartupItemsAsync(showScanningText: false);
                MessageBox.Show($"'{item.Name}' başlangıçtan tamamen kaldırıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Kayıt silinemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null) return;

            string target = !string.IsNullOrWhiteSpace(item.CleanExePath) ? item.CleanExePath : item.FilePath;
            if (!string.IsNullOrWhiteSpace(target) && File.Exists(target))
            {
                try
                {
                    Process.Start("explorer.exe", $"/select,\"{target}\"");
                }
                catch { }
            }
            else
            {
                MessageBox.Show($"Dosya bulunamadı:\n{item.Command}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuBlockInFirewall_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null) return;

            if (string.IsNullOrWhiteSpace(item.CleanExePath) || !File.Exists(item.CleanExePath))
            {
                MessageBox.Show("Geçerli bir .exe dosyası bulunamadı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string appKey = FirewallManager.GetAppRuleKey(item.CleanExePath);
            FirewallManager.ApplyRule(appKey, item.CleanExePath, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: false);
            MessageBox.Show($"'{item.CleanExePath}' güvenlik duvarında tamamen engellendi!", "Güvenlik Duvarı Engeli", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuAllowInFirewall_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null) return;

            if (string.IsNullOrWhiteSpace(item.CleanExePath) || !File.Exists(item.CleanExePath))
            {
                MessageBox.Show("Geçerli bir .exe dosyası bulunamadı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string appKey = FirewallManager.GetAppRuleKey(item.CleanExePath);
            FirewallManager.ApplyRule(appKey, item.CleanExePath, blockInbound: false, blockOutbound: false, isEnabled: true, isAllow: true);
            MessageBox.Show($"'{item.CleanExePath}' güvenlik duvarında izin verildi.", "Güvenlik Duvarı İzni", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnAddNew_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Başlangıçta Çalıştırılacak Programı Seçin",
                Filter = "Yürütülebilir Dosyalar (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|Tüm Dosyalar (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                string name = Path.GetFileNameWithoutExtension(ofd.FileName);
                bool ok = StartupManager.AddStartupItem(name, ofd.FileName);
                if (ok)
                {
                    await LoadStartupItemsAsync(showScanningText: false);
                    MessageBox.Show($"'{name}' başarıyla başlangıç listesine eklendi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Başlangıç kaydı eklenemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
