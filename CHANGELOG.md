# 📜 Değişiklik Günlüğü / Changelog

Tüm önemli değişiklikler bu dosyada belgelenmektedir. / All notable changes to this project will be documented in this file.

## [v6.11.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **📊 Detaylı Gelen/Giden Kural Sayacı Rozeti Düzeltildi:** Üst bardaki kural sayacı rozeti birleştirilmemiş ham Windows Güvenlik Duvarı kurallarını doğrudan sorgulayacak şekilde güncellendi. Engellenen ve İzin Verilen kural sayıları Gelen (`⬇️`) ve Giden (`⬆️`) bağlantı kırılımlarıyla birlikte gösterilir (`⛔ Engellenen: X (⬇️A ⬆️B) | 🟢 İzinli: Y (⬇️C ⬆️D)`). 7 dilde i18n desteği sağlandı.
- **🚀 Uygulama EXE Çıktı İsmi "HaYTooL Firewall.exe" Olarak Güncellendi:** Proje Anayasası (`0nogithub/clinerules.md`), `HaYTooL_Firewall.csproj`, `0nogithub/build.ps1` ve `0nogithub/build_release_zip.ps1` scriptlerinde derlenen yürütülebilir dosya adı alt çizgili halinden boşluklu **"HaYTooL Firewall.exe"** biçimine dönüştürüldü.
- **🗑️ "Tüm Kuralları Sil" Butonu & Canlı İlerleme Takibi:**
  - Panel 2 (İçerik) başlığına `Senkronize Et` butonunun yanına **"🗑️ Tüm Kuralları Sil"** butonu eklendi.
  - Windows Güvenlik Duvarı'ndaki tüm HaYTooL kurallarını silmeden önce kullanıcıdan onay uyarı penceresi alınır.
  - Silme işlemi sırasında uygulamanın donmasını önlemek için arka plan iş parçacığında (`async/await` + `Task.Run`) yürütülür.
  - İlerleme kartında silinen kural sayısı, silinmekte olan kural adı ve ilerleme çubuğu canlı olarak güncellenir.
  - Tüm 7 dilde (`TR`, `EN`, `ES`, `DE`, `PT`, `AR`, `RU`) i18n desteği ve çift tema uyumluluğu sağlandı.

---

### 🇬🇧 English (EN)
- **📊 Detailed Inbound/Outbound Rule Counter Badge Fixed:** Updated header bar rule counter badge to query raw unmerged Windows Firewall rules. Displays Blocked and Allowed totals with explicit Inbound (`⬇️`) and Outbound (`⬆️`) breakdowns (`⛔ Blocked: X (⬇️A ⬆️B) | 🟢 Allowed: Y (⬇️C ⬆️D)`). Full i18n support across 7 languages.
- **🚀 Executable Output Name Updated to "HaYTooL Firewall.exe":** Updated project constitution (`0nogithub/clinerules.md`), `HaYTooL_Firewall.csproj`, `0nogithub/build.ps1`, and `0nogithub/build_release_zip.ps1` to output single-file executable as **"HaYTooL Firewall.exe"** with spaces instead of underscores.
- **🗑️ "Delete All Rules" Button & Live Progress Tracking:**
  - Added **"🗑️ Delete All Rules"** button next to the `Sync Folders` button in Panel 2 header.
  - Asks for user confirmation before clearing all HaYTooL rules from Windows Firewall.
  - Runs in a background thread (`async/await` + `Task.Run`) to keep the UI completely responsive without freezing.
  - Live progress overlay dynamically reports deleted rule count, current rule name being removed, and progress bar state.
  - Full i18n support across 7 languages (`TR`, `EN`, `ES`, `DE`, `PT`, `AR`, `RU`) and dual-theme compatibility.

---

## [v6.10.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Görev Yöneticisinden Profile Eklemede Anında Kural Engelleme:**
  - Görev Yöneticisinde `📄 EXE'yi Profile Ekle` veya `📁 Klasörü Profile Ekle` eylemleri seçildiğinde, ilgili öge sadece profile eklenmekle kalmaz; Windows Güvenlik Duvarı'nda **Gelen & Giden Engelleme kuralı anında uygulanır** (klasörler için içerisindeki tüm EXE'leri tarayarak uygular).

---

### 🇬🇧 English (EN)
- **⚡ Instant Firewall Rule Application on Profile Add from Task Manager:**
  - When selecting `Add EXE to Profile` or `Add Folder to Profile` in Task Manager, items are not only appended to the target profile, but Windows Firewall **Inbound & Outbound Block rules are immediately enforced** (for folders, all nested EXEs are scanned and blocked).

---

## [v6.9.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Görev Yöneticisinde Otomatik Liste Yenileme:**
  - Görev Yöneticisinde `⛔ Engelle`, `🟢 İzin Ver`, `🗑️ Kuralı Sil`, `📄 EXE'yi Profile Ekle` veya `📁 Klasörü Profile Ekle` işlemlerinden herhangi biri yapıldığında, işlem tamamlanır tamamlanmaz Görev Yöneticisi tüm süreç listesini, durum rozetlerini ve profil eşleşmelerini otomatik olarak anında yeniler.

---

### 🇬🇧 English (EN)
- **⚡ Automatic Task Manager List Refresh:**
  - After executing any action in Task Manager (`Block`, `Allow`, `Delete Rule`, `Add EXE to Profile`, or `Add Folder to Profile`), the process list, firewall status badges, and profile memberships automatically refresh in real-time.

---

## [v6.8.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Durum Rozeti Dil Çevirileri & Klasör Kuralı Akıllı Kural Silme Algılayıcısı:**
  1. **Tüm 7 Dilde Durum Rozeti Desteği:** `TaskMgrStatusBlockedBoth`, `TaskMgrStatusAllowedBoth`, `TaskMgrNetActive` vb. 8 çeviri anahtarı 7 dilde (TR, EN, ES, DE, PT, AR, RU) tanımlanarak `TaskMgrstatusblockedboth` ham metin görünmesi kesin olarak engellendi.
  2. **Klasör Kuralı Akıllı Silme Akışı:** Görev Yöneticisinde bir sürecin kuralı silinirken, eğer o süreç profilde bir **Klasör ögesi** kapsamında engelleniyorsa akıllı diyalog penceresi belirir ve ister sadece o EXE'nin özel kuralını, ister profildeki tüm klasör kuralını silme tercihi kullanıcıya sunulur.

---

### 🇬🇧 English (EN)
- **⚡ Status Badge Localizations & Smart Folder Rule Deletion:**
  1. **7-Language Status Badges:** Added 8 status badge translation keys across all 7 supported languages, fixing raw key string display bugs.
  2. **Smart Folder Rule Deletion:** When deleting a process rule in Task Manager that is part of a profile folder rule, an intelligent prompt allows deleting either the single EXE firewall rule or the entire parent folder profile rule.

---

## [v6.7.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Derinlemesine Kural Temizliği (`RemoveRulesByPath`) & Dil Çeviri Anahtar Düzeltmesi:**
  1. **Tam Kural Temizliği (`RemoveRulesByPath`):** `BtnDeleteRule_Click` butonunun çalışmama sorunu çözüldü. Artık sürecin EXE yoluna (`ApplicationName`) bağlı tüm Windows Güvenlik Duvarı kurallarını tam tespit edip temizler ve ekli olduğu profilden de siler.
  2. **Eksik Dil Anahtarı Düzeltildi:** TR ve EN sözlüklerine eksik olan `TaskMgrBtnDeleteRule` (`🗑️ Kuralı Sil` / `🗑️ Delete Rule`) anahtarı işlenerek ham metin görünmesi engellendi.

---

### 🇬🇧 English (EN)
- **⚡ Deep Rule Deletion (`RemoveRulesByPath`) & Localization Key Fix:**
  1. **Deep Rule Deletion (`RemoveRulesByPath`):** Fixed `BtnDeleteRule_Click` execution logic. It now scans and purges all matching Windows Firewall rules by application path (`ApplicationName`) and removes the item from any profile containing it.
  2. **Localization Key Fix:** Added missing `TaskMgrBtnDeleteRule` key to TR and EN language dictionaries, resolving raw key text display issues.

---

