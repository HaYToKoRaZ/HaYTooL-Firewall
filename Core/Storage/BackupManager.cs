using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GuvenlikDuvarim.Core.Storage
{
    public static class BackupManager
    {
        private const int MaxLocalBackups = 30;

        private static string BaseBackupDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
        private static string CurrentIniPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HaYTooL_Firewall.ini");

        /// <summary>
        /// Uygulama her açıldığında konfigürasyonu yerel 'backup' klasörüne yedekler.
        /// Toplam yedek sayısı 30'u aştığında en eski yedekleri temizler.
        /// </summary>
        public static void AutoBackupOnStartup()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            CreateManualBackup(timestamp);
        }

        /// <summary>
        /// Özel isim notu ile yerel yedek dosyası oluşturur.
        /// </summary>
        public static bool CreateManualBackup(string customFileName)
        {
            try
            {
                if (!File.Exists(CurrentIniPath)) return false;

                if (!Directory.Exists(BaseBackupDir))
                {
                    Directory.CreateDirectory(BaseBackupDir);
                }

                string name = string.IsNullOrWhiteSpace(customFileName)
                    ? DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                    : customFileName.Trim();

                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    name = name.Replace(c, '_');
                }

                if (!name.StartsWith("HaYTooL_Backup_", StringComparison.OrdinalIgnoreCase))
                {
                    name = "HaYTooL_Backup_" + name;
                }

                if (!name.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    name += ".ini";
                }

                string destPath = Path.Combine(BaseBackupDir, name);
                File.Copy(CurrentIniPath, destPath, overwrite: true);

                PurgeOldBackups();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Manuel yedek alma hatası: {ex.Message}");
                return false;
            }
        }

        private static void PurgeOldBackups()
        {
            try
            {
                if (!Directory.Exists(BaseBackupDir)) return;
                var backupFiles = new DirectoryInfo(BaseBackupDir)
                    .GetFiles("HaYTooL_Backup_*.ini")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                if (backupFiles.Count > MaxLocalBackups)
                {
                    for (int i = MaxLocalBackups; i < backupFiles.Count; i++)
                    {
                        try { backupFiles[i].Delete(); } catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Mevcut konfigürasyonu GitHub Gist'e yedekler.
        /// existingGistId doluysa var olan Gist PATCH ile güncellenir.
        /// </summary>
        public static async Task<(bool Success, string GistId, string GistUrl, string Message)> UploadToGistAsync(string token, string existingGistId = "")
        {
            try
            {
                if (!File.Exists(CurrentIniPath))
                {
                    return (false, "", "", "Yedeklenecek HaYTooL_Firewall.ini dosyası bulunamadı.");
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    return (false, "", "", "GitHub Gist yüklemesi için Personal Access Token (PAT) gereklidir.\nLütfen '🔑 1-Tıkla Token Al' bağlantısına tıklayarak token alın ve yapıştırın.");
                }

                string iniContent = await File.ReadAllTextAsync(CurrentIniPath, Encoding.UTF8);

                // GÜVENLİK UYARISI DÜZELTMESİ:
                // Gist dosyası içine token yazılmasını önlemek için Regex ile GitHubToken satırını temizliyoruz.
                // Böylece GitHub Secret Scanner tokenı algılayıp otomatik iptal (revoke) etmez!
                string sanitizedIniContent = System.Text.RegularExpressions.Regex.Replace(
                    iniContent,
                    @"(?m)^GitHubToken=.*$",
                    "GitHubToken=",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HaYTooL-Firewall", "1.4.1"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

                var payload = new
                {
                    description = $"HaYTooL Firewall Configuration Backup ({DateTime.Now:yyyy-MM-dd HH:mm})",
                    @public = false,
                    files = new
                    {
                        HaYTooL_Firewall_ini = new
                        {
                            filename = "HaYTooL_Firewall.ini",
                            content = sanitizedIniContent
                        }
                    }
                };

                string jsonStr = JsonSerializer.Serialize(payload);

                // Eğer önceden kaydedilmiş Gist ID varsa, güncellemek için PATCH atıyoruz
                if (!string.IsNullOrWhiteSpace(existingGistId))
                {
                    string patchGistId = ExtractGistId(existingGistId);
                    if (!string.IsNullOrEmpty(patchGistId))
                    {
                        var patchContent = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://api.github.com/gists/{patchGistId}")
                        {
                            Content = patchContent
                        };

                        var patchResponse = await httpClient.SendAsync(request);
                        string patchResponseBody = await patchResponse.Content.ReadAsStringAsync();

                        if (patchResponse.IsSuccessStatusCode)
                        {
                            using var doc = JsonDocument.Parse(patchResponseBody);
                            var root = doc.RootElement;
                            string resId = root.GetProperty("id").GetString() ?? patchGistId;
                            string htmlUrl = root.GetProperty("html_url").GetString() ?? "";

                            return (true, resId, htmlUrl, "Mevcut GitHub Gist yedeklemeniz başarıyla güncellendi!");
                        }
                    }
                }

                // İlk defa yükleniyorsa veya PATCH 404 döndüyse POST ile yeni Gist aç
                var postContent = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://api.github.com/gists", postContent);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    string gistId = root.GetProperty("id").GetString() ?? "";
                    string htmlUrl = root.GetProperty("html_url").GetString() ?? "";

                    return (true, gistId, htmlUrl, "Yeni GitHub Gist yedeklemeniz başarıyla oluşturuldu!");
                }
                else
                {
                    return (false, "", "", $"GitHub Gist yükleme hatası ({response.StatusCode}): {responseBody}");
                }
            }
            catch (Exception ex)
            {
                return (false, "", "", $"Gist yükleme sırasında hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// GitHub Gist URL'sinden veya Gist ID'sinden konfigürasyonu indirip geri yükler.
        /// </summary>
        public static async Task<(bool Success, string Message)> DownloadFromGistAsync(string gistIdOrUrl, string token = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gistIdOrUrl))
                {
                    return (false, "Lütfen bir GitHub Gist ID veya URL girin.");
                }

                string gistId = ExtractGistId(gistIdOrUrl);
                if (string.IsNullOrEmpty(gistId))
                {
                    return (false, "Geçersiz GitHub Gist ID veya URL biçimi.");
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HaYTooL-Firewall", "1.3.0"));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                if (!string.IsNullOrWhiteSpace(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                }

                var response = await httpClient.GetAsync($"https://api.github.com/gists/{gistId}");
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Gist bulunamadı veya yetki hatası ({response.StatusCode}).");
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                var filesProp = root.GetProperty("files");

                string downloadedContent = "";
                foreach (var fileProp in filesProp.EnumerateObject())
                {
                    string filename = fileProp.Name;
                    if (filename.EndsWith(".ini", StringComparison.OrdinalIgnoreCase) || downloadedContent == "")
                    {
                        downloadedContent = fileProp.Value.GetProperty("content").GetString() ?? "";
                        if (filename.Equals("HaYTooL_Firewall.ini", StringComparison.OrdinalIgnoreCase)) break;
                    }
                }

                if (string.IsNullOrWhiteSpace(downloadedContent))
                {
                    return (false, "Gist içerisinde geçerli konfigürasyon içeriği bulunamadı.");
                }

                // Mevcut konfigürasyonu önce yerel yedekle
                AutoBackupOnStartup();

                await File.WriteAllTextAsync(CurrentIniPath, downloadedContent, Encoding.UTF8);
                return (true, "Yedek GitHub Gist'ten başarıyla indirildi ve uygulandı!");
            }
            catch (Exception ex)
            {
                return (false, $"Gist geri yükleme hatası: {ex.Message}");
            }
        }

        private static string ExtractGistId(string input)
        {
            string trimmed = input.Trim();
            var match = Regex.Match(trimmed, @"(?:gist\.github\.com\/(?:[^\/]+\/)?)([a-f0-9]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            if (Regex.IsMatch(trimmed, @"^[a-f0-9]+$", RegexOptions.IgnoreCase))
            {
                return trimmed;
            }
            return trimmed;
        }
    }
}
