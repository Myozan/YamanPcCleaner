// "Sistemi Optimize Et" ve "Neden yavaş?" (Genel Bakış sekmesindeki iki düğme).
// Sistemi Optimize Et: ne yapacağını önce gösterir (seçilebilir adımlar), sonra adım adım yapar ve önce/sonra farkını sayarak
// gösterir. Yalnızca risksiz işler: RAM boşaltma, işaretli arka plan programlarını kapatma (kaydedilmemiş işi olan pencere
// kapanmaz), yöneticisiz geçici dosya temizliği; isteğe bağlı olarak Turbo'nun oyun profilini açar.
// Neden yavaş: iki kısa ölçümün farkından o an bilgisayarı neyin yavaşlattığını sade bir dille söyler. Hiçbir şeyi değiştirmez.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

class HazirlikSonucu
{
    public long RamOnce, RamSonra, RamToplam, Bosalan, DiskAcilan;
    public double SanalOnce, SanalSonra;
    public int Kapanan, Kalan;
    public bool ProfilAcildi;
}

partial class AnaForm
{
    static readonly string[] SekmeAdlari = { "Genel Bakış", "Sistem", "Disk", "Sürücü", "Sağlık", "Başlangıç", "Windows", "Turbo", "Kişisel Bakım", "Ayarlar" };

    // Genel Bakış / Neden yavaş satırlarındaki düğmeler: sekme adı ya da "ram" (RAM'i boşalt), "sanal" (sanal bellek ayarı).
    internal void HedefeGit(string hedef)
    {
        if (string.IsNullOrEmpty(hedef)) return;
        if (hedef == "ram") { KisayolOptimize(); return; }
        if (hedef == "sanal") { SanalBellekAyarla(); return; }
        int n = Array.IndexOf(SekmeAdlari, hedef);
        if (n >= 0 && sekmeSec != null) sekmeSec(n);
    }

    // ---- Sistemi Optimize Et ----