## [v6.6.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Görev Yöneticisinde Kuralı Sil Butonu & 7 Dilde Eksiksiz Çeviri Düzeltmesi:**
  1. **Görev Yöneticisinde Kuralı Sil Butonu (`btnDeleteRule` / `ctxDeleteRule`):** Görev Yöneticisi alt araç çubuğuna ve sağ tık menüsüne **`🗑️ Kuralı Sil`** eylemi eklendi. Seçili sürecin Windows Güvenlik Duvarı kuralını anında temizler ve durumunu `⚪ Kural Yok` olarak günceller.
  2. **Üst Araç Çubuğu Buton Çevirisi:** Ana penceredeki `⚡ Görev Yöneticisi` (`btnProcessManager`) butonunun dil değiştirildiğinde seçilen dile çevrilmesi sağlandı.
  3. **Eksiksiz 7 Dil Desteği:** `LanguageManager.cs` içerisindeki `ES`, `DE`, `PT`, `AR`, `RU` dil sözlüklerine tüm Görev Yöneticisi metinleri tam olarak işlendi. Diğer dillerde Türkçe'ye düşme (fallback) sorunu tamamen çözüldü.

---

### 🇬🇧 English (EN)
- **⚡ Delete Rule Action in Task Manager & Full 7-Language Dictionary Fix:**
  1. **Delete Rule Action (`btnDeleteRule` / `ctxDeleteRule`):** Added a **`🗑️ Delete Rule`** action button and context menu item to remove firewall rules directly from Task Manager.
  2. **Header Button Localization:** Fixed `⚡ Task Manager` (`btnProcessManager`) button text in main window toolbar to dynamically update across all languages.
  3. **Full 7-Language Dictionaries:** Added missing Task Manager translation keys to Spanish (ES), German (DE), Portuguese (PT), Arabic (AR), and Russian (RU) dictionaries.

---

## [v6.5.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Görev Yöneticisinde Profil Sütunu, Profil Konumlandırma & Tam 7 Dil Sözlüğü:**
  1. **Profil Sütunu (`colProfile`):** Görev Yöneticisi tablosuna süreçlerin hangi HaYTooL Firewall profilinde yer aldığını anında gösteren **`Profil`** sütunu eklendi (`📁 Oyunlar`, `📁 İnternet` veya `⚪ Profil Dışı`).
  2. **Profilini Bul & Göster (`ctxLocateProfile`):** Görev Yöneticisinde herhangi bir sürece sağ tıklayıp `🎯 Profilini Bul & Göster` seçeneğine basıldığında, ana penceredeki ilgili profili otomatik bulur ve Panel 1'de seçer.
  3. **Exsiksiz 7 Dil Sözlüğü:** Tüm Görev Yöneticisi metinleri, başlıkları, filtre tanımları, ram/ağ/profil sütun başlıkları ve rozet metinleri 7 dilde (TR, EN, ES, DE, PT, AR, RU) tam sözlük olarak tanımlandı.

---

### 🇬🇧 English (EN)
- **⚡ Profile Column, Locate Profile Action & Full 7-Language Dictionary:**
  1. **Profile Column (`colProfile`):** Added a dedicated **`Profile`** column to Task Manager displaying process profile membership (`📁 Games`, `📁 Internet` or `⚪ Outside Profile`).
  2. **Locate Profile (`ctxLocateProfile`):** Right-clicking any process and selecting `🎯 Locate Profile` automatically finds and selects the corresponding profile in Panel 1 of the main window.
  3. **Full 7-Language Dictionaries:** Added complete Task Manager translation keys across all 7 supported languages (TR, EN, ES, DE, PT, AR, RU).

---

## [v6.4.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Kural Eylemlerinde Otomatik Profile Ekleme & Çoklu CheckBox Filtreleme:**
  1. **Kural Verildiğinde Profile Otomatik Kayıt:** Görev Yöneticisinde bir sürece `⛔ Engelle` veya `🟢 İzin Ver` denildiğinde hem Windows Güvenlik Duvarı kuralı oluşturulur hem de süreç EXE'si otomatik olarak ana penceredeki aktif seçili profile eklenip kaydedilir.
  2. **Ufaltılmış Arama & Çoklu CheckBox Filtre:** Arama kutusu ufaltıldı (`250px`). ComboBox yerine birden fazla filtrenin aynı anda seçilebildiği CheckBox filtre grubu (`[x] 📡 Ağ Aktif`, `[x] ⛔ Engelliler`, `[x] 🟢 İzinliler`, `[x] ⚪ Kuralı Olmayanlar`) eklendi.
  3. **7 Dilde Eksiksiz Çeviri:** Görev Yöneticisi butonu, başlıklar, butonlar, sağ tık context menüsü ve dinamik durum metinleri 7 dilde tam çevrildi. Ana pencereden dil değiştirildiğinde açık olan Görev Yöneticisi de canlı olarak dili günceller.

---

### 🇬🇧 English (EN)
- **⚡ Auto Add-to-Profile on Rule Action & Multi-CheckBox Filters:**
  1. **Auto Add to Profile:** Clicking `⛔ Block` or `🟢 Allow` in Task Manager applies the firewall rule and automatically adds/saves the EXE into the active profile.
  2. **Compact Search & Multi-CheckBox Filters:** Compacted search bar (`250px`) and added multi-selection CheckBox filter controls (`[x] 📡 Network Active`, `[x] ⛔ Blocked`, `[x] 🟢 Allowed`, `[x] ⚪ No Rule`).
  3. **Full 7-Language Localization:** Fully localized Task Manager headers, buttons, context menus, and live status badges across all 7 supported languages.

---

## [v6.3.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Görev Yöneticisinde Canlı Ağ Etkileşimi & Yön Bazlı Güvenlik Duvarı Durumu:**
  1. **Gelen/Giden Yön Ayrımı:** Güvenlik Duvarı Durumu sütununda artık detaylı yön bilgileri (`⛔ Gelen & Giden Engelli`, `📥 Gelen Engelli`, `📤 Giden Engelli`, `🟢 Gelen & Giden İzinli`) rozetler ile gösterilmektedir.
  2. **Gereksiz PID Sütunu Kaldırıldı:** Tablodan gereksiz PID sütunu çıkarıldı.
  3. **Canlı Ağ Bağlantısı Etkileşimi (`NetworkHelper`):** Win32 IP Helper API (`GetExtendedTcpTable`/`GetExtendedUdpTable`) entegre edilerek her sürecin o anki canlı TCP/UDP ağ etkileşimi (`🌐 X Aktif Bağlantı` / `💤 Ağ Yok`) tablosuna eklendi ve ağ trafiğine göre filtreleme seçeneği sunuldu.
  4. **Klasör Bazlı Profile Ekleme:** Görev Yöneticisi sağ tık menüsüne ve alt eylem çubuğuna **`📁 EXE'nin Klasörünü Profile Ekle`** butonu eklendi.

---

### 🇬🇧 English (EN)
- **⚡ Live Network Interaction & Directional Firewall Status in Task Manager:**
  1. **Directional Firewall Status:** Detailed directional status badges (`⛔ Inbound & Outbound Blocked`, `📥 Inbound Blocked`, `📤 Outbound Blocked`, `🟢 Inbound & Outbound Allowed`).
  2. **PID Column Removed:** Cleaned up table space by removing unnecessary PID column.
  3. **Live Network Activity (`NetworkHelper`):** Integrated Win32 IP Helper API (`GetExtendedTcpTable`/`GetExtendedUdpTable`) to display live TCP/UDP socket activity (`🌐 X Active Connections` / `💤 No Network`) per process with network filtering.
  4. **Add Parent Directory to Profile:** Added **`📁 Add Folder to Profile`** button to right-click context menu and action toolbar.

---

## [v6.2.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **🚀 Bağımsız (Modeless) Görev Yöneticisi & Canlı Profil Senkronizasyonu:** `ProcessWindow` (Görev Yöneticisi) modal engellemeden çıkarılıp bağımsız modeless (`.Show()`) mimariye geçirildi. Artık Görev Yöneticisi açıkken ana pencere tam aktif kalır; kullanıcı ana pencerede profil değiştirebilir ve Görev Yöneticisi'ndeki **`📁 Profile Ekle`** butonu o an seçili olan profile canlı olarak öge ekler. Hiçbir kilitlenme veya engel oluşturmaz.

---

### 🇬🇧 English (EN)
- **🚀 Modeless Task Manager & Live Active Profile Sync:** Switched `ProcessWindow` from modal dialog (`ShowDialog()`) to modeless execution (`Show()`). Users can now interact with the main window while Task Manager is open, switch active profiles in real-time, and add running process EXEs directly to the currently highlighted profile.

---

## [v6.1.1] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **🐛 Görev Yöneticisi NullReferenceException Düzeltmesi:** `ProcessWindow` oluşturulurken `cmbFilter` varsayılan seçim olayının (`SelectionChanged`) henüz yüklenmemiş kontrol elemanlarına erişerek `NullReferenceException` hatası vermesi engellendi.

---

### 🇬🇧 English (EN)
- **🐛 Task Manager NullReferenceException Fix:** Resolved a `NullReferenceException` in `ProcessWindow` caused by early triggering of `cmbFilter` selection changes before XAML visual tree initialization.

