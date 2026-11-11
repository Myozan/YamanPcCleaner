# Yaman PC Cleaner

**Ücretsiz ve açık kaynak PC temizleme ve hızlandırma programı.** Windows 10 / 11 için sade, koyu temalı, Türkçe bir sistem izleyici ve temizleyici: RAM'i boşaltır (RAM temizleyici), gereksiz dosyaları temizler, "bellek yetersiz" hatalarının sebebini bulur ve oyunlarda takılmayı azaltıp FPS'i artıran ayarlarla bilgisayarını oyun için hazırlar.

Tanıtım sayfası: https://myozan.github.io/YamanPcCleaner/

<p align="center">
  <img src="kaynaklar/logo-256.png" width="104" alt="Yaman PC Cleaner logo">
</p>

<p align="center">
  <img src="kaynaklar/onizleme-10.png" width="32%" alt="Yaman PC Cleaner ekran görüntüsü 1">
  <img src="kaynaklar/onizleme-59.png" width="32%" alt="Yaman PC Cleaner ekran görüntüsü 2">
  <img src="kaynaklar/onizleme-255.png" width="32%" alt="Yaman PC Cleaner ekran görüntüsü 3">
</p>

**[En son sürümü indir](https://github.com/Myozan/YamanPcCleaner/releases/latest)** · Windows 10 / 11 · Kurulum gerektirmez (tek exe) · Türkçe · Ücretsiz ve açık kaynak ([MIT](LICENSE)) · [Sürüm notları](CHANGELOG.md)

## Neler yapabilir

- **Genel Bakış** — Açılınca bilgisayarının 100 üzerinden puanını gösterir: sanal bellek, boş yer ve gereksiz dosyalar, disk sağlığı (istersen ömrü ve sıcaklığı), dizüstünde pil sağlığı, açılışta başlayan programlar, oyun ayarları, NVIDIA sürücüsü ve son 7 günün çökmeleri. Her önerinin yanında onu düzelteceğin yere götüren bir düğme vardır; durum panosu yalnızca okur, hiçbir şeyi kendiliğinden değiştirmez. Sistem kimliğini (donanım özeti) tek tıkla kopyalar, bugüne kadar ne kadar yer açıldığını gösterir.
- **Sistemi Optimize Et** — Tek tıkla: Sistem sekmesinde işaretli arka plan programlarını kapatır, RAM'i boşaltır, geçici dosyaları siler. Ne yapacağını önce gösterir, sonunda ne kazandığını sayarak söyler.
- **Neden yavaş?** — Birkaç saniye ölçüp o an bilgisayarı neyin yavaşlattığını sade bir dille söyler: işlemciyi ya da diski meşgul eden program (Windows güncellemesi, Defender taraması gibi Windows işleri dahil), dolan bellek, ısınma, pilde çalışma, güç modu.
- **Sistem** — İşlemci, ekran kartı, RAM, ekran kartı belleği, sanal bellek ve ısıyı son 2 dakikanın grafiğiyle canlı gösterir. Altta kapatılabilir programları (işlemci, bellek, ekran kartı kullanımıyla) listeler; seçtiklerini kapatır ve RAM'i boşaltır. Sistem parçaları, sürücüler, antivirüs ve benzeri programlar listelenmez.
- **Disk** — Geçici dosyaları, önbellekleri ve Windows kalıntılarını temizler (normal ve derin temizlik). Depolama analizi hangi klasörün ne kadar yer tuttuğunu gösterir.
- **Sürücü** — NVIDIA ekran kartı sürücüsünün yeni sürümünü denetler; istersen indirir, imzasını kontrol eder ve geri yükleme noktası oluşturarak kurar.
- **Sağlık** — Bellek bekçisinin durumu, sürekli büyüyen (sızıntı şüphesi olan) programlar, son 7 günün çökme geçmişi ve masaüstüne kaydedilen destek raporu.
- **Başlangıç** — Windows ile birlikte açılan programları ve açılışta çalışan zamanlanmış görevleri (güncelleyiciler gibi) listeler; açıp kapatmanı sağlar (programlarda Görev Yöneticisi ile aynı ayar).
- **Windows** — Geri alınabilir Windows ayarları, hazır uygulamaları kaldırma, DNS seçimi ve ağ bağlantı testi, Wi-Fi güç tasarrufu, güç modu ve güç planı.
- **Turbo** — Oyun ayarları (işlemci, güç, ekran kartı, bellek ve disk), oyun açıkken güç modunu yükselten oyun profili ve donanım kontrolü.
- **Kişisel Bakım** — Yaman'a özel karşılama, haftalık bakım planı ve hatırlatma; sistem puanı, toplam kazanç ve son bakım özeti; yerel işlem günlüğü; tek tıkla geri yükleme noktası ve masaüstüne kaydedilen HTML bakım raporu. Ad ve günlük yalnızca bu bilgisayarda tutulur.
- **Ayarlar** — Bütün ayarlar tek sayfada, bölüm bölüm: vurgu rengi, tepsi simgesinde ne yazacağı, hareketli geçişler, açılış sekmesi; arka planda çalışma, Windows ile başlama, oyun modu; genel kısayol (varsayılan Ctrl+Alt+O ile RAM boşaltma) ve bildirim sesi; sistem optimizasyonu varsayılanları; güncelleme denetimi; toplam kazancı sıfırlama ve ayar klasörü. Değişiklik hemen uygulanır.
- **Sanal bellek ayarı** — Bilgisayarının RAM'ine göre önerilen boyutu uygular; önceki ayara dönebilirsin.
- **Otomatik / Full optimize** — Tek tıkla temizlik ve hızlandırma. Otomatik optimize sormadan yalnızca risksiz işleri yapar; Full optimize seçenekleri sana bırakır.
- **Tepside bekçi** — Pencereyi kapatınca saatin yanında çalışmaya devam eder; simgede RAM doluluğu yazar, üstüne gelince küçük bir özet açılır; sanal bellek ya da RAM dolarsa haber verir.
- **Otomatik güncelleme** — Yeni sürümü GitHub Releases üzerinden bulur, imzasını doğrular ve sen onaylayınca kurar. Elle denetlemek için sol alttaki **Hakkında ve güncelleme**'ye tıkla → **Şimdi denetle**, ya da saatin yanındaki simge → **Güncellemeleri denetle**.

## İndirme

1. [Releases](https://github.com/Myozan/YamanPcCleaner/releases) sayfasından en son sürümün **YamanPcCleaner.exe** dosyasını indir.
2. Masaüstü ya da Belgeler gibi bir klasöre koy ve çalıştır. Kurulum yok; tek bir exe.

Windows 10 / 11 gerekir. İlk açılışta Windows SmartScreen "bilinmeyen yayıncı" uyarısı gösterebilir: **Ek bilgi → Yine de çalıştır**. (Program ücretli bir kod imzası sertifikası kullanmıyor; kaynak kodu açık ve güncellemeler aşağıda anlatıldığı gibi ayrıca imzalanıyor.)

Bazı işler (sanal bellek ayarı, Windows kalıntılarını temizleme, sürücü kurulumu, makine geneli Windows ve ağ ayarları, DNS değiştirme) Windows'ta yönetici izni ister; yalnızca o iş için ve o an Windows'un izin penceresiyle sorulur, program normalde yönetici olmadan çalışır.

## Güvenli mi?

- **Açık kaynak.** Bütün kod bu depoda; ne yaptığını okuyabilir, kendin derleyebilirsin.
- **Kendiliğinden önemli bir şey yapmaz.** Program kapatma, disk temizliği, Windows ayarları, sanal bellek, DNS ve sürücü kurulumu yalnızca sen ilgili düğmeye basınca çalışır (silme ve optimize işleri önce uyarı penceresiyle sorar). Genel Bakış ve "Neden yavaş?" yalnızca okur. Sorusuz çalışan tek şey RAM boşaltma ile "Otomatik optimize"dir; bunlar program kapatmaz, ayar değiştirmez, yalnızca RAM'i boşaltır ve yönetici izni gerektirmeyen geçici dosya/önbellek temizliği yapar. Turbo'daki "Oyun profili"ni açarsan (varsayılan kapalı) oyun süresince Windows güç modunu "En iyi performans" yapar ve oyun bitince eski hâline döndürür.
- **Değişiklikler geri alınabilir.** Windows, Turbo, DNS ve sanal bellek ayarlarını değiştirmeden önce eski değerler `%APPDATA%\YamanPcCleaner` klasörüne yedeklenir; ilgili sekmelerden eski hâle dönebilirsin. NVIDIA sürücüsü kurulmadan önce geri yükleme noktası oluşturulur.
- **İnternete az çıkar.** Yalnızca: güncelleme denetimi (GitHub), NVIDIA sürücü kontrolü ve indirmesi (NVIDIA'nın sitesi) ve ağ bağlantı testi (ağ geçidi, 1.1.1.1 ve 8.8.8.8'e ping). Güncelleme denetimini Hakkında penceresinden kapatabilirsin.
- **Kişisel veri toplamaz, göndermez.** Telemetri yok. Destek raporu yalnızca masaüstüne bir dosya olarak yazılır; kimseye gönderilmez, kullanıcı ve bilgisayar adı raporda gizlenir.
- **Güncellemeler imzalı.** Aşağıya bak.

## Otomatik güncelleme ve imza doğrulaması

Program en çok günde bir kez GitHub'daki son sürüme bakar (Hakkında penceresinden kapatılabilir). Yeni sürüm varsa sana sorar; sen "Güncelle" dersen dosyayı program içinde indirir, tarayıcı açılmaz.

Her sürümün `YamanPcCleaner.exe` dosyası yazarın **gizli anahtarıyla imzalanır** (`YamanPcCleaner.exe.imza`, RSA-3072 / SHA-256). Programın içinde yalnızca karşılığı olan **açık anahtar** gömülüdür. İndirilen dosyanın imzası tutmazsa güncelleme kurulmaz; ayrıca sürüm numarası etiketle uyuşmuyorsa ya da şimdikinden eski ise de kurulmaz. Gizli anahtar bu depoda bulunmaz.

## Derleme

Ek araç gerekmez: Windows'un hazır gelen .NET Framework 4.x C# derleyicisi kullanılır.

```
derle.cmd
```

`YamanPcCleaner.exe` aynı klasörde oluşur. Kodu C# 5 sözdizimiyle yazılmıştır; derleme betiği `src` altındaki bütün `.cs` dosyalarını kendiliğinden toplar, yeni dosya eklerken betiği değiştirmen gerekmez.

### Proje yapısı

```
src/
  Program.cs        Giriş noktası
  Cekirdek/         Motor: ölçüm, temizlik, ayarlar, güncelleme, marka
  Arayuz/           Ana pencere, sekmeler, tema ve pencereler
araclar/            Derleme, imzalama, yayınlama ve test betikleri
kaynaklar/          Simgeler ve logo
docs/               Tanıtım sayfası (GitHub Pages)
```

Ürün adı, sürüm ve bağlantılar tek kaynaktan yönetilir: `src/Cekirdek/Marka.cs` ve `src/Cekirdek/SurumBilgisi.cs`.

## Yayınlama (GitHub)

Güncelleme kanalı GitHub Releases'tir. Yeni sürümü yayınlamak için iki yol var:

1. **Yerel:** `powershell -File araclar\yayinla.ps1 -Notlar "Sürüm notları"` — betik sürümü `src\Cekirdek\SurumBilgisi.cs`'ten okur, derler, `araclar\imzala.ps1` ile imzalar ve `YamanPcCleaner.exe` + `YamanPcCleaner.exe.imza` dosyalarını release'e yükler. `gh` kuruluysa onun oturumunu, değilse `GITHUB_TOKEN` ortam değişkenini kullanır.
2. **GitHub Actions:** `src\Cekirdek\SurumBilgisi.cs` içindeki sürümü artırıp `v1.3.0` gibi bir etiket push et. `.github/workflows/yayinla.yml` derler, `IMZA_GIZLI_ANAHTAR` repository secret'ı ile imzalar ve release'i oluşturur. Secret, `Belgeler\YamanPcCleaner-imza\gizli-anahtar.xml` dosyasının içeriğidir; depoya eklenmez.

Depo adı `src\Cekirdek\Marka.cs` içindeki `Myozan/YamanPcCleaner` ile aynı olmalıdır; güncelleme merkezi bu değeri oradan okur.

## Teşekkür

Windows sekmesindeki ayarların kaynağı [Win11Debloat](https://github.com/Raphire/Win11Debloat) (MIT lisanslı, Raphire).

## Lisans

[MIT](LICENSE) © 2026 Yaman
