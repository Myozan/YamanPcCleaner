// Disk temizligi: guvenli ve derin temizlik listesi, boyut hesabi ve silme.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

// Silinecek her şeyin tek listesi. Yönetici gerektiren maddeler, exe kendini "--disk-temizle" ile
// yönetici olarak açınca aynı listeden anahtarla seçilip silinir.
static class DiskTemizlik
{
    // cleanmgr'nin (Disk Temizleme) kendi güvenli temizlikleri; "Windows ESD" bilerek yok (PC'yi sıfırla için gerekir).
    static readonly string[] WindowsTemizlikleri =
    {
        "Previous Installations", "Update Cleanup", "Temporary Setup Files", "Windows Upgrade Log Files",
        "Setup Log Files", "Device Driver Packages", "Delivery Optimization Files"
    };

    public static List<Temizlik> Liste(bool derin)
    {
        string yerel = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string yerelLow = Path.Combine(Path.GetDirectoryName(yerel), "LocalLow");
        string dolasan = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string ortak = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string sistem = Path.GetPathRoot(Environment.SystemDirectory);
        int gun = derin ? 0 : 1;
        var steam = SteamKutuphaneleri();
        var l = new List<Temizlik>();
        Temizlik t;

        t = Ekle(l, "temp", "Geçici dosyalar (%TEMP%)", derin ? "Programların geçici dosyaları, yenileri dahil"
                                                              : "Programların bıraktığı geçici dosyalar (son 24 saat hariç)", true);
        t.EnAzGun = gun;
        t.Klasorler.Add(Path.GetTempPath());

        Chromium(l, "chrome", "Chrome önbelleği", Path.Combine(yerel, @"Google\Chrome\User Data"), "chrome");
        Chromium(l, "edge", "Edge önbelleği", Path.Combine(yerel, @"Microsoft\Edge\User Data"), "msedge");
        Chromium(l, "brave", "Brave önbelleği", Path.Combine(yerel, @"BraveSoftware\Brave-Browser\User Data"), "brave");
        Chromium(l, "operagx", "Opera GX önbelleği", Path.Combine(yerel, @"Opera Software\Opera GX Stable"), "opera");
        Chromium(l, "opera", "Opera önbelleği", Path.Combine(yerel, @"Opera Software\Opera Stable"), "opera");

        t = Ekle(l, "firefox", "Firefox önbelleği", "Site önbelleği; şifreler, çerezler ve geçmiş silinmez", true);
        t.Surecler = new[] { "firefox" };
        foreach (var p in AltKlasorler(Path.Combine(yerel, @"Mozilla\Firefox\Profiles"))) t.Klasorler.Add(Path.Combine(p, "cache2"));

        t = Ekle(l, "discord", "Discord önbelleği", "Resim ve video önbelleği; mesajlar ve ayarlar silinmez", true);
        t.Surecler = new[] { "Discord" };
        foreach (var a in new[] { "Cache", "Code Cache", "GPUCache" }) t.Klasorler.Add(Path.Combine(dolasan, "discord", a));

        t = Ekle(l, "steamweb", "Steam mağaza önbelleği", "Mağaza ve topluluk sayfalarının önbelleği; oyunlar silinmez", true);
        t.Surecler = new[] { "steam", "steamwebhelper" };
        t.Klasorler.Add(Path.Combine(yerel, @"Steam\htmlcache"));
        if (steam.Count > 0) t.Klasorler.Add(Path.Combine(steam[0], @"appcache\httpcache"));

        t = Ekle(l, "epicweb", "Epic Games önbelleği", "Başlatıcının web önbelleği; oyunlar silinmez", true);
        t.Surecler = new[] { "EpicGamesLauncher" };
        foreach (var p in AltKlasorler(Path.Combine(yerel, @"EpicGamesLauncher\Saved")))
            if (Path.GetFileName(p).StartsWith("webcache", StringComparison.OrdinalIgnoreCase)) t.Klasorler.Add(p);

        t = Ekle(l, "cokme", "Program çökme kayıtları", "Bir oyunun neden çöktüğünü bulmak için gerekebilir", derin);
        t.Klasorler.Add(Path.Combine(yerel, "CrashDumps"));
        t.Klasorler.Add(Path.Combine(yerel, @"Microsoft\Windows\WER\ReportArchive"));
        t.Klasorler.Add(Path.Combine(yerel, @"Microsoft\Windows\WER\ReportQueue"));

        t = Ekle(l, "geridonusum", "Geri dönüşüm kutusu", "Çöpe atılan dosyalar kalıcı olarak silinir", false);
        t.Ozel = "geridonusum";

        if (derin)
        {
            t = Ekle(l, "shader", "Ekran kartı shader önbellekleri", "Oyunlar ilk açılışta takılır, shader'lar yeniden derlenir", true);
            t.Derin = true;
            foreach (var y in new[] {
                Path.Combine(yerel, @"NVIDIA\DXCache"), Path.Combine(yerel, @"NVIDIA\GLCache"),
                Path.Combine(yerelLow, @"NVIDIA\PerDriverVersion\DXCache"), Path.Combine(yerelLow, @"NVIDIA\PerDriverVersion\GLCache"),
                Path.Combine(yerel, @"AMD\DxCache"), Path.Combine(yerel, @"AMD\DxcCache"), Path.Combine(yerel, @"AMD\GLCache"),
                Path.Combine(yerel, @"AMD\VkCache"), Path.Combine(yerelLow, @"Intel\ShaderCache"), Path.Combine(yerel, "D3DSCache") })
                t.Klasorler.Add(y);

            t = Ekle(l, "steamshader", "Steam oyun shader önbelleği", "Oyunlar ilk açılışta takılır; Steam gerekirse yeniden indirir", true);
            t.Derin = true;
            foreach (var k in steam) t.Klasorler.Add(Path.Combine(k, @"steamapps\shadercache"));
        }

        t = Ekle(l, "wintemp", "Windows geçici dosyaları", "Windows ve kurulumların geçici dosyaları", true);
        t.Yonetici = true; t.EnAzGun = gun;
        t.Klasorler.Add(Path.Combine(win, "Temp"));

        t = Ekle(l, "wu", "Windows Update indirmeleri", "Kurulmuş güncellemelerin indirme dosyaları", true);
        t.Yonetici = true; t.EnAzGun = gun;
        t.Klasorler.Add(Path.Combine(win, @"SoftwareDistribution\Download"));

        t = Ekle(l, "sistemcokme", "Sistem çökme dökümleri", "Mavi ekran sebebini bulmak için gerekebilir", derin);
        t.Yonetici = true;
        t.Klasorler.Add(Path.Combine(win, "Minidump"));
        t.Klasorler.Add(Path.Combine(win, "LiveKernelReports"));
        t.Klasorler.Add(Path.Combine(ortak, @"Microsoft\Windows\WER\ReportArchive"));
        t.Klasorler.Add(Path.Combine(ortak, @"Microsoft\Windows\WER\ReportQueue"));
        t.Dosyalar.Add(Path.Combine(win, "MEMORY.DMP"));

        if (derin)
        {
            t = Ekle(l, "windows", "Eski Windows ve güncelleme kalıntıları",
                "Windows.old, güncelleme yedekleri, Teslim İyileştirme, eski sürücüler; geri alma kaybolur. Windows'un aracıyla, uzun sürebilir", true);
            t.Yonetici = true; t.Derin = true; t.Ozel = "windows";
            t.Klasorler.Add(Path.Combine(sistem, "Windows.old"));

            t = Ekle(l, "winlog", "Windows günlük dosyaları", "Kurulum ve bakım günlükleri; sorun araştırmak zorlaşır", true);
            t.Yonetici = true; t.Derin = true;
            t.Klasorler.Add(Path.Combine(win, "Logs"));

            long hazirda = DosyaBoyutu(sistem, "hiberfil.sys");
            if (hazirda > 0)
            {
                t = Ekle(l, "hazirda", "Hazırda bekletme dosyası",
                    "Hazırda bekletme ve Hızlı Başlatma kapanır; geri açmak için yönetici CMD'de: powercfg /h on", false);
                t.Yonetici = true; t.Derin = true; t.Ozel = "hazirda"; t.Boyut = hazirda;
            }
        }

        l.RemoveAll(x => x.Ozel == null && !x.Klasorler.Any(Directory.Exists) && !x.Dosyalar.Any(File.Exists));
        return l;
    }