    void OyunaHazirlaAc()
    {
        if (temizleniyor || diskSekmesi.Calisiyor)
        {
            MessageBox.Show(this, "Şu an başka bir iş sürüyor; bitince yeniden dene.", "Sistemi Optimize Et", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var programlar = liste.Items.Cast<ListViewItem>().Where(o => o.Checked).Select(o => (Grup)o.Tag).ToList();
        using (var f = new OyunaHazirlaFormu(this, programlar)) f.ShowDialog(this);
    }

    // Formun "Hazırla" düğmesi: seçilen adımları sırayla yapar. ilerleme(adım, metin): 0 kapat, 1 RAM, 2 disk, 3 profil.
    internal async Task<HazirlikSonucu> HazirlikYap(List<Grup> programlar, bool ram, bool disk, bool profil, Action<int, string> ilerleme)
    {
        var s = new HazirlikSonucu();
        OptimizeDurumu(true, "Sistem optimize ediliyor...");
        try
        {
            var once = Yerel.Bellek();
            s.RamToplam = (long)once.ullTotalPhys;
            s.RamOnce = (long)(once.ullTotalPhys - once.ullAvailPhys);
            s.SanalOnce = once.ullTotalPageFile > 0 ? (double)(once.ullTotalPageFile - once.ullAvailPageFile) / once.ullTotalPageFile : 0;
            long diskOnce = DiskSekmesi.BosAlan();
            var atla = oyunPidleri;

            if (programlar.Count > 0)
            {
                ilerleme(0, "Arka plan programları kapatılıyor...");
                int k = 0, kalan = 0;
                await Task.Run(() => { foreach (var g in programlar) { if (GrubuKapat(g) == 1) k++; else kalan++; } });
                s.Kapanan = k; s.Kalan = kalan;
                ilerleme(0, k + " program kapatıldı" + (kalan > 0 ? ", " + kalan + " tanesi kapanmadı (kaydedilmemiş işi olabilir)" : ""));
            }
            if (ram)
            {
                ilerleme(1, "RAM boşaltılıyor...");
                await Task.Run(() => { CalismaKumeleriniBosalt(atla); Thread.Sleep(700); });
                long bosalan = Math.Max(0, (long)Yerel.Bellek().ullAvailPhys - (long)once.ullAvailPhys);
                ilerleme(1, "RAM'de " + Boyut(bosalan) + " boşaldı");
            }
            if (disk)
            {
                ilerleme(2, "Geçici dosyalar temizleniyor...");
                await Task.Run(() =>
                {
                    foreach (var t in DiskTemizlik.Liste(false))
                    {
                        if (t.Yonetici || !t.Varsayilan || t.Ozel != null) continue;
                        DiskTemizlik.Hesapla(t);
                        if (t.AcikSurec == null) { try { DiskTemizlik.Temizle(t); } catch { } }
                    }
                });
                s.DiskAcilan = Math.Max(0, DiskSekmesi.BosAlan() - diskOnce);
                ilerleme(2, "Diskte " + Boyut(s.DiskAcilan) + " yer açıldı");
            }
            if (profil && !OyunProfili.Acik)
            {
                OyunProfili.Acik = true;
                s.ProfilAcildi = true;
                ilerleme(3, "Oyun profili açıldı: oyunda En iyi performans");
            }

            var sonra = Yerel.Bellek();
            s.RamSonra = (long)(sonra.ullTotalPhys - sonra.ullAvailPhys);
            s.SanalSonra = sonra.ullTotalPageFile > 0 ? (double)(sonra.ullTotalPageFile - sonra.ullAvailPageFile) / sonra.ullTotalPageFile : 0;
            s.Bosalan = Math.Max(0, s.RamOnce - s.RamSonra);
            Kazanc.Ekle(s.Bosalan, s.DiskAcilan);
        }
        finally
        {
            OptimizeDurumu(false, null);
            diskSekmesi.Yenile();
            Olc();
        }
        Ses.Cal(SesTuru.Tamam);
        return s;
    }

    // ---- Neden yavaş? ----

    void NedenYavasAc()
    {
        using (var f = new NedenYavasFormu(this)) f.ShowDialog(this);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS { public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }

    [DllImport("kernel32.dll")]
    static extern bool GetProcessTimes(IntPtr h, out long olusma, out long cikis, out long cekirdek, out long kullanici);
    [DllImport("kernel32.dll")]
    static extern bool GetProcessIoCounters(IntPtr h, out IO_COUNTERS sayac);

    class SurecOrnegi { public string Ad; public long Zaman, Gc; }

    // Bütün süreçler (Windows'unkiler dahil: Defender, güncelleme, dizinleme de sebep olabilir).
    static Dictionary<int, SurecOrnegi> SurecleriOrnekle()
    {
        var d = new Dictionary<int, SurecOrnegi>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.Id == 0) continue;
                IntPtr h = Yerel.OpenProcess(0x1000 /* QUERY_LIMITED */, false, p.Id);
                if (h == IntPtr.Zero) continue;
                try
                {
                    long a, b, c, k;
                    IO_COUNTERS io;
                    var o = new SurecOrnegi { Ad = p.ProcessName };
                    if (GetProcessTimes(h, out a, out b, out c, out k)) o.Zaman = c + k;
                    if (GetProcessIoCounters(h, out io)) o.Gc = (long)(io.ReadBytes + io.WriteBytes);
                    d[p.Id] = o;
                }
                finally { Yerel.CloseHandle(h); }
            }
            catch { }
            finally { p.Dispose(); }
        }
        return d;
    }

