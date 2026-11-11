// Ana pencere: sistem izleme sekmesi, olcum dongusu ve yasam dongusu.

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

partial class AnaForm : Form
{
    // Hiç listelenmez (Windows klasöründen çalışanlar zaten listelenmez): güvenlik yazılımları,
    // hile önleyiciler (kapatılırsa oyundan atar) ve yaygın bağlantı araçları. Adı bilinmeyen DPI / VPN araçları
    // WinDivert sürücüsünü taşımalarından tanınır (WinDivertTasiyor).
    static readonly HashSet<string> Korunan = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MsMpEng", "SecurityHealthSystray", "AvastUI", "AvastSvc", "AVGUI", "avgsvc", "avp", "avpui",
        "bdagent", "bdservicehost", "egui", "ekrn", "MBAMTray", "MBAMService", "mcshield", "McUICnt",
        "NortonUI", "NortonSecurity", "ccSvcHst", "360tray", "360sd",
        "vgtray", "vgc", "EasyAntiCheat", "EasyAntiCheat_EOS", "BEService", "BEService_x64",
        "FACEIT", "FACEITService", "faceitclient", "SGuard64", "SGuardSvc64", "ACE-Tray", "ACE-Guard",
        "xhunter1", "GameGuard",
        "goodbyedpi", "winws",
        "YamanPcCleaner"
    };

    // Adı bunlarla başlayanlar da listelenmez: ekran kartı, ses ve dokunmatik yüzey yazılımları.
    static readonly string[] KorunanOnek =
    {
        "NVIDIA", "nvcontainer", "NVDisplay", "nvsphelper", "AMD", "Radeon", "atiec", "atiesr", "Intel", "igfx", "IGCC",
        "Rtk", "Realtek", "Nahimic", "A-Volute", "Dolby", "Waves", "MaxxAudio", "SonicStudio",
        "SynTP", "Synaptics", "ETD", "Elan", "ApntEx", "Apoint"
    };

    // Bilgisayar üreticisinin yazılımları (fan, Fn tuşları, pil, klavye ışığı): yayıncıdan (CompanyName) tanınır,
    // her üretici için en az bir yayıncı adı. Exe adı önekleri yalnızca yayıncı boşsa yedek olarak kullanılır;
    // böylece aynı markanın isteğe bağlı araçları (ör. MSI Afterburner, yayıncısı "MSI Co., LTD") listelenebilir.
    public static readonly string[] UreticiYayincilari =
    {
        "Micro-Star", "ASUSTeK", "Lenovo", "Dell", "Alienware", "HP Inc", "Hewlett-Packard", "HP Development",
        "Acer", "GIGA-BYTE", "Gigabyte", "Samsung Electronics", "Huawei", "Honor Device", "LG Electronics",
        "Clevo", "Tongfang", "Uniwill", "Monster", "Casper", "Xiaomi"
    };

    public static readonly string[] UreticiOnek =
    {
        "MSI", "DCv2", "LEDKeeper", "OmAp", "Armoury", "Asus", "Lenovo", "Dell", "Alienware",
        "OmenCommandCenter", "HPSystemEvent", "Acer", "PredatorSense", "Gigabyte", "AORUS"
    };

    // Listelenir ama varsayılan olarak işaretsiz gelir: kapatınca oyun, sesli sohbet, kayıt,
    // fare/klavye ayarı ya da uzaktan yardım kopar. Komut satırından / terminalden başlatılmış süreçler de
    // (kaydedilmemiş işleri olabilir) adlarından bağımsız olarak işaretsiz gelir (Grup.Konsol).
    static readonly HashSet<string> VarsayilanIsaretsiz = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "steam", "steamwebhelper", "steamservice", "gameoverlayui", "gameoverlayui64",
        "EpicGamesLauncher", "EpicWebHelper", "Battle.net", "RiotClientServices", "RiotClientUx", "RiotClientUxRender",
        "EADesktop", "EABackgroundService", "upc", "UbisoftConnect", "GalaxyClient", "XboxPcApp",
        "Discord", "DiscordPTB", "DiscordCanary", "TeamSpeak", "ts3client_win64", "ms-teams", "Teams",
        "Skype", "WhatsApp", "Telegram", "Zoom",
        "obs64", "obs32", "RTSS", "RTSSHooksLoader64", "EncoderServer", "Medal", "Overwolf", "ShareX", "bdcam",
        "lghub", "lghub_agent", "lghub_system_tray", "Razer Synapse 3", "RazerCentral", "Razer Synapse Service Process",
        "iCUE", "SteelSeriesGG", "SteelSeriesEngine", "SteelSeriesSonar",
        "TeamViewer", "TeamViewer_Service", "AnyDesk", "RustDesk", "parsecd", "OneDrive", "GoogleDriveFS", "Dropbox",
        "msedgewebview2"
    };

    // Adı bunlarla başlayan fare/klavye/kulaklık yazılımlarının tüm parçaları da işaretsiz gelir.
    static readonly string[] VarsayilanIsaretsizOnek = { "SteelSeries", "Razer", "lghub", "Corsair", "iCUE", "HyperX", "Wooting" };

    static readonly string[] Basliklar = { "Program", "Durum", "İşlemci", "Bellek", "GPU", "VRAM" };

    static readonly string AyarDosyasi = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YamanPcCleaner", "secimler.txt");

    const string BilgiMetni = "İşaretli programlar \"Temizle\" ile kapanır. Açık pencereler ve oyunların parçaları işaretsiz gelir.\n" +
                              "Sağ tık → tek programı hemen kapat. Sütun başlığına tıkla → sırala.";

    readonly Gosterge gIslemci = new Gosterge("İŞLEMCİ", "");
    readonly Gosterge gGpu = new Gosterge("EKRAN KARTI", "");
    readonly Gosterge gRam = new Gosterge("RAM", "Fiziksel bellek");
    readonly Gosterge gVram = new Gosterge("VRAM", "Ekran kartı belleği");
    readonly Gosterge gSanal = new Gosterge("SANAL BELLEK", "RAM + sayfa dosyası · dolarsa programlar çöker");
    readonly Gosterge gIsi = new Gosterge("ISI", "");
    readonly Label lblBilgi = new Label();
    readonly KoyuListe liste = new KoyuListe();
    readonly Siralayici siralayici = new Siralayici();
    readonly Button btnTemizle = new Button(), btnSanal = new Button();
    Button btnOto, btnFull;
    readonly Label lblDurum = new Label();
    readonly Label lblIs = new Label();
    readonly DiskSekmesi diskSekmesi = new DiskSekmesi { Visible = false };
    readonly SurucuSekmesi surucuSekmesi = new SurucuSekmesi { Visible = false };
    readonly BaslangicSekmesi baslangicSekmesi = new BaslangicSekmesi { Visible = false };
    readonly WindowsSekmesi windowsSekmesi = new WindowsSekmesi { Visible = false };
    readonly TurboSekmesi turboSekmesi = new TurboSekmesi { Visible = false };
    readonly KisiselBakimPaneli kisiselBakim = new KisiselBakimPaneli { Visible = false };
    readonly SaglikSekmesi saglikSekmesi;
    readonly DurumPanosu durumPanosu;
    readonly AyarSekmesi ayarSekmesi;
    Action<int> sekmeSec;
    readonly System.Windows.Forms.Timer zamanlayici = new System.Windows.Forms.Timer();
    readonly BellekBekcisi bekci;

    // true: pencere ilk açılışta gösterilmez (Program.Main "--arkaplan"); yalnızca tepsi simgesi çalışır.
    // Load olayı (ilk ölçüm, sürücü kontrolü) pencere ilk kez gösterilene kadar ertelenir.
    public bool GizliBasla;

    // Açılış ekranı: ilk ölçüm ve ilk liste doldurma bitince bir kez tetiklenir (Program.Main bağlar).
    public event Action IlkOlcumBitti;
    bool ilkOlcumBildirildi;

    Olcer olcer;
    readonly object olcerKilit = new object();
    bool olcuyor, temizleniyor, dolduruluyor, yuklendi;
    HashSet<int> oyunPidleri = new HashSet<int>();
    readonly Dictionary<string, ListViewItem> ogeler = new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, bool> hatirlanan = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public static bool Korunuyor(string ad)
    {
        if (Korunan.Contains(ad)) return true;
        foreach (var onek in KorunanOnek)
            if (ad.StartsWith(onek, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static readonly Dictionary<string, bool> yolKorumasi = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    // Ada ek olarak dosyaya bakan kurallar: üretici yazılımı (yayıncıdan) ve WinDivert taşıyan bağlantı araçları.
    // Sonuç yol başına saklanır (sürüm bilgisi her ölçümde yeniden okunmasın). Birden çok iş parçacığından çağrılır.
    public static bool Korunuyor(string ad, string yol)
    {
        if (Korunuyor(ad)) return true;
        if (string.IsNullOrEmpty(yol)) return UreticiYazilimi(ad, null);
        bool sonuc;
        lock (yolKorumasi) if (yolKorumasi.TryGetValue(yol, out sonuc)) return sonuc;
        string yayinci = null;
        try { yayinci = FileVersionInfo.GetVersionInfo(yol).CompanyName; } catch { }
        sonuc = UreticiYazilimi(ad, yayinci) || WinDivertTasiyor(yol);
        lock (yolKorumasi) yolKorumasi[yol] = sonuc;
        return sonuc;
    }

    // Yayıncı biliniyorsa yalnızca ona bakılır; boşsa exe adı öneki yedektir.
    public static bool UreticiYazilimi(string ad, string yayinci)
    {
        if (!string.IsNullOrWhiteSpace(yayinci))
            return UreticiYayincilari.Any(y => yayinci.IndexOf(y, StringComparison.OrdinalIgnoreCase) >= 0);
        return !string.IsNullOrEmpty(ad) && UreticiOnek.Any(o => ad.StartsWith(o, StringComparison.OrdinalIgnoreCase));
    }

    // Exe ile aynı klasörde WinDivert sürücüsü var mı: DPI aşma / VPN / paket yönlendirme aracıdır (goodbyedpi,
    // zapret ve adı bilinmeyen benzerleri); kapatılırsa bağlantı kopar.
    public static bool WinDivertTasiyor(string yol)
    {
        try
        {
            if (string.IsNullOrEmpty(yol)) return false;
            string k = Path.GetDirectoryName(yol);
            if (string.IsNullOrEmpty(k)) return false;
            foreach (var d in new[] { "WinDivert.dll", "WinDivert64.sys", "WinDivert32.sys" })
                if (File.Exists(Path.Combine(k, d))) return true;
        }
        catch { }
        return false;
    }

    public AnaForm()
    {
        Text = Marka.Ad + "  ·  v" + Surum.Metin;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Renk.Arka;
        ForeColor = Renk.Yazi;
        ClientSize = new Size(1200, 830);
        StartPosition = FormStartPosition.CenterScreen;
        EkranaSigdir(Screen.FromPoint(Cursor.Position));
        Icon = Logo.Simge();

        // Bekçi: tepsi simgesi, bellek uyarıları, sızıntı izleme. Pencere kapalıyken de çalışır.
        bekci = new BellekBekcisi(this, delegate { OtomatikOptimize_Click(this, EventArgs.Empty); }, KisayolOptimize);
        // Turbo sekmesinin oyun profili: oyun sırasında kapanmış bir önceki oturumun güç modu önce geri yazılır.
        OyunProfili.AcilistaGeriYukle();
        bekci.Profil = OyunProfili.Ornek = new OyunProfili(OndeGpuKullaniyor, delegate { return KisayolIsi(true); });
        var guncelleyici = new Guncelleyici(this, bekci);
        FormClosed += delegate { guncelleyici.Dispose(); };
        saglikSekmesi = new SaglikSekmesi(bekci) { Visible = false };
        durumPanosu = new DurumPanosu(bekci) { Visible = false };
        ayarSekmesi = new AyarSekmesi(bekci) { Visible = false };

        var ust = new TableLayoutPanel { Dock = DockStyle.Top, Height = 250, ColumnCount = 2, RowCount = 3, Padding = new Padding(8, 8, 8, 0) };
        ust.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ust.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 3; i++) ust.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
        ust.Controls.Add(gIslemci, 0, 0); ust.Controls.Add(gGpu, 1, 0);
        ust.Controls.Add(gRam, 0, 1);     ust.Controls.Add(gVram, 1, 1);
        ust.Controls.Add(gSanal, 0, 2);   ust.Controls.Add(gIsi, 1, 2);

        lblBilgi.Dock = DockStyle.Top;
        lblBilgi.Height = 48;
        lblBilgi.Padding = new Padding(14, 6, 12, 0);
        lblBilgi.ForeColor = Renk.Soluk;
        lblBilgi.Text = BilgiMetni;

        foreach (var b in Basliklar) liste.Columns.Add(b);
        liste.Columns[1].Width = 128;
        for (int i = 2; i < 6; i++) { liste.Columns[i].TextAlign = HorizontalAlignment.Right; liste.Columns[i].Width = 78; }
        liste.Columns[3].Width = 92;
        liste.Dock = DockStyle.Fill;
        liste.ListViewItemSorter = siralayici;
        liste.Resize += delegate { SutunlariAyarla(); };
        liste.ItemChecked += delegate(object s, ItemCheckedEventArgs e)
        {
            if (!dolduruluyor) hatirlanan[((Grup)e.Item.Tag).Ad] = e.Item.Checked;
        };
        liste.ColumnClick += delegate(object s, ColumnClickEventArgs e)
        {
            if (siralayici.Sutun == e.Column) siralayici.Azalan = !siralayici.Azalan;
            else { siralayici.Sutun = e.Column; siralayici.Azalan = e.Column > 1; }
            BasliklariYaz();
            liste.Sort();
        };
        BasliklariYaz();

        var menu = new ContextMenuStrip();
        var mKapat = new ToolStripMenuItem("Bu programı hemen kapat");
        mKapat.Click += async delegate
        {
            var secili = liste.SelectedItems.Cast<ListViewItem>().Select(o => (Grup)o.Tag).ToList();
            if (secili.Count > 0) await Kapat(secili, false);
        };
        menu.Items.Add(mKapat);
        menu.Opening += delegate(object s, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = liste.SelectedItems.Count == 0 || temizleniyor;
        };
        liste.ContextMenuStrip = menu;

        var orta = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
        orta.Controls.Add(liste);

        var alt = new Panel { Dock = DockStyle.Bottom, Height = 100, Padding = new Padding(12, 8, 12, 10) };
        btnTemizle.Text = "Temizle";
        btnTemizle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
        btnTemizle.BackColor = Renk.Vurgu;
        btnTemizle.ForeColor = Color.White;
        btnTemizle.FlatStyle = FlatStyle.Flat;
        btnTemizle.FlatAppearance.BorderSize = 0;
        btnTemizle.Dock = DockStyle.Fill;
        btnTemizle.Cursor = Cursors.Hand;
        btnTemizle.Click += Temizle_Click;
        lblDurum.Dock = DockStyle.Fill;
        lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        lblDurum.ForeColor = Renk.Soluk;
        lblDurum.Text = "Ölçülüyor...";
        var satir = new Panel { Dock = DockStyle.Bottom, Height = 32 };
        satir.Controls.Add(lblDurum);
        btnSanal.Text = "Sanal bellek ayarı";
        btnSanal.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        btnSanal.BackColor = Renk.Panel;
        btnSanal.ForeColor = Renk.Yazi;
        btnSanal.FlatStyle = FlatStyle.Flat;
        btnSanal.FlatAppearance.BorderColor = Renk.Cizgi;
        btnSanal.Dock = DockStyle.Right;
        btnSanal.Width = 200;
        btnSanal.Cursor = Cursors.Hand;
        btnSanal.Click += SanalBellek_Click;
        alt.Controls.Add(btnTemizle);
        alt.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
        alt.Controls.Add(btnSanal);
        alt.Controls.Add(satir);

        var sistem = new Panel { Dock = DockStyle.Fill };
        sistem.Controls.Add(orta);
        sistem.Controls.Add(lblBilgi);
        sistem.Controls.Add(ust);
        sistem.Controls.Add(alt);
        ArayuzKur(sistem);

        zamanlayici.Interval = 2000;
        zamanlayici.Tick += delegate { Olc(); };

        SecimleriYukle();
        // Sürücü kontrolü açılışta bir kez arka planda yapılır; yeni sürüm varsa sekme "Sürücü •" olur.
        // "--arkaplan" ile gizli açılışta Load, pencere ilk kez gösterilince çalışır.
        // Açılış sekmesi (Ayarlar → Görünüm) de burada: gizli açılışta sekmelerin Gosterildi işleri pencere görünmeden başlamasın.
        Load += delegate
        {
            int ilk = AcilisSekmesi.Sekme(SekmeSayisi);
            if (ilk != acikSekme) SayfaSec(ilk);
            yuklendi = true; Olc(); zamanlayici.Start(); surucuSekmesi.Kontrol(); if (acikSekme == 0) durumPanosu.Gosterildi();
        };
        // Pencere gizliyken (tepside) 2 sn'lik ölçüm boşuna çalışmasın; bekçinin kendi zamanlayıcıları sürer.
        VisibleChanged += delegate
        {
            if (!yuklendi) return;
            if (Visible) { Olc(); zamanlayici.Start(); }
            else zamanlayici.Stop();
        };
        FormClosing += delegate(object s, FormClosingEventArgs e)
        {
            // Arka planda çalışma açıksa pencere yalnızca gizlenir (tepsiye iner); bekçi çalışmaya devam eder.
            if (bekci != null && bekci.KapanisiYakala(e)) { SecimleriKaydet(); return; }
            SecimleriKaydet();
        };
        // Durdurma işleri FormClosed'da: Windows kapanışında FormClosing ilk aşamada gelir, kapanış iptal edilirse
        // (ör. "Kaydet?" sorusunda İptal) program ayakta kalır; zamanlayıcı, indirme ve bekçi çalışmaya devam etmeli.
        // Bekçi kendini FormClosed'da kapatır (BellekBekcisi kurucusu).
        FormClosed += delegate { zamanlayici.Stop(); surucuSekmesi.Kapaniyor(); };
    }

    // Pencereyi ekranın çalışma alanına sığdırır (1366x768 dizüstünde görev çubuğunun altına taşmasın).
    void EkranaSigdir(Screen ekran)
    {
        var alan = ekran.WorkingArea;
        // Kurumsal kabuk: solda 244 px gezinme, üstte başlık ve iki hızlı işlem; en dar hâlde durum panosunun iki sütunu
        // ve sağlık listesi okunaklı kalır (1366x768 dizüstünde görev çubuğu dahil sığar).
        MinimumSize = new Size(Math.Min(1180, alan.Width), Math.Min(700, alan.Height));
        Size = new Size(Math.Min(Width, alan.Width), Math.Min(Height, alan.Height));
    }

    // Tek örnek: ikinci kez açılan exe sinyal gönderir (Program.Main); bu, iş parçacığı havuzundan çağrılır.
    public void GosterIstegi()
    {
        try
        {
            if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(delegate { if (bekci != null) bekci.Goster(); else Show(); }));
        }
        catch { }
    }

    // "--arkaplan": ilk görünür olma isteği yutulur (pencere tutamacı yine oluşur ki BeginInvoke çalışsın).
    // Sonraki Show/Goster normal çalışır; Load da o zaman tetiklenir.
    protected override void SetVisibleCore(bool value)
    {
        if (GizliBasla)
        {
            GizliBasla = false;
            if (!IsHandleCreated) CreateHandle();
            value = false;
        }
        base.SetVisibleCore(value);
    }

    protected override void OnLoad(EventArgs e)
    {
        // Pencere hangi ekranda açılıyorsa ona göre yeniden sığdır, sonra (StartPosition) ortala.
        EkranaSigdir(Screen.FromPoint(Cursor.Position));
        base.OnLoad(e);
        var alan = Screen.FromControl(this).WorkingArea;
        if (!alan.Contains(Bounds))
            Location = new Point(alan.Left + Math.Max(0, (alan.Width - Width) / 2), alan.Top + Math.Max(0, (alan.Height - Height) / 2));
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Yerel.KoyuKaydirma(this);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try { int koyu = 1; Yerel.DwmSetWindowAttribute(Handle, 20 /* koyu başlık çubuğu */, ref koyu, 4); } catch { }
    }

    async void Olc()
    {
        if (olcuyor) return;
        olcuyor = true;
        try
        {
            var o = await Task.Run(() =>
            {
                lock (olcerKilit)
                {
                    if (olcer == null) olcer = new Olcer();
                    return olcer.Topla();
                }
            });
            Goster(o);
            if (!temizleniyor) ListeyiGuncelle(o.Gruplar);
            if (!ilkOlcumBildirildi)
            {
                ilkOlcumBildirildi = true;
                var isleyici = IlkOlcumBitti;
                if (isleyici != null) { try { isleyici(); } catch { } }
            }
        }
        catch { }
        finally { olcuyor = false; }
    }

    void Goster(Olcum o)
    {
        const double GB = 1073741824.0;
        var m = o.Bellek;

        if (string.IsNullOrEmpty(gIslemci.Alt) && olcer != null)
        {
            gIslemci.Alt = olcer.CpuAdi;
            gGpu.Alt = olcer.GpuAdi;
        }

        if (o.CpuYuzde >= 0) gIslemci.Ayarla(string.Format("%{0:0}", o.CpuYuzde), o.CpuYuzde / 100);
        if (o.GpuYuzde >= 0) gGpu.Ayarla(string.Format("%{0:0}", o.GpuYuzde), o.GpuYuzde / 100);

        double ramTop = m.ullTotalPhys / GB, ramKul = (m.ullTotalPhys - m.ullAvailPhys) / GB;
        gRam.Ayarla(string.Format("{0:0.0} / {1:0.0} GB", ramKul, ramTop), ramKul / ramTop);

        double sTop = m.ullTotalPageFile / GB, sKul = (m.ullTotalPageFile - m.ullAvailPageFile) / GB;
        gSanal.Ayarla(string.Format("{0:0.0} / {1:0.0} GB", sKul, sTop), sKul / sTop);

        if (o.VramKullanilan >= 0 && o.VramToplam > 0)
            gVram.Ayarla(string.Format("{0:0.0} / {1:0.0} GB", o.VramKullanilan / GB, o.VramToplam / GB), (double)o.VramKullanilan / o.VramToplam);
        else if (o.VramKullanilan >= 0)
            gVram.Ayarla(string.Format("{0:0.0} GB", o.VramKullanilan / GB), 0);

        var parcalar = new List<string>();
        if (o.CpuIsi >= 0) parcalar.Add(string.Format("İşlemci ~{0:0}°", o.CpuIsi));
        if (o.GpuIsi >= 0) parcalar.Add(string.Format("GPU {0:0}°", o.GpuIsi));
        gIsi.Alt = o.CpuIsi >= 0 ? "İşlemci değeri yaklaşıktır (ACPI)" : (o.GpuIsi >= 0 ? "İşlemci ısısı bu bilgisayarda okunamıyor" : "Bu bilgisayarda okunamıyor");
        gIsi.Ayarla(parcalar.Count > 0 ? string.Join("  ", parcalar) : "–", Math.Max(o.CpuIsi, o.GpuIsi) / 100);

        // Ekran kartını yoğun kullananlar (oyunlar) Temizle'de bellekten atılmaz; takılma yapar.
        var oyun = new HashSet<int>();
        foreach (var g in o.Gruplar) if (g.Gpu >= 10) foreach (var pid in g.Pidler) oyun.Add(pid);
        oyunPidleri = oyun;

        // Uyarılar genel; oyuna özel ipucu yalnızca şu an ekran kartını yoğun kullanan bir program varsa.
        if (sKul / sTop >= 0.9)
        {
            lblBilgi.ForeColor = Renk.Kirmizi;
            lblBilgi.Text = "Sanal bellek dolmak üzere! Programlar (oyunlar dahil) \"bellek yetersiz\" diye kapanabilir." +
                            (oyun.Count > 0 ? " Oyundaysan ilerlemeni kaydet." : "") + "\n" +
                            "Şimdilik Temizle'ye bas; sık oluyorsa bir kere \"Sanal belleği büyüt\"e bas.";
        }
        else if (ramKul / ramTop >= 0.9)
        {
            lblBilgi.ForeColor = Renk.Turuncu;
            lblBilgi.Text = "RAM neredeyse dolu; bilgisayar" + (oyun.Count > 0 ? " ve açık oyun" : "") +
                            " takılabilir. Gereksiz programları işaretleyip Temizle'ye bas.\n" +
                            "Sağ tık → tek programı hemen kapat. Sütun başlığına tıkla → sırala.";
        }
        else
        {
            lblBilgi.ForeColor = Renk.Soluk;
            lblBilgi.Text = BilgiMetni;
        }
    }

    void ListeyiGuncelle(List<Grup> gruplar)
    {
        dolduruluyor = true;
        liste.BeginUpdate();
        try
        {
            var gorulen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in gruplar)
            {
                gorulen.Add(g.Ad);
                ListViewItem oge;
                if (!ogeler.TryGetValue(g.Ad, out oge))
                {
                    string ad = g.Gorunen.Equals(g.Ad, StringComparison.OrdinalIgnoreCase) ? g.Ad : g.Gorunen + "  (" + g.Ad + ")";
                    oge = new ListViewItem(ad) { Tag = g };
                    for (int i = 1; i < Basliklar.Length; i++) oge.SubItems.Add("");
                    bool isaretli;
                    if (!hatirlanan.TryGetValue(g.Ad, out isaretli))
                        isaretli = !g.Pencereli && !g.Bagli && !g.Konsol && !VarsayilanIsaretsiz.Contains(g.Ad)
                                   && !VarsayilanIsaretsizOnek.Any(o => g.Ad.StartsWith(o, StringComparison.OrdinalIgnoreCase));
                    oge.Checked = isaretli;
                    Yaz(oge, g);
                    liste.Items.Add(oge);
                    ogeler[g.Ad] = oge;
                }
                else
                {
                    oge.Tag = g;
                    Yaz(oge, g);
                }
            }
            foreach (var ad in ogeler.Keys.Where(k => !gorulen.Contains(k)).ToList())
            {
                liste.Items.Remove(ogeler[ad]);
                ogeler.Remove(ad);
            }
            liste.Sort();
        }
        finally
        {
            liste.EndUpdate();
            dolduruluyor = false;
        }
        SutunlariAyarla();
        if (!temizleniyor && lblDurum.Tag == null)
            lblDurum.Text = string.Format("{0} program listelendi, {1} tanesi işaretli.",
                liste.Items.Count, liste.Items.Cast<ListViewItem>().Count(i => i.Checked));
    }

    static void Yaz(ListViewItem oge, Grup g)
    {
        Ayarla(oge.SubItems[1], g.Pencereli ? "Açık pencere" : g.Bagli ? "Uygulama parçası" : g.Konsol ? "Konsol işlemi" : "Arka plan");
        Ayarla(oge.SubItems[2], g.Cpu >= 0.05 ? string.Format("%{0:0.0}", g.Cpu) : "–");
        Ayarla(oge.SubItems[3], Boyut(g.Bellek));
        Ayarla(oge.SubItems[4], g.Gpu >= 0.5 ? string.Format("%{0:0}", g.Gpu) : "–");
        Ayarla(oge.SubItems[5], g.Vram >= 1048576 ? Boyut(g.Vram) : "–");
    }

    static void Ayarla(ListViewItem.ListViewSubItem s, string metin) { if (s.Text != metin) s.Text = metin; }

    public static string Boyut(long bayt)
    {
        return bayt >= 1073741824 ? string.Format("{0:0.0} GB", bayt / 1073741824.0) : string.Format("{0:N0} MB", bayt / 1048576);
    }

    void BasliklariYaz()
    {
        for (int i = 0; i < Basliklar.Length; i++)
            liste.Columns[i].Text = Basliklar[i] + (i == siralayici.Sutun ? (siralayici.Azalan ? " ▼" : " ▲") : "");
        liste.Invalidate();
    }

    // Program sütunu kalan genişliği doldurur (dikey kaydırma çubuğu hesaba katılır).
    void SutunlariAyarla()
    {
        int kalan = liste.ClientSize.Width;
        for (int i = 1; i < liste.Columns.Count; i++) kalan -= liste.Columns[i].Width;
        if (kalan > 140 && liste.Columns[0].Width != kalan) liste.Columns[0].Width = kalan;
    }

    // Sanal bellek = RAM + sayfa dosyası. Sayfa dosyası küçük ya da kapalıysa programlar "bellek yetersiz" der;
    // sistem sürücüsünde (SanalBellek.Surucu) RAM'e göre boyutlanan (bkz. SanalBellek.Hedef) bir sayfa dosyası ayarlanır.
    void SanalBellek_Click(object sender, EventArgs e)
    {
        SanalBellekAyarla();
    }

    // Kendini yönetici olarak argümanlarla çalıştırır; başarılıysa true, değilse kullanıcıya söyleyip false.
    bool YoneticiOlarakCalistir(string argumanlar, string baslik)
    {
        int sonuc;
        try
        {
            var bilgi = new ProcessStartInfo(Application.ExecutablePath, argumanlar) { UseShellExecute = true, Verb = "runas" };
            using (var p = Process.Start(bilgi)) { p.WaitForExit(); sonuc = p.ExitCode; }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this, "Yönetici izni verilmedi, hiçbir şey değişmedi.", baslik, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        if (sonuc != 0)
        {
            MessageBox.Show(this, "Ayar yazılamadı, hiçbir şey değişmedi.", baslik, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        return true;
    }

    void YenidenBaslatSor(string mesaj, string baslik)
    {
        if (MessageBox.Show(this, mesaj + "\n\nŞimdi yeniden başlatılsın mı? (Önce açık işlerini kaydet.)", baslik,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 10 /c \"Yaman PC Cleaner: sanal bellek ayari icin yeniden baslatiliyor\"")
            {
                UseShellExecute = false, CreateNoWindow = true
            });
        }
        catch { }
    }

    // Sanal bellek büyütülmeli mi; büyütülecekse boyutlar (MB). Gerek yoksa ya da diskte yer yoksa false ve sebebi.
    internal static bool SanalHedef(out long ilk, out long enCok, out string neden)
    {
        ilk = enCok = 0;
        neden = null;
        var satirlar = SanalBellek.Oku();
        int hedefIlk, hedefEnCok;
        var m = Yerel.Bellek();
        SanalBellek.Hedef(m.ullTotalPhys, out hedefIlk, out hedefEnCok);
        // Windows yönetiyorsa kayıt defterinde boyut yok; şu anki gerçek boyut (sınır − RAM) hedeften büyükse
        // yeni ayar sanal belleği küçültür, dokunma.
        long suankiMB = m.ullTotalPageFile > m.ullTotalPhys ? (long)((m.ullTotalPageFile - m.ullTotalPhys) / 1048576) : 0;
        if (SanalBellek.YedekVar || SanalBellek.ElleIlkMB(satirlar) >= hedefIlk || suankiMB >= hedefIlk)
        {
            neden = "Sayfa dosyası zaten yeterince büyük, değiştirmeye gerek yok.";
            return false;
        }
        long bosMB;
        try { bosMB = new DriveInfo(SanalBellek.Surucu).AvailableFreeSpace / 1048576; } catch { bosMB = 0; }
        ilk = Math.Min(hedefIlk, bosMB - 10240) / 1024 * 1024;   // diskte en az 10 GB boş kalsın
        if (ilk < 4096)
        {
            neden = string.Format("{0} diskinde yeterli boş yer yok ({1:0.0} GB). Önce en az 15 GB yer aç.", SanalBellek.Surucu, bosMB / 1024.0);
            return false;
        }
        enCok = Math.Max(ilk, Math.Min(hedefEnCok, bosMB - 5120) / 1024 * 1024);
        return true;
    }

    // Kısa bilgi: iş yazısı yerinde 8 sn görünür (bir iş sürüyorsa yazılmaz; iş yazısını ezmesin).
    public void BilgiYaz(string metin)
    {
        if (temizleniyor || IsDisposed) return;
        lblIs.Text = metin;
        var t = new System.Windows.Forms.Timer { Interval = 8000 };
        t.Tick += delegate
        {
            t.Stop(); t.Dispose();
            if (!temizleniyor && !IsDisposed && lblIs.Text == metin) lblIs.Text = "";
        };
        t.Start();
    }

    void OptimizeDurumu(bool calisiyor, string metin)
    {
        temizleniyor = calisiyor;
        btnOto.Enabled = btnFull.Enabled = btnTemizle.Enabled = btnSanal.Enabled = !calisiyor;
        lblIs.Text = metin ?? "";
    }

    // Soru sormadan, yönetici izni istemeden yalnızca risksiz işler: RAM boşaltma, geçici dosyalar
    // ve kapalı programların önbellekleri. Program kapatmaz, ayar değiştirmez.
    // Tepsi menüsünden de çağrılır (bekçi); pencere gizliyken sonuç balonla söylenir.
    async void OtomatikOptimize_Click(object sender, EventArgs e)
    {
        // Pencere gizliyse ya da önde tam ekran oyun varsa ileti kutusu açılmaz (oyunun önüne geçip onu küçültür).
        bool sessiz = !Visible || (bekci != null && bekci.OyunSuruyor);
        if (temizleniyor || !btnOto.Enabled || diskSekmesi.Calisiyor)
        {
            if (sessiz && bekci != null) bekci.Bildir("Otomatik optimize", "Şu an başka bir iş sürüyor; bitince yeniden dene.", ToolTipIcon.Info);
            return;
        }
        OptimizeDurumu(true, "Otomatik optimize çalışıyor...");
        var once = Yerel.Bellek();
        long diskOnce = DiskSekmesi.BosAlan();
        var atla = oyunPidleri;
        bool olcumEski = !zamanlayici.Enabled;
        await Task.Run(() =>
        {
            if (olcumEski) { var taze = TazeOyunPidleri(); if (taze != null) atla = taze; }
            CalismaKumeleriniBosalt(atla);
            foreach (var t in DiskTemizlik.Liste(false))
            {
                if (t.Yonetici || !t.Varsayilan || t.Ozel != null) continue;
                DiskTemizlik.Hesapla(t);
                if (t.AcikSurec == null) { try { DiskTemizlik.Temizle(t); } catch { } }
            }
            Thread.Sleep(800);
        });
        long ram = Math.Max(0, (long)Yerel.Bellek().ullAvailPhys - (long)once.ullAvailPhys);
        long disk = Math.Max(0, DiskSekmesi.BosAlan() - diskOnce);
        Kazanc.Ekle(ram, disk);
        OptimizeDurumu(false, null);
        diskSekmesi.Yenile();
        Olc();

        string metin = string.Format("Bitti.\n\n•  RAM'de {0} boşaldı.\n•  Diskte {1} yer açıldı (geçici dosyalar ve kapalı programların önbellekleri).\n\n" +
                                     "Hiçbir program kapatılmadı, hiçbir ayara dokunulmadı.", Boyut(ram), Boyut(disk));
        long ilk, enCok; string neden;
        bool sanalKucuk = SanalHedef(out ilk, out enCok, out neden);
        bool oyunda = bekci != null && bekci.OyunSuruyor;   // iş sürerken oyuna geçilmiş olabilir
        if (bekci != null && (sessiz || oyunda || !Visible))
        {
            if (oyunda) Ses.Cal(SesTuru.Tamam);
            bekci.Bildir("Otomatik optimize bitti",
                string.Format("RAM'de {0} boşaldı, diskte {1} yer açıldı. Hiçbir program kapatılmadı.{2}", Boyut(ram), Boyut(disk),
                    sanalKucuk ? " Sanal bellek küçük: programı açıp \"Sanal belleği büyüt\"e bas." : ""),
                ToolTipIcon.Info);
            return;
        }
        if (sanalKucuk)
            metin += "\n\nİpucu: sanal bellek bu bilgisayar için küçük. \"Full optimize\" ya da \"Sanal belleği büyüt\" ile kalıcı olarak düzeltebilirsin.";
        MessageBox.Show(this, metin, "Otomatik optimize", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Seçenekleri tek pencerede sunar, seçilenleri sırayla yapar; yönetici gerekenler tek izinle.
    async void FullOptimize_Click(object sender, EventArgs e)
    {
        // Temizle ya da Disk sekmesindeki temizlik sürerken iki iş aynı programlara / klasörlere dokunmasın.
        if (temizleniyor || diskSekmesi.Calisiyor) return;
        var programlar = liste.Items.Cast<ListViewItem>().Where(o => o.Checked).Select(o => (Grup)o.Tag).ToList();
        long ilk, enCok; string neden;
        bool sanalGerekli = SanalHedef(out ilk, out enCok, out neden);
        string sanalAciklama = sanalGerekli
            ? string.Format("{0} sürücüsünde {1} GB sayfa dosyası (gerekirse {2} GB'a kadar); programların (oyunlar dahil) \"bellek yetersiz\" diye kapanmasını önler. Yeniden başlatınca geçerli olur.",
                SanalBellek.Surucu, ilk / 1024, enCok / 1024)
            : neden;
        long hazirdaBoyut = DiskTemizlik.HazirdaBoyutu();

        bool prog, ram, disk, derin, geri, sanal, hazirda;
        using (var f = new OptimizeFormu(programlar.Count, sanalAciklama, sanalGerekli, hazirdaBoyut))
        {
            if (f.ShowDialog(this) != DialogResult.OK) return;
            if (temizleniyor) return;   // pencere açıkken tepsiden Otomatik optimize başlamış olabilir
            prog = f.Programlar.Checked; ram = f.Ram.Checked; disk = f.Disk.Checked; derin = f.Derin.Checked;
            geri = f.GeriDonusum.Checked; sanal = f.Sanal.Checked; hazirda = f.Hazirda.Checked;
        }

        OptimizeDurumu(true, "Full optimize çalışıyor...");
        var rapor = new List<string>();
        var once = Yerel.Bellek();
        long diskOnce = DiskSekmesi.BosAlan();

        if (prog && programlar.Count > 0)
        {
            lblIs.Text = "Arka plan programları kapatılıyor...";
            int kapanan = 0, kalan = 0;
            await Task.Run(() => { foreach (var g in programlar) { if (GrubuKapat(g) == 1) kapanan++; else kalan++; } });
            rapor.Add(string.Format("{0} arka plan programı kapatıldı{1}.", kapanan, kalan > 0 ? string.Format(", {0} tanesi kapanmadı", kalan) : ""));
        }
        if (ram)
        {
            lblIs.Text = "RAM boşaltılıyor...";
            var atla = oyunPidleri;
            await Task.Run(() => { CalismaKumeleriniBosalt(atla); Thread.Sleep(800); });
        }

        var yoneticiIsleri = new List<string>();
        if (disk || geri)
        {
            lblIs.Text = "Gereksiz dosyalar temizleniyor...";
            var secilen = await Task.Run(() =>
            {
                var l = new List<Temizlik>();
                foreach (var t in DiskTemizlik.Liste(derin))
                {
                    bool istendi = t.Ozel == "geridonusum" ? geri : disk && t.Varsayilan;
                    if (!istendi) continue;
                    DiskTemizlik.Hesapla(t);
                    if (t.AcikSurec != null) continue;
                    l.Add(t);
                    if (!t.Yonetici) { try { DiskTemizlik.Temizle(t); } catch { } }
                }
                return l;
            });
            yoneticiIsleri.AddRange(secilen.Where(t => t.Yonetici).Select(t => t.Anahtar));
        }
        if (hazirda) yoneticiIsleri.Add("hazirda");
        if (sanal) yoneticiIsleri.Add(string.Format("sanal={0},{1}", ilk, enCok));

        bool yenidenBaslat = false;
        if (yoneticiIsleri.Count > 0)
        {
            lblIs.Text = derin ? "Windows kalıntıları temizleniyor; Windows'un aracı uzun sürebilir..." : "Windows kalıntıları temizleniyor...";
            var eskiSanal = SanalBellek.Oku();
            int kod = -1;
            try
            {
                var bilgi = new ProcessStartInfo(Application.ExecutablePath,
                    "--disk-temizle " + (derin ? "derin " : "normal ") + string.Join(" ", yoneticiIsleri))
                {
                    UseShellExecute = true, Verb = "runas"
                };
                var p = Process.Start(bilgi);
                await Task.Run(() => { p.WaitForExit(); kod = p.ExitCode; p.Dispose(); });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                rapor.Add("Yönetici izni verilmedi; Windows kalıntıları" + (sanal ? ", sanal bellek" : "") + (hazirda ? ", hazırda bekletme" : "") + " atlandı.");
            }
            if (kod == 0)
            {
                if (sanal)
                {
                    SanalBellek.Yedekle(eskiSanal);
                    rapor.Add(string.Format("Sanal bellek {0} GB'a büyütüldü (yeniden başlatınca geçerli; geri almak için \"Sanal bellek ayarı\" → önceki ayara dön).", ilk / 1024));
                    yenidenBaslat = true;
                }
                if (hazirda) rapor.Add("Hazırda bekletme kapatıldı (geri açmak: yönetici CMD'de powercfg /h on).");
            }
            else if (kod > 0) rapor.Add("Sanal bellek ayarı yazılamadı, değişmedi.");
        }

        long ramB = Math.Max(0, (long)Yerel.Bellek().ullAvailPhys - (long)once.ullAvailPhys);
        long diskB = Math.Max(0, DiskSekmesi.BosAlan() - diskOnce);
        if (prog || ram) rapor.Add(string.Format("RAM'de {0} boşaldı.", Boyut(ramB)));
        if (disk || geri || hazirda) rapor.Add(string.Format("Diskte {0} yer açıldı.", Boyut(diskB)));
        Kazanc.Ekle(prog || ram ? ramB : 0, disk || geri || hazirda ? diskB : 0);
        OptimizeDurumu(false, null);
        diskSekmesi.Yenile();
        Olc();

        string metin = "Full optimize bitti.\n\n" + string.Join("\n", rapor.Select(r => "•  " + r));
        // Pencere tepsideyken (oyun sırasında) görünmeyen pencereye bağlı soru açılmasın; balonla söylenir.
        if (!Visible && bekci != null)
        {
            bekci.Bildir("Full optimize bitti", (yenidenBaslat ? "Sanal bellek ayarlandı; yeniden başlatınca geçerli olur. " : "") +
                "Ayrıntı için programı aç.", ToolTipIcon.Info);
            return;
        }
        if (yenidenBaslat) YenidenBaslatSor(metin, "Full optimize");
        else MessageBox.Show(this, metin, "Full optimize", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Seçim penceresini açar: önerilen / hazır / elle boyut, Windows'a bırakma, varsa programdan önceki ayara dönüş.
    void SanalBellekAyarla()
    {
        const string Baslik = "Sanal bellek";
        var satirlar = SanalBellek.Oku();
        var m = Yerel.Bellek();
        int hedefIlk, hedefEnCok;
        SanalBellek.Hedef(m.ullTotalPhys, out hedefIlk, out hedefEnCok);
        long ramMB = (long)(m.ullTotalPhys / 1048576);
        long suankiMB = m.ullTotalPageFile > m.ullTotalPhys ? (long)((m.ullTotalPageFile - m.ullTotalPhys) / 1048576) : 0;
        // Eski sayfa dosyası yenisine yer açar; boş yere onun boyutu da eklenir.
        long bosMB = 0;
        try { bosMB = new DriveInfo(SanalBellek.Surucu).AvailableFreeSpace / 1048576; } catch { }
        try { bosMB += new FileInfo(SanalBellek.Surucu + "\\pagefile.sys").Length / 1048576; } catch { }
        string[] yedek = SanalBellek.YedekVar ? SanalBellek.YedekOku() : null;

        string secim;
        int ilk, enCok;
        using (var f = new SanalBellekFormu(satirlar, ramMB, suankiMB, bosMB, hedefIlk, hedefEnCok, yedek))
        {
            if (f.ShowDialog(this) != DialogResult.OK) return;
            secim = f.Secim; ilk = f.IlkMB; enCok = f.EnCokMB;
        }

        if (secim == "geri")
        {
            string argumanlar = "--sanal-bellek-geri" + string.Concat(yedek.Select(s => " \"" + s.Trim() + "\""));
            if (!YoneticiOlarakCalistir(argumanlar, Baslik)) return;
            SanalBellek.YedekSil();
            YenidenBaslatSor("Programdan önceki ayara dönüldü; bilgisayar yeniden başlayınca geçerli olacak.", Baslik);
            return;
        }
        string arg = secim == "otomatik" ? "--sanal-bellek-geri \"?:\\pagefile.sys\"" : string.Format("--sanal-bellek {0} {1}", ilk, enCok);
        if (!YoneticiOlarakCalistir(arg, Baslik)) return;
        SanalBellek.Yedekle(satirlar);
        YenidenBaslatSor("Sanal bellek ayarlandı; bilgisayar yeniden başlayınca geçerli olacak.\n" +
            "Vazgeçersen aynı düğmeden \"Programdan önceki ayara dön\"ü seçebilirsin.", Baslik);
    }

    async void Temizle_Click(object sender, EventArgs e)
    {
        var secili = liste.Items.Cast<ListViewItem>().Where(o => o.Checked).Select(o => (Grup)o.Tag).ToList();
        if (secili.Count > 0)
        {
            var adlar = string.Join("\n", secili.Take(12).Select(g => "•  " + g.Gorunen));
            if (secili.Count > 12) adlar += string.Format("\n•  ... ve {0} program daha", secili.Count - 12);
            var metin = string.Format("{0} program kapatılacak:\n\n{1}\n\nKaydedilmemiş işi olan pencereler kapanmaz, seni bekler. Devam edilsin mi?",
                secili.Count, adlar);
            if (MessageBox.Show(this, metin, "Yaman PC Cleaner", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        }
        if (temizleniyor) return;   // soru açıkken tepsiden Otomatik optimize başlamış olabilir
        await Kapat(secili, true);
    }

    async Task Kapat(List<Grup> gruplar, bool bosalt)
    {
        OptimizeDurumu(true, null);   // optimize düğmeleri de kapanır: iki iş aynı programları kapatmasın
        lblDurum.Tag = "mesaj";
        lblDurum.ForeColor = Renk.Soluk;
        lblDurum.Text = bosalt ? "Temizleniyor..." : "Kapatılıyor...";
        SecimleriKaydet();

        var once = Yerel.Bellek();
        var atla = oyunPidleri;
        int kapanan = 0, acikKalan = 0, hata = 0;

        await Task.Run(() =>
        {
            foreach (var g in gruplar)
            {
                int sonuc = GrubuKapat(g);
                if (sonuc == 1) kapanan++; else if (sonuc == 0) acikKalan++; else hata++;
            }
            if (bosalt) CalismaKumeleriniBosalt(atla);
            Thread.Sleep(800);
        });

        var sonra = Yerel.Bellek();
        long ram = Math.Max(0, (long)sonra.ullAvailPhys - (long)once.ullAvailPhys) / 1048576;
        long sanal = Math.Max(0, (long)sonra.ullAvailPageFile - (long)once.ullAvailPageFile) / 1048576;
        Kazanc.Ekle(ram * 1048576, 0);

        var mesaj = string.Format("{0} program kapatıldı · RAM'de {1:N0} MB, sanal bellekte {2:N0} MB yer açıldı.", kapanan, ram, sanal);
        if (acikKalan > 0) mesaj += string.Format(" {0} program kaydetme bekliyor olabilir.", acikKalan);
        if (hata > 0) mesaj += string.Format(" {0} program kapatılamadı.", hata);
        lblDurum.ForeColor = Renk.Yesil;
        lblDurum.Text = mesaj;

        OptimizeDurumu(false, null);
        Olc();
        // Sonuç mesajı birkaç ölçüm boyunca ekranda kalsın.
        var bekle = new System.Windows.Forms.Timer { Interval = 8000 };
        bekle.Tick += delegate { bekle.Dispose(); lblDurum.Tag = null; lblDurum.ForeColor = Renk.Soluk; };
        bekle.Start();
    }

    // 1 = kapandı, 0 = penceresi açık kaldı (ör. "Kaydet?" sorusu), -1 = yetki/hata
    static int GrubuKapat(Grup g)
    {
        int oturum;
        using (var ben = Process.GetCurrentProcess()) oturum = ben.SessionId;
        var surecler = new List<Process>();
        try
        {
            foreach (var p in Process.GetProcessesByName(g.Ad))
            {
                if (p.SessionId == oturum) surecler.Add(p); else p.Dispose();
            }

            // Pencereli programa önce düzgünce "kapan" de; kaydedilmemiş iş varsa zorlamayız.
            var pencereliler = surecler.Where(p => !p.HasExited && p.MainWindowHandle != IntPtr.Zero).ToList();
            foreach (var p in pencereliler) p.CloseMainWindow();
            foreach (var p in pencereliler)
            {
                if (p.WaitForExit(2500)) continue;
                p.Refresh();
                if (p.MainWindowHandle != IntPtr.Zero) return 0; // hâlâ görünür pencere var: kullanıcıya bırak
            }

            // Kalanlar (arka plan / tepsiye küçülenler) zorla kapatılır.
            bool hepsi = true;
            foreach (var p in surecler)
            {
                try { if (!p.HasExited) p.Kill(); }
                catch { hepsi = false; }
            }
            return hepsi ? 1 : -1;
        }
        catch { return -1; }
        finally { foreach (var p in surecler) p.Dispose(); }
    }

    bool kisayolCalisiyor;

    // Genel kısayol ve tepsideki "RAM boşalt": oyundayken de güvenli. Yalnızca RAM boşaltılır; disk temizliği yok
    // (oyunda takılma yapmasın). Ekran kartını yoğun kullananlar ve öndeki program atlanır. Hiçbir pencere / ileti
    // kutusu açmaz: sonuç kısa bir sesle ve balonla söylenir. RAM boşaltmak sanal belleği (commit) düşürmez; sanal
    // bellek doluysa uyarı sesi çalar ve en çok kullanan oyun dışı programın adı söylenir. Hiçbir şeyi kapatmaz.
    async void KisayolOptimize() { await KisayolIsi(false); }

    // sessiz: Turbo sekmesinin oyun profili "oyun açılınca RAM boşalt" için; aynı iş ama ses, balon ve "meşgul" bildirimi yok
    // (başka iş sürüyorsa sessizce atlanır).
    async Task KisayolIsi(bool sessiz)
    {
        if (kisayolCalisiyor) return;
        if (temizleniyor || diskSekmesi.Calisiyor)
        {
            if (sessiz) return;
            Ses.Cal(SesTuru.Uyari);
            if (bekci != null) bekci.Bildir("RAM boşaltılamadı", "Şu an başka bir iş sürüyor; bitince yeniden dene.", ToolTipIcon.Info);
            return;
        }
        kisayolCalisiyor = true;
        OptimizeDurumu(true, sessiz ? "" : "RAM boşaltılıyor...");
        try
        {
            var once = Yerel.Bellek();
            int onde = TamEkran.OndekiSurec();
            var atla = new HashSet<int>(oyunPidleri);
            bool olcumEski = !zamanlayici.Enabled;
            EnCokKullanan suclu = null;
            double commit = 0;
            await Task.Run(() =>
            {
                if (olcumEski) { var taze = TazeOyunPidleri(); if (taze != null) atla.UnionWith(taze); }
                OndekiProgramiEkle(atla, onde);
                CalismaKumeleriniBosalt(atla);
                Thread.Sleep(500);
                var m = Yerel.Bellek();
                if (m.ullTotalPageFile > 0) commit = (double)(m.ullTotalPageFile - m.ullAvailPageFile) / m.ullTotalPageFile;
                if (commit >= 0.90) suclu = EnCokSanalBellek(atla);
#if DENEME
                if (suclu == null) suclu = EnCokSanalBellek(atla);   // sanal bellek dolu olmasa da sınansın
#endif
            });
            long ram = Math.Max(0, (long)Yerel.Bellek().ullAvailPhys - (long)once.ullAvailPhys);
            Kazanc.Ekle(ram, 0);
            int yuzde = (int)Math.Floor(Math.Min(1, commit) * 100);
#if DENEME
            Deneme.Yaz(string.Format("kısayol işi: önde={0} tamEkran={1} oyunda={2} sanal=%{3} suçlu={4}", onde, TamEkran.Var(),
                bekci != null && bekci.OyunSuruyor, yuzde, suclu != null ? suclu.Ad + " " + Boyut(suclu.Bellek) : "-"));
#endif
            if (sessiz) { }
            else if (commit >= 0.90)
            {
                Ses.Cal(SesTuru.Uyari);
                string kim = suclu != null
                    ? string.Format(" En çok kullanan: {0} ({1}); gerekmiyorsa kapat.", Kisalt(suclu.Ad, 40), Boyut(suclu.Bellek))
                    : " Gereksiz programları kapat.";
                if (bekci != null)
                    bekci.Bildir(string.Format("Sanal bellek %{0} dolu!", yuzde),
                        string.Format("RAM'de {0} boşaldı ama RAM boşaltmak sanal belleği düşürmez.{1} Sık oluyorsa \"Sanal belleği büyüt\"e bas.",
                            Boyut(ram), kim),
                        ToolTipIcon.Warning);
            }
            else
            {
                Ses.Cal(SesTuru.Tamam);
                if (bekci != null)
                    bekci.Bildir("RAM boşaltıldı",
                        string.Format("RAM'de {0} boşaldı · sanal bellek %{1}. Oyun ve öndeki program atlandı, hiçbir şey kapatılmadı.",
                            Boyut(ram), yuzde),
                        ToolTipIcon.Info);
            }
        }
        catch { }
        finally
        {
            OptimizeDurumu(false, null);
            kisayolCalisiyor = false;
            if (Visible) Olc();
        }
    }

    // Oyun profili: öndeki programın (aynı adlı bütün parçalarıyla) ekran kartı kullanımı %10 ve üstü mü? Arayüz
    // iş parçacığında çağrılır, beklemez. Pencere açıksa 2 sn'lik ölçümün sonucu kullanılır; gizliyken (tepside)
    // ölçüm durduğu için bir ölçüm yapılır (iki çağrı arasındaki fark; ilk çağrı genelde 0 döner, profil 2 kontrol ister).
    Task<bool> OndeGpuKullaniyor(int pid)
    {
        if (zamanlayici.Enabled) return Task.FromResult(oyunPidleri.Contains(pid));
        return Task.Run(() =>
        {
            try
            {
                Olcum o;
                lock (olcerKilit) { if (olcer == null) olcer = new Olcer(); o = olcer.Topla(); }
                foreach (var g in o.Gruplar) if (g.Gpu >= 10 && g.Pidler.Contains(pid)) return true;
            }
            catch { }
            return false;
        });
    }

    static string Kisalt(string s, int n) { return s == null || s.Length <= n ? s : s.Substring(0, n - 1) + "…"; }

    // Pencere gizliyken 2 sn'lik ölçüm durduğu için oyun listesi eski olabilir: iki kısa ölçümle yenilenir
    // (ekran kartı yüzdesi iki örnek arasındaki farktan çıkar). Oyunun belleği boşaltılırsa oyun takılır.
    // Arka plan iş parçacığında çağrılır; ölçülemezse null.
    HashSet<int> TazeOyunPidleri()
    {
        try
        {
            lock (olcerKilit) { if (olcer == null) olcer = new Olcer(); olcer.Topla(); }
            Thread.Sleep(1000);
            Olcum o;
            lock (olcerKilit) o = olcer.Topla();
            var oyun = new HashSet<int>();
            foreach (var g in o.Gruplar) if (g.Gpu >= 10) foreach (var pid in g.Pidler) oyun.Add(pid);
            return oyun;
        }
        catch { return null; }
    }

    // Öndeki program (oyun menüdeyken ekran kartını az kullanabilir) ve aynı adlı bütün parçaları atlanır.
    static void OndekiProgramiEkle(HashSet<int> atla, int onde)
    {
        if (onde <= 0) return;
        atla.Add(onde);
        try
        {
            string ad;
            using (var p = Process.GetProcessById(onde)) ad = p.ProcessName;
            foreach (var p in Process.GetProcessesByName(ad)) using (p) atla.Add(p.Id);
        }
        catch { }
    }

    class EnCokKullanan { public string Ad; public long Bellek; }

    // Sanal belleği (özel / commit bellek) en çok kullanan, kapatılabilir oyun dışı program; Sistem sekmesinin
    // koruma kuralları geçerli (Windows klasörü, güvenlik / hile önleyici / sürücü / üretici yazılımı sayılmaz).
    static EnCokKullanan EnCokSanalBellek(HashSet<int> atla)
    {
        string winKlasoru = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";
        int benim, oturum;
        using (var b = Process.GetCurrentProcess()) { benim = b.Id; oturum = b.SessionId; }
        var toplam = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var yollar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    if (p.Id == benim || p.Id == 0 || p.SessionId != oturum || atla.Contains(p.Id)) continue;
                    string ad = p.ProcessName;
                    if (string.IsNullOrEmpty(ad) || Korunuyor(ad)) continue;
                    string yol;
                    if (!yollar.TryGetValue(ad, out yol))
                    {
                        yol = SurecYolu(p.Id);
                        yollar[ad] = yol;
                    }
                    if (string.IsNullOrEmpty(yol) || yol.StartsWith(winKlasoru, StringComparison.OrdinalIgnoreCase) || Korunuyor(ad, yol)) continue;
                    long eski;
                    toplam.TryGetValue(ad, out eski);
                    toplam[ad] = eski + p.PrivateMemorySize64;
                }
                catch { }
            }
        }
        if (toplam.Count == 0) return null;
        var en = toplam.OrderByDescending(k => k.Value).First();
        string gorunen = en.Key;
        try
        {
            string d = FileVersionInfo.GetVersionInfo(yollar[en.Key]).FileDescription;
            if (!string.IsNullOrWhiteSpace(d)) gorunen = d.Trim();
        }
        catch { }
        return new EnCokKullanan { Ad = gorunen, Bellek = en.Value };
    }

    static string SurecYolu(int pid)
    {
        IntPtr h = Yerel.OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, pid);
        if (h == IntPtr.Zero) return null;
        try
        {
            var tampon = new StringBuilder(1024);
            int boyut = tampon.Capacity;
            return Yerel.QueryFullProcessImageName(h, 0, tampon, ref boyut) ? tampon.ToString() : null;
        }
        finally { Yerel.CloseHandle(h); }
    }

    static void CalismaKumeleriniBosalt(HashSet<int> atla)
    {
#if DENEME
        // Deneme derlemesi (araclar\test\deneme-derle.cmd): gerçek bilgisayarda hiçbir programın RAM'ine dokunmaz.
        Deneme.Yaz("RAM boşaltma atlandı; atlanan süreç sayısı " + atla.Count);
        if (atla.Count >= 0) return;
#endif
        const uint PROCESS_QUERY_INFORMATION = 0x0400, PROCESS_SET_QUOTA = 0x0100;
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (atla.Contains(p.Id)) continue;
                IntPtr h = Yerel.OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA, false, p.Id);
                if (h != IntPtr.Zero) { Yerel.EmptyWorkingSet(h); Yerel.CloseHandle(h); }
            }
            catch { }
            finally { p.Dispose(); }
        }
    }

    void SecimleriYukle()
    {
        try
        {
            if (!File.Exists(AyarDosyasi)) return;
            foreach (var satir in File.ReadAllLines(AyarDosyasi))
                if (satir.Length > 1 && (satir[0] == '+' || satir[0] == '-'))
                    hatirlanan[satir.Substring(1)] = satir[0] == '+';
        }
        catch { }
    }

    void SecimleriKaydet()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AyarDosyasi));
            File.WriteAllLines(AyarDosyasi, hatirlanan.Select(k => (k.Value ? "+" : "-") + k.Key));
        }
        catch { }
    }
}
