// Genel Bakış (ilk sekme): bilgisayarın durumu tek bakışta. 100 üzerinden puan, her denetim için yeşil / turuncu /
// kırmızı bir satır ve gerekiyorsa düğme (ilgili sekmeye ya da ayara götürür; kendiliğinden hiçbir şey değiştirmez).
// Solda "Sistemi Optimize Et", "Neden yavaş?", toplam kazanç ve sistem kimliği kartı.
// Denetimler arka planda, birbirinden bağımsız çalışır; biten satır hemen yerine oturur, puan da onunla birlikte akar.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

// Programın bugüne kadar açtığı yer: her temizlik işi bitince eklenir (ayarlar.txt, MB cinsinden).
static class Kazanc
{
    const string AyarDisk = "kazanc_disk_mb", AyarRam = "kazanc_ram_mb", AyarIs = "kazanc_is";

    public static event Action Degisti;

    public static long DiskBayt { get { return (long)Ayarlar.OkuSayi(AyarDisk, 0) * 1048576; } }
    public static long RamBayt { get { return (long)Ayarlar.OkuSayi(AyarRam, 0) * 1048576; } }
    public static int IsSayisi { get { return Ayarlar.OkuSayi(AyarIs, 0); } }

    public static void Ekle(long ramBayt, long diskBayt)
    {
        try
        {
            if (ramBayt > 0) Ayarlar.YazSayi(AyarRam, Topla(AyarRam, ramBayt));
            if (diskBayt > 0) Ayarlar.YazSayi(AyarDisk, Topla(AyarDisk, diskBayt));
            Ayarlar.YazSayi(AyarIs, (int)Math.Min(int.MaxValue - 1, (long)IsSayisi + 1));
        }
        catch { }
        try
        {
            var parcalar = new List<string>();
            if (ramBayt > 0) parcalar.Add("RAM " + AnaForm.Boyut(ramBayt));
            if (diskBayt > 0) parcalar.Add("disk " + AnaForm.Boyut(diskBayt));
            Denetim.Ekle("Temizlik", parcalar.Count > 0 ? string.Join(" · ", parcalar) : "Bakım tamamlandı");
        }
        catch { }
        var d = Degisti;
        if (d != null) try { d(); } catch { }
    }

    // Ayarlar → "Sıfırla": yalnızca sayaç.
    public static void Sifirla()
    {
        Ayarlar.YazSayi(AyarDisk, 0);
        Ayarlar.YazSayi(AyarRam, 0);
        Ayarlar.YazSayi(AyarIs, 0);
        var d = Degisti;
        if (d != null) try { d(); } catch { }
    }

    static int Topla(string ad, long bayt)
    {
        long mb = Ayarlar.OkuSayi(ad, 0) + bayt / 1048576;
        return (int)Math.Min(int.MaxValue - 1, Math.Max(0, mb));
    }

    public static string Metin()
    {
        if (IsSayisi == 0) return "Henüz temizlik yapılmadı. İlk temizlikten sonra toplam kazancın burada birikir.";
        return string.Format("Diskte {0} yer açıldı\nRAM'de {1} boşaltıldı · {2} temizlik",
            AnaForm.Boyut(DiskBayt), AnaForm.Boyut(RamBayt), IsSayisi.ToString("N0"));
    }
}

enum DenetimDurumu { Bekliyor, Iyi, Bilgi, Uyari, Kotu }

class DenetimSatiri
{
    public DenetimDurumu Durum;
    public string Baslik, Aciklama;
    public int Kesinti;           // puandan düşülen
    public string Dugme, Hedef;   // Hedef: sekme adı ("Disk", "Turbo"...) ya da özel iş ("sanal", "bekci", "disk-omur")

    public static DenetimSatiri Yap(DenetimDurumu d, int kesinti, string baslik, string aciklama, string dugme, string hedef)
    {
        return new DenetimSatiri { Durum = d, Kesinti = kesinti, Baslik = baslik, Aciklama = aciklama, Dugme = dugme, Hedef = hedef };
    }
}

// Pil sağlığı: sürücüden doğrudan (Windows'un pil raporunun okuduğu yer); yönetici gerekmez.
static class PilBilgisi
{
    public class Sonuc { public long Tasarim, Dolu; public int Dongu; }

    static Guid PilSinifi = new Guid("72631e54-78a4-11d0-bcf7-00aa00b7b32a");

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }

    [StructLayout(LayoutKind.Sequential)]
    struct BATTERY_QUERY_INFORMATION { public uint BatteryTag; public int InformationLevel; public int AtRate; }

    [StructLayout(LayoutKind.Sequential)]
    struct BATTERY_INFORMATION
    {
        public uint Capabilities; public byte Technology, R1, R2, R3; public uint Chemistry;
        public uint DesignedCapacity, FullChargedCapacity, DefaultAlert1, DefaultAlert2, CriticalBias, CycleCount;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid sinif, IntPtr numaralayici, IntPtr pencere, uint bayrak);
    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInterfaces(IntPtr kume, IntPtr veri, ref Guid sinif, uint sira, ref SP_DEVICE_INTERFACE_DATA arayuz);
    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr kume, ref SP_DEVICE_INTERFACE_DATA arayuz, IntPtr ayrinti, int boyut, out int gereken, IntPtr veri);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr kume);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr CreateFile(string ad, uint erisim, uint paylasim, IntPtr guvenlik, uint olustur, uint bayrak, IntPtr sablon);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr h, uint kod, ref uint girdi, int girdiBoyu, out uint cikti, int ciktiBoyu, out int donen, IntPtr ortusme);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr h, uint kod, ref BATTERY_QUERY_INFORMATION girdi, int girdiBoyu, out BATTERY_INFORMATION cikti, int ciktiBoyu, out int donen, IntPtr ortusme);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);

    // İlk pil; pil yoksa ya da okunamazsa null.
    public static Sonuc Oku()
    {
        IntPtr kume = SetupDiGetClassDevs(ref PilSinifi, IntPtr.Zero, IntPtr.Zero, 0x2 | 0x10 /* PRESENT | DEVICEINTERFACE */);
        if (kume == IntPtr.Zero || kume == new IntPtr(-1)) return null;
        try
        {
            for (uint i = 0; i < 4; i++)
            {
                var arayuz = new SP_DEVICE_INTERFACE_DATA();
                arayuz.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                if (!SetupDiEnumDeviceInterfaces(kume, IntPtr.Zero, ref PilSinifi, i, ref arayuz)) break;
                int gereken;
                SetupDiGetDeviceInterfaceDetail(kume, ref arayuz, IntPtr.Zero, 0, out gereken, IntPtr.Zero);
                if (gereken <= 0) continue;
                IntPtr ayrinti = Marshal.AllocHGlobal(gereken);
                string yol;
                try
                {
                    Marshal.WriteInt32(ayrinti, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(kume, ref arayuz, ayrinti, gereken, out gereken, IntPtr.Zero)) continue;
                    yol = Marshal.PtrToStringUni(new IntPtr(ayrinti.ToInt64() + 4));
                }
                finally { Marshal.FreeHGlobal(ayrinti); }

                IntPtr h = CreateFile(yol, 0xC0000000, 3, IntPtr.Zero, 3 /* OPEN_EXISTING */, 0x80, IntPtr.Zero);
                if (h == new IntPtr(-1)) continue;
                try
                {
                    uint bekle = 0, etiket; int donen;
                    if (!DeviceIoControl(h, 0x294040 /* QUERY_TAG */, ref bekle, 4, out etiket, 4, out donen, IntPtr.Zero) || etiket == 0) continue;
                    var sorgu = new BATTERY_QUERY_INFORMATION { BatteryTag = etiket, InformationLevel = 0 };
                    BATTERY_INFORMATION bilgi;
                    if (!DeviceIoControl(h, 0x294044 /* QUERY_INFORMATION */, ref sorgu, Marshal.SizeOf(typeof(BATTERY_QUERY_INFORMATION)),
                                         out bilgi, Marshal.SizeOf(typeof(BATTERY_INFORMATION)), out donen, IntPtr.Zero)) continue;
                    // Bağıl kapasite bildiren piller (yüzde) karşılaştırılamaz; sistem pili olmayan (UPS) atlanır.
                    if ((bilgi.Capabilities & 0x40000000) != 0 || (bilgi.Capabilities & 0x80000000) == 0) continue;
                    if (bilgi.DesignedCapacity == 0 || bilgi.FullChargedCapacity == 0) continue;
                    return new Sonuc { Tasarim = bilgi.DesignedCapacity, Dolu = bilgi.FullChargedCapacity, Dongu = (int)bilgi.CycleCount };
                }
                finally { CloseHandle(h); }
            }
        }
        catch { }
        finally { SetupDiDestroyDeviceInfoList(kume); }
        return null;
    }
}

