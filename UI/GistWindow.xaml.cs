using GuvenlikDuvarim.Core.I18n;
using GuvenlikDuvarim.Core.Storage;
using System;
using System.Diagnostics;
using System.Windows;

namespace GuvenlikDuvarim.UI
{
    public partial class GistWindow : Window
    {
        public bool Restored { get; private set; } = false;

        private bool _isInitializing = true;

        public GistWindow()
        {
            InitializeComponent();
            ApplyLanguageText();
            LoadSavedValues();
            _isInitializing = false;
        }

        private void AutoGist_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            bool isChecked = chkAutoGist.IsChecked == true;
            IniStorage.SaveValue("Settings", "AutoGistOnStartup", isChecked ? "True" : "False");
        }

        private void PbToken_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            string token = pbToken.Password.Trim();
            IniStorage.SaveValue("Settings", "GitHub", token);
        }

        private void BtnClearToken_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(pbToken.Password)) return;

            if (MessageBox.Show("Kayıtlı GitHub Token bilgisini silmek istediğinizden emin misiniz?", "Token Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                pbToken.Password = "";
                IniStorage.SaveValue("Settings", "GitHub", "");
                txtStatus.Text = "🗑️ Token başarıyla silindi.";
            }
        }

        private void ApplyLanguageText()
        {
            Title = LanguageManager.Get("GistTitle");
            txtGistTitle.Text = LanguageManager.Get("GistTitle");
            txtGistHowToUse.Text = LanguageManager.Get("GistHowToUse");
            lblPatToken.Text = LanguageManager.Get("GistPatTokenLabel");
            runGetTokenLink.Text = LanguageManager.Get("GistGetTokenLink");
            lblGistId.Text = LanguageManager.Get("GistIdLabel");
            chkAutoGist.Content = LanguageManager.Get("GistAutoSync");
            btnUpload.Content = LanguageManager.Get("GistUploadBtn");
            btnDownload.Content = LanguageManager.Get("GistDownloadBtn");
            btnOpenGistUrl.Content = LanguageManager.Get("GistOpenUrlBtn");
            if (btnClearToken != null)
            {
                btnClearToken.Content = LanguageManager.Get("GistClearTokenBtn");
                btnClearToken.ToolTip = LanguageManager.Get("GistClearTokenToolTip");
            }
        }

        private void LoadSavedValues()
        {
            string token = IniStorage.ReadValue("Settings", "GitHub", "");
            string gistId = IniStorage.ReadValue("Settings", "LastGistId", "");
            string autoGistStr = IniStorage.ReadValue("Settings", "AutoGistOnStartup", "False");

            pbToken.Password = token;
            txtGistId.Text = gistId;
            chkAutoGist.IsChecked = bool.TryParse(autoGistStr, out bool autoGist) && autoGist;
        }

        private void HyperlinkGetToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = "https://github.com/settings/tokens/new?description=HaYTooL_Firewall_Gist_Backup&scopes=gist";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tarayıcı açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenGistUrl_Click(object sender, RoutedEventArgs e)
        {
            string gistInput = txtGistId.Text.Trim();
            string gistUrl = IniStorage.ReadValue("Settings", "LastGistUrl", "");

            if (string.IsNullOrEmpty(gistUrl) && !string.IsNullOrEmpty(gistInput))
            {
                if (gistInput.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    gistUrl = gistInput;
                }
                else
                {
                    gistUrl = $"https://gist.github.com/{gistInput}";
                }
            }

            if (!string.IsNullOrEmpty(gistUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(gistUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Tarayıcı açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Açılacak kayıtlı bir Gist URL'si veya Gist ID'si bulunamadı.\nLütfen önce 'Gist'e Yükle' butonuna basın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            string token = pbToken.Password.Trim();
            string existingGistId = txtGistId.Text.Trim();

            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("GitHub Gist yüklemesi yapmak için Personal Access Token (PAT) gereklidir.\n\nLütfen yukarıdaki '🔑 1-Tıkla Token Al' bağlantısına tıklayarak 1 saniyede token alın ve kutucuğa yapıştırın.", "Token Gerekli", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Token ve Otomatik Gist Ayarını INI'ye kaydet
            IniStorage.SaveValue("Settings", "GitHub", token);
            IniStorage.SaveValue("Settings", "AutoGistOnStartup", chkAutoGist.IsChecked == true ? "True" : "False");

            txtStatus.Text = "⏳ Gist'e yükleniyor...";
            btnUpload.IsEnabled = false;
            btnDownload.IsEnabled = false;

            var result = await BackupManager.UploadToGistAsync(token, existingGistId);

            btnUpload.IsEnabled = true;
            btnDownload.IsEnabled = true;

            if (result.Success)
            {
                txtGistId.Text = result.GistId;
                IniStorage.SaveValue("Settings", "LastGistId", result.GistId);
                IniStorage.SaveValue("Settings", "LastGistUrl", result.GistUrl);
                txtStatus.Text = $"✅ Yüklendi! Gist ID: {result.GistId}";
            }
            else
            {
                txtStatus.Text = "❌ Yükleme başarısız.";
                MessageBox.Show(result.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            string gistInput = txtGistId.Text.Trim();
            string token = pbToken.Password.Trim();

            if (string.IsNullOrEmpty(gistInput))
            {
                MessageBox.Show("Lütfen indirmek istediğiniz Gist ID veya URL adresini girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtStatus.Text = "⏳ Gist indiriliyor...";
            btnUpload.IsEnabled = false;
            btnDownload.IsEnabled = false;

            var result = await BackupManager.DownloadFromGistAsync(gistInput, token);

            btnUpload.IsEnabled = true;
            btnDownload.IsEnabled = true;

            if (result.Success)
            {
                IniStorage.SaveValue("Settings", "LastGistId", gistInput);
                if (!string.IsNullOrEmpty(token)) IniStorage.SaveValue("Settings", "GitHub", token);
                IniStorage.SaveValue("Settings", "AutoGistOnStartup", chkAutoGist.IsChecked == true ? "True" : "False");

                Restored = true;
                txtStatus.Text = "✅ Geri yükleme başarılı!";
                MessageBox.Show(result.Message, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                txtStatus.Text = "❌ İndirme başarısız.";
                MessageBox.Show(result.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