    static Temizlik Ekle(List<Temizlik> l, string anahtar, string ad, string aciklama, bool varsayilan)
    {
        var t = new Temizlik { Anahtar = anahtar, Ad = ad, Aciklama = aciklama, Varsayilan = varsayilan };
        l.Add(t);
        return t;
    }

    // Chromium tabanlı tarayıcılar: kökte ya da profil klasörlerinde (Default, Profile 1...) önbellek klasörleri.
    static void Chromium(List<Temizlik> l, string anahtar, string ad, string kok, string surec)
    {
        if (!Directory.Exists(kok)) return;
        var t = Ekle(l, anahtar, ad, "Site önbelleği; şifreler, çerezler ve geçmiş silinmez", true);
        t.Surecler = new[] { surec };
        var profiller = new List<string> { kok };
        profiller.AddRange(AltKlasorler(kok));
        foreach (var p in profiller)
            foreach (var a in new[] { "Cache", "Code Cache", "GPUCache", "ShaderCache", "GrShaderCache", "DawnCache", "DawnGraphiteCache", "DawnWebGPUCache" })
            {
                var y = Path.Combine(p, a);
                if (Directory.Exists(y)) t.Klasorler.Add(y);
            }
    }

    static string[] AltKlasorler(string yol)
    {
        try { return Directory.Exists(yol) ? Directory.GetDirectories(yol) : new string[0]; }
        catch { return new string[0]; }
    }