// Fiziksel diskler (Windows'un depolama sağlayıcısından, yönetici gerekmez) ve isteğe bağlı ömür bilgisi
// (aşınma, sıcaklık, çalışma saati: yalnızca yönetici okuyabilir; "--disk-omru" yardımcısı sabit bir dosyaya yazar).
static class DiskSagligi
{
    public class Disk { public string Kimlik, Ad, Tur; public int Saglik; public long Boyut; public int Asinma = -1, Isi = -1, Saat = -1; }

    static readonly string OmurDosyasi = Path.Combine(Path.GetTempPath(), "YamanPcCleaner-disk-omru.txt");
    public static Dictionary<string, int[]> Omur;   // kimlik → { aşınma, ısı, saat }; okunmadıysa null

    public static List<Disk> Listele()
    {
        var l = new List<Disk>();
        try
        {
            var kapsam = new ManagementScope(@"root\Microsoft\Windows\Storage");
            using (var s = new ManagementObjectSearcher(kapsam, new ObjectQuery("SELECT DeviceId, FriendlyName, MediaType, BusType, HealthStatus, Size FROM MSFT_PhysicalDisk")))
            using (var sonuc = s.Get())
            {
                foreach (ManagementObject m in sonuc)
                {
                    using (m)
                    {
                        var d = new Disk
                        {
                            Kimlik = Convert.ToString(m["DeviceId"], CultureInfo.InvariantCulture),
                            Ad = (Convert.ToString(m["FriendlyName"]) ?? "Disk").Trim(),
                            Saglik = Sayi(m["HealthStatus"]),
                            Boyut = m["Size"] != null ? Convert.ToInt64(m["Size"], CultureInfo.InvariantCulture) : 0
                        };
                        int tur = Sayi(m["MediaType"]), yol = Sayi(m["BusType"]);
                        string ortam = tur == 4 ? "SSD" : tur == 3 ? "HDD" : "";
                        string bag = yol == 17 ? "NVMe" : yol == 11 ? "SATA" : yol == 7 ? "USB" : "";
                        d.Tur = (bag + " " + ortam).Trim();
                        if (bag == "USB") continue;   // takılı flaş bellek / harici disk durum panosunda sayılmaz
                        l.Add(d);
                    }
                }
            }
        }
        catch { }
        var omur = Omur;
        if (omur != null)
            foreach (var d in l)
            {
                int[] v;
                if (d.Kimlik != null && omur.TryGetValue(d.Kimlik, out v)) { d.Asinma = v[0]; d.Isi = v[1]; d.Saat = v[2]; }
            }
        return l;
    }

    static int Sayi(object o)
    {
        try { return o == null ? -1 : Convert.ToInt32(o, CultureInfo.InvariantCulture); } catch { return -1; }
    }

    // Yönetici olarak: exe --disk-omru. Satır "kimlik TAB aşınma TAB ısı TAB saat" (-1 = bilinmiyor). 0 = tamam.
    public static int YoneticiYaz()
    {
        try
        {
            var satirlar = new List<string>();
            var kapsam = new ManagementScope(@"root\Microsoft\Windows\Storage");
            using (var s = new ManagementObjectSearcher(kapsam, new ObjectQuery("SELECT DeviceId, Wear, Temperature, PowerOnHours FROM MSFT_StorageReliabilityCounter")))
            using (var sonuc = s.Get())
                foreach (ManagementObject m in sonuc)
                    using (m)
                        satirlar.Add(string.Join("\t", Convert.ToString(m["DeviceId"], CultureInfo.InvariantCulture),
                            Sayi(m["Wear"]), Sayi(m["Temperature"]), Sayi(m["PowerOnHours"])));
            File.WriteAllLines(OmurDosyasi, satirlar);
            return 0;
        }
        catch { return 1; }
    }

    // Yardımcıyı yönetici izniyle çalıştırır ve sonucu okur. -2 = izin verilmedi, -1 = okunamadı, 0 = tamam.
    public static int OmruOku()
    {
        try { File.Delete(OmurDosyasi); } catch { }
        try
        {
            using (var p = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--disk-omru") { UseShellExecute = true, Verb = "runas" }))
            {
                p.WaitForExit(30000);
                if (!p.HasExited || p.ExitCode != 0) return -1;
            }
        }
        catch (System.ComponentModel.Win32Exception) { return -2; }
        catch { return -1; }
        try
        {
            var d = new Dictionary<string, int[]>();
            foreach (var satir in File.ReadAllLines(OmurDosyasi))
            {
                var p = satir.Split('\t');
                if (p.Length < 4) continue;
                int a, i, s;
                int.TryParse(p[1], out a); int.TryParse(p[2], out i); int.TryParse(p[3], out s);
                d[p[0]] = new[] { a, i, s };
            }
            try { File.Delete(OmurDosyasi); } catch { }
            Omur = d;
            return 0;
        }
        catch { return -1; }
    }
}

// Durum panosunun denetimleri. Her biri arka planda çalışır ve tek satır döner (null = bu bilgisayarda gösterilmez).
static class SistemDenetimi
{
    public static readonly string[] Sira = { "sanal", "ram", "disk", "saglik", "pil", "baslangic", "turbo", "surucu", "cokme", "sizinti", "bekci" };

