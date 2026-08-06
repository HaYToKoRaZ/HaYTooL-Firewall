# 📜 Değişiklik Günlüğü / Changelog

Tüm önemli değişiklikler bu dosyada belgelenmektedir. / All notable changes to this project will be documented in this file.

## [v2.0.2] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Temiz Release Paketleme & Zengin README Rozetleri
- **🧹 Otomatik Klasör Hijyeni:** Derleme öncesi `0nogithub/release` klasörü tamamen temizlenir, artık dosyaların yüklenmesi kesin olarak önlendi.
- **📦 Sadece Portable ZIP Sunumu:** Son kullanıcılar için GitHub Release sayfasına yalnızca içinde `HaYTooL_Firewall.exe`, `README.md`, `LICENSE` ve `CHANGELOG.md` bulunan temiz Portable ZIP paketi yüklenir.
- **🎨 Zengin README Navigasyonu & Rozetleri:** `README.md` başlığına dil yönlendirme butonları (`🇬🇧 English Version | 🇹🇷 Türkçe Versiyon`) ve canlı Shields.io rozetleri eklendi.

---

### 🇬🇧 English (EN)
#### 🚀 Clean Release Packaging & Rich README Badges
- **🧹 Automated Release Sanitation:** Ensures `0nogithub/release` is fully wiped before compiling, preventing stale or duplicate files.
- **📦 ZIP-Only End-User Release:** Uploads exclusively the standalone Portable ZIP archive containing the executable and essential documentation.

---

## [v2.0.1] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Otomatik Release Paketi & Yükleyici Entegrasyonu
- **📦 Birleşik Release Zip Otomasyonu:** `0nogithub/upload_release_asset.ps1` scripti güncellendi; doğrudan `build_release_zip.ps1` scriptini tetikleyerek yeni bağımsız `.exe` ve `.zip` paketi üretip GitHub Release sayfasına yükleme veya sürükle-bırak için klasörü/tarayıcıyı otomatik açma desteği sağlandı.
- **🔗 Tıklanabilir Versiyon Etiketi:** Başlıktaki `v2.0.1` versiyon rozetine tıklama ve direkt GitHub deposunu açma özelliği eklendi.

---

### 🇬🇧 English (EN)
#### 🚀 Automated Release Builder & Asset Uploader
- **📦 Integrated Release Builder:** `upload_release_asset.ps1` now automatically triggers `build_release_zip.ps1` to produce fresh single-file executables and portable zip archives before publishing to GitHub.

---

## [v2.0.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 GitHub İlk Açık Kaynak Sürümü & Büyük Güncelleme
- **🌐 GitHub Yayın Hazırlığı:** İki dilli (EN + TR) zengin biçimlendirmeli `README.md`, `.gitignore` ve açık kaynak lisans yapılandırması tamamlandı.
- **🛡️ UAC Yönetici Hakları & Auto-Elevation:** `app.manifest` (`highestAvailable`) ve `App.xaml.cs` (`runas`) üzerinden otomatik UAC yetki yükseltme entegrasyonu sağlandı.
- **🔄 Tüm Profilleri Kapsayan Akıllı Senkronizasyon:** Tek tıkla tüm profillerdeki klasörleri tarar, silinen/adı değişen EXE'lerin eski kurallarını kaldırır ve güncel kuralları uygular.
- **📁 Klasör EXE Sayacı:** Panel 2 ağaç görünümünde (TreeView) klasör adlarının yanında anlık kaç adet EXE olduğu gösterilir.
- **🎨 4 Premium Tema:** Koyu, Açık, Discord ve YouTube temalarında %100 kusursuz metin ve arkaplan kontrast uyumu sağlandı.
- **📦 Portable Release Zip Scripti:** `0nogithub/build_release_zip.ps1` scripti ile tarih ve versiyon içeren tek tıkla taşınabilir `.zip` paketi üretimi.

---

### 🇬🇧 English (EN)
#### 🚀 Initial GitHub Open Source Release & Major Update
- **🌐 GitHub Release Preparation:** Added bilingual (EN + TR) rich `README.md`, `.gitignore`, and open source structure.
- **🛡️ UAC Administrator Privileges & Auto-Elevation:** Enforced mandatory UAC elevation via `app.manifest` (`highestAvailable`) and `App.xaml.cs` (`runas`).
- **🔄 Multi-Profile Dynamic Synchronization:** One-click full synchronization across all profiles, automatically cleaning stale/orphaned rules for deleted or renamed executables.
- **📁 Folder Executable Counter:** Displays real-time `.exe` counts next to folder names in Panel 2 TreeView.
- **🎨 4 Premium Themes:** Polished color contrast and typography for Dark, Light, Discord, and YouTube themes.
- **📦 Portable Release Zip Script:** Added `0nogithub/build_release_zip.ps1` script for automated single-file executable and versioned/dated portable `.zip` packaging.

---

