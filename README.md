<p align="center">
  <img src="Resources/firewall.png" alt="HaYTooL Firewall Logo" width="128" />
</p>

<h1 align="center">🛡️ HaYTooL Firewall v2.3.0</h1>

<p align="center">
  <a href="#-english-en">🇬🇧 English Version</a> | <a href="#-türkçe-tr">🇹🇷 Türkçe Versiyon</a>
</p>

<p align="center">
  <a href="https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/latest"><img src="https://img.shields.io/badge/platform-Windows%2010%2F11%20(x64)-blue" alt="Platform" /></a>
  <a href="https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/latest"><img src="https://img.shields.io/badge/version-v2.3.0-brightgreen" alt="Version" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-orange" alt="License" /></a>
  <a href="https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/latest"><img src="https://img.shields.io/github/downloads/HaYToKoRaZ/HaYTooL-Firewall/total?color=success&label=Downloads" alt="GitHub Downloads" /></a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-purple" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Framework-WPF-blue" alt="WPF" />
  <img src="https://img.shields.io/badge/Language-C%23-green" alt="C#" />
  <img src="https://img.shields.io/badge/API-NetFwTypeLib-red" alt="Windows Firewall COM" />
</p>

<p align="center">
  <img src="Resources/screenshot.png" alt="HaYTooL Firewall Screenshot" width="820" />
</p>

---

## 🇬🇧 English (EN)

### 🎯 Purpose of the Application
**HaYTooL Firewall** is a modern, lightweight, and intuitive Windows Firewall control panel designed to **block or allow (whitelist) network access for games, software, and entire folders with a single click**, without getting lost in complicated Windows Defender Firewall snap-in menus.

While traditional tools require hours to select executables and configure rules manually, **HaYTooL Firewall** uses an **intelligent recursive folder scanner** and **profile management architecture** to categorize thousands of applications and apply rules in seconds.

---

### 🚀 Capabilities & Features

#### 1. 📂 Profile & Category Management
- Create custom profiles (e.g., Games, Work Apps, System Tools).
- Enable/disable profiles with one click and apply bulk rules per profile.
- Drag-and-Drop files and folders directly into profiles.

#### 2. ⚡ Recursive Folder Scanning & Auto-Sync
- Adding a folder automatically scans **all `.exe` files in all subdirectories** and lists them in a hierarchical TreeView.
- Displays real-time executable counts next to folder names (e.g., `Steam (24 EXE)`).
- **Auto-Sync:** Cleans up stale rules for deleted/moved files and creates updated rules for newly added executables automatically.

#### 3. ⛔ Allow (Whitelist) & Block Modes
- Independent control over **Inbound** and **Outbound** network traffic.
- Configure selective **Whitelist (Allow)** or **Block** rules per profile.

#### 4. 🛡️ FullSafe Mode (Default-Deny Protection)
- Enforces system-wide default-deny outbound internet traffic.
- Only applications explicitly whitelisted in your active profiles gain internet access.

#### 5. 🖼️ Real Shell Icon Extraction
- Integrates with Windows Shell API (`SHGetFileInfo`) to display genuine, high-quality application icons for every executable and folder.

#### 6. 💾 Local & GitHub Gist Cloud Backup
- **Local Backup Manager:** Stores configuration in a clean `HaYTooL_Firewall.ini` file with automatic `.7z` archives.
- **GitHub Gist Sync:** Backup and sync your rules across multiple PCs using your personal private GitHub Gist token.

#### 7. 🎨 4 Premium Themes
- **Dark Theme:** Modern Slate dark palette.
- **Light Theme:** Multi-layered clean light blue palette.
- **Discord Theme:** Official Discord design system colors.
- **YouTube Theme:** Official YouTube design system colors.

#### 8. 🌐 7 Languages Support (i18n)
- Instant live translations and ToolTips in **Turkish, English, Spanish, German, Portuguese, Arabic, and Russian**.

#### 9. 🔒 Security & Privacy
- Automatic UAC elevation (`runas`), single instance protection, 100% local, zero telemetry.

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

### 🎯 Uygulamanın Amacı
Windows'un karmaşık Güvenlik Duvarı (Gelişmiş Güvenlikli Windows Defender Güvenlik Duvarı) menülerinde kaybolmadan, **oyunların, programların ve klasörlerin internet erişimini tek tıkla engellemek veya izin vermek (whitelist)** amacıyla geliştirilmiş modern, hızlı ve kullanıcı dostu bir kontrol panelidir.

