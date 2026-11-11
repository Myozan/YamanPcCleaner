// Yaman PC Cleaner · kurumsal kabuk.
// Solda marka ve gezinme, üstte sayfa başlığı ile hızlı işlemler, ortada etkin sekme.
// Sekmeler görünürlükle değişir; her sekme kendi Gosterildi işini korur.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

partial class AnaForm
{
    int acikSekme;
    const int SekmeSayisi = 10;

    static readonly string[] SayfaBasliklari =
    {
        "Genel Bakış", "Sistem İzleme", "Disk Temizliği", "Sürücü Merkezi", "Sağlık ve Güvenlik",
        "Başlangıç Yönetimi", "Windows Bakımı", "Turbo Modu", "Kişisel Bakım", "Ayarlar"
    };

    static readonly string[] SayfaAciklamalari =
    {
        "Sistem sağlığını tek bakışta görün; önerilen işlemleri sırayla uygulayın.",
        "İşlemci, bellek, ekran kartı ve sıcaklık değerlerini canlı izleyin.",
        "Gereksiz dosyaları temizleyin ve depolama alanını analiz edin.",
        "Ekran kartı sürücüsünü ve güncellemeleri tek yerden denetleyin.",
        "Disk sağlığı, pil, güvenlik ve sistem bütünlüğü durumunu izleyin.",
        "Windows açılışında çalışan programları yönetin.",
        "Windows ayarlarını ve sistem kalıntılarını güvenle temizleyin.",
        "Oyun ve ağır işler için performans profilini etkinleştirin.",
        "Size özel bakım planı, işlem günlüğü ve dışa aktarılabilir rapor.",
        "Görünüm, bildirim, kısayol ve program tercihlerini yönetin."
    };

    readonly List<GezinmeDugmesi> gezinmeler = new List<GezinmeDugmesi>();
    readonly List<Control> sayfalar = new List<Control>();
    Panel icerikAlani;
    Label lblSayfaBaslik, lblSayfaAlt;
    GezinmeDugmesi navSurucu;
    bool navSik;

    void ArayuzKur(Control sistem)
    {
        // ---- Sol gezinme ----
        var yan = new YanPanel { Dock = DockStyle.Left, Width = Tasarim.YanGenislik, BackColor = Renk.Yan };

        var marka = new Panel { Dock = DockStyle.Top, Height = 108, BackColor = Renk.Yan };
        marka.Controls.Add(new PictureBox
        {
            Image = Logo.Resim(), SizeMode = PictureBoxSizeMode.Zoom,
            Bounds = new Rectangle(20, 27, 44, 44), BackColor = Color.Transparent
        });
        marka.Controls.Add(new Label
        {
            Text = Marka.KisaAd.ToUpperInvariant(), Font = Tasarim.Marka, ForeColor = Renk.Yazi,
            Bounds = new Rectangle(74, 28, 160, 30), TextAlign = ContentAlignment.MiddleLeft
        });
        marka.Controls.Add(new Label
        {
            Text = Marka.UrunAlt, Font = Tasarim.MarkaAlt, ForeColor = Renk.Vurgu,
            Bounds = new Rectangle(76, 55, 160, 18), TextAlign = ContentAlignment.MiddleLeft
        });
        marka.Controls.Add(new Label
        {
            Text = Marka.SloganBuyuk, Font = Tasarim.KpiEtiket, ForeColor = Renk.Soluk,
            Bounds = new Rectangle(22, 82, 214, 16), TextAlign = ContentAlignment.MiddleLeft
        });

        var gezinme = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            BackColor = Renk.Yan, Padding = new Padding(0, 4, 0, 8), AutoScroll = true
        };
        Action<string> bolum = delegate(string ad)
        {
            gezinme.Controls.Add(new Label
            {
                Text = ad, Font = Tasarim.KpiEtiket, ForeColor = Renk.Soluk, Width = 190, Height = 24,
                Margin = new Padding(16, 8, 0, 2), TextAlign = ContentAlignment.BottomLeft
            });
        };
        Action<string, string> ekle = delegate(string metin, string ikon)
        {
            int n = gezinmeler.Count;
            var d = new GezinmeDugmesi(metin, ikon)
            {
                Width = Tasarim.YanGenislik - 44, Height = 38, Margin = new Padding(9, 1, 9, 1)
            };
            d.Click += delegate { SayfaSec(n); };
            gezinmeler.Add(d);
            gezinme.Controls.Add(d);
        };

        bolum("SİSTEM BAKIMI");
        ekle("Genel Bakış", "\uE9D9");
        ekle("Sistem", "\uE770");
        ekle("Disk", "\uEDA2");
        ekle("Sürücü", "\uE772");
        ekle("Sağlık", "\uEB51");
        navSurucu = gezinmeler[3];

