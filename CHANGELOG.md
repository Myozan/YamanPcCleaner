# Sürüm Notları

Bu dosya 1.3.0'dan itibaren tutulur. Daha eski sürümler için
[GitHub Releases](https://github.com/Myozan/YamanPcCleaner/releases) sayfasına bakabilirsin.

## 1.3.0 — 11 Kasım 2026

### Arayüz ve kimlik

- "Sistem Bakım Merkezi" kimliği yenilendi: gradyan yan panel, rafine başlık, kartlar ve düğmeler.
- Hakkında penceresi baştan tasarlandı: sürüm rozeti, güncelleme kartı, açık kaynak bağlantıları ve özellik özeti tek bakışta.
- Koyu tema tutarlılaştırıldı; düğmeler, anahtarlar ve sekmeler aynı görsel dili kullanıyor.
- Koyu mod uygulama genelinde zorlandı; yerel liste ve panel kaydırma çubukları da koyu temaya uydu.
- Yan panel bölümleri yeniden adlandırıldı: Sistem Bakımı, Yönetim Araçları, Kişisel, Program.
- Alt bilgi sürüm etiketi ve pencere başlığı marka sabitlerinden besleniyor.

### Kaynak kodu

- 3.970 satırlık tek dosya, sınıf başına ayrı dosyalara bölündü.
- Yeni düzen: `src/Program.cs`, `src/Cekirdek/` (motor) ve `src/Arayuz/` (arayüz).
- Ürün adı, sürüm, site ve depo bilgileri `src/Cekirdek/Marka.cs` içinde toplandı.
- `derle.cmd` artık `src` altındaki bütün `.cs` dosyalarını otomatik topluyor; yeni dosya eklerken betik değişmiyor.
- Yayınlama, imzalama ve deneme derleme betikleri yeni dosya yollarına uyarlandı.

### Belgeler

- README yeni proje yapısı, sürüm ve yayınlama adımlarıyla güncellendi.
- Tanıtım sayfası (GitHub Pages) modern bir açılış sayfası olarak yenilendi.
- Bu sürüm notları dosyası eklendi.

### Düzeltmeler

- Açılış programları ve korunan süreç listelerindeki yinelenen kayıtlar temizlendi.
- Kişisel Bakım'daki işletim sistemi adı Windows 10/11 olarak düzeltildi.
- Arayüz otomasyon testindeki değişken çakışması giderildi.
- Çeşitli metin, kararlılık ve tutarlılık düzeltmeleri.
