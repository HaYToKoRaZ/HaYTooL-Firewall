using System.Text;
using System.Windows;
using GuvenlikDuvarim.Core.Storage;
using GuvenlikDuvarim.Core.Utils;

namespace GuvenlikDuvarim.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _settings;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            chkStartWithWindows.IsChecked = _settings.StartWithWindows;
            chkStartMinimizedToTray.IsChecked = _settings.StartMinimizedToTray;
            chkMinimizeToTray.IsChecked = _settings.MinimizeToTray;
            chkEnableToast.IsChecked = _settings.EnableToastNotifications;
            chkEnableStartupGuard.IsChecked = _settings.EnableStartupGuard;

            chkStartMinimizedToTray.IsEnabled = chkStartWithWindows.IsChecked == true;
            chkStartWithWindows.Checked += (s, e) => chkStartMinimizedToTray.IsEnabled = true;
            chkStartWithWindows.Unchecked += (s, e) =>
            {
                chkStartMinimizedToTray.IsEnabled = false;
                chkStartMinimizedToTray.IsChecked = false;
            };

            LoadCliHelp();
        }

        private void LoadCliHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# HaYTooL Firewall CLI Rehberi");
            sb.AppendLine("haytool sync --all                    # Tüm profillerdeki klasörleri senkronize et");
            sb.AppendLine("haytool sync --profile \"Profil Adı\"   # Belirli bir profili senkronize et");
            sb.AppendLine("haytool profile --enable \"Profil\"    # Profili ve kurallarını aktif et");
            sb.AppendLine("haytool profile --disable \"Profil\"   # Profili pasife al, kurallarını kaldır");
            sb.AppendLine("haytool firewall --status             # Güvenlik duvarı durumunu göster");
            sb.AppendLine("haytool fullsafe --on                 # FullSafe modunu (Zero-Trust) aç");
            sb.AppendLine("haytool fullsafe --off                # FullSafe modunu kapat");
            sb.AppendLine("haytool taskmgr                       # Çift motorlu Görev Yöneticisini aç");
            txtCliCommands.Text = sb.ToString();
        }

        private void BtnCopyCli_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtCliCommands.Text);
            MessageBox.Show("CLI komutları panoya kopyalandı!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _settings.StartWithWindows = chkStartWithWindows.IsChecked == true;
            _settings.StartMinimizedToTray = chkStartMinimizedToTray.IsChecked == true;
            _settings.MinimizeToTray = chkMinimizeToTray.IsChecked == true;
            _settings.EnableToastNotifications = chkEnableToast.IsChecked == true;
            _settings.EnableStartupGuard = chkEnableStartupGuard.IsChecked == true;
            GuvenlikDuvarim.Core.Guard.StartupGuardManager.IsEnabled = _settings.EnableStartupGuard;

            AutoStartManager.SetAutoStart(_settings.StartWithWindows, _settings.StartMinimizedToTray);

            DialogResult = true;
            Close();
        }
    }
}
