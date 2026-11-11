// NVIDIA surucu denetimi, indirmesi, imza dogrulamasi ve kurulumu.

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

class NvidiaBilgi
{
    public string KartAdi, KuruluSurum, KuruluTarih;            // KuruluSurum NVIDIA biçiminde: 617.42
    public string SonSurum, SonTarih, Boyut, IndirmeAdresi, DetayAdresi;
    public string Uygulama;                                     // NVIDIA App / GeForce Experience yolu; varsa güncellemeyi o yapar
    public string Hata;

    public bool YeniVar
    {
        get
        {
            double k, s;
            return Hata == null && SonSurum != null
                && double.TryParse(KuruluSurum, NumberStyles.Float, CultureInfo.InvariantCulture, out k)
                && double.TryParse(SonSurum, NumberStyles.Float, CultureInfo.InvariantCulture, out s) && s > k;
        }
    }
}

// NVIDIA ekran kartı sürücüsünü NVIDIA'nın kendi sitesindeki sürücü aramasıyla karşılaştırır. Bu adresler
// NVIDIA'nın resmi olarak belgelediği bir API değil (sitenin kendi arama sayfası kullanıyor); değişirse
// yalnızca bu bölüm "öğrenilemedi" der.
static class NvidiaSurucu
{
    const string UrunListesi = "https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=3";
    const string Arama = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php" +
                         "?func=DriverManualLookup&psid={0}&pfid={1}&osID={2}&languageCode=1033&isWHQL=1&dch=1&upCRD=0&sort1=0&numberOfResults=1";

