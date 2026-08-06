using GuvenlikDuvarim.Core.I18n;
using GuvenlikDuvarim.Core.Storage;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GuvenlikDuvarim.UI
{
    public class BackupItemModel
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string FormattedDate => DateCreated.ToString("yyyy-MM-dd HH:mm:ss");
        public long FileSize { get; set; }
        public string FormattedSize => $"{FileSize / 1024.0:F1} KB";
    }

    public partial class BackupWindow : Window
    {
        public bool Restored { get; private set; } = false;
        private bool _isInitializing = true;

        public BackupWindow()
        {
            InitializeComponent();
            ApplyLanguageText();
            LoadSettings();
            txtCustomBackupName.Text = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_";
            RefreshBackupList();
            _isInitializing = false;
        }

        private void ApplyLanguageText()
        {
            Title = LanguageManager.Get("BackupTitle");
            txtBackupTitle.Text = LanguageManager.Get("BackupTitle");
            lblBackupCustomName.Text = LanguageManager.Get("BackupCustomName");
            btnCreateBackupNow.Content = LanguageManager.Get("BackupManualBtn");
            chkAutoBackupOnStartup.Content = LanguageManager.Get("BackupAutoCheck");
            lblMaxBackupCount.Text = LanguageManager.Get("BackupMaxCountLabel");
            lblBackupListHeader.Text = LanguageManager.Get("BackupListHeader");
            btnDeleteBackup.Content = LanguageManager.Get("BackupDeleteBtn");
            btnRestoreBackup.Content = LanguageManager.Get("BackupRestoreBtn");

            if (dgBackups != null && dgBackups.Columns.Count >= 3)
            {
                dgBackups.Columns[0].Header = LanguageManager.Get("ColBackupName");
                dgBackups.Columns[1].Header = LanguageManager.Get("ColBackupDate");
                dgBackups.Columns[2].Header = LanguageManager.Get("ColBackupSize");
            }
        }

        private void LoadSettings()
        {
            string autoBackupStr = IniStorage.ReadValue("Settings", "AutoBackupOnStartup", "True");
            string maxBackupStr = IniStorage.ReadValue("Settings", "MaxBackupCount", "30");

            chkAutoBackupOnStartup.IsChecked = bool.TryParse(autoBackupStr, out bool b) ? b : true;
            txtMaxBackupCount.Text = maxBackupStr;
        }

        private void SettingsChanged(object sender, TextChangedEventArgs e) => SaveSettings();
        private void SettingsChanged(object sender, RoutedEventArgs e) => SaveSettings();

        private void SaveSettings()
        {
            if (_isInitializing) return;
            string autoBackup = chkAutoBackupOnStartup.IsChecked == true ? "True" : "False";
            string maxBackup = int.TryParse(txtMaxBackupCount.Text.Trim(), out int m) ? m.ToString() : "30";

            IniStorage.SaveValue("Settings", "AutoBackupOnStartup", autoBackup);
            IniStorage.SaveValue("Settings", "MaxBackupCount", maxBackup);
        }

        private void RefreshBackupList()
        {
            try
            {
                string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var files = new DirectoryInfo(backupDir)
                    .GetFiles("HaYTooL_Backup_*.ini")
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => new BackupItemModel
                    {
                        FilePath = f.FullName,
                        FileName = f.Name,
                        DateCreated = f.CreationTime,
                        FileSize = f.Length
                    })
                    .ToList();

                dgBackups.ItemsSource = files;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yedekler listelenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCreateBackupNow_Click(object sender, RoutedEventArgs e)
        {
            string customNote = txtCustomBackupName.Text.Trim();
            if (string.IsNullOrWhiteSpace(customNote))
            {
                customNote = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            }

            bool ok = BackupManager.CreateManualBackup(customNote);
            if (ok)
            {
                RefreshBackupList();
                txtCustomBackupName.Text = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_";
                MessageBox.Show("Yeni manuel yerel yedek 'backup/' klasörüne kaydedildi!", "Yedek Alındı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnDeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            if (dgBackups.SelectedItem is BackupItemModel item)
            {
                if (MessageBox.Show($"'{item.FileName}' yedeğini silmek istediğinizden emin misiniz?", "Yedek Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (File.Exists(item.FilePath))
                        {
                            File.Delete(item.FilePath);
                            RefreshBackupList();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Yedek silinirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen listeden silinecek bir yedek seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (dgBackups.SelectedItem is BackupItemModel item)
            {
                if (MessageBox.Show($"Uygulama ayarlarınız ve profilleriniz '{item.FileName}' yedeğindeki verilerle değiştirilecek. Onaylıyor musunuz?", "Yedek Geri Yükle", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string currentIni = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HaYTooL_Firewall.ini");
                        File.Copy(item.FilePath, currentIni, true);
                        Restored = true;
                        MessageBox.Show("Yerel yedek başarıyla geri yüklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Yedek geri yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen listeden geri yüklenecek bir yedek seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
