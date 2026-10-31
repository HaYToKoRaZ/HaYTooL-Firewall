using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GuvenlikDuvarim.Core.Firewall;
using GuvenlikDuvarim.Core.Guard;
using GuvenlikDuvarim.Core.Services;

namespace GuvenlikDuvarim.UI
{
    public partial class ServicesWindow : Window
    {
        private List<ServiceItemModel> _allServices = new();
        private List<ServiceItemModel> _filteredServices = new();
        private bool _isLoading = false;

        public ServicesWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                StartupGuardManager.SystemStateChanged += OnGuardSystemStateChanged;
                await LoadServicesAsync();
            };
            Closed += (s, e) =>
            {
                StartupGuardManager.SystemStateChanged -= OnGuardSystemStateChanged;
            };
        }

        private async void OnGuardSystemStateChanged()
        {
            await LoadServicesAsync();
        }

        private async Task LoadServicesAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                string? prevSelected = (dgServices?.SelectedItem as ServiceItemModel)?.ServiceName;
                txtServiceCount.Text = "Taranıyor...";
                _allServices = await Task.Run(() => ServiceManager.GetAllServices());
                FilterServices(prevSelected);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void FilterServices(string? restoreServiceName = null)
        {
            string? selectedName = restoreServiceName ?? (dgServices?.SelectedItem as ServiceItemModel)?.ServiceName;
            string query = txtSearch.Text.Trim();
            bool runningOnly = chkRunningOnly.IsChecked == true;
            bool stoppedOnly = chkStoppedOnly.IsChecked == true;
            bool autoOnly = chkAutoOnly.IsChecked == true;
            bool disabledOnly = chkDisabledOnly.IsChecked == true;
            bool hideMs = chkHideMicrosoft.IsChecked == true;

            var q = _allServices.AsEnumerable();

            if (hideMs)
            {
                q = q.Where(s => !s.IsMicrosoftService);
            }
            if (runningOnly)
            {
                q = q.Where(s => s.IsRunning);
            }
            if (stoppedOnly)
            {
                q = q.Where(s => !s.IsRunning);
            }
            if (autoOnly)
            {
                q = q.Where(s => s.StartupType == "Otomatik");
            }
            if (disabledOnly)
            {
                q = q.Where(s => s.StartupType == "Devre Dışı");
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(s => s.ServiceName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 s.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                 s.CleanExePath.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            _filteredServices = q.ToList();
            if (dgServices != null)
            {
                dgServices.ItemsSource = _filteredServices;
                if (!string.IsNullOrWhiteSpace(selectedName))
                {
                    var match = _filteredServices.FirstOrDefault(x => string.Equals(x.ServiceName, selectedName, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        dgServices.SelectedItem = match;
                    }
                }
            }
            if (txtServiceCount != null)
            {
                txtServiceCount.Text = $"{_filteredServices.Count} Hizmet Listelendi (Toplam: {_allServices.Count})";
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterServices();
        private void Filter_Changed(object sender, RoutedEventArgs e) => FilterServices();
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadServicesAsync();

        private ServiceItemModel? GetSelectedService()
        {
            if (dgServices.SelectedItem is ServiceItemModel selected) return selected;
            MessageBox.Show("Lütfen önce listeden bir hizmet seçin.", "Hizmet Seçimi", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        private async void MenuStart_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSelectedService();
            if (s == null) return;
            bool ok = await Task.Run(() => ServiceManager.StartService(s.ServiceName));
            if (!ok) MessageBox.Show($"'{s.DisplayName}' başlatılamadı. Yönetici izinlerini kontrol edin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            await LoadServicesAsync();
        }

        private async void MenuStop_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSelectedService();
            if (s == null) return;
            bool ok = await Task.Run(() => ServiceManager.StopService(s.ServiceName));
            if (!ok) MessageBox.Show($"'{s.DisplayName}' durdurulamadı. Başka bir hizmete bağımlı olabilir.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            await LoadServicesAsync();
        }

        private async void MenuRestart_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSelectedService();
            if (s == null) return;
            await Task.Run(() => ServiceManager.RestartService(s.ServiceName));
            await LoadServicesAsync();
        }

        private async void MenuSetAuto_Click(object sender, RoutedEventArgs e) => await SetStartupType("auto");
        private async void MenuSetManual_Click(object sender, RoutedEventArgs e) => await SetStartupType("demand");
        private async void MenuSetDisabled_Click(object sender, RoutedEventArgs e) => await SetStartupType("disabled");

        private async void BtnApplyStartupType_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStartupTypeAction.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                await SetStartupType(tag);
            }
        }

        private async Task SetStartupType(string type)
        {
            var s = GetSelectedService();
            if (s == null) return;
            bool ok = await Task.Run(() => ServiceManager.SetStartupType(s.ServiceName, type));
            if (ok)
            {
                StartupGuardManager.RecordUserAuthorizedService(s, isEnabled: !string.Equals(type, "disabled", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                MessageBox.Show($"Başlangıç türü değiştirilemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            await LoadServicesAsync();
        }

        private async void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSelectedService();
            if (s == null) return;

            if (s.IsMicrosoftService)
            {
                MessageBox.Show($"'{s.DisplayName}' bir Windows sistem hizmetidir ve silinmesi sistem çökmesine yol açabilir!", "Kritik Sistem Hizmeti", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"'{s.DisplayName}' ({s.ServiceName}) hizmetini kalıcı olarak sistemden silmek istediğinize emin misiniz?", "Hizmet Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            bool ok = await Task.Run(() => ServiceManager.DeleteService(s.ServiceName));
            if (ok)
            {
                StartupGuardManager.RecordUserRemovedService(s.ServiceName);
                MessageBox.Show($"'{s.DisplayName}' hizmeti silindi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Hizmet silinemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            await LoadServicesAsync();
        }

        private void MenuBlockInFirewall_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSelectedService();
            if (s == null) return;
            if (string.IsNullOrWhiteSpace(s.CleanExePath) || !File.Exists(s.CleanExePath))
            {
                MessageBox.Show("Bu hizmetin doğrudan bir .exe dosyası bulunamadı (svchost veya DLL tabanlı olabilir).", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string appKey = FirewallManager.GetAppRuleKey(s.CleanExePath);
            FirewallManager.ApplyRule(appKey, s.CleanExePath, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: false);
            MessageBox.Show($"'{s.CleanExePath}' güvenlik duvarında tamamen engellendi!", "Güvenlik Duvarı Engeli", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuAllowInFirewall_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSelectedService();
            if (s == null) return;
            if (string.IsNullOrWhiteSpace(s.CleanExePath) || !File.Exists(s.CleanExePath))
            {
                MessageBox.Show("Bu hizmetin doğrudan bir .exe dosyası bulunamadı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string appKey = FirewallManager.GetAppRuleKey(s.CleanExePath);
            FirewallManager.ApplyRule(appKey, s.CleanExePath, blockInbound: false, blockOutbound: false, isEnabled: true, isAllow: true);
            MessageBox.Show($"'{s.CleanExePath}' güvenlik duvarında izin verildi.", "Güvenlik Duvarı İzni", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            var s = GetSelectedService();
            if (s == null || string.IsNullOrWhiteSpace(s.CleanExePath) || !File.Exists(s.CleanExePath))
            {
                MessageBox.Show("Yürütülebilir dosya yolu bulunamadı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Process.Start("explorer.exe", $"/select,\"{s.CleanExePath}\"");
            }
            catch { }
        }
    }
}
