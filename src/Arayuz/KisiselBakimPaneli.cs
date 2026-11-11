// Kişisel Bakım paneli: Yaman'a özel karşılama, haftalık bakım planı, yerel işlem günlüğü,
// sistem özeti, geri yükleme noktası ve HTML bakım raporu. Tüm veriler bu bilgisayarda kalır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Görünen ad: yalnızca bu bilgisayarda saklanır, raporlarda ve karşılamada kullanılır.
static class Kisisel
{
    const string AyarAd = "kisisel_ad";

    public static string Ad
    {
        get
        {
            string s = Ayarlar.OkuMetin(AyarAd, "Yaman");
            return string.IsNullOrWhiteSpace(s) ? "Yaman" : s.Trim();
        }
        set { Ayarlar.YazMetin(AyarAd, (value ?? "").Trim()); }
    }
}

// Haftalık bakım planı: gün + saat. Son bakım tarihi ayrıca saklanır.
static class BakimPlani
{
    const string AyarAcik = "bakim_acik", AyarGun = "bakim_gun", AyarSaat = "bakim_saat", AyarSon = "bakim_son";

    public static readonly string[] GunAdlari =
    { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };

    public static bool Acik
    {
        get { return Ayarlar.Oku(AyarAcik, false); }
        set { Ayarlar.Yaz(AyarAcik, value); }
    }

    public static int Gun
    {
        get { int n = Ayarlar.OkuSayi(AyarGun, 4); return n >= 0 && n < 7 ? n : 4; }
        set { Ayarlar.YazSayi(AyarGun, Math.Max(0, Math.Min(6, value))); }
    }

    public static int Saat
    {
        get { int n = Ayarlar.OkuSayi(AyarSaat, 20); return n >= 0 && n <= 23 ? n : 20; }
        set { Ayarlar.YazSayi(AyarSaat, Math.Max(0, Math.Min(23, value))); }
    }

    public static DateTime? Son
    {
        get
        {
            string s = Ayarlar.OkuMetin(AyarSon, "");
            DateTime d;
            if (DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d;
            return null;
        }
    }

    public static void Tamamlandi()
    {
        Ayarlar.YazMetin(AyarSon, DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
    }

    public static DateTime Sonraki(DateTime simdi)
    {
        int bugun = ((int)simdi.DayOfWeek + 6) % 7;   // Pazartesi = 0
        int fark = (Gun - bugun + 7) % 7;
        var aday = simdi.Date.AddDays(fark).AddHours(Saat);
        if (aday <= simdi) aday = aday.AddDays(7);
        return aday;
    }

    public static string SonrakiMetin()
    {
        if (!Acik) return "Plan kapalı · otomatik hatırlatma yapılmaz";
        var s = Sonraki(DateTime.Now);
        int gun = ((int)s.DayOfWeek + 6) % 7;
        return "Sonraki bakım: " + s.ToString("dd.MM.yyyy HH:mm") + " · " + GunAdlari[gun];
    }
}

class DenetimKaydi
{
    public DateTime Zaman;
    public string Islem, Ayrinti;
}

// Yerel işlem günlüğü: temizlik, geri yükleme noktası ve rapor işlemleri.
// Dosya: %APPDATA%\YamanPcCleaner\denetim.txt. Son 240 kayıt tutulur.
static class Denetim
{
    static readonly object kilit = new object();
    static readonly string Dosya = Path.Combine(Ayarlar.Klasor, "denetim.txt");

    public static event Action Degisti;

    public static void Ekle(string islem, string ayrinti)
    {
        islem = TekSatir(islem);
        ayrinti = TekSatir(ayrinti);
        lock (kilit)
        {
            try
            {
                Directory.CreateDirectory(Ayarlar.Klasor);
                var satirlar = File.Exists(Dosya)
                    ? File.ReadAllLines(Dosya, Encoding.UTF8).ToList()
                    : new List<string>();
                satirlar.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + "|" + islem + "|" + ayrinti);
                if (satirlar.Count > 240) satirlar.RemoveRange(0, satirlar.Count - 240);
                string gecici = Dosya + ".yeni";
                File.WriteAllLines(gecici, satirlar.ToArray(), new UTF8Encoding(false));
                if (File.Exists(Dosya)) File.Replace(gecici, Dosya, null);
                else File.Move(gecici, Dosya);
            }
            catch { }
        }
        var d = Degisti;
        if (d != null) try { d(); } catch { }
    }

    // En yeniden eskiye doğru son kayıtlar.
    public static List<DenetimKaydi> Son(int adet)
    {
        var l = new List<DenetimKaydi>();
        lock (kilit)
        {
            try
            {
                if (!File.Exists(Dosya)) return l;
                var satirlar = File.ReadAllLines(Dosya, Encoding.UTF8);
                for (int i = satirlar.Length - 1; i >= 0 && l.Count < adet; i--)
                {
                    var p = satirlar[i].Split(new[] { '|' }, 3);
                    if (p.Length < 2) continue;
                    DateTime z;
                    DateTime.TryParseExact(p[0], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out z);
                    l.Add(new DenetimKaydi { Zaman = z, Islem = p[1], Ayrinti = p.Length > 2 ? p[2] : "" });
                }
            }
            catch { }
        }
        return l;
    }

    public static void Temizle()
    {
        lock (kilit)
        {
            try { if (File.Exists(Dosya)) File.Delete(Dosya); } catch { }
        }
        var d = Degisti;
        if (d != null) try { d(); } catch { }
    }

    static string TekSatir(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();
    }
}

class KisiselBakimPaneli : Panel
{
    static readonly Font SelamYazi = new Font("Segoe UI Semibold", 18.5f, FontStyle.Bold);
    static readonly Font AdYazi = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);