---

## [v6.1.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Dahili Güvenlik Duvarı Görev Yöneticisi (`ProcessWindow`):** Üst araç çubuğuna **`⚡ Görev Yöneticisi`** butonu eklendi. Bilgisayarda aktif çalışan tüm süreçleri (PID, Uygulama Adı, Bellek/RAM kullanımı, Gerçek Zamanlı Güvenlik Duvarı Durumu `⛔ Engelli / 🟢 İzinli / ⚪ Kural Yok`) listeleyen, canlı arama, filtreleme, tek tıkla engelleme/izin verme, seçili profile aktarma ve süreç sonlandırma (Kill Process) imkanı sunan özel pencere geliştirildi. 7 dilde tam destek sağlandı.

---

### 🇬🇧 English (EN)
- **⚡ Built-In Firewall Task Manager (`ProcessWindow`):** Added a dedicated **`⚡ Task Manager`** window accessible from the main toolbar. Displays active Windows processes with PID, RAM usage, process icons, and live Firewall Status (`⛔ Blocked / 🟢 Allowed / ⚪ No Rule`). Features live search, filtering, one-click block/allow, direct addition to active firewall profile, process termination (Kill Process), and Explorer location opening.

---

## [v6.0.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Çift Yönlü (Gelen + Giden) Profil Düzeyinde Hızlı Kural Yönetimi:** Profil sağ tık menüsüne tek tıkla hem Gelen hem de Giden bağlantıları aynı anda yöneten **`⛔ Gelen / Giden Tümünü Engelle`** ve **`🟢 Gelen / Giden Tümüne İzin Ver`** komutları eklendi. Seçilen profil içerisindeki tüm EXE ve klasörlerin kuralları çift yönlü olarak anında güncellenir. 7 dilde tam destek sağlandı.

---

### 🇬🇧 English (EN)
- **⚡ Dual-Direction (Inbound + Outbound) Profile-Wide Quick Actions:** Added **`⛔ Block All (Inbound + Outbound)`** and **`🟢 Allow All (Inbound + Outbound)`** options directly to the profile right-click context menu. One-click solution to update both Inbound & Outbound rules simultaneously for all EXEs and folders inside a profile.

---

## [v5.9.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **⚡ Profil Düzeyinde Toplu Kural Yönetimi:** Sol menüdeki profillere (`lstCategories`) sağ tıklandığında **`📥 Gelen Bağlantı (Tüm Profil)`** ve **`📤 Giden Bağlantı (Tüm Profil)`** alt menüleri eklendi. Seçilen kural eylemi (`⛔ Tümünü Engelle` / `🟢 Tümüne İzin Ver`) ilgili profilde yer alan **tüm EXE ve klasörlere** anında topluca uygulanır. 7 dile tam duyarlılık sağlandı.

---

### 🇬🇧 English (EN)
- **⚡ Profile-Wide Batch Rule Management:** Added right-click context submenus (**`📥 Inbound Connections (All Profile)`** & **`📤 Outbound Connections (All Profile)`**) to Panel 1 profiles. Allows batch applying Block All or Allow All actions across **all EXEs and folders** in a profile in one click. Fully localized across 7 languages.

---

## [v5.8.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **📊 Üst Menüde Engellenmiş / İzin Verilmiş EXE Sayacı:** Dil seçim açılır kutusunun (`cmbLanguage`) soluna aktif Windows Güvenlik Duvarı kurallarına dayalı canlı **`⛔ X | 🟢 Y`** EXE sayacı rozeti (`bdRuleCounters`) eklendi. 7 dilde dinamik ipucu (ToolTip) desteği sağlandı.

---

### 🇬🇧 English (EN)
- **📊 Header Live Blocked / Allowed EXE Counters:** Added a live EXE rule status badge (`bdRuleCounters`) displaying **`⛔ X | 🟢 Y`** to the left of the language dropdown. Fully localized with hover tooltips across all 7 supported languages.

---

## [v5.7.0] - 2026-08-08

### 🇹🇷 Türkçe (TR)
- **🐛 Pasif Profil Kural Yönetimi Düzeltmesi:** Pasif (🔴 Pasif / İzin Verilmemiş) profillerde öğelere sağ tıklanıp Gelen/Giden bağlantı durumu değiştirildiğinde, kuralların Windows Güvenlik Duvarı'nda hatalı şekilde aktifleşmesi engellendi. Ayarlar INI dosyasına kaydedilir, ancak profil pasif olduğu sürece Windows Güvenlik Duvarı'na aktif kural eklenmez (`RemoveAppRules` ile korunur).

---

### 🇬🇧 English (EN)
- **🐛 Passive Profile Rule Enforcement Fix:** Fixed an issue where changing Inbound/Outbound rule states on items inside a passive (disabled) profile inadvertently created active rules in Windows Firewall. Item rule states are saved locally, but rules remain inactive in Windows Firewall until the profile itself is toggled on.

---

## [v5.6.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🎯 Panel 2 (Profil İçeriği) Ortalanmış Tarama Katmanı & Keskinleştirilmiş İkaz Kartı:** Klasör sağ tık kural eylemlerinde açılan canlı tarama penceresi (`gridProgressOverlay`) Panel 1'e kayması engellenerek tam olarak Panel 2'nin (Profil İçeriği alanının) ortasına taşındı. Boş profil ikaz kartındaki (`borderEmptyProfile`) bulanıklık hissi `UseLayoutRounding="True"`, `SnapsToDevicePixels="True"` ve `ClearType` piksel hizalamalarıyla giderilerek 4K keskinliğe ulaştırıldı.

---

### 🇬🇧 English (EN)
- **🎯 Centered Panel 2 Scan Overlay & Sharp HD Empty State Card:** Moved live progress scan overlay to be centered directly over Panel 2 (Profile Content area). Enhanced empty profile card rendering with ClearType subpixel snapping and crisp layout rounding to eliminate fuzziness.

---

## [v5.5.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **✨ Ortalanmış Boş Profil Kartı (DataGrid Tablo Bütünlüğü Korundu):** Boş profillerde DataGrid tablosuna sahte satır ekleme yöntemi tamamen kaldırıldı; böylece tablo sütun düzeni (`Simge`, `Adı`, `Konum`, `Gelen`, `Giden`) hiç bozulmadan saf ve temiz bırakıldı. Boş profillerde panelin tam ortasında şık, gölgeli ve 7 dile tam duyarlı **`borderEmptyProfile`** overlay kartı gösterilir.

---

### 🇬🇧 English (EN)
- **✨ Centered Empty Profile Card Overlay:** Replaced DataGrid dummy rows with a clean centered `borderEmptyProfile` overlay card. Preserves DataGrid table column integrity and structure while providing multi-language empty state guidance.

---

## [v5.4.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🎨 Vurgulu & Dikkat Çekici Boş Profil Görseli:** Boş profillerde görüntülenen ikaz metni `💡 Bu profil henüz boş. Sürükle-bırak ile klasör veya EXE ekleyebilirsiniz.` şeklinde ampul simgesi, kalın tipografi (Bold) ve tüm temalarda yüksek görünürlüklü elektrik mavisi (`#3B82F6`) vurgu rengi ile yenilendi.

---

### 🇬🇧 English (EN)
- **🎨 Eye-Catching Empty Profile State UI:** Enhanced empty profile indicator with a bold electric blue (`#3B82F6`) accent color and bulb icon for maximum visibility across dark and light themes.

---

## [v5.3.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **⚡ Anında Profil İçeriği Yükleme (0ms Latency) & Seri Geçiş Koruması:** Profil seçiminde kök öğeler 0 milisaniyede anında DataGrid tablosuna basılır; klasörler arka planda taranıp dinamik genişletilir. Seri profil geçişlerinde eski taramaları iptal eden yenileme kimliği koruması eklenerek alanın boş görünmesi veya çakışması tamamen engellendi.

---

### 🇬🇧 English (EN)
- **⚡ Instant 0ms Profile Content Rendering & Rapid Switch Guard:** Immediately renders profile root items to the DataGrid without any loading delay while deep folder scanning runs asynchronously in background. Prevents empty UI state and race conditions during rapid profile switching.

---

## [v5.2.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🔍 Genişletilmiş Canlı Tarama & İlerleme Penceresi UI İyileştirmesi:** Büyük klasörler taranırken açılan canlı ilerleme katmanı (`gridProgressOverlay`) genişliği 520px'den 740px'e çıkarıldı. Derin dosya/klasör yollarının kesilmeden tam okunabilmesi için yazı boyutu, paddings ve 2 satırlı kaydırmalı yol görünümü sağlandı.

---

