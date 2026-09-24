using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GuvenlikDuvarim.Core.Utils
{
    /// <summary>
    /// HaYTooL Pulse merkezi canlı kullanıcı sayacı entegrasyonu.
    /// Tamamen anonimdir; hiçbir IP, kişisel veri veya sistem bilgisi toplamaz.
    /// </summary>
    public static class PulseClient
    {
        private const string PingUrl = "https://hayto-telemetry.korazhayto.workers.dev/api/ping";
        private const string AppId = "pc_firewall";
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private static int _started;

        public static void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1) return;

            Task.Run(async () =>
            {
                string sessionId = "pc_" + Guid.NewGuid().ToString("N").Substring(0, 10);
                bool isFirst = true;

                while (true)
                {
                    try
                    {
                        var payload = new
                        {
                            app = AppId,
                            session_id = sessionId,
                            is_new_session = isFirst
                        };

                        string json = JsonSerializer.Serialize(payload);
                        using var content = new StringContent(json, Encoding.UTF8, "application/json");
                        await HttpClient.PostAsync(PingUrl, content).ConfigureAwait(false);

                        isFirst = false;
                    }
                    catch
                    {
                        // Ağ hatası veya çevrimdışı durumda sessizce devam et
                    }

                    await Task.Delay(TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                }
            });
        }
    }
}
