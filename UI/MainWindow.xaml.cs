using GuvenlikDuvarim.Core.Firewall;
using GuvenlikDuvarim.Core.I18n;
using GuvenlikDuvarim.Core.Scanner;
using GuvenlikDuvarim.Core.Storage;
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
            Loaded += (s, e) =>
            {
                PerformStartupAutoBackups();
                CheckForUpdatesAsync();
            };
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
        }

        private void UpdateHeaderRuleCounters()
        {
            List<FirewallRuleInfo> rawRules = new();
            try
            {
                rawRules = FirewallManager.GetRawActiveRules();
            }
            catch { }

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
                    foreach (var item in category.Items)
                    {
                        if (item.IsFolder)
                        {
                            var exeFiles = FileScanner.FindExeFiles(item.Path, progress);
                            foreach (var exe in exeFiles)
                            {
                                string appName = System.IO.Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
                                FirewallManager.RemoveAppRules(appName);
                                removedCount++;
                                progress.Report(new ScanProgressReport { CurrentPath = exe, FilesFoundCount = removedCount });
                            }
                        }
                        else
                        {
                            string appName = System.IO.Path.GetFileNameWithoutExtension(item.Path) + "_" + item.Path.GetHashCode();
                            FirewallManager.RemoveAppRules(appName);
                            removedCount++;
                            progress.Report(new ScanProgressReport { CurrentPath = item.Path, FilesFoundCount = removedCount });
                        }
                    }
                });
            }
            catch { }
            finally
            {
                HideProgress();
            }
        }

        private (string status, string color, string bg) GetInboundRuleStatus(string path, CategoryModel category)
        {
            if (string.IsNullOrWhiteSpace(path)) return ("-", "#6B7280", "Transparent");

            string lowerPath = path.ToLowerInvariant();
            var matchRule = _allActiveRules.FirstOrDefault(r => (r.ApplicationPath ?? "").ToLowerInvariant() == lowerPath);

            if (matchRule != null && (matchRule.RawDirection == 1 || matchRule.RawDirection == 3))
            {
                if (matchRule.RawAction == 0 && matchRule.IsEnabled)
                    return (LanguageManager.Get("StatusBlocked"), "#DC2626", "#FEE2E2");
                else if (matchRule.RawAction == 1 && matchRule.IsEnabled)
                    return (LanguageManager.Get("StatusAllowed"), "#16A34A", "#DCFCE7");
            }

            var matchItem = category.Items.FirstOrDefault(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                         ?? category.Items.FirstOrDefault(i => i.IsFolder && path.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase));

            bool isBlocked = matchItem != null ? matchItem.BlockInbound : category.BlockInbound;

            if (isBlocked)
            {
                if (category.IsAllowRule)
                    return (LanguageManager.Get("StatusAllowed"), "#16A34A", "#DCFCE7");
                else
                    return (LanguageManager.Get("StatusBlocked"), "#DC2626", "#FEE2E2");
            }
            return (LanguageManager.Get("StatusAllowed"), "#16A34A", "#DCFCE7");
        }

        private (string status, string color, string bg) GetOutboundRuleStatus(string path, CategoryModel category)
        {
            if (string.IsNullOrWhiteSpace(path)) return ("-", "#6B7280", "Transparent");

            string lowerPath = path.ToLowerInvariant();
            var matchRule = _allActiveRules.FirstOrDefault(r => (r.ApplicationPath ?? "").ToLowerInvariant() == lowerPath);

            if (matchRule != null && (matchRule.RawDirection == 2 || matchRule.RawDirection == 3))
            {
                if (matchRule.RawAction == 0 && matchRule.IsEnabled)
                    return (LanguageManager.Get("StatusBlocked"), "#DC2626", "#FEE2E2");
                else if (matchRule.RawAction == 1 && matchRule.IsEnabled)
                    return (LanguageManager.Get("StatusAllowed"), "#16A34A", "#DCFCE7");
            }

            var matchItem = category.Items.FirstOrDefault(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                         ?? category.Items.FirstOrDefault(i => i.IsFolder && path.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase));

            bool isBlocked = matchItem != null ? matchItem.BlockOutbound : category.BlockOutbound;

            if (isBlocked)
            {
                if (category.IsAllowRule)
                    return (LanguageManager.Get("StatusAllowed"), "#16A34A", "#DCFCE7");
                else
                    return (LanguageManager.Get("StatusBlocked"), "#DC2626", "#FEE2E2");
            }
            return (LanguageManager.Get("StatusAllowed"), "#16A34A", "#DCFCE7");
        }

        private int _currentRefreshId = 0;

        /// <summary>
        /// Seçili profile ait öğeleri Panel 2 DataGrid tablosuna anında yükler.
        /// Kök öğeler 0ms yükleme süresiyle hemen gösterilir; klasörler arka planda taranıp dinamik genişletilir.
        /// Seri profil geçişlerinde eski taramalar iptal edilir.
        /// </summary>
        private async void RefreshItems(CategoryModel category)
        {
            if (category == null)
            {
                dgContent.ItemsSource = null;
                return;
            }

            int refreshId = ++_currentRefreshId;
            var displayList = new ObservableCollection<ContentTreeNode>();
            dgContent.ItemsSource = displayList;

            if (category.Items.Count == 0)
            {
                txtEmptyProfileTitle.Text = LanguageManager.Get("EmptyProfileTitle");
                txtEmptyProfileSub.Text = LanguageManager.Get("EmptyProfileSub");
                borderEmptyProfile.Visibility = Visibility.Visible;
                dgContent.ItemsSource = null;
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

                var (inStatus, inColor, inBg) = GetInboundRuleStatus(item.Path, category);
                var (outStatus, outColor, outBg) = GetOutboundRuleStatus(item.Path, category);

                var node = new ContentTreeNode
                {
                    DisplayName = item.IsFolder ? $"📁 {name} (⏳ taranıyor...)" : $"📄 {name}",
                    FullPath = item.Path,
                    IsFolder = item.IsFolder,
                    IndentMargin = new Thickness(0),
                    InboundStatus = inStatus,
                    InboundStatusColor = inColor,
                    InboundBadgeBackground = inBg,
                    OutboundStatus = outStatus,
                    OutboundStatusColor = outColor,
                    OutboundBadgeBackground = outBg
                };

                displayList.Add(node);
                if (item.IsFolder)
                {
                    folderNodes.Add((item, node));
                }
            }

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

                int insertIdx = displayList.IndexOf(node);
                if (insertIdx < 0) continue;

                if (exeFiles.Count == 0)
                {
                    displayList.Insert(insertIdx + 1, new ContentTreeNode
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
                    int offset = 1;
                    foreach (var exe in exeFiles)
                    {
                        if (refreshId != _currentRefreshId) return;

                        var (exeInStatus, exeInColor, exeInBg) = GetInboundRuleStatus(exe, category);
                        var (exeOutStatus, exeOutColor, exeOutBg) = GetOutboundRuleStatus(exe, category);

                        displayList.Insert(insertIdx + offset, new ContentTreeNode
                        {
                            DisplayName = $"↳ {Path.GetFileName(exe)}",
                            FullPath = exe,
                            IsFolder = false,
                            IndentMargin = new Thickness(20, 0, 0, 0),
                            InboundStatus = exeInStatus,
                            InboundStatusColor = exeInColor,
                            InboundBadgeBackground = exeInBg,
                            OutboundStatus = exeOutStatus,
                            OutboundStatusColor = exeOutColor,
                            OutboundBadgeBackground = exeOutBg
                        });
                        offset++;
                    }
                }
            }
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
            if (lstCategories.SelectedItem is CategoryModel category)
            {
                RefreshItems(category);
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
                            string appName = System.IO.Path.GetFileNameWithoutExtension(file) + "_" + file.GetHashCode();
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
                                    string appName = System.IO.Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
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
                            string appName = System.IO.Path.GetFileNameWithoutExtension(path) + "_" + path.GetHashCode();
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
                                    string appName = System.IO.Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
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

            if (dgContent.SelectedItem is not ContentTreeNode selectedNode)
            {
                MessageBox.Show("Listeden çıkarmak istediğiniz öğeyi seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var itemToRemove = GetSelectedItemModel();
            if (itemToRemove == null)
            {
                MessageBox.Show("Bu EXE bir klasörün içinden taranarak bulunmuştur.\nSilmek için üst klasör düğümünü seçin.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (itemToRemove is AppItemModel itrm && lstCategories.SelectedItem is CategoryModel cat)
            {
                if (itrm.IsFolder)
                {
                    ShowProgress("🗑️ Kurallar Temizleniyor...", itrm.Path);
                    IProgress<ScanProgressReport> progress = new Progress<ScanProgressReport>(report =>
                    {
                        UpdateProgress(report.CurrentPath, report.FilesFoundCount);
                    });

                    int removedCount = 0;
                    try
                    {
                        await Task.Run(() =>
                        {
                            var exeFiles = FileScanner.FindExeFiles(itrm.Path, progress);
                            foreach (var exe in exeFiles)
                            {
                                string appName = Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
                                FirewallManager.RemoveAppRules(appName);
                                removedCount++;
                                progress.Report(new ScanProgressReport { CurrentPath = exe, FilesFoundCount = removedCount });
                            }
                        });
                    }
                    catch { }
                    finally { HideProgress(); }
                }
                else
                {
                    try
                    {
                        string appName = Path.GetFileNameWithoutExtension(itrm.Path) + "_" + itrm.Path.GetHashCode();
                        FirewallManager.RemoveAppRules(appName);
                    }
                    catch { }
                }

                cat.Items.Remove(itrm);
                SaveDataToIni();
                RefreshItems(cat);
            }
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
                row.IsSelected = true;
                row.Focus();
            }
        }

        private void ContextMenu_Opening(object sender, RoutedEventArgs e)
        {
            if (miItemOpenLocation != null) miItemOpenLocation.Header = LanguageManager.Get("CtxOpenLocation");
            if (miItemRemove != null) miItemRemove.Header = LanguageManager.Get("CtxItemRemove");
            if (menuInbound != null) menuInbound.Header = LanguageManager.Get("MenuInbound");
            if (menuOutbound != null) menuOutbound.Header = LanguageManager.Get("MenuOutbound");
            if (miInboundBlock != null) miInboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miInboundAllow != null) miInboundAllow.Header = LanguageManager.Get("MenuActionAllow");
            if (miOutboundBlock != null) miOutboundBlock.Header = LanguageManager.Get("MenuActionBlock");
            if (miOutboundAllow != null) miOutboundAllow.Header = LanguageManager.Get("MenuActionAllow");
        }

        private async Task SetItemRuleStatusAsync(ContentTreeNode node, bool? blockInbound, bool? blockOutbound)
        {
            var category = GetSelectedOrActiveCategory();
            if (category == null || string.IsNullOrEmpty(node.FullPath)) return;

            if (node.IsFolder)
            {
                // 1. Klasör için AppItemModel bul veya ekle
                var folderItem = category.Items.FirstOrDefault(i => i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
                if (folderItem == null)
                {
                    folderItem = new AppItemModel { Path = node.FullPath, IsFolder = true };
                    category.Items.Add(folderItem);
                }

                if (blockInbound.HasValue) folderItem.BlockInbound = blockInbound.Value;
                if (blockOutbound.HasValue) folderItem.BlockOutbound = blockOutbound.Value;

                // 2. Klasör altındaki listede kayıtlı olan tüm alt EXE öğelerini de klasörün yeni ayarıyla güncelle
                var childItems = category.Items.Where(i => !i.IsFolder && i.Path.StartsWith(node.FullPath, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var child in childItems)
                {
                    if (blockInbound.HasValue) child.BlockInbound = blockInbound.Value;
                    if (blockOutbound.HasValue) child.BlockOutbound = blockOutbound.Value;
                }
            }
            else
            {
                // Tekil EXE seçildiyse: EXE için özel AppItemModel oluştur veya güncelle
                var exeItem = category.Items.FirstOrDefault(i => i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
                if (exeItem == null)
                {
                    var parentFolder = category.Items.FirstOrDefault(i => i.IsFolder && node.FullPath.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase));
                    bool initialIn = parentFolder?.BlockInbound ?? true;
                    bool initialOut = parentFolder?.BlockOutbound ?? true;

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
            }

            SaveDataToIni();

            string statusMsg = blockInbound.HasValue
                ? (blockInbound.Value ? "⛔ Gelen Bağlantı Engelleniyor..." : "🟢 Gelen Bağlantıya İzin Veriliyor...")
                : (blockOutbound.Value ? "⛔ Giden Bağlantı Engelleniyor..." : "🟢 Giden Bağlantıya İzin Veriliyor...");

            ShowProgress(statusMsg);

            try
            {
                await Task.Run(() =>
                {
                    bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);

                    if (node.IsFolder)
                    {
                        var folderItem = category.Items.FirstOrDefault(i => i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
                        bool inVal = folderItem?.BlockInbound ?? true;
                        bool outVal = folderItem?.BlockOutbound ?? true;

                        var exeFiles = FileScanner.FindExeFiles(node.FullPath);
                        foreach (var exe in exeFiles)
                        {
                            if (!FirewallManager.IsProtectedSystemPath(exe))
                            {
                                var specificItem = category.Items.FirstOrDefault(i => i.Path.Equals(exe, StringComparison.OrdinalIgnoreCase));
                                bool exeIn = specificItem?.BlockInbound ?? inVal;
                                bool exeOut = specificItem?.BlockOutbound ?? outVal;

                                string name = System.IO.Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
                                if (category.IsEnabled)
                                {
                                    FirewallManager.ApplyRule(name, exe, blockInbound: exeIn, blockOutbound: exeOut, isEnabled: true, isAllow: isAllow);
                                }
                                else
                                {
                                    FirewallManager.RemoveAppRules(name);
                                }
                            }
                        }
                    }
                    else
                    {
                        var exeItem = category.Items.FirstOrDefault(i => i.Path.Equals(node.FullPath, StringComparison.OrdinalIgnoreCase));
                        var parentFolder = category.Items.FirstOrDefault(i => i.IsFolder && node.FullPath.StartsWith(i.Path, StringComparison.OrdinalIgnoreCase));

                        bool inVal = exeItem?.BlockInbound ?? parentFolder?.BlockInbound ?? true;
                        bool outVal = exeItem?.BlockOutbound ?? parentFolder?.BlockOutbound ?? true;

                        if (!FirewallManager.IsProtectedSystemPath(node.FullPath))
                        {
                            string name = System.IO.Path.GetFileNameWithoutExtension(node.FullPath) + "_" + node.FullPath.GetHashCode();
                            if (category.IsEnabled)
                            {
                                FirewallManager.ApplyRule(name, node.FullPath, blockInbound: inVal, blockOutbound: outVal, isEnabled: true, isAllow: isAllow);
                            }
                            else
                            {
                                FirewallManager.RemoveAppRules(name);
                            }
                        }
                    }
                });
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

        private async void CtxInboundBlock_Click(object sender, RoutedEventArgs e)
        {
            if (dgContent.SelectedItem is ContentTreeNode node)
                await SetItemRuleStatusAsync(node, blockInbound: true, blockOutbound: null);
        }

        private async void CtxInboundAllow_Click(object sender, RoutedEventArgs e)
        {
            if (dgContent.SelectedItem is ContentTreeNode node)
                await SetItemRuleStatusAsync(node, blockInbound: false, blockOutbound: null);
        }

        private async void CtxOutboundBlock_Click(object sender, RoutedEventArgs e)
        {
            if (dgContent.SelectedItem is ContentTreeNode node)
                await SetItemRuleStatusAsync(node, blockInbound: null, blockOutbound: true);
        }

        private async void CtxOutboundAllow_Click(object sender, RoutedEventArgs e)
        {
            if (dgContent.SelectedItem is ContentTreeNode node)
                await SetItemRuleStatusAsync(node, blockInbound: null, blockOutbound: false);
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

            int totalRemovedCount = 0;
            int totalUpdatedCount = 0;
            int totalNewCount = 0;
            int scannedFoldersCount = 0;
            var scannedCategories = new HashSet<CategoryModel>();

            try
            {
                await Task.Run(() =>
                {
                    var activeRules = FirewallManager.GetActiveRules();

                    foreach (var (category, folderItem) in allProfileFolders)
                    {
                        if (!Directory.Exists(folderItem.Path)) continue;

                        scannedFoldersCount++;
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

                        // Artık diskte bulunmayan (adı değişmiş veya silinmiş) eski EXE yollarını bul ve temizle
                        var orphanedPaths = existingPathsInFolder
                            .Where(path => !currentExeSet.Contains(path))
                            .ToList();

                        if (orphanedPaths.Count > 0)
                        {
                            totalRemovedCount += FirewallManager.RemoveRulesByApplicationPaths(orphanedPaths);
                        }

                        // 3) Güncel EXE'ler için kuralları yenile / uygula
                        int processedExes = 0;
                        foreach (var exe in currentExes)
                        {
                            if (FirewallManager.IsProtectedSystemPath(exe)) continue;

                            string lowerExe = exe.ToLowerInvariant();
                            bool isNew = !existingPathsInFolder.Contains(lowerExe);

                            string appName = Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
                            FirewallManager.ApplyRule(appName, exe,
                                category.BlockInbound, category.BlockOutbound,
                                category.IsEnabled, category.IsAllowRule);

                            int ruleCountPerExe = (category.BlockInbound ? 1 : 0) + (category.BlockOutbound ? 1 : 0);
                            if (isNew) totalNewCount += ruleCountPerExe;
                            else totalUpdatedCount += ruleCountPerExe;

                            processedExes++;
                            progress.Report(new ScanProgressReport { CurrentPath = exe, FilesFoundCount = processedExes });
                        }
                    }
                });

                HideProgress();

                MessageBox.Show(
                    $"Tüm profiller başarıyla senkronize edildi!\n\n" +
                    $"• Taranan Profil Sayısı: {scannedCategories.Count}\n" +
                    $"• Taranan Klasör Sayısı: {scannedFoldersCount}\n" +
                    $"• Güncellenen Kural: {totalUpdatedCount}\n" +
                    $"• Silinen Eski Kural (Diskte Olmayan): {totalRemovedCount}\n" +
                    $"• Yeni Eklenen Kural: {totalNewCount}",
                    "Tüm Profiller Senkronize Edildi", MessageBoxButton.OK, MessageBoxImage.Information);

                // Seçili profil varsa onun TreeView'unu güncelle
                if (lstCategories.SelectedItem is CategoryModel selectedCat)
                {
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
                    foreach (var item in category.Items)
                    {
                        if (item.IsFolder)
                        {
                            var exeFiles = FileScanner.FindExeFiles(item.Path, progress);
                            foreach (var exe in exeFiles)
                            {
                                if (!FirewallManager.IsProtectedSystemPath(exe))
                                {
                                    string appName = System.IO.Path.GetFileNameWithoutExtension(exe) + "_" + exe.GetHashCode();
                                    bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);
                                    if (category.IsEnabled)
                                    {
                                        FirewallManager.ApplyRule(appName, exe, blockInbound: item.BlockInbound, blockOutbound: item.BlockOutbound, isEnabled: true, isAllow: isAllow);
                                    }
                                    else
                                    {
                                        FirewallManager.RemoveAppRules(appName);
                                    }
                                    count++;
                                    progress.Report(new ScanProgressReport { CurrentPath = exe, FilesFoundCount = count });
                                }
                            }
                        }
                        else
                        {
                            if (!FirewallManager.IsProtectedSystemPath(item.Path))
                            {
                                bool isAllow = category.IsAllowRule || category.Name.Contains("FullSafe", StringComparison.OrdinalIgnoreCase);
                                string appName = System.IO.Path.GetFileNameWithoutExtension(item.Path) + "_" + item.Path.GetHashCode();
                                if (category.IsEnabled)
                                {
                                    FirewallManager.ApplyRule(appName, item.Path, blockInbound: item.BlockInbound, blockOutbound: item.BlockOutbound, isEnabled: true, isAllow: isAllow);
                                }
                                else
                                {
                                    FirewallManager.RemoveAppRules(appName);
                                }
                                count++;
                                progress.Report(new ScanProgressReport { CurrentPath = item.Path, FilesFoundCount = count });
                            }
                        }
                    }
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

            if (miCategoryBothBlock != null) miCategoryBothBlock.Header = LanguageManager.Get("MenuCategoryBothBlock");
            if (miCategoryBothAllow != null) miCategoryBothAllow.Header = LanguageManager.Get("MenuCategoryBothAllow");

            if (menuCategoryInbound != null) menuCategoryInbound.Header = LanguageManager.Get("MenuCategoryInbound");
            if (menuCategoryOutbound != null) menuCategoryOutbound.Header = LanguageManager.Get("MenuCategoryOutbound");
            if (miCategoryInboundBlock != null) miCategoryInboundBlock.Header = LanguageManager.Get("MenuActionBlockAll");
            if (miCategoryInboundAllow != null) miCategoryInboundAllow.Header = LanguageManager.Get("MenuActionAllowAll");
            if (miCategoryOutboundBlock != null) miCategoryOutboundBlock.Header = LanguageManager.Get("MenuActionBlockAll");
            if (miCategoryOutboundAllow != null) miCategoryOutboundAllow.Header = LanguageManager.Get("MenuActionAllowAll");
        }

        private async Task SetCategoryRuleStatusAsync(CategoryModel category, bool? blockInbound, bool? blockOutbound)
        {
            if (category == null || category.Items.Count == 0) return;

            string statusMsg;
            if (blockInbound.HasValue && blockOutbound.HasValue)
            {
                statusMsg = (blockInbound.Value && blockOutbound.Value)
                    ? "⛔ Tüm Profil İçin Gelen & Giden Bağlantılar Engelleniyor..."
                    : "🟢 Tüm Profil İçin Gelen & Giden Bağlantılara İzin Veriliyor...";
            }
            else if (blockInbound.HasValue)
            {
                statusMsg = blockInbound.Value
                    ? "⛔ Tüm Profil İçin Gelen Bağlantı Engelleniyor..."
                    : "🟢 Tüm Profil İçin Gelen Bağlantıya İzin Veriliyor...";
            }
            else
            {
                statusMsg = blockOutbound.Value
                    ? "⛔ Tüm Profil İçin Giden Bağlantı Engelleniyor..."
                    : "🟢 Tüm Profil İçin Giden Bağlantıya İzin Veriliyor...";
            }

            ShowProgress(statusMsg);

            try
            {
                foreach (var item in category.Items)
                {
                    if (blockInbound.HasValue) item.BlockInbound = blockInbound.Value;
                    if (blockOutbound.HasValue) item.BlockOutbound = blockOutbound.Value;
                }

                SaveDataToIni();

                if (category.IsEnabled)
                {
                    await ApplyCategoryRulesToFirewallAsync(category, isSync: false);
                }
                else
                {
                    await RemoveCategoryRulesFromFirewallAsync(category);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Profil kuralları güncellenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideProgress();
                _allActiveRules = FirewallManager.GetActiveRules();
                RefreshItems(category);
                UpdateHeaderRuleCounters();
            }
        }

        private async void CtxCategoryBothBlock_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
                await SetCategoryRuleStatusAsync(category, blockInbound: true, blockOutbound: true);
        }

        private async void CtxCategoryBothAllow_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
                await SetCategoryRuleStatusAsync(category, blockInbound: false, blockOutbound: false);
        }

        private async void CtxCategoryInboundBlock_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
                await SetCategoryRuleStatusAsync(category, blockInbound: true, blockOutbound: null);
        }

        private async void CtxCategoryInboundAllow_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
                await SetCategoryRuleStatusAsync(category, blockInbound: false, blockOutbound: null);
        }

        private async void CtxCategoryOutboundBlock_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
                await SetCategoryRuleStatusAsync(category, blockInbound: null, blockOutbound: true);
        }

        private async void CtxCategoryOutboundAllow_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
                await SetCategoryRuleStatusAsync(category, blockInbound: null, blockOutbound: false);
        }

        private async void CtxCategoryToggle_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
            {
                category.IsEnabled = !category.IsEnabled;
                SaveDataToIni();
                await ApplyCategoryRulesToFirewallAsync(category);
                lstCategories.Items.Refresh();
                UpdateHeaderRuleCounters();
            }
        }

        private void CtxCategoryRename_Click(object sender, RoutedEventArgs e)
        {
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

        private void CtxCategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is CategoryModel category)
            {
                DeleteCategory(category);
            }
        }

        private bool _themeChanging = false;

        private void CmbThemeSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_themeChanging) return;
            if (cmbThemeSelector.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;

            string tag = item.Tag?.ToString() ?? "Dark";
            ApplyTheme(tag);

            // Temayı kaydet
            IniStorage.SaveValue("Settings", "Theme", tag);
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