### 🇬🇧 English (EN)
- **🔍 Expanded Live Progress Modal UI Enhancement:** Expanded live scan overlay modal width from 520px to 740px. Increased readability and padding for long file paths during deep folder scans.

---

## [v5.1.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **📖 FullSafe Dokümantasyon Güncellemesi (`README.md`):** FullSafe Modunun (Sıfır Güven / Zero-Trust) çalışma prensipleri, genel giden bağlantı kilitlenmesi ve `🛡️ FullSafe` beyaz liste profilinin sürükle-bırak izin verme mekanizması `README.md` dokümanında Türkçe ve İngilizce bölümlerine ayrıntılı olarak eklendi.

---

### 🇬🇧 English (EN)
- **📖 FullSafe Documentation Enhancement (`README.md`):** Updated `README.md` with comprehensive documentation on FullSafe Zero-Trust default-deny protection and the `🛡️ FullSafe` Whitelist isolation workflow in both Turkish and English sections.

---

## [v5.0.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🛡️ Varsayılan "FullSafe" Kategorisi & Otomatik İzin Verilen Sürükle-Bırak:** Varsayılan başlangıç kategorisi tekil olarak `🛡️ FullSafe` yapıldı (diğer kategoriler temizlendi). `FullSafe` kategorisine sürükle-bırak veya menüden eklenen tüm `.exe` ve klasörler için gelen/giden Windows Güvenlik Duvarı kuralları otomatik olarak **İzin Verildi (Allow / Whitelist)** statüsünde oluşturulur.

---

### 🇬🇧 English (EN)
- **🛡️ Default "FullSafe" Category & Auto-Allowed Drag and Drop:** Simplified default initial profile setup to a single `🛡️ FullSafe` category. All `.exe` files and folders dropped or added into `FullSafe` profiles now automatically generate **Allowed (Whitelist)** inbound and outbound rules.

---

## [v4.9.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **💬 Gist İpucu & Çoklu Dil Çeviri Temizliği:** "☁️ Gist" butonu üzerindeki karmaşık Gist ID kodu yerine sade ve açıklayıcı ipucu metni getirildi. Yerel yedek yönetimindeki "Manuel Yedek Adı / Notu" ve tablo başlıklarının tüm 7 dilde eş zamanlı çevrilmesi sağlandı.

---

### 🇬🇧 English (EN)
- **💬 Gist ToolTip & i18n Localization Fix:** Replaced complex Gist ID hash in button ToolTip with clear, friendly text. Added missing `BackupCustomName` and table column headers across all 7 language dictionaries.

---

## [v4.8.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🔐 Gist Token Güvenlik İyileştirmesi & "Tokenı Sil" Butonu:** Gist bulut yedekleme penceresindeki Token göster/gizle ikonu tamamen kaldırıldı. Kayıtlı tokenın açık metin olarak sızmasını önlemek adına şifreli maskeleme korundu ve yerine `🗑️ Tokenı Sil` butonu eklendi.

---

### 🇬🇧 English (EN)
- **🔐 Gist Token Security Enhancement & "Clear Token" Button:** Removed plain-text token toggle icon from Gist dialog. Preserved masked password input and introduced a dynamic `🗑️ Clear Token` action button for user credential wiping.

---

## [v4.7.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **📜 Kural 45 (Anayasa Güncellemesi - Derleme Sonrası Otomatik EXE Başlatma Kuralı):** Proje Anayasası (`0nogithub/clinerules.md`) güncellenerek Kural 45 eklendi. `0nogithub/build.ps1` derlemesi tamamlandığı an `cmd /c start ""` komutu ile üretilen `HaYTooL_Firewall.exe` doğrudan masaüstünde otomatik çalıştırılır.

---

### 🇬🇧 English (EN)
- **📜 Rule 45 (Constitution Update - Post-Build Desktop EXE Auto-Launch Rule):** Updated project constitution (`0nogithub/clinerules.md`) to add Rule 45, automatically triggering desktop GUI execution of `HaYTooL_Firewall.exe` via Windows Shell after every successful build.

---

## [v4.6.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🏷️ Sürüm Bilgili Otomatik & Manuel Yedek İsimlendirmesi:** Yerel olarak alınan tüm otomatik ve manuel yedek dosyalarının isimlerine mevcut uygulama versiyonu otomatik eklenir (Örn: `HaYTooL_Backup_v4.6.0_2026-08-08_00-39-45.ini`).

---

### 🇬🇧 English (EN)
- **🏷️ Version-Tagged Backup Naming:** Automatically embeds current application release versions into generated backup filenames (e.g. `HaYTooL_Backup_v4.6.0_2026-08-08_00-39-45.ini`).

---

## [v4.5.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🔄 Sıfırdan Temiz Geri Yükleme (Clean Restore Engine):** Yerel veya GitHub Gist yedeği geri yüklenirken, eski kurallarla çakışmayı veya kural birleşmelerini önlemek amacıyla Windows Güvenlik Duvarı'ndaki tüm var olan kurallar tamamen temizlenir ve yedeğin içeriği sıfırdan uygulanır.
- **🛡️ 30 Yedek Limiti & 7 Gün Korumalı Günlük Rotasyon Politikası:** Toplam yerel yedek limiti 30 olarak uygulanır. Rotasyon sırasında son 7 günün her bir günü için en güncel 1 yedek korumalı olarak saklanır; kalan esnek yedeklerden en eskileri temizlenerek toplam sayı 30'a tamamlanır.

---

### 🇬🇧 English (EN)
- **🔄 Clean Restore Engine:** Prevents rule duplication or merging during local or Gist backup restores by completely purging existing Firewall rules before applying restored profile contents from scratch.
- **🛡️ 30 Backup Limit & 7-Day Protected Rotation Policy:** Enforces a 30 backup limit with smart daily retention. Preserves 1 protected backup per day for the last 7 days while pruning the oldest non-protected backups to maintain the 30 backup quota.

---

## [v4.4.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **👁️ Maskeli Token Girişi & Göz İkonu Butonu (`GistWindow` UI Security):** Gist ekranındaki token alanı varsayılan olarak **PasswordBox (`••••••••`)** şeklinde gizlenmiştir. Ekran paylaşımı, video kaydı veya yanınızdaki birinin tokenı görmesi %100 engellenir.
- **🙈 Tek Tıkla Göster / Gizle:** Yanındaki `👁️` butonuna tıklanarak token istendiğinde düz metin olarak görüntülenebilir veya tekrar gizlenebilir.

---

### <ctrl42> English (EN)
- **👁️ Masked Token Input with Show/Hide Eye Toggle (`GistWindow` UI Security):** Masked PAT input field using `PasswordBox` (`••••••••`) by default to prevent shoulder surfing or stream leaks.
- **🙈 One-Click Toggle:** Click `👁️` eye icon to safely reveal or re-hide credentials.

---

## [v4.3.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **⚙️ Yapılandırma Depolama İyileştirmeleri:** `.ini` ayar dosyası okuma ve yazma kararlılığı artırıldı.

---

### 🇬🇧 English (EN)
- **⚙️ Configuration Storage Enhancements:** Improved `.ini` settings storage and retrieval stability.

---

## [v4.2.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🛠️ `[Window]` Sistem Bölümü Düzeltmesi (System Section Parsing Fix):** Uygulama kapatılırken pencere koordinatlarının kaydedildiği `[Window]` INI bölümünün yanlışlıkla profil kategorisi olarak yüklenmesi engellendi.
- **✨ Varsayılan 5 Kafa Başlangıç Kategorisi:** Hiç kategori olmaması durumunda ilk kurulumda kullanıcılara 5 hazır standart kategori sunulur (`🎮 Oyunlar`, `🌐 Tarayıcılar`, `💬 Sohbet & İletişim`, `🛠️ Araçlar & Sistem`, `🎬 Medya & Eğlence`).

---

### 🇬🇧 English (EN)
- **🛠️ System `[Window]` Section Parsing Fix:** Prevented window layout metadata sections (`[Window]`) from being misparsed into application profile categories.
- **✨ 5 Default Preset Categories:** Added 5 popular default categories for fresh installations (`🎮 Games`, `🌐 Browsers`, `💬 Chat & Communication`, `🛠️ Tools & System`, `🎬 Media & Entertainment`).

---

## [v4.1.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🔑 INI Dosyasında GitHub Token Anahtarı Güncellendi (`GitHub=`):** `HaYTooL_Firewall.ini` dosyasında kişisel erişim tokenı saklayan `GitHubToken=` anahtarı kısa ve net olarak `GitHub=` olarak güncellendi.
- **🛡️ Tam Geriye Dönük Uyumluluk & Gist Güvenlik Temizliği:** Eski `GitHubToken=` anahtarını içeren `.ini` dosyaları kesintisiz okunabilmektedir. Gist bulut yedeklemesi öncesinde hem `GitHub=` hem de `GitHubToken=` satırlarındaki hassas token bilgisi Regex ile otomatik olarak temizlenir, böylece GitHub Secret Scanner'ın tokenı iptal etmesi %100 engellenir.

