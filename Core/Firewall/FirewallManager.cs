using GuvenlikDuvarim.Core.I18n;
using System;
using System.Collections.Generic;
using System.IO;

namespace GuvenlikDuvarim.Core.Firewall
{
    /// <summary>
    /// Windows Güvenlik Duvarı (INetFwPolicy2) işlemlerini yöneten sınıf.
    /// </summary>
    public static class FirewallManager
    {
        private const string RulePrefix = "HaYTooL_";
        private const string FullSafeRuleName = "HaYTooL_FullSafe_BlockAllOutbound";
        private const string LegacyRulePrefix = "GuvenlikDuvarim_";

        /// <summary>
        /// Belirtilen EXE dosyası için kural oluşturur veya günceller.
        /// </summary>
        public static void ApplyRule(string appName, string appPath, bool blockInbound, bool blockOutbound, bool isEnabled = true, bool isAllow = false)
        {
            Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2") 
                                 ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
            dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;
            dynamic rules = fwPolicy2.Rules;

            // Önceki kuralları temizle (Inbound ve Outbound)
            RemoveRuleIfExist(rules, $"{RulePrefix}IN_{appName}");
            RemoveRuleIfExist(rules, $"{RulePrefix}OUT_{appName}");

            Type typeFwRule = Type.GetTypeFromProgID("HNetCfg.FWRule")
                              ?? throw new Exception("Firewall Rule COM nesnesi bulunamadı.");

            int actionCode = isAllow ? 1 : 0; // NET_FW_ACTION_ALLOW = 1, NET_FW_ACTION_BLOCK = 0

            // Gelen Bağlantı (Inbound)
            if (blockInbound)
            {
                dynamic ruleIn = Activator.CreateInstance(typeFwRule)!;
                ruleIn.Name = $"{RulePrefix}IN_{appName}";
                ruleIn.Description = isAllow ? "HaYTooL Firewall tarafindan gelen baglantiya izin verildi." : "HaYTooL Firewall tarafindan gelen baglanti engellendi.";
                ruleIn.ApplicationName = appPath;
                ruleIn.Action = actionCode; 
                ruleIn.Direction = 1; // NET_FW_RULE_DIR_IN = 1
                ruleIn.Enabled = isEnabled;
                rules.Add(ruleIn);
            }

            // Giden Bağlantı (Outbound)
            if (blockOutbound)
            {
                dynamic ruleOut = Activator.CreateInstance(typeFwRule)!;
                ruleOut.Name = $"{RulePrefix}OUT_{appName}";
                ruleOut.Description = isAllow ? "HaYTooL Firewall tarafindan giden baglantiya izin verildi." : "HaYTooL Firewall tarafindan giden baglanti engellendi.";
                ruleOut.ApplicationName = appPath;
                ruleOut.Action = actionCode; 
                ruleOut.Direction = 2; // NET_FW_RULE_DIR_OUT = 2
                ruleOut.Enabled = isEnabled;
                rules.Add(ruleOut);
            }
        }

        /// <summary>
        /// FullSafe Modunu (Sadece İzin Verilenler İnternete Çıksın) açar veya kapatır.
        /// </summary>
        public static void SetFullSafeMode(bool enabled)
        {
            Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2") 
                                 ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
            dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;
            dynamic rules = fwPolicy2.Rules;

            RemoveRuleIfExist(rules, FullSafeRuleName);

            if (enabled)
            {
                Type typeFwRule = Type.GetTypeFromProgID("HNetCfg.FWRule")
                                  ?? throw new Exception("Firewall Rule COM nesnesi bulunamadı.");

                dynamic ruleBlockAll = Activator.CreateInstance(typeFwRule)!;
                ruleBlockAll.Name = FullSafeRuleName;
                ruleBlockAll.Description = "HaYTooL Firewall FullSafe Modu: Tüm giden bağlantılar engellendi (Sadece izinliler hariç).";
                ruleBlockAll.Action = 0; // NET_FW_ACTION_BLOCK = 0
                ruleBlockAll.Direction = 2; // NET_FW_RULE_DIR_OUT = 2
                ruleBlockAll.Enabled = true;
                rules.Add(ruleBlockAll);
            }
        }