    // Windows'un kendi bileşenleri: yükü yapan buysa sade açıklaması.
    static readonly Dictionary<string, string> WindowsIsleri = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "MsMpEng", "Windows Defender tarama yapıyor; birkaç dakika içinde kendiliğinden biter" },
        { "MpDefenderCoreService", "Windows Defender tarama yapıyor; birkaç dakika içinde kendiliğinden biter" },
        { "NisSrv", "Windows Defender ağı tarıyor; kısa sürer" },
        { "TiWorker", "Windows güncelleme kuruyor; bitmesini bekle, bilgisayarı kapatma" },
        { "TrustedInstaller", "Windows güncelleme kuruyor; bitmesini bekle, bilgisayarı kapatma" },
        { "MoUsoCoreWorker", "Windows güncellemeleri hazırlıyor; kısa sürer" },
        { "usocoreworker", "Windows güncellemeleri hazırlıyor; kısa sürer" },
        { "wuauclt", "Windows güncellemeleri indiriyor" },
        { "SetupHost", "Windows büyük bir güncelleme hazırlıyor; uzun sürebilir" },
        { "SearchIndexer", "Windows arama için dosyaları dizinliyor; kısa sürer" },
        { "SearchProtocolHost", "Windows arama için dosyaları dizinliyor; kısa sürer" },
        { "SearchFilterHost", "Windows arama için dosyaları dizinliyor; kısa sürer" },
        { "CompatTelRunner", "Windows uyumluluk verisi topluyor; kısa sürer" },
        { "msiexec", "bir program kuruluyor ya da kaldırılıyor" },
        { "MemCompression", "RAM dolu olduğu için Windows belleği sıkıştırıyor; programları kapatmak rahatlatır" },
        { "dwm", "ekran çizimi (Windows); sürekli yüksekse ekran kartı sürücüsünü güncelle" },
        { "System", "Windows çekirdeği ve sürücüler" },
        { "svchost", "Windows hizmetleri" },
        { "explorer", "Dosya Gezgini" }
    };

    // ~2 saniye sürer, arka planda çağrılır.
    internal List<DenetimSatiri> YavaslikOlc()
    {
        var l = new List<DenetimSatiri>();
        int cekirdek = Environment.ProcessorCount;
        PerformanceCounter diskSayaci = null;
        try { diskSayaci = new PerformanceCounter("PhysicalDisk", "% Idle Time", "_Total"); diskSayaci.NextValue(); } catch { diskSayaci = null; }

        var saat = Stopwatch.StartNew();
        var ilk = SurecleriOrnekle();
        lock (olcerKilit) { if (olcer == null) olcer = new Olcer(); olcer.Topla(); }
        Thread.Sleep(1800);
        var son = SurecleriOrnekle();
        double sn = saat.Elapsed.TotalSeconds;
        Olcum o;
        lock (olcerKilit) o = olcer.Topla();
        double diskMesgul = -1;
        try { if (diskSayaci != null) diskMesgul = Math.Max(0, Math.Min(100, 100 - diskSayaci.NextValue())); } catch { }
        if (diskSayaci != null) diskSayaci.Dispose();

        // Süreç adına göre topla: işlemci yüzdesi (tüm çekirdeklerin toplamına göre) ve disk okuma/yazma hızı.
        var cpu = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var gc = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var pidler = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in son)
        {
            SurecOrnegi once;
            if (!ilk.TryGetValue(kv.Key, out once) || once.Ad != kv.Value.Ad || kv.Value.Ad == "Idle") continue;
            double y = (kv.Value.Zaman - once.Zaman) / 1e7 / sn / cekirdek * 100;
            double h = (kv.Value.Gc - once.Gc) / sn;
            double t;
            cpu.TryGetValue(kv.Value.Ad, out t); cpu[kv.Value.Ad] = t + Math.Max(0, y);
            gc.TryGetValue(kv.Value.Ad, out t); gc[kv.Value.Ad] = t + Math.Max(0, h);
            if (!pidler.ContainsKey(kv.Value.Ad)) pidler[kv.Value.Ad] = kv.Key;
        }
        string benim = Process.GetCurrentProcess().ProcessName;
        cpu.Remove(benim); gc.Remove(benim);

        var m = o.Bellek;
        double ram = m.ullTotalPhys > 0 ? (double)(m.ullTotalPhys - m.ullAvailPhys) / m.ullTotalPhys : 0;
        double sanal = m.ullTotalPageFile > 0 ? (double)(m.ullTotalPageFile - m.ullAvailPageFile) / m.ullTotalPageFile : 0;
        double cpuTop = o.CpuYuzde;

        // İşlemci
        var enCpu = cpu.OrderByDescending(k => k.Value).Take(2).Where(k => k.Value >= 5).ToList();
        if (enCpu.Count > 0 && (cpuTop >= 80 || enCpu[0].Value >= 35))
        {
            var bas = enCpu[0];
            string ad = Gorunen(bas.Key, pidler);
            string bilinen;
            bool windows = WindowsIsleri.TryGetValue(bas.Key, out bilinen);
            string baslik = cpuTop >= 80 ? string.Format("İşlemci %{0:0} yükte", cpuTop) : string.Format("{0} işlemciyi yoğun kullanıyor", ad);
            string aciklama = string.Format("{0}: %{1:0}{2}", ad, bas.Value, windows ? " · " + bilinen : "");
            if (enCpu.Count > 1 && cpuTop >= 80) aciklama += string.Format(" · {0}: %{1:0}", Gorunen(enCpu[1].Key, pidler), enCpu[1].Value);
            bool kapatilabilir = !windows && !Korunuyor(bas.Key);
            l.Add(DenetimSatiri.Yap(cpuTop >= 80 ? DenetimDurumu.Kotu : DenetimDurumu.Uyari, 0, baslik, aciklama,
                kapatilabilir ? "Göster" : null, kapatilabilir ? "Sistem" : null));
        }

        // Bellek
        if (sanal >= 0.9)
        {
            var en = EnCokSanalBellek(new HashSet<int>(oyunPidleri));
            l.Add(DenetimSatiri.Yap(DenetimDurumu.Kotu, 0, string.Format("Sanal bellek %{0:0} dolu", sanal * 100),
                "Programlar yavaşlıyor ve \"bellek yetersiz\" diye kapanabilir." + (en != null ? " En çok: " + Kisalt(en.Ad, 30) + " (" + Boyut(en.Bellek) + ")." : ""),
                "Göster", "Sistem"));
        }
        if (ram >= 0.9)
        {
            var enRam = o.Gruplar.OrderByDescending(g => g.Bellek).Take(2).Select(g => Kisalt(g.Gorunen, 24) + " (" + Boyut(g.Bellek) + ")").ToList();
            l.Add(DenetimSatiri.Yap(DenetimDurumu.Kotu, 0, string.Format("RAM %{0:0} dolu", ram * 100),
                "Windows diske taşıyor, her şey yavaşlar." + (enRam.Count > 0 ? " En çok: " + string.Join(", ", enRam) + "." : ""),
                "RAM'i boşalt", "ram"));
        }

        // Disk
        if (diskMesgul >= 85)
        {
            var enGc = gc.OrderByDescending(k => k.Value).FirstOrDefault();
            string kim = enGc.Key != null && enGc.Value >= 1048576
                ? " En çok: " + Gorunen(enGc.Key, pidler) + " (" + (enGc.Value / 1048576).ToString("0") + " MB/sn)" +
                  (WindowsIsleri.ContainsKey(enGc.Key) ? " · " + WindowsIsleri[enGc.Key] : "") + "."
                : "";
            bool hdd = false;
            try { hdd = DiskSagligi.Listele().Any(d => d.Tur.EndsWith("HDD")); } catch { }
            l.Add(DenetimSatiri.Yap(DenetimDurumu.Kotu, 0, string.Format("Disk sürekli meşgul (%{0:0})", diskMesgul),
                "Programlar diski bekliyor." + kim + (hdd ? " Sabit diskte (HDD) bu sık olur; SSD her ayardan çok fark eder." : ""), null, null));
        }

        // Ekran kartı ve ısı
        if (o.GpuYuzde >= 95)
            l.Add(DenetimSatiri.Yap(DenetimDurumu.Bilgi, 0, string.Format("Ekran kartı tam yükte (%{0:0})", o.GpuYuzde),
                "Oyun açıksa normal. FPS düşükse oyunun grafik ayarlarını bir kademe düşür.", null, null));
        if (o.GpuIsi >= 87)
            l.Add(DenetimSatiri.Yap(DenetimDurumu.Uyari, 0, string.Format("Ekran kartı çok sıcak ({0:0} °C)", o.GpuIsi),
                "Isınınca kendini yavaşlatır. Hava deliklerini temizle; dizüstünü düz, sert bir yüzeye koy.", null, null));
        if (o.CpuIsi >= 95)
            l.Add(DenetimSatiri.Yap(DenetimDurumu.Uyari, 0, string.Format("İşlemci çok sıcak (~{0:0} °C)", o.CpuIsi),
                "Isınınca kendini yavaşlatır. Fanları ve hava deliklerini temizle.", null, null));

        // Güç
        try
        {
            if (GucAyarlari.PilVar && !GucAyarlari.Prizde)
                l.Add(DenetimSatiri.Yap(DenetimDurumu.Uyari, 0, "Pilde çalışıyorsun",
                    "Windows ve ekran kartı pilde hızı kısar; oyunda fark çok büyüktür. Şarja tak.", null, null));
            else if (GucAyarlari.EtkinPlan() == GucAyarlari.DengeliPlan && GucAyarlari.Mod(true) == 0)
                l.Add(DenetimSatiri.Yap(DenetimDurumu.Uyari, 0, "Güç modu: En iyi pil verimliliği",
                    "Prizdeyken bile hız kısılıyor. Turbo → Oyun profili ya da Windows → Güç'ten değiştirebilirsin.", "Göster", "Turbo"));
        }
        catch { }

        string ozet = string.Format("İşlemci %{0:0} · RAM %{1:0} · sanal bellek %{2:0}{3}{4}", Math.Max(0, cpuTop), ram * 100, sanal * 100,
            diskMesgul >= 0 ? string.Format(" · disk %{0:0}", diskMesgul) : "", o.GpuYuzde >= 0 ? string.Format(" · ekran kartı %{0:0}", o.GpuYuzde) : "");
        if (l.Count == 0)
            l.Add(DenetimSatiri.Yap(DenetimDurumu.Iyi, 0, "Şu an bir yavaşlık sebebi görünmüyor",
                ozet + ". Yavaşlık belli anlarda oluyorsa tam o anda yeniden ölç.", null, null));
        else l.Add(DenetimSatiri.Yap(DenetimDurumu.Bilgi, 0, "Ölçüm", ozet, null, null));
        return l;
    }

    static string Gorunen(string ad, Dictionary<string, int> pidler)
    {
        int pid;
        if (pidler.TryGetValue(ad, out pid))
        {
            try
            {
                string yol = SurecYolu(pid);
                if (!string.IsNullOrEmpty(yol))
                {
                    string d = FileVersionInfo.GetVersionInfo(yol).FileDescription;
                    if (!string.IsNullOrWhiteSpace(d)) return Kisalt(d.Trim(), 32);
                }
            }
            catch { }
        }
        return ad;
    }
}

// Ortak koyu iletişim penceresi kalıbı.
class KoyuPencere : Form
{
    protected static readonly Font BaslikYazi = new Font("Segoe UI", 15f, FontStyle.Bold);
    protected static readonly Font Kalin = new Font("Segoe UI", 9.5f, FontStyle.Bold);

    public KoyuPencere(string baslik, Size boyut)
    {
        Text = baslik;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Renk.Arka;
        ForeColor = Renk.Yazi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = boyut;
        Icon = Logo.Simge();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try { int koyu = 1; Yerel.DwmSetWindowAttribute(Handle, 20, ref koyu, 4); } catch { }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Yerel.KoyuKaydirma(this);
    }

    public static Button Dugme(string metin, bool vurgu)
    {
        var b = new EylemDugmesi(metin, vurgu ? EylemDugmesi.Tur.Birincil : EylemDugmesi.Tur.Ikincil);
        b.Height = 38;
        return b;
    }
}

class OyunaHazirlaFormu : KoyuPencere
{
    readonly AnaForm ana;
    readonly List<Grup> programlar;
    readonly CheckBox chkKapat = new CheckBox(), chkRam = new CheckBox(), chkDisk = new CheckBox(), chkProfil = new CheckBox();
    readonly Panel secim = new Panel(), adimlar = new Panel(), sonuc = new Panel();
    readonly Button btnHazirla, btnKapat;
    readonly Label[] adimYazilari = new Label[4];
    bool calisiyor;