---

### 🇬🇧 English (EN)
- **🔑 Updated INI Key to `GitHub=`:** Renamed the INI setting key from `GitHubToken=` to `GitHub=` for streamlined configuration readability.
- **🛡️ Full Backward Compatibility & Enhanced Gist Token Sanitization:** Maintained backward compatibility for reading legacy `GitHubToken=` settings while enhancing Gist upload regex sanitization to scrub both `GitHub=` and `GitHubToken=` payload lines.

---

## [v4.0.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🛡️ Şık Windows Güvenlik Duvarı Hızlı Açılır Menüsü (Windows Firewall State Management & Dropdown UI):** Başlık çubuğundaki kaba/standart buton kaldırılarak yerine uygulamanın şık tasarım diline %100 uyumlu `🛡️ Güvenlik Duvarı ▾` açılır menü butonu eklendi.
- **⚡ 3 Kritik Güvenlik Duvarı Aksiyonu:**
  1. **🛡️ Özellikleri Aç (`wf.msc`):** Windows Defender Gelişmiş Güvenlik konsolunu çalıştırır.
  2. **🟢 Güvenlik Duvarı'nı Aç (`netsh`):** Windows Güvenlik Duvarı'nı tüm profiller için anında **Etkinleştirir (Açar)**.
  3. **🔴 Güvenlik Duvarı'nı Kapat (`netsh`):** Onay uyarısı ile Windows Güvenlik Duvarı'nı tüm profiller için **Devre Dışı Bırakır (Kapatır)**.
- **🌍 7 Dilde Tam Yerelleştirme:** Tüm menü elemanları ve bildirimler 7 dilde desteklenmektedir.

---

### 🇬🇧 English (EN)
- **🛡️ Windows Firewall State Management Dropdown:** Upgraded the single button into a sleek `🛡️ Firewall ▾` dropdown menu matching our custom UI themes.
- **⚡ 3 Firewall Control Actions:**
  1. **🛡️ Open Console (`wf.msc`):** Launches Windows Defender Firewall with Advanced Security.
  2. **🟢 Turn On Firewall (`netsh`):** Instantly enables Windows Firewall across all profiles.
  3. **🔴 Turn Off Firewall (`netsh`):** Disables Windows Firewall across all profiles with a safety confirmation prompt.

---

## [v3.9.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **📺 Tam Ekran Başlatma (`WindowState="Maximized"`) & Genişletilmiş Boyutlar:** İlk yükleyen veya çalıştıran tüm kullanıcıların uygulamayı dar/bozuk görmemesi için pencerevarsayılan olarak **Tam Ekran (Maximized)** başlatılır. Varsayılan pencere boyutları 1380x820px, minimum boyutlar 1100x650px yapıldı.
- **🎨 Duyarlı Üst Başlık Çubuğu Düzeni (Responsive Header WrapPanel):** Başlık çubuğu `DockPanel` yerine `WrapPanel` duyarlı hizalama yapısına geçirildi. Ekran çözünürlüğü veya Windows DPI ölçekleme seviyesi ne olursa olsun butonlar asla çakışmaz veya birbirinin üzerine binmez.

---

### 🇬🇧 English (EN)
- **📺 Maximized Default Window Startup (`WindowState="Maximized"`):** Ensuring first-time users experience a spacious, high-end dashboard. Expanded default fallback window dimensions to 1380x820px with 1100x650px minimum constraints.
- **🎨 Responsive Header Bar Layout:** Replaced rigid docking with a responsive `Grid` + `WrapPanel` header layout to eliminate element overlap regardless of Windows DPI scaling settings.

---

## [v3.8.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **📶 "Adı" ve "Konum" Sütunlarına Tıklayarak Sıralama Desteği (DataGrid Column Sorting):** 2. Profil İçeriği tablosunda "Adı", "Konum", "Gelen" ve "Giden" sütun başlıklarına tıklandığında tablonun artan/azalan (A-Z, Z-A) sıralanması sağlandı.
- **🌿 Net Hiyerarşi & Bağımsız EXE İkonları (Visual Hierarchy Fix):** Klasör ekledikten sonra bağımsız EXE eklendiğinde klasörün altındaymış gibi algılanmaması için visual hiyerarşi ayrıştırıldı: Klasör kökü `📁`, altındaki EXE'ler `↳`, bağımsız doğrudan eklenen EXE'ler `📄` ikonu ile belirginleştirildi.

---

### 🇬🇧 English (EN)
- **📶 DataGrid Column Sorting Support:** Enabled native column sorting (A-Z / Z-A) when clicking on "Name", "Location", "Inbound", or "Outbound" headers in the Profile Content table.
- **🌿 Distinct Hierarchy Indicators:** Added explicit visual tree indicators (`📁` for Folders, `↳` for Folder-scanned EXEs, `📄` for Standalone Root EXEs) to ensure root-level files never appear confused with folder children.

---

## [v3.7.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **⚡ Otomatik Engelleme Kuralları (Auto Firewall Enforcement on Add):** Bir profile dosya (.exe) veya klasör eklendiğinde (Sürükle-Bırak veya Buton ile), Gelen ve Giden bağlantıları engelleme kuralları varsayılan olarak **Windows Güvenlik Duvarı'na anında otomatik uygulanır**. Kullanıcının sağ tık yapmasına gerek kalmadan `wf.msc` konsolunda kurallar anında oluşturulur.
- **🛡️ Windows Güvenlik Duvarı Butonu Sadece İkon (Icon-Only UI Fix):** Başlık çubuğundaki Windows Güvenlik Duvarı açma butonu kullanıcı talebi üzerine sadece kalkan simgesi (`🛡️`) olarak küçültüldü ve buton görünümü şıklaştırıldı.

---

### 🇬🇧 English (EN)
- **⚡ Immediate Firewall Rules Enforcement on Add:** Adding files or folders via drag-and-drop or file dialogs now automatically applies default inbound & outbound block rules in Windows Defender Firewall instantly without requiring manual right-click actions.
- **🛡️ Icon-Only Windows Firewall Button:** Streamlined the header bar button to an elegant icon-only (`🛡️`) format with localized tooltips.

---

## [v3.6.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🎯 Çift Satır (Duplicate Item) Görüntüleme Çözüldü:** Bir klasör altındaki EXE'ye özel sağ tık kuralı uygulandığında ilgili EXE'nin tabloda ikinci kez bağımsız satır olarak çift görünmesi engellendi. Alt EXE'ler ait olduğu klasör altında tek bir girintili satır olarak kusursuz gösterilmektedir.
- **🛡️ Windows Güvenlik Duvarı Hızlı Erişim Butonu Eklendi:** Başlık çubuğuna ("🛡️ Windows Güvenlik Duvarı") butonu eklendi. Tıklandığında Windows Gelişmiş Güvenlik Özellikli Güvenlik Duvarı yönetim konsolunu (`wf.msc`) doğrudan açar. Metin ve ToolTip 7 dil için yerelleştirildi.

---

### 🇬🇧 English (EN)
- **🎯 Duplicate Row Fix:** Prevented child EXE files from creating duplicate top-level DataGrid rows when custom item rules are saved under a folder.
- **🛡️ Direct Windows Firewall Shortcut:** Added a dedicated "🛡️ Windows Firewall" button in the main header bar to directly open the Windows Defender Firewall with Advanced Security Console (`wf.msc`), fully localized across all 7 supported languages.

---

## [v3.5.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🔄 Klasör Sağ Tık Kural Cascading (Hiyerarşik Alt EXE Güncelleme):** Klasöre sağ tıklayıp Gelen veya Giden bağlantısı engellendiğinde/izin verildiğinde, klasörün altındaki tüm .exe dosyalarının engelleme durumları, Windows Güvenlik Duvarı kuralları ve tablodaki durum rozetleri topluca klasörün yeni kuralıyla eşitlenmektedir.

---

### 🇬🇧 English (EN)
- **🔄 Folder Cascading Rule Updates:** Right-clicking a folder to block or allow inbound/outbound connections now automatically cascades the rule update to all contained EXE files, updating their INI properties, Windows Firewall rules, and DataGrid badges.

---