        /// <summary>
        /// Uygulamaya ait kuralları güvenlik duvarından kaldırır.
        /// </summary>
        public static void RemoveAppRules(string appName)
        {
            Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2") 
                                 ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
            dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;
            dynamic rules = fwPolicy2.Rules;

            RemoveRuleIfExist(rules, $"{RulePrefix}IN_{appName}");
            RemoveRuleIfExist(rules, $"{RulePrefix}OUT_{appName}");
        }

        /// <summary>
        /// İsmi verilen kuralı tekil olarak siler.
        /// </summary>
        public static void RemoveRuleByName(string ruleName)
        {
            Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2") 
                                 ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
            dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;
            dynamic rules = fwPolicy2.Rules;

            RemoveRuleIfExist(rules, ruleName);
        }

        /// <summary>
        /// İsmi verilen kuralın durumunu (Etkin/Pasif) değiştirir.
        /// </summary>
        public static void ToggleRuleEnabled(string ruleName, bool enable)
        {
            Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2") 
                                 ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
            dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;
            
            foreach (dynamic rule in fwPolicy2.Rules)
            {
                if (rule.Name == ruleName)
                {
                    rule.Enabled = enable;
                    break;
                }
            }
        }

        private static void RemoveRuleIfExist(dynamic rules, string ruleName)
        {
            try
            {
                rules.Remove(ruleName);
            }
            catch
            {
                // Kural bulunamadığında sessizce geç
            }
        }

        /// <summary>
        /// Eski "GuvenlikDuvarim_" prefix'li kuralları Windows Güvenlik Duvarı'ndan kaldırır.
        /// Uygulama ilk açılışta bir kez çağrılır.
        /// </summary>
        public static int MigrateOldRules()
        {
            int removed = 0;
            try
            {
                Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2")
                                     ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
                dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;

                // Eski kural isimlerini önce topla (foreach içinde silme yapılmaz)
                var oldRuleNames = new List<string>();
                foreach (dynamic rule in fwPolicy2.Rules)
                {
                    string name = rule.Name as string ?? string.Empty;
                    if (name.StartsWith(LegacyRulePrefix))
                        oldRuleNames.Add(name);
                }

                // Toplu sil
                dynamic rules = fwPolicy2.Rules;
                foreach (string name in oldRuleNames)
                {
                    RemoveRuleIfExist(rules, name);
                    removed++;
                }
            }
            catch { }
            return removed;
        }

        /// <summary>
        /// Uygulamamız tarafından oluşturulan tüm aktif kuralları döndürür.
        /// Aynı uygulama için hem Gelen hem Giden kuralı varsa tek satırda birleştirir.
        /// </summary>
        public static List<FirewallRuleInfo> GetActiveRules()
        {
            var rawRules = new List<FirewallRuleInfo>();
            Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2") 
                                 ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
            dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;
            
            foreach (dynamic rule in fwPolicy2.Rules)
            {
                string ruleName = rule.Name;
                if (ruleName != null && ruleName.StartsWith(RulePrefix))
                {
                    bool isEnabled = (bool)rule.Enabled;
                    int dir = (int)rule.Direction;
                    int act = (int)rule.Action;

                    rawRules.Add(new FirewallRuleInfo
                    {
                        Name = ruleName,
                        ApplicationPath = rule.ApplicationName ?? string.Empty,
                        RawDirection = dir,
                        RawAction = act,
                        IsEnabled = isEnabled
                    });
                }
            }

            // Aynı uygulama yoluna sahip kuralları birleştir (Gelen + Giden tek satır)
            var mergedList = new List<FirewallRuleInfo>();
            var grouped = rawRules.GroupBy(r => (r.ApplicationPath ?? "").ToLowerInvariant());

            foreach (var group in grouped)
            {
                var list = group.ToList();
                if (list.Count == 0) continue;

                var inRule = list.FirstOrDefault(r => r.RawDirection == 1);
                var outRule = list.FirstOrDefault(r => r.RawDirection == 2);

                if (inRule != null && outRule != null)
                {
                    mergedList.Add(new FirewallRuleInfo
                    {
                        Name = inRule.Name,
                        ApplicationPath = inRule.ApplicationPath,
                        InboundRuleName = inRule.Name,
                        OutboundRuleName = outRule.Name,
                        RawDirection = 3, // 3 = Gelen + Giden
                        RawAction = inRule.RawAction,
                        IsEnabled = inRule.IsEnabled || outRule.IsEnabled
                    });
                }
                else
                {
                    foreach (var item in list)
                    {
                        if (item.RawDirection == 1) item.InboundRuleName = item.Name;
                        if (item.RawDirection == 2) item.OutboundRuleName = item.Name;
                        mergedList.Add(item);
                    }
                }
            }

            return mergedList;
        }