    // Kök klasörü listeleyerek boyut okur; hiberfil.sys gibi kilitli dosyalarda da çalışır.
    static long DosyaBoyutu(string klasor, string ad)
    {
        try
        {
            var f = new DirectoryInfo(klasor).GetFiles(ad);
            return f.Length > 0 ? f[0].Length : 0;
        }
        catch { return 0; }
    }

    static List<string> SteamKutuphaneleri()
    {
        var l = new List<string>();
        string steam = null;
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                if (k != null) steam = k.GetValue("SteamPath") as string;
        }
        catch { }
        if (string.IsNullOrEmpty(steam)) return l;
        l.Add(steam.Replace('/', '\\'));
        try
        {
            string vdf = Path.Combine(l[0], @"steamapps\libraryfolders.vdf");
            if (File.Exists(vdf))
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                {
                    string y = m.Groups[1].Value.Replace(@"\\", @"\");
                    if (!l.Any(x => x.TrimEnd('\\').Equals(y.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))) l.Add(y);
                }
        }
        catch { }
        return l;
    }

    public static void Hesapla(Temizlik t)
    {
        t.AcikSurec = null;
        foreach (var s in t.Surecler)
        {
            var ps = Process.GetProcessesByName(s);
            bool acik = ps.Length > 0;
            foreach (var p in ps) p.Dispose();
            if (acik) { t.AcikSurec = s; break; }
        }
        if (t.Ozel == "geridonusum")
        {
            var b = new Yerel.SHQUERYRBINFO();
            b.cbSize = Marshal.SizeOf(b);
            t.Boyut = Yerel.SHQueryRecycleBin(null, ref b) == 0 ? b.i64Size : -1;
            return;
        }
        if (t.Ozel == "hazirda") return;   // boyutu listeyi kurarken okundu

        long toplam = 0;
        bool okunamayan = false;
        foreach (var k in t.Klasorler)
        {
            long b = Gez(k, t.EnAzGun, false);
            if (b < 0) okunamayan = true; else toplam += b;
        }
        foreach (var f in t.Dosyalar)
        {
            try { var fi = new FileInfo(f); if (fi.Exists) toplam += fi.Length; } catch { okunamayan = true; }
        }
        t.Boyut = okunamayan && toplam == 0 ? -1 : toplam;
    }

    public static long Temizle(Temizlik t)
    {
        switch (t.Ozel)
        {
            case "geridonusum":
                Hesapla(t);
                Yerel.SHEmptyRecycleBin(IntPtr.Zero, null, 0x7);   // onay, ilerleme penceresi ve ses yok
                return Math.Max(0, t.Boyut);
            case "hazirda":
                Calistir("powercfg.exe", "/h off");
                return 0;
            case "windows":
                WindowsTemizligi();
                return 0;
        }
        long toplam = 0;
        foreach (var k in t.Klasorler) toplam += Math.Max(0, Gez(k, t.EnAzGun, true));
        foreach (var f in t.Dosyalar)
        {
            try { var fi = new FileInfo(f); if (fi.Exists) { long b = fi.Length; fi.Delete(); toplam += b; } } catch { }
        }
        return toplam;
    }