## [v1.6.2] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Siyah Fırça & İkon Görünmeme Kök Neden Çözümü
- **🖼️ Siyah Renk Fırçası Kök Neden Çözümü (`CreateFallbackBitmap`):** Yedek ikon üreten emoji çizim sistemindeki siyah renk fırçası (`Brushes.Black`) Koyu Temada (`#0F172A`) simgeleri görünmez kılıyordu. Vektörel Goldenrod (Sarı Klasör) ve DodgerBlue (Mavi EXE) simge çizicisi ile tüm temalarda %100 kusursuz görünürlük sağlandı.
- **⚡ Şeffaf HICON Dönüşümü (`ConvertHIconToBitmapSource`):** `Imaging.CreateBitmapSourceFromHIcon` doğrudan dondurularak 16x16 netliğinde 2. panelde canlı simgeler elde edildi.

---

### 🇬🇧 English (EN)
#### 🚀 Bug Fix & Vector Icon Rendering
- **🖼️ Dark Theme Invisible Black Icon Fix:** Resolved black brush rendering flaw in dark mode by replacing emoji raster fallback with vibrant Goldenrod and DodgerBlue vector folder/EXE icons.

---

## [v1.6.1] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Kök Neden Çözümü & Kalıcı GDI Bitmap Dönüşümü
- **🖼️ `IconToBitmapSource` ile Kalıcı Simge İşleme:** Windows Icon nesnelerinin Dispose/using anında yok olmasından kaynaklı simge görünmeme sorunu `GetHbitmap()` ve `Imaging.CreateBitmapSourceFromHBitmap` dönüşümü ile kökten çözüldü. WPF RAM belleğine dondurulup (`bs.Freeze()`) kalıcı ve kayıpsız olarak aktarıldı.
- **⚡ SHGetFileInfo Win32 Entegrasyonu:** Klasörler ve .EXE dosyalarının simgeleri Windows Kabuk API'si ile %100 kusursuz ve anında çekilmektedir.

---

### 🇬🇧 English (EN)
#### 🚀 Bug Fix & Icon Extraction Stability
- **🖼️ Permanent WPF Bitmap Source Conversion (`IconToBitmapSource`):** Resolved icon visibility issue by cloning HICON handles into frozen WPF `BitmapSource` instances before handle destruction.
- **⚡ Native SHGetFileInfo Extraction:** Seamlessly extracts icons for executables and directories using Windows Shell API.

---

## [v1.6.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & Yerel Gerçek Windows Simgeleri
- **🖼️ 2. Panelde Gerçek Ekran/Exe Simgeleri (`IconExtractor`):** `Tür` sütunundaki metin ifadeleri ve renk rozetleri tamamen kaldırıldı. Yerine Windows Shell API ile doğrudan `.EXE` dosyasının kendi gerçek ikonu (ör: Chrome logosu, Steam logosu) ve Klasörler için orijinal Windows Klasör ikonu çekilerek 18x18 yüksek kaliteli simge sütunu (`Simge`) oluşturuldu.
- **✨ Temiz & Yerel Tasarım:** Metin karmaşası ve göz yoran renkler kaldırılıp yerel Windows Gezgini görünümüne geçildi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🖼️ Native Windows Executable & Folder Icon Extraction (`IconExtractor`):** Replaced text labels in Panel 2 with native Windows icons extracted directly from `.EXE` files and shell folder resources.
- **✨ Clean Native Windows UI:** Removed text badges for a sleek, compact, and authentic Windows Explorer look.

---

## [v1.5.3] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & Görsel İyileştirmeler
- **🎨 `AppItemModel.TypeText` Eklemesi & Kesin Klasör / EXE Renk Ayrımı (Turuncu 🟠 vs Mavi/Cyan 🔵):** `AppItemModel` modeline `TypeText` özelliği eklendi ve WPF `DataTrigger` veri bağlamı %100 kesinleştirildi.
  - **📁 Klasörler:** Canlı Sıcak Turuncu / Amber (`#F97316`) metin ve Turuncu pill rozeti (`FolderBgBrush`).
  - **📄 EXE Dosyaları:** Elektrik Mavi / Cyan (`#38BDF8`) metin ve Cyan pill rozeti (`ExeBgBrush`).
  - Turuncu (Klasör) ve Cyan Mavi (EXE) renk paleti zıt (tamamlayıcı) renkler olduğu için 2. paneldeki tüm öğeler anında ve göz alıcı biçimde ayırt edilir.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🎨 `AppItemModel.TypeText` Binding & Foolproof Orange 🟠 vs Cyan 🔵 Contrast:** Added `TypeText` property to `AppItemModel` to fix WPF DataGrid DataTrigger evaluation.
  - **📁 Folders:** Highlighted in Warm Orange/Amber (`#F97316`) with orange pill background.
  - **📄 EXEs:** Highlighted in Electric Cyan/Blue (`#38BDF8`) with cyan pill background.

---

## [v1.5.2] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & Görsel İyileştirmeler
- **🔥 Radikal Klasör ve EXE Renk Kontrastı (Turuncu vs Mor Pill Rozetleri):** 2. Profil İçeriği tablosunda klasörler ve EXE dosyaları gece ile gündüz gibi ayrıştırıldı:
  - **📁 Klasörler:** Canlı Turuncu Amber (`#F97316`) metin ve Turuncu yarı şeffaf rozet arka planı (`FolderBgBrush`).
  - **📄 EXE Dosyaları:** Elektrik Mor / Viyole (`#A855F7`) metin ve Mor yarı şeffaf rozet arka planı (`ExeBgBrush`).