    internal const string AyarKapat = "hazirla_kapat", AyarRam = "hazirla_ram", AyarDisk = "hazirla_disk";   // Ayarlar sekmesi de yazar

    public OyunaHazirlaFormu(AnaForm ana, List<Grup> programlar) : base("Sistemi Optimize Et", new Size(560, 470))
    {
        this.ana = ana;
        this.programlar = programlar;

        var baslik = new Label { Text = "Sistemi Optimize Et", Font = BaslikYazi, Left = 24, Top = 18, Width = 500, Height = 32 };
        var alt = new Label
        {
            Text = "Bilgisayar yavaşladığında bir kez bas. Ne yapılacağını seç; hiçbir Windows ayarı değişmez.",
            ForeColor = Renk.Soluk, Left = 24, Top = 52, Width = 510, Height = 22
        };

        secim.SetBounds(24, 86, 512, 296);
        int y = 0;
        y = Secenek(chkKapat, "Arka plan programlarını kapat" + (programlar.Count > 0 ? " (" + programlar.Count + ")" : ""),
            programlar.Count > 0
                ? "Sistem sekmesinde işaretli olanlar: " + string.Join(", ", programlar.Take(4).Select(g => g.Gorunen)) + (programlar.Count > 4 ? " ve " + (programlar.Count - 4) + " tane daha" : "") +
                  ". Kaydedilmemiş işi olan pencere kapanmaz."
                : "Kapatılacak arka plan programı yok (Sistem sekmesinde işaretli program yok).", y);
        chkKapat.Enabled = programlar.Count > 0;
        chkKapat.Checked = programlar.Count > 0 && Ayarlar.Oku(AyarKapat, true);
        y = Secenek(chkRam, "RAM'i boşalt", "Programların o an kullanmadığı belleği geri alır. Oyun açıksa ona dokunulmaz.", y);
        chkRam.Checked = Ayarlar.Oku(AyarRam, true);
        y = Secenek(chkDisk, "Geçici dosyaları temizle", "Yönetici izni istemeyen, risksiz temizlik (Otomatik optimize ile aynı).", y);
        chkDisk.Checked = Ayarlar.Oku(AyarDisk, true);
        if (!OyunProfili.Acik)
        {
            y = Secenek(chkProfil, "Oyun profilini aç", "Oyun açılınca güç modu En iyi performans olur, oyun kapanınca eski hâline döner (Turbo'dan kapatılır).", y);
            chkProfil.Checked = false;
        }
        else
        {
            var not = new Label { Text = "✓  Oyun profili açık: oyun başlayınca güç modu kendiliğinden En iyi performans olur.", ForeColor = Renk.Yesil, Left = 0, Top = y + 6, Width = 512, Height = 22 };
            secim.Controls.Add(not);
        }

        adimlar.SetBounds(24, 86, 512, 296);
        adimlar.Visible = false;
        string[] adlar = { "Arka plan programları", "RAM", "Geçici dosyalar", "Oyun profili" };
        for (int i = 0; i < 4; i++)
        {
            adimYazilari[i] = new Label { Left = 0, Top = i * 44, Width = 512, Height = 36, ForeColor = Renk.Soluk, Font = new Font("Segoe UI", 11f), Text = "○  " + adlar[i] };
            adimlar.Controls.Add(adimYazilari[i]);
        }

        sonuc.SetBounds(24, 86, 512, 296);
        sonuc.Visible = false;

        btnHazirla = Dugme("Optimize Et", true);
        btnHazirla.SetBounds(316, 404, 220, 44);
        btnHazirla.Click += async delegate { await Hazirla(); };
        btnKapat = Dugme("Vazgeç", false);
        btnKapat.SetBounds(196, 404, 110, 44);
        btnKapat.Click += delegate { if (!calisiyor) Close(); };
        AcceptButton = btnHazirla;
        CancelButton = btnKapat;

        Controls.AddRange(new Control[] { baslik, alt, secim, adimlar, sonuc, btnHazirla, btnKapat });
        FormClosing += delegate(object s, FormClosingEventArgs e) { if (calisiyor) e.Cancel = true; };
    }