## [v3.4.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🧹 INI Dosyası Temizlendi & Gereksiz Girdiler Kaldırıldı:** Artık her dosya/klasörün kendi özel engelleme kuralı olduğu için kategori düzeyindeki eski `BlockInbound`, `BlockOutbound` ve `IsAllowRule` girdileri `HaYTooL_Firewall.ini` dosyasından tamamen temizlendi.
- **🎯 Profil Aktif / Pasif Aç/Kapat Düzeltildi:** Profile sağ tıklanıp "Seçili Profili Aç / Kapat (Aktif/Pasif)" yapıldığında profil içindeki öğelerin özel engelleme kuralları korunarak Windows Güvenlik Duvarı kuralları toplu halde devre dışı bırakılır veya tekrar etkinleştirilir.

---

### 🇬🇧 English (EN)
- **🧹 INI Cleanup & Unused Keys Removal:** Removed legacy category-level `BlockInbound`, `BlockOutbound`, and `IsAllowRule` settings from `HaYTooL_Firewall.ini` as rules are now strictly per-item.
- **🎯 Profile Active / Passive Toggle Fix:** Toggling a profile active/passive now seamlessly deactivates or restores all individual item rules in Windows Firewall while preserving item-level configurations.

---

## [v3.3.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🎨 Kusursuz Açılır Alt Menü Şablonu (Submenu ControlTemplate & Popup Color Fix):** 4 temanın tamamı (`DarkTheme`, `LightTheme`, `DiscordTheme`, `YouTubeTheme`) için özel `MenuItem` ControlTemplate şablonu yazıldı. Alt menü penceresi (`Popup SubMenuBorder`) aktif temanın kart rengi (`CardBackgroundBrush`) ve kenarlık rengi (`BorderBrush`) ile %100 uyumlu renklendirildi. Alt menülü elemanlarda otomatik sağ ok işareti (`▶`) ve hover renk vurgusu eklendi.

---

### 🇬🇧 English (EN)
- **🎨 Full Submenu ControlTemplate & Popup Color Fix:** Crafted an explicit `MenuItem` ControlTemplate across all 4 themes (`DarkTheme`, `LightTheme`, `DiscordTheme`, `YouTubeTheme`). The popup submenu border is now styled with `{StaticResource CardBackgroundBrush}` and `{StaticResource BorderBrush}`, complete with hover highlights and automatic arrow indicators (`▶`).

---

## [v3.2.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **⚡ Çalıştırma & Başlatma Düzeltmesi (XamlParseException Fix):** XAML ayrıştırmasında çalışma zamanı hatasına (`XamlParseException`) neden olan static fırça referansları temizlendi. Uygulamanın sorunsuz açılması sağlandı.

---

### 🇬🇧 English (EN)
- **⚡ Startup Crash Fix (XamlParseException Fix):** Cleaned up invalid XAML static resource brush references that triggered a runtime `XamlParseException` on startup. The application now launches cleanly.

---

## [v3.1.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🎨 Açılır Alt Menü (Submenu) Yerel Fırça Anahtarları Eklendi:** Tüm tema dosyalarına (`DarkTheme`, `LightTheme`, `DiscordTheme`, `YouTubeTheme`) `MenuItem.SubMenuBackgroundBrushKey` ve `MenuItem.SubMenuBorderBrushKey` eklenerek alt menü açılır pencere (Popup) arkaplanının ve kenarlıklarının aktif temayla tam uyumu sağlandı.

---

### 🇬🇧 English (EN)
- **🎨 Submenu Native Popup Brush Keys:** Added `MenuItem.SubMenuBackgroundBrushKey` and `MenuItem.SubMenuBorderBrushKey` across all 4 themes (`DarkTheme`, `LightTheme`, `DiscordTheme`, `YouTubeTheme`) to guarantee 100% theme alignment for submenu popup panels and borders.

---

## [v3.0.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **📂 Klasör Kural Kontrolü & Rozet Çözümü:** Klasörlerin Gelen ve Giden engelleme/izin verme durumları `⛔ Engellendi` ve `🟢 İzin Verildi` olarak rozetlerde doğru gösterildi ve altındaki tüm EXE'lerin durumu hiyerarşik olarak senkronize edildi.
- **🎨 Temaya Göre Sağ Tık Alt Menü Renkleri:** 4 tema için (`DarkTheme`, `LightTheme`, `DiscordTheme`, `YouTubeTheme`) `SystemColors.MenuBrushKey`, `MenuTextBrushKey`, `HighlightBrushKey` ve `HighlightTextBrushKey` tanımlanarak alt menülerin arkaplan ve yazı renkleri ilgili temayla %100 uyumlu hale getirildi.

---

### 🇬🇧 English (EN)
- **📂 Folder Rule Inspection & Badge Fix:** Fixed folder row status badges to accurately display `⛔ Blocked` and `🟢 Allowed` and hierarchically sync status to all contained EXE files.
- **🎨 Theme-Specific Submenu Styling:** Applied theme resources (`SystemColors.MenuBrushKey`, `MenuTextBrushKey`, `HighlightBrushKey`, `HighlightTextBrushKey`) across all 4 themes so context submenus perfectly inherit active theme colors.

---

## [v2.9.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🛠️ Alt Menü Popup Görünürlük Çözümü (MenuItem ControlTemplate Fix):** Tema dosyalarındaki (`DarkTheme.xaml`, `LightTheme.xaml`, `DiscordTheme.xaml`, `YouTubeTheme.xaml`) varsayılan MenuItem şablonlarında alt menü açılmasını engelleyen hatalı `ControlTemplate` kaldırıldı.
- **🗂️ Açılır Alt Menü Tam Uyum:** "📥 Gelen Bağlantı ▶" ve "📤 Giden Bağlantı ▶" üzerine gelindiğinde `⛔ Engelle` ve `🟢 İzin Ver` seçenekleri tüm temalarda kusursuz olarak açılmakta ve çalışmaktadır.

---

### 🇬🇧 English (EN)
- **🛠️ MenuItem Submenu Popup Template Fix:** Fixed theme files where custom `ControlTemplate` suppressed native WPF MenuItem popup rendering and child item visibility.
- **🗂️ Submenu Full Compatibility:** Hovering "📥 Inbound Connection ▶" and "📤 Outbound Connection ▶" now perfectly opens `⛔ Block` and `🟢 Allow` submenus across all themes.

---

## [v2.8.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🎯 Sağ Tık Anında Satır Odaklaması:** Tablo satırlarına sağ tıklandığında imlecin altındaki satır anında seçili hale getirilerek alt menü kural değişikliklerinin doğru satıra uygulanması garanti edildi (`PreviewMouseRightButtonDown`).
- **🗂️ Açılır Alt Menü İşleyicileri:** Sağ tık menüsü "📥 Gelen Bağlantı ▶" ve "📤 Giden Bağlantı ▶" alt menü eylemleri (`⛔ Engelle` / `🟢 İzin Ver`) doğrudan bağımsız kural kaydedicisine bağlandı.
- **🌐 Dil Desteği Tamamlandı:** Sağ tık menüsündeki tüm başlıklar 7 dil için dinamik yerelleştirildi.

---

### 🇬🇧 English (EN)
- **🎯 Right-Click Instant Row Selection:** Right-clicking any DataGrid row now immediately selects that row via `PreviewMouseRightButtonDown`, ensuring submenus apply rules to the exact row clicked.
- **🗂️ Submenu Rule Handlers:** Fully wired up `⛔ Block` and `🟢 Allow` submenu click events to update per-item firewall rules and INI persistence.
- **🌐 Full i18n:** All menu headers dynamically localized across all 7 supported languages.

---

## [v2.7.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🗂️ Şık Alt Menü Sağ Tık Yapısı:** Sağ tık menüsü "📥 Gelen Bağlantı ▶" ve "📤 Giden Bağlantı ▶" şeklinde açılır alt menülere (Submenu) ayrıldı. Fare ile üzerine gelindiğinde `⛔ Engelle` ve `🟢 İzin Ver` seçenekleri görünür.
- **🔄 Klasör ve EXE Hiyerarşik Senkronizasyonu:** Klasöre uygulanan kural altındaki tüm EXE'lere ve bağımsız EXE'lere anında aktarılır, tablo rozetleri ve INI kayıtları %100 eş zamanlı güncellenir.
- **🌐 Menü Hammadde Metni Düzeltildi:** `"ctxopen location"` hatası düzeltilerek tüm menü başlıkları 7 dile tam olarak bağlandı.

---

### 🇬🇧 English (EN)
- **🗂️ Elegant Submenu Context Layout:** Replaced single context toggles with clean submenus ("📥 Inbound Connection ▶" and "📤 Outbound Connection ▶") featuring hover-activated `⛔ Block` and `🟢 Allow` items.
- **🔄 Folder & EXE Hierarchical Sync:** Rule changes applied to a folder now seamlessly propagate to all contained EXE files with live badge and INI updates.
- **🌐 Context Key Fix:** Fixed the unlocalized `"ctxopen location"` string and localized all menu strings across all 7 languages.