- **🎨 4 Temanın Tamamında Benzersiz Vurgu:** Koyu (Dark), Açık (Light), Discord ve YouTube temalarında özel kontrast matrisleri uygulandı.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🔥 Radical Folder vs EXE Color Contrast (Orange vs Purple Pill Badges):** Implemented high-contrast badges and text colors in Panel 2:
  - **📁 Folders:** Styled in vivid Orange/Amber (`#F97316`) with subtle orange pill badges.
  - **📄 EXEs:** Styled in Electric Purple/Violet (`#A855F7`) with subtle purple pill badges.
- **🎨 Theme-Specific Contrast Tuning:** Custom tuned for Dark, Light, Discord, and YouTube themes.

---

## [v1.5.1] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & Görsel İyileştirmeler
- **🎨 2. Panelde Klasör ve EXE Renk Ayrımı:** `dgItems` tablosundaki öğelerin metin renkleri türüne göre ayrıştırıldı:
  - **📁 Klasörler:** Canlı Sarı / Amber tonunda (`FolderItemBrush`) vurgulanır.
  - **📄 EXE Dosyaları:** Parlak Mavi / Cyan tonunda (`ExeItemBrush`) vurgulanır.
- **🌈 Tüm Temalarda Kusursuz Okunabilirlik:** `FolderItemBrush` ve `ExeItemBrush` kaynakları Koyu (Dark), Açık (Light), Discord ve YouTube temalarına özel kontrast renklerle tanımlanarak tüm temalarda harika görünürlük sağlandı.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🎨 Distinct Folder vs EXE Colors in Panel 2:** DataGrid items now feature type-specific text colors:
  - **📁 Folders:** Highlighted in warm Gold/Amber (`FolderItemBrush`).
  - **📄 EXEs:** Highlighted in bright Sky Blue/Cyan (`ExeItemBrush`).
- **🌈 Theme-Aware High Contrast:** Added dynamic brush definitions tailored for Dark, Light, Discord, and YouTube themes for crystal-clear readability across all palettes.

---

## [v1.5.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & Mimari İyileştirmeler
- **⚡ Akıllı Sürükle - Bırak (Smart Drop Routing):** Konum sınırlaması ve Karmaşık koordinat hesapları tamamen kaldırıldı. Pencerenin neresine sürüklenirse sürüklensin:
  - **Klasörler:** Doğrudan **2. Panel (Profil İçeriği)** alanına eklenir.
  - **EXE Dosyaları:** Doğrudan **3. Panel (Aktif Engelleme Listesi)** alanına eklenir ve Windows Güvenlik Duvarı kuralları **anında** oluşturulur.
  - Sürükle-bırak uyarı pencerelerine gerek kalmamış, kodlar tamamen sadeleştirilmiştir.
- **✏️ Manuel Yedekleme Özel İsim / Not Alanı:** Yerel Yedekler penceresine `txtCustomBackupName` not kutusu eklendi. Açılışta tarih-saat etiketi (`yyyy-MM-dd_HH-mm-ss_`) otomatik doldurulur, kullanıcı yanına dilediği özel notu (ör: `MinecraftOyunu_`) yazarak benzersiz isimli yedek alabilir.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **⚡ Smart Automatic Drag & Drop Routing:** Removed drop position restrictions and coordinate calculations. Files dropped anywhere in the app are automatically sorted:
  - **Folders:** Automatically added to **Panel 2 (Profile Content)**.
  - **EXEs:** Automatically added to **Panel 3 (Active Blocked Rules)** with instant Windows Firewall rule creation.
- **✏️ Custom Manual Backup Naming / Note Field:** Added `txtCustomBackupName` to `BackupWindow`. Pre-fills timestamp (`yyyy-MM-dd_HH-mm-ss_`) automatically, allowing users to type custom backup labels.

---

## [v1.4.6] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📁 Win32 Shell `DragQueryPoint` ile İstemci Koordinat Tabanlı Sürükle - Bırak Kök Neden Çözümü:** Win32 `WM_DROPFILES` modunda `DragQueryPoint` ile pencere içi piksel X koordinatı doğrudan alınıp `bdPanel3` sol sınırı ile karşılaştırıldı. Panel 2 (Sadece Klasörler) ve Panel 3 (Sadece .EXE Dosyaları) ayrımı %100 kusursuzlaştırıldı.
- **⚡ Panel 3 Bırakmada Anında Güvenlik Duvarı Kuralı Oluşturma:** Panel 3'e bir `.EXE` bırakıldığında kural anında Windows Güvenlik Duvarı'na uygulanır ve listede anında canlı görünür.
- **🌍 Sürükle - Bırak Uyarı İletilerinde Dinamik Çoklu Dil:** Yanlış panel sürüklemelerindeki uyarı iletişim pencereleri 7 dilin tamamında (TR, EN, ES, DE, PT, AR, RU) seçili dilde görüntülenir.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📁 Win32 Shell `DragQueryPoint` Client-Based Drag & Drop:** Leverages `DragQueryPoint` for exact drop position relative to `bdPanel3`, accurately filtering Folders to Panel 2 and `.EXE` files to Panel 3.
- **⚡ Real-Time Rule Creation on Panel 3 Drop:** Dropping an `.EXE` onto Panel 3 immediately creates the Windows Firewall rule and updates the active list in real-time.
- **🌍 Localized Drag & Drop Warning Dialogs:** Warning dialogs for invalid drop targets automatically translate into all 7 supported languages.

