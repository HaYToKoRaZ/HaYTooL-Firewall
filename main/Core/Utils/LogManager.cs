using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using GuvenlikDuvarim.Core.Storage;

namespace GuvenlikDuvarim.Core.Utils
{
    /// <summary>
    /// Uygulama log kayıtlarını %LocalAppData%\HaYTooL Firewall\logs\app.log dosyasına yazan,
    /// belirlenen satır sayısını aşan eski kayıtları otomatik temizleyen yardımcı sınıf.
    /// </summary>
    public static class LogManager
    {
        private const string LogFileName = "app.log";
        private static readonly object Sync = new object();

        /// <summary>
        /// Log klasörünün tam yolu.
        /// </summary>
        public static string LogFolder
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(appData, "HaYTooL Firewall", "logs");
            }
        }

        /// <summary>
        /// Aktif log dosyasının tam yolu.
        /// </summary>
        public static string LogFilePath => Path.Combine(LogFolder, LogFileName);

        /// <summary>
        /// Kullanıcının ayarladığı maksimum saklanacak satır sayısı (varsayılan 2000).
        /// </summary>
        public static int MaxLogLines
        {
            get
            {
                int max = int.TryParse(IniStorage.ReadValue("Settings", "MaxLogLines", "2000"), out int m) ? m : 2000;
                return max <= 0 ? 2000 : max;
            }
        }

        /// <summary>
        /// Belirtilen mesajı zaman damgalı olarak log dosyasına ekler.
        /// Satır sayısı MaxLogLines değerini aşarsa eski satırları temizler.
        /// </summary>
        public static void Log(string message)
        {
            lock (Sync)
            {
                try
                {
                    Directory.CreateDirectory(LogFolder);
                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                    List<string> lines;
                    if (File.Exists(LogFilePath))
                    {
                        lines = new List<string>(File.ReadAllLines(LogFilePath));
                    }
                    else
                    {
                        lines = new List<string>();
                    }

                    lines.Add(line);

                    int max = MaxLogLines;
                    if (lines.Count > max)
                    {
                        lines.RemoveRange(0, lines.Count - max);
                    }

                    File.WriteAllLines(LogFilePath, lines, Encoding.UTF8);
                }
                catch
                {
                    // Loglama asla ana akışı bozmamalıdır (sessiz kalır).
                }
            }
        }

        /// <summary>
        /// Log klasörünü Windows Gezgini'nde açar.
        /// </summary>
        public static void OpenLogFolder()
        {
            try
            {
                Directory.CreateDirectory(LogFolder);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{LogFolder}\"",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Açılamazsa sessiz kalır.
            }
        }
    }
}