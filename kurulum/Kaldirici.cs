// Yaman PC Cleaner kaldırma penceresi. Programı, kısayolları ve kayıt girdilerini temizler;
// kişisel ayarlar isteğe bağlı olarak silinir. Kaldirici.exe /S sessiz kaldırma yapar.
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

class KaldiriciFormu : Form
{
    readonly OnayKutusu chkAyarlar = new OnayKutusu("Kişisel ayarları ve işlem günlüğünü de sil");
    readonly KurulumDugmesi btnKaldir = new KurulumDugmesi("Kaldır", DugmeTuru.Tehlike);
    readonly KurulumDugmesi btnVazgec = new KurulumDugmesi("Vazgeç", DugmeTuru.Ikincil);
    readonly Label lblDurum = new Label();
    readonly string kuruluSurum;
    bool kaldiriliyor, bitti;

    public KaldiriciFormu()
    {
        Text = Marka.Ad + " Kaldırma";
        ClientSize = new Size(600, 450);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = KurulumTema.Zemin;
        ForeColor = KurulumTema.Yazi;
        Font = KurulumTema.Govde;
        AutoScaleMode = AutoScaleMode.None;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        kuruluSurum = KurulumBilgi.KuruluSurum() ?? Surum.Metin;
        Yerlestir();
    }