---

## [v1.4.5] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **🌍 Tüm 7 Dilde %100 Eksiksiz Çeviri Sözlüğü:** `LanguageManager.cs` içerisindeki tüm eksik çeviri anahtarları tamamlandı. Türkçe, İngilizce, Almanca, İspanyolca, Portekizce, Arapça ve Rusça dillerinde tüm tablo sütun başlıkları ve tema isimleri tam çevrilmektedir.
- **🎨 Tema İsimlerinde Dinamik Dil Güncellemesi:** Dil değiştirildiğinde tema seçici kutusundaki tüm tema adları (*Koyu Tema*, *Açık Tema*, *Discord Teması*, *YouTube Teması*) seçilen dilde anında güncellenir.
- **📁 WPF `VisualTreeHelper.HitTest` ile Sürükle - Bırak Kök Neden Çözümü:** Win32 `WM_DROPFILES` modunda `VisualTreeHelper.HitTest` yöntemi ile imlecin bıraktığı görsel kontrol tespit edildi. Panel 2 (Klasörler) ve Panel 3 (EXE Dosyaları) ayrımı %100 kesinleştirildi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🌍 Complete 100% i18n Dictionaries for 7 Languages:** Added all missing translation keys across TR, EN, ES, DE, PT, AR, and RU for all DataGrid column headers, dialogs, and theme names.
- **🎨 Dynamic Theme Selector Names:** Theme dropdown names update instantly upon changing the application language.
- **📁 WPF `VisualTreeHelper.HitTest` Drag & Drop Fix:** Leverages visual tree hit testing during Win32 drop handling to isolate Panel 2 (Folders) and Panel 3 (EXEs) with 100% accuracy.

---

## [v1.4.4] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📊 Tüm Tablo Sütun Başlıklarında Tam Dinamik Dil Desteği:** `dgItems` (Profil İçeriği), `dgActiveRules` (Aktif Engelleme Listesi) ve `dgBackups` (Yedekler) tablolarındaki tüm sütun başlıkları seçilen dile göre (TR, EN, DE, ES, PT, AR, RU) anında güncellenir.
- **🚀 Anında `AutoGistOnStartup` Kaydı:** `chkAutoGist` onay kutusu işaretlendiği veya kaldırıldığı an `HaYTooL_Firewall.ini` dosyasına `AutoGistOnStartup=True/False` olarak anında kaydedilir. Uygulama açılışındaki otomatik bulut yedekleme sorunsuz çalışır.
- **⚡ Panel 3 Sürükle - Bırak Anında Kural Uygulama:** 3. Paneline bir `.EXE` dosyası bırakıldığında Windows Güvenlik Duvarı kuralı anında oluşturulur ve engelleme listesinde anında görünür.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📊 Dynamic DataGrid Column Headers Localization:** Column titles across all DataGrids (`dgItems`, `dgActiveRules`, `dgBackups`) dynamically translate upon language selection.
- **🚀 Instant `AutoGistOnStartup` Auto-Save:** Immediately persists `AutoGistOnStartup=True/False` to INI when checkbox state changes, ensuring startup Gist sync functions flawlessly.
- **⚡ Immediate Firewall Rule Creation on Panel 3 Drop:** Dropping an `.EXE` onto Panel 3 instantly applies the firewall rule and refreshes the list in real-time.

---

## [v1.4.3] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📁 WPF DPI-Uyumlu Ekran İmleç Sürükle - Bırak Hesabı (`GetCursorPos` + `PointFromScreen`):** Sürükle bırak anında fare imlecinin ekran pikseli WPF `PointFromScreen` ile dönüştürülüp Panel 3 sınırları (`panel3Bounds.Contains`) kusursuz kontrol edilir. %125/%150 DPI ölçeklemesinde dahi Panel 2 (Klasör) ve Panel 3 (EXE) ayrımı %100 kusursuz çalışır.
- **🌍 `btnLocalBackup` ve Tema İsimlerinde Tam Dinamik Çoklu Dil:** Dil değiştirildiğinde `💾 Yedekler` butonu, tooltip'leri ve `cmbThemeSelector` içerisindeki tüm tema adları (Koyu/Açık/Discord/YouTube) seçilen dilde anında güncellenir.
- **☁️ Açılışta Gist Yükleme `Loaded` Bağlantısı & Canlı Renklendirme:** Otomatik Gist yüklemesi pencerenin `Loaded` olayına bağlanarak UI yüklendikten sonra tetiklenmesi sağlandı. Yeşil (`#22C55E`) ve Kırmızı (`#EF4444`) arka plan fırçaları doğrudan atanarak durum butonunun belirgin renklendirilmesi kesinleştirildi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📁 DPI-Aware WPF Cursor Hit-Testing (`GetCursorPos` + `PointFromScreen`):** Accurately calculates drop coordinates across any DPI scaling (100%-200%), isolating Panel 2 (Folders) and Panel 3 (EXEs).
- **🌍 Dynamic Localization for Backup Button & Theme Selector:** Translates `btnLocalBackup` and all theme dropdown items dynamically upon language change.
- **☁️ Startup Gist Sync Attachment & Vibrant Status Color:** Attached startup Gist upload to Window `Loaded` event and set explicit SolidColorBrush colors (Green/Red) on `btnGistBackup`.