    public static DenetimSatiri SanalBellek()
    {
        var m = Yerel.Bellek();
        if (m.ullTotalPageFile == 0) return null;
        double c = (double)(m.ullTotalPageFile - m.ullAvailPageFile) / m.ullTotalPageFile;
        int y = (int)Math.Round(c * 100);
        long ilk, enCok; string neden;
        bool kucuk = AnaForm.SanalHedef(out ilk, out enCok, out neden);
        if (c >= 0.9)
            return DenetimSatiri.Yap(DenetimDurumu.Kotu, 25, "Sanal bellek %" + y + " dolu",
                "Programlar (oyunlar dahil) \"bellek yetersiz\" diye kapanabilir. Gereksiz programları kapat.", "Göster", "Sistem");
        if (kucuk)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 12, "Sanal bellek bu bilgisayar için küçük",
                string.Format("Önerilen: {0} GB. Oyunların \"bellek yetersiz\" diye kapanmasını önler; geri alınabilir.", ilk / 1024), "Düzelt", "sanal");
        if (c >= 0.75)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 5, "Sanal bellek %" + y + " dolu", "Biraz kalabalık; uzun oyunlarda dolabilir.", "Göster", "Sistem");
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "Sanal bellek rahat", "%" + y + " kullanımda; boyutu bu bilgisayar için yeterli.", null, null);
    }

    public static DenetimSatiri Ram()
    {
        var m = Yerel.Bellek();
        if (m.ullTotalPhys == 0) return null;
        double r = (double)(m.ullTotalPhys - m.ullAvailPhys) / m.ullTotalPhys;
        int gb = (int)Math.Round(m.ullTotalPhys / 1073741824.0);
        int y = (int)Math.Round(r * 100);
        if (r >= 0.9)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 8, "RAM %" + y + " dolu", "Bilgisayar takılabilir; gereksiz programları kapat ya da RAM'i boşalt.", "Göster", "Sistem");
        if (gb <= 8)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 6, gb + " GB RAM", "Yeni oyunlar için az; 16 GB önerilir. Şu an %" + y + " kullanımda.", null, null);
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, gb + " GB RAM", "%" + y + " kullanımda.", null, null);
    }

    // Sistem diskinde boş yer + yöneticisiz temizlenebilecek gereksiz dosyalar (Otomatik optimize'nin maddeleri).
    public static DenetimSatiri Disk()
    {
        string kok = Path.GetPathRoot(Environment.SystemDirectory);
        DriveInfo d;
        try { d = new DriveInfo(kok); if (!d.IsReady) return null; } catch { return null; }
        long bos = d.AvailableFreeSpace, top = d.TotalSize;
        long cop = 0;
        try
        {
            foreach (var t in DiskTemizlik.Liste(false))
            {
                if (t.Yonetici || !t.Varsayilan || t.Ozel != null) continue;
                DiskTemizlik.Hesapla(t);
                if (t.Boyut > 0 && t.AcikSurec == null) cop += t.Boyut;
            }
        }
        catch { }
        string harf = kok.TrimEnd('\\');
        string copMetin = cop >= 200L * 1048576 ? " · " + AnaForm.Boyut(cop) + " gereksiz dosya temizlenebilir" : "";
        double oran = top > 0 ? (double)bos / top : 1;
        if (oran < 0.08 || bos < 10L * 1073741824)
            return DenetimSatiri.Yap(DenetimDurumu.Kotu, 20, harf + " diskinde yer kalmadı: " + AnaForm.Boyut(bos) + " boş",
                "Windows ve oyunlar yavaşlar, güncellemeler kurulamaz." + copMetin, "Temizle", "Disk");
        if (oran < 0.15 || bos < 30L * 1073741824)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 8, harf + " diskinde az yer var: " + AnaForm.Boyut(bos) + " boş",
                "Oyun güncellemeleri için yer açmak iyi olur." + copMetin, "Temizle", "Disk");
        if (cop >= 2L * 1073741824)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 3, AnaForm.Boyut(cop) + " gereksiz dosya var",
                harf + " diskinde " + AnaForm.Boyut(bos) + " boş; geçici dosyalar ve önbellekler temizlenebilir.", "Temizle", "Disk");
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, harf + " diskinde " + AnaForm.Boyut(bos) + " boş", "Yer yeterli." + copMetin, null, null);
    }

    public static DenetimSatiri Saglik()
    {
        var l = DiskSagligi.Listele();
        if (l.Count == 0) return null;
        var kotu = l.FirstOrDefault(x => x.Saglik == 2);
        var uyari = l.FirstOrDefault(x => x.Saglik == 1);
        var asinmis = l.Where(x => x.Asinma >= 0).OrderByDescending(x => x.Asinma).FirstOrDefault();
        if (kotu != null)
            return DenetimSatiri.Yap(DenetimDurumu.Kotu, 30, "Disk arızalanıyor olabilir: " + kotu.Ad,
                "Windows bu diski sağlıksız görüyor. Önemli dosyalarını hemen başka yere yedekle.", null, null);
        if (uyari != null)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 15, "Disk uyarı veriyor: " + uyari.Ad,
                "Windows bu diskte sorun belirtisi görüyor. Önemli dosyalarını yedeklemek iyi olur.", null, null);
        if (asinmis != null && asinmis.Asinma >= 90)
            return DenetimSatiri.Yap(DenetimDurumu.Kotu, 20, asinmis.Ad + " ömrünün sonunda",
                OmurMetni(asinmis) + ". Önemli dosyalarını yedekle, yeni disk düşün.", null, null);
        if (asinmis != null && asinmis.Asinma >= 70)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 8, asinmis.Ad + " yıpranmaya başladı", OmurMetni(asinmis) + ".", null, null);
        string ozet = string.Join(" · ", l.Select(x => x.Asinma >= 0 ? x.Ad + ": " + OmurMetni(x) : x.Ad + (x.Tur.Length > 0 ? " (" + x.Tur + ")" : "")));
        bool omurVar = l.Any(x => x.Asinma >= 0);
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, l.Count == 1 ? "Disk sağlıklı" : "Diskler sağlıklı", ozet,
            omurVar ? null : "Ömrü göster", omurVar ? null : "disk-omur");
    }

    static string OmurMetni(DiskSagligi.Disk d)
    {
        var p = new List<string> { "ömrünün %" + d.Asinma + "'ü kullanıldı" };
        if (d.Isi > 0) p.Add(d.Isi + " °C");
        if (d.Saat > 0) p.Add(d.Saat.ToString("N0") + " saat çalıştı");
        return string.Join(", ", p);
    }

    public static DenetimSatiri Pil()
    {
        if (!GucAyarlari.PilVar) return null;
        var p = PilBilgisi.Oku();
        if (p == null) return null;
        int y = (int)Math.Min(100, Math.Round(100.0 * p.Dolu / p.Tasarim));
        string dongu = p.Dongu > 0 ? " · " + p.Dongu + " şarj döngüsü" : "";
        if (y < 60)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 6, "Pil çok yıpranmış: %" + y,
                "İlk hâlinin %" + y + "'i kadar şarj tutuyor; çabuk biter. Değişim düşünülebilir" + dongu + ".", null, null);
        if (y < 80)
            return DenetimSatiri.Yap(DenetimDurumu.Bilgi, 0, "Pil biraz yıpranmış: %" + y, "İlk hâlinin %" + y + "'i kadar şarj tutuyor" + dongu + ". Normal yaşlanma.", null, null);
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "Pil sağlıklı: %" + y, "İlk hâlinin %" + y + "'i kadar şarj tutuyor" + dongu + ".", null, null);
    }

    public static DenetimSatiri Baslangic()
    {
        var l = BaslangicYoneticisi.Listele();
        var acik = l.Where(o => o.Acik).ToList();
        int gorev = acik.Count(o => o.OnayAnahtari == BaslangicYoneticisi.GorevAnahtari);
        int kapatilabilir = acik.Count(o => !o.Onerilmez);
        string ek = gorev > 0 ? " (" + gorev + " zamanlanmış görev dahil)" : "";
        if (kapatilabilir >= 12)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 8, "Açılışta " + acik.Count + " program başlıyor" + ek,
                "Bilgisayar geç açılır, RAM baştan dolar. Gerekmeyenleri kapat.", "Göster", "Başlangıç");
        if (kapatilabilir >= 8)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 4, "Açılışta " + acik.Count + " program başlıyor" + ek,
                "Biraz kalabalık; gerekmeyenleri kapatabilirsin.", "Göster", "Başlangıç");
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "Açılışta " + acik.Count + " program başlıyor" + ek, "Kalabalık değil.", null, null);
    }

    public static DenetimSatiri Turbo()
    {
        var o = TurboAyarlari.Oku();
        var uyarilar = o.Satirlar.Where(s => s.Uyari).ToList();
        if (uyarilar.Count == 0)
            return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "Oyun ayarları yerinde", "Turbo'nun durum denetiminde öneri yok.", null, null);
        string ilk = uyarilar[0].Metin;
        int i = ilk.IndexOf(':');
        if (i > 0) ilk = ilk.Substring(0, i);
        return DenetimSatiri.Yap(DenetimDurumu.Uyari, Math.Min(10, 3 * uyarilar.Count),
            "Oyun ayarlarında " + uyarilar.Count + " öneri var", ilk + (uyarilar.Count > 1 ? " ve diğerleri" : ""), "Göster", "Turbo");
    }

    // Arayüzden: Sürücü sekmesinin açılıştaki denetimi. bilgi null: henüz denetlenmedi.
    public static DenetimSatiri Surucu(NvidiaBilgi bilgi, bool denetlendi)
    {
        if (!denetlendi) return DenetimSatiri.Yap(DenetimDurumu.Bekliyor, 0, "NVIDIA sürücüsü", "Denetleniyor...", null, null);
        if (bilgi == null || string.IsNullOrEmpty(bilgi.KartAdi)) return null;   // NVIDIA kartı yok
        if (bilgi.YeniVar)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 6, "Yeni NVIDIA sürücüsü var: " + bilgi.SonSurum,
                "Kurulu: " + bilgi.KuruluSurum + ". Yeni oyunlarda hata ve performans düzeltmeleri getirir.", "Göster", "Sürücü");
        if (!string.IsNullOrEmpty(bilgi.Hata))
            return DenetimSatiri.Yap(DenetimDurumu.Bilgi, 0, "NVIDIA sürücüsü denetlenemedi", "Kurulu: " + bilgi.KuruluSurum + ". İnternet bağlantısını kontrol et.", null, null);
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "NVIDIA sürücüsü güncel", "Kurulu: " + bilgi.KuruluSurum, null, null);
    }

    public static DenetimSatiri Cokme()
    {
        var l = (CokmeGecmisi.Oku(7) ?? new List<CokmeOlayi>()).Where(o => !CokmeGecmisi.OnemsizMi(o)).ToList();
        int mavi = l.Count(o => o.Kategori == "maviekran");
        int bellek = l.Count(o => o.Kategori == "bellek" || o.BellekDoluyken);
        if (mavi > 0)
            return DenetimSatiri.Yap(DenetimDurumu.Kotu, 15, "Son 7 günde " + mavi + " mavi ekran",
                "Ayrıntısı ve olası sebebi Sağlık sekmesinde.", "Göster", "Sağlık");
        if (bellek > 0)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 10, "Son 7 günde " + bellek + " \"bellek yetersiz\" olayı",
                "Sanal belleği büyütmek ve sızıntı yapan programı kapatmak genelde çözer.", "Göster", "Sağlık");
        if (l.Count >= 3)
            return DenetimSatiri.Yap(DenetimDurumu.Uyari, 5, "Son 7 günde " + l.Count + " çökme", "Hangi programın çöktüğü Sağlık sekmesinde.", "Göster", "Sağlık");
        return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, l.Count == 0 ? "Son 7 günde çökme yok" : "Son 7 günde " + l.Count + " çökme",
            l.Count == 0 ? "Windows hiç çökme, donma ya da mavi ekran kaydetmedi." : "Az; endişelenecek bir şey yok.", null, null);
    }

    public static DenetimSatiri Sizinti(BellekBekcisi bekci)
    {
        List<SizintiSupheli> l = null;
        try { if (bekci != null && bekci.Izleyici != null) l = bekci.Izleyici.Supheliler(); } catch { }
        if (l == null || l.Count == 0)
            return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "Sürekli büyüyen program yok", "Bekçi 30 saniyede bir ölçüyor.", null, null);
        var s = l[0];
        string ad = s.Gorunen ?? s.Ad;
        return DenetimSatiri.Yap(DenetimDurumu.Uyari, 5, ad + " sürekli büyüyor",
            "Bellek sızıntısı olabilir (şu an " + AnaForm.Boyut(s.Simdi) + "); gerekmiyorsa kapat.", "Göster", "Sağlık");
    }

    public static DenetimSatiri Bekci(BellekBekcisi bekci)
    {
        if (bekci == null) return null;
        bool acik = false;
        try { acik = bekci.WindowsIleBaslat && bekci.ArkaPlandaCalis; } catch { }
        if (acik) return DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "Bellek bekçisi açık", "Windows ile başlıyor; bellek dolmadan önce haber verir.", null, null);
        return DenetimSatiri.Yap(DenetimDurumu.Uyari, 3, "Bellek bekçisi Windows ile başlamıyor",
            "Açarsan saatin yanında bekler, bellek dolmadan önce haber verir.", "Aç", "bekci");
    }
}

