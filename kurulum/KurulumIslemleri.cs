// Kurulum ve kaldırma işlemlerinin ortak arka uçları: klasörler, kısayollar, kayıt defteri ve dosya çıkarma.
// Hem setup.exe hem Kaldirici.exe bu dosyayı kullanır; böylece iki taraf aynı yolları ve kayıt girdilerini bilir.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class KurulumBilgi
{
    public const string ExeAdi = "YamanPcCleaner.exe";
    public const string KaldiriciAdi = "Kaldirici.exe";
    public const string UninstallAnahtari = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\YamanPcCleaner";
    public const string RunAnahtari = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string OnayAnahtari = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    public const string RunDegeri = "YamanPcCleaner";

    public static string Klasor
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Yaman PC Cleaner");
        }
    }

    public static string Exe { get { return Path.Combine(Klasor, ExeAdi); } }
    public static string Kaldirici { get { return Path.Combine(Klasor, KaldiriciAdi); } }

    public static string AyarlarKlasoru
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YamanPcCleaner");
        }
    }

    public static string MasaustuKisayolu
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Marka.Ad + ".lnk"); }
    }

    public static string BaslatKisayolu
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Marka.Ad + ".lnk"); }
    }

    public static string LogDosyasi
    {
        get { return Path.Combine(Path.GetTempPath(), "yaman-kurulum.log"); }
    }

    public static void Log(string metin)
    {
        try
        {
            File.AppendAllText(LogDosyasi,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + metin + Environment.NewLine,
                new UTF8Encoding(false));
        }
        catch { }
    }

    public static string KuruluSurum()
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(UninstallAnahtari))
                return k == null ? null : k.GetValue("DisplayVersion") as string;
        }
        catch { return null; }
    }

    public static bool UygulamaAcik()
    {
        Process[] ps = Process.GetProcessesByName("YamanPcCleaner");
        try { return ps.Length > 0; }
        finally { foreach (var p in ps) p.Dispose(); }
    }

    public static void UygulamayiKapat(bool zorla)
    {
        Process[] ps = Process.GetProcessesByName("YamanPcCleaner");
        foreach (var p in ps)
        {
            try { if (!p.HasExited) p.CloseMainWindow(); }
            catch { }
        }
        if (!zorla) Thread.Sleep(600);
        foreach (var p in ps)
        {
            try { if (!p.HasExited) p.Kill(); }
            catch { }
            try { p.Dispose(); } catch { }
        }
        Log("Yaman PC Cleaner kapatıldı.");
    }

    public static void Kur(bool masaustuKisayolu)
    {
        Directory.CreateDirectory(Klasor);
        KaynaktanCikar(ExeAdi, Exe);
        KaynaktanCikar(KaldiriciAdi, Kaldirici);
        Kaydet(Klasor);

        try { KisayolOlustur(BaslatKisayolu, Exe, Marka.Ad); }
        catch (Exception e) { Log("Başlat menüsü kısayolu oluşturulamadı: " + e.Message); }

        if (masaustuKisayolu)
        {
            try { KisayolOlustur(MasaustuKisayolu, Exe, Marka.Ad); }
            catch (Exception e) { Log("Masaüstü kısayolu oluşturulamadı: " + e.Message); }
        }
        Log("Kuruldu: " + Klasor);
    }

    public static string Kaldir(bool ayarlariSil)
    {
        if (UygulamaAcik()) UygulamayiKapat(true);

        KisayollariSil();
        KayitSil();
        if (ayarlariSil) SilKlasor(AyarlarKlasoru);

        string klasor = Path.GetDirectoryName(Application.ExecutablePath);
        string kendisi = Path.GetFullPath(Application.ExecutablePath);
        try
        {
            foreach (string dosya in Directory.GetFiles(klasor))
            {
                if (string.Equals(Path.GetFullPath(dosya), kendisi, StringComparison.OrdinalIgnoreCase)) continue;
                try { File.Delete(dosya); }
                catch (Exception e) { Log("Dosya silinemedi: " + dosya + " · " + e.Message); }
            }
            foreach (string alt in Directory.GetDirectories(klasor))
            {
                try { Directory.Delete(alt, true); }
                catch (Exception e) { Log("Klasör silinemedi: " + alt + " · " + e.Message); }
            }
        }
        catch (Exception e) { Log("Kaldırma sırasında hata: " + e.Message); }

        Log("Kaldırıldı: " + klasor);
        return klasor;
    }

    static void Kaydet(string kurulumKlasoru)
    {
        using (var k = Registry.CurrentUser.CreateSubKey(UninstallAnahtari))
        {
            if (k == null) throw new InvalidOperationException("Kaldırma kaydı oluşturulamadı.");
            string kaldirici = Path.Combine(kurulumKlasoru, KaldiriciAdi);
            k.SetValue("DisplayName", Marka.Ad);
            k.SetValue("DisplayVersion", Surum.Metin);
            k.SetValue("Publisher", Marka.Sirket);
            k.SetValue("InstallLocation", kurulumKlasoru);
            k.SetValue("DisplayIcon", Path.Combine(kurulumKlasoru, ExeAdi) + ",0");
            k.SetValue("UninstallString", "\"" + kaldirici + "\"");
            k.SetValue("QuietUninstallString", "\"" + kaldirici + "\" /S");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            k.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            k.SetValue("URLInfoAbout", Marka.Site);

            long boyut = 0;
            try
            {
                boyut += new FileInfo(Path.Combine(kurulumKlasoru, ExeAdi)).Length;
                boyut += new FileInfo(kaldirici).Length;
            }
            catch { }
            k.SetValue("EstimatedSize", (int)Math.Max(1, boyut / 1024), RegistryValueKind.DWord);
        }
    }

    static void KayitSil()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallAnahtari, false); }
        catch { }

        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunAnahtari, true))
            {
                if (k != null)
                {
                    object deger = k.GetValue(RunDegeri);
                    string metin = deger as string;
                    if (metin == null || metin.IndexOf("YamanPcCleaner", StringComparison.OrdinalIgnoreCase) >= 0)
                        k.DeleteValue(RunDegeri, false);
                }
            }
        }
        catch { }

        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(OnayAnahtari, true))
                if (k != null) k.DeleteValue(RunDegeri, false);
        }
        catch { }
    }

    static void KisayollariSil()
    {
        TrySil(MasaustuKisayolu);
        TrySil(BaslatKisayolu);
    }

    static void TrySil(string yol)
    {
        try { if (File.Exists(yol)) File.Delete(yol); }
        catch (Exception e) { Log("Kısayol silinemedi: " + yol + " · " + e.Message); }
    }

    static void SilKlasor(string yol)
    {
        try { if (Directory.Exists(yol)) Directory.Delete(yol, true); }
        catch (Exception e) { Log("Ayarlar silinemedi: " + yol + " · " + e.Message); }
    }

    // Çalışan kaldırıcı kendi klasörünü silemez; kısa bir gecikmeden sonra cmd ile temizler.
    public static void TemizligiPlanla(string klasor)
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe",
                "/c ping 127.0.0.1 -n 3 >nul & rmdir /s /q \"" + klasor + "\"");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);
        }
        catch (Exception e) { Log("Geçici temizlik başlatılamadı: " + e.Message); }
    }

    static void KaynaktanCikar(string kaynakAdi, string hedef)
    {
        using (var kaynak = Assembly.GetExecutingAssembly().GetManifestResourceStream(kaynakAdi))
        {
            if (kaynak == null) throw new InvalidOperationException("Kurulum dosyası eksik: " + kaynakAdi);
            string gecici = hedef + ".yeni";
            try
            {
                using (var dosya = new FileStream(gecici, FileMode.Create, FileAccess.Write, FileShare.None))
                    kaynak.CopyTo(dosya);

                Exception son = null;
                for (int i = 0; i < 8; i++)
                {
                    try
                    {
                        if (File.Exists(hedef)) File.Delete(hedef);
                        File.Move(gecici, hedef);
                        return;
                    }
                    catch (IOException e) { son = e; Thread.Sleep(250); }
                    catch (UnauthorizedAccessException e) { son = e; Thread.Sleep(250); }
                }
                throw new IOException("Dosya yazılamadı: " + hedef, son);
            }
            finally
            {
                try { if (File.Exists(gecici)) File.Delete(gecici); }
                catch { }
            }
        }
    }

    static void KisayolOlustur(string yol, string hedef, string aciklama)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(yol));
        object kabuk = null, kisayol = null;
        try
        {
            Type tip = Type.GetTypeFromProgID("WScript.Shell");
            if (tip == null) throw new InvalidOperationException("Windows Script Host bulunamadı.");
            kabuk = Activator.CreateInstance(tip);
            kisayol = tip.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, kabuk, new object[] { yol });
            Type kt = kisayol.GetType();
            kt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, kisayol, new object[] { hedef });
            kt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, kisayol, new object[] { Path.GetDirectoryName(hedef) });
            kt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, kisayol, new object[] { hedef + ",0" });
            kt.InvokeMember("Description", BindingFlags.SetProperty, null, kisayol, new object[] { aciklama });
            kt.InvokeMember("Save", BindingFlags.InvokeMethod, null, kisayol, new object[0]);
        }
        finally
        {
            if (kisayol != null && Marshal.IsComObject(kisayol)) Marshal.ReleaseComObject(kisayol);
            if (kabuk != null && Marshal.IsComObject(kabuk)) Marshal.ReleaseComObject(kabuk);
        }
    }
}
