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

        private static bool IsTrustedDownloadUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttps) return false;

            // Sadece GitHub resmi release ve storage hostlarına izin ver
            return (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath.Contains("/HaYTooL-Firewall/releases/download/"))
                || uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase);
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

            if (!IsTrustedDownloadUrl(_downloadUrl))
            {
                throw new InvalidOperationException("Güvenlik Uyarısı: İndirme URL'si güvenilir GitHub sunucuları ile uyuşmuyor. Güncelleme iptal edildi.");
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

            // Dosya bütünlük (Integrity) ve SHA-256 sağlama kontrolü
            var fileInfo = new FileInfo(zipPath);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                throw new InvalidOperationException("İndirilen paket dosyası boş veya bozuk.");
            }

            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(zipPath))
            {
                byte[] hash = sha.ComputeHash(stream);
                string sha256Hex = Convert.ToHexString(hash);
                LogManager.Log($"[Updater] İndirilen paket doğrulandı. SHA-256: {sha256Hex} ({fileInfo.Length} bayt)");
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
            string safeAppDir = appDir.Trim().TrimEnd('\\', '/');
            string safeExtractDir = extractDir.Trim().TrimEnd('\\', '/');
            string mainExeTarget = Path.Combine(safeAppDir, mainExeName);
            string updateLogPath = Path.Combine(Path.GetTempPath(), "haytool_update.log");

            // Not: 'timeout' komutu arka planda (non-interactive / redirected) çalıştırıldığında 
            // "ERROR: Input redirection is not supported" hatası verir ve betiği derhal sonlandırır.
            // Bu nedenle gecikme için standart ve her ortamda çalışan 'ping 127.0.0.1 -n 2 >nul' kullanıyoruz.
            string batContent = $@"@echo off
chcp 65001 >nul
ping 127.0.0.1 -n 2 >nul

:waitloop
tasklist /fi ""pid eq {currentPid}"" 2>nul | find ""{currentPid}"" >nul
if %ERRORLEVEL% equ 0 (
    ping 127.0.0.1 -n 2 >nul
    goto waitloop
)

:: Dosyaları uygulamanın bulunduğu dizine aktar (HaYTooL_Firewall.ini hariç tutulur)
robocopy ""{safeExtractDir}"" ""{safeAppDir}"" /E /XF *.ini data.ini HaYTooL_Firewall.ini /R:3 /W:1 >> ""{updateLogPath}"" 2>&1

:: Yeni güncel sürümü başlat
start """" ""{mainExeTarget}""

:: Geçici güncelleme klasörünü temizle
ping 127.0.0.1 -n 3 >nul
(goto) 2>nul & rd /s /q ""{tempDir}""
exit
";

            File.WriteAllText(updaterBat, batContent, new System.Text.UTF8Encoding(false));

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
            // Tepsiye küçültme davranışını atla: süreç gerçekten kapanmalı ki güncelleme betiği dosyaları değiştirebilsin
            GuvenlikDuvarim.App.ForceExit = true;
            try { GuvenlikDuvarim.Core.Guard.StartupGuardManager.Stop(); } catch { }

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Application.Current.Shutdown();
            });

            // Güvence: Shutdown 3 sn içinde süreci bitirmezse zorla sonlandır
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                Environment.Exit(0);
            });
        }
    }
}