Geleneksel arayüzlerde yüzlerce `.exe` dosyasını tek tek seçip kural yazmak saatler sürerken, **HaYTooL Firewall** geliştirdiği **özyinelemeli (recursive) akıllı klasör taraması** ve **profil mimarisi** sayesinde binlerce uygulamayı saniyeler içinde kategorize eder ve güvenlik duvarı kurallarını otomatik uygular.

---

### 🚀 Uygulamanın Neleri Yapabildiği (Tüm Özellikler)

#### 1. 📂 Profil ve Kategori Yönetimi
- Oyunlar, İş Uygulamaları, Sistem Araçları gibi özel profiller (kategoriler) oluşturabilirsiniz.
- Seçilen bir profili tek tıkla **Aktif/Pasif** duruma getirebilir, profil bazında toplu kurallar uygulayabilirsiniz.
- Sürükle-Bırak (Drag & Drop) desteği ile dosyaları doğrudan profillere aktarabilirsiniz.

#### 2. ⚡ Özyinelemeli (Recursive) Akıllı Klasör Taraması
- Bir oyun veya program klasörünü eklediğinizde, alt klasörlerdeki **tüm `.exe` dosyaları otomatik taranır** ve hiyerarşik Ağaç Görünümünde (TreeView) listelenir.
- Klasör isminin yanında içerdiği gerçek zamanlı `.exe` sayısı gösterilir (Örn: `Steam (24 EXE)`).
- **Akıllı Senkronizasyon (Sync):** Klasör içeriği değiştiğinde, adı silinen veya taşınan dosyaların eski kuralları otomatik temizlenir; yeni eklenen uygulamalar için kurallar anında oluşturulur.

#### 3. ⛔ İzin Ver (Allow / Whitelist) & Engelle (Block) Modları
- **Gelen (Inbound)** ve **Giden (Outbound)** ağ bağlantıları bağımsız olarak kontrol edilebilir.
- Seçili profil için sadece engelleme değil, **seçmeli izin verme (whitelist)** kuralları da tanımlanabilir.

#### 4. 🛡️ FullSafe Modu (Tam Güvenlik / Beyaz Liste Yönetimi)
- **FullSafe** modu aktif edildiğinde, bilgisayarın tüm giden internet erişimi varsayılan olarak engellenir (Default Deny).
- Sadece beyaz listeye eklediğiniz ve izin verdiğiniz güvenli uygulamalar internete erişebilir.

#### 5. 🖼️ Canlı Simge ve Amblem Özütleme (Shell Extract)
- Windows Shell API (`SHGetFileInfo`) ve `Icon.ExtractAssociatedIcon` entegrasyonu sayesinde uygulamaların ve klasörlerin **gerçek orijinal ikonları** arayüzde görüntülenir.

#### 6. 💾 Yerel & GitHub Gist Bulut Yedekleme
- **Yerel Yedek Yönetimi:** Tüm profil ve kural yapılandırması insanca okunabilir `HaYTooL_Firewall.ini` dosyasında saklanır. Otomatik ve manuel `.7z` arşiv yedekleri alınabilir.
- **GitHub Gist Bulut Senkronizasyonu:** Ayarlarınızı kişisel gizli GitHub Gist hesabınıza aktarabilir ve farklı bilgisayarlar arasında tek tıkla senkronize edebilirsiniz.

#### 7. 🎨 4 Farklı Premium Tema
- **Koyu Tema (Dark):** Slate koyu gri/lacivert göz yormayan arayüz.
- **Açık Tema (Light):** Derinlik katmanlı, temiz beyaz/açık mavi arayüz.
- **Discord Teması:** Resmi Discord tasarım renk paleti.
- **YouTube Teması:** Resmi YouTube tasarım renk paleti.
- Tüm tema geçişleri **canlı (dynamic)** olarak anında uygulanır.

#### 8. 🌐 7 Dil Desteği (i18n)
- **Türkçe, İngilizce, İspanyolca, Almanca, Portekizce, Arapça ve Rusça** dillerinde anlık canlı arayüz ve ipucu (ToolTip) çevirileri.

#### 9. 🔒 Güvenlik, Performans ve Gizlilik
- **UAC Otomatik Yükseltme:** Windows Güvenlik Duvarı COM API'sine erişim için gerekli Yönetici Haklarını otomatik talep eder.
- **Tek Örnek Garantisi (Single Instance):** Çift çalıştırmayı engeller, çalışan uygulamayı öne getirir.
- **%100 Gizlilik:** Sıfır izleyici (telemetri), sıfır analitik. Tüm veriler sadece yerel bilgisayarınızda tutulur.

---

### 📄 License / Lisans
This project is licensed under the [MIT License](LICENSE).
