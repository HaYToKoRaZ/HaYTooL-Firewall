using System;
using System.Collections.Generic;
using System.IO;

namespace GuvenlikDuvarim.Core.Scanner
{
    public class ScanProgressReport
    {
        public string CurrentPath { get; set; } = string.Empty;
        public int FilesFoundCount { get; set; }
    }

    /// <summary>
    /// Klasörlerdeki EXE dosyalarını özyineli (recursive) ve canlı ilerlemeli tarayan sınıf.
    /// </summary>
    public static class FileScanner
    {
        private const int ThrottleMs = 50; // UI'ı boğmamak için rapor aralığı (ms)

        /// <summary>
        /// Verilen klasördeki tüm .exe dosyalarını alt klasörlerle birlikte bulur.
        /// Progress gönderimi 50ms throttle ile sınırlandırılmıştır.
        /// </summary>
        /// <param name="folderPath">Taranacak ana klasör yolu</param>
        /// <param name="progress">Canlı ilerleme bildirim arayüzü</param>
        /// <returns>Bulunan .exe dosyalarının tam yolları</returns>
        public static List<string> FindExeFiles(string folderPath, IProgress<ScanProgressReport>? progress = null)
        {
            var exeFiles = new List<string>();
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return exeFiles;

            var queue = new Queue<string>();
            queue.Enqueue(folderPath);
            var lastReportTime = DateTime.MinValue;

            void TryReport(string path)
            {
                var now = DateTime.UtcNow;
                if ((now - lastReportTime).TotalMilliseconds >= ThrottleMs)
                {
                    progress?.Report(new ScanProgressReport
                    {
                        CurrentPath = path,
                        FilesFoundCount = exeFiles.Count
                    });
                    lastReportTime = now;
                }
            }

            while (queue.Count > 0)
            {
                string currentDir = queue.Dequeue();

                try
                {
                    // Yeni klasöre girildiğinde her zaman raporla (throttle'a rağmen)
                    progress?.Report(new ScanProgressReport
                    {
                        CurrentPath = currentDir,
                        FilesFoundCount = exeFiles.Count
                    });
                    lastReportTime = DateTime.UtcNow;

                    // Klasör içindeki .exe dosyalarını tara
                    foreach (string file in Directory.EnumerateFiles(currentDir, "*.exe"))
                    {
                        exeFiles.Add(file);
                        TryReport(file); // Throttle'lı rapor
                    }

                    // Alt klasörleri kuyruğa ekle
                    foreach (string subDir in Directory.EnumerateDirectories(currentDir))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Korumalı alt klasörlerde erişim engellendiğinde sessizce devam et
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Tarama hatası ({currentDir}): {ex.Message}");
                }
            }

            // Tarama bitti — son sayıyı raporla
            progress?.Report(new ScanProgressReport
            {
                CurrentPath = folderPath,
                FilesFoundCount = exeFiles.Count
            });

            return exeFiles;
        }
    }
}
