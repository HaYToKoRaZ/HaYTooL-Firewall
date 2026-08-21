using GuvenlikDuvarim.Core.I18n;
using GuvenlikDuvarim.Core.Scanner;
using GuvenlikDuvarim.Core.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GuvenlikDuvarim.Core.Firewall
{
    /// <summary>
    /// Kural orkestrasyon motoru. Kategorilerin güvenlik duvarına uygulanması, silinmesi
    /// ve durum doğrulaması burada merkezileştirilir; UI yalnızca ilerleme gösterir.
    /// </summary>
    public static class RuleEngine
    {
        /// <summary>
        /// Etkin engelleme durumuna göre kuralı uygular veya siler.
        /// Engelleme gerekmiyorsa (her iki yön de izinli) mevcut kural kaldırılır;
        /// profil pasifse kural kaldırılır; engelleme varsa kural oluşturulur/güncellenir.
        /// </summary>
        public static void ApplyOrRemove(string appName, string appPath, bool blockInbound, bool blockOutbound, bool isEnabled, bool isAllow)
        {
            if (isEnabled)
            {
                if (blockInbound || blockOutbound)
                    FirewallManager.ApplyRule(appName, appPath, blockInbound, blockOutbound, isEnabled: true, isAllow: isAllow);
                else
                    FirewallManager.RemoveRulesByPath(appPath);
            }
            else
            {
                FirewallManager.RemoveRulesByPath(appPath);
            }
        }

        /// <summary>
        /// Bir kategoriye ait kuralları güvenlik duvarına uygular (veya profil pasifse siler).
        /// Klasörler taranır, içlerindeki EXE'lere kurallar işlenir.
        /// </summary>
        public static int ApplyCategoryRules(CategoryModel category, IProgress<ScanProgressReport>? progress = null)
        {
            int count = 0;

            foreach (var item in category.Items)
            {
                if (item.IsFolder)
                {
                    var exeFiles = FileScanner.FindExeFiles(item.Path, progress);
                    foreach (var exe in exeFiles)
                    {
                        if (FirewallManager.IsProtectedSystemPath(exe)) continue;

                        string appName = FirewallManager.GetAppRuleKey(exe);
                        bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);

                        ApplyOrRemove(appName, exe, item.BlockInbound, item.BlockOutbound, category.IsEnabled, isAllow);

                        count++;
                        progress?.Report(new ScanProgressReport { CurrentPath = exe, FilesFoundCount = count });
                    }
                }
                else
                {
                    if (FirewallManager.IsProtectedSystemPath(item.Path)) continue;

                    string appName = FirewallManager.GetAppRuleKey(item.Path);
                    bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);

                    ApplyOrRemove(appName, item.Path, item.BlockInbound, item.BlockOutbound, category.IsEnabled, isAllow);

                    count++;
                    progress?.Report(new ScanProgressReport { CurrentPath = item.Path, FilesFoundCount = count });
                }
            }

            return count;
        }

        /// <summary>
        /// Bir kategoriye ait tüm kuralları (klasör içindeki EXE'ler dahil) güvenlik duvarından siler.
        /// </summary>
        public static int RemoveCategoryRules(CategoryModel category, IProgress<ScanProgressReport>? progress = null)
        {
            int count = 0;

            foreach (var item in category.Items)
            {
                if (item.IsFolder)
                {
                    var exeFiles = FileScanner.FindExeFiles(item.Path, progress);
                    foreach (var exe in exeFiles)
                    {
                        FirewallManager.RemoveRulesByPath(exe);
                        count++;
                        progress?.Report(new ScanProgressReport { CurrentPath = exe, FilesFoundCount = count });
                    }
                }
                else
                {
                    FirewallManager.RemoveRulesByPath(item.Path);
                    count++;
                    progress?.Report(new ScanProgressReport { CurrentPath = item.Path, FilesFoundCount = count });
                }
            }

            return count;
        }

        /// <summary>
        /// Tüm profillerdeki klasörleri diske göre senkronize eder: diskte olmayan eski kuralları temizler,
        /// güncel EXE'ler için kuralları yeniler/uygular. Sonuçları özetler halinde döndürür.
        /// </summary>
        public static SyncResult SyncAll(IEnumerable<(CategoryModel Category, AppItemModel FolderItem)> folders, IProgress<ScanProgressReport>? progress = null)
        {
            var result = new SyncResult();
            var scannedCategories = new HashSet<CategoryModel>();
            var activeRules = FirewallManager.GetActiveRules();

            foreach (var (category, folderItem) in folders)
            {
                if (!Directory.Exists(folderItem.Path)) continue;

                result.ScannedFolders++;
                scannedCategories.Add(category);

                // 1) Diski tara — güncel EXE listesini al
                var currentExes = FileScanner.FindExeFiles(folderItem.Path, progress);
                var currentExeSet = new HashSet<string>(currentExes.Select(e => e.ToLowerInvariant()));

                // 2) Bu klasör yolunun altındaki mevcut HaYTooL_ kurallarını bul
                string folderWithSep = folderItem.Path.TrimEnd('\\') + '\\';
                var existingRulesInFolder = activeRules
                    .Where(r => !string.IsNullOrEmpty(r.ApplicationPath) &&
                                (r.ApplicationPath.StartsWith(folderWithSep, StringComparison.OrdinalIgnoreCase) ||
                                 r.ApplicationPath.Equals(folderItem.Path, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var existingPathsInFolder = existingRulesInFolder
                    .Select(r => r.ApplicationPath.ToLowerInvariant())
                    .Distinct()
                    .ToHashSet();

                // Artık diskte bulunmayan (adı değişmiş veya silinmiş) eski EXE yollarını temizle
                var orphanedPaths = existingPathsInFolder
                    .Where(path => !currentExeSet.Contains(path))
                    .ToList();

                if (orphanedPaths.Count > 0)
                    result.Removed += FirewallManager.RemoveRulesByApplicationPaths(orphanedPaths);

                // 3) Güncel EXE'ler için kuralları yenile / uygula
                int processedExes = 0;
                foreach (var exe in currentExes)
                {
                    if (FirewallManager.IsProtectedSystemPath(exe)) continue;

                    string lowerExe = exe.ToLowerInvariant();
                    bool isNew = !existingPathsInFolder.Contains(lowerExe);

                    string appName = FirewallManager.GetAppRuleKey(exe);
                    ApplyOrRemove(appName, exe, category.BlockInbound, category.BlockOutbound, category.IsEnabled, category.IsAllowRule);

                    int ruleCountPerExe = (category.BlockInbound ? 1 : 0) + (category.BlockOutbound ? 1 : 0);
                    if (isNew) result.NewCount += ruleCountPerExe;
                    else result.Updated += ruleCountPerExe;

                    processedExes++;
                    progress?.Report(new ScanProgressReport { CurrentPath = exe, FilesFoundCount = processedExes });
                }
            }

            result.ScannedCategories = scannedCategories.Count;
            return result;
        }

        /// <summary>
        /// Bir yol için etkin gelen bağlantı durumunu döndürür (tek kaynak): önce tam eşleşen öğe,
        /// yoksa en içteki üst klasörün değeri, o da yoksa profil varsayılanı.
        /// </summary>
        public static bool EffectiveInbound(string path, CategoryModel category)
        {
            if (string.IsNullOrWhiteSpace(path)) return category.BlockInbound;
            var exact = category.Items.FirstOrDefault(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact.BlockInbound;
            var folder = category.Items
                .Where(i => i.IsFolder && path.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Path.Length)
                .FirstOrDefault();
            if (folder != null) return folder.BlockInbound;
            return category.BlockInbound;
        }

        /// <summary>
        /// Bir yol için etkin giden bağlantı durumunu döndürür (tek kaynak): önce tam eşleşen öğe,
        /// yoksa en içteki üst klasörün değeri, o da yoksa profil varsayılanı.
        /// </summary>
        public static bool EffectiveOutbound(string path, CategoryModel category)
        {
            if (string.IsNullOrWhiteSpace(path)) return category.BlockOutbound;
            var exact = category.Items.FirstOrDefault(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact.BlockOutbound;
            var folder = category.Items
                .Where(i => i.IsFolder && path.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Path.Length)
                .FirstOrDefault();
            if (folder != null) return folder.BlockOutbound;
            return category.BlockOutbound;
        }

        /// <summary>
        /// Bir yol için gelen/giden kural durumu ve senkron (uygulandı/uygulanmadı) durumunu hesaplar.
        /// UI tablosunun her satırı için tek çağrı olarak kullanılır.
        /// </summary>
        public static RuleStatus GetRuleStatus(string path, CategoryModel category, IEnumerable<FirewallRuleInfo> activeRules)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return RuleStatus.Empty;
            }

            string lowerPath = path.ToLowerInvariant();
            var matchRule = activeRules.FirstOrDefault(r => (r.ApplicationPath ?? "").ToLowerInvariant() == lowerPath);

            // Yalnızca TAM eşleşen öğe "bu satır bir klasör satırı mı?" sorusunu yanıtlar.
            // (Bir EXE klasörün altındaysa yedek klasör eşleşmesi onu yanlışlıkla "klasör" sanmamalı.)
            var exactItem = category.Items.FirstOrDefault(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

            var result = new RuleStatus();

            // Gelen (Inbound)
            bool inHandled = false;
            if (matchRule != null && (matchRule.RawDirection == 1 || matchRule.RawDirection == 3))
            {
                if (matchRule.RawAction == 0 && matchRule.IsEnabled) { result.SetIn("StatusBlocked", "#DC2626", "#FEE2E2"); inHandled = true; }
                else if (matchRule.RawAction == 1 && matchRule.IsEnabled) { result.SetIn("StatusAllowed", "#16A34A", "#DCFCE7"); inHandled = true; }
            }
            if (!inHandled)
            {
                bool inBlocked = EffectiveInbound(path, category);
                bool showAllow = category.IsAllowRule || !inBlocked;
                result.SetIn(showAllow ? "StatusAllowed" : "StatusBlocked",
                             showAllow ? "#16A34A" : "#DC2626",
                             showAllow ? "#DCFCE7" : "#FEE2E2");
            }

            // Giden (Outbound)
            bool outHandled = false;
            if (matchRule != null && (matchRule.RawDirection == 2 || matchRule.RawDirection == 3))
            {
                if (matchRule.RawAction == 0 && matchRule.IsEnabled) { result.SetOut("StatusBlocked", "#DC2626", "#FEE2E2"); outHandled = true; }
                else if (matchRule.RawAction == 1 && matchRule.IsEnabled) { result.SetOut("StatusAllowed", "#16A34A", "#DCFCE7"); outHandled = true; }
            }
            if (!outHandled)
            {
                bool outBlocked = EffectiveOutbound(path, category);
                bool showAllow = category.IsAllowRule || !outBlocked;
                result.SetOut(showAllow ? "StatusAllowed" : "StatusBlocked",
                              showAllow ? "#16A34A" : "#DC2626",
                              showAllow ? "#DCFCE7" : "#FEE2E2");
            }

            // Senkron (Uygulandı / Uygulanmadı)
            bool expectIn = EffectiveInbound(path, category);
            bool expectOut = EffectiveOutbound(path, category);
            bool needsRule = category.IsEnabled && (expectIn || expectOut);

            if (!needsRule)
            {
                result.SetSync("SyncNoRule", "#6B7280", "Transparent");
                return result;
            }

            bool isFolder = exactItem != null && exactItem.IsFolder;
            bool isApplied;
            if (isFolder)
            {
                // Klasörün kendisine doğrudan kural uygulanmaz; kurallar içindeki EXE'lere gider.
                // Senkron, klasörün altında herhangi bir kuralın olup olmamasına göre belirlenir.
                string folderPrefix = lowerPath.TrimEnd('\\') + '\\';
                isApplied = activeRules.Any(r =>
                    !string.IsNullOrEmpty(r.ApplicationPath) &&
                    r.ApplicationPath.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                isApplied = matchRule != null;
            }

            result.SetSync(isApplied ? "SyncApplied" : "SyncNotApplied",
                           isApplied ? "#16A34A" : "#DC2626",
                           isApplied ? "#DCFCE7" : "#FEE2E2");

            return result;
        }
    }

    /// <summary>
    /// Bir satır için gelen/giden/senkron durum rozetlerini barındırır.
    /// </summary>
    public struct RuleStatus
    {
        public static readonly RuleStatus Empty = new RuleStatus
        {
            InStatus = "-", InColor = "#6B7280", InBg = "Transparent",
            OutStatus = "-", OutColor = "#6B7280", OutBg = "Transparent",
            SyncStatus = "-", SyncColor = "#6B7280", SyncBg = "Transparent"
        };

        public string InStatus; public string InColor; public string InBg;
        public string OutStatus; public string OutColor; public string OutBg;
        public string SyncStatus; public string SyncColor; public string SyncBg;

        public void SetIn(string key, string color, string bg)
        {
            InStatus = LanguageManager.Get(key); InColor = color; InBg = bg;
        }
        public void SetOut(string key, string color, string bg)
        {
            OutStatus = LanguageManager.Get(key); OutColor = color; OutBg = bg;
        }
        public void SetSync(string key, string color, string bg)
        {
            SyncStatus = LanguageManager.Get(key); SyncColor = color; SyncBg = bg;
        }
    }

    /// <summary>
    /// SyncAll sonucunda üretilen özet sayaçları barındırır.
    /// </summary>
    public struct SyncResult
    {
        public int ScannedCategories;
        public int ScannedFolders;
        public int Removed;
        public int Updated;
        public int NewCount;
    }
}