    int Secenek(CheckBox c, string metin, string aciklama, int y)
    {
        c.Text = metin;
        c.Font = Kalin;
        c.ForeColor = Renk.Yazi;
        c.SetBounds(0, y, 512, 26);
        c.Cursor = Cursors.Hand;
        var a = new Label { Text = aciklama, ForeColor = Renk.Soluk, Left = 20, Top = y + 26, Width = 492, Height = 40, Font = new Font("Segoe UI", 9f) };
        secim.Controls.Add(c);
        secim.Controls.Add(a);
        return y + 72;
    }

    async Task Hazirla()
    {
        if (calisiyor) return;
        bool kapat = chkKapat.Enabled && chkKapat.Checked, ram = chkRam.Checked, disk = chkDisk.Checked, profil = chkProfil.Checked;
        if (!kapat && !ram && !disk && !profil) return;
        if (chkKapat.Enabled) Ayarlar.Yaz(AyarKapat, chkKapat.Checked);
        Ayarlar.Yaz(AyarRam, ram);
        Ayarlar.Yaz(AyarDisk, disk);

        calisiyor = true;
        secim.Visible = false;
        adimlar.Visible = true;
        btnHazirla.Enabled = false;
        btnKapat.Enabled = false;
        bool[] secili = { kapat, ram, disk, profil };
        for (int i = 0; i < 4; i++) if (!secili[i]) adimYazilari[i].Visible = false;
        // Görünen adımları üstten sırala.
        int sira = 0;
        for (int i = 0; i < 4; i++) if (secili[i]) adimYazilari[i].Top = (sira++) * 44;

        HazirlikSonucu s = null;
        try
        {
            s = await ana.HazirlikYap(kapat ? programlar : new List<Grup>(), ram, disk, profil, (adim, metin) =>
            {
                bool bitti = !metin.EndsWith("...");
                adimYazilari[adim].ForeColor = bitti ? Renk.Yesil : Renk.Yazi;
                adimYazilari[adim].Text = (bitti ? "✓  " : "●  ") + metin;
            });
        }
        catch { }
        calisiyor = false;
        btnKapat.Enabled = true;
        btnKapat.Text = "Kapat";
        if (s == null) { btnKapat.Text = "Kapat"; return; }
        await Task.Delay(400);
        SonucuGoster(s);
    }