    void Yerlestir()
    {
        Controls.Add(new BaslikPaneli
        {
            BaslikMetni = Marka.Ad,
            AltMetin = Marka.Slogan,
            SurumMetni = "v" + kuruluSurum,
            LogoResmi = KurulumTema.Logo()
        });

        Controls.Add(new Label
        {
            Text = "Kaldırma",
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = KurulumTema.Yazi,
            BackColor = KurulumTema.Zemin,
            AutoSize = true,
            Location = new Point(28, 132)
        });

        Controls.Add(new Label
        {
            Text = "Yaman PC Cleaner bu bilgisayardan kaldırılacak. Program dosyaları, masaüstü ve " +
                   "başlat menüsü kısayolları silinir. Kişisel ayarlarınız varsayılan olarak korunur.",
            ForeColor = KurulumTema.Soluk,
            BackColor = KurulumTema.Zemin,
            AutoSize = false,
            Location = new Point(28, 168),
            Size = new Size(544, 48)
        });

        var kart = new KurulumKarti { Location = new Point(28, 226), Size = new Size(544, 98) };
        kart.Controls.Add(new Label
        {
            Text = "KURULUM KLASÖRÜ",
            Font = KurulumTema.Etiket,
            ForeColor = KurulumTema.Soluk,
            BackColor = KurulumTema.Panel,
            AutoSize = true,
            Location = new Point(20, 14)
        });
        kart.Controls.Add(new Label
        {
            Text = KurulumBilgi.Klasor,
            Font = KurulumTema.Govde,
            ForeColor = KurulumTema.Yazi,
            BackColor = KurulumTema.Panel,
            AutoSize = false,
            AutoEllipsis = true,
            Location = new Point(20, 36),
            Size = new Size(504, 22)
        });
        kart.Controls.Add(new Label
        {
            Text = "Kurulu sürüm: v" + kuruluSurum,
            Font = KurulumTema.Kucuk,
            ForeColor = KurulumTema.Soluk,
            BackColor = KurulumTema.Panel,
            AutoSize = true,
            Location = new Point(20, 64)
        });
        Controls.Add(kart);

        chkAyarlar.Location = new Point(28, 342);
        chkAyarlar.Width = 500;
        chkAyarlar.Secili = false;
        Controls.Add(chkAyarlar);

        Controls.Add(new Panel
        {
            BackColor = KurulumTema.Cizgi,
            Location = new Point(28, 384),
            Size = new Size(544, 1)
        });

        lblDurum.Text = "Kaldırmaya hazır.";
        lblDurum.ForeColor = KurulumTema.Soluk;
        lblDurum.BackColor = KurulumTema.Zemin;
        lblDurum.AutoSize = false;
        lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        lblDurum.Bounds = new Rectangle(28, 398, 330, 22);
        Controls.Add(lblDurum);

        btnVazgec.Bounds = new Rectangle(372, 392, 90, 40);
        btnVazgec.Click += delegate { Close(); };
        Controls.Add(btnVazgec);

        btnKaldir.Bounds = new Rectangle(472, 392, 100, 40);
        btnKaldir.Click += Kaldir_Click;
        Controls.Add(btnKaldir);

        CancelButton = btnVazgec;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        KurulumTema.KoyuBaslik(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (kaldiriliyor && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }

    void Kaldir_Click(object sender, EventArgs e)
    {
        if (bitti) { Close(); return; }
        if (kaldiriliyor) return;

        if (KurulumBilgi.UygulamaAcik())
        {
            var cevap = MessageBox.Show(this,
                "Yaman PC Cleaner şu anda açık. Kaldırmaya devam etmek için kapatılsın mı?",
                Marka.Ad, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (cevap != DialogResult.Yes)
            {
                lblDurum.Text = "Kaldırma için önce uygulamayı kapat.";
                return;
            }
            KurulumBilgi.UygulamayiKapat(true);
        }

        kaldiriliyor = true;
        chkAyarlar.Enabled = false;
        btnKaldir.Enabled = false;
        btnVazgec.Enabled = false;
        lblDurum.ForeColor = KurulumTema.Soluk;
        lblDurum.Text = "Kaldırılıyor...";
        Cursor = Cursors.WaitCursor;

        bool ayarlar = chkAyarlar.Secili;
        ThreadPool.QueueUserWorkItem(delegate(object durum)
        {
            string hata = null, klasor = null;
            try { klasor = KurulumBilgi.Kaldir(ayarlar); }
            catch (Exception ex)
            {
                hata = ex.Message;
                KurulumBilgi.Log("Kaldırma hatası: " + ex);
            }
            try { BeginInvoke(new Action(delegate { KaldirmaBitti(hata, klasor, ayarlar); })); }
            catch { }
        });
    }

    void KaldirmaBitti(string hata, string klasor, bool ayarlarSilindi)
    {
        kaldiriliyor = false;
        Cursor = Cursors.Default;

        if (hata != null)
        {
            lblDurum.ForeColor = KurulumTema.Kirmizi;
            lblDurum.Text = "Kaldırma tamamlanamadı.";
            chkAyarlar.Enabled = true;
            btnKaldir.Enabled = true;
            btnVazgec.Enabled = true;
            MessageBox.Show(this, "Kaldırma tamamlanamadı.\n\n" + hata,
                Marka.Ad + " Kaldırma", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        bitti = true;
        lblDurum.ForeColor = KurulumTema.Yesil;
        lblDurum.Text = "Yaman PC Cleaner kaldırıldı.";
        btnKaldir.Text = "Kapat";
        btnKaldir.Enabled = true;
        btnVazgec.Visible = false;

        MessageBox.Show(this,
            "Yaman PC Cleaner bu bilgisayardan kaldırıldı.\n\n" +
            (ayarlarSilindi ? "Kişisel ayarlar da silindi." : "Kişisel ayarlarınız korundu."),
            Marka.Ad, MessageBoxButtons.OK, MessageBoxIcon.Information);

        if (!string.IsNullOrEmpty(klasor)) KurulumBilgi.TemizligiPlanla(klasor);
        Close();
    }
}

static class Kaldirici
{
    [STAThread]
    static void Main(string[] args)
    {
        bool sessiz = false;
        foreach (string a in args)
        {
            if (a.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("/sessiz", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--silent", StringComparison.OrdinalIgnoreCase))
                sessiz = true;
        }

        if (sessiz)
        {
            try
            {
                string klasor = KurulumBilgi.Kaldir(false);
                KurulumBilgi.TemizligiPlanla(klasor);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                KurulumBilgi.Log("Sessiz kaldırma hatası: " + e);
                Environment.Exit(1);
            }
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new KaldiriciFormu());
    }
}