    // null: bu bilgisayarda NVIDIA kart yok.
    // Windows derleme numarası kayıt defterinden: manifestsiz .NET Framework exe'de Environment.OSVersion
    // Windows 11'de de 6.2.9200 der, ona güvenilmez.
    static int WindowsDerlemesi()
    {
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                int d;
                if (k != null && int.TryParse(Convert.ToString(k.GetValue("CurrentBuildNumber")), out d)) return d;
            }
        }
        catch { }
        return Environment.OSVersion.Version.Build;
    }

    public static NvidiaBilgi Kontrol()
    {
        string ad, surum, tarih;
        if (!KartBul(out ad, out surum, out tarih)) return null;
        var b = new NvidiaBilgi { KartAdi = ad, KuruluSurum = SurumCevir(surum), KuruluTarih = tarih, Uygulama = UygulamaBul() };
        try
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;   // TLS 1.2 (.NET 4.0 hedefinde ad yok)
            string hedef = Sade(ad), psid = null, pfid = null;
            foreach (Match m in Regex.Matches(Getir(UrunListesi), "<LookupValue ParentID=\"(\\d+)\">\\s*<Name>([^<]*)</Name>\\s*<Value>(\\d+)</Value>"))
                if (Sade(WebUtility.HtmlDecode(m.Groups[2].Value)) == hedef) { psid = m.Groups[1].Value; pfid = m.Groups[3].Value; break; }
            if (pfid == null) { b.Hata = "Bu kart NVIDIA'nın sürücü listesinde bulunamadı."; return b; }

            // Windows 11 / Windows 10 64 bit / Windows 10 32 bit (Windows 11'in 32 bit sürümü yok).
            int isletim = !Environment.Is64BitOperatingSystem ? 56 : WindowsDerlemesi() >= 22000 ? 135 : 57;
            string json = Getir(string.Format(Arama, psid, pfid, isletim));
            b.SonSurum = Alan(json, "Version");
            b.SonTarih = Alan(json, "ReleaseDateTime");
            b.Boyut = Alan(json, "DownloadURLFileSize");
            b.IndirmeAdresi = Alan(json, "DownloadURL");
            b.DetayAdresi = Alan(json, "DetailsURL");
            if (b.SonSurum == null || b.IndirmeAdresi == null) b.Hata = "NVIDIA'dan sürüm bilgisi alınamadı.";
        }
        catch (Exception e) { b.Hata = "NVIDIA'ya bağlanılamadı (" + e.Message + ")."; }
        return b;
    }

    static string Getir(string adres)
    {
        using (var w = new WebClient())
        {
            w.Encoding = Encoding.UTF8;
            w.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 YamanPcCleaner";
            return w.DownloadString(adres);
        }
    }

    // Bu bilgisayarda takılı, sürücüsü yüklü bir NVIDIA kartı var mı (internete gitmez).
    public static bool KartVar()
    {
        string ad, surum, tarih;
        return KartBul(out ad, out surum, out tarih);
    }

    // Takılı ve sürücüsü yüklü NVIDIA kartları içinden en çok VRAM'i olan: ad, Windows biçiminde sürüm
    // (32.0.16.1742), tarih. Sökülmüş kartların kalıntı girdileri sayılmaz.
    static bool KartBul(out string ad, out string surum, out string tarih)
    {
        ad = surum = tarih = null;
        long enCok = -1;
        try
        {
            var takili = EkranKartlari.Takililar();
            using (var sinif = Registry.LocalMachine.OpenSubKey(EkranKartlari.SinifAnahtari))
            {
                if (sinif == null) return false;
                foreach (var alt in sinif.GetSubKeyNames())
                {
                    if (alt.Length != 4) continue;
                    if (takili != null && !takili.Contains(alt)) continue;
                    try
                    {
                        using (var k = sinif.OpenSubKey(alt))
                        {
                            string d = k == null ? null : k.GetValue("DriverDesc") as string;
                            if (d == null || d.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) < 0) continue;
                            string s = k.GetValue("DriverVersion") as string;
                            long vram = EkranKartlari.Vram(k);
                            if (s == null || vram <= enCok) continue;
                            enCok = vram;
                            ad = d.Trim();
                            surum = s;
                            DateTime t;
                            string ham = k.GetValue("DriverDate") as string;
                            tarih = DateTime.TryParseExact(ham, "M-d-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out t)
                                ? t.ToString("dd.MM.yyyy") : ham;
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
        return surum != null;
    }

    // Windows sürümünün son 5 hanesi NVIDIA sürümüdür: 32.0.16.1742 → 61742 → 617.42
    static string SurumCevir(string windows)
    {
        string h = new string(windows.Where(char.IsDigit).ToArray());
        if (h.Length < 5) return windows;
        h = h.Substring(h.Length - 5);
        return h.Substring(0, 3) + "." + h.Substring(3);
    }

    static string Sade(string ad)
    {
        ad = ad.Trim();
        if (ad.StartsWith("NVIDIA ", StringComparison.OrdinalIgnoreCase)) ad = ad.Substring(7);
        return ad.ToLowerInvariant();
    }

    static string Alan(string json, string ad)
    {
        var m = Regex.Match(json, "\"" + ad + "\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success && m.Groups[1].Value.Length > 0 ? Uri.UnescapeDataString(m.Groups[1].Value) : null;
    }

    public static string UygulamaBul()
    {
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        foreach (var y in new[] {
            Path.Combine(pf, @"NVIDIA Corporation\NVIDIA App\CEF\NVIDIA App.exe"),
            Path.Combine(pf, @"NVIDIA Corporation\NVIDIA GeForce Experience\NVIDIA GeForce Experience.exe"),
            Path.Combine(pf86, @"NVIDIA Corporation\NVIDIA GeForce Experience\NVIDIA GeForce Experience.exe") })
            if (File.Exists(y)) return y;
        return null;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
        public IntPtr hFile, pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData, pSIPClientData;
        public uint dwUIChoice, fdwRevocationChecks, dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData, pwszURLReference;
        public uint dwProvFlags, dwUIContext;
        public IntPtr pSignatureSettings;
    }

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
    static extern int WinVerifyTrust(IntPtr hwnd, ref Guid eylem, ref WINTRUST_DATA veri);

    // Dosyanın Authenticode imzası geçerli mi ve imzalayan NVIDIA Corporation mı?
    public static bool ImzaNvidia(string yol)
    {
        var dosya = new WINTRUST_FILE_INFO { pcwszFilePath = yol };
        dosya.cbStruct = (uint)Marshal.SizeOf(dosya);
        IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(dosya));
        try
        {
            Marshal.StructureToPtr(dosya, p, false);
            var veri = new WINTRUST_DATA { dwUIChoice = 2 /* arayüz yok */, dwUnionChoice = 1 /* dosya */, pFile = p };
            veri.cbStruct = (uint)Marshal.SizeOf(veri);
            var eylem = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");   // WINTRUST_ACTION_GENERIC_VERIFY_V2
            if (WinVerifyTrust(IntPtr.Zero, ref eylem, ref veri) != 0) return false;
            var sertifika = X509Certificate.CreateFromSignedFile(yol);
            return sertifika.Subject.IndexOf("O=NVIDIA Corporation", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return false; }
        finally
        {
            Marshal.DestroyStructure(p, typeof(WINTRUST_FILE_INFO));
            Marshal.FreeHGlobal(p);
        }
    }

    // Yönetici olarak çalışır: imzayı yeniden doğrular, geri yükleme noktası ister, NVIDIA kurulumunu açar.
    // 0 = kurulum açıldı, 3 = açıldı ama geri yükleme noktası oluşmadı, 2 = imza geçersiz, 1 = açılamadı.
    public static int Kur(string yol)
    {
        if (!File.Exists(yol) || !ImzaNvidia(yol)) return 2;
        bool nokta = false;
        try
        {
            var bilgi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -NonInteractive -Command \"Checkpoint-Computer -Description 'Yaman PC Cleaner: NVIDIA surucusu oncesi' " +
                "-RestorePointType DEVICE_DRIVER_INSTALL -ErrorAction Stop\"")
            {
                UseShellExecute = false, CreateNoWindow = true
            };
            using (var p = Process.Start(bilgi)) { p.WaitForExit(); nokta = p.ExitCode == 0; }
        }
        catch { }
        try { Process.Start(new ProcessStartInfo(yol) { UseShellExecute = true }); }
        catch { return 1; }
        return nokta ? 0 : 3;
    }
}