// Sistem kimliği: donanım özeti, forumda / Discord'da paylaşmak için. Kullanıcı ve bilgisayar adı içermez.
static class SistemKimligi
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields, dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplaySettings(string aygit, int kip, ref DEVMODE dm);

    static readonly object kimlikKilit = new object();
    static List<KeyValuePair<string, string>> kimlikOnbellek;

    // (etiket, değer) çiftleri; arka planda çağrılır. WMI sorguları pahalı olduğu için bir kez toplanır.
    public static List<KeyValuePair<string, string>> Topla()
    {
        lock (kimlikKilit)
        {
            if (kimlikOnbellek == null) kimlikOnbellek = ToplaIcerik();
            return new List<KeyValuePair<string, string>>(kimlikOnbellek);
        }
    }

    static List<KeyValuePair<string, string>> ToplaIcerik()
    {
        var l = new List<KeyValuePair<string, string>>();
        Action<string, string> ekle = (a, b) => { if (!string.IsNullOrEmpty(b)) l.Add(new KeyValuePair<string, string>(a, b)); };

        // Bilgisayar (dizüstü) ya da anakart (masaüstü); üreticinin boş bıraktığı yer tutucular atlanır.
        try
        {
            if (GucAyarlari.PilVar)
            {
                var cs = Wmi("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
                if (cs != null) ekle("Bilgisayar", Birlestir(Uretici(cs[0]), Gecerli(cs[1])));
            }
            else
            {
                var bb = Wmi("SELECT Manufacturer, Product FROM Win32_BaseBoard");
                if (bb != null) ekle("Anakart", Birlestir(Uretici(bb[0]), Gecerli(bb[1])));
            }
        }
        catch { }

        try
        {
            var c = Wmi("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            if (c != null) ekle("İşlemci", Sadelestir(c[0]) + " · " + c[1] + " çekirdek / " + c[2] + " iş parçacığı");
        }
        catch { }

        try
        {
            var kartlar = new List<string>();
            var takili = EkranKartlari.Takililar();
            using (var k = Registry.LocalMachine.OpenSubKey(EkranKartlari.SinifAnahtari))
                if (k != null)
                    foreach (var ad in k.GetSubKeyNames())
                    {
                        if (ad.Length != 4 || (takili != null && !takili.Contains(ad))) continue;
                        using (var a = k.OpenSubKey(ad))
                        {
                            if (a == null) continue;
                            string adi = a.GetValue("DriverDesc") as string;
                            if (string.IsNullOrEmpty(adi) || adi.IndexOf("Basic", StringComparison.OrdinalIgnoreCase) >= 0
                                || adi.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            long v = EkranKartlari.Vram(a);
                            kartlar.Add(adi.Trim() + (v >= 512L * 1048576 ? " (" + Math.Round(v / 1073741824.0) + " GB)" : ""));
                        }
                    }
            ekle("Ekran kartı", string.Join(" + ", kartlar.Distinct()));
        }
        catch { }

        try
        {
            var moduller = WmiCok("SELECT Capacity, Speed, ConfiguredClockSpeed, SMBIOSMemoryType FROM Win32_PhysicalMemory");
            long top = (long)Yerel.Bellek().ullTotalPhys;
            var p = new List<string> { Math.Round(top / 1073741824.0) + " GB" };
            if (moduller.Count > 0)
            {
                p.Add(moduller.Count + " modül");
                int tur = Sayi(moduller[0][3]);
                string turAd = tur == 34 ? "DDR5" : tur == 35 ? "LPDDR5" : tur == 26 ? "DDR4" : tur == 30 ? "LPDDR4" : tur == 24 ? "DDR3" : null;
                int hiz = moduller.Select(m => Sayi(m[2]) > 0 ? Sayi(m[2]) : Sayi(m[1])).Where(h => h > 0).DefaultIfEmpty(0).Min();
                if (turAd != null || hiz > 0) p.Add(((turAd ?? "") + (hiz > 0 ? " " + hiz + " MT/s" : "")).Trim());
            }
            ekle("RAM", string.Join(" · ", p));
        }
        catch { }

        try
        {
            foreach (var d in DiskSagligi.Listele())
                ekle("Disk", d.Ad + " · " + Boyut(d.Boyut) + (d.Tur.Length > 0 ? " · " + d.Tur : ""));
        }
        catch { }

        try
        {
            foreach (var e in Screen.AllScreens)
            {
                var dm = new DEVMODE { dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)) };
                if (EnumDisplaySettings(e.DeviceName, -1 /* ENUM_CURRENT_SETTINGS */, ref dm))
                    ekle("Ekran", dm.dmPelsWidth + "×" + dm.dmPelsHeight + (dm.dmDisplayFrequency > 1 ? " · " + dm.dmDisplayFrequency + " Hz" : ""));
            }
        }
        catch { }

        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                string urun = k.GetValue("ProductName") as string ?? "Windows";
                string derleme = k.GetValue("CurrentBuildNumber") as string ?? "";
                int d; int.TryParse(derleme, out d);
                if (d >= 22000) urun = urun.Replace("Windows 10", "Windows 11");   // Windows 11 de "Windows 10" yazar
                string surum = k.GetValue("DisplayVersion") as string;
                object ubr = k.GetValue("UBR");
                ekle("Windows", urun + (string.IsNullOrEmpty(surum) ? "" : " " + surum) + " (" + derleme + (ubr is int ? "." + ubr : "") + ")");
            }
        }
        catch { }
        return l;
    }

    public static string Metin(List<KeyValuePair<string, string>> l)
    {
        return string.Join("\r\n", l.Select(k => k.Key + ": " + k.Value));
    }

    static string Boyut(long b)
    {
        if (b >= 1000L * 1000 * 1000 * 1000) return Math.Round(b / 1e12, 1).ToString("0.#") + " TB";
        return Math.Round(b / 1e9) + " GB";   // diskler üreticinin birimiyle (1 TB = 10^12)
    }

    static string Sadelestir(string ad)
    {
        ad = (ad ?? "").Replace("(R)", "").Replace("(TM)", "").Replace("(tm)", "");
        int i = ad.IndexOf(" CPU @", StringComparison.OrdinalIgnoreCase);
        if (i > 0) ad = ad.Substring(0, i);
        while (ad.Contains("  ")) ad = ad.Replace("  ", " ");
        return ad.Trim();
    }

    static readonly string[] YerTutucular = { "System manufacturer", "System Product Name", "To Be Filled", "Default string", "O.E.M.", "Not Applicable", "Base Board" };

    static string Gecerli(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return YerTutucular.Any(y => s.IndexOf(y, StringComparison.OrdinalIgnoreCase) >= 0) ? null : s;
    }

    static string Uretici(string s)
    {
        s = Gecerli(s);
        if (s == null) return null;
        foreach (var ek in new[] { " International Co., Ltd.", " Computer Inc.", " COMPUTER INC.", " Co., Ltd.", " Corporation", " Inc.", " Technology" })
            if (s.EndsWith(ek, StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - ek.Length);
        if (s.Equals("Micro-Star", StringComparison.OrdinalIgnoreCase)) s = "MSI";
        return s.Trim();
    }

    static string Birlestir(string a, string b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return b.StartsWith(a, StringComparison.OrdinalIgnoreCase) ? b : a + " " + b;
    }

    static int Sayi(string s) { int n; return int.TryParse(s, out n) ? n : 0; }

    static string[] Wmi(string sorgu)
    {
        var l = WmiCok(sorgu);
        return l.Count > 0 ? l[0] : null;
    }

    static List<string[]> WmiCok(string sorgu)
    {
        var l = new List<string[]>();
        // Alanlar sorgudaki sırayla döner: "SELECT a, b, c FROM ..."
        var adlar = sorgu.Substring(7, sorgu.IndexOf(" FROM ", StringComparison.OrdinalIgnoreCase) - 7).Split(',').Select(x => x.Trim()).ToArray();
        using (var s = new ManagementObjectSearcher(sorgu))
        using (var sonuc = s.Get())
            foreach (ManagementObject m in sonuc)
                using (m)
                    l.Add(adlar.Select(a => { var o = m[a]; return o == null ? null : Convert.ToString(o, CultureInfo.InvariantCulture).Trim(); }).ToArray());
        return l;
    }
}

