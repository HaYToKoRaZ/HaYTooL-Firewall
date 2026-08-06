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

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
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

            // 2. TEK ÖRNEK (SINGLE INSTANCE) KONTROLÜ
            const string mutexName = "HaYTooL_Firewall_SingleInstance_Mutex";
            _mutex = new Mutex(true, mutexName, out bool isNewInstance);

            if (!isNewInstance)
            {
                // Zaten çalışan bir HaYTooL Firewall örneği var, onu öne getir ve yeni açılan örneği sonlandır
                BringExistingInstanceToForeground();
                Shutdown();
                return;
            }

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

                var processInfo = new ProcessStartInfo
                {
                    FileName = exePath,
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

        private static void BringExistingInstanceToForeground()
        {
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (var process in processes)
            {
                if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(process.MainWindowHandle);
                    break;
                }
            }
        }
    }
}
