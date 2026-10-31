using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using GuvenlikDuvarim.Core.Firewall;
using GuvenlikDuvarim.Core.Guard;
using GuvenlikDuvarim.Core.Services;
using GuvenlikDuvarim.Core.Startup;

namespace GuvenlikDuvarim.UI
{
    public partial class StartupGuardAlertWindow : Window
    {
        private readonly GuardEntryInfo _info;
        private bool _decisionHandled = false;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public StartupGuardAlertWindow(GuardEntryInfo info)
        {
            InitializeComponent();
            _info = info;

            txtEventTitle.Text = _info.EventTitle;
            txtType.Text = _info.Type;
            txtName.Text = _info.Name;
            txtCommand.Text = _info.CommandOrPath;
            txtLocation.Text = _info.Location;

            Loaded += StartupGuardAlertWindow_Loaded;
        }

        private void StartupGuardAlertWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ShowWindow(hwnd, SW_RESTORE);
                    BringWindowToTop(hwnd);
                    SetForegroundWindow(hwnd);
                }
                Topmost = false;
                Topmost = true;
                Activate();
                Focus();
            }
            catch { }
        }

        private void BtnAllow_Click(object sender, RoutedEventArgs e)
        {
            _decisionHandled = true;
            StartupGuardManager.CompleteAlertDecision(_info, existsAfterDecision: true, isEnabledAfterDecision: true);
            Close();
        }

        private void BtnDisable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_info.RawItem is StartupItemModel startupItem)
                {
                    StartupManager.DisableStartupItem(startupItem);
                }
                else if (_info.RawItem is ServiceItemModel serviceItem)
                {
                    ServiceManager.StopService(serviceItem.ServiceName);
                    ServiceManager.DisableService(serviceItem.ServiceName);
                }

                _decisionHandled = true;
                StartupGuardManager.CompleteAlertDecision(_info, existsAfterDecision: true, isEnabledAfterDecision: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İşlem sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Close();
        }

        private void BtnRemoveOnly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool remainsInList = false;

                if (_info.RawItem is StartupItemModel startupItem)
                {
                    StartupManager.DeleteStartupItem(startupItem);
                    // UWP uygulamalarının paket manifestosu silinemez, devre dışı olarak kalır
                    remainsInList = startupItem.IsUwp;
                }
                else if (_info.RawItem is ServiceItemModel serviceItem)
                {
                    ServiceManager.StopService(serviceItem.ServiceName);
                    if (!serviceItem.IsMicrosoftService && _info.WasNewlyAdded)
                    {
                        bool deleted = ServiceManager.DeleteService(serviceItem.ServiceName);
                        if (!deleted)
                        {
                            ServiceManager.DisableService(serviceItem.ServiceName);
                            remainsInList = true;
                        }
                    }
                    else
                    {
                        ServiceManager.DisableService(serviceItem.ServiceName);
                        remainsInList = true;
                    }
                }

                _decisionHandled = true;
                StartupGuardManager.CompleteAlertDecision(_info, existsAfterDecision: remainsInList, isEnabledAfterDecision: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaldırma işlemi sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Close();
        }

        private void BtnBlockAndDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool remainsInList = false;

                // 1. Başlangıç veya Servis kaydını kaldır / devre dışı bırak
                if (_info.RawItem is StartupItemModel startupItem)
                {
                    StartupManager.DeleteStartupItem(startupItem);
                    remainsInList = startupItem.IsUwp;
                }
                else if (_info.RawItem is ServiceItemModel serviceItem)
                {
                    ServiceManager.StopService(serviceItem.ServiceName);
                    if (!serviceItem.IsMicrosoftService && _info.WasNewlyAdded)
                    {
                        bool deleted = ServiceManager.DeleteService(serviceItem.ServiceName);
                        if (!deleted)
                        {
                            ServiceManager.DisableService(serviceItem.ServiceName);
                            remainsInList = true;
                        }
                    }
                    else
                    {
                        ServiceManager.DisableService(serviceItem.ServiceName);
                        remainsInList = true;
                    }
                }

                // 2. Eğer yürütülebilir bir .exe varsa Güvenlik Duvarında iki yönlü engelle
                if (!string.IsNullOrWhiteSpace(_info.CleanExePath) && File.Exists(_info.CleanExePath))
                {
                    string ruleKey = FirewallManager.GetAppRuleKey(_info.CleanExePath);
                    FirewallManager.ApplyRule(ruleKey, _info.CleanExePath, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: false);
                }

                _decisionHandled = true;
                StartupGuardManager.CompleteAlertDecision(_info, existsAfterDecision: remainsInList, isEnabledAfterDecision: false);

                MessageBox.Show($"🛡️ '{_info.Name}' başlangıçtan temizlendi ve ağ erişimi Güvenlik Duvarı'nda tamamen ENGELLENDİ!", "Tehdit Engellendi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İşlem sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!_decisionHandled)
            {
                // Pencere X ile kapatıldıysa mevcut haliyle kaydet ki tekrar tekrar sormasın
                StartupGuardManager.CompleteAlertDecision(_info, existsAfterDecision: true, isEnabledAfterDecision: true);
            }
            base.OnClosed(e);
        }
    }
}