    void SonucuGoster(HazirlikSonucu s)
    {
        adimlar.Visible = false;
        sonuc.Visible = true;
        sonuc.Controls.Add(new Label
        {
            Text = "Optimize edildi", Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = Renk.Yesil,
            Left = 0, Top = 0, Width = 512, Height = 46, TextAlign = ContentAlignment.MiddleCenter
        });
        sonuc.Controls.Add(new Label
        {
            Text = "Bilgisayarın daha rahat çalışacak.", ForeColor = Renk.Soluk, Left = 0, Top = 46, Width = 512, Height = 22, TextAlign = ContentAlignment.MiddleCenter
        });

        // Kartlar: büyük sayı yukarı sayarak gelir.
        var kartlar = new List<Tuple<string, double, Func<double, string>>>();
        kartlar.Add(Tuple.Create("RAM boşaldı", (double)s.Bosalan, (Func<double, string>)(v => AnaForm.Boyut((long)v))));
        if (s.DiskAcilan > 0 || chkDisk.Checked) kartlar.Add(Tuple.Create("Yer açıldı", (double)s.DiskAcilan, (Func<double, string>)(v => AnaForm.Boyut((long)v))));
        if (s.Kapanan > 0) kartlar.Add(Tuple.Create("Program kapandı", (double)s.Kapanan, (Func<double, string>)(v => ((int)Math.Round(v)).ToString())));
        int genislik = (512 - (kartlar.Count - 1) * 10) / kartlar.Count;
        var sayilar = new List<Label>();
        for (int i = 0; i < kartlar.Count; i++)
        {
            var kart = new Panel { Left = i * (genislik + 10), Top = 86, Width = genislik, Height = 92, BackColor = Renk.Panel };
            var sayi = new Label { Left = 0, Top = 12, Width = genislik, Height = 40, Font = new Font("Segoe UI Semibold", 20f), ForeColor = Renk.Yazi, TextAlign = ContentAlignment.MiddleCenter, Text = kartlar[i].Item3(0) };
            var ad = new Label { Left = 0, Top = 56, Width = genislik, Height = 22, ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleCenter, Text = kartlar[i].Item1 };
            kart.Controls.Add(sayi);
            kart.Controls.Add(ad);
            sonuc.Controls.Add(kart);
            sayilar.Add(sayi);
        }

        const double GB = 1073741824.0;
        string fark = string.Format("RAM: {0:0.0} → {1:0.0} GB kullanımda   ·   Sanal bellek: %{2:0} → %{3:0}",
            s.RamOnce / GB, s.RamSonra / GB, s.SanalOnce * 100, s.SanalSonra * 100);
        sonuc.Controls.Add(new Label { Text = fark, ForeColor = Renk.Yazi, Left = 0, Top = 196, Width = 512, Height = 22, TextAlign = ContentAlignment.MiddleCenter });
        string ipucu = "Oyunun içindeyken RAM'i boşaltmak için: " + KisayolAyari.Metin(KisayolAyari.Oku()) + (s.ProfilAcildi ? "   ·   Oyun profili açıldı" : "");
        if (KisayolAyari.Oku() == Keys.None) ipucu = s.ProfilAcildi ? "Oyun profili açıldı." : "";
        sonuc.Controls.Add(new Label { Text = ipucu, ForeColor = Renk.Soluk, Left = 0, Top = 226, Width = 512, Height = 22, TextAlign = ContentAlignment.MiddleCenter });

        btnHazirla.Visible = false;
        btnKapat.Text = "Kapat";
        btnKapat.BackColor = Renk.Vurgu;
        btnKapat.ForeColor = Color.White;
        btnKapat.FlatAppearance.BorderSize = 0;
        btnKapat.SetBounds(196, 404, 168, 44);
        btnKapat.Focus();

        var baslangic = DateTime.Now;
        var t = new System.Windows.Forms.Timer { Interval = 16 };
        t.Tick += delegate
        {
            double ilerleme = Hareket.Acik ? Math.Min(1, (DateTime.Now - baslangic).TotalMilliseconds / 900.0) : 1;
            double yumusak = 1 - Math.Pow(1 - ilerleme, 3);
            for (int i = 0; i < kartlar.Count; i++) if (!sayilar[i].IsDisposed) sayilar[i].Text = kartlar[i].Item3(kartlar[i].Item2 * yumusak);
            if (ilerleme >= 1) { t.Stop(); t.Dispose(); }
        };
        t.Start();
    }
}