    // Yönetici olarak çalışır: args[0] "derin"/"normal", kalanlar madde anahtarları.
    // "sanal=ilkMB,enCokMB" sayfa dosyasını da ayarlar; dönen kod o işin sonucudur (yoksa 0).
    public static int YoneticiTemizle(string[] args)
    {
        if (args.Length < 1) return 1;
        bool derin = args[0] == "derin";
        int sonuc = 0;
        var anahtarlar = new HashSet<string>();
        foreach (var a in args.Skip(1))
        {
            var p = a.Split('=', ',');
            int ilk, enCok;
            if (p.Length == 3 && p[0] == "sanal" && int.TryParse(p[1], out ilk) && int.TryParse(p[2], out enCok))
                sonuc = SanalBellek.Uygula(ilk, enCok);
            else
                anahtarlar.Add(a);
        }
        // Normal modda da hazırda bekletme gibi yalnızca derin listede olan işler istenebilir (Full optimize).
        var liste = Liste(derin);
        if (!derin) liste.AddRange(Liste(true).Where(t => t.Derin && !liste.Any(x => x.Anahtar == t.Anahtar)));
        foreach (var t in liste)
            if (t.Yonetici && anahtarlar.Contains(t.Anahtar))
                try { Temizle(t); } catch { }
        return sonuc;
    }

    public static long HazirdaBoyutu() { return DosyaBoyutu(Path.GetPathRoot(Environment.SystemDirectory), "hiberfil.sys"); }

    // cleanmgr'ye hangi temizlikleri yapacağını kayıt defterinden söyleyip sessiz çalıştırır, sonra izleri siler.
    static void WindowsTemizligi()
    {
        const string Kok = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\";
        var ayarlanan = new List<string>();
        foreach (var ad in WindowsTemizlikleri)
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(Kok + ad, true))
                    if (k != null) { k.SetValue("StateFlags0777", 2, RegistryValueKind.DWord); ayarlanan.Add(ad); }
            }
            catch { }
        }
        if (ayarlanan.Count > 0) Calistir("cleanmgr.exe", "/sagerun:777");
        foreach (var ad in ayarlanan)
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(Kok + ad, true))
                    if (k != null) k.DeleteValue("StateFlags0777", false);
            }
            catch { }
        }
    }

    static void Calistir(string program, string argumanlar)
    {
        try
        {
            var bilgi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, program), argumanlar)
            {
                UseShellExecute = false, CreateNoWindow = true
            };
            using (var p = Process.Start(bilgi)) p.WaitForExit();
        }
        catch { }
    }

    // Klasörün içini gezer; bağlantı noktalarına (junction/symlink) girmez, böylece başka yerlere taşmaz.
    // sil=true ise siler. Dönen: silinen (ya da sayılan) bayt; kök okunamıyorsa -1. Kökün kendisi silinmez.
    static long Gez(string kok, int enAzGun, bool sil)
    {
        if (!Directory.Exists(kok)) return 0;
        try { return GezIc(new DirectoryInfo(kok), DateTime.UtcNow.AddDays(-enAzGun), sil, true); }
        catch { return -1; }
    }

    static long GezIc(DirectoryInfo d, DateTime sinir, bool sil, bool kok)
    {
        FileSystemInfo[] ogeler;
        try { ogeler = d.GetFileSystemInfos(); }
        catch { if (kok) throw; return 0; }
        long toplam = 0;
        foreach (var o in ogeler)
        {
            try
            {
                if ((o.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                var alt = o as DirectoryInfo;
                if (alt != null)
                {
                    toplam += GezIc(alt, sinir, sil, false);
                    // Tarih, gezmeden önceki değerdir (FileSystemInfo önbellekler); boş kaldıysa silinir.
                    if (sil && alt.LastWriteTimeUtc <= sinir) { try { alt.Delete(false); } catch { } }
                    continue;
                }
                var f = (FileInfo)o;
                if (f.LastWriteTimeUtc > sinir) continue;
                long boyut = f.Length;
                if (sil)
                {
                    if ((f.Attributes & FileAttributes.ReadOnly) != 0) f.Attributes = FileAttributes.Normal;
                    f.Delete();   // kullanımdaysa hata verir, atlanır
                }
                toplam += boyut;
            }
            catch { }
        }
        return toplam;
    }
}
