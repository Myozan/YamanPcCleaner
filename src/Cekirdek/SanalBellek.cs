// Sanal bellek (sayfa dosyasi) okuma, oneri ve geri alinabilir uygulama.

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

// Sayfa dosyası ayarı: HKLM\...\Memory Management\PagingFiles. Her satır "C:\pagefile.sys ilkMB enCokMB";
// "?:\pagefile.sys" = Windows otomatik yönetiyor, "0 0" = o sürücüde Windows yönetiyor, hiç satır yok = kapalı.
static class SanalBellek
{
    const string Anahtar = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
    public static readonly string Surucu = Path.GetPathRoot(Environment.SystemDirectory).TrimEnd('\\');

    // RAM'e göre boyut: RAM + sayfa dosyası en az ~32 GB olsun (güncel oyunlar bunu ister), 32 GB'tan az RAM'de
    // başlangıç en az 16 GB (Windows'un kendi ayarladığından küçük olmasın), büyük RAM'de RAM'in dörtte biri yeter.
    // Başlangıç 8–24 GB, üst sınır bunun iki katı (16–32 GB).
    //   8 GB RAM → 24 / 32 GB    16 / 24 GB RAM → 16 / 32 GB    32 GB RAM → 8 / 16 GB    64 GB RAM → 16 / 32 GB
    public static void Hedef(ulong ramBayt, out int ilkMB, out int enCokMB)
    {
        int ramGB = (int)Math.Round(ramBayt / 1073741824.0);
        int ilkGB = Math.Max(8, Math.Min(24, Math.Max(ramGB < 32 ? 16 : 0, Math.Max(32 - ramGB, ramGB / 4))));
        ilkMB = ilkGB * 1024;
        enCokMB = Math.Max(16, Math.Min(32, ilkGB * 2)) * 1024;
    }

    public static string[] Oku()
    {
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(Anahtar))
                return (k == null ? null : k.GetValue("PagingFiles") as string[]) ?? new string[0];
        }
        catch { return new string[0]; }
    }

    static string[] Parcala(string satir)
    {
        return satir.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string Anlat(string[] satirlar)
    {
        var parcalar = new List<string>();
        foreach (var s in satirlar)
        {
            var p = Parcala(s);
            if (p.Length == 0) continue;
            if (p[0].StartsWith("?:")) { parcalar.Add("Windows otomatik yönetiyor"); continue; }
            string sur = p[0].Length >= 2 ? p[0].Substring(0, 2) : p[0];
            int a, b;
            if (p.Length >= 3 && int.TryParse(p[1], out a) && int.TryParse(p[2], out b) && a > 0)
                parcalar.Add(string.Format("{0} sürücüsünde {1:0.#}–{2:0.#} GB", sur, a / 1024.0, b / 1024.0));
            else
                parcalar.Add(sur + " sürücüsünde, boyutu Windows yönetiyor");
        }
        return parcalar.Count == 0 ? "KAPALI (programların \"bellek yetersiz\" diye kapanmasının en sık sebebi)" : string.Join(", ", parcalar);
    }

    // Elle verilmiş başlangıç boyutlarının toplamı (MB); Windows yönetiyorsa ya da kapalıysa 0.
    public static int ElleIlkMB(string[] satirlar)
    {
        int toplam = 0;
        foreach (var s in satirlar)
        {
            var p = Parcala(s);
            int a;
            if (p.Length >= 3 && !p[0].StartsWith("?:") && int.TryParse(p[1], out a) && a > 0) toplam += a;
        }
        return toplam;
    }

    // Büyütmeden önceki ayar burada durur; dosya varsa düğme "geri al" olur, geri alınınca silinir.
    static readonly string YedekYolu = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YamanPcCleaner", "sanal-bellek-yedek.txt");

    public static bool YedekVar { get { return File.Exists(YedekYolu); } }

    public static void Yedekle(string[] satirlar)
    {
        try
        {
            if (File.Exists(YedekYolu)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(YedekYolu));
            File.WriteAllLines(YedekYolu, new[] { "# Yaman PC Cleaner öncesi sayfa dosyası ayarı (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")" }.Concat(satirlar));
        }
        catch { }
    }

    public static string[] YedekOku()
    {
        try { return File.ReadAllLines(YedekYolu).Where(s => s.Trim().Length > 0 && !s.StartsWith("#")).ToArray(); }
        catch { return null; }
    }

    public static void YedekSil() { try { File.Delete(YedekYolu); } catch { } }

    // Yönetici olarak çalışır. Sistem sürücüsündeki ve otomatik satırları yenisiyle değiştirir,
    // diğer sürücülerdeki sayfa dosyalarına dokunmaz. 0 = tamam, 1 = hata.
    public static int Uygula(int ilkMB, int enCokMB)
    {
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(Anahtar, true))
            {
                var eski = (k.GetValue("PagingFiles") as string[]) ?? new string[0];
                var yeni = eski.Where(s =>
                {
                    var t = s.Trim();
                    return t.Length > 0 && !t.StartsWith("?:") && !t.StartsWith(Surucu, StringComparison.OrdinalIgnoreCase);
                }).ToList();
                yeni.Insert(0, string.Format("{0}\\pagefile.sys {1} {2}", Surucu, ilkMB, enCokMB));
                k.SetValue("PagingFiles", yeni.ToArray(), RegistryValueKind.MultiString);
            }
            return 0;
        }
        catch { return 1; }
    }

    // Yönetici olarak çalışır; yedekteki satırları olduğu gibi geri yazar. Yalnızca sayfa dosyası
    // satırı biçimindekileri kabul eder. 0 = tamam, 1 = hata.
    public static int GeriYukle(string[] satirlar)
    {
        var bicim = new System.Text.RegularExpressions.Regex(@"^([A-Za-z]|\?):\\pagefile\.sys(\s+\d+\s+\d+)?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (satirlar.Any(s => !bicim.IsMatch(s.Trim()))) return 1;
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(Anahtar, true))
                k.SetValue("PagingFiles", satirlar.Select(s => s.Trim()).ToArray(), RegistryValueKind.MultiString);
            return 0;
        }
        catch { return 1; }
    }
}
