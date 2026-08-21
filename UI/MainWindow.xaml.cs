using GuvenlikDuvarim.Core.Firewall;
using GuvenlikDuvarim.Core.I18n;
using GuvenlikDuvarim.Core.Scanner;
using GuvenlikDuvarim.Core.Storage;
using GuvenlikDuvarim.Core.Utils;
using GuvenlikDuvarim.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GuvenlikDuvarim.UI
{
    public partial class MainWindow : Window
    {
        private List<CategoryModel> _categories = new();
        private AppSettings _settings = new();
        private List<FirewallRuleInfo> _allActiveRules = new();
        private List<ContentTreeNode> _currentCategoryNodes = new();
        private ProcessWindow? _processWindow = null;

        private bool _hasUpdateAvailable = false;
        private string _onlineLatestVersion = string.Empty;
        private readonly string _latestReleaseUrl = "https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/latest";

        private int _gistStatusState = 0; // 0: Idle, 1: Uploading, 2: Success, 3: Error
        private string _lastGistStatusMessage = string.Empty;

        private void UpdateGistButtonUi()
        {
            if (btnGistBackup == null) return;
            switch (_gistStatusState)
            {
                case 1:
                    btnGistBackup.Content = LanguageManager.Get("GistUploading");
                    break;
                case 2:
                    btnGistBackup.Content = LanguageManager.Get("GistUploaded");
                    btnGistBackup.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E"));
                    btnGistBackup.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    btnGistBackup.ToolTip = LanguageManager.Get("GistToolTipSuccess");
                    break;
                case 3:
                    btnGistBackup.Content = LanguageManager.Get("GistError");
                    btnGistBackup.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
                    btnGistBackup.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    btnGistBackup.ToolTip = LanguageManager.Get("GistToolTipError");
                    break;
                default:
                    btnGistBackup.Content = LanguageManager.Get("HeaderGistBackup");
                    btnGistBackup.ToolTip = LanguageManager.Get("GistTitle");
                    break;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadVersionBadgeFromRoot();
            LoadSavedWindowBounds();
            LoadSavedTheme();
            LoadDataFromIni();
            MigrateAndLoadRules();
            GuvenlikDuvarim.Core.Utils.LogManager.Log("Uygulama başlatıldı.");
            Loaded += (s, e) =>
            {
                RegisterIpcMessageHandler();
                PerformStartupAutoBackups();
                CheckForUpdatesAsync();
            };
        }

        /// <summary>
        /// CLI veya harici süreçlerden gelen canlı GUI yenileme mesajını (WM_HAYTOOL_REFRESH) dinler.
        /// </summary>
        private void RegisterIpcMessageHandler()
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);
            }
            catch (Exception ex)
            {
                GuvenlikDuvarim.Core.Utils.LogManager.Log($"IPC mesaj dinleyici kurulurken hata: {ex}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((uint)msg == GuvenlikDuvarim.Core.CLI.CliManager.WM_HAYTOOL_REFRESH)
            {
                Dispatcher.Invoke(() =>
                {
                    LoadDataFromIni();
                    if (lstCategories != null && lstCategories.SelectedItem is CategoryModel selectedCat)
                    {
                        RefreshItems(selectedCat);
                    }
                });
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void LoadVersionBadgeFromRoot()
        {
            try
            {
                // 1. Yerel / Debug ortamında VERSION dosyasını dene
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string vFile = System.IO.Path.Combine(baseDir, "VERSION");
                if (!System.IO.File.Exists(vFile))
                {
                    vFile = System.IO.Path.Combine(baseDir, "..", "..", "..", "VERSION");
                }
                if (!System.IO.File.Exists(vFile))
                {
                    vFile = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "VERSION");
                }

                if (System.IO.File.Exists(vFile))
                {
                    string verStr = System.IO.File.ReadAllText(vFile).Trim();
                    if (!string.IsNullOrWhiteSpace(verStr))
                    {
                        if (txtVersionBadge != null) txtVersionBadge.Text = verStr.StartsWith("v") ? verStr : "v" + verStr;
                        return;
                    }
                }

                // 2. Single-File derlemesinde AssemblyInformationalVersion özniteliğinden okuma (dotnet publish -p:Version= ile otomatik gömülür)
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var infoAttr = asm.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
                if (infoAttr != null && !string.IsNullOrWhiteSpace(infoAttr.InformationalVersion))
                {
                    string cleanVer = infoAttr.InformationalVersion.Split('+')[0].Trim();
                    if (!string.IsNullOrWhiteSpace(cleanVer) && cleanVer != "1.0.0")
                    {
                        if (txtVersionBadge != null) txtVersionBadge.Text = cleanVer.StartsWith("v") ? cleanVer : "v" + cleanVer;
                        return;
                    }
                }

                var asmVer = asm.GetName().Version;
                if (asmVer != null && txtVersionBadge != null)
                {
                    txtVersionBadge.Text = $"v{asmVer.Major}.{asmVer.Minor}.{asmVer.Build}";
                }
            }
            catch { }
        }

        /// <summary>
        /// Eski prefix'li (GuvenlikDuvarim_) kuralları temizler, ardından aktif kuralları yükler.
        /// </summary>
        private void MigrateAndLoadRules()
        {
            try
            {
                int removed = FirewallManager.MigrateOldRules();
                if (removed > 0)
                {
                    // Sessiz migrasyon — kullanıcıya bildirim gerekmez, sadece yükleme
                    System.Diagnostics.Debug.WriteLine($"[HaYTooL] {removed} adet eski GuvenlikDuvarim_ kuralı temizlendi.");
                }
            }
            catch { }
        }

        private async void PerformStartupAutoBackups()
        {
            if (_settings.AutoBackupOnStartup)
            {
                BackupManager.AutoBackupOnStartup();
            }

            if (_settings.AutoGistOnStartup && !string.IsNullOrWhiteSpace(_settings.GitHubToken))
            {
                _gistStatusState = 1;
                UpdateGistButtonUi();
                try
                {
                    var result = await BackupManager.UploadToGistAsync(_settings.GitHubToken, _settings.LastGistId);
                    if (result.Success)
                    {
                        _settings.LastGistId = result.GistId;
                        _settings.LastGistUrl = result.GistUrl;
                        SaveDataToIni();

                        _gistStatusState = 2;
                        _lastGistStatusMessage = result.GistId;
                        UpdateGistButtonUi();
                    }
                    else
                    {
                        _gistStatusState = 3;
                        _lastGistStatusMessage = result.Message;
                        UpdateGistButtonUi();
                    }
                }
                catch (Exception ex)
                {
                    _gistStatusState = 3;
                    _lastGistStatusMessage = ex.Message;
                    UpdateGistButtonUi();
                }
            }
        }

        private void LoadSavedWindowBounds()
        {
            try
            {
                string topStr = IniStorage.ReadValue("Window", "Top", "");
                string leftStr = IniStorage.ReadValue("Window", "Left", "");
                string widthStr = IniStorage.ReadValue("Window", "Width", "");
                string heightStr = IniStorage.ReadValue("Window", "Height", "");
                string stateStr = IniStorage.ReadValue("Window", "State", "");

                if (double.TryParse(widthStr, out double w) && w >= 600) Width = w;
                if (double.TryParse(heightStr, out double h) && h >= 400) Height = h;

                if (double.TryParse(topStr, out double t) && double.TryParse(leftStr, out double l))
                {
                    if (t >= 0 && l >= 0 && l < SystemParameters.VirtualScreenWidth && t < SystemParameters.VirtualScreenHeight)
                    {
                        Top = t;
                        Left = l;
                        WindowStartupLocation = WindowStartupLocation.Manual;
                    }
                }

                if (Enum.TryParse<WindowState>(stateStr, out var state))
                {
                    WindowState = state;
                }
            }
            catch { }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            try
            {
                if (WindowState == WindowState.Normal)
                {
                    IniStorage.SaveValue("Window", "Top", Top.ToString());
                    IniStorage.SaveValue("Window", "Left", Left.ToString());
                    IniStorage.SaveValue("Window", "Width", Width.ToString());
                    IniStorage.SaveValue("Window", "Height", Height.ToString());
                }
                IniStorage.SaveValue("Window", "State", WindowState.ToString());
            }
            catch { }
        }

        private void LoadDataFromIni()
        {
            var data = IniStorage.LoadData();
            _categories = data.Categories;
            _settings = data.Settings;

            LanguageManager.CurrentLanguage = string.IsNullOrEmpty(_settings.Language) ? "TR" : _settings.Language;

            cmbLanguage.ItemsSource = LanguageManager.SupportedLanguages;
            cmbLanguage.SelectedValue = LanguageManager.CurrentLanguage;

            lstCategories.ItemsSource = null;
            lstCategories.ItemsSource = _categories;
            if (_categories.Count > 0 && lstCategories.SelectedItem == null)
            {
                lstCategories.SelectedIndex = 0;
            }

            UpdateFullSafeUi();
            ApplyLanguageText();
            UpdateHeaderRuleCounters();

            _dataLoaded = true;
        }

        private void UpdateHeaderRuleCounters()
        {
            List<FirewallRuleInfo> rawRules = new();
            try
            {
                rawRules = FirewallManager.GetRawActiveRules();
            }
            catch (Exception ex)
            {
                GuvenlikDuvarim.Core.Utils.LogManager.Log($"Kural sayacı güncellenirken hata: {ex}");
            }

            int inboundBlocked = 0;
            int outboundBlocked = 0;
            int inboundAllowed = 0;
            int outboundAllowed = 0;

            if (rawRules != null && rawRules.Count > 0)
            {
                foreach (var r in rawRules)
                {
                    if (!r.IsEnabled) continue;

                    if (r.RawAction == 0) // Block
                    {
                        if (r.RawDirection == 1) inboundBlocked++;
                        else if (r.RawDirection == 2) outboundBlocked++;
                    }
                    else if (r.RawAction == 1) // Allow
                    {
                        if (r.RawDirection == 1) inboundAllowed++;
                        else if (r.RawDirection == 2) outboundAllowed++;
                    }
                }
            }
            else
            {
                var processedExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var category in _categories)
                {
                    if (!category.IsEnabled) continue;
                    bool isAllowProfile = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);

                    foreach (var item in category.Items)
                    {
                        if (!item.IsFolder)
                        {
                            if (string.IsNullOrWhiteSpace(item.Path)) continue;
                            if (!processedExes.Add(item.Path)) continue;

                            if (isAllowProfile)
                            {
                                inboundAllowed++;
                                outboundAllowed++;
                            }
                            else
                            {
                                if (item.BlockInbound) inboundBlocked++; else inboundAllowed++;
                                if (item.BlockOutbound) outboundBlocked++; else outboundAllowed++;
                            }
                        }
                    }
                }
            }

            int totalBlocked = inboundBlocked + outboundBlocked;
            int totalAllowed = inboundAllowed + outboundAllowed;

            string lblBlocked = LanguageManager.Get("CounterBlocked");
            string lblAllowed = LanguageManager.Get("CounterAllowed");

            if (txtBlockedCount != null) txtBlockedCount.Text = $"⛔ {lblBlocked}: {totalBlocked}";
            if (txtBlockedDetail != null) txtBlockedDetail.Text = $" (⬇️{inboundBlocked} ⬆️{outboundBlocked})";

            if (txtAllowedCount != null) txtAllowedCount.Text = $"🟢 {lblAllowed}: {totalAllowed}";
            if (txtAllowedDetail != null) txtAllowedDetail.Text = $" (⬇️{inboundAllowed} ⬆️{outboundAllowed})";

            if (bdRuleCounters != null)
            {
                string tTipFormat = LanguageManager.Get("CounterToolTipFormat");
                try
                {
                    bdRuleCounters.ToolTip = string.Format(tTipFormat, totalBlocked, inboundBlocked, outboundBlocked, totalAllowed, inboundAllowed, outboundAllowed);
                }
                catch
                {
                    bdRuleCounters.ToolTip = $"Aktif Güvenlik Duvarı Bağlantı Kuralları:\n⛔ Toplam Engellenen: {totalBlocked} (⬇️ Gelen: {inboundBlocked} | ⬆️ Giden: {outboundBlocked})\n🟢 Toplam İzin Verilen: {totalAllowed} (⬇️ Gelen: {inboundAllowed} | ⬆️ Giden: {outboundAllowed})";
                }
            }
        }

        private void SaveDataToIni()
        {
            var currentSelected = lstCategories.SelectedItem;
            IniStorage.SaveData(_categories, _settings);
            lstCategories.ItemsSource = null;
            lstCategories.ItemsSource = _categories;

            if (currentSelected != null && _categories.Contains(currentSelected))
            {
                lstCategories.SelectedItem = currentSelected;
            }
            else if (_categories.Count > 0)
            {
                lstCategories.SelectedIndex = 0;
            }
        }

        private void ApplyLanguageText()
        {
            Title = LanguageManager.Get("AppTitle");
            txtHeaderTitle.Text = LanguageManager.Get("HeaderTitle");
            txtProfilesTitle.Text = LanguageManager.Get("ProfilesTitle");
            txtProfileContentTitle.Text = LanguageManager.Get("ProfileContentTitle");
            if (txtEmptyProfileTitle != null) txtEmptyProfileTitle.Text = LanguageManager.Get("EmptyProfileTitle");
            if (txtEmptyProfileSub != null) txtEmptyProfileSub.Text = LanguageManager.Get("EmptyProfileSub");

            btnAddCategory.Content = LanguageManager.Get("AddProfile");
            btnDeleteCategory.Content = LanguageManager.Get("DeleteProfile");
            btnAddExe.Content = LanguageManager.Get("AddExe");
            btnAddFolder.Content = LanguageManager.Get("AddFolder");
            btnRemoveItem.Content = LanguageManager.Get("RemoveItem");
            btnSyncFolders.Content = LanguageManager.Get("SyncFolders");
            btnSyncFolders.ToolTip = LanguageManager.Get("SyncHelp");
            if (btnExpandAll != null) btnExpandAll.Content = LanguageManager.Get("ExpandAll");
            if (btnCollapseAll != null) btnCollapseAll.Content = LanguageManager.Get("CollapseAll");
            if (miExpandAll != null) miExpandAll.Header = LanguageManager.Get("ExpandAll");
            if (miCollapseAll != null) miCollapseAll.Header = LanguageManager.Get("CollapseAll");
            if (btnDeleteAllRules != null)
            {
                btnDeleteAllRules.Content = LanguageManager.Get("DeleteAllRules");
                btnDeleteAllRules.ToolTip = LanguageManager.Get("DeleteAllRulesHelp");
            }

            btnRefreshHeader.Content = LanguageManager.Get("RefreshList");
            if (btnProcessManager != null) btnProcessManager.Content = LanguageManager.Get("TaskMgrButtonHeader");

            if (miCategoryToggle != null) miCategoryToggle.Header = LanguageManager.Get("CtxCategoryToggle");
            if (miCategoryRename != null) miCategoryRename.Header = LanguageManager.Get("CtxCategoryRename");
            if (miCategoryDelete != null) miCategoryDelete.Header = LanguageManager.Get("CtxCategoryDelete");

            if (miItemOpenLocation != null) miItemOpenLocation.Header = LanguageManager.Get("CtxItemOpenLocation");
            if (miItemRemove != null) miItemRemove.Header = LanguageManager.Get("CtxItemRemove");
            if (txtNewCategoryPlaceholder != null)
                txtNewCategoryPlaceholder.Text = LanguageManager.Get("NewProfilePlaceholder");

            if (btnLocalBackup != null)
            {
                btnLocalBackup.Content = LanguageManager.Get("HeaderLocalBackup");
                btnLocalBackup.ToolTip = LanguageManager.Get("BackupTitle");
            }

            if (btnOpenLog != null)
            {
                btnOpenLog.Content = LanguageManager.Get("BtnOpenLog");
                btnOpenLog.ToolTip = LanguageManager.Get("LogMaxCountLabel");
            }

            if (btnProcessManager != null)
            {
                btnProcessManager.Content = LanguageManager.Get("TaskMgrButtonHeader");
                btnProcessManager.ToolTip = LanguageManager.Get("TaskMgrTitle");
            }
            
            UpdateGistButtonUi();

            if (btnFullSafeHelp != null) btnFullSafeHelp.ToolTip = LanguageManager.Get("FullSafeHelpToolTip");
            if (cmbThemeSelector != null)
            {
                cmbThemeSelector.ToolTip = LanguageManager.Get("ThemeSelectorToolTip");
                if (cmbThemeSelector.Items.Count >= 4)
                {
                    if (cmbThemeSelector.Items[0] is ComboBoxItem item0) item0.Content = LanguageManager.Get("ThemeDark");
                    if (cmbThemeSelector.Items[1] is ComboBoxItem item1) item1.Content = LanguageManager.Get("ThemeLight");
                    if (cmbThemeSelector.Items[2] is ComboBoxItem item2) item2.Content = LanguageManager.Get("ThemeDiscord");
                    if (cmbThemeSelector.Items[3] is ComboBoxItem item3) item3.Content = LanguageManager.Get("ThemeYouTube");
                }
            }
            if (bdVersion != null)
            {
                if (_hasUpdateAvailable && !string.IsNullOrEmpty(_onlineLatestVersion))
                {
                    bdVersion.ToolTip = string.Format(LanguageManager.Get("UpdateAvailableToolTip"), _onlineLatestVersion);
                }
                else
                {
                    bdVersion.ToolTip = LanguageManager.Get("VersionBadgeToolTip");
                }
            }

            if (colIcon != null) colIcon.Header = LanguageManager.Get("ColHeaderIcon");
            if (colName != null) colName.Header = LanguageManager.Get("ColHeaderName");
            if (colPath != null) colPath.Header = LanguageManager.Get("ColHeaderPath");
            if (colInbound != null) colInbound.Header = LanguageManager.Get("ColHeaderInbound");
            if (colOutbound != null) colOutbound.Header = LanguageManager.Get("ColHeaderOutbound");
            if (colSync != null) colSync.Header = LanguageManager.Get("ColHeaderSync");

            if (miItemOpenLocation != null) miItemOpenLocation.Header = LanguageManager.Get("CtxOpenLocation");
            if (miItemRemove != null) miItemRemove.Header = LanguageManager.Get("CtxItemRemove");
            if (menuInbound != null) menuInbound.Header = LanguageManager.Get("MenuInbound");
            if (menuOutbound != null) menuOutbound.Header = LanguageManager.Get("MenuOutbound");
            if (miInboundBlock != null) miInboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miInboundAllow != null) miInboundAllow.Header = LanguageManager.Get("MenuActionAllow");
            if (miOutboundBlock != null) miOutboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miOutboundAllow != null) miOutboundAllow.Header = LanguageManager.Get("MenuActionAllow");

            if (btnWinFirewall != null)
            {
                btnWinFirewall.Content = LanguageManager.Get("BtnWinFirewallMenu");
                btnWinFirewall.ToolTip = LanguageManager.Get("BtnWindowsFirewallToolTip");
            }

            if (miWinFirewallConsole != null) miWinFirewallConsole.Header = LanguageManager.Get("MenuWinFirewallConsole");
            if (miWinFirewallEnable != null) miWinFirewallEnable.Header = LanguageManager.Get("MenuWinFirewallEnable");
            if (miWinFirewallDisable != null) miWinFirewallDisable.Header = LanguageManager.Get("MenuWinFirewallDisable");

            UpdateFullSafeUi();
            UpdateAdminStatusUi();
            UpdateHeaderRuleCounters();
            if (_processWindow != null && _processWindow.IsLoaded)
            {
                _processWindow.ApplyLanguageText();
            }
        }

        private void UpdateAdminStatusUi()
        {
            bool isAdmin = System.Security.Principal.WindowsIdentity.GetCurrent().Owner?.IsWellKnown(System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid) == true
                           || new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (isAdmin)
            {
                bdAdminStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                txtAdminStatus.Foreground = Brushes.White;
                txtAdminStatus.Text = LanguageManager.Get("AdminMode");
            }
            else
            {
                bdAdminStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                txtAdminStatus.Foreground = Brushes.White;
                txtAdminStatus.Text = LanguageManager.Get("LimitedMode");
            }
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLanguage.SelectedValue is string langCode && !string.IsNullOrEmpty(langCode))
            {
                _settings.Language = langCode;
                LanguageManager.CurrentLanguage = langCode;
                SaveDataToIni();
                ApplyLanguageText();
            }
        }

        private void UpdateFullSafeUi()
        {
            if (_settings.FullSafeMode)
            {
                btnToggleFullSafe.Content = LanguageManager.Get("FullSafeOn");
                btnToggleFullSafe.Style = (Style)FindResource("SuccessButton");
            }
            else
            {
                btnToggleFullSafe.Content = LanguageManager.Get("FullSafeOff");
                btnToggleFullSafe.Style = (Style)FindResource("DangerButton");
            }
        }

        private void BtnToggleFullSafe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _settings.FullSafeMode = !_settings.FullSafeMode;
                SaveDataToIni();

                FirewallManager.SetFullSafeMode(_settings.FullSafeMode);
                UpdateFullSafeUi();

                string statusMsg = _settings.FullSafeMode 
                    ? "FullSafe Modu AKTİF edildi!\n\nWindows varsayılan olarak TÜM giden bağlantıları engelledi.\nİstediğiniz uygulamalara izin vermek için profilde '🟢 İzin Ver (Allow)' seçeneğini işaretleyip uygulayın."
                    : "FullSafe Modu DEVRE DIŞI bırakıldı.\n\nWindows varsayılan güvenlik duvarı ayarlarına geri döndü.";

                MessageBox.Show(statusMsg, "FullSafe Modu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FullSafe Modu değiştirilirken yetki hatası oluştu.\nLütfen programı 'Yönetici Olarak Çalıştır' seçeneğiyle açın.\n\nHata Detayı: {ex.Message}", "Yönetici Yetkisi Gerekli", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [System.Runtime.InteropServices.DllImport("ole32.dll")]
        private static extern int RevokeDragDrop(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilter(uint msg, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);

        [System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError = true)]
        private static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, System.Text.StringBuilder? lpszFile, uint cch);

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern void DragFinish(IntPtr hDrop);

        private const uint WM_DROPFILES = 0x0233;
        private const uint WM_COPYDATA = 0x004A;
        private const uint WM_COPYGLOBALDATA = 0x0049;
        private const uint MSGFLT_ALLOW = 1;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                ChangeWindowMessageFilter(WM_DROPFILES, MSGFLT_ALLOW);
                ChangeWindowMessageFilter(WM_COPYDATA, MSGFLT_ALLOW);
                ChangeWindowMessageFilter(WM_COPYGLOBALDATA, MSGFLT_ALLOW);

                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    RevokeDragDrop(hwnd); // WPF'in OLE kilidini kaldırıp saf Win32 WM_DROPFILES modunu aktifleştirir
                    ChangeWindowMessageFilterEx(hwnd, WM_DROPFILES, MSGFLT_ALLOW, IntPtr.Zero);
                    ChangeWindowMessageFilterEx(hwnd, WM_COPYDATA, MSGFLT_ALLOW, IntPtr.Zero);
                    ChangeWindowMessageFilterEx(hwnd, WM_COPYGLOBALDATA, MSGFLT_ALLOW, IntPtr.Zero);
                    DragAcceptFiles(hwnd, true);

                    var hwndSource = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                    hwndSource?.AddHook(WndProcHook);
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)WM_DROPFILES)
            {
                handled = true;
                IntPtr hDrop = wParam;

                uint count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                var droppedPaths = new List<string>();

                for (uint i = 0; i < count; i++)
                {
                    var sb = new System.Text.StringBuilder(1024);
                    if (DragQueryFile(hDrop, i, sb, 1024) > 0)
                    {
                        droppedPaths.Add(sb.ToString());
                    }
                }
                DragFinish(hDrop);

                if (droppedPaths.Count > 0)
                {
                    ProcessSmartDrop(droppedPaths);
                }
            }
            return IntPtr.Zero;
        }

        private void BtnFullSafeHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(LanguageManager.Get("FullSafeHelp"), "FullSafe Modu Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnProcessManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_processWindow != null && _processWindow.IsLoaded)
                {
                    _processWindow.Activate();
                    if (_processWindow.WindowState == WindowState.Minimized)
                    {
                        _processWindow.WindowState = WindowState.Normal;
                    }
                    return;
                }

                _processWindow = new ProcessWindow(this);
                _processWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Görev Yöneticisi penceresi açılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void NotifyProfileItemAdded(CategoryModel category)
        {
            SaveDataToIni();
            _allActiveRules = FirewallManager.GetActiveRules();
            RefreshItems(category);
            UpdateHeaderRuleCounters();
        }

        private async Task RestoreAndApplyFromIniAsync()
        {
            ShowProgress("🔄 Eski Güvenlik Duvarı Kuralları Temizleniyor & Yedek Sıfırdan Uygulanıyor...");
            try
            {
                await Task.Run(() =>
                {
                    FirewallManager.RemoveAllHaYTooLRules();
                });

                LoadDataFromIni();

                foreach (var cat in _categories)
                {
                    if (cat.IsEnabled)
                    {
                        await ApplyCategoryRulesToFirewallAsync(cat, isSync: true);
                    }
                }
                MessageBox.Show("✅ Yedek sıfırdan başarıyla uygulandı! Tüm eski kurallar temizlendi ve yedeğin aktif profilleri uygulandı.", "Geri Yükleme Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geri yükleme sonrasında kurallar uygulanırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
            }
        }

        private async void BtnLocalBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new BackupWindow { Owner = this };
                if (dlg.ShowDialog() == true && dlg.Restored)
                {
                    await RestoreAndApplyFromIniAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yerel Yedek Yönetimi penceresi açılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGistBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new GistWindow { Owner = this };
                if (dlg.ShowDialog() == true && dlg.Restored)
                {
                    await RestoreAndApplyFromIniAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"GitHub Gist Bulut Yedekleme penceresi açılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Uygulama log dosyalarının bulunduğu klasörü Windows Gezgini'nde açar.
        /// </summary>
        private void BtnOpenLog_Click(object sender, RoutedEventArgs e)
        {
            GuvenlikDuvarim.Core.Utils.LogManager.OpenLogFolder();
        }

        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            string catName = txtNewCategory.Text.Trim();
            if (string.IsNullOrEmpty(catName)) return;

            if (_categories.Any(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"'{catName}' adında bir profil zaten mevcut.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isFullSafe = catName.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);
            var category = new CategoryModel 
            { 
                Name = catName,
                IsEnabled = true,
                IsAllowRule = isFullSafe
            };

            _categories.Add(category);
            SaveDataToIni();

            txtNewCategory.Clear();
            lstCategories.SelectedItem = category;
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
            {
                DeleteCategory(category);
            }
            else
            {
                MessageBox.Show("Lütfen önce silinecek bir profil seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteCategory(CategoryModel category)
        {
            if (MessageBox.Show($"'{category.Name}' profilini ve içerisindeki tüm öğeleri silmek istediğinizden emin misiniz?",
                                "Profil Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await RemoveCategoryRulesFromFirewallAsync(category);
                _categories.Remove(category);
                SaveDataToIni();
                dgContent.ItemsSource = null;
            }
        }

        private async Task RemoveCategoryRulesFromFirewallAsync(CategoryModel category)
        {
            ShowProgress("🗑️ Profil Kuralları Temizleniyor...");
            IProgress<ScanProgressReport> progress = new Progress<ScanProgressReport>(report =>
            {
                UpdateProgress(report.CurrentPath, report.FilesFoundCount);
            });

            int removedCount = 0;
            try
            {
                await Task.Run(() =>
                {
                    removedCount = RuleEngine.RemoveCategoryRules(category, progress);
                });
            }
            catch { }
            finally
            {
                _allActiveRules = FirewallManager.GetActiveRules();
                HideProgress();
            }
        }

        private int _currentRefreshId = 0;

        /// <summary>
        /// Seçili profile ait öğeleri Panel 2 DataGrid tablosuna anında yükler.
        /// Kök öğeler hemen gösterilir (klasörler varsayılan olarak daraltılmış gelir).
        /// </summary>
        private async void RefreshItems(CategoryModel category)
        {
            if (category == null)
            {
                _currentCategoryNodes.Clear();
                dgContent.ItemsSource = null;
                return;
            }

            int refreshId = ++_currentRefreshId;
            _currentCategoryNodes.Clear();
            dgContent.ItemsSource = null;

            if (category.Items.Count == 0)
            {
                txtEmptyProfileTitle.Text = LanguageManager.Get("EmptyProfileTitle");
                txtEmptyProfileSub.Text = LanguageManager.Get("EmptyProfileSub");
                borderEmptyProfile.Visibility = Visibility.Visible;
                return;
            }

            borderEmptyProfile.Visibility = Visibility.Collapsed;

            var folderNodes = new List<(AppItemModel Item, ContentTreeNode Node)>();

            foreach (var item in category.Items)
            {
                if (refreshId != _currentRefreshId) return;

                if (!item.IsFolder && category.Items.Any(f => f.IsFolder && item.Path.StartsWith(f.Path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string name = item.IsFolder
                    ? (Path.GetFileName(item.Path.TrimEnd('\\', '/')) is string fn && !string.IsNullOrEmpty(fn) ? fn : item.Path)
                    : Path.GetFileName(item.Path);

                var rs = RuleEngine.GetRuleStatus(item.Path, category, _allActiveRules);

                var node = new ContentTreeNode
                {
                    DisplayName = item.IsFolder ? $"📁 {name} (⏳ taranıyor...)" : $"📄 {name}",
                    FullPath = item.Path,
                    IsFolder = item.IsFolder,
                    IsExpanded = false, // Varsayılan olarak DARALTILMIŞ
                    IndentMargin = new Thickness(0),
                    InboundStatus = rs.InStatus,
                    InboundStatusColor = rs.InColor,
                    InboundBadgeBackground = rs.InBg,
                    OutboundStatus = rs.OutStatus,
                    OutboundStatusColor = rs.OutColor,
                    OutboundBadgeBackground = rs.OutBg,
                    SyncStatus = rs.SyncStatus,
                    SyncStatusColor = rs.SyncColor,
                    SyncBadgeBackground = rs.SyncBg
                };

                _currentCategoryNodes.Add(node);
                if (item.IsFolder)
                {
                    folderNodes.Add((item, node));
                }
            }

            RebuildDisplayList();

            foreach (var (item, node) in folderNodes)
            {
                if (refreshId != _currentRefreshId) return;

                string folderPath = item.Path;
                string folderName = Path.GetFileName(folderPath.TrimEnd('\\', '/'));
                if (string.IsNullOrEmpty(folderName)) folderName = folderPath;

                List<string> exeFiles = new();
                try
                {
                    exeFiles = await Task.Run(() => FileScanner.FindExeFiles(folderPath));
                }
                catch { }

                if (refreshId != _currentRefreshId) return;

                node.DisplayName = $"📁 {folderName} ({exeFiles.Count} EXE)";
                node.Children.Clear();

                if (exeFiles.Count == 0)
                {
                    node.Children.Add(new ContentTreeNode
                    {
                        DisplayName = "↳ 📥 Bu klasörde EXE bulunamadı",
                        FullPath = string.Empty,
                        IsFolder = false,
                        IndentMargin = new Thickness(20, 0, 0, 0),
                        InboundStatus = "-",
                        OutboundStatus = "-"
                    });
                }
                else
                {
                    foreach (var exe in exeFiles)
                    {
                        if (refreshId != _currentRefreshId) return;

                        var exeRs = RuleEngine.GetRuleStatus(exe, category, _allActiveRules);

                        node.Children.Add(new ContentTreeNode
                        {
                            DisplayName = $"↳ {Path.GetFileName(exe)}",
                            FullPath = exe,
                            IsFolder = false,
                            IndentMargin = new Thickness(20, 0, 0, 0),
                            InboundStatus = exeRs.InStatus,
                            InboundStatusColor = exeRs.InColor,
                            InboundBadgeBackground = exeRs.InBg,
                            OutboundStatus = exeRs.OutStatus,
                            OutboundStatusColor = exeRs.OutColor,
                            OutboundBadgeBackground = exeRs.OutBg,
                            SyncStatus = exeRs.SyncStatus,
                            SyncStatusColor = exeRs.SyncColor,
                            SyncBadgeBackground = exeRs.SyncBg
                        });
                    }
                }

                RebuildDisplayList();
            }
        }

        /// <summary>
        /// Daraltılmış/genişletilmiş klasör durumlarına göre tablo satırlarını bellekten anında oluşturur.
        /// </summary>
        private void RebuildDisplayList()
        {
            var displayList = new ObservableCollection<ContentTreeNode>();
            foreach (var parent in _currentCategoryNodes)
            {
                displayList.Add(parent);
                if (parent.IsFolder && parent.IsExpanded)
                {
                    foreach (var child in parent.Children)
                    {
                        displayList.Add(child);
                    }
                }
            }
            dgContent.ItemsSource = displayList;
        }

        private void BtnToggleExpand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ContentTreeNode node } && node.IsFolder)
            {
                node.IsExpanded = !node.IsExpanded;
                RebuildDisplayList();
            }
        }

        private void DataGridRow_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow { DataContext: ContentTreeNode node } && node.IsFolder)
            {
                node.IsExpanded = !node.IsExpanded;
                RebuildDisplayList();
                e.Handled = true;
            }
        }

        private void BtnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in _currentCategoryNodes.Where(n => n.IsFolder))
            {
                node.IsExpanded = true;
            }
            RebuildDisplayList();
        }

        private void BtnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in _currentCategoryNodes.Where(n => n.IsFolder))
            {
                node.IsExpanded = false;
            }
            RebuildDisplayList();
        }

        /// <summary>
        /// DataGrid'de seçili satıra karşılık gelen AppItemModel'i döndürür.
        /// </summary>
        private AppItemModel? GetSelectedItemModel()
        {
            if (dgContent.SelectedItem is ContentTreeNode node &&
                lstCategories.SelectedItem is CategoryModel category)
            {
                return category.Items.FirstOrDefault(i =>
                    i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
            }
            return null;
        }

        private List<CategoryModel> GetSelectedCategories()
        {
            var list = new List<CategoryModel>();
            foreach (var item in lstCategories.SelectedItems)
            {
                if (item is CategoryModel cat) list.Add(cat);
            }
            return list;
        }

        private List<ContentTreeNode> GetSelectedNodes()
        {
            var list = new List<ContentTreeNode>();
            foreach (var item in dgContent.SelectedItems)
            {
                if (item is ContentTreeNode n) list.Add(n);
            }
            return list;
        }

        public List<CategoryModel> Categories => _categories;

        public void SelectCategory(CategoryModel category)
        {
            if (category != null && _categories.Contains(category))
            {
                lstCategories.SelectedItem = category;
            }
        }

        private void LstCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCategories.SelectedItems.Count == 1 && lstCategories.SelectedItem is CategoryModel category)
            {
                txtSelectedCountHint.Text = "";
                RefreshItems(category);
            }
            else if (lstCategories.SelectedItems.Count > 1)
            {
                var count = lstCategories.SelectedItems.Count;
                string text = string.Format(LanguageManager.Get("ProfilesSelectedCount"), count);
                txtSelectedCountHint.Text = text;
                dgContent.ItemsSource = null;
            }
            else
            {
                txtSelectedCountHint.Text = "";
                // Varsayılan: ilk profili göster (eski davranış)
                if (_categories.Count > 0 && lstCategories.SelectedItem == null)
                {
                    lstCategories.SelectedIndex = 0;
                    return;
                }
                if (lstCategories.SelectedItem is CategoryModel activeCategory)
                {
                    RefreshItems(activeCategory);
                }
            }
        }

        public CategoryModel? GetSelectedOrActiveCategory()
        {
            if (lstCategories.SelectedItem is CategoryModel selected)
            {
                return selected;
            }

            if (_categories.Count > 0)
            {
                lstCategories.SelectedIndex = 0;
                return _categories[0];
            }

            MessageBox.Show("Lütfen önce sol menüden bir profil ekleyin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        private void BtnAddExe_Click(object sender, RoutedEventArgs e)
        {
            var category = GetSelectedOrActiveCategory();
            if (category == null) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Çalıştırılabilir Dosyalar (*.exe)|*.exe",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    if (FirewallManager.IsProtectedSystemPath(file))
                    {
                        MessageBox.Show($"DİKKAT: '{file}' kritik bir sistem dosyasıdır ve engellenmesi ağ çökmesine yol açabilir. Eklenmedi.", "Sistem Koruması", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    if (!category.Items.Any(i => i.Path.Equals(file, StringComparison.OrdinalIgnoreCase)))
                    {
                        category.Items.Add(new AppItemModel { Path = file, IsFolder = false, BlockInbound = true, BlockOutbound = true });
                        if (category.IsEnabled)
                        {
                            bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);
                            string appName = FirewallManager.GetAppRuleKey(file);
                            FirewallManager.ApplyRule(appName, file, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: isAllow);
                        }
                    }
                }
                SaveDataToIni();
                _allActiveRules = FirewallManager.GetActiveRules();
                RefreshItems(category);
            }
        }

        // SÜRÜKLE - BIRAK (DRAG & DROP) DESTEĞİ (KÖK PENCERE VE PANEL EVENTS)
        private void Window_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems && droppedItems.Length > 0)
            {
                e.Handled = true;
                ProcessSmartDrop(droppedItems);
            }
        }

        private void ProcessSmartDrop(IEnumerable<string> droppedPaths)
        {
            var category = GetSelectedOrActiveCategory();
            if (category == null) return;

            bool folderAdded = false;
            bool exeAdded = false;

            foreach (string rawPath in droppedPaths)
            {
                string path = rawPath.Trim('"', ' ');

                if (Directory.Exists(path))
                {
                    if (FirewallManager.IsProtectedSystemPath(path))
                    {
                        MessageBox.Show($"DİKKAT: '{path}' kritik bir sistem yoludur ve engellenmesi ağ çökmesine yol açabilir. Eklenmedi.", "Sistem Koruması", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    if (!category.Items.Any(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    {
                        category.Items.Add(new AppItemModel { Path = path, IsFolder = true, BlockInbound = true, BlockOutbound = true });
                        folderAdded = true;

                        if (category.IsEnabled)
                        {
                            bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);
                            var exeFiles = FileScanner.FindExeFiles(path);
                            foreach (var exe in exeFiles)
                            {
                                if (!FirewallManager.IsProtectedSystemPath(exe))
                                {
                                    string appName = FirewallManager.GetAppRuleKey(exe);
                                    FirewallManager.ApplyRule(appName, exe, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: isAllow);
                                }
                            }
                        }
                    }
                }
                else if (File.Exists(path) && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (FirewallManager.IsProtectedSystemPath(path))
                    {
                        MessageBox.Show($"DİKKAT: '{path}' kritik bir sistem dosyasıdır. Eklenmedi.", "Sistem Koruması", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    if (!category.Items.Any(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    {
                        category.Items.Add(new AppItemModel { Path = path, IsFolder = false, BlockInbound = true, BlockOutbound = true });
                        exeAdded = true;

                        // Anında Windows Güvenlik Duvarı Kuralı Ekle
                        if (category.IsEnabled)
                        {
                            bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);
                            string appName = FirewallManager.GetAppRuleKey(path);
                            FirewallManager.ApplyRule(appName, path, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: isAllow);
                        }
                    }
                }
            }

            if (folderAdded || exeAdded)
            {
                SaveDataToIni();
                _allActiveRules = FirewallManager.GetActiveRules();
                RefreshItems(category);
            }

            if (exeAdded)
            {
            }
        }

        private async void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var category = GetSelectedOrActiveCategory();
            if (category == null) return;

            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                string folder = dialog.FolderName;
                
                if (FirewallManager.IsProtectedSystemPath(folder))
                {
                    MessageBox.Show($"DİKKAT: '{folder}' kritik bir sistem klasörüdür ve engellenmesi ağ çökmesine yol açabilir. Eklenmedi.", "Sistem Koruması", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!category.Items.Any(i => i.Path.Equals(folder, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowProgress("🔍 Klasör Taranıyor ve Varsayılan Engelleme Kuralları Uygulanıyor...", folder);

                    var progress = new Progress<ScanProgressReport>(report =>
                    {
                        UpdateProgress(report.CurrentPath, report.FilesFoundCount);
                    });

                    await Task.Run(() =>
                    {
                        var exeFiles = FileScanner.FindExeFiles(folder, progress);
                        if (category.IsEnabled)
                        {
                            bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);
                            foreach (var exe in exeFiles)
                            {
                                if (!FirewallManager.IsProtectedSystemPath(exe))
                                {
                                    string appName = FirewallManager.GetAppRuleKey(exe);
                                    FirewallManager.ApplyRule(appName, exe, blockInbound: true, blockOutbound: true, isEnabled: true, isAllow: isAllow);
                                }
                            }
                        }
                    });

                    category.Items.Add(new AppItemModel { Path = folder, IsFolder = true, BlockInbound = true, BlockOutbound = true });
                    SaveDataToIni();
                    HideProgress();
                    _allActiveRules = FirewallManager.GetActiveRules();
                    RefreshItems(category);
                }
            }
        }

        // Öğeyi ve (Klasör ise altındaki tüm EXE'lerin) güvenlik duvarı kurallarını temizleme
        private async void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is not CategoryModel category)
            {
                MessageBox.Show("Önce sol menüden bir profil seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var nodes = GetSelectedNodes();
            if (nodes.Count == 0)
            {
                MessageBox.Show("Listeden çıkarmak istediğiniz öğeleri seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var toRemove = new List<AppItemModel>();
            bool skippedAny = false;
            foreach (var node in nodes)
            {
                var item = category.Items.FirstOrDefault(i => i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    toRemove.Add(item);
                }
                else
                {
                    skippedAny = true;
                }
            }

            if (skippedAny)
            {
                MessageBox.Show("Bazı seçili EXE'ler bir klasörün içinden taranarak bulunmuştur ve ayrı ayrı kaldırılamaz.\nOnları çıkarmak için üst klasör düğümünü kaldırın.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (toRemove.Count == 0) return;

            bool folderRemoval = toRemove.Any(x => x.IsFolder);
            if (folderRemoval)
            {
                ShowProgress("🗑️ Kurallar Temizleniyor...");
            }

            try
            {
                await Task.Run(() =>
                {
                    IProgress<ScanProgressReport>? progress = folderRemoval
                        ? new Progress<ScanProgressReport>(report => UpdateProgress(report.CurrentPath, report.FilesFoundCount))
                        : null;

                    foreach (var itrm in toRemove)
                    {
                        if (itrm.IsFolder)
                        {
                            var exeFiles = progress != null
                                ? FileScanner.FindExeFiles(itrm.Path, progress)
                                : FileScanner.FindExeFiles(itrm.Path);
                            foreach (var exe in exeFiles)
                            {
                                FirewallManager.RemoveRulesByPath(exe);
                            }
                        }
                        else
                        {
                            FirewallManager.RemoveRulesByPath(itrm.Path);
                        }
                    }
                });
            }
            catch { }
            finally
            {
                HideProgress();
            }

            foreach (var itrm in toRemove)
            {
                category.Items.Remove(itrm);
            }
            SaveDataToIni();
            _allActiveRules = FirewallManager.GetActiveRules();
            RefreshItems(category);
        }

        private void OpenInExplorer(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", $"\"{path}\"");
                }
                else
                {
                    MessageBox.Show("Belirtilen dosya veya klasör sistemde bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Konum açılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshHeader_Click(object sender, RoutedEventArgs e)
        {
            // Profile seçiliyse içeriği yenile
            if (lstCategories.SelectedItem is CategoryModel category)
                RefreshItems(category);
        }

        private void BtnWinFirewall_Click(object sender, RoutedEventArgs e)
        {
            if (btnWinFirewall.ContextMenu != null)
            {
                btnWinFirewall.ContextMenu.PlacementTarget = btnWinFirewall;
                btnWinFirewall.ContextMenu.IsOpen = true;
            }
        }

        private void MiWinFirewallConsole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wf.msc",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Windows Güvenlik Duvarı konsolu açılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MiWinFirewallEnable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgress("🟢 Windows Güvenlik Duvarı Etkinleştiriliyor...");
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "advfirewall set allprofiles state on",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Verb = "runas"
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit();
                });
                HideProgress();
                MessageBox.Show("🟢 Windows Güvenlik Duvarı tüm profiller için başarıyla etkinleştirildi (AÇILDI).", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                HideProgress();
                MessageBox.Show($"Güvenlik duvarı etkinleştirilirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MiWinFirewallDisable_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("⚠️ DİKKAT: Windows Güvenlik Duvarı'nı kapatmak bilgisayarınızı dış tehditlere karşı savunmasız bırakabilir.\n\nDevam etmek istediğinizden emin misiniz?", "Güvenlik Uyarısı", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    ShowProgress("🔴 Windows Güvenlik Duvarı Devre Dışı Bırakılıyor...");
                    await Task.Run(() =>
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = "advfirewall set allprofiles state off",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            Verb = "runas"
                        };
                        using var p = Process.Start(psi);
                        p?.WaitForExit();
                    });
                    HideProgress();
                    MessageBox.Show("🔴 Windows Güvenlik Duvarı tüm profiller için devre dışı bırakıldı (KAPATILDI).", "İşlem Başarılı", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    HideProgress();
                    MessageBox.Show($"Güvenlik duvarı kapatılırken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                row.Focus();
                if (!row.IsSelected)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                    {
                        dgContent.SelectedItems.Clear();
                    }
                    row.IsSelected = true;
                }
            }
        }

        private void ContextMenu_Opening(object sender, RoutedEventArgs e)
        {
            if (miItemOpenLocation != null) miItemOpenLocation.Header = LanguageManager.Get("CtxOpenLocation");
            if (miItemRemove != null) miItemRemove.Header = LanguageManager.Get("CtxItemRemove");
            if (miItemBothBlock != null) miItemBothBlock.Header = LanguageManager.Get("MenuActionBlockAll");
            if (miItemBothAllow != null) miItemBothAllow.Header = LanguageManager.Get("MenuActionAllowAll");
            if (menuInbound != null) menuInbound.Header = LanguageManager.Get("MenuInbound");
            if (menuOutbound != null) menuOutbound.Header = LanguageManager.Get("MenuOutbound");
            if (miInboundBlock != null) miInboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miInboundAllow != null) miInboundAllow.Header = LanguageManager.Get("MenuActionAllow");
            if (miOutboundBlock != null) miOutboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miOutboundAllow != null) miOutboundAllow.Header = LanguageManager.Get("MenuActionAllow");

            if (miItemBothBlock != null && miItemBothAllow != null)
            {
                bool folderSelected = dgContent.SelectedItems.Count > 0 &&
                    dgContent.SelectedItems.Cast<ContentTreeNode>().All(n => n.IsFolder);
                miItemBothBlock.Visibility = folderSelected ? Visibility.Visible : Visibility.Collapsed;
                miItemBothAllow.Visibility = folderSelected ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async Task SetItemRuleStatusAsync(ContentTreeNode node, bool? blockInbound, bool? blockOutbound)
        {
            var category = GetSelectedOrActiveCategory();
            if (category == null || string.IsNullOrEmpty(node.FullPath)) return;
            await SetItemsRuleStatusAsync(new[] { node }, blockInbound, blockOutbound, category);
        }

        private async Task SetItemsRuleStatusAsync(IEnumerable<ContentTreeNode> nodes, bool? blockInbound, bool? blockOutbound, CategoryModel? category = null)
        {
            var nodeList = nodes.ToList();
            if (nodeList.Count == 0) return;
            category ??= GetSelectedOrActiveCategory();
            if (category == null) return;

            string statusMsg = blockInbound.HasValue && blockOutbound.HasValue
                ? (blockInbound.Value && blockOutbound.Value ? "⛔ Gelen & Giden Bağlantılar Engelleniyor..." : "🟢 Gelen & Giden Bağlantılara İzin Veriliyor...")
                : blockInbound.HasValue
                    ? (blockInbound.Value ? "⛔ Gelen Bağlantı Engelleniyor..." : "🟢 Gelen Bağlantıya İzin Veriliyor...")
                    : (blockOutbound.Value ? "⛔ Giden Bağlantı Engelleniyor..." : "🟢 Giden Bağlantıya İzin Veriliyor...");

            ShowProgress(statusMsg);

            try
            {
                await Task.Run(() =>
                {
                    bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);

                    foreach (var node in nodeList)
                    {
                        if (string.IsNullOrEmpty(node.FullPath)) continue;

                        if (node.IsFolder)
                        {
                            // 1. Model güncelle: klasör için AppItemModel bul veya ekle
                            var folderItem = category.Items.FirstOrDefault(i => i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
                            if (folderItem == null)
                            {
                                folderItem = new AppItemModel { Path = node.FullPath, IsFolder = true };
                                category.Items.Add(folderItem);
                            }

                            if (blockInbound.HasValue) folderItem.BlockInbound = blockInbound.Value;
                            if (blockOutbound.HasValue) folderItem.BlockOutbound = blockOutbound.Value;

                            // Klasör altındaki listede kayıtlı tüm alt EXE öğelerini de klasörün yeni ayarıyla güncelle
                            var childItems = category.Items.Where(i => !i.IsFolder && i.Path.StartsWith(node.FullPath, StringComparison.OrdinalIgnoreCase)).ToList();
                            foreach (var child in childItems)
                            {
                                if (blockInbound.HasValue) child.BlockInbound = blockInbound.Value;
                                if (blockOutbound.HasValue) child.BlockOutbound = blockOutbound.Value;
                            }

                            // 2. Güvenlik duvarına uygula
                            bool inVal = folderItem.BlockInbound;
                            bool outVal = folderItem.BlockOutbound;

                            var exeFiles = FileScanner.FindExeFiles(node.FullPath);
                            foreach (var exe in exeFiles)
                            {
                                if (!FirewallManager.IsProtectedSystemPath(exe))
                                {
                                    string name = FirewallManager.GetAppRuleKey(exe);
                                    RuleEngine.ApplyOrRemove(name, exe, inVal, outVal, category.IsEnabled, isAllow);
                                }
                            }
                        }
                        else
                        {
                            // 1. Model güncelle: EXE için özel AppItemModel oluştur veya güncelle
                            var exeItem = category.Items.FirstOrDefault(i => i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
                            if (exeItem == null)
                            {
                                var parentFolder = category.Items.FirstOrDefault(i => i.IsFolder && node.FullPath.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase));
                                bool initialIn = parentFolder?.BlockInbound ?? category.BlockInbound;
                                bool initialOut = parentFolder?.BlockOutbound ?? category.BlockOutbound;

                                exeItem = new AppItemModel
                                {
                                    Path = node.FullPath,
                                    IsFolder = false,
                                    BlockInbound = initialIn,
                                    BlockOutbound = initialOut
                                };
                                category.Items.Add(exeItem);
                            }

                            if (blockInbound.HasValue) exeItem.BlockInbound = blockInbound.Value;
                            if (blockOutbound.HasValue) exeItem.BlockOutbound = blockOutbound.Value;

                            // 2. Güvenlik duvarına uygula
                            var exeParentFolder = category.Items.FirstOrDefault(i => i.IsFolder && node.FullPath.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase));
                            bool exeInVal = exeItem.BlockInbound;
                            bool exeOutVal = exeItem.BlockOutbound;

                            if (!FirewallManager.IsProtectedSystemPath(node.FullPath))
                            {
                                string name = FirewallManager.GetAppRuleKey(node.FullPath);
                                RuleEngine.ApplyOrRemove(name, node.FullPath, exeInVal, exeOutVal, category.IsEnabled, isAllow);
                            }
                        }
                    }
                });

                SaveDataToIni();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kural güncellenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
                _allActiveRules = FirewallManager.GetActiveRules();
                RefreshItems(category);
            }
        }

        private async void CtxItemBothBlock_Click(object sender, RoutedEventArgs e)
        {
            await SetItemsRuleStatusAsync(GetSelectedNodes(), blockInbound: true, blockOutbound: true);
        }

        private async void CtxItemBothAllow_Click(object sender, RoutedEventArgs e)
        {
            await SetItemsRuleStatusAsync(GetSelectedNodes(), blockInbound: false, blockOutbound: false);
        }

        private async void CtxInboundBlock_Click(object sender, RoutedEventArgs e)
        {
            await SetItemsRuleStatusAsync(GetSelectedNodes(), blockInbound: true, blockOutbound: null);
        }

        private async void CtxInboundAllow_Click(object sender, RoutedEventArgs e)
        {
            await SetItemsRuleStatusAsync(GetSelectedNodes(), blockInbound: false, blockOutbound: null);
        }

        private async void CtxOutboundBlock_Click(object sender, RoutedEventArgs e)
        {
            await SetItemsRuleStatusAsync(GetSelectedNodes(), blockInbound: null, blockOutbound: true);
        }

        private async void CtxOutboundAllow_Click(object sender, RoutedEventArgs e)
        {
            await SetItemsRuleStatusAsync(GetSelectedNodes(), blockInbound: null, blockOutbound: false);
        }

        private void CtxOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (dgContent.SelectedItem is ContentTreeNode node && !string.IsNullOrEmpty(node.FullPath))
            {
                OpenInExplorer(node.FullPath);
            }
        }

        // İşlem sırasında dokunulamayacak kontroller
        private readonly List<Control> _actionControls = new();

        private void ShowProgress(string title, string initialPath = "")
        {
            txtProgressTitle.Text = title;
            txtProgressCount.Text = "0 .exe bulundu";
            txtProgressCurrentPath.Text = initialPath;
            gridProgressOverlay.Visibility = Visibility.Visible;

            // Kontrolleri tıklanamaz yap (IsEnabled yerine IsHitTestVisible kullanarak tema rengini koru)
            _actionControls.Clear();
            foreach (var ctrl in new Control[] { btnAddExe, btnAddFolder, btnRemoveItem,
                                                  btnSyncFolders, btnDeleteAllRules, btnAddCategory, btnDeleteCategory, lstCategories })
            {
                ctrl.IsHitTestVisible = false;
                _actionControls.Add(ctrl);
            }
        }

        private void UpdateProgress(string currentPath, int count)
        {
            txtProgressCount.Text = $"{count} .exe bulundu";
            txtProgressCurrentPath.Text = currentPath;
        }

        private void HideProgress()
        {
            gridProgressOverlay.Visibility = Visibility.Collapsed;
            // Kontrollerin tıklanabilirliğini tekrar aç
            foreach (var ctrl in _actionControls)
                ctrl.IsHitTestVisible = true;
            _actionControls.Clear();
        }

        /// <summary>
        /// HaYTooL Firewall tarafından Windows Güvenlik Duvarı'nda oluşturulmuş TÜM kuralları onaylı olarak siler.
        /// Canlı ilerleme (Progress Bar & Canlı Sayaç) ve donmasız (async/await) arka plan işlemi sunar.
        /// </summary>
        private async void BtnDeleteAllRules_Click(object sender, RoutedEventArgs e)
        {
            var confirmResult = MessageBox.Show(
                LanguageManager.Get("DeleteAllRulesConfirm"),
                LanguageManager.Get("DeleteAllRules"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmResult != MessageBoxResult.Yes) return;

            string titleProgress = LanguageManager.Get("DeletingRulesProgress");
            ShowProgress(titleProgress);

            string countFormat = LanguageManager.Get("RulesDeletedCountFormat");

            var progress = new Progress<(string RuleName, int RemovedCount, int TotalCount)>(report =>
            {
                txtProgressCount.Text = string.Format(countFormat, report.RemovedCount, report.TotalCount);
                txtProgressCurrentPath.Text = report.RuleName;
                if (pbScanProgress != null)
                {
                    pbScanProgress.IsIndeterminate = false;
                    pbScanProgress.Minimum = 0;
                    pbScanProgress.Maximum = report.TotalCount > 0 ? report.TotalCount : 1;
                    pbScanProgress.Value = report.RemovedCount;
                }
            });

            int removedCount = 0;

            try
            {
                await Task.Run(() =>
                {
                    removedCount = FirewallManager.RemoveAllHaYTooLRules(progress);
                });

                HideProgress();
                if (pbScanProgress != null)
                {
                    pbScanProgress.IsIndeterminate = true;
                }

                string successMessage = string.Format(LanguageManager.Get("DeleteAllRulesSuccess"), removedCount);
                MessageBox.Show(successMessage, LanguageManager.Get("DeleteAllRules"), MessageBoxButton.OK, MessageBoxImage.Information);

                if (lstCategories.SelectedItem is CategoryModel selectedCat)
                {
                    _allActiveRules = FirewallManager.GetActiveRules();
                    RefreshItems(selectedCat);
                }
            }
            catch (Exception ex)
            {
                HideProgress();
                if (pbScanProgress != null)
                {
                    pbScanProgress.IsIndeterminate = true;
                }
                MessageBox.Show($"Kurallar silinirken bir hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Tüm profillerdeki tüm klasörleri ve alt klasörleri tara.
        /// Silinen/adı değişen EXE'lerin eski kurallarını kaldırır ve güncel EXE'ler için kuralları yeniler.
        /// </summary>
        private async void BtnSyncFolders_Click(object sender, RoutedEventArgs e)
        {
            if (_categories.Count == 0)
            {
                MessageBox.Show("Sistemde senkronize edilecek hiçbir profil bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tüm profillerdeki klasörleri topla
            var allProfileFolders = _categories
                .SelectMany(cat => cat.Items.Where(item => item.IsFolder).Select(item => (Category: cat, FolderItem: item)))
                .ToList();

            if (!allProfileFolders.Any())
            {
                MessageBox.Show("Tüm profiller kontrol edildi: Senkronize edilecek hiçbir klasör bulunamadı.\nSadece doğrudan eklenen EXE'ler için senkronizasyon gerekmez.", 
                                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowProgress("🔄 Tüm Profiller Senkronize Ediliyor...");

            IProgress<ScanProgressReport> progress = new Progress<ScanProgressReport>(report =>
            {
                UpdateProgress(report.CurrentPath, report.FilesFoundCount);
            });

            SyncResult result = new SyncResult();

            try
            {
                await Task.Run(() =>
                {
                    result = RuleEngine.SyncAll(allProfileFolders, progress);
                });

                HideProgress();

                MessageBox.Show(
                    $"Tüm profiller başarıyla senkronize edildi!\n\n" +
                    $"• Taranan Profil Sayısı: {result.ScannedCategories}\n" +
                    $"• Taranan Klasör Sayısı: {result.ScannedFolders}\n" +
                    $"• Güncellenen Kural: {result.Updated}\n" +
                    $"• Silinen Eski Kural (Diskte Olmayan): {result.Removed}\n" +
                    $"• Yeni Eklenen Kural: {result.NewCount}",
                    "Tüm Profiller Senkronize Edildi", MessageBoxButton.OK, MessageBoxImage.Information);

                // Seçili profil varsa onun TreeView'unu güncelle
                if (lstCategories.SelectedItem is CategoryModel selectedCat)
                {
                    _allActiveRules = FirewallManager.GetActiveRules();
                    RefreshItems(selectedCat);
                }
            }
            catch (Exception ex)
            {
                HideProgress();
                MessageBox.Show($"Senkronizasyon sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApplyCategoryRulesToFirewallAsync(CategoryModel category, bool isSync = false)
        {
            string statusText = isSync ? "🔄 Klasörler Senkronize Ediliyor..." : (category.IsEnabled ? "🟢 Profil Etkinleştiriliyor..." : "🔴 Profil Pasifleştiriliyor...");
            ShowProgress(statusText);

            IProgress<ScanProgressReport> progress = new Progress<ScanProgressReport>(report =>
            {
                UpdateProgress(report.CurrentPath, report.FilesFoundCount);
            });

            int count = 0;

            try
            {
                await Task.Run(() =>
                {
                    count = RuleEngine.ApplyCategoryRules(category, progress);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kurallar işlenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
                _allActiveRules = FirewallManager.GetActiveRules();
                RefreshItems(category);
            }
        }

        private void CategoryContextMenu_Opening(object sender, RoutedEventArgs e)
        {
            if (miCategoryToggle != null) miCategoryToggle.Header = LanguageManager.Get("CtxCategoryToggle");
            if (miCategoryRename != null) miCategoryRename.Header = LanguageManager.Get("CtxCategoryRename");
            if (miCategoryDelete != null) miCategoryDelete.Header = LanguageManager.Get("CtxCategoryDelete");

            if (miCategoryBothBlock != null) miCategoryBothBlock.Header = LanguageManager.Get("MenuActionBlockAll");
            if (miCategoryBothAllow != null) miCategoryBothAllow.Header = LanguageManager.Get("MenuActionAllowAll");

            if (menuCategoryInbound != null) menuCategoryInbound.Header = LanguageManager.Get("MenuInbound");
            if (menuCategoryOutbound != null) menuCategoryOutbound.Header = LanguageManager.Get("MenuOutbound");
            if (miCategoryInboundBlock != null) miCategoryInboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miCategoryInboundAllow != null) miCategoryInboundAllow.Header = LanguageManager.Get("MenuActionAllow");
            if (miCategoryOutboundBlock != null) miCategoryOutboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miCategoryOutboundAllow != null) miCategoryOutboundAllow.Header = LanguageManager.Get("MenuActionAllow");
        }

        private async Task SetCategoriesRuleStatusAsync(IEnumerable<CategoryModel> categories, bool? blockInbound, bool? blockOutbound)
        {
            var cats = categories.ToList();
            if (cats.Count == 0) return;

            string statusMsg;
            if (blockInbound.HasValue && blockOutbound.HasValue)
            {
                statusMsg = (blockInbound.Value && blockOutbound.Value)
                    ? "⛔ Seçili Profiller İçin Gelen & Giden Bağlantılar Engelleniyor..."
                    : "🟢 Seçili Profiller İçin Gelen & Giden Bağlantılara İzin Veriliyor...";
            }
            else if (blockInbound.HasValue)
            {
                statusMsg = blockInbound.Value
                    ? "⛔ Seçili Profiller İçin Gelen Bağlantı Engelleniyor..."
                    : "🟢 Seçili Profiller İçin Gelen Bağlantıya İzin Veriliyor...";
            }
            else
            {
                statusMsg = blockOutbound.Value
                    ? "⛔ Seçili Profiller İçin Giden Bağlantı Engelleniyor..."
                    : "🟢 Seçili Profiller İçin Giden Bağlantıya İzin Veriliyor...";
            }

            ShowProgress(statusMsg);

            try
            {
                foreach (var category in cats)
                {
                    if (category.Items.Count == 0) continue;

                    // Tek kaynak: profil varsayılanı güncellenir ve tüm öğeler buna uydurulur.
                    if (blockInbound.HasValue) category.BlockInbound = blockInbound.Value;
                    if (blockOutbound.HasValue) category.BlockOutbound = blockOutbound.Value;

                    foreach (var item in category.Items)
                    {
                        if (blockInbound.HasValue) item.BlockInbound = blockInbound.Value;
                        if (blockOutbound.HasValue) item.BlockOutbound = blockOutbound.Value;
                    }

                    if (category.IsEnabled)
                    {
                        await ApplyCategoryRulesToFirewallAsync(category, isSync: false);
                    }
                    else
                    {
                        await RemoveCategoryRulesFromFirewallAsync(category);
                    }
                }

                SaveDataToIni();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Profil kuralları güncellenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
                _allActiveRules = FirewallManager.GetActiveRules();
                if (lstCategories.SelectedItems.Count == 1 && lstCategories.SelectedItem is CategoryModel active)
                {
                    RefreshItems(active);
                }
                UpdateHeaderRuleCounters();
            }
        }

        private async void CtxCategoryBothBlock_Click(object sender, RoutedEventArgs e)
        {
            await SetCategoriesRuleStatusAsync(GetSelectedCategories(), blockInbound: true, blockOutbound: true);
        }

        private async void CtxCategoryBothAllow_Click(object sender, RoutedEventArgs e)
        {
            await SetCategoriesRuleStatusAsync(GetSelectedCategories(), blockInbound: false, blockOutbound: false);
        }

        private async void CtxCategoryInboundBlock_Click(object sender, RoutedEventArgs e)
        {
            await SetCategoriesRuleStatusAsync(GetSelectedCategories(), blockInbound: true, blockOutbound: null);
        }

        private async void CtxCategoryInboundAllow_Click(object sender, RoutedEventArgs e)
        {
            await SetCategoriesRuleStatusAsync(GetSelectedCategories(), blockInbound: false, blockOutbound: null);
        }

        private async void CtxCategoryOutboundBlock_Click(object sender, RoutedEventArgs e)
        {
            await SetCategoriesRuleStatusAsync(GetSelectedCategories(), blockInbound: null, blockOutbound: true);
        }

        private async void CtxCategoryOutboundAllow_Click(object sender, RoutedEventArgs e)
        {
            await SetCategoriesRuleStatusAsync(GetSelectedCategories(), blockInbound: null, blockOutbound: false);
        }

        private async void CtxCategoryToggle_Click(object sender, RoutedEventArgs e)
        {
            var categories = GetSelectedCategories();
            if (categories.Count == 0) return;

            ShowProgress("⚙️ Seçili Profiller Güncelleniyor...");
            try
            {
                foreach (var category in categories)
                {
                    category.IsEnabled = !category.IsEnabled;
                    if (category.IsEnabled)
                        await ApplyCategoryRulesToFirewallAsync(category, isSync: false);
                    else
                        await RemoveCategoryRulesFromFirewallAsync(category);
                }
                SaveDataToIni();
                lstCategories.Items.Refresh();
                UpdateHeaderRuleCounters();
            }
            finally
            {
                HideProgress();
                _allActiveRules = FirewallManager.GetActiveRules();
                if (lstCategories.SelectedItems.Count == 1 && lstCategories.SelectedItem is CategoryModel active)
                {
                    RefreshItems(active);
                }
            }
        }

        private void CtxCategoryRename_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItems.Count != 1)
            {
                MessageBox.Show("Yeniden adlandırma için yalnızca tek bir profil seçin.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (lstCategories.SelectedItem is CategoryModel category)
            {
                try
                {
                    string? input = InputDialog.Show(
                        this,
                        LanguageManager.Get("EnterNewProfileName"), 
                        LanguageManager.Get("RenameProfileTitle"), 
                        category.Name);

                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        string trimmedName = input.Trim();
                        if (trimmedName != category.Name)
                        {
                            if (_categories.Any(c => c != category && c.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
                            {
                                MessageBox.Show(LanguageManager.Get("ProfileExists"), "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            category.Name = trimmedName;
                            SaveDataToIni();
                            lstCategories.Items.Refresh();
                            // Re-trigger selection to update panel header and details
                            var temp = category;
                            lstCategories.SelectedItem = null;
                            lstCategories.SelectedItem = temp;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Profil yeniden adlandırılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CtxCategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            var cats = GetSelectedCategories();
            if (cats.Count == 0) return;

            string names = string.Join(", ", cats.Select(c => $"'{c.Name}'"));
            if (MessageBox.Show($"{names} profil(ler)ini ve içerisindeki tüm öğeleri silmek istediğinizden emin misiniz?",
                                "Profil Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var category in cats)
            {
                await RemoveCategoryRulesFromFirewallAsync(category);
                _categories.Remove(category);
            }

            SaveDataToIni();
            dgContent.ItemsSource = null;
            lstCategories.Items.Refresh();
        }

        private bool _themeChanging = false;
        private bool _dataLoaded = false;

        private void CmbThemeSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_themeChanging) return;
            if (cmbThemeSelector.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;

            string tag = item.Tag?.ToString() ?? "Dark";
            ApplyTheme(tag);

            // Temayı modelde tut; veri yüklendikten sonra tam kayıtta da korunur.
            // (InitializeComponent sırasında da tetiklenir ama o an _dataLoaded=false olduğundan dosya boşaltılmaz)
            _settings.Theme = tag;
            if (_dataLoaded)
            {
                SaveDataToIni();
            }
        }

        private void ApplyTheme(string themeName)
        {
            string themePath = themeName switch
            {
                "Light"   => "UI/Themes/LightTheme.xaml",
                "Discord" => "UI/Themes/DiscordTheme.xaml",
                "YouTube" => "UI/Themes/YouTubeTheme.xaml",
                _         => "UI/Themes/DarkTheme.xaml"
            };

            var dict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        private void LoadSavedTheme()
        {
            string saved = IniStorage.ReadValue("Settings", "Theme", "Dark");
            _settings.Theme = saved;

            _themeChanging = true;
            foreach (System.Windows.Controls.ComboBoxItem ci in cmbThemeSelector.Items)
            {
                if (ci.Tag?.ToString() == saved)
                {
                    cmbThemeSelector.SelectedItem = ci;
                    break;
                }
            }
            _themeChanging = false;

            ApplyTheme(saved);
        }

        private void BdVersion_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                string targetUrl = _hasUpdateAvailable ? _latestReleaseUrl : "https://github.com/HaYToKoRaZ/HaYTooL-Firewall";
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetUrl,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        /// <summary>
        /// Arka planda GitHub üzerinden yeni versiyon olup olmadığını rahatsız etmeden sessizce kontrol eder.
        /// </summary>
        private async void CheckForUpdatesAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        string currentVerText = "";
                        Dispatcher.Invoke(() =>
                        {
                            currentVerText = txtVersionBadge?.Text?.TrimStart('v') ?? "2.2.0";
                        });

                        using var client = new System.Net.Http.HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(5);
                        client.DefaultRequestHeaders.Add("User-Agent", "HaYTooL-Firewall-UpdateChecker");

                        // GitHub'daki VERSION dosyasını canlı kontrol et
                        string rawOnline = await client.GetStringAsync("https://raw.githubusercontent.com/HaYToKoRaZ/HaYTooL-Firewall/main/VERSION");
                        if (string.IsNullOrWhiteSpace(rawOnline)) return;

                        string onlineCleanStr = rawOnline.Trim().TrimStart('v');

                        if (Version.TryParse(currentVerText, out Version? currentVer) &&
                            Version.TryParse(onlineCleanStr, out Version? onlineVer))
                        {
                            if (onlineVer > currentVer)
                            {
                                _hasUpdateAvailable = true;
                                _onlineLatestVersion = "v" + onlineCleanStr;

                                Dispatcher.Invoke(() =>
                                {
                                    if (ellipseUpdateDot != null) ellipseUpdateDot.Visibility = Visibility.Visible;
                                    if (txtVersionBadge != null)
                                    {
                                        txtVersionBadge.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
                                    }
                                    if (bdVersion != null)
                                    {
                                        bdVersion.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FEF2F2"));
                                        bdVersion.ToolTip = string.Format(LanguageManager.Get("UpdateAvailableToolTip"), _onlineLatestVersion);
                                    }
                                });
                            }
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }
    }
}