---

## [v2.6.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **💾 Öğe Bazlı Bağımsız INI Kaydı:** Profil içerisindeki her dosya ve klasör için sağ tık ile uygulanan Gelen ve Giden engelleme durumları `HaYTooL_Firewall.ini` dosyasına her öğe için bağımsız olarak kaydedilir ve uygulama açılışında otomatik hatırlanır.
- **🌐 Dinamik Sağ Tık Menü Metinleri:** Sağ tık bağlam menüsünde hammaddesi kalan anahtar metinler ("ctxopen location") düzeltilmiş ve 7 dil için tamamen dinamik yerelleştirilmiş başlıklar entegre edilmiştir.

---

### 🇬🇧 English (EN)
- **💾 Per-Item Independent INI Persistence:** Individual Inbound and Outbound rule settings applied via right-click are now independently stored for every file/folder in `HaYTooL_Firewall.ini`.
- **🌐 Dynamic Context Menu i18n Fix:** Fixed unlocalized key strings in the right-click menu, ensuring proper dynamic localization across all 7 supported languages.

---

## [v2.5.0] - 2026-08-07

### 🇹🇷 Türkçe (TR)
- **🖱️ Sağ Tık Bağlantı Yönetimi:** Tablodaki her öğeye (EXE veya Klasör) sağ tıklandığında anlık olarak Gelen (`Inbound`) ve Giden (`Outbound`) kuralını değiştiren dinamik menü eklendi (`⛔ Engelle` / `🟢 İzin Ver`).
- **✨ Panel 2 Sadeleştirmesi:** Eski toplu kural uygulama butonları ve karmaşık onay kutuları ("2. Profil İçeriği") altından kaldırılarak tablo alanı maksimum yüksekliğe genişletildi.
- **🌐 7 Dil Desteği:** Sağ tık bağlam menüsü seçenekleri 7 dil için tam uyumlu hale getirildi.

---

### 🇬🇧 English (EN)
- **🖱️ Right-Click Connection Toggle:** Added interactive context menu items to toggle Inbound and Outbound rules directly per item (`⛔ Block` / `🟢 Allow`).
- **✨ Panel 2 UI Cleanup:** Removed legacy bulk option panels and buttons under "2. Profile Content", allowing the DataGrid table to expand full-height.
- **🌐 7 Languages Support:** Fully localized context menu items across all 7 supported languages.

---

