using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows;

namespace GuvenlikDuvarim
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? _mutex;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    MessageBox.Show(
                        $"Beklenmeyen bir hata oluştu:\n\n{args.Exception.Message}",
                        "HaYTooL Firewall Hatası",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    args.Handled = true;
                }
                catch { }
            };

            // 1. ZORUNLU YÖNETİCİ KONTROLÜ VE OTOMATİK YÖNETİCİ OLARAK YENİDEN BAŞLATMA
            if (!IsRunningAsAdministrator())
            {
                if (!RelaunchAsAdministrator())
                {
                    MessageBox.Show(
                        "HaYTooL Firewall'un Windows Güvenlik Duvarı kurallarını yönetebilmesi için Yönetici Hakları zorunludur.\n\nLütfen uygulamayı 'Yönetici Olarak Çalıştır' seçeneğiyle açın.",
                        "Yönetici İzni Gerekli",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                Shutdown();
                return;
            }

            // 2. CLI KOMUT SATIRI ARGÜMANI KONTROLÜ
            if (e.Args.Length > 0)
            {
                if (GuvenlikDuvarim.Core.CLI.CliManager.ProcessArgs(e.Args))
                {
                    Shutdown();
                    return;
                }

                // 2.1 Doğrudan Görev Yöneticisi Başlatma Modu ("HaYTooL Firewall.exe" taskmgr)
                if (GuvenlikDuvarim.Core.CLI.CliManager.IsTaskMgrLaunch(e.Args))
                {
                    string effLang = GuvenlikDuvarim.Core.CLI.CliManager.GetEffectiveLanguage(e.Args);
                    GuvenlikDuvarim.Core.I18n.LanguageManager.CurrentLanguage = effLang;

                    // Kayıtlı temayı uygula
                    string savedTheme = GuvenlikDuvarim.Core.Storage.IniStorage.ReadValue("Settings", "Theme", "Dark");
                    string themePath = savedTheme switch
                    {
                        "Light" => "UI/Themes/LightTheme.xaml",
                        "Discord" => "UI/Themes/DiscordTheme.xaml",
                        "YouTube" => "UI/Themes/YouTubeTheme.xaml",
                        _ => "UI/Themes/DarkTheme.xaml"
                    };

                    try
                    {
                        var dict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };
                        Current.Resources.MergedDictionaries.Clear();
                        Current.Resources.MergedDictionaries.Add(dict);
                    }
                    catch { }

                    GuvenlikDuvarim.Core.Utils.PulseClient.Start();

                    // ProcessWindow penceresini bağımsız (standalone) ana pencere olarak başlat
                    var procWin = new GuvenlikDuvarim.UI.ProcessWindow();
                    procWin.Closed += (s, args) => Shutdown();
                    MainWindow = procWin;
                    procWin.Show();
                    return;
                }
            }

            // 3. TEK ÖRNEK (SINGLE INSTANCE) KONTROLÜ
            const string mutexName = "HaYTooL_Firewall_SingleInstance_Mutex";
            _mutex = new Mutex(true, mutexName, out bool isNewInstance);

            if (!isNewInstance)
            {
                // Zaten çalışan bir HaYTooL Firewall örneği var, onu öne getir ve yeni açılan örneği sonlandır
                BringExistingInstanceToForeground();
                Shutdown();
                return;
            }

            GuvenlikDuvarim.Core.Utils.PulseClient.Start();

            base.OnStartup(e);
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static bool RelaunchAsAdministrator()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? "";
                if (string.IsNullOrEmpty(exePath)) return false;

                string[] cmdArgs = Environment.GetCommandLineArgs();
                string formattedArgs = cmdArgs.Length > 1
                    ? string.Join(" ", System.Linq.Enumerable.Select(System.Linq.Enumerable.Skip(cmdArgs, 1), a => $"\"{a}\""))
                    : "";

                var processInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = formattedArgs,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(processInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Halihazırda açık olan uygulamanın penceresini bulur, simge durumundaysa geri yükler ve en öne getirir.
        /// </summary>
        private static void BringExistingInstanceToForeground()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in processes)
                {
                    if (process.Id == currentProcess.Id) continue;

                    IntPtr targetHwnd = IntPtr.Zero;

                    // 1. Standart MainWindowHandle doluysa ve geçerliyse
                    try
                    {
                        process.Refresh();
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            targetHwnd = process.MainWindowHandle;
                        }
                    }
                    catch { }

                    // 2. MainWindowHandle boşsa veya 0 ise, sürecin açık olan pencerelerini tara
                    if (targetHwnd == IntPtr.Zero)
                    {
                        EnumWindows((hWnd, lParam) =>
                        {
                            GetWindowThreadProcessId(hWnd, out uint pid);
                            if (pid == (uint)process.Id && IsWindowVisible(hWnd))
                            {
                                targetHwnd = hWnd;
                                return false; // İlk görünür pencereyi bulduk, aramayı bitir
                            }
                            return true;
                        }, IntPtr.Zero);
                    }

                    if (targetHwnd != IntPtr.Zero)
                    {
                        // Varsa aktif alt açılır pencereyi (dialog) al
                        IntPtr popup = GetLastActivePopup(targetHwnd);
                        if (popup != IntPtr.Zero) targetHwnd = popup;

                        if (IsIconic(targetHwnd))
                        {
                            ShowWindow(targetHwnd, SW_RESTORE);
                        }
                        else
                        {
                            ShowWindow(targetHwnd, SW_SHOW);
                        }

                        BringWindowToTop(targetHwnd);
                        SetForegroundWindow(targetHwnd);
                        break;
                    }
                }
            }
            catch { }
        }
    }
}
