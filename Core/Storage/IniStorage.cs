using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GuvenlikDuvarim.Core.Storage
{
    public class CategoryModel
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool BlockInbound { get; set; } = true;
        public bool BlockOutbound { get; set; } = true;
        public bool IsAllowRule { get; set; } = false; // false = Engelle (Block), true = İzin Ver (Allow / Whitelist)
        public List<AppItemModel> Items { get; set; } = new();

        public string StatusDot => IsEnabled ? "🟢" : "🔴";
        public string StatusColor => IsEnabled ? "#22C55E" : "#EF4444";
    }

    public class AppItemModel
    {
        public string Path { get; set; } = string.Empty;
        public bool IsFolder { get; set; }

        public string TypeText => IsFolder ? "📁 Klasör" : "📄 EXE";
        public System.Windows.Media.ImageSource IconImage => GuvenlikDuvarim.Core.Utils.IconExtractor.GetIcon(Path, IsFolder);
    }

    public class AppSettings
    {
        public bool FullSafeMode { get; set; }
        public string Language { get; set; } = "TR";
        public string GitHubToken { get; set; } = "";
        public string LastGistId { get; set; } = "";
        public string LastGistUrl { get; set; } = "";
        public bool AutoGistOnStartup { get; set; } = false;
        public bool AutoBackupOnStartup { get; set; } = true;
        public int MaxBackupCount { get; set; } = 30;
    }

    /// <summary>
    /// Verileri insanca okunabilir data.ini dosyasında saklayan sınıf.
    /// Örnek Format:
    /// [Anno 1800]
    /// Enabled=True
    /// BlockInbound=True
    /// BlockOutbound=True
    /// IsAllowRule=False
    /// FolderLocation="G:\CrackGame\Anno 1800"
    /// ExeLocation="C:\Games\Anno.exe"
    /// </summary>
    public static class IniStorage
    {
        private static string IniPath
        {
            get
            {
                string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.ini");
                string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HaYTooL_Firewall.ini");

                if (File.Exists(oldPath) && !File.Exists(newPath))
                {
                    try { File.Move(oldPath, newPath); } catch { }
                }

                return File.Exists(newPath) ? newPath : (File.Exists(oldPath) ? oldPath : newPath);
            }
        }

        public static (List<CategoryModel> Categories, AppSettings Settings) LoadData()
        {
            var categories = new List<CategoryModel>();
            var settings = new AppSettings();

            if (!File.Exists(IniPath)) return (categories, settings);

            string[] lines = File.ReadAllLines(IniPath, Encoding.UTF8);
            CategoryModel? currentCat = null;
            bool isSettingsSection = false;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string sectionName = line.Substring(1, line.Length - 2).Trim();
                    if (sectionName.Equals("Settings", StringComparison.OrdinalIgnoreCase))
                    {
                        isSettingsSection = true;
                        currentCat = null;
                    }
                    else
                    {
                        isSettingsSection = false;
                        currentCat = new CategoryModel { Name = sectionName };
                        categories.Add(currentCat);
                    }
                }
                else if (isSettingsSection && line.Contains("="))
                {
                    int eqIdx = line.IndexOf('=');
                    string key = line.Substring(0, eqIdx).Trim();
                    string val = Unquote(line.Substring(eqIdx + 1).Trim());

                    if (key.Equals("FullSafeMode", StringComparison.OrdinalIgnoreCase))
                        settings.FullSafeMode = bool.TryParse(val, out bool b) && b;
                    else if (key.Equals("Language", StringComparison.OrdinalIgnoreCase))
                        settings.Language = val;
                    else if (key.Equals("GitHubToken", StringComparison.OrdinalIgnoreCase))
                        settings.GitHubToken = val;
                    else if (key.Equals("LastGistId", StringComparison.OrdinalIgnoreCase))
                        settings.LastGistId = val;
                    else if (key.Equals("LastGistUrl", StringComparison.OrdinalIgnoreCase))
                        settings.LastGistUrl = val;
                    else if (key.Equals("AutoGistOnStartup", StringComparison.OrdinalIgnoreCase))
                        settings.AutoGistOnStartup = bool.TryParse(val, out bool b) && b;
                    else if (key.Equals("AutoBackupOnStartup", StringComparison.OrdinalIgnoreCase))
                        settings.AutoBackupOnStartup = bool.TryParse(val, out bool b) && b;
                    else if (key.Equals("MaxBackupCount", StringComparison.OrdinalIgnoreCase))
                        settings.MaxBackupCount = int.TryParse(val, out int m) ? m : 30;
                }
                else if (currentCat != null && line.Contains("="))
                {
                    int eqIdx = line.IndexOf('=');
                    string key = line.Substring(0, eqIdx).Trim();
                    string val = Unquote(line.Substring(eqIdx + 1).Trim());

                    if (key.Equals("Enabled", StringComparison.OrdinalIgnoreCase) || key.Equals("Status", StringComparison.OrdinalIgnoreCase))
                        currentCat.IsEnabled = bool.TryParse(val, out bool b) && b;
                    else if (key.Equals("BlockInbound", StringComparison.OrdinalIgnoreCase))
                        currentCat.BlockInbound = bool.TryParse(val, out bool b) && b;
                    else if (key.Equals("BlockOutbound", StringComparison.OrdinalIgnoreCase))
                        currentCat.BlockOutbound = bool.TryParse(val, out bool b) && b;
                    else if (key.Equals("IsAllowRule", StringComparison.OrdinalIgnoreCase))
                        currentCat.IsAllowRule = bool.TryParse(val, out bool b) && b;
                    else if (key.StartsWith("FolderLocation", StringComparison.OrdinalIgnoreCase))
                    {
                        currentCat.Items.Add(new AppItemModel { Path = val, IsFolder = true });
                    }
                    else if (key.StartsWith("ExeLocation", StringComparison.OrdinalIgnoreCase))
                    {
                        currentCat.Items.Add(new AppItemModel { Path = val, IsFolder = false });
                    }
                    else if (key.StartsWith("Item_", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = val.Split('|');
                        if (parts.Length >= 2)
                        {
                            currentCat.Items.Add(new AppItemModel
                            {
                                Path = Unquote(parts[0]),
                                IsFolder = bool.TryParse(parts[1], out bool isFolder) && isFolder
                            });
                        }
                    }
                }
            }
            return (categories, settings);
        }

        public static void SaveData(List<CategoryModel> categories, AppSettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("; HaYTooL Firewall Kategori & Ayar Yapilandirmasi (HaYTooL_Firewall.ini)");
            sb.AppendLine("; Bu dosya insanca okunabilir formatta yazilmistir. Elle de duzenleyebilirsiniz.\n");

            sb.AppendLine("[Settings]");
            sb.AppendLine($"FullSafeMode={settings.FullSafeMode}");
            sb.AppendLine($"Language={settings.Language}");
            sb.AppendLine($"GitHubToken={settings.GitHubToken}");
            sb.AppendLine($"LastGistId={settings.LastGistId}");
            sb.AppendLine($"LastGistUrl={settings.LastGistUrl}");
            sb.AppendLine($"AutoGistOnStartup={settings.AutoGistOnStartup}");
            sb.AppendLine($"AutoBackupOnStartup={settings.AutoBackupOnStartup}");
            sb.AppendLine($"MaxBackupCount={settings.MaxBackupCount}");
            sb.AppendLine();

            foreach (var cat in categories)
            {
                sb.AppendLine($"[{cat.Name}]");
                sb.AppendLine($"Enabled={cat.IsEnabled}");
                sb.AppendLine($"BlockInbound={cat.BlockInbound}");
                sb.AppendLine($"BlockOutbound={cat.BlockOutbound}");
                sb.AppendLine($"IsAllowRule={cat.IsAllowRule}");

                var folders = cat.Items.FindAll(i => i.IsFolder);
                var exes = cat.Items.FindAll(i => !i.IsFolder);

                for (int i = 0; i < folders.Count; i++)
                {
                    string key = folders.Count == 1 ? "FolderLocation" : $"FolderLocation_{i + 1}";
                    sb.AppendLine($"{key}=\"{folders[i].Path}\"");
                }

                for (int i = 0; i < exes.Count; i++)
                {
                    string key = exes.Count == 1 ? "ExeLocation" : $"ExeLocation_{i + 1}";
                    sb.AppendLine($"{key}=\"{exes[i].Path}\"");
                }

                sb.AppendLine();
            }

            File.WriteAllText(IniPath, sb.ToString(), Encoding.UTF8);
        }

        private static string Unquote(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            if (str.StartsWith("\"") && str.EndsWith("\"") && str.Length >= 2)
            {
                return str.Substring(1, str.Length - 2);
            }
            return str;
        }

        /// <summary>
        /// [Settings] bölümündeki herhangi bir anahtarı okur.
        /// Anahtar bulunamazsa defaultValue döner.
        /// </summary>
        public static string ReadValue(string section, string key, string defaultValue = "")
        {
            if (!File.Exists(IniPath)) return defaultValue;

            string[] lines = File.ReadAllLines(IniPath, Encoding.UTF8);
            bool inSection = false;
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    inSection = line.Substring(1, line.Length - 2).Trim()
                                   .Equals(section, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (inSection && line.Contains("="))
                {
                    int eqIdx = line.IndexOf('=');
                    string k = line.Substring(0, eqIdx).Trim();
                    string v = Unquote(line.Substring(eqIdx + 1).Trim());
                    if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return v;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// [Settings] bölümündeki bir anahtarı günceller veya ekler.
        /// </summary>
        public static void SaveValue(string section, string key, string value)
        {
            List<string> lines = File.Exists(IniPath)
                ? new List<string>(File.ReadAllLines(IniPath, Encoding.UTF8))
                : new List<string>();

            bool inSection = false;
            bool keyFound = false;
            int sectionLine = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (inSection && !keyFound)
                    {
                        // Bölüm bitti, anahtar yok — bölümün sonuna ekle
                        lines.Insert(i, $"{key}={value}");
                        keyFound = true;
                        break;
                    }
                    inSection = line.Substring(1, line.Length - 2).Trim()
                                   .Equals(section, StringComparison.OrdinalIgnoreCase);
                    if (inSection) sectionLine = i;
                    continue;
                }

                if (inSection && line.Contains("="))
                {
                    int eqIdx = line.IndexOf('=');
                    string k = line.Substring(0, eqIdx).Trim();
                    if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = $"{key}={value}";
                        keyFound = true;
                        break;
                    }
                }
            }

            if (!keyFound)
            {
                if (sectionLine < 0)
                {
                    // Bölüm hiç yok — dosyanın başına ekle
                    lines.Insert(0, "");
                    lines.Insert(0, $"{key}={value}");
                    lines.Insert(0, $"[{section}]");
                }
                else
                {
                    // Bölüm var ama anahtar yok — sonuna ekle
                    lines.Add($"{key}={value}");
                }
            }

            File.WriteAllLines(IniPath, lines, Encoding.UTF8);
        }
    }
}