    public Action SimdiBakim;

    Label lblSelam, lblMakine, lblPlanSonraki, lblDurum, lblKimlik;
    Label lblPuan, lblPuanAlt, lblKazanc, lblKazancAlt, lblSon, lblSonAlt, lblGelecek, lblGelecekAlt;
    readonly Panel denetimSatirlar = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Renk.Panel };
    ComboBox cmbGun, cmbSaat;
    AnahtarDugmesi anPlan;
    TextBox txtAd;
    EylemDugmesi btnBakim, btnNokta, btnRapor;
    bool ayarlaniyor, kimlikYuklendi, kimlikYukleniyor;

    public KisiselBakimPaneli()
    {
        Dock = DockStyle.Fill;
        BackColor = Renk.Arka;

        var kok = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, BackColor = Renk.Arka, Margin = Padding.Empty
        };
        kok.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        kok.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        kok.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        kok.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        kok.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        kok.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        kok.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        // ---- Karşılama kartı ----
        var karsilama = new KartPaneli { Dock = DockStyle.Fill, Margin = Padding.Empty };

        var sol = new Panel { Dock = DockStyle.Fill, BackColor = Renk.Panel };
        var logo = new PictureBox
        {
            Image = Logo.Resim(), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Renk.Panel,
            Bounds = new Rectangle(20, 28, 58, 58)
        };
        lblSelam = new Label
        {
            Text = "Hoş geldin, " + Kisisel.Ad, Font = SelamYazi, ForeColor = Renk.Yazi, BackColor = Renk.Panel,
            Bounds = new Rectangle(94, 18, 500, 36), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        lblMakine = new Label
        {
            Text = MakineMetni(), Font = Tasarim.Alt, ForeColor = Renk.Soluk, BackColor = Renk.Panel,
            Bounds = new Rectangle(96, 52, 500, 19), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        sol.Controls.Add(logo);
        sol.Controls.Add(lblSelam);
        sol.Controls.Add(lblMakine);

        var adAlani = new Panel { Dock = DockStyle.Right, Width = 268, BackColor = Renk.Panel, Padding = new Padding(0, 16, 22, 12) };
        txtAd = new TextBox
        {
            Dock = DockStyle.Top, Height = 30, Text = Kisisel.Ad, Font = AdYazi,
            BackColor = Renk.PanelUzerinde, ForeColor = Renk.Yazi, BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 4, 0, 4)
        };
        txtAd.Leave += delegate { AdKaydet(); };
        txtAd.KeyDown += delegate(object s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { AdKaydet(); e.SuppressKeyPress = true; }
        };
        var adEtiket = new Label
        {
            Dock = DockStyle.Top, Height = 18, Text = "KİŞİSEL AD", Font = Tasarim.KpiEtiket,
            ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(1, 0, 0, 0)
        };
        adAlani.Controls.Add(txtAd);
        adAlani.Controls.Add(adEtiket);

        karsilama.Controls.Add(sol);
        karsilama.Controls.Add(adAlani);
        kok.Controls.Add(karsilama, 0, 0);

        // ---- KPI şeridi ----
        var kpi = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Renk.Arka, Margin = Padding.Empty };
        for (int i = 0; i < 4; i++) kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        kpi.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Label d1, a1, d2, a2, d3, a3, d4, a4;
        var k1 = Kpi("SİSTEM PUANI", out d1, out a1);
        var k2 = Kpi("TOPLAM KAZANÇ", out d2, out a2);
        var k3 = Kpi("SON BAKIM", out d3, out a3);
        var k4 = Kpi("SONRAKİ BAKIM", out d4, out a4);
        lblPuan = d1; lblPuanAlt = a1; lblKazanc = d2; lblKazancAlt = a2;
        lblSon = d3; lblSonAlt = a3; lblGelecek = d4; lblGelecekAlt = a4;
        k1.Margin = new Padding(0, 0, 6, 0);
        k2.Margin = new Padding(6, 0, 6, 0);
        k3.Margin = new Padding(6, 0, 6, 0);
        k4.Margin = new Padding(6, 0, 0, 0);
        kpi.Controls.Add(k1, 0, 0);
        kpi.Controls.Add(k2, 1, 0);
        kpi.Controls.Add(k3, 2, 0);
        kpi.Controls.Add(k4, 3, 0);
        kok.Controls.Add(kpi, 0, 2);

        // ---- Orta: plan + sistem özeti / günlük ----
        var orta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Renk.Arka, Margin = Padding.Empty };
        orta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        orta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        orta.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var solOrta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Renk.Arka, Margin = new Padding(0, 0, 8, 0) };
        solOrta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        solOrta.RowStyles.Add(new RowStyle(SizeType.Percent, 61));
        solOrta.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        solOrta.RowStyles.Add(new RowStyle(SizeType.Percent, 39));

        var plan = PlanKarti();
        var kimlik = KimlikKarti();
        solOrta.Controls.Add(plan, 0, 0);
        solOrta.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Renk.Arka, Margin = Padding.Empty }, 0, 1);
        solOrta.Controls.Add(kimlik, 0, 2);
        orta.Controls.Add(solOrta, 0, 0);

        var gunluk = GunlukKarti();
        gunluk.Margin = new Padding(8, 0, 0, 0);
        orta.Controls.Add(gunluk, 1, 0);
        kok.Controls.Add(orta, 0, 4);

        lblDurum = new Label
        {
            Dock = DockStyle.Fill, Font = Tasarim.Kucuk, ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft,
            Text = "Hazır.", AutoEllipsis = true
        };
        kok.Controls.Add(lblDurum, 0, 5);

        Controls.Add(kok);
        Denetim.Degisti += DenetimDegisti;
        Disposed += delegate { Denetim.Degisti -= DenetimDegisti; };
        VisibleChanged += delegate { if (Visible) Tazele(); };
    }

    void DenetimDegisti()
    {
        if (IsDisposed) return;
        try
        {
            if (IsHandleCreated) BeginInvoke(new Action(Tazele));
        }
        catch { }
    }

    public void Gosterildi()
    {
        Tazele();
        if (!kimlikYuklendi && !kimlikYukleniyor) KimlikOku();
    }

    void Tazele()
    {
        if (lblSelam == null) return;
        if (lblSelam.Text != "Hoş geldin, " + Kisisel.Ad) lblSelam.Text = "Hoş geldin, " + Kisisel.Ad;
        if (txtAd.Text != Kisisel.Ad && !txtAd.Focused) txtAd.Text = Kisisel.Ad;

        lblPuan.Text = DurumPanosu.SonPuan >= 0 ? DurumPanosu.SonPuan.ToString(CultureInfo.InvariantCulture) : "—";
        lblPuanAlt.Text = DurumPanosu.SonPuan >= 0 ? "100 üzerinden" : "henüz denetlenmedi";

        long toplam = Kazanc.DiskBayt + Kazanc.RamBayt;
        lblKazanc.Text = Kazanc.IsSayisi > 0 ? AnaForm.Boyut(toplam) : "—";
        lblKazancAlt.Text = Kazanc.IsSayisi > 0 ? Kazanc.IsSayisi.ToString("N0", CultureInfo.CurrentCulture) + " bakım" : "ilk temizlik bekleniyor";

        var son = BakimPlani.Son;
        lblSon.Text = son.HasValue ? son.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : "—";
        lblSonAlt.Text = son.HasValue ? son.Value.ToString("HH:mm", CultureInfo.InvariantCulture) + " · son planlı bakım" : "henüz plan uygulanmadı";

        lblGelecek.Text = BakimPlani.Acik ? BakimPlani.Sonraki(DateTime.Now).ToString("dd.MM", CultureInfo.InvariantCulture) : "Kapalı";
        lblGelecekAlt.Text = BakimPlani.Acik
            ? BakimPlani.Sonraki(DateTime.Now).ToString("HH:mm", CultureInfo.InvariantCulture) + " · " + BakimPlani.GunAdlari[BakimPlani.Gun]
            : "hatırlatma kapalı";
        lblPlanSonraki.Text = BakimPlani.SonrakiMetin();
        DenetimleriYaz();
    }

    string MakineMetni()
    {
        string os = Yerel.WindowsAdi();
        return Environment.MachineName + "  ·  " + os + "  ·  " + (Environment.Is64BitOperatingSystem ? "64 bit" : "32 bit");
    }

    void AdKaydet()
    {
        string eski = Kisisel.Ad;
        Kisisel.Ad = txtAd.Text;
        string yeni = Kisisel.Ad;
        txtAd.Text = yeni;
        lblSelam.Text = "Hoş geldin, " + yeni;
        Durum(eski == yeni ? "Ad değişmedi." : "Tamam: bu bilgisayarda görünen ad \"" + yeni + "\".");
        Tazele();
        if (eski != yeni) Denetim.Ekle("Kişisel ad", "Görünen ad güncellendi.");
    }

    void Durum(string metin)
    {
        if (lblDurum != null) lblDurum.Text = metin;
    }

    Panel Kpi(string baslik, out Label deger, out Label alt)
    {
        var kart = new KartPaneli { Dock = DockStyle.Fill, Margin = new Padding(0) };
        var b = new Label
        {
            Dock = DockStyle.Top, Height = 16, Text = baslik, Font = Tasarim.KpiEtiket,
            ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft
        };
        deger = new Label
        {
            Dock = DockStyle.Top, Height = 34, Font = Tasarim.Kpi, ForeColor = Renk.Yazi,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Text = "—"
        };
        alt = new Label
        {
            Dock = DockStyle.Fill, Font = Tasarim.Kucuk, ForeColor = Renk.Soluk,
            TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true
        };
        kart.Controls.Add(alt);
        kart.Controls.Add(deger);
        kart.Controls.Add(b);
        return kart;
    }

    Panel PlanKarti()
    {
        var kart = new KartPaneli { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(20, 14, 20, 14) };
        var baslik = new Label
        {
            Dock = DockStyle.Top, Height = 24, Text = "Haftalık bakım planı", Font = Tasarim.KartBaslik,
            ForeColor = Renk.Yazi, TextAlign = ContentAlignment.MiddleLeft
        };

        var anahtarSatiri = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Renk.Panel };
        var anahtarEtiket = new Label
        {
            Dock = DockStyle.Fill, Text = "Bakım zamanı hatırlat", Font = Tasarim.Metin,
            ForeColor = Renk.Yazi, TextAlign = ContentAlignment.MiddleLeft
        };
        var anahtarKutu = new Panel { Dock = DockStyle.Right, Width = 56, BackColor = Renk.Panel };
        anPlan = new AnahtarDugmesi { Location = new Point(8, 5) };
        anahtarKutu.Controls.Add(anPlan);
        anahtarSatiri.Controls.Add(anahtarEtiket);
        anahtarSatiri.Controls.Add(anahtarKutu);

        var secimSatiri = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 38, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, BackColor = Renk.Panel, Margin = Padding.Empty
        };
        cmbGun = Kombinasyon(140);
        cmbSaat = Kombinasyon(92);
        foreach (var g in BakimPlani.GunAdlari) cmbGun.Items.Add(g);
        for (int i = 0; i < 24; i++) cmbSaat.Items.Add(i.ToString("00", CultureInfo.InvariantCulture) + ":00");
        ayarlaniyor = true;
        cmbGun.SelectedIndex = BakimPlani.Gun;
        cmbSaat.SelectedIndex = BakimPlani.Saat;
        anPlan.Checked = BakimPlani.Acik;
        ayarlaniyor = false;
        var gunEtiket = new Label { Text = "Gün", Font = Tasarim.Kucuk, ForeColor = Renk.Soluk, AutoSize = true, Margin = new Padding(0, 10, 6, 0) };
        var saatEtiket = new Label { Text = "Saat", Font = Tasarim.Kucuk, ForeColor = Renk.Soluk, AutoSize = true, Margin = new Padding(14, 10, 6, 0) };
        secimSatiri.Controls.Add(gunEtiket);
        secimSatiri.Controls.Add(cmbGun);
        secimSatiri.Controls.Add(saatEtiket);
        secimSatiri.Controls.Add(cmbSaat);

        anPlan.CheckedChanged += delegate
        {
            if (ayarlaniyor) return;
            BakimPlani.Acik = anPlan.Checked;
            Durum(anPlan.Checked ? "Tamam: " + BakimPlani.SonrakiMetin() : "Tamam: bakım hatırlatması kapatıldı.");
            Tazele();
        };
        cmbGun.SelectedIndexChanged += delegate
        {
            if (ayarlaniyor || cmbGun.SelectedIndex < 0) return;
            BakimPlani.Gun = cmbGun.SelectedIndex;
            Durum("Tamam: " + BakimPlani.SonrakiMetin());
            Tazele();
        };
        cmbSaat.SelectedIndexChanged += delegate
        {
            if (ayarlaniyor || cmbSaat.SelectedIndex < 0) return;
            BakimPlani.Saat = cmbSaat.SelectedIndex;
            Durum("Tamam: " + BakimPlani.SonrakiMetin());
            Tazele();
        };

        lblPlanSonraki = new Label
        {
            Dock = DockStyle.Top, Height = 22, Text = BakimPlani.SonrakiMetin(), Font = Tasarim.Kucuk,
            ForeColor = Renk.Turuncu, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };

        var dugmeler = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, BackColor = Renk.Panel, Margin = Padding.Empty
        };
        btnBakim = new EylemDugmesi("Bakım yap", EylemDugmesi.Tur.Birincil) { Ikon = "\uE74E", Width = 116, Height = 34, Margin = new Padding(0, 2, 8, 0) };
        btnNokta = new EylemDugmesi("Geri yükleme noktası", EylemDugmesi.Tur.Ikincil) { Ikon = "\uE81C", Width = 184, Height = 34, Margin = new Padding(0, 2, 8, 0) };
        btnRapor = new EylemDugmesi("Rapor", EylemDugmesi.Tur.Ikincil) { Ikon = "\uE7C3", Width = 104, Height = 34, Margin = new Padding(0, 2, 0, 0) };
        btnBakim.Click += delegate
        {
            BakimPlani.Tamamlandi();
            Denetim.Ekle("Bakım planı", "Kişisel bakım başlatıldı.");
            Tazele();
            Durum("Bakım başlatıldı: güvenli temizlik ve RAM boşaltma sırayla çalışır.");
            if (SimdiBakim != null) SimdiBakim();
        };
        btnNokta.Click += NoktaOlustur;
        btnRapor.Click += RaporOlustur;
        dugmeler.Controls.Add(btnBakim);
        dugmeler.Controls.Add(btnNokta);
        dugmeler.Controls.Add(btnRapor);

        kart.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "", BackColor = Renk.Panel });
        kart.Controls.Add(dugmeler);
        kart.Controls.Add(lblPlanSonraki);
        kart.Controls.Add(secimSatiri);
        kart.Controls.Add(anahtarSatiri);
        kart.Controls.Add(baslik);
        return kart;
    }

    Panel KimlikKarti()
    {
        var kart = new KartPaneli { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(18, 12, 18, 12) };
        var baslik = new Label
        {
            Dock = DockStyle.Top, Height = 22, Text = "Bu bilgisayar", Font = Tasarim.KartBaslik,
            ForeColor = Renk.Yazi, TextAlign = ContentAlignment.MiddleLeft
        };
        lblKimlik = new Label
        {
            Dock = DockStyle.Fill, Font = Tasarim.Mono, ForeColor = Renk.Soluk,
            Text = "Sistem bilgisi okunuyor...", TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true
        };
        kart.Controls.Add(lblKimlik);
        kart.Controls.Add(baslik);
        return kart;
    }

    Panel GunlukKarti()
    {
        var kart = new KartPaneli { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(18, 14, 18, 12) };
        var baslikSatiri = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Renk.Panel };
        var temizle = new EylemDugmesi("Temizle", EylemDugmesi.Tur.Sade) { Dock = DockStyle.Right, Width = 76, Height = 26 };
        temizle.Font = Tasarim.Kucuk;
        temizle.Click += delegate
        {
            if (MessageBox.Show(FindForm(), "Bakım günlüğü silinsin mi? Bu işlem yalnızca bu bilgisayardaki kayıtları temizler.",
                    "Bakım günlüğü", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Denetim.Temizle();
            Durum("Bakım günlüğü temizlendi.");
            Tazele();
        };
        var baslik = new Label
        {
            Dock = DockStyle.Fill, Text = "Bakım günlüğü", Font = Tasarim.KartBaslik,
            ForeColor = Renk.Yazi, TextAlign = ContentAlignment.MiddleLeft
        };
        baslikSatiri.Controls.Add(baslik);
        baslikSatiri.Controls.Add(temizle);
        var alt = new Label
        {
            Dock = DockStyle.Top, Height = 22, Text = "Son işlemler",
            Font = Tasarim.Kucuk, ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft
        };
        var cizgi = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Renk.Cizgi };
        kart.Controls.Add(denetimSatirlar);
        kart.Controls.Add(cizgi);
        kart.Controls.Add(alt);
        kart.Controls.Add(baslikSatiri);
        return kart;
    }

    ComboBox Kombinasyon(int genislik)
    {
        var c = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
            BackColor = Renk.PanelUzerinde, ForeColor = Renk.Yazi, Font = Tasarim.Metin,
            Width = genislik, Height = 28, Margin = new Padding(0, 5, 0, 0)
        };
        return c;
    }

    void DenetimleriYaz()
    {
        if (denetimSatirlar == null) return;
        denetimSatirlar.SuspendLayout();
        try
        {
            foreach (Control c in denetimSatirlar.Controls.Cast<Control>().ToList()) c.Dispose();
            denetimSatirlar.Controls.Clear();
            var kayitlar = Denetim.Son(8);
            if (kayitlar.Count == 0)
            {
                denetimSatirlar.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill, Text = "Henüz kayıt yok.",
                    Font = Tasarim.Metin, ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleCenter
                });
                return;
            }
            // En yeni kayıt en üstte kalacak şekilde eskiden yeniye ekle.
            for (int i = kayitlar.Count - 1; i >= 0; i--)
            {
                var k = kayitlar[i];
                var satir = new Panel
                {
                    Dock = DockStyle.Top, Height = 42, BackColor = Renk.PanelUzerinde,
                    Padding = new Padding(10, 6, 10, 4), Margin = Padding.Empty
                };
                var zaman = new Label
                {
                    Dock = DockStyle.Left, Width = 92, Text = k.Zaman.ToString("dd.MM HH:mm", CultureInfo.InvariantCulture),
                    Font = Tasarim.Mono, ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft
                };
                var islem = new Label
                {
                    Dock = DockStyle.Left, Width = 108, Text = k.Islem, Font = Tasarim.Kucuk,
                    ForeColor = Renk.Yazi, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
                };
                var ayrinti = new Label
                {
                    Dock = DockStyle.Fill, Text = k.Ayrinti, Font = Tasarim.Kucuk,
                    ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
                };
                satir.Controls.Add(ayrinti);
                satir.Controls.Add(islem);
                satir.Controls.Add(zaman);
                denetimSatirlar.Controls.Add(satir);
                denetimSatirlar.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Renk.Panel });
            }
        }
        finally { denetimSatirlar.ResumeLayout(true); }
    }

    async void KimlikOku()
    {
        kimlikYukleniyor = true;
        lblKimlik.Text = "Sistem bilgisi okunuyor...";
        List<KeyValuePair<string, string>> kimlik;
        try { kimlik = await Task.Run(() => SistemKimligi.Topla()); }
        catch { kimlik = new List<KeyValuePair<string, string>>(); }
        kimlikYukleniyor = false;
        if (IsDisposed) return;
        kimlikYuklendi = true;
        if (kimlik.Count == 0) { lblKimlik.Text = "Sistem bilgisi okunamadı."; return; }
        var sb = new StringBuilder();
        foreach (var k in kimlik.Take(5)) sb.Append(k.Key).Append(": ").Append(k.Value).Append("\n");
        lblKimlik.Text = sb.ToString().TrimEnd();
    }

    async void NoktaOlustur(object sender, EventArgs e)
    {
        btnNokta.Enabled = false;
        Durum("Geri yükleme noktası isteniyor; Windows izin isteyebilir...");
        try
        {
            await Task.Run(() => WindowsAyarlari.GeriYuklemeNoktasi("Yaman PC Cleaner kişisel bakım"));
            Denetim.Ekle("Geri yükleme noktası", "Sistem geri yükleme noktası istendi.");
            Durum("İstek gönderildi. Windows Koruma açıksa nokta Sistem Koruması'nda görünür.");
        }
        catch (Exception ex) { Durum("Geri yükleme noktası oluşturulamadı: " + ex.Message); }
        finally { if (!IsDisposed) btnNokta.Enabled = true; }
    }

    async void RaporOlustur(object sender, EventArgs e)
    {
        btnRapor.Enabled = false;
        Durum("Bakım raporu hazırlanıyor...");
        List<KeyValuePair<string, string>> kimlik;
        try { kimlik = await Task.Run(() => SistemKimligi.Topla()); }
        catch { kimlik = new List<KeyValuePair<string, string>>(); }
        if (IsDisposed) return;
        try
        {
            string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrEmpty(masaustu)) masaustu = Ayarlar.Klasor;
            string yol = Path.Combine(masaustu, "YamanPcCleaner-Rapor-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ".html");
            File.WriteAllText(yol, RaporHtml(kimlik), new UTF8Encoding(true));
            Denetim.Ekle("Rapor", Path.GetFileName(yol));
            Durum("Rapor oluşturuldu: " + yol);
            try { Process.Start(yol); } catch { }
            Tazele();
        }
        catch (Exception ex) { Durum("Rapor oluşturulamadı: " + ex.Message); }
        finally { if (!IsDisposed) btnRapor.Enabled = true; }
    }

    string RaporHtml(List<KeyValuePair<string, string>> kimlik)
    {
        var sb = new StringBuilder();
        string tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
        sb.Append("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>Yaman PC Cleaner · Kişisel Bakım Raporu</title><style>");
        sb.Append("body{margin:0;background:#0a101c;color:#eaf0f8;font:14px/1.55 'Segoe UI',Arial,sans-serif}");
        sb.Append(".sayfa{max-width:980px;margin:0 auto;padding:38px 26px 48px}");
        sb.Append(".ust{display:flex;align-items:center;gap:16px;border-bottom:1px solid #22314a;padding-bottom:20px;margin-bottom:24px}");
        sb.Append(".logo{width:64px;height:64px;border-radius:16px;background:linear-gradient(135deg,#2563eb,#2dd4bf);display:flex;align-items:center;justify-content:center;font-size:34px;font-weight:700;color:#fff}");
        sb.Append("h1{font-size:23px;margin:0}.alt{color:#93a6c2;font-size:12px;margin-top:3px}");
        sb.Append(".kutular{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:18px 0 26px}");
        sb.Append(".kutu{background:#111b2e;border:1px solid #22314a;border-radius:12px;padding:14px 16px}");
        sb.Append(".kutu b{display:block;font-size:11px;color:#93a6c2;letter-spacing:.06em;margin-bottom:6px}");
        sb.Append(".kutu span{font-size:21px;font-weight:600}");
        sb.Append("h2{font-size:13px;letter-spacing:.08em;color:#93a6c2;margin:24px 0 10px}");
        sb.Append("table{width:100%;border-collapse:collapse;background:#111b2e;border:1px solid #22314a;border-radius:12px;overflow:hidden}");
        sb.Append("td,th{padding:9px 12px;border-bottom:1px solid #1c2940;text-align:left;font-size:13px;vertical-align:top}");
        sb.Append("th{color:#93a6c2;font-size:11px;letter-spacing:.05em;background:#0d1728}");
        sb.Append("tr:last-child td{border-bottom:0}td:first-child{color:#93a6c2;width:150px}");
        sb.Append(".iyi{color:#34d399}.uyari{color:#f5a524}.not{color:#93a6c2}");
        sb.Append(".dip{color:#93a6c2;font-size:11px;border-top:1px solid #22314a;margin-top:28px;padding-top:16px}");
        sb.Append("@media(max-width:720px){.kutular{grid-template-columns:repeat(2,1fr)}}");
        sb.Append("</style></head><body><div class=\"sayfa\">");

        sb.Append("<div class=\"ust\"><div class=\"logo\">Y</div><div><h1>Yaman PC Cleaner · Kişisel Bakım Raporu</h1>");
        sb.Append("<div class=\"alt\">").Append(Html(Kisisel.Ad)).Append(" · ").Append(Html(Environment.MachineName)).Append(" · ").Append(Html(tarih)).Append("</div></div></div>");

        long toplam = Kazanc.DiskBayt + Kazanc.RamBayt;
        sb.Append("<div class=\"kutular\">");
        sb.Append("<div class=\"kutu\"><b>SİSTEM PUANI</b><span>").Append(DurumPanosu.SonPuan >= 0 ? DurumPanosu.SonPuan + " / 100" : "—").Append("</span></div>");
        sb.Append("<div class=\"kutu\"><b>TOPLAM KAZANÇ</b><span>").Append(Kazanc.IsSayisi > 0 ? Html(AnaForm.Boyut(toplam)) : "—").Append("</span></div>");
        sb.Append("<div class=\"kutu\"><b>BAKIM SAYISI</b><span>").Append(Kazanc.IsSayisi.ToString("N0", CultureInfo.CurrentCulture)).Append("</span></div>");
        var son = BakimPlani.Son;
        sb.Append("<div class=\"kutu\"><b>SON PLANLI BAKIM</b><span>").Append(son.HasValue ? son.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) : "—").Append("</span></div>");
        sb.Append("</div>");

        sb.Append("<h2>SİSTEM ÖZETİ</h2><table>");
        foreach (var k in kimlik) sb.Append("<tr><td>").Append(Html(k.Key)).Append("</td><td>").Append(Html(k.Value)).Append("</td></tr>");
        if (kimlik.Count == 0) sb.Append("<tr><td colspan=\"2\" class=\"not\">Sistem bilgisi okunamadı.</td></tr>");
        sb.Append("</table>");

        sb.Append("<h2>BAKIM PLANI</h2><table>");
        sb.Append("<tr><td>Durum</td><td>").Append(BakimPlani.Acik ? "<span class=\"iyi\">Açık</span>" : "<span class=\"not\">Kapalı</span>").Append("</td></tr>");
        sb.Append("<tr><td>Gün / saat</td><td>").Append(Html(BakimPlani.GunAdlari[BakimPlani.Gun])).Append(" · ").Append(BakimPlani.Saat.ToString("00", CultureInfo.InvariantCulture)).Append(":00</td></tr>");
        sb.Append("<tr><td>Sonraki bakım</td><td>").Append(BakimPlani.Acik ? Html(BakimPlani.Sonraki(DateTime.Now).ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)) : "—").Append("</td></tr>");
        sb.Append("</table>");

        sb.Append("<h2>SON İŞLEMLER</h2><table><tr><th>Zaman</th><th>İşlem</th><th>Ayrıntı</th></tr>");
        var kayitlar = Denetim.Son(20);
        if (kayitlar.Count == 0) sb.Append("<tr><td colspan=\"3\" class=\"not\">Henüz kayıt yok.</td></tr>");
        foreach (var k in kayitlar)
            sb.Append("<tr><td>").Append(k.Zaman.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)).Append("</td><td>").Append(Html(k.Islem)).Append("</td><td>").Append(Html(k.Ayrinti)).Append("</td></tr>");
        sb.Append("</table>");

        sb.Append("<div class=\"dip\">Bu rapor Yaman PC Cleaner tarafından bu bilgisayarda oluşturuldu. Veriler buluta gönderilmez; dosyayı istediğin zaman silebilirsin.</div>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    static string Html(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
