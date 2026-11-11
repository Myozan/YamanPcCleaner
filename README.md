<div align="center">

# Yaman PC Cleaner

### Windows için sade, şeffaf ve kullanıcı kontrolünde sistem bakımı.

RAM ve disk kullanımını izle, gereksiz dosyaları analiz et, Windows bakım araçlarına tek merkezden ulaş. Ne değişeceğine sen karar ver.

[![GitHub stars](https://img.shields.io/github/stars/Myozan/YamanPcCleaner?style=for-the-badge&logo=github&label=Stars)](https://github.com/Myozan/YamanPcCleaner/stargazers)
[![Latest release](https://img.shields.io/github/v/release/Myozan/YamanPcCleaner?style=for-the-badge&logo=github&label=Release)](https://github.com/Myozan/YamanPcCleaner/releases/latest)
[![License](https://img.shields.io/github/license/Myozan/YamanPcCleaner?style=for-the-badge)](LICENSE)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows)](https://github.com/Myozan/YamanPcCleaner)
[![Build](https://img.shields.io/github/actions/workflow/status/Myozan/YamanPcCleaner/dotnet.yml?branch=main&style=for-the-badge&label=Build)](https://github.com/Myozan/YamanPcCleaner/actions/workflows/dotnet.yml)

[🌐 Tanıtım sitesi](https://myozan.github.io/YamanPcCleaner/) · [⬇️ Son sürüm](https://github.com/Myozan/YamanPcCleaner/releases/latest) · [🧾 Sürüm notları](CHANGELOG.md) · [🇬🇧 English](README.en.md)

</div>

---

<p align="center">
  <a href="https://myozan.github.io/YamanPcCleaner/">
    <img src="kaynaklar/onizleme-10.png" alt="Yaman PC Cleaner masaüstü uygulaması – ana ekran" width="900">
  </a>
</p>

## Neden Yaman PC Cleaner?

Windows bakım araçları çoğu zaman dağınık, teknik ve neyi değiştirdiği belirsizdir. Yaman PC Cleaner; sistem durumunu anlaşılır biçimde sunmayı, bakım seçeneklerini tek yerde toplamayı ve önemli değişikliklerde kontrolü kullanıcıda bırakmayı amaçlayan ücretsiz, açık kaynaklı bir Windows masaüstü uygulamasıdır.

**Temel yaklaşım:** Önce göster. Ne yapılacağını açıkla. Önemli değişikliklerde kullanıcı onayı al.

## Öne çıkan özellikler

| Alan | Neler sunar? |
|---|---|
| **Sistem özeti** | Sistem puanı, sanal bellek, disk alanı/sağlığı, pil ve başlangıç yükü hakkında görünüm |
| **Performans analizi** | CPU, GPU, RAM, VRAM ve sıcaklıkların canlı izlenmesi; yavaşlık nedenlerine yönelik açıklamalar |
| **Disk temizliği** | Geçici dosyalar, önbellekler ve Windows kalıntıları için analiz ve temizleme seçenekleri |
| **Bellek araçları** | RAM kullanımı, bellek bekçisi ve sanal bellek yapılandırma seçenekleri |
| **Windows bakımı** | Geri alınabilir sistem ayarları, DNS seçimi ve ağ testi |
| **Oyun profili** | Güç modu ve oyun odaklı ayarları yönetme seçenekleri |
| **Sürücü merkezi** | NVIDIA sürüm kontrolü, imza kontrolü ve kurulum öncesi geri yükleme noktası |
| **Bakım raporu** | Yerel işlem günlüğü, bakım özeti ve HTML raporu |

<p align="center">
  <img src="kaynaklar/onizleme-59.png" alt="Yaman PC Cleaner sistem araçları" width="49%">
  <img src="kaynaklar/onizleme-255.png" alt="Yaman PC Cleaner bakım araçları" width="49%">
</p>

## İndir ve çalıştır

<div align="center">

### En güncel sürümü GitHub Releases üzerinden al

[![Download](https://img.shields.io/badge/İNDİR-Windows%20EXE-2563EB?style=for-the-badge&logo=windows)](https://github.com/Myozan/YamanPcCleaner/releases/latest)

</div>

1. [Son sürüm sayfasını aç](https://github.com/Myozan/YamanPcCleaner/releases/latest).
2. Sürüm varlıkları arasından `YamanPcCleaner.exe` dosyasını indir.
3. Dosyayı bir klasöre kaydet ve çalıştır. Kurulum sihirbazı gerekmez.

**Gereksinimler:** Windows 10 veya Windows 11. Uygulama normal kullanımda yönetici izni istemez; sistem genelinde değişiklik gerektiren belirli işlemlerde Windows izin sorabilir.

> **Windows SmartScreen notu:** Uygulama şu anda ücretli Authenticode kod imzalama sertifikasıyla imzalanmış değildir. Bu nedenle Windows bilinmeyen yayıncı uyarısı gösterebilir. İndirmeden önce kaynak kodunu ve sürüm dosyalarını inceleyebilirsin. Güncelleme imza doğrulaması, uygulamanın kendi güncelleme mekanizmasına ait ayrı bir kontroldür.

## Güvenlik ve gizlilik

- **Açık kaynak:** Kaynak kodu bu depoda incelenebilir ve lisans koşulları altında derlenebilir.
- **Kullanıcı kontrolü:** Sistem ayarları, sürücü kurulumu ve temizleme işlemleri kullanıcı tarafından başlatılır.
- **Geri alınabilir ayarlar:** Desteklenen Windows, DNS, Turbo ve sanal bellek değişiklikleri öncesinde eski değerler yedeklenir.
- **Telemetri yok:** Uygulama kişisel kullanım telemetrisi toplamak üzere tasarlanmamıştır.
- **İmzalı uygulama güncellemeleri:** Uygulama güncelleme paketinin imzasını doğrulamadan kuruluma geçmez. Bu mekanizma Windows Authenticode imzası ile aynı şey değildir.

Güvenlik açığı bildirmek için lütfen herkese açık bir issue açmak yerine [SECURITY.md](.github/SECURITY.md) yönergelerini kullan.

## Derleme

Proje klasik Windows .NET Framework 4.x C# derleyicisini kullanır. Derleme Windows üzerinde yapılmalıdır.

```bat
derle.cmd
```

Başarılı derleme sonunda depo kökünde `YamanPcCleaner.exe` oluşur. Betik `src\` altındaki C# dosyalarını otomatik toplar.

### Proje yapısı

```text
src/
  Program.cs
  Cekirdek/       Uygulama motoru, sistem ölçümü, bakım ve güncelleme
  Arayuz/         WinForms pencereleri, sekmeler ve görsel tema
araclar/          Derleme, test, imzalama ve yayınlama betikleri
kaynaklar/        Uygulama simgeleri ve görseller
docs/             GitHub Pages tanıtım sitesi
.github/
  workflows/      CI, Pages ve Release otomasyonları
```

## Sürümler ve yayınlama

- [Tüm sürümler](https://github.com/Myozan/YamanPcCleaner/releases)
- [Değişiklik günlüğü](CHANGELOG.md)
- [GitHub Actions iş akışları](https://github.com/Myozan/YamanPcCleaner/actions)

Yeni sürümler `src/Cekirdek/SurumBilgisi.cs` dosyasındaki sürüm bilgisiyle eşleştirilir. Release otomasyonu, gerekli özel imzalama anahtarı tanımlı değilse imzasız güncelleme yayınlamaz. Gizli imzalama anahtarı depoya eklenmemelidir.

## Yol haritası

Geliştirme öncelikleri ve öneriler [GitHub Issues](https://github.com/Myozan/YamanPcCleaner/issues) üzerinden takip edilir. Yeni fikir, hata bildirimi veya iyileştirme önerisi açabilirsin.

## Katkıda bulun

Kod, test, dokümantasyon, çeviri ve hata raporları değerlidir. Başlamadan önce [CONTRIBUTING.md](.github/CONTRIBUTING.md) dosyasını oku. Davranış standartlarımız için [Code of Conduct](.github/CODE_OF_CONDUCT.md) dosyasına bak.

## Lisans ve teşekkür

Yaman PC Cleaner [MIT Lisansı](LICENSE) ile yayımlanır.

Windows bakım seçeneklerinin bir bölümü [Raphire/Win11Debloat](https://github.com/Raphire/Win11Debloat) projesinden esinlenmiştir; ilgili lisans ve atıflar korunur.

---

<div align="center">

### Proje işine yaradı mı?

⭐ **GitHub'da yıldız vererek projeyi destekleyebilirsin.** Yıldız, projeyi takip etmeyi ve başkalarının keşfetmesini kolaylaştırır.

[![Star on GitHub](https://img.shields.io/badge/⭐%20Star%20Yaman%20PC%20Cleaner-GitHub-181717?style=for-the-badge&logo=github)](https://github.com/Myozan/YamanPcCleaner)

**Yaman Software** · Technology · Innovation · Software

</div>