        /// <summary>
        /// Belirli uygulama yollarına ait HaYTooL_ prefix'li tüm kuralları siler.
        /// Senkronizasyon öncesinde eski EXE kurallarını temizlemek için kullanılır.
        /// </summary>
        public static int RemoveRulesByApplicationPaths(IEnumerable<string> appPaths)
        {
            int removed = 0;
            try
            {
                Type typeFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2")
                                     ?? throw new Exception("Firewall COM nesnesi bulunamadı.");
                dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2)!;

                // Kural adlarını önce topla (foreach içinde silme yapılmaz)
                var toRemove = new List<string>();
                var pathSet = new HashSet<string>(appPaths.Select(p => p.ToLowerInvariant()));

                foreach (dynamic rule in fwPolicy2.Rules)
                {
                    string name = rule.Name as string ?? string.Empty;
                    if (!name.StartsWith(RulePrefix)) continue;

                    string appPath = (rule.ApplicationName as string ?? string.Empty).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(appPath) && pathSet.Contains(appPath))
                        toRemove.Add(name);
                }

                dynamic rules = fwPolicy2.Rules;
                foreach (string name in toRemove)
                {
                    RemoveRuleIfExist(rules, name);
                    removed++;
                }
            }
            catch { }
            return removed;
        }

        /// <summary>
        /// Sistemin çökmesini önlemek için korunması gereken kritik sistem yolları.
        /// </summary>
        public static bool IsProtectedSystemPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            string lowerPath = path.ToLowerInvariant();
            
            if (lowerPath.Contains(@"\windows\system32\") || 
                lowerPath.Contains(@"\windows\syswow64\"))
            {
                return true;
            }
            return false;
        }
    }

    public class FirewallRuleInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ApplicationPath { get; set; } = string.Empty;
        public int RawDirection { get; set; } // 1 = Inbound, 2 = Outbound, 3 = Both
        public int RawAction { get; set; } // 0 = Block, 1 = Allow
        public bool IsEnabled { get; set; } = true;

        public string? InboundRuleName { get; set; }
        public string? OutboundRuleName { get; set; }
        public bool IsMerged => !string.IsNullOrEmpty(InboundRuleName) && !string.IsNullOrEmpty(OutboundRuleName);

        public string LocalizedDirection
        {
            get
            {
                if (IsMerged || RawDirection == 3)
                {
                    return LanguageManager.CurrentLanguage switch
                    {
                        "TR" => "Gelen + Giden",
                        "EN" => "Inbound + Outbound",
                        "ES" => "Entrante + Saliente",
                        "DE" => "Eingehend + Ausgehend",
                        "PT" => "Entrada + Saída",
                        "AR" => "وارد + صادرة",
                        "RU" => "Входящее + Исходящее",
                        _ => "Gelen + Giden"
                    };
                }

                return RawDirection == 1 
                    ? (LanguageManager.CurrentLanguage == "TR" ? "Gelen" : "Inbound")
                    : (LanguageManager.CurrentLanguage == "TR" ? "Giden" : "Outbound");
            }
        }

        public string LocalizedStatus => IsEnabled 
            ? ("🟢 " + LanguageManager.Get("Active"))
            : ("🔴 " + LanguageManager.Get("Passive"));

        public string StatusColor => IsEnabled ? "#22C55E" : "#EF4444";
    }
}