---

## [v1.4.2] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📁 Win32 `DragQueryPoint` Koordinat Tabanlı Sürükle - Bırak:** Fare konum pikseli alınarak Panel 2 (Klasör) ve Panel 3 (EXE) ayrımı %100 kesinleştirildi. Panel 3'e `.EXE` bırakıldığında anında profil kuralı uygulanıyor.
- **🌍 Gist ve Yerel Yedekler Pencerelerinde Dinamik Dil Desteği:** `GistWindow` ve `BackupWindow` pencerelerindeki tüm metinler, açıklamalar ve butonlar seçili dilde (TR, EN, DE, ES, PT, AR, RU) dinamik güncellenir.
- **💬 Yüksek Kontrastlı Okuşabilir ToolTip Stil Tanımı:** Tüm 4 temaya yüksek kontrastlı `ToolTip` stili eklendi. Fare butonların üzerine geldiğinde sarı/siyah okunmama ve arka plan rengi çakışması çözüldü.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📁 Win32 `DragQueryPoint` Coordinate Hit Testing:** Accurately isolates drop targets by mouse cursor coordinates (Panel 2: Folders, Panel 3: EXEs).
- **🌍 Dynamic i18n for Gist & Backup Windows:** Full dynamic localization across all languages for `GistWindow` and `BackupWindow`.
- **💬 High-Contrast Legible ToolTip Style:** Added contrast-rich `<Style TargetType="ToolTip">` across all 4 themes, ensuring 100% legible tooltips on hover.

---

## [v1.4.1] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📁 Panellere Özel Sürükle - Bırak (Drag & Drop) Kuralları:** 
  - Panel 2 (Profil İçeriği) yalnızca **Klasörleri** kabul eder, `.exe` dosyası sürüklendiğinde uyarı verir.
  - Panel 3 (Aktif Engelleme Listesi) yalnızca **`.EXE` dosyalarını** kabul eder, Klasör sürüklendiğinde uyarı verir.
- **☁️ Canlı Gist Yükleme Durum Butonu (Yeşil / Kırmızı):** `btnGistBackup` butonu yükleme esnasında `⏳ Yükleniyor`, başarılı olursa yeşil `✅ Yüklendi`, hata alırsa kırmızı `❌ Hata` olarak anlık renk değiştirir.
- **🎨 BackupWindow Tasarım İyileştirmesi:** Maksimum yedek sayısı giriş alanı dikey ortalanmış, hizalı ve şık Grid tasarımına dönüştürüldü.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📁 Panel-Specific Drag & Drop Rules:** Panel 2 accepts Folders only; Panel 3 accepts `.EXE` files only with explicit user guidance dialogs.
- **☁️ Visual Live Gist Status Indicator (Green/Red):** Header Gist button dynamically updates to `✅ Uploaded` (Green) or `❌ Error` (Red) with error tooltips.
- **🎨 BackupWindow Layout Refinement:** Realigned Max Backup Count setting inside a clean responsive Grid container.

---

## [v1.4.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **🔑 Token Silinme Sorununun Çözümü:** `AppSettings` sınıfına `GitHubToken`, `LastGistId`, `AutoGistOnStartup` vb. alanlar eklenerek, `.ini` dosyasına yapılan kaydetme işlemlerinde tokenın silinmesi %100 engellendi.
- **📁 Win32 OLE İptali & Kesin Drag & Drop Fix (`RevokeDragDrop`):** Win32 `RevokeDragDrop(hwnd)` çağrılarak WPF'in kısıtlayıcı OLE sürükle-bırak kilidi kaldırıldı. Yönetici (Admin) Modunda dahi masaüstünden/klasörden sürüklenen tüm dosyalar eksiksiz kabul ediliyor.
- **💾 Yeni Yerel Yedek Yönetimi Ekranı (`BackupWindow`):** Header barına `💾 Yedekler` butonu eklendi. Manuel yerel yedek alma, mevcut yedekleri listeleme/silme/geri yükleme ve açılışta yedekleme seçenekleri tek ekranda toplandı.
- **🚀 Açılışta Otomatik Gist Yedekleme:** `GistWindow` ekranına `🚀 Uygulama açılışında otomatik Gist'e yedekle` seçeneği eklendi. Gist yükleme sonrasındaki onay sorusu kaldırıldı.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🔑 Persistent Token Settings:** Integrated `GitHubToken` into `AppSettings` to prevent token erasure during INI save operations.
- **📁 OLE DragDrop Revocation (`RevokeDragDrop`):** Invoked Win32 `RevokeDragDrop` to bypass WPF OLE lock, enabling 100% working native Drag & Drop in Admin Mode.
- **💾 Local Backup Manager (`BackupWindow`):** Added a dedicated popup window to create, list, delete, and restore local timestamped INI backups.
- **🚀 Automatic Startup Gist Sync:** Option in `GistWindow` to automatically back up configuration to Gist on application launch.

---

