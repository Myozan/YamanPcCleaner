// Sistem olcum modelleri ve canli olcum motoru (CPU, GPU, RAM, sicaklik, surecler).

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

class Grup
{
    public string Ad;          // süreç adı (chrome)
    public string Gorunen;     // dosya açıklaması (Google Chrome)
    public string Klasor;
    public List<int> Pidler = new List<int>();
    public long Bellek, Vram;  // Bellek: özel (commit) bellek; sanal belleği dolduran budur
    public double Cpu, Gpu;
    public bool Pencereli;
    public bool Bagli;         // açık bir pencerenin klasöründe: oyunun/uygulamanın parçası
    public bool Konsol;        // komut satırından / terminalden başlatılmış ya da kendi konsolu var
}

class Olcum
{
    public Yerel.MEMORYSTATUSEX Bellek;
    public double CpuYuzde = -1, GpuYuzde = -1, CpuIsi = -1, GpuIsi = -1;
    public long VramKullanilan = -1, VramToplam = -1;
    public List<Grup> Gruplar;
}

// Arka planda çalışır; her çağrıda bir önceki örnekle farka bakarak yüzdeleri hesaplar.
class Olcer
{
    static readonly string WinKlasoru = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";

    public readonly string CpuAdi, GpuAdi;
    readonly long kayitliVram;
    readonly int benim, oturum, cekirdekSayisi = Environment.ProcessorCount;
    readonly Stopwatch saat = Stopwatch.StartNew();
    double oncekiSaniye;
    long oncekiBos, oncekiCekirdek, oncekiKullanici;
    Dictionary<int, long> oncekiSurecZamani = new Dictionary<int, long>();
    Dictionary<string, CounterSample> oncekiGpu = new Dictionary<string, CounterSample>();
    readonly Dictionary<string, string> aciklamalar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    bool gpuSayaciYok, isiSayaciYok;
    string nvidiaSmi;
    DateTime sonNvidia = DateTime.MinValue;
    double nvIsi = -1;
    long nvKullanilan = -1, nvToplam = -1;

