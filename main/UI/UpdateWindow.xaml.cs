using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using GuvenlikDuvarim.Core.Utils;

namespace GuvenlikDuvarim.UI
{
    public partial class UpdateWindow : Window
    {
        private readonly string _currentVersion;
        private readonly string _targetVersion;
        private string _downloadUrl = string.Empty;
        private bool _isUpdating = false;

        public UpdateWindow(string currentVersion, string targetVersion)
        {
            InitializeComponent();
            _currentVersion = currentVersion;
            _targetVersion = targetVersion;

            txtCurrentVersion.Text = _currentVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? _currentVersion : "v" + _currentVersion;
            txtNewVersion.Text = _targetVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? _targetVersion : "v" + _targetVersion;

            Loaded += async (s, e) => await ResolveDownloadUrlAsync();
        }

        private async Task ResolveDownloadUrlAsync()
        {
            try
            {
                txtStatus.Text = "GitHub sürüm detayları kontrol ediliyor...";
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.Add("User-Agent", "HaYTooL-Firewall-Updater");

                string apiUrl = "https://api.github.com/repos/HaYToKoRaZ/HaYTooL-Firewall/releases/latest";
                string json = await client.GetStringAsync(apiUrl);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            _downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(_downloadUrl))
                {
                    string cleanTag = _targetVersion.TrimStart('v');
                    _downloadUrl = $"https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/download/v{cleanTag}/HaYTooL_Firewall_v{cleanTag}_Portable.zip";
                }

                txtStatus.Text = "Son sürüm paketi bulundu. Güncellemeye hazır.";
            }
            catch (Exception ex)
            {
                LogManager.Log($"İndirme URL tespit hatası: {ex.Message}");
                string cleanTag = _targetVersion.TrimStart('v');
                _downloadUrl = $"https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/download/v{cleanTag}/HaYTooL_Firewall_v{cleanTag}_Portable.zip";
                txtStatus.Text = "Güncellemeye hazır.";
            }
        }

        private void BtnViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/latest",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
            {
                if (MessageBox.Show("İndirme işlemi devam ediyor. İptal etmek istediğinize emin misiniz?", "Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            Close();
        }

        private async void BtnStartUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            btnStartUpdate.IsEnabled = false;
            btnCancel.IsEnabled = false;

            try
            {
                await PerformUpdateAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güncelleme sırasında hata oluştu:\n{ex.Message}", "Güncelleme Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStartUpdate.IsEnabled = true;
                btnCancel.IsEnabled = true;
                _isUpdating = false;
                txtStatus.Text = "Güncelleme başarısız oldu.";
            }
        }

        private async Task PerformUpdateAsync()
        {
            if (string.IsNullOrWhiteSpace(_downloadUrl))
            {
                await ResolveDownloadUrlAsync();
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string tempDir = Path.Combine(Path.GetTempPath(), "HaYTooL_Update_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(tempDir, "update.zip");
            string extractDir = Path.Combine(tempDir, "extracted");
            Directory.CreateDirectory(extractDir);

            txtStatus.Text = "Yeni sürüm paketi indiriliyor...";

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(3);
                client.DefaultRequestHeaders.Add("User-Agent", "HaYTooL-Firewall-Updater");

                using var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int read;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        double percent = (double)totalRead / totalBytes.Value * 100.0;
                        Dispatcher.Invoke(() =>
                        {
                            pbProgress.Value = percent;
                            txtPercent.Text = $"{(int)percent}%";
                            txtStatus.Text = $"İndiriliyor: {(totalRead / 1024.0 / 1024.0):F2} MB / {(totalBytes.Value / 1024.0 / 1024.0):F2} MB";
                        });
                    }
                }
            }

            txtStatus.Text = "Arşiv açılıyor...";
            pbProgress.IsIndeterminate = true;
            await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true));

            // HaYTooL_Firewall.ini koruması: Arşiv içinde ini varsa kesinlikle hedef üzerine yazılmaması için siliyoruz!
            string extractedIni = Path.Combine(extractDir, "HaYTooL_Firewall.ini");
            if (File.Exists(extractedIni))
            {
                try { File.Delete(extractedIni); } catch { }
            }
            string extractedDataIni = Path.Combine(extractDir, "data.ini");
            if (File.Exists(extractedDataIni))
            {
                try { File.Delete(extractedDataIni); } catch { }
            }

            txtStatus.Text = "Güncelleme uygulanıyor ve yeniden başlatılıyor...";

            // Güncelleme uygulayıcı betiği (.cmd) oluştur
            // Bu betik mevcut süreci bekler, dosyaları kopyalar (ini hariç), ardından güncel exe'yi başlatır
            string updaterBat = Path.Combine(tempDir, "apply_update.cmd");
            int currentPid = Process.GetCurrentProcess().Id;
            string mainExeName = "HaYTooL Firewall.exe";
            string mainExeTarget = Path.Combine(appDir, mainExeName);

            string batContent = $@"@echo off
chcp 65001 >nul
timeout /t 1 /nobreak >nul

:waitloop
tasklist /fi ""pid eq {currentPid}"" 2>nul | find ""{currentPid}"" >nul
if %ERRORLEVEL% equ 0 (
    timeout /t 1 /nobreak >nul
    goto waitloop
)

:: Dosyaları uygulamanın bulunduğu dizine aktar (HaYTooL_Firewall.ini hariç tutulur)
robocopy ""{extractDir}"" ""{appDir}"" /E /XF *.ini data.ini HaYTooL_Firewall.ini /R:3 /W:1 >nul

:: Yeni güncel sürümü başlat
start """" ""{mainExeTarget}""

:: Geçici güncelleme klasörünü temizle
timeout /t 2 /nobreak >nul
(goto) 2>nul & rd /s /q ""{tempDir}""
exit
";

            File.WriteAllText(updaterBat, batContent, System.Text.Encoding.Default);

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{updaterBat}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi);

            // Mevcut uygulamayı kapat
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }
    }
}
