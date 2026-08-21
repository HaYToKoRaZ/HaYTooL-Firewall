using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using GuvenlikDuvarim.Core.Firewall;
using GuvenlikDuvarim.Core.I18n;
using GuvenlikDuvarim.Core.Storage;

namespace GuvenlikDuvarim.Core.CLI
{
    /// <summary>
    /// HaYTooL Firewall Komut Satırı (CLI) işlemlerini yöneten sınıf.
    /// Terminal üzerinden profil açma/kapatma, listeleme, FullSafe mod yönetimi ve kural senkronizasyonu sağlar.
    /// </summary>
    public static class CliManager
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int ATTACH_PARENT_PROCESS = -1;
        private static readonly IntPtr HWND_BROADCAST = (IntPtr)0xffff;

        public static readonly uint WM_HAYTOOL_REFRESH = RegisterWindowMessage("HaYTooL_Firewall_Refresh_Message");

        /// <summary>
        /// Gelen komut satırı argümanlarını analiz eder ve CLI modunda çalışıp çalışmayacağını belirler.
        /// </summary>
        /// <param name="args">Komut satırından gelen parametre dizisi</param>
        /// <returns>Eğer geçerli bir CLI komutu işlendiyse true, GUI açılması gerekiyorsa false döner</returns>
        public static bool ProcessArgs(string[] args)
        {
            if (args == null || args.Length == 0) return false;

            // Terminal çıktısı için üst prosese (CMD / PowerShell) bağlan ve UTF-8 kod sayfasını (CP 65001) aktif et
            if (AttachConsole(ATTACH_PARENT_PROCESS) || AllocConsole())
            {
                try
                {
                    SetConsoleOutputCP(65001);
                    SetConsoleCP(65001);
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;
                }
                catch { }

                var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
            }

            // Dil ayarını komut satırından veya ini yapılandırmasından belirle
            string currentLang = GetEffectiveLanguage(args);
            LanguageManager.CurrentLanguage = currentLang;

            // Parametrelerden --lang / -l bayraklarını ve değerlerini temizleyerek komut dizisini filtrele
            string[] cleanArgs = FilterLangArgs(args);
            if (cleanArgs.Length == 0)
            {
                ShowHelp();
                return true;
            }

            string primaryArg = cleanArgs[0].ToLowerInvariant();

            try
            {
                switch (primaryArg)
                {
                    case "help":
                    case "--help":
                    case "-h":
                    case "/?":
                        ShowHelp();
                        return true;

                    case "profile":
                    case "profiles":
                    case "--profile":
                        return HandleProfileCommand(cleanArgs.Skip(1).ToArray());

                    case "--list-profiles":
                    case "-lp":
                        ListProfiles();
                        return true;

                    case "--profile-enable":
                    case "-pe":
                        if (cleanArgs.Length > 1) EnableProfile(cleanArgs[1]);
                        else PrintError(currentLang == "TR" ? "Profil adı belirtilmedi. Örnek: --profile-enable \"Oyun\"" : "Profile name required. Example: --profile-enable \"Games\"");
                        return true;

                    case "--profile-disable":
                    case "-pd":
                        if (cleanArgs.Length > 1) DisableProfile(cleanArgs[1]);
                        else PrintError(currentLang == "TR" ? "Profil adı belirtilmedi. Örnek: --profile-disable \"Oyun\"" : "Profile name required. Example: --profile-disable \"Games\"");
                        return true;

                    case "--profile-toggle":
                    case "-pt":
                        if (cleanArgs.Length > 1) ToggleProfile(cleanArgs[1]);
                        else PrintError(currentLang == "TR" ? "Profil adı belirtilmedi. Örnek: --profile-toggle \"Oyun\"" : "Profile name required. Example: --profile-toggle \"Games\"");
                        return true;

                    case "--enable-all":
                        SetAllProfilesState(true);
                        return true;

                    case "--disable-all":
                        SetAllProfilesState(false);
                        return true;

                    case "fullsafe":
                    case "--fullsafe":
                        return HandleFullSafeCommand(cleanArgs.Skip(1).ToArray());

                    case "--fullsafe-on":
                        SetFullSafeMode(true);
                        return true;

                    case "--fullsafe-off":
                        SetFullSafeMode(false);
                        return true;

                    case "apply":
                    case "--apply":
                    case "-a":
                        ApplyAllRules();
                        return true;

                    case "status":
                    case "--status":
                    case "-s":
                        ShowStatus();
                        return true;

                    default:
                        if (currentLang == "TR")
                        {
                            Console.WriteLine($"❌ Geçersiz CLI Komutu: {cleanArgs[0]}");
                            Console.WriteLine("Kullanılabilir komutları görmek için '--help' yazabilirsiniz.");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Invalid CLI Command: {cleanArgs[0]}");
                            Console.WriteLine("Type '--help' to see available commands.");
                        }
                        return true;
                }
            }
            catch (Exception ex)
            {
                PrintError(currentLang == "TR" ? $"Hata Oluştu: {ex.Message}" : $"Error Occurred: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// CLI argümanlarından efektif uygulama dilini belirler.
        /// Eğer --lang veya -l verilmişse onu kullanır, aksi halde HaYTooL_Firewall.ini ayarını okur.
        /// </summary>
        private static string GetEffectiveLanguage(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Equals("--lang", StringComparison.OrdinalIgnoreCase) || args[i].Equals("-l", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                {
                    return args[i + 1].ToUpperInvariant();
                }
            }

            var (_, settings) = IniStorage.LoadData();
            return string.IsNullOrEmpty(settings.Language) ? "TR" : settings.Language.ToUpperInvariant();
        }

        /// <summary>
        /// Argüman dizisinden --lang / -l ve değerini temizler.
        /// </summary>
        private static string[] FilterLangArgs(string[] args)
        {
            var list = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Equals("--lang", StringComparison.OrdinalIgnoreCase) || args[i].Equals("-l", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                {
                    i++; // Bayrağı ve dil kodunu atla
                    continue;
                }
                list.Add(args[i]);
            }
            return list.ToArray();
        }

        private static void PrintError(string msg)
        {
            Console.WriteLine($"❌ {msg}");
        }

        /// <summary>
        /// Profil alt komutlarını (list, enable, disable, toggle, enable-all, disable-all) işler.
        /// </summary>
        private static bool HandleProfileCommand(string[] subArgs)
        {
            string lang = LanguageManager.CurrentLanguage;
            if (subArgs.Length == 0 || subArgs[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                ListProfiles();
                return true;
            }

            string subCmd = subArgs[0].ToLowerInvariant();
            string targetProfile = subArgs.Length > 1 ? subArgs[1] : string.Empty;

            switch (subCmd)
            {
                case "enable":
                case "on":
                    if (!string.IsNullOrWhiteSpace(targetProfile)) EnableProfile(targetProfile);
                    else PrintError(lang == "TR" ? "Lütfen açmak istediğiniz profil adını belirtin." : "Please specify profile name to enable.");
                    break;

                case "disable":
                case "off":
                    if (!string.IsNullOrWhiteSpace(targetProfile)) DisableProfile(targetProfile);
                    else PrintError(lang == "TR" ? "Lütfen kapatmak istediğiniz profil adını belirtin." : "Please specify profile name to disable.");
                    break;

                case "toggle":
                    if (!string.IsNullOrWhiteSpace(targetProfile)) ToggleProfile(targetProfile);
                    else PrintError(lang == "TR" ? "Lütfen durumunu değiştirmek istediğiniz profil adını belirtin." : "Please specify profile name to toggle.");
                    break;

                case "enable-all":
                case "on-all":
                    SetAllProfilesState(true);
                    break;

                case "disable-all":
                case "off-all":
                    SetAllProfilesState(false);
                    break;

                default:
                    PrintError(lang == "TR" ? $"Bilinmeyen profil alt komutu: {subCmd}" : $"Unknown profile subcommand: {subCmd}");
                    break;
            }

            return true;
        }

        /// <summary>
        /// FullSafe alt komutlarını (enable, disable, status) işler.
        /// </summary>
        private static bool HandleFullSafeCommand(string[] subArgs)
        {
            string lang = LanguageManager.CurrentLanguage;
            if (subArgs.Length == 0 || subArgs[0].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                var (_, settings) = IniStorage.LoadData();
                string statusText = settings.FullSafeMode ? (lang == "TR" ? "🟢 AKTİF" : "🟢 ENABLED") : (lang == "TR" ? "🔴 PASİF" : "🔴 DISABLED");
                Console.WriteLine($"🛡️ FullSafe Mode Status: {statusText}");
                return true;
            }

            string subCmd = subArgs[0].ToLowerInvariant();
            if (subCmd == "enable" || subCmd == "on" || subCmd == "1" || subCmd == "true")
            {
                SetFullSafeMode(true);
            }
            else if (subCmd == "disable" || subCmd == "off" || subCmd == "0" || subCmd == "false")
            {
                SetFullSafeMode(false);
            }
            else
            {
                PrintError(lang == "TR" ? $"Geçersiz FullSafe komutu: {subCmd}" : $"Invalid FullSafe command: {subCmd}");
            }

            return true;
        }

        /// <summary>
        /// Tanımlı tüm profilleri ve mevcut durumlarını terminale listeler.
        /// </summary>
        private static void ListProfiles()
        {
            string lang = LanguageManager.CurrentLanguage;
            var (categories, _) = IniStorage.LoadData();
            Console.WriteLine("\n===============================================");
            Console.WriteLine(lang == "TR" ? "🛡️  HaYTooL Firewall - Profil Listesi" : "🛡️  HaYTooL Firewall - Profile List");
            Console.WriteLine("===============================================");

            if (categories.Count == 0)
            {
                Console.WriteLine(lang == "TR" ? "⚠️ Henüz tanımlı bir profil bulunamadı." : "⚠️ No profiles found.");
                return;
            }

            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                string statusText = cat.IsEnabled 
                    ? (lang == "TR" ? "🟢 [AÇIK / AKTİF]" : "🟢 [ON / ACTIVE]") 
                    : (lang == "TR" ? "🔴 [KAPALI / PASİF]" : "🔴 [OFF / INACTIVE]");
                string typeText = cat.IsAllowRule 
                    ? (lang == "TR" ? "🟢 İzin Ver" : "🟢 Allow") 
                    : (lang == "TR" ? "⛔ Engelle" : "⛔ Block");
                string itemText = lang == "TR" ? $"{cat.Items.Count} Öğe" : $"{cat.Items.Count} Items";
                Console.WriteLine($" {i + 1}. {cat.Name,-25} {statusText,-20} ({typeText}, {itemText})");
            }
            Console.WriteLine("===============================================\n");
        }

        private static void EnableProfile(string profileName)
        {
            SetProfileState(profileName, true);
        }

        private static void DisableProfile(string profileName)
        {
            SetProfileState(profileName, false);
        }

        private static void ToggleProfile(string profileName)
        {
            string lang = LanguageManager.CurrentLanguage;
            var (categories, _) = IniStorage.LoadData();
            var targetCat = categories.FirstOrDefault(c => c.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase) || c.Name.Contains(profileName, StringComparison.OrdinalIgnoreCase));

            if (targetCat == null)
            {
                PrintError(lang == "TR" ? $"'{profileName}' adında bir profil bulunamadı." : $"Profile '{profileName}' not found.");
                return;
            }

            SetProfileState(targetCat.Name, !targetCat.IsEnabled);
        }

        private static void SetProfileState(string profileName, bool enable)
        {
            string lang = LanguageManager.CurrentLanguage;
            var (categories, settings) = IniStorage.LoadData();
            var targetCat = categories.FirstOrDefault(c => c.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase) || c.Name.Contains(profileName, StringComparison.OrdinalIgnoreCase));

            if (targetCat == null)
            {
                PrintError(lang == "TR" ? $"'{profileName}' adında bir profil bulunamadı." : $"Profile '{profileName}' not found.");
                return;
            }

            targetCat.IsEnabled = enable;
            IniStorage.SaveData(categories, settings);

            ApplyProfileRules(targetCat);

            string actionStr = enable 
                ? (lang == "TR" ? "🟢 AÇILDI (Aktif)" : "🟢 ENABLED (Active)") 
                : (lang == "TR" ? "🔴 KAPATILDI (Pasif)" : "🔴 DISABLED (Inactive)");
            
            if (lang == "TR")
                Console.WriteLine($"✅ '{targetCat.Name}' profili başarıyla {actionStr}.");
            else
                Console.WriteLine($"✅ Profile '{targetCat.Name}' successfully {actionStr}.");

            NotifyRunningGui();
        }

        private static void SetAllProfilesState(bool enable)
        {
            string lang = LanguageManager.CurrentLanguage;
            var (categories, settings) = IniStorage.LoadData();
            if (categories.Count == 0)
            {
                Console.WriteLine(lang == "TR" ? "⚠️ İşlem yapılacak profil bulunamadı." : "⚠️ No profiles available to update.");
                return;
            }

            foreach (var cat in categories)
            {
                cat.IsEnabled = enable;
                ApplyProfileRules(cat);
            }

            IniStorage.SaveData(categories, settings);
            
            if (lang == "TR")
            {
                string actionStr = enable ? "🟢 TÜM PROFİLLER AÇILDI" : "🔴 TÜM PROFİLLER KAPATILDI";
                Console.WriteLine($"✅ {actionStr}. Toplam {categories.Count} profil güncellendi.");
            }
            else
            {
                string actionStr = enable ? "🟢 ALL PROFILES ENABLED" : "🔴 ALL PROFILES DISABLED";
                Console.WriteLine($"✅ {actionStr}. Updated {categories.Count} profiles.");
            }

            NotifyRunningGui();
        }

        private static void ApplyProfileRules(CategoryModel cat)
        {
            foreach (var item in cat.Items)
            {
                if (item.IsFolder)
                {
                    if (!Directory.Exists(item.Path)) continue;

                    var exes = Directory.GetFiles(item.Path, "*.exe", SearchOption.AllDirectories);
                    foreach (var exePath in exes)
                    {
                        if (FirewallManager.IsProtectedSystemPath(exePath)) continue;
                        string appName = FirewallManager.GetAppRuleKey(exePath);
                        RuleEngine.ApplyOrRemove(appName, exePath, item.BlockInbound, item.BlockOutbound, cat.IsEnabled, cat.IsAllowRule);
                    }
                }
                else
                {
                    if (!File.Exists(item.Path)) continue;

                    if (FirewallManager.IsProtectedSystemPath(item.Path)) continue;
                    string appName = FirewallManager.GetAppRuleKey(item.Path);
                    RuleEngine.ApplyOrRemove(appName, item.Path, item.BlockInbound, item.BlockOutbound, cat.IsEnabled, cat.IsAllowRule);
                }
            }
        }

        private static void SetFullSafeMode(bool enable)
        {
            string lang = LanguageManager.CurrentLanguage;
            var (categories, settings) = IniStorage.LoadData();
            settings.FullSafeMode = enable;
            IniStorage.SaveData(categories, settings);

            FirewallManager.SetFullSafeMode(enable);

            string stateText = enable 
                ? (lang == "TR" ? "🟢 AKTİF EDİLDİ" : "🟢 ENABLED") 
                : (lang == "TR" ? "🔴 KAPATILDI" : "🔴 DISABLED");
            
            Console.WriteLine(lang == "TR" ? $"🛡️ FullSafe Modu {stateText}." : $"🛡️ FullSafe Mode {stateText}.");

            NotifyRunningGui();
        }

        private static void ApplyAllRules()
        {
            string lang = LanguageManager.CurrentLanguage;
            var (categories, settings) = IniStorage.LoadData();
            Console.WriteLine(lang == "TR" 
                ? "⚡ Tüm profil kuralları Windows Güvenlik Duvarı'na yeniden uygulanıyor..." 
                : "⚡ Re-applying all profile rules to Windows Firewall...");

            FirewallManager.SetFullSafeMode(settings.FullSafeMode);

            foreach (var cat in categories)
            {
                ApplyProfileRules(cat);
            }

            Console.WriteLine(lang == "TR" 
                ? "✅ Tüm kurallar başarıyla güncellendi ve uygulandı." 
                : "✅ All rules successfully updated and applied.");
            NotifyRunningGui();
        }

        private static void ShowStatus()
        {
            string lang = LanguageManager.CurrentLanguage;
            var (categories, settings) = IniStorage.LoadData();
            var activeRules = FirewallManager.GetRawActiveRules();

            int activeCatCount = categories.Count(c => c.IsEnabled);
            int blockedCount = activeRules.Count(r => r.RawAction == 0);
            int allowedCount = activeRules.Count(r => r.RawAction == 1);

            Console.WriteLine("\n===============================================");
            Console.WriteLine(lang == "TR" ? "📊  HaYTooL Firewall - Sistem Durumu" : "📊  HaYTooL Firewall - System Status");
            Console.WriteLine("===============================================");
            if (lang == "TR")
            {
                Console.WriteLine($" PROFILER       : Toplam {categories.Count} Profil ({activeCatCount} Aktif, {categories.Count - activeCatCount} Pasif)");
                Console.WriteLine($" FULLSAFE MODU  : {(settings.FullSafeMode ? "🟢 AKTİF" : "🔴 PASİF")}");
                Console.WriteLine($" AKTIF KURALLAR : Toplam {activeRules.Count} Güvenlik Duvarı Kuralı");
                Console.WriteLine($"   ⛔ Engellenen : {blockedCount} kural");
                Console.WriteLine($"   🟢 İzinli     : {allowedCount} kural");
            }
            else
            {
                Console.WriteLine($" PROFILES       : Total {categories.Count} Profiles ({activeCatCount} Active, {categories.Count - activeCatCount} Inactive)");
                Console.WriteLine($" FULLSAFE MODE  : {(settings.FullSafeMode ? "🟢 ENABLED" : "🔴 DISABLED")}");
                Console.WriteLine($" ACTIVE RULES   : Total {activeRules.Count} Firewall Rules");
                Console.WriteLine($"   ⛔ Blocked   : {blockedCount} rules");
                Console.WriteLine($"   🟢 Allowed   : {allowedCount} rules");
            }
            Console.WriteLine("===============================================\n");
        }

        private static void ShowHelp()
        {
            string lang = LanguageManager.CurrentLanguage;

            if (lang != "TR")
            {
                Console.WriteLine("\n=========================================================================");
                Console.WriteLine("🛡️  HaYTooL Firewall - Command Line Interface (CLI) Help Guide");
                Console.WriteLine("    Developer: HaYTo | Support: korazhayto@gmail.com");
                Console.WriteLine("=========================================================================\n");

                Console.WriteLine("USAGE:");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" <command> [options] [--lang <TR|EN|ES|DE|PT|AR|RU>]\n");

                Console.WriteLine("PROFILE COMMANDS:");
                Console.WriteLine("  profile list | --list-profiles | -lp");
                Console.WriteLine("      Lists all profiles and their status ([ON]/[OFF]).\n");

                Console.WriteLine("  profile enable \"<Name>\" | --profile-enable \"<Name>\" | -pe \"<Name>\"");
                Console.WriteLine("      Enables specified profile and applies firewall rules.\n");

                Console.WriteLine("  profile disable \"<Name>\" | --profile-disable \"<Name>\" | -pd \"<Name>\"");
                Console.WriteLine("      Disables specified profile and removes firewall rules.\n");

                Console.WriteLine("  profile toggle \"<Name>\" | --profile-toggle \"<Name>\" | -pt \"<Name>\"");
                Console.WriteLine("      Toggles profile status (if ON -> OFF, if OFF -> ON).\n");

                Console.WriteLine("  profile enable-all | --enable-all");
                Console.WriteLine("      Enables all profiles at once.\n");

                Console.WriteLine("  profile disable-all | --disable-all");
                Console.WriteLine("      Disables all profiles at once.\n");

                Console.WriteLine("FULLSAFE MODE COMMANDS:");
                Console.WriteLine("  fullsafe enable | --fullsafe-on     : Enables FullSafe mode.");
                Console.WriteLine("  fullsafe disable | --fullsafe-off   : Disables FullSafe mode.");
                Console.WriteLine("  fullsafe status                     : Displays FullSafe mode status.\n");

                Console.WriteLine("GENERAL COMMANDS:");
                Console.WriteLine("  apply | --apply | -a                : Re-applies all profiles and rules.");
                Console.WriteLine("  status | --status | -s              : Displays system status and rule statistics.");
                Console.WriteLine("  help | --help | -h | /?             : Displays this help screen.\n");

                Console.WriteLine("LANGUAGE SELECTION:");
                Console.WriteLine("  --lang <TR|EN|ES|DE|PT|AR|RU> | -l <TR|EN|ES|DE|PT|AR|RU>");
                Console.WriteLine("      Overrides output language for CLI execution (default: app setting).\n");

                Console.WriteLine("EXAMPLES:");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" profile enable \"Games\"");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" profile disable \"Work\" --lang EN");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" --status\n");
                Console.WriteLine("=========================================================================\n");
            }
            else
            {
                Console.WriteLine("\n=========================================================================");
                Console.WriteLine("🛡️  HaYTooL Firewall - Komut Satiri Istemcisi (CLI) Yardim Rehberi");
                Console.WriteLine("    Gelistirici: HaYTo | Destek: korazhayto@gmail.com");
                Console.WriteLine("=========================================================================\n");

                Console.WriteLine("KULLANIM:");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" <komut> [secenekler] [--lang <TR|EN|ES|DE|PT|AR|RU>]\n");

                Console.WriteLine("PROFIL KOMUTLARI:");
                Console.WriteLine("  profile list | --list-profiles | -lp");
                Console.WriteLine("      Tum profilleri ve durumlarini ([ON]/[OFF]) listeler.\n");

                Console.WriteLine("  profile enable \"<Profil Adi>\" | --profile-enable \"<Profil Adi>\" | -pe \"<Profil Adi>\"");
                Console.WriteLine("      Belirtilen profili ACAR (Aktif) ve kurallarini uygular.\n");

                Console.WriteLine("  profile disable \"<Profil Adi>\" | --profile-disable \"<Profil Adi>\" | -pd \"<Profil Adi>\"");
                Console.WriteLine("      Belirtilen profili KAPATIR (Pasif) ve kurallarini devre disi birakir.\n");

                Console.WriteLine("  profile toggle \"<Profil Adi>\" | --profile-toggle \"<Profil Adi>\" | -pt \"<Profil Adi>\"");
                Console.WriteLine("      Profil acik ise kapatir, kapali ise acar.\n");

                Console.WriteLine("  profile enable-all | --enable-all");
                Console.WriteLine("      Tum profilleri tek seferde ACAR.\n");

                Console.WriteLine("  profile disable-all | --disable-all");
                Console.WriteLine("      Tum profilleri tek seferde KAPATIR.\n");

                Console.WriteLine("FULLSAFE MODU KOMUTLARI:");
                Console.WriteLine("  fullsafe enable | --fullsafe-on     : FullSafe modunu ACAR.");
                Console.WriteLine("  fullsafe disable | --fullsafe-off   : FullSafe modunu KAPATIR.");
                Console.WriteLine("  fullsafe status                     : FullSafe mod durumunu gosterir.\n");

                Console.WriteLine("GENEL KOMUTLAR:");
                Console.WriteLine("  apply | --apply | -a                : Tum profilleri ve kurallari yeniden uygular.");
                Console.WriteLine("  status | --status | -s              : Sistem durumunu ve kural istatistiklerini basar.");
                Console.WriteLine("  help | --help | -h | /?             : Bu yardim ekranini gosterir.\n");

                Console.WriteLine("DIL SECIMI:");
                Console.WriteLine("  --lang <TR|EN|ES|DE|PT|AR|RU> | -l <TR|EN|ES|DE|PT|AR|RU>");
                Console.WriteLine("      CLI ciktisinin dilini anlik degistirir (Varsayilan: Uygulama dil ayari).\n");

                Console.WriteLine("ORNEKLER:");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" profile enable \"Oyunlar\"");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" profile disable \"Is Yeri\" --lang EN");
                Console.WriteLine("  \"HaYTooL Firewall.exe\" --status\n");
                Console.WriteLine("=========================================================================\n");
            }
        }

        private static void NotifyRunningGui()
        {
            try
            {
                PostMessage(HWND_BROADCAST, WM_HAYTOOL_REFRESH, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }
    }
}
