// "Hakkında" penceresi: sürüm, özellik özeti, açık kaynak bağlantısı ve güncelleme ayarları.
// Açılış: sol alttaki "Hakkında ve güncelleme" ve tepsi menüsü. Aynı anda tek pencere.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

class Hakkinda : Form
{
    public const string Depo = "github.com/" + GuncellemeMotoru.Depo;

    // Özellik özeti (kullanıcı diliyle). README.md'deki "Neler yapabilir" bölümüyle uyumlu tut.
    public const string Ozet =
        "• Genel Bakış: açılınca bilgisayarının 100 üzerinden puanı; sanal bellek, boş yer, disk ve pil sağlığı, açılış yükü, oyun ayarları, sürücü ve çökmeler için tek tıkla düzeltilecek öneriler. Sistem kimliğini kopyalar, bugüne kadarki toplam kazancı gösterir.\r\n" +
        "• Sistemi Optimize Et: arka plan programlarını kapatır, RAM'i boşaltır, geçici dosyaları siler; ne kazandığını sayarak gösterir.\r\n" +
        "• Neden yavaş?: o an bilgisayarı neyin yavaşlattığını (işlemci, bellek, disk, ısı, güç) sade bir dille söyler.\r\n" +
        "• Sistem: işlemci, ekran kartı, RAM, ekran kartı belleği, sanal bellek ve ısıyı son 2 dakikanın grafiğiyle canlı gösterir; RAM'i boşaltır, gereksiz programları kapatmana yardım eder.\r\n" +
        "• Disk: gereksiz dosyaları (derin temizlik dahil) temizler; hangi klasör ne kadar yer tutuyor gösterir.\r\n" +
        "• Sürücü: NVIDIA ekran kartı sürücüsünün yeni sürümünü denetler, indirip kurar.\r\n" +
        "• Sağlık: bellek bekçisinin durumu, sızıntı şüphelileri, çökme geçmişi, destek raporu.\r\n" +
        "• Başlangıç: Windows ile açılan programları ve açılışta çalışan zamanlanmış görevleri listeler, açıp kapatmanı sağlar.\r\n" +
        "• Windows: geri alınabilir Windows ayarları, hazır uygulamaları kaldırma, DNS ve ağ testi, güç modu.\r\n" +
        "• Turbo: oyun ayarları, oyun profili, donanım kontrolü.\r\n" +
        "• Kişisel Bakım: Yaman'a özel karşılama, haftalık bakım planı ve hatırlatma, yerel işlem günlüğü, geri yükleme noktası ve masaüstüne kaydedilen HTML bakım raporu.\r\n" +
        "• Ayarlar: vurgu rengi, tepsi simgesi, hareketli geçişler, açılış sekmesi, arka planda çalışma, genel kısayol (Ctrl+Alt+O), bildirim sesi, sistem optimizasyonu varsayılanları, güncelleme; hepsi tek sayfada.\r\n" +
        "• Sanal bellek ayarı: önerilen boyutu uygular, geri alınabilir.\r\n" +
        "• Otomatik / Full optimize: tek tıkla temizlik ve hızlandırma.\r\n" +
        "• Tepside bekçi: pencere kapalıyken de bellek dolarsa haber verir; simgede RAM yüzdesi yazar, üstüne gelince özet açılır.\r\n" +
        "• Otomatik güncelleme: günde bir kez bakar, yeni sürümü imzasını doğrulayıp sorarak kurar. Elle denetlemek için: aşağıdaki \"Şimdi denetle\" ya da saatin yanındaki simge → \"Güncellemeleri denetle\".";

    static Hakkinda acik;

    readonly AnahtarDugmesi chkDenetle = new AnahtarDugmesi();
    readonly Label lblSon = new Label(), lblSonuc = new Label();
    readonly EylemDugmesi btnDenetle = new EylemDugmesi("Şimdi denetle", EylemDugmesi.Tur.Birincil);
    bool denetliyor;

    public static void Goster()
    {
        if (acik != null && !acik.IsDisposed)
        {
            if (acik.WindowState == FormWindowState.Minimized) acik.WindowState = FormWindowState.Normal;
            acik.Show();
            acik.Activate();
            return;
        }
        acik = new Hakkinda();
        acik.Show();
        acik.Activate();
    }

    Hakkinda()
    {
        Text = "Hakkında · " + Marka.Ad;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Renk.Arka;
        ForeColor = Renk.Yazi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 676);
        Icon = Logo.Simge();