## [v2.4.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
- **📊 5 Sütunlu Gelişmiş İçerik Tablosu (2. Profil İçeriği):** "2. Profil İçeriği" bölümü standart TreeView görünümünden 5 sütunlu modern, dinamik DataGrid tablosuna dönüştürüldü.
- **🖼️ Sütun Yapısı:**
  1. **Simge:** Uygulama veya klasörün orijinal Windows kabuk simgesi.
  2. **Adı:** Dosya/klasör ismi (Klasör altındaki EXE'ler hiyerarşik girintili gösterilir).
  3. **Konum:** Dosya veya klasörün tam disk yolu.
  4. **Gelen:** Windows Güvenlik Duvarı Gelen bağlantı kural durumu (`⛔ Engellendi` / `🟢 İzin Verildi`).
  5. **Giden:** Windows Güvenlik Duvarı Giden bağlantı kural durumu (`⛔ Engellendi` / `🟢 İzin Verildi`).
- **🌐 7 Dil Tablo Başlıkları:** Tablo sütun başlıkları ve kural durum amblemleri 7 dil için (`TR`, `EN`, `ES`, `DE`, `PT`, `AR`, `RU`) dinamik hale getirildi.

---

### 🇬🇧 English (EN)
- **📊 5-Column Advanced Content Table (2. Profile Content):** Converted "2. Profile Content" section into a modern, 5-column DataGrid table.
- **🖼️ Column Layout:**
  1. **Icon:** Original Windows shell icon for files and folders.
  2. **Name:** Executable or folder name (Child EXEs displayed with hierarchical indentation).
  3. **Path:** Full disk path.
  4. **Inbound:** Live Windows Firewall Inbound rule status (`⛔ Blocked` / `🟢 Allowed`).
  5. **Outbound:** Live Windows Firewall Outbound rule status (`⛔ Blocked` / `🟢 Allowed`).
- **🌐 7 Languages Support:** Fully localized column headers and status badges across all 7 supported languages.

---

## [v2.3.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
- **🚀 Canlı Güncelleme Test Sürümü:** Çevrim içi otomatik güncelleme mekanizmasının test edilmesi amacıyla yayınlanan resmi v2.3.0 sürümü.
- **🔔 Sessiz Güncelleme Bildirimi:** Eski sürümleri kullanan tüm kullanıcılara otomatik kırmızı nokta amblemi ve indirme ipucu sağlar.

---

### 🇬🇧 English (EN)
- **🚀 Live Update Test Release:** Official release v2.3.0 pushed for testing the automated online update notification system.
- **🔔 Silent Update Badge:** Provides a silent red notification dot and download tooltip for users running older versions.

---

## [v2.2.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
- **🔔 Sessiz & Rahatsız Etmeyen Çevrim İçi Güncelleme Kontrolü:** Uygulama açıldığında arka planda GitHub üzerinden yeni bir sürüm olup olmadığı sessizce kontrol edilir.
- **🔴 Dinamik Kırmızı Amblem & İpucu (ToolTip):** Yeni bir sürüm yayınlandığında sürüm rozetinde kırmızı bildirim noktası belirir ve fare ile üzerine gelindiğinde `🎉 Yeni Sürüm Mevcut! (vX.X.X) - İndirmek İçin Tıklayın` ipucu gösterilir. Rozete tıklandığında doğrudan GitHub Releases indirme sayfasına yönlendirilir.
- **🌐 7 Dil Entegrasyonu:** Güncelleme ipucu metinleri tüm 7 desteklenen dilde (`TR`, `EN`, `ES`, `DE`, `PT`, `AR`, `RU`) yerelleştirildi.

---

### 🇬🇧 English (EN)
- **🔔 Non-intrusive Silent Online Update Checker:** Automatically and silently checks GitHub for newer releases in the background upon application startup without annoying popups.
- **🔴 Dynamic Red Badge & ToolTip Indicator:** When a new version is detected, a subtle red indicator dot lights up on the version badge, and the ToolTip prompts `🎉 New Version Available! (vX.X.X) - Click to Download`. Clicking the badge opens the GitHub Releases download page directly.
- **🌐 7 Languages Localization:** Fully localized update notifications across all 7 supported languages.

---

## [v2.1.0] - 2026-08-06

### 🇹🇷 Türkçe (TR)
- **📖 README Dil Sıralaması:** `README.md` dosyasında İngilizce (EN) bölümü ilk sıraya, Türkçe (TR) bölümü ikinci sıraya alındı.
- **⚡ Derleme Scripti (build.ps1) İyileştirmesi:** `0nogithub/build.ps1` scriptine derleme öncesinde çalışan `HaYTooL_Firewall.exe` sürecini kesin olarak kapatma ve derleme başarıyla tamamlandıktan sonra üretilen yeni EXE'yi otomatik başlatma mantığı eklendi.
- **📜 Proje Anayasası (clinerules.md) Güncellemesi:** Anayasamızın 5. maddesine %100 yerel çalışma prensibi ve GitHub push yasağı koyu ve net kurallarla işlendi. AI, kullanıcının o anki talimatında açıkça "github'a pushla / yükle" ifadesi olmadığı sürece asla push betiklerini çalıştırmayacaktır.

---

### 🇬🇧 English (EN)
- **📖 README Language Order:** Updated `README.md` to place English (EN) section first and Turkish (TR) section second.
- **⚡ Build Script (build.ps1) Enhancement:** Enhanced `0nogithub/build.ps1` to stop any running `HaYTooL_Firewall.exe` process before compiling and automatically launch the newly compiled EXE after build succeeds.
- **📜 Constitution (clinerules.md) Update:** Reinforced Section 5 with a strict local-only policy. The AI will never execute push scripts unless the user explicitly provides a standalone push command.

---

## [v2.0.9] - 2026-08-06

### 🇹🇷 Türkçe (TR)
- **📖 Detaylı README Güncellemesi:** `README.md` dosyasına uygulamanın amacı, tüm yetenekleri ve özellikleri (özyinelemeli tarama, profiller, FullSafe modu, yedekleme, i18n, temalar) detaylı Türkçe ve İngilizce kılavuz olarak eklendi.
- **🎨 Açık Tema Buton Renk Uyumlaştırması:** `💾 Yedekler`, `☁️ Gist`, `🔄 Listeyi Yenile`, `🔄 Senkronize Et` ve `+ Klasör` butonlarının arka plan ve metin renkleri `+ EXE` butonu ile tamamen eşitlendi (gri arka plan, kırmızı `DangerBrush` metin).
- **🎨 Açık Temada Beyaz Yazı Stili:** `❓ (FullSafe Yardım)` ve `− Çıkar` butonlarının metin renkleri saf beyaz (`#FFFFFF`) olarak güncellendi.
- **🌐 ToolTip i18n Düzeltmesi:** Tema seçici ComboBox (`cmbThemeSelector`), FullSafe yardım butonu (`btnFullSafeHelp`) ve Sürüm rozeti (`bdVersion`) ipuçları 7 dil için dinamik hale getirildi.

---

### 🇬🇧 English (EN)
- **📖 Comprehensive README Update:** Updated `README.md` with detailed sections on application purpose and full capabilities in both Turkish and English.
- **🎨 Light Theme Button Color Harmonization:** Harmonized `Backups`, `Gist`, `Refresh List`, `Sync Folders`, and `+ Folder` buttons to match `+ EXE` button colors (secondary grey background with red `DangerBrush` text).
- **🎨 Light Theme White Text:** Updated `❓ (FullSafe Help)` and `− Remove` buttons to use pure white (`#FFFFFF`) text.
- **🌐 ToolTip i18n Fix:** Made theme selector, FullSafe help, and version badge ToolTips dynamically localized across all 7 supported languages.

---

## [v2.0.8] - 2026-08-06

### 🇹🇷 Türkçe (TR)
- **🎨 Buton Stil Güncellemesi:** `+ EXE` butonunun metin rengi dikkat çekici kırmızı (`DangerBrush`) olarak güncellendi.

---

### 🇬🇧 English (EN)
- **🎨 Button Style Update:** Changed text color of `+ EXE` button to eye-catching red (`DangerBrush`).

---

## [v2.0.7] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🎨 Tema Sistemi Komple Yenileme — Tüm Pencereler
- **🔑 Yeni Renk Katmanı Sistemi:** 4 tema dosyasına (`Dark`, `Light`, `Discord`, `YouTube`) `PanelBackgroundBrush`, `InnerSectionBrush`, `TreeViewBackgroundBrush`, `WindowBackgroundBrush`, `AlternatingRowBrush` adında 5 yeni renk token'ı eklendi.
- **🎨 Panel Derinliği Düzeltildi:** Profiller (Panel 1) ve Profil İçeriği (Panel 2) artık Header'dan farklı bir arka plan rengine sahip; görsel derinlik ve hiyerarşi oluşturuldu.
- **🌳 TreeView Arkaplanı:** Her temaya özel `TreeViewBackgroundBrush` ile ağaç görünümü temalara uyumlu hale getirildi.
- **📦 Engelleme Seçenekleri:** `BackgroundBrush` yerine `InnerSectionBrush` kullanılarak tüm temalarda okunabilir kontrast sağlandı (YouTube'da siyah üstü siyah sorunu çözüldü).
- **💾 Yedekler Penceresi:** `BackupWindow.xaml` dış arka planı `WindowBackgroundBrush`'a bağlandı.
- **☁️ Gist Penceresi:** `GistWindow.xaml` dış arka planı `WindowBackgroundBrush`'a bağlandı.
- **🐛 Kritik Bug Düzeltmesi:** `InputDialog.xaml`'da kullanılan `WindowBackgroundBrush` token'ı hiçbir tema dosyasında tanımlı değildi; artık tüm 4 temaya eklendi — Profil yeniden adlandırma penceresi her temada doğru rengi alacak.
- **📊 DataGrid Satır Renkleri:** `AlternatingRowBackground` hardcoded değer yerine `AlternatingRowBrush` token'ına bağlandı; tüm temalarda doğru renk gösterilecek.

---

### 🇬🇧 English (EN)
#### 🎨 Full Theme System Overhaul — All Windows
- **5 new color depth tokens** added to all 4 themes: `PanelBackgroundBrush`, `InnerSectionBrush`, `TreeViewBackgroundBrush`, `WindowBackgroundBrush`, `AlternatingRowBrush`.
- **Panel depth fixed**: Profiles (Panel 1) and Content (Panel 2) now have distinct backgrounds from the header.
- **TreeView background** is now theme-aware with `TreeViewBackgroundBrush`.
- **Block Options border**: Switched to `InnerSectionBrush` — visibility issue fixed on YouTube theme.
- **BackupWindow & GistWindow**: Outer backgrounds bound to `WindowBackgroundBrush`.
- **Critical bug fix**: `InputDialog.xaml` used undefined `WindowBackgroundBrush` — now properly defined in all 4 themes.
- **DataGrid alternating rows**: Bound to `AlternatingRowBrush` token for theme-accurate display.

---

## [v2.0.6] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🔑 Merkezi Sürüm Yönetimi (Single Source of Truth) & Dinamik Scriptler
- **🔑 Root `VERSION` Dosyası Entegrasyonu:** Proje sürüm numarası kök dizindeki `VERSION` dosyasına bağlandı.
- **⚡ Otomatik Sürüm Gömmeli Single-File Derleme:** `build.ps1` ve `build_release_zip.ps1` scriptleri versiyonu `VERSION` dosyasından okuyup `.exe` içine gömecek şekilde dinamikleştirildi.
- **💾 Versiyonlu Arşiv Yedeği:** `backup.ps1` scripti oluşturulan 7z yedek arşiv adına versiyon numarasını otomatik ekleyecek şekilde güncellendi (`HaYTooL_Yedek_v2.0.6_...7z`).

---

### 🇬🇧 English (EN)
#### 🔑 Single Source of Truth Versioning System
- **🔑 Root `VERSION` File Integration:** Version management centralized into root `VERSION` file.
- **⚡ Dynamic Single-File EXE Embedding:** `build.ps1` and `build_release_zip.ps1` dynamically embed version into output EXE.
- **💾 Versioned Backups:** `backup.ps1` automatically appends version number to generated 7z backup archives.

---

## [v2.0.5] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🖼️ Güncel Ekran Görüntüsü Entegrasyonu & Dinamik Çeviri İyileştirmeleri
- **🖼️ README Ekran Görüntüsü Güncellendi:** `0nogithub/screenshot.png` dosyası Git takibindeki `Resources/screenshot.png` konumuna aktarıldı ve README'deki görsel yenilendi.
- **🌐 Canlı Dil Çevirisi Dinamikleştirildi:** `Yeni Profil Adı...` placeholder ve Gist durum butonlarının dil değişimlerinde anlık güncellenmesi sağlandı.

---

### 🇬🇧 English (EN)
#### 🖼️ Updated Screenshot & Dynamic Localization Enhancements
- **🖼️ README Screenshot Refreshed:** Updated `Resources/screenshot.png` with the latest UI preview.
- **🌐 Dynamic i18n Translation:** Placeholder and Gist status button texts now update dynamically when switching languages.

---

## [v2.0.4] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🖼️ Canlı Uygulama Ekran Görüntüsü & Yayın Temizliği
- **🖼️ README Önizleme Görseli:** `0nogithub/s-s.png` ekran görüntüsü Git takibindeki `Resources/screenshot.png` konumuna taşındı ve `README.md` dokümanına büyük önizleme kartı olarak yerleştirildi.
- **🚀 Otomatik Push & Release:** GitHub `main` dalı, `v2.0.4` etiketi ve son kullanıcı için hazırlanmış tek dosya Portable ZIP paketi güncellendi.

---

### 🇬🇧 English (EN)
#### 🖼️ Application Screenshot Preview & Clean Release
- **🖼️ README Live Preview:** Added full-width application screenshot (`Resources/screenshot.png`) to `README.md`.
- **🚀 Automated Push & Asset Release:** Pushed code to GitHub main branch and published clean `v2.0.4` release ZIP.

---

## [v2.0.3] - 2026-08-06

### 🇹🇷 Türkçe (TR)
#### 🚀 Görsel İyileştirmeler & Doğrudan İndirme Linkleri
- **🖼️ Resmi Amblem/Logo Entegrasyonu:** `README.md` üst kısmına uygulamanın resmi logosu (`Resources/firewall.png`) eklendi.
- **🔗 Doğrudan En Son Sürüm İndirme Linkleri:** `README.md` başlığındaki indirme ve versiyon rozetleri doğrudan en son çıkan sürüme ([latest release](https://github.com/HaYToKoRaZ/HaYTooL-Firewall/releases/latest)) yönlendirildi.

---

### 🇬🇧 English (EN)
#### 🚀 Visual Branding & Direct Latest Release Links
- **🖼️ Official Application Logo:** Embedded official application logo in `README.md`.
- **🔗 Direct Download Links:** Badge links now redirect users straight to the latest GitHub Release download page.

---

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