## [v1.3.5] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **🛡️ Gist Payload Token Temizliği (Güvenlik Düzeltmesi):** Gist'e gönderilen `.ini` dosyasındaki `GitHubToken=...` satırı Regex ile otomatik temizlenir. Böylece GitHub Secret Scanner'ın tokenı ham metinde algılayıp otomatik iptal (revoke) etmesi %100 engellendi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🛡️ Gist Payload Token Sanitization:** Automatically scrubs `GitHubToken=...` lines from Gist payload text before sending, preventing GitHub Secret Scanner from revoking PAT tokens.

---

## [v1.3.4] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **☁️ Gist Akıllı PATCH Güncelleme Desteği:** İlk yüklemede oluşturulan Gist ID saklanır, sonraki tüm yüklemelerde `PATCH` atılarak var olan Gist güncellenir. İkinci yüklemelerde oluşan yetki/çakışma hatası tamamen giderildi.
- **📁 Sürükle - Bırak `PreviewDragEnter` ve Legacy MessageFilter Entegrasyonu:** WPF'in sürüklemeyi pencere sınırında iptal etmesini önlemek için `PreviewDragEnter` olayı ve Win32 `ChangeWindowMessageFilter` eklendi. Sürükle-bırak %100 sorunsuz hale getirildi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **☁️ Gist Smart PATCH Update:** Re-uploads now issue a REST `PATCH` request to update existing Gists seamlessly without duplicate or conflict errors.
- **📁 Drag & Drop `PreviewDragEnter` & Legacy Win32 Filter:** Added `PreviewDragEnter` and process-wide `ChangeWindowMessageFilter` to resolve WPF drag entry cancellation.

---

## [v1.3.3] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📁 Ana Pencere Seviyesinde Sürükle - Bırak (Root Window AllowDrop):** `MainWindow.xaml` kök etiketine `AllowDrop="True"` ve `PreviewDragOver`/`PreviewDrop` tanımlanarak sürükle-bırakın engellenmesi kökten çözüldü. Pencerenin herhangi bir yerine sürüklendiğinde dosya/klasör anında eklenir.
- **🔑 Gist Token Kalıcılığı & 🌐 Tarayıcıda Gist Aç Butonu:** GitHub Personal Access Token (PAT) bilgisi `HaYTooL_Firewall.ini` dosyasına kalıcı yazılır. Ayrıca Gist penceresine tek tıkla Gist sayfanızı tarayıcıda açan **`🌐 Gist'i Aç`** butonu eklendi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📁 Root Window AllowDrop Drag & Drop:** Added `AllowDrop="True"` and `PreviewDragOver`/`PreviewDrop` directly on the root `<Window>` tag to guarantee universal drop handling across the entire application.
- **🔑 Persistent Gist Token & 🌐 Open Gist in Browser:** Persisted GitHub PAT token to INI and added a **`🌐 Open Gist`** button to open the Gist page in your default browser.

---

## [v1.3.2] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📁 Win32 Yerel Drag & Drop (`WM_DROPFILES` / `HDROP` Kancası):** Windows OS seviyesinde mesaj kancası (`DragQueryFile`) eklenerek Yönetici (Admin) Modunda ve tüm Windows sürümlerinde %100 kusursuz çalışan yerel sürükle-bırak desteği sağlandı.
- **🔑 1-Tıkla GitHub Gist Token Oluşturma Bağlantısı:** `GistWindow` ekranına `🔑 1-Tıkla Token Al` bağlantısı eklendi. Tıklandığında doğrudan GitHub token oluşturma sayfasını açar. 401 yetkisizlik hatası ve token yönlendirmeleri düzeltildi.
- **📐 Pencere Konumu ve Boyutunun Kaydedilmesi:** Uygulama kapatıldığında pencerenin boyutu (`Width`, `Height`), konumu (`Top`, `Left`) ve durumu (`WindowState`) `HaYTooL_Firewall.ini` dosyasına kaydedilir ve yeniden açılışta birebir aynı konumda başlar.
- **🛑 GistWindow Çökme Düzeltmesi:** Tüm temalara `PrimaryButton` stili tanımlanarak Gist butonuna tıklandığında uygulamanın kapanmasına neden olan XAML kaynak hatası tamamen giderildi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📁 Native Win32 Drag & Drop (`WM_DROPFILES` Hook):** Implemented native Win32 `DragQueryFile` OS hook to guarantee 100% working file/folder drop even in elevated Admin Mode.
- **🔑 1-Click GitHub Gist Token Link:** Added a direct link in `GistWindow` to create a GitHub PAT with `gist` scope in 1 click.
- **📐 Window Bounds & Position Persistence:** Saves window dimensions, screen coordinates, and `WindowState` on exit and restores them on launch.
- **🛑 GistWindow Crash Fix:** Added `PrimaryButton` style to all theme resource dictionaries, fixing XAML ParseException on Gist button click.

---