        bolum("YÖNETİM ARAÇLARI");
        ekle("Başlangıç", "\uE945");
        ekle("Windows", "\uE782");
        ekle("Turbo", "\uE9D2");

        bolum("KİŞİSEL");
        ekle("Kişisel Bakım", "\uE8FB");

        bolum("PROGRAM");
        ekle("Ayarlar", "\uE713");
        gezinme.SizeChanged += delegate { NavSikistir(gezinme); };

        var dip = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = Renk.Yan, Padding = new Padding(18, 0, 18, 10) };
        dip.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Renk.Cizgi });
        var hakkinda = new EylemDugmesi("Hakkında ve güncelleme", EylemDugmesi.Tur.Sade)
        {
            Dock = DockStyle.Bottom, Height = 30, Ikon = "\uE946"
        };
        hakkinda.Click += delegate { Hakkinda.Goster(); };
        var surum = new Label
        {
            Dock = DockStyle.Bottom, Height = 20, Text = "v" + Surum.Metin + "   ·   Kararlı sürüm",
            Font = Tasarim.Kucuk, ForeColor = Renk.Soluk, TextAlign = ContentAlignment.MiddleLeft
        };
        dip.Controls.Add(hakkinda);
        dip.Controls.Add(surum);

        yan.Controls.Add(gezinme);
        yan.Controls.Add(dip);
        yan.Controls.Add(marka);

        // ---- Üst başlık ----
        var baslikPaneli = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Renk.Arka, Padding = new Padding(24, 14, 24, 10) };
        lblSayfaBaslik = new Label
        {
            Dock = DockStyle.Top, Height = 30, Font = Tasarim.Sayfa, ForeColor = Renk.Yazi,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Text = SayfaBasliklari[0]
        };
        lblSayfaAlt = new Label
        {
            Dock = DockStyle.Top, Height = 20, Font = Tasarim.Alt, ForeColor = Renk.Soluk,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Text = SayfaAciklamalari[0]
        };

        var eylemler = new Panel { Dock = DockStyle.Right, Width = 372, BackColor = Renk.Arka };
        btnOto = new EylemDugmesi("Otomatik optimize", EylemDugmesi.Tur.Birincil)
        {
            Ikon = "\uE945", Width = 174, Height = 34, Margin = new Padding(8, 0, 0, 0)
        };
        btnFull = new EylemDugmesi("Full optimize", EylemDugmesi.Tur.Ikincil)
        {
            Ikon = "\uE9D2", Width = 156, Height = 34, Margin = new Padding(8, 0, 0, 0)
        };
        btnOto.Click += OtomatikOptimize_Click;
        btnFull.Click += FullOptimize_Click;
        new ToolTip().SetToolTip(btnOto, "Güvenli, yönetici izni istemeyen temizlik: geçici dosyalar ve RAM.");
        new ToolTip().SetToolTip(btnFull, "Kapsamlı bakım: disk, RAM, Windows kalıntıları ve oyun profili.");

        var dugmeSatiri = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 38, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = Renk.Arka
        };
        dugmeSatiri.Controls.Add(btnFull);
        dugmeSatiri.Controls.Add(btnOto);

        lblIs.Dock = DockStyle.Fill;
        lblIs.TextAlign = ContentAlignment.MiddleRight;
        lblIs.ForeColor = Renk.Turuncu;
        lblIs.Font = Tasarim.Kucuk;
        lblIs.AutoEllipsis = true;
        lblIs.Padding = new Padding(0, 4, 2, 0);

        eylemler.Controls.Add(lblIs);
        eylemler.Controls.Add(dugmeSatiri);

        baslikPaneli.Controls.Add(lblSayfaAlt);
        baslikPaneli.Controls.Add(lblSayfaBaslik);
        baslikPaneli.Controls.Add(eylemler);
        baslikPaneli.Controls.Add(new VurguCizgisi { Dock = DockStyle.Bottom });

        // ---- İçerik ----
        icerikAlani = new Panel { Dock = DockStyle.Fill, BackColor = Renk.Arka, Padding = new Padding(22, 14, 22, 18) };
        sayfalar.Clear();
        sayfalar.Add(durumPanosu);
        sayfalar.Add(sistem);
        sayfalar.Add(diskSekmesi);
        sayfalar.Add(surucuSekmesi);
        sayfalar.Add(saglikSekmesi);
        sayfalar.Add(baslangicSekmesi);
        sayfalar.Add(windowsSekmesi);
        sayfalar.Add(turboSekmesi);
        sayfalar.Add(kisiselBakim);
        sayfalar.Add(ayarSekmesi);
        foreach (var s in sayfalar)
        {
            s.Dock = DockStyle.Fill;
            s.Visible = false;
            icerikAlani.Controls.Add(s);
        }

        // Bağlantılar: durum panosu / ayarlar / sağlık tek merkezden sekmeye gider.
        sekmeSec = SayfaSec;
        surucuSekmesi.GuncellemeDurumu += delegate(bool yeni)
        {
            if (navSurucu != null) { navSurucu.Uyari = yeni; navSurucu.Invalidate(); }
            durumPanosu.SurucuDegisti();
        };
        durumPanosu.Git = HedefeGit;
        durumPanosu.SanalBellekAc = SanalBellekAyarla;
        durumPanosu.OyunaHazirla = OyunaHazirlaAc;
        durumPanosu.NedenYavas = NedenYavasAc;
        durumPanosu.SurucuBilgisi = delegate { return surucuSekmesi.SonBilgi; };
        durumPanosu.SurucuDenetlendi = delegate { return surucuSekmesi.Denetlendi; };
        ayarSekmesi.Git = HedefeGit;
        saglikSekmesi.AyarlaraGit = delegate { SayfaSec(9); };
        kisiselBakim.SimdiBakim = delegate { OtomatikOptimize_Click(this, EventArgs.Empty); };
        bekci.OyunaHazirla = delegate { bekci.Goster(); SayfaSec(0); OyunaHazirlaAc(); };

        // Yerleşim: içerik önce eklenir (Dock=Fill), sonra başlık, en son sol çubuk.
        Controls.Add(icerikAlani);
        Controls.Add(baslikPaneli);
        Controls.Add(yan);
        SayfaSec(0);
    }

    // Dar pencerede (ör. 700 px yükseklik) sol gezinmede kaydırma çubuğu çıkmasın diye
    // öğeleri bir miktar sıkıştırır; geniş pencerede normal aralıklara döner.
    void NavSikistir(FlowLayoutPanel g)
    {
        int bolum = 0, oge = 0;
        foreach (Control c in g.Controls)
        {
            if (c is GezinmeDugmesi) oge++;
            else if (c is Label) bolum++;
        }
        bool sik = g.ClientSize.Height < 12 + bolum * 34 + oge * 40;
        if (sik == navSik) return;
        navSik = sik;
        g.SuspendLayout();
        try
        {
            foreach (Control c in g.Controls)
            {
                if (c is GezinmeDugmesi)
                {
                    c.Height = sik ? 36 : 38;
                    c.Margin = sik ? new Padding(9, 0, 9, 0) : new Padding(9, 1, 9, 1);
                }
                else if (c is Label)
                {
                    c.Height = sik ? 18 : 24;
                    c.Margin = sik ? new Padding(16, 4, 0, 0) : new Padding(16, 8, 0, 2);
                }
            }
            // Sıkıştırılmış hâl sığıyorsa kaydırma çubuğu hiç görünmesin; yalnızca çok kısa pencerede açılsın.
            g.AutoScroll = g.ClientSize.Height < 12 + bolum * 22 + oge * 36;
            if (!g.AutoScroll) g.AutoScrollPosition = new Point(0, 0);
        }
        finally { g.ResumeLayout(true); }
    }

    void SayfaSec(int n)
    {
        if (n < 0 || n >= sayfalar.Count) return;
        acikSekme = n;
        for (int i = 0; i < sayfalar.Count; i++) sayfalar[i].Visible = i == n;
        for (int i = 0; i < gezinmeler.Count; i++) gezinmeler[i].Secili = i == n;
        if (lblSayfaBaslik != null) lblSayfaBaslik.Text = SayfaBasliklari[n];
        if (lblSayfaAlt != null) lblSayfaAlt.Text = SayfaAciklamalari[n];
        if (n == 0) durumPanosu.Gosterildi();
        if (n == 2) diskSekmesi.Gosterildi();
        if (n == 3) surucuSekmesi.Gosterildi();
        if (n == 4) saglikSekmesi.Gosterildi();
        if (n == 5) baslangicSekmesi.Gosterildi();
        if (n == 6) windowsSekmesi.Gosterildi();
        if (n == 7) turboSekmesi.Gosterildi();
        if (n == 8) kisiselBakim.Gosterildi();
        if (n == 9) ayarSekmesi.Gosterildi();
        if (yuklendi) AcilisSekmesi.Hatirla(n);
        Yerel.KoyuKaydirma(sayfalar[n]);
    }
}
