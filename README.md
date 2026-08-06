# 🛡️ HaYTooL Firewall v2.0.0

> **Developer:** HaYTo  
> **Contact:** Email: `korazhayto@gmail.com` | X: [https://x.com/HaYTo](https://x.com/HaYTo) | GitHub: [https://github.com/HaYToKoRaZ/HaYTooL-Firewall](https://github.com/HaYToKoRaZ/HaYTooL-Firewall)  
> **License:** MIT  

---

## 🇬🇧 English (EN)

### 📌 About The Project
**HaYTooL Firewall** is a modern, single-screen, lightweight Windows Firewall management dashboard built with .NET 10 & WPF. It allows you to organize firewall rules into profiles/categories, automatically blocking or allowing (whitelisting) thousands of `.exe` files in seconds with dynamic recursive folder scanning.

All profile configurations and settings are saved in a clean, human-readable **`HaYTooL_Firewall.ini`** file.

---

### ✨ Key Features
- **🛡️ Mandatory UAC Auto-Elevation:** Automatically requests and enforces Administrator privileges (`highestAvailable` + `runas` auto-relaunch) required for managing Windows Firewall rules.
- **🔄 Multi-Profile Dynamic Synchronization:** Scans all added folders across all profiles with one click. Automatically removes stale/orphaned firewall rules for deleted or renamed `.exe` files and applies updated rules.
- **🖼️ Real Shell & App Icon Extraction:** Displays genuine Windows shell icons and embedded application logos (`SHGetFileInfo` + `Icon.ExtractAssociatedIcon`) directly in the TreeView list for both folders and executables.
- **📁 Folder Executable Counter:** Displays real-time `.exe` file counts next to folder names in Panel 2 TreeView (e.g. `Games (12 EXE)`).
- **🛡️ FullSafe Mode & Whitelisting:** Enforces default-deny outbound network traffic across Windows while allowing selective whitelisting for trusted applications.
- **🌐 7 Languages (i18n):** Turkish, English, Spanish, German, Portuguese, Arabic, and Russian with instant live translations.
- **🎨 4 Premium Themes:** Modern Dark, Light, Discord, and YouTube design system themes.
- **💾 Local & GitHub Gist Cloud Backup:** Take instant local INI backups or sync your rules securely to your private GitHub Gist.
- **🔒 Single Instance Protection:** Prevents duplicate instances and brings the active window to the front.
- **🛡️ Privacy & Zero Telemetry:** 100% local, zero tracking, zero analytics. Personal Gist tokens are entered locally by the user and never hardcoded or shared.

---

### 🛠️ Build & Run
```bash
# Clone the repository:
git clone https://github.com/HaYToKoRaZ/HaYTooL-Firewall.git
cd HaYTooL-Firewall

# Build project:
dotnet build

# Publish standalone single-file EXE:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 🇹🇷 Türkçe (TR)

### 📌 Proje Hakkında
**HaYTooL Firewall**, Windows Güvenlik Duvarı kurallarını kategoriler (profiller) halinde düzenlemenize, binlerce `.exe` dosyasını saniyeler içinde özyinelemeli (recursive) klasör taraması ile engellemenize veya izin vermenize (whitelist) olanak sağlayan .NET 10 & WPF ile geliştirilmiş modern ve hafif bir masaüstü kontrol panelidir.

Tüm profil verileri ve ayarlar insanca okunabilir **`HaYTooL_Firewall.ini`** dosyasında saklanır.

---

### ✨ Öne Çıkan Özellikler
- **🛡️ Zorunlu UAC Yönetici Hakları:** Windows Güvenlik Duvarı kurallarını yönetmek için gerekli olan Yönetici Haklarını otomatik talep eder ve uygular (`highestAvailable` + `runas` otomatik yükseltme).
- **🔄 Tüm Profilleri Kapsayan Akıllı Senkronizasyon:** Tek tıkla tüm profillerdeki klasörleri tara. Adı değişen veya silinen `.exe` dosyalarının eski kurallarını otomatik kaldırır, güncel dosyaları işler.
- **🖼️ Gerçek Windows & Uygulama Simgeleri:** Hem klasörler hem de `.exe` dosyaları için Windows Shell ve uygulama amblem simgelerini (`SHGetFileInfo`) ağaç görünümünde (TreeView) canlı olarak görüntüler.
- **📁 Klasör EXE Sayacı:** Klasör isimlerinin yanında anlık kaç adet `.exe` içerdiğini gösterir (Örn: `Oyunlar (12 EXE)`).
- **🛡️ FullSafe Modu & İzin Verme (Whitelist):** Varsayılan giden internet trafiğini engelleyen FullSafe modu ve seçili uygulamalara internet izni verme seçeneği.
- **🌐 7 Dil Desteği (i18n):** Türkçe, İngilizce, İspanyolca, Almanca, Portekizce, Arapça ve Rusça dillerinde anında yeniden başlatmasız canlı çeviri.
- **🎨 4 Harika Tema:** Koyu (Dark), Açık (Light), Discord ve YouTube tema tasarımları.
- **💾 Yerel & GitHub Gist Bulut Yedekleme:** Tek tıkla yerel INI yedeği alma veya kişisel GitHub Gist hesabınıza kuralları bulutta yedekleme.
- **🔒 Tek Örnek Çalıştırma (Single Instance):** Uygulama zaten açıkken tekrar çalıştırıldığında ikinci kopya engellenir ve mevcut pencere öne getirilir.
- **🛡️ %100 Gizlilik & Sıfır Takip:** Hiçbir analitik, telemetri veya izleyici barındırmaz. Kullanıcının Gist token bilgisi yerel olarak girilir, koda gömülmez veya paylaşılmaz.

---

### 🛠️ Derleme & Çalıştırma
```bash
# Projeyi klonlayın:
git clone https://github.com/HaYToKoRaZ/HaYTooL-Firewall.git
cd HaYTooL-Firewall

# Projeyi derlemek için:
dotnet build

# Tek dosya bağımsız .exe çıktısı almak için:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 📜 License
This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details.