// Puan halkası: ortada puan, çevresinde dolan yay. Denetim sürerken yay döner; puan gelince sayarak dolar.
class PuanHalkasi : Control
{
    static readonly Font PuanYazi = new Font("Segoe UI Semibold", 38f);
    static readonly Font KucukYazi = new Font("Segoe UI", 9f);
    static readonly Font DurumYazi = new Font("Segoe UI", 12.5f, FontStyle.Bold);

    int hedef = -1;
    double gorunen;
    float donus;
    string durum = "Denetleniyor...", alt = "";
    readonly Timer zamanlayici = new Timer { Interval = 16 };

    public PuanHalkasi()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Renk.Arka;
        zamanlayici.Tick += delegate
        {
            if (hedef < 0) donus = (donus + 6) % 360;
            else
            {
                double fark = hedef - gorunen;
                if (Math.Abs(fark) < 0.4) { gorunen = hedef; zamanlayici.Stop(); }
                else gorunen += fark * 0.09 + Math.Sign(fark) * 0.15;
            }
            Invalidate();
        };
        zamanlayici.Start();
        Disposed += delegate { zamanlayici.Dispose(); };
    }

    // puan < 0: denetim sürüyor (dönen yay).
    public void Ayarla(int puan, string durumMetni, string altMetin)
    {
        durum = durumMetni ?? "";
        alt = altMetin ?? "";
        if (puan != hedef)
        {
            hedef = puan;
            // Hareketler kapalıysa puan sayarak gelmez (bekleme yayı yine döner).
            if (Visible && (Hareket.Acik || puan < 0)) zamanlayici.Start(); else if (puan >= 0) gorunen = puan;
        }
        Invalidate();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Hareket.Acik && hedef >= 0) gorunen = hedef;
        if (Visible && (hedef < 0 || Math.Abs(hedef - gorunen) >= 0.4)) zamanlayici.Start();
        else zamanlayici.Stop();
    }

    public static Color PuanRengi(double p) { return p >= 85 ? Renk.Yesil : p >= 60 ? Renk.Turuncu : Renk.Kirmizi; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int cap = Math.Min(Width - 20, Height - 70);
        if (cap < 60) return;
        var kare = new Rectangle((Width - cap) / 2, 8, cap, cap);
        float kalinlik = Math.Max(8, cap / 14f);
        var ic = Rectangle.Inflate(kare, -(int)(kalinlik / 2), -(int)(kalinlik / 2));
        using (var p = new Pen(Renk.Cizgi, kalinlik)) g.DrawEllipse(p, ic);
        if (hedef < 0)
        {
            using (var p = new Pen(Renk.Soluk, kalinlik) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(p, ic, donus - 90, 70);
        }
        else if (gorunen > 0.5)
        {
            using (var p = new Pen(PuanRengi(gorunen), kalinlik) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(p, ic, -90, (float)(360 * gorunen / 100));
        }
        string sayi = hedef < 0 ? "…" : ((int)Math.Round(gorunen)).ToString();
        var merkez = new Rectangle(kare.X, kare.Y + cap / 2 - 38, cap, 60);
        TextRenderer.DrawText(g, sayi, PuanYazi, merkez, Renk.Yazi, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        if (hedef >= 0)
            TextRenderer.DrawText(g, "/ 100", KucukYazi, new Rectangle(kare.X, merkez.Bottom - 4, cap, 18), Renk.Soluk,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, durum, DurumYazi, new Rectangle(0, kare.Bottom + 8, Width, 26),
            hedef < 0 ? Renk.Soluk : PuanRengi(hedef), TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, alt, KucukYazi, new Rectangle(0, kare.Bottom + 34, Width, 20), Renk.Soluk,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}

// Durum panosunun bir satırı: renkli nokta, başlık, açıklama ve sağda isteğe bağlı düğme.
class DenetimKutusu : Panel
{
    static readonly Font BaslikYazi = new Font("Segoe UI", 10f, FontStyle.Bold);
    static readonly Font AciklamaYazi = new Font("Segoe UI", 9f);
    public readonly DenetimSatiri Satir;
    readonly Button dugme;

    public DenetimKutusu(DenetimSatiri s, Action<DenetimSatiri> tiklaninca)
    {
        Satir = s;
        Dock = DockStyle.Top;
        Height = 58;
        BackColor = Renk.Panel;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        if (!string.IsNullOrEmpty(s.Dugme))
        {
            dugme = new Button
            {
                Text = s.Dugme, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Height = 30, AutoSize = true, MinimumSize = new Size(96, 30),
                BackColor = s.Durum == DenetimDurumu.Iyi ? Renk.Panel : Renk.Arka, ForeColor = Renk.Yazi, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            dugme.FlatAppearance.BorderColor = s.Durum == DenetimDurumu.Kotu ? Renk.Kirmizi : s.Durum == DenetimDurumu.Uyari ? Renk.Turuncu : Renk.Cizgi;
            dugme.Click += delegate { if (tiklaninca != null) tiklaninca(s); };
            Controls.Add(dugme);
        }
        new ToolTip().SetToolTip(this, s.Baslik + "\n" + s.Aciklama);
    }

    public Button Dugme { get { return dugme; } }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (dugme != null) dugme.Location = new Point(ClientSize.Width - dugme.Width - 12, (Height - dugme.Height) / 2);
    }

    static Color NoktaRengi(DenetimDurumu d)
    {
        switch (d)
        {
            case DenetimDurumu.Iyi: return Renk.Yesil;
            case DenetimDurumu.Uyari: return Renk.Turuncu;
            case DenetimDurumu.Kotu: return Renk.Kirmizi;
            case DenetimDurumu.Bilgi: return Color.FromArgb(100, 170, 255);
            default: return Renk.Cizgi;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(NoktaRengi(Satir.Durum))) g.FillEllipse(b, 16, Height / 2 - 6, 12, 12);
        int sag = dugme != null ? dugme.Left - 12 : ClientSize.Width - 12;
        var bayrak = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        TextRenderer.DrawText(g, Satir.Baslik, BaslikYazi, new Rectangle(42, 9, sag - 42, 20),
            Satir.Durum == DenetimDurumu.Bekliyor ? Renk.Soluk : Renk.Yazi, bayrak);
        TextRenderer.DrawText(g, Satir.Aciklama, AciklamaYazi, new Rectangle(42, 31, sag - 42, 18), Renk.Soluk, bayrak);
    }
}

class DurumPanosu : Panel
{
    public static int SonPuan = -1;   // tepsi özeti için (-1: henüz denetlenmedi)

    // Ana form bağlar.
    public Action<string> Git;                       // sekmeye git: "Sistem", "Disk", "Sürücü", "Sağlık", "Başlangıç", "Turbo"
    public Action SanalBellekAc, OyunaHazirla, NedenYavas;
    public Func<NvidiaBilgi> SurucuBilgisi;
    public Func<bool> SurucuDenetlendi;

    readonly BellekBekcisi bekci;
    readonly PuanHalkasi halka = new PuanHalkasi();
    readonly Panel satirlar = new Panel();
    readonly Label lblSon = new Label(), lblKazanc = new Label(), lblKimlik = new Label();
    readonly Button btnHazirla, btnYavas, btnKopyala, btnYenile;
    readonly Dictionary<string, DenetimSatiri> sonuclar = new Dictionary<string, DenetimSatiri>();
    List<KeyValuePair<string, string>> kimlik;
    DateTime sonDenetim = DateTime.MinValue;
    int nesil, bekleyen;
    bool kimlikOkunuyor;

    public DurumPanosu(BellekBekcisi bekci)
    {
        this.bekci = bekci;
        Dock = DockStyle.Fill;
        BackColor = Renk.Arka;

        // Sol sütun
        var sol = new Panel { Dock = DockStyle.Left, Width = 330, Padding = new Padding(14, 10, 8, 12) };
        halka.Dock = DockStyle.Top;
        halka.Height = 262;
        btnHazirla = new Button
        {
            Text = "Sistemi Optimize Et", Dock = DockStyle.Top, Height = 48, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = Renk.Vurgu, ForeColor = Color.White, Font = new Font("Segoe UI", 12.5f, FontStyle.Bold)
        };
        btnHazirla.FlatAppearance.BorderSize = 0;
        btnHazirla.Click += delegate { if (OyunaHazirla != null) OyunaHazirla(); };
        new ToolTip().SetToolTip(btnHazirla, "RAM'i boşaltır, gereksiz programları kapatır, geçici dosyaları siler. Ne yapacağını önce gösterir.");
        btnYavas = new Button
        {
            Text = "Neden yavaş?", Dock = DockStyle.Top, Height = 38, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = Renk.Panel, ForeColor = Renk.Yazi, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        btnYavas.FlatAppearance.BorderColor = Renk.Cizgi;
        btnYavas.Click += delegate { if (NedenYavas != null) NedenYavas(); };
        new ToolTip().SetToolTip(btnYavas, "Şu an bilgisayarı neyin yavaşlattığını birkaç saniye ölçüp sade bir dille söyler.");

        var kazancKart = new Panel { Dock = DockStyle.Top, Height = 66, Padding = new Padding(12, 8, 12, 6), BackColor = Renk.Panel };
        lblKazanc.Dock = DockStyle.Fill;
        lblKazanc.ForeColor = Renk.Yazi;
        lblKazanc.AutoEllipsis = true;
        kazancKart.Controls.Add(lblKazanc);
        kazancKart.Controls.Add(new Label { Dock = DockStyle.Top, Height = 18, ForeColor = Renk.Soluk, Text = "BUGÜNE KADAR", Font = new Font("Segoe UI", 8f, FontStyle.Bold) });

        var kimlikKart = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 10, 8), BackColor = Renk.Panel };
        var kimlikBaslik = new Panel { Dock = DockStyle.Top, Height = 26 };
        btnKopyala = new Button
        {
            Text = "Kopyala", Dock = DockStyle.Right, Width = 78, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = Renk.Arka, ForeColor = Renk.Yazi, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Enabled = false
        };
        btnKopyala.FlatAppearance.BorderColor = Renk.Cizgi;
        btnKopyala.Click += delegate { Kopyala(); };
        new ToolTip().SetToolTip(btnKopyala, "Forumda ya da Discord'da \"sistemim şu\" diye yapıştırmak için. Kullanıcı ve bilgisayar adı yoktur.");
        kimlikBaslik.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Renk.Soluk, Text = "SİSTEM KİMLİĞİ", Font = new Font("Segoe UI", 8f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        kimlikBaslik.Controls.Add(btnKopyala);
        lblKimlik.Dock = DockStyle.Fill;
        lblKimlik.ForeColor = Renk.Yazi;
        lblKimlik.Font = new Font("Segoe UI", 8.5f);
        lblKimlik.Text = "Okunuyor...";
        kimlikKart.Controls.Add(lblKimlik);
        kimlikKart.Controls.Add(kimlikBaslik);

        sol.Controls.Add(kimlikKart);
        sol.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        sol.Controls.Add(kazancKart);
        sol.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        sol.Controls.Add(btnYavas);
        sol.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
        sol.Controls.Add(btnHazirla);
        sol.Controls.Add(halka);

        // Sağ: başlık ve satırlar
        var sag = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 12, 12, 12) };
        var baslik = new Panel { Dock = DockStyle.Top, Height = 46 };
        btnYenile = new Button
        {
            Text = "Yeniden denetle", Dock = DockStyle.Right, Width = 130, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = Renk.Panel, ForeColor = Renk.Yazi, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btnYenile.FlatAppearance.BorderColor = Renk.Cizgi;
        btnYenile.Click += delegate { Denetle(); };
        var baslikYazi = new Label
        {
            Dock = DockStyle.Fill, ForeColor = Renk.Yazi, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "Sistem durumu",
            TextAlign = ContentAlignment.TopLeft
        };
        lblSon.Dock = DockStyle.Bottom;
        lblSon.Height = 20;
        lblSon.ForeColor = Renk.Soluk;
        lblSon.AutoEllipsis = true;
        var baslikSol = new Panel { Dock = DockStyle.Fill };
        baslikSol.Controls.Add(baslikYazi);
        baslikSol.Controls.Add(lblSon);
        baslik.Controls.Add(baslikSol);
        baslik.Controls.Add(btnYenile);

        satirlar.Dock = DockStyle.Fill;
        satirlar.AutoScroll = true;
        satirlar.HandleCreated += delegate { try { Yerel.SetWindowTheme(satirlar.Handle, "DarkMode_Explorer", null); } catch { } };
        sag.Controls.Add(satirlar);
        sag.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6 });
        sag.Controls.Add(baslik);

        Controls.Add(sag);
        Controls.Add(sol);

        KazanciYaz();
        Kazanc.Degisti += KazanciYaz;
        Disposed += delegate { Kazanc.Degisti -= KazanciYaz; };
    }

    void KazanciYaz()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(new Action(KazanciYaz)); } catch { } return; }
        lblKazanc.Text = Kazanc.Metin();
        lblKazanc.ForeColor = Kazanc.IsSayisi == 0 ? Renk.Soluk : Renk.Yazi;
    }

    // Sekme görününce: ilk kez ya da son denetimden 2 dakika geçtiyse yeniden denetle.
    public void Gosterildi()
    {
        var f = FindForm();
        if (f == null || !f.Visible) return;   // gizli açılışta (tepside) denetim yapılmaz; pencere görününce yapılır
        if (kimlik == null && !kimlikOkunuyor) KimligiOku();
        if (bekleyen == 0 && DateTime.Now - sonDenetim > TimeSpan.FromMinutes(2)) Denetle();
    }

    // Sürücü denetimi bitince (ana form çağırır) yalnızca o satır tazelenir.
    public void SurucuDegisti()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(new Action(SurucuDegisti)); } catch { } return; }
        if (bekleyen > 0 && !sonuclar.ContainsKey("surucu")) return;
        sonuclar["surucu"] = SurucuSatiri();
        SatirlariYaz();
        PuanYaz();
    }

    DenetimSatiri SurucuSatiri()
    {
        bool denetlendi = SurucuDenetlendi != null && SurucuDenetlendi();
        return SistemDenetimi.Surucu(SurucuBilgisi != null ? SurucuBilgisi() : null, denetlendi);
    }

    public void Denetle()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(new Action(Denetle)); } catch { } return; }
        if (bekleyen > 0) return;
        int bu = ++nesil;
        sonuclar.Clear();
        btnYenile.Enabled = false;
        lblSon.Text = "Denetleniyor...";
        halka.Ayarla(-1, "Denetleniyor...", "");

        // Arayüzde okunanlar hemen; gerisi arka planda ve birbirini beklemeden.
        sonuclar["surucu"] = SurucuSatiri();
        sonuclar["sizinti"] = SistemDenetimi.Sizinti(bekci);
        sonuclar["bekci"] = SistemDenetimi.Bekci(bekci);
        var isler = new Dictionary<string, Func<DenetimSatiri>>
        {
            { "sanal", SistemDenetimi.SanalBellek }, { "ram", SistemDenetimi.Ram }, { "disk", SistemDenetimi.Disk },
            { "saglik", SistemDenetimi.Saglik }, { "pil", SistemDenetimi.Pil }, { "baslangic", SistemDenetimi.Baslangic },
            { "turbo", SistemDenetimi.Turbo }, { "cokme", SistemDenetimi.Cokme }
        };
        bekleyen = isler.Count;
        foreach (var kv in isler)
        {
            sonuclar[kv.Key] = DenetimSatiri.Yap(DenetimDurumu.Bekliyor, 0, BekleyenBaslik(kv.Key), "Denetleniyor...", null, null);
            Calistir(kv.Key, kv.Value, bu);
        }
        SatirlariYaz();
    }

    static string BekleyenBaslik(string k)
    {
        switch (k)
        {
            case "sanal": return "Sanal bellek";
            case "ram": return "RAM";
            case "disk": return "Boş yer ve gereksiz dosyalar";
            case "saglik": return "Disk sağlığı";
            case "pil": return "Pil sağlığı";
            case "baslangic": return "Açılışta başlayan programlar";
            case "turbo": return "Oyun ayarları";
            case "cokme": return "Çökme geçmişi";
            default: return k;
        }
    }

    async void Calistir(string anahtar, Func<DenetimSatiri> denetim, int bu)
    {
        DenetimSatiri s = null;
        try { s = await Task.Run(denetim); }
        catch { s = null; }
        if (bu != nesil || IsDisposed) return;
        sonuclar[anahtar] = s;
        bekleyen--;
        SatirlariYaz();
        PuanYaz();
        if (bekleyen == 0)
        {
            sonDenetim = DateTime.Now;
            btnYenile.Enabled = true;
            lblSon.Text = "Son denetim " + sonDenetim.ToString("HH:mm") + " · hiçbir şey kendiliğinden değiştirilmez";
        }
    }

    int Puan()
    {
        return Math.Max(0, 100 - sonuclar.Values.Where(s => s != null).Sum(s => s.Kesinti));
    }

    void PuanYaz()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(new Action(PuanYaz)); } catch { } return; }
        if (bekleyen > 0) return;
        int p = Puan();
        SonPuan = p;
        int oneri = sonuclar.Values.Count(s => s != null && (s.Durum == DenetimDurumu.Uyari || s.Durum == DenetimDurumu.Kotu));
        string durum = p >= 90 ? "Harika" : p >= 75 ? "İyi" : p >= 50 ? "İdare eder" : "Bakım gerekiyor";
        halka.Ayarla(p, durum, oneri == 0 ? "Her şey yolunda" : oneri + " öneri var");
    }

    void SatirlariYaz()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(new Action(SatirlariYaz)); } catch { } return; }
        int kaydirma = -satirlar.AutoScrollPosition.Y;
        satirlar.SuspendLayout();
        try
        {
            foreach (Control c in satirlar.Controls.Cast<Control>().ToList()) c.Dispose();
            satirlar.Controls.Clear();
            // Önce dikkat isteyenler (kırmızı, turuncu), sonra bilgi ve iyiler; kendi içinde sabit sırayla.
            var sirali = SistemDenetimi.Sira.Where(k => sonuclar.ContainsKey(k) && sonuclar[k] != null)
                .Select(k => sonuclar[k])
                .OrderBy(s => s.Durum == DenetimDurumu.Kotu ? 0 : s.Durum == DenetimDurumu.Uyari ? 1 : s.Durum == DenetimDurumu.Bekliyor ? 3 : 2)
                .ToList();
            for (int i = sirali.Count - 1; i >= 0; i--)   // Dock=Top: son eklenen en üstte
            {
                satirlar.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6 });
                satirlar.Controls.Add(new DenetimKutusu(sirali[i], Tikla));
            }
        }
        finally { satirlar.ResumeLayout(true); }
        try { satirlar.AutoScrollPosition = new Point(0, kaydirma); } catch { }
    }

    async void Tikla(DenetimSatiri s)
    {
        switch (s.Hedef)
        {
            case "sanal":
                if (SanalBellekAc != null) SanalBellekAc();
                sonuclar["sanal"] = SistemDenetimi.SanalBellek();
                SatirlariYaz(); PuanYaz();
                return;
            case "bekci":
                if (bekci == null) return;
                bekci.ArkaPlandaCalis = true;
                bekci.WindowsIleBaslat = true;
                sonuclar["bekci"] = SistemDenetimi.Bekci(bekci);
                SatirlariYaz(); PuanYaz();
                return;
            case "disk-omur":
                lblSon.Text = "Disk ömrü okunuyor; Windows yönetici izni isteyecek...";
                int kod = await Task.Run(() => DiskSagligi.OmruOku());
                if (IsDisposed) return;
                if (kod == 0)
                {
                    sonuclar["saglik"] = await Task.Run(() => SistemDenetimi.Saglik());
                    lblSon.Text = "Disk ömrü okundu.";
                }
                else lblSon.Text = kod == -2 ? "Yönetici izni verilmedi; disk ömrü okunmadı." : "Disk ömrü okunamadı (bu disk bildirmiyor olabilir).";
                SatirlariYaz(); PuanYaz();
                return;
            default:
                if (Git != null && !string.IsNullOrEmpty(s.Hedef)) Git(s.Hedef);
                return;
        }
    }

    async void KimligiOku()
    {
        kimlikOkunuyor = true;
        try { kimlik = await Task.Run(() => SistemKimligi.Topla()); } catch { kimlik = new List<KeyValuePair<string, string>>(); }
        kimlikOkunuyor = false;
        if (IsDisposed) return;
        var sb = new StringBuilder();
        foreach (var k in kimlik) sb.Append(k.Key).Append(":  ").Append(k.Value).Append("\n");
        lblKimlik.Text = sb.Length > 0 ? sb.ToString().TrimEnd() : "Okunamadı.";
        btnKopyala.Enabled = kimlik.Count > 0;
    }

    void Kopyala()
    {
        if (kimlik == null || kimlik.Count == 0) return;
        try
        {
            Clipboard.SetText(SistemKimligi.Metin(kimlik));
            btnKopyala.Text = "Kopyalandı";
            var t = new Timer { Interval = 2000 };
            t.Tick += delegate { t.Dispose(); if (!btnKopyala.IsDisposed) btnKopyala.Text = "Kopyala"; };
            t.Start();
        }
        catch { }
    }
}