## [v1.3.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **💾 Başlangıçta Otomatik Yerel Yedekleme (Max 30):** Uygulama her açıldığında `backup/` klasörüne zaman damgalı yedek kaydeder. Toplam yedek sayısı 30'u aştığında en eski yedekler otomatik temizlenir.
- **☁️ GitHub Gist Bulut Yedekleme & Geri Yükleme:** Konfigürasyonu tek tıkla GitHub Gist'e yükleme ve Gist ID/URL ile geri yükleme ekranı (`GistWindow`) eklendi.
- **📁 Tam Sürükle - Bırak (Drag & Drop) Desteği:** `PreviewDragOver` ve `PreviewDrop` tünelleme olayları kullanılarak DataGrid engelleyicisi aşıldı, Windows Gezgini'nden sürüklenen EXE ve klasörlerin eklenmesi kusursuzlaştırıldı.
- **🎨 İlerleme Ekranı Tema Arka Plan Koruması:** `ShowProgress` esnasında kontroller `IsEnabled` yerine `IsHitTestVisible = false` yapılarak kitlendi. Böylece 1. paneldeki profiller ve arayüz elemanlarının arka plan rengi bozulmadan tema renkleri korundu.
- **📏 Yön Sütunu Genişliği Düzeltmesi:** Aktif kurallar listesindeki "Yön" sütunu genişliği 130px'e çıkarılarak Almanca, İngilizce ve Türkçe metinlerin sığmama sorunu giderildi.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **💾 Startup Auto-Backup (Max 30 Rotation):** Auto-saves timestamped INI backups to `backup/` on every launch. Rotates and cleans backups beyond 30 files.
- **☁️ GitHub Gist Cloud Backup & Restore:** Export and import configuration via GitHub Gist (`GistWindow`).
- **📁 Drag & Drop Fixed:** Implemented `PreviewDragOver` and `PreviewDrop` events to reliably intercept file and folder drops over DataGrid.
- **🎨 Theme Background Protection During Progress:** Replaced `IsEnabled = false` with `IsHitTestVisible = false` to preserve control backgrounds and theme colors during scanning.
- **📏 Direction Column Width:** Expanded Direction column width to 130px to prevent text clipping across languages.

---

## [v1.2.1] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **📁 Profil İçeriğinde Sürükle - Bırak (Drag & Drop):** Windows Gezgini'nden EXE dosyaları veya klasörler doğrudan Profil İçeriği paneline sürüklenip bırakılarak eklenebilir.
- **🔗 Tek Satırda Kural Birleştirme (Gelen + Giden):** Aynı uygulama için hem Gelen hem Giden kuralı eklendiğinde listede 2 satır yerine tek satır gösterilir (`Gelen + Giden`). Kural silme veya durdurma işlemleri her iki kuralı da kapsar.
- **🗑️ Silme İşleminde Canlı İlerleme Çubuğu:** Profil silerken veya klasör çıkarırken kaldırılan kurallar anlık canlı sayaçla gösterilir (`x .exe temizlendi`), uygulama asla donmaz.
- **🎨 Tema & Renk Paleti İyileştirmeleri:** Light, Dark, Discord ve YouTube temalarında DataGrid satır renkleri, RadioButton, ComboBox ve Progress Overlay renk uyumsuzlukları giderildi.
- **🔤 Title Case Normalizasyonu:** TÜMÜ BÜYÜK HARF olan başlıklar ve buton metinleri ilk harfleri büyük, sonrası küçük şık Title Case formatına dönüştürüldü.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **📁 Profile Drag & Drop Support:** Drag and drop EXE files or folders directly into the Profile Content panel from Windows Explorer.
- **🔗 Single-Row Rule Merging (Inbound + Outbound):** When an app has both Inbound and Outbound rules, it is merged into a single row (`Inbound + Outbound`) instead of 2 lines.
- **🗑️ Live Deletion Progress Overlay:** Deleting categories or removing folders now shows a live counting progress overlay (`x .exe removed`).
- **🎨 Theme & Color Palette Refinement:** Polished DataGrid alternating row colors, RadioButtons, ComboBoxes, and overlays across Light, Dark, Discord, and YouTube themes.
- **🔤 Title Case Text Normalization:** Normalized ALL-CAPS headers and buttons to clean Title Case.

---

## [v1.2.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **🎨 Discord & YouTube Temaları:** Tema değiştirici yeniden tasarlandı. Basit toggle yerine 4 seçenekli ComboBox menüsü eklendi: `🌙 Koyu`, `☀️ Açık`, `🔵 Discord (Blurple)`, `▶️ YouTube (Kırmızı)`. Seçilen tema `HaYTooL_Firewall.ini` dosyasına kaydedilir ve her başlatmada otomatik yüklenir.
- **📂 Canlı Klasör Tarama İlerleme Çubuğu:** Klasör eklenirken veya kurallar uygulanırken uygulama artık donmuyor. Glassmorphism tasarımlı overlay ile hangi klasörün tarandığı, bulunan EXE sayısı canlı olarak gösterilir.
- **⚡ Asenkron Güvenlik Duvarı İşlemleri:** Tüm tarama, senkronizasyon ve kural uygulama işlemleri arka planda (`Task.Run`) çalışır, UI her zaman yanıt verir.
- **💾 Tema Kalıcılığı:** `IniStorage` sınıfına `ReadValue` / `SaveValue` genel yardımcı metodları eklendi. Tema tercihi uygulama genelinde `[Settings]` bölümüne yazılır.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🎨 Discord & YouTube Themes:** Theme switcher redesigned from a simple toggle button to a 4-option ComboBox: `🌙 Dark`, `☀️ Light`, `🔵 Discord (Blurple)`, `▶️ YouTube (Red)`. Selected theme is saved to `HaYTooL_Firewall.ini` and restored on startup.
- **📂 Live Folder Scan Progress Overlay:** Application no longer freezes when adding folders or applying rules. A glassmorphism overlay displays current scan path and found EXE count in real-time.
- **⚡ Async Firewall Operations:** All scanning, sync, and rule application operations run in background threads (`Task.Run`), keeping the UI always responsive.
- **💾 Theme Persistence:** Added `ReadValue` / `SaveValue` generic helper methods to `IniStorage`. Theme preference is persisted in `[Settings]` section.