        // ---- Üst marka alanı ----
        var ust = new Panel { Bounds = new Rectangle(0, 0, 620, 148), BackColor = Renk.Panel };
        ust.Paint += delegate(object s, PaintEventArgs e)
        {
            using (var b = new LinearGradientBrush(ust.ClientRectangle, Renk.Panel, Color.FromArgb(14, 23, 39), 90f))
                e.Graphics.FillRectangle(b, ust.ClientRectangle);
            using (var b = new LinearGradientBrush(new Rectangle(0, 146, 620, 2), Renk.Vurgu, Renk.Cizgi, 0f))
                e.Graphics.FillRectangle(b, 0, 146, 620, 2);
        };

        var logo = new PictureBox
        {
            Bounds = new Rectangle(24, 26, 88, 88), SizeMode = PictureBoxSizeMode.Zoom,
            Image = Logo.Resim(), BackColor = Color.Transparent
        };
        var ad = new Label
        {
            Text = Marka.Ad, Bounds = new Rectangle(128, 26, 460, 34), AutoEllipsis = true,
            Font = new Font("Segoe UI Semibold", 19f, FontStyle.Bold), ForeColor = Renk.Yazi
        };
        var slogan = new Label
        {
            Text = Marka.Slogan, Bounds = new Rectangle(130, 60, 460, 24), AutoEllipsis = true,
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), ForeColor = Renk.Vurgu
        };
        var surum = new Kapsul("SÜRÜM " + Surum.Metin)
        {
            Bounds = new Rectangle(130, 90, 108, 24), Dolgu = Renk.Arka, Cizgi = Renk.Cizgi,
            YaziRengi = Renk.Yesil
        };
        var platform = new Label
        {
            Text = "Windows 10 / 11  ·  Türkçe  ·  Kurulum gerektirmez",
            Bounds = new Rectangle(250, 90, 350, 24), AutoEllipsis = true,
            Font = Tasarim.Kucuk, ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft
        };
        var lisans = new Label
        {
            Text = "Açık kaynak · MIT lisansı", Bounds = new Rectangle(130, 116, 460, 20),
            Font = Tasarim.Kucuk, ForeColor = Renk.Soluk
        };
        ust.Controls.AddRange(new Control[] { logo, ad, slogan, surum, platform, lisans });

        // ---- Özellik özeti ----
        var baslik = new Label
        {
            Text = "NELER YAPAR?", Bounds = new Rectangle(24, 166, 300, 18),
            Font = Tasarim.KpiEtiket, ForeColor = Renk.Soluk
        };
        var ozetKart = new KartPaneli
        {
            Bounds = new Rectangle(24, 188, 572, 238), Dolgu = Renk.Panel, Cerceve = true
        };
        var ozet = new TextBox
        {
            Bounds = new Rectangle(14, 12, 544, 212), Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, WordWrap = true, BorderStyle = BorderStyle.None,
            BackColor = Renk.Panel, ForeColor = Renk.Yazi, Font = new Font("Segoe UI", 8.75f),
            Text = Ozet, TabStop = false
        };
        ozet.Select(0, 0);
        ozetKart.Controls.Add(ozet);

        // ---- Bağlantılar ----
        var baglantiEtiket = new Label
        {
            Text = "Açık kaynak:", Bounds = new Rectangle(24, 438, 90, 20),
            Font = Tasarim.Kucuk, ForeColor = Renk.Soluk
        };
        var baglanti = Baglanti(Depo, 110, 438, 310, "https://" + Depo);
        var site = Baglanti("Tanıtım sayfası", 430, 438, 166, Marka.Site);
        site.TextAlign = ContentAlignment.TopRight;
        var tanitim = Baglanti("Tanıtımı yeniden göster", 430, 458, 166, null);
        tanitim.TextAlign = ContentAlignment.TopRight;
        tanitim.LinkClicked += delegate { Tanitim.TuruGoster(this, null); };
        var lisans2 = new Label
        {
            Text = Marka.Telif, Bounds = new Rectangle(24, 458, 300, 20),
            Font = Tasarim.Kucuk, ForeColor = Renk.Soluk
        };

        // ---- Güncelleme ----
        var guncelKart = new KartPaneli
        {
            Bounds = new Rectangle(24, 492, 572, 142), Dolgu = Renk.Panel, Cerceve = true
        };
        var guncelBaslik = new Label
        {
            Text = "Güncelleme", Bounds = new Rectangle(16, 14, 300, 20),
            Font = Tasarim.KartBaslik, ForeColor = Renk.Yazi
        };
        var chkEtiket = new Label
        {
            Text = "Güncellemeleri kendiliğinden denetle", Bounds = new Rectangle(16, 44, 380, 24),
            Font = Tasarim.Metin, ForeColor = Renk.Yazi, TextAlign = ContentAlignment.MiddleLeft
        };
        chkDenetle.SetBounds(470, 44, 46, 24);
        chkDenetle.Checked = Guncelleyici.Acik;
        chkDenetle.CheckedChanged += delegate { Ayarlar.Yaz(Guncelleyici.AyarDenetle, chkDenetle.Checked); };
        lblSon.SetBounds(16, 74, 500, 20);
        lblSon.ForeColor = Renk.Soluk;
        lblSon.Font = Tasarim.Kucuk;
        SonYaz();

        btnDenetle.SetBounds(16, 98, 146, 32);
        btnDenetle.Click += delegate { Denetle(); };
        lblSonuc.SetBounds(174, 98, 250, 32);
        lblSonuc.TextAlign = ContentAlignment.MiddleLeft;
        lblSonuc.AutoEllipsis = true;
        lblSonuc.Font = Tasarim.Kucuk;
        var btnKapat = new EylemDugmesi("Kapat", EylemDugmesi.Tur.Ikincil) { Bounds = new Rectangle(458, 98, 98, 32) };
        btnKapat.Click += delegate { Close(); };
        CancelButton = btnKapat;
        guncelKart.Controls.AddRange(new Control[] { guncelBaslik, chkEtiket, chkDenetle, lblSon, btnDenetle, lblSonuc, btnKapat });

        Controls.AddRange(new Control[] { ust, baslik, ozetKart, baglantiEtiket, baglanti, site, lisans2, tanitim, guncelKart });
        FormClosed += delegate { if (acik == this) acik = null; };
    }

    static LinkLabel Baglanti(string metin, int sol, int ust, int genislik, string adres)
    {
        var l = new LinkLabel
        {
            Text = metin, Left = sol, Top = ust, Width = genislik, Height = 20, AutoSize = false,
            LinkColor = Color.FromArgb(110, 176, 255), ActiveLinkColor = Color.White,
            VisitedLinkColor = Color.FromArgb(110, 176, 255), LinkBehavior = LinkBehavior.HoverUnderline,
            Font = Tasarim.Kucuk
        };
        if (adres != null) l.LinkClicked += delegate { Ac(adres); };
        return l;
    }

    static void Ac(string adres)
    {
        try { Process.Start(adres); } catch { }
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

    void SonYaz()
    {
        var son = Guncelleyici.SonDenetim;
        lblSon.Text = "Son denetim: " + (son == null ? "henüz yapılmadı" : son.Value.ToString("d MMMM yyyy HH:mm"));
    }

    async void Denetle()
    {
        var g = Guncelleyici.Ornek;
        if (denetliyor || g == null) return;
        denetliyor = true;
        btnDenetle.Enabled = false;
        lblSonuc.ForeColor = Renk.Soluk;
        lblSonuc.Text = "Denetleniyor...";
        DenetimYaniti y;
        try { y = await g.SimdiDenetle(); }
        catch (Exception e) { y = new DenetimYaniti { Sonuc = DenetimSonucu.Hata, Hata = GuncellemeMotoru.KisaNeden(e) }; }
        denetliyor = false;
        if (IsDisposed) return;
        btnDenetle.Enabled = true;
        SonYaz();
        switch (y.Sonuc)
        {
            case DenetimSonucu.Guncel:
                lblSonuc.ForeColor = Renk.Yesil;
                lblSonuc.Text = "Güncelsin (v" + Surum.Metin + ")";
                break;
            case DenetimSonucu.Yeni:
                lblSonuc.ForeColor = Renk.Turuncu;
                lblSonuc.Text = "Yeni sürüm var (v" + Guncelleyici.SurumMetni(y.Bilgi.Surum) + ")";
                g.FormuAc(y.Bilgi, this);
                break;
            case DenetimSonucu.Eksik:
                lblSonuc.ForeColor = Renk.Turuncu;
                lblSonuc.Text = "Yeni sürüm var ama dosyaları eksik; sonra dene.";
                break;
            case DenetimSonucu.YayinYok:
                lblSonuc.ForeColor = Renk.Soluk;
                lblSonuc.Text = "Henüz GitHub yayını yok.";
                break;
            default:
                lblSonuc.ForeColor = Renk.Turuncu;
                lblSonuc.Text = "Denetlenemedi: " + y.Hata;
                break;
        }
    }
}
