# Katkıda Bulunma

Yaman PC Cleaner'a ilgi gösterdiğin için teşekkürler. Kod, test, dokümantasyon, çeviri ve hata raporları katkı olarak değerlidir.

## Başlamadan önce

1. Önce mevcut [Issues](https://github.com/Myozan/YamanPcCleaner/issues) kayıtlarını kontrol et.
2. Büyük değişiklikler için önce bir issue açıp yaklaşımı konuşalım.
3. Depoyu fork'la ve değişikliklerini ayrı bir branch üzerinde yap.

## Geliştirme ortamı

- Windows 10 veya Windows 11
- .NET Framework 4.x C# derleyicisi
- Git

Projeyi derlemek için depo kökünde:

```bat
derle.cmd
```

## Pull request kontrol listesi

- [ ] Değişiklik tek bir amaca odaklanıyor.
- [ ] Derleme başarılı.
- [ ] İlgili davranışlar elle veya uygun testlerle kontrol edildi.
- [ ] Kullanıcı verisi, gizli anahtar veya kişisel bilgi eklenmedi.
- [ ] Kullanıcıya görünen metinler Türkçe karakterleri koruyor.
- [ ] README / CHANGELOG gerekiyorsa güncellendi.
- [ ] Geri alınabilirlik ve yönetici izni gereksinimleri açıklandı.

## Güvenlik ve sistem değişiklikleri

Disk temizliği, kayıt defteri, DNS, sanal bellek, sürücü ve Windows ayarlarında güvenli varsayılanları koru. Yıkıcı işlemleri sessizce başlatma. Mümkün olduğunda önceki değerleri yedekle ve geri alma yolu sun.

## Commit ve PR

Commit mesajlarında kısa ve açıklayıcı bir özet kullan. PR açıklamasında sorunu, çözümü, test adımlarını ve kullanıcıya etkisini belirt. İlgili issue varsa bağlantısını ekle.

## Davranış kuralları

Katılımcılar [Code of Conduct](CODE_OF_CONDUCT.md) kurallarına uymalıdır.