---

## [v1.1.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Yeni Özellikler & İyileştirmeler
- **🔒 Çift Çalıştırma Koruması (Single Instance Mode):** `Mutex` ve Win32 `SetForegroundWindow` API'leri entegre edildi. Yazılım zaten açıkken 2. örnek açılmaya çalışıldığında yeni pencere kapatılıp mevcut pencere öne getirilir.
- **🏷️ Yönetici Yetki Rozeti (Admin Badge):** Başlık alanına uygulamanın yetkisini canlı gösteren yeşil/kırmızı yetki göstergesi (`🟢 Yönetici Modu` / `🔴 Sınırlı Mod`) eklendi.
- **🎨 Gerçek Renkli Ülke Bayrakları:** Windows varsayılan taslak emoji çizimleri yerine projenin `Resources/Flags/` dizinine 7 ülkeye ait yüksek kaliteli PNG bayrak görselleri eklendi (`flag_tr.png`, `flag_en.png`, `flag_es.png`, `flag_de.png`, `flag_pt.png`, `flag_ar.png`, `flag_ru.png`).
- **💬 Yerli WPF Giriş Penceresi (`InputDialog`):** WinForms bağımlılığı taşıyan eski `InputBox` kaldırılarak yerli, hafif ve çift temaya tam uyumlu WPF `InputDialog` oluşturuldu.
- **🌐 7 Dilli Dinamik Sağ Tık Menüleri:** 1, 2 ve 3. alanlardaki tüm sağ tık (Context) menü başlıkları dinamikleştirildi. Dil değiştirildiğinde menüler yeniden başlatma gerekmeden o dile çevrilir.
- **📝 İnsanca Okunabilir INI Formatı (`data.ini`):** Klasör ve EXE yolları `FolderLocation="..."` ve `ExeLocation="..."` şeklinde okunabilir anahtarlara dönüştürüldü.
- **🛡️ FullSafe İzin Verme (Whitelist) Mantığı:** FullSafe aktifken uygulamalara izin vermek için profillere `🟢 İzin Ver (Allow / Whitelist)` seçeneği ve `❓` yardım butonu eklendi.
- **⚡ Çoklu Seçim & Toplu İşlem:** Aktif engelleme listesinde `Ctrl+A` ile tüm kuralları seçip topluca silme ve durumlarnı değiştirme desteği getirildi.
- **🗑️ Klasör Temizleme:** Profil içeriğinden bir klasör çıkarıldığında altındaki tüm EXE kuralları otomatik Güvenlik Duvarı'ndan kaldırılır.

---

### 🇬🇧 English (EN)
#### 🚀 New Features & Improvements
- **🔒 Single Instance Protection:** Integrated `Mutex` and Win32 `SetForegroundWindow` APIs. Prevents multiple instances and brings existing window to front.
- **🏷️ Admin Status Badge:** Added a live privilege indicator (`🟢 Admin Mode` / `🔴 Limited Mode`) in the header.
- **🎨 Real Color Country Flag Icons:** Embedded 7 high-quality country flag PNGs in `Resources/Flags/` to render true flag icons on all Windows machines.
- **💬 Native WPF `InputDialog`:** Replaced WinForms-dependent `InputBox` with a native, lightweight, theme-aware WPF `InputDialog`.
- **🌐 7-Language Dynamic Context Menus:** Fully localized right-click context menus across all 3 panel sections.
- **📝 Human-Readable INI Storage (`data.ini`):** Formatted folder/exe paths with `FolderLocation="..."` and `ExeLocation="..."` key names.
- **🛡️ FullSafe Whitelist Support:** Added `🟢 Allow (Whitelist)` action option and a `❓` help button for FullSafe mode.
- **⚡ Multi-Selection & Batch Actions:** Extended DataGrid multi-selection (`Ctrl+A`) for batch delete and toggle actions.
- **🗑️ Folder Cleanup:** Removing a folder from a profile automatically purges all associated firewall rules for contained EXEs.

---

## [v1.0.0] - 2026-08-05

- **🎉 İlk Sürüm Yayınlandı (Initial Release):**
  - WPF tabanlı, INI depolamalı, tek ekranlı Windows Güvenlik Duvarı Yönetim Aracı.
  - Koyu (Dark) ve Açık (Light) tema desteği.
  - 7 Dil Desteği (`TR`, `EN`, `ES`, `DE`, `PT`, `AR`, `RU`).
  - Geliştirici: **HaYTo** (`korazhayto@gmail.com`).