class NedenYavasFormu : KoyuPencere
{
    readonly AnaForm ana;
    readonly Label lblDurum = new Label();
    readonly Panel satirlar = new Panel();
    readonly Button btnYeniden, btnKapat;
    readonly System.Windows.Forms.Timer nokta = new System.Windows.Forms.Timer { Interval = 300 };
    bool olcuyor;
    int adim;

    public NedenYavasFormu(AnaForm ana) : base("Neden yavaş?", new Size(640, 470))
    {
        this.ana = ana;
        var baslik = new Label { Text = "Neden yavaş?", Font = BaslikYazi, Left = 24, Top = 18, Width = 580, Height = 32 };
        lblDurum.SetBounds(24, 52, 590, 22);
        lblDurum.ForeColor = Renk.Soluk;
        satirlar.SetBounds(24, 84, 592, 310);
        satirlar.AutoScroll = true;
        satirlar.HandleCreated += delegate { try { Yerel.SetWindowTheme(satirlar.Handle, "DarkMode_Explorer", null); } catch { } };
        btnYeniden = Dugme("Yeniden ölç", false);
        btnYeniden.SetBounds(316, 410, 140, 40);
        btnYeniden.Click += async delegate { await Olc(); };
        btnKapat = Dugme("Kapat", false);
        btnKapat.SetBounds(466, 410, 150, 40);
        btnKapat.Click += delegate { Close(); };
        CancelButton = btnKapat;
        Controls.AddRange(new Control[] { baslik, lblDurum, satirlar, btnYeniden, btnKapat });
        nokta.Tick += delegate { adim = (adim + 1) % 4; lblDurum.Text = "Ölçülüyor" + new string('.', adim + 1) + "  (iki saniye; hiçbir şey değiştirilmez)"; };
        Shown += async delegate { await Olc(); };
        FormClosed += delegate { nokta.Dispose(); };
    }

    async Task Olc()
    {
        if (olcuyor) return;
        olcuyor = true;
        btnYeniden.Enabled = false;
        nokta.Start();
        foreach (Control c in satirlar.Controls.Cast<Control>().ToList()) c.Dispose();
        satirlar.Controls.Clear();
        List<DenetimSatiri> l;
        try { l = await Task.Run(() => ana.YavaslikOlc()); }
        catch { l = new List<DenetimSatiri> { DenetimSatiri.Yap(DenetimDurumu.Bilgi, 0, "Ölçülemedi", "Bir daha dene.", null, null) }; }
        nokta.Stop();
        olcuyor = false;
        if (IsDisposed) return;
        btnYeniden.Enabled = true;
        int sorun = l.Count(s => s.Durum == DenetimDurumu.Kotu || s.Durum == DenetimDurumu.Uyari);
        lblDurum.Text = sorun == 0 ? "Ölçüldü: " + DateTime.Now.ToString("HH:mm:ss") : sorun + " sebep bulundu (" + DateTime.Now.ToString("HH:mm:ss") + ")";
        satirlar.SuspendLayout();
        for (int i = l.Count - 1; i >= 0; i--)
        {
            satirlar.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6 });
            satirlar.Controls.Add(new DenetimKutusu(l[i], Tikla));
        }
        satirlar.ResumeLayout(true);
    }

    void Tikla(DenetimSatiri s)
    {
        Close();
        ana.HedefeGit(s.Hedef);
    }
}