    public Olcer()
    {
        using (var p = Process.GetCurrentProcess()) { benim = p.Id; oturum = p.SessionId; }
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                if (k != null) CpuAdi = ((k.GetValue("ProcessorNameString") as string) ?? "").Trim();
        }
        catch { }
        EkranKartiBul(out GpuAdi, out kayitliVram);
        foreach (var yol in new[] {
            Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\NVSMI\nvidia-smi.exe") })
            if (File.Exists(yol)) { nvidiaSmi = yol; break; }
        Yerel.GetSystemTimes(out oncekiBos, out oncekiCekirdek, out oncekiKullanici);
    }

    public Olcum Topla()
    {
        var o = new Olcum { Bellek = Yerel.Bellek() };

        double simdi = saat.Elapsed.TotalSeconds, gecen = simdi - oncekiSaniye;
        oncekiSaniye = simdi;

        long bos, cekirdek, kullanici;
        Yerel.GetSystemTimes(out bos, out cekirdek, out kullanici);
        long dBos = bos - oncekiBos, dToplam = (cekirdek - oncekiCekirdek) + (kullanici - oncekiKullanici);
        oncekiBos = bos; oncekiCekirdek = cekirdek; oncekiKullanici = kullanici;
        if (dToplam > 0) o.CpuYuzde = Math.Max(0, Math.Min(100, 100.0 * (dToplam - dBos) / dToplam));

        var gpuPid = new Dictionary<int, double>();
        var vramPid = new Dictionary<int, long>();
        GpuOku(o, gpuPid, vramPid);
        o.CpuIsi = CpuIsisiOku();
        NvidiaOku(o);
        o.Gruplar = SurecleriTopla(gecen, gpuPid, vramPid);
        return o;
    }

    // Görev Yöneticisi'nin yöntemi: her motorun (3D, video, kopyalama...) yükü süreçlerin toplamıdır,
    // ekran kartı yüzdesi en yüklü motordur.
    void GpuOku(Olcum o, Dictionary<int, double> gpuPid, Dictionary<int, long> vramPid)
    {
        if (gpuSayaciYok) return;
        try
        {
            var motorlar = new Dictionary<string, double>();
            var yeni = new Dictionary<string, CounterSample>();
            bool oncekiVar = oncekiGpu.Count > 0;
            var kullanim = new PerformanceCounterCategory("GPU Engine").ReadCategory()["Utilization Percentage"];
            if (kullanim != null)
            {
                foreach (InstanceData d in kullanim.Values)
                {
                    yeni[d.InstanceName] = d.Sample;
                    CounterSample eski;
                    if (!oncekiGpu.TryGetValue(d.InstanceName, out eski)) continue;
                    float y = CounterSample.Calculate(eski, d.Sample);
                    int pid; string motor;
                    if (!(y > 0) || !PidAyir(d.InstanceName, out pid, out motor)) continue;
                    double t;
                    motorlar.TryGetValue(motor, out t);
                    motorlar[motor] = t + y;
                    double p;
                    if (!gpuPid.TryGetValue(pid, out p) || y > p) gpuPid[pid] = y;
                }
            }
            oncekiGpu = yeni;
            if (oncekiVar) o.GpuYuzde = motorlar.Count == 0 ? 0 : Math.Min(100, motorlar.Values.Max());

            var surecBellek = new PerformanceCounterCategory("GPU Process Memory").ReadCategory()["Dedicated Usage"];
            if (surecBellek != null)
            {
                foreach (InstanceData d in surecBellek.Values)
                {
                    int pid; string kalan;
                    if (!PidAyir(d.InstanceName, out pid, out kalan)) continue;
                    long v;
                    vramPid.TryGetValue(pid, out v);
                    vramPid[pid] = v + d.RawValue;
                }
            }

            var kartBellek = new PerformanceCounterCategory("GPU Adapter Memory").ReadCategory()["Dedicated Usage"];
            if (kartBellek != null)
                foreach (InstanceData d in kartBellek.Values) o.VramKullanilan = Math.Max(o.VramKullanilan, d.RawValue);
        }
        catch (InvalidOperationException) { gpuSayaciYok = true; }  // eski Windows: sayaç yok
        catch { }
    }

    // "pid_1234_luid_0x..._eng_0_engtype_3D" → 1234, "luid_0x..._eng_0_engtype_3D"
    static bool PidAyir(string ad, out int pid, out string kalan)
    {
        pid = 0; kalan = null;
        if (ad == null || !ad.StartsWith("pid_", StringComparison.OrdinalIgnoreCase)) return false;
        int i = ad.IndexOf('_', 4);
        if (i < 0 || !int.TryParse(ad.Substring(4, i - 4), out pid)) return false;
        kalan = ad.Substring(i + 1);
        return true;
    }

    // ACPI ısı bölgesi: yönetici izni istemez ama her bilgisayarda gerçek işlemci ısısını vermez.
    // Masaüstü kartlarda çoğu zaman sabit ~28 °C döner; 30 °C altını "ölçülemiyor" sayıyoruz.
    double CpuIsisiOku()
    {
        if (isiSayaciYok) return -1;
        try
        {
            var veri = new PerformanceCounterCategory("Thermal Zone Information").ReadCategory()["Temperature"];
            double enCok = -1;
            if (veri != null)
                foreach (InstanceData d in veri.Values) enCok = Math.Max(enCok, d.RawValue - 273.15);
            return enCok > 30 && enCok < 115 ? enCok : -1;
        }
        catch (InvalidOperationException) { isiSayaciYok = true; return -1; }
        catch { return -1; }
    }

    // NVIDIA kartlarda ısı ve VRAM'i sürücüyle gelen nvidia-smi verir; 5 saniyede bir sorulur.
    void NvidiaOku(Olcum o)
    {
        if (nvidiaSmi != null && (DateTime.UtcNow - sonNvidia).TotalSeconds >= 5)
        {
            sonNvidia = DateTime.UtcNow;
            long enCokToplam = 0;
            try
            {
                var bilgi = new ProcessStartInfo(nvidiaSmi, "--query-gpu=temperature.gpu,memory.used,memory.total --format=csv,noheader,nounits")
                {
                    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
                };
                using (var p = Process.Start(bilgi))
                {
                    var okuma = p.StandardOutput.ReadToEndAsync();
                    if (!okuma.Wait(4000)) { try { p.Kill(); } catch { } }
                    else
                    {
                        // Her satır bir NVIDIA kartı: en çok VRAM'i olan (asıl kart) gösterilir.
                        foreach (var satir in okuma.Result.Split('\n'))
                        {
                            var parca = satir.Split(',');
                            double isi; long kul, top;
                            if (parca.Length >= 3
                                && double.TryParse(parca[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out isi)
                                && long.TryParse(parca[1].Trim(), out kul) && long.TryParse(parca[2].Trim(), out top)
                                && top * 1048576 > enCokToplam)
                            {
                                enCokToplam = top * 1048576;
                                nvIsi = isi; nvKullanilan = kul * 1048576; nvToplam = top * 1048576;
                            }
                        }
                    }
                }
            }
            catch { nvidiaSmi = null; }
        }
        if (nvIsi >= 0) o.GpuIsi = nvIsi;
        if (nvToplam > 0) { o.VramKullanilan = nvKullanilan; o.VramToplam = nvToplam; }
        else if (kayitliVram > 0) o.VramToplam = kayitliVram;
    }

    // Takılı ekran kartlarından en çok belleği olanın adı ve VRAM boyutu (sürücünün yazdığı kayıt değerleri).
    static void EkranKartiBul(out string ad, out long vram)
    {
        ad = null; vram = 0;
        try
        {
            var takili = EkranKartlari.Takililar();
            using (var sinif = Registry.LocalMachine.OpenSubKey(EkranKartlari.SinifAnahtari))
            {
                if (sinif == null) return;
                foreach (var alt in sinif.GetSubKeyNames())
                {
                    if (alt.Length != 4) continue;
                    if (takili != null && !takili.Contains(alt)) continue;
                    try
                    {
                        using (var k = sinif.OpenSubKey(alt))
                        {
                            if (k == null) continue;
                            long boyut = EkranKartlari.Vram(k);
                            if (boyut > vram) { vram = boyut; ad = k.GetValue("DriverDesc") as string; }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    List<Grup> SurecleriTopla(double gecen, Dictionary<int, double> gpuPid, Dictionary<int, long> vramPid)
    {
        var pencereli = Yerel.PencereliSurecler();
        var konsol = Yerel.KonsolSurecleri();
        var gruplar = new Dictionary<string, Grup>(StringComparer.OrdinalIgnoreCase);
        var yeniZaman = new Dictionary<int, long>();
        var pencereKlasorleri = new List<string>();
        var tampon = new StringBuilder(1024);

        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    if (p.Id == benim || p.SessionId != oturum) continue;
                    string ad = p.ProcessName;
                    if (AnaForm.Korunuyor(ad)) continue;

                    // Kapatma izni yoksa (yönetici olarak çalışan, korumalı) listelemenin anlamı yok.
                    const uint SORGU = 0x1000, SONLANDIR = 0x0001;
                    IntPtr h = Yerel.OpenProcess(SORGU | SONLANDIR, false, p.Id);
                    if (h == IntPtr.Zero) continue;
                    string yol = null;
                    long zaman = -1;
                    try
                    {
                        int boyut = tampon.Capacity;
                        if (Yerel.QueryFullProcessImageName(h, 0, tampon, ref boyut)) yol = tampon.ToString();
                        long a, b, c, d;
                        if (Yerel.GetProcessTimes(h, out a, out b, out c, out d)) zaman = c + d;
                    }
                    finally { Yerel.CloseHandle(h); }

                    // Windows klasöründen çalışan her şey (sistem, sürücü yardımcıları) dokunulmaz.
                    if (string.IsNullOrEmpty(yol) || yol.StartsWith(WinKlasoru, StringComparison.OrdinalIgnoreCase)) continue;
                    // Dosyaya bakan kurallar: üretici yazılımı (yayıncıdan), WinDivert taşıyan bağlantı araçları.
                    if (AnaForm.Korunuyor(ad, yol)) continue;

                    double cpu = 0;
                    if (zaman >= 0)
                    {
                        yeniZaman[p.Id] = zaman;
                        long eski;
                        if (gecen > 0 && oncekiSurecZamani.TryGetValue(p.Id, out eski))
                            cpu = Math.Max(0, (zaman - eski) / (gecen * 1e7 * cekirdekSayisi) * 100);
                    }

                    Grup g;
                    if (!gruplar.TryGetValue(ad, out g))
                    {
                        g = new Grup { Ad = ad, Gorunen = Aciklama(yol, ad), Klasor = Path.GetDirectoryName(yol) };
                        gruplar[ad] = g;
                    }
                    g.Pidler.Add(p.Id);
                    g.Bellek += p.PrivateMemorySize64;
                    g.Cpu += cpu;
                    double gp; if (gpuPid.TryGetValue(p.Id, out gp)) g.Gpu += gp;
                    long vr; if (vramPid.TryGetValue(p.Id, out vr)) g.Vram += vr;
                    if (pencereli.Contains(p.Id)) { g.Pencereli = true; pencereKlasorleri.Add(Path.GetDirectoryName(yol)); }
                    if (konsol.Contains(p.Id)) g.Konsol = true;
                }
                catch { }
            }
        }
        oncekiSurecZamani = yeniZaman;

        foreach (var g in gruplar.Values)
        {
            g.Cpu = Math.Min(100, g.Cpu);
            g.Gpu = Math.Min(100, g.Gpu);
            if (!g.Pencereli) g.Bagli = AyniUygulama(g.Klasor, pencereKlasorleri);
        }
        return gruplar.Values.ToList();
    }

    string Aciklama(string yol, string ad)
    {
        string a;
        if (aciklamalar.TryGetValue(yol, out a)) return a;
        a = ad;
        try
        {
            string d = FileVersionInfo.GetVersionInfo(yol).FileDescription;
            if (!string.IsNullOrWhiteSpace(d)) a = d.Trim();
        }
        catch { }
        aciklamalar[yol] = a;
        return a;
    }

    // Arka plan süreci açık bir pencerenin klasöründe (ya da üst klasöründe) mi? Oyunların yardımcı
    // süreçleri böyle durur; kapatılırsa oyun çöker. "C:\Program Files" gibi sığ klasörler sayılmaz.
    static bool AyniUygulama(string klasor, List<string> pencereKlasorleri)
    {
        if (string.IsNullOrEmpty(klasor)) return false;
        foreach (var pk in pencereKlasorleri)
        {
            if (string.IsNullOrEmpty(pk)) continue;
            if ((Derinlik(pk) >= 3 && Icinde(klasor, pk)) || (Derinlik(klasor) >= 3 && Icinde(pk, klasor))) return true;
        }
        return false;
    }

    static int Derinlik(string klasor) { return klasor.TrimEnd('\\').Split('\\').Length; }

    static bool Icinde(string alt, string ust)
    {
        ust = ust.TrimEnd('\\');
        return alt.Equals(ust, StringComparison.OrdinalIgnoreCase)
            || alt.StartsWith(ust + "\\", StringComparison.OrdinalIgnoreCase);
    }
}
