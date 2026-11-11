// Yaman PC Cleaner kurulum penceresi. Uygulamayı kullanıcı hesabına kurar; yönetici izni istemez.
// setup.exe /S sessiz kurulum yapar (kısayol ve otomatik açma olmadan).
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

class KurulumFormu : Form
{
    readonly OnayKutusu chkMasaustu = new OnayKutusu("Masaüstüne kısayol oluştur");
    readonly OnayKutusu chkAc = new OnayKutusu("Kurulumdan sonra Yaman PC Cleaner'ı aç");
    readonly KurulumDugmesi btnKur = new KurulumDugmesi("Kur", DugmeTuru.Birincil);
    readonly KurulumDugmesi btnIptal = new KurulumDugmesi("İptal", DugmeTuru.Ikincil);
    readonly Label lblDurum = new Label();
    readonly string kuruluSurum;
    bool kuruluyor, kuruldu;

    public KurulumFormu()
    {
        Text = Marka.Ad + " Kurulum";
        ClientSize = new Size(620, 500);
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

        kuruluSurum = KurulumBilgi.KuruluSurum();
        Yerlestir();
    }

    void Yerlestir()
    {
        var baslik = new BaslikPaneli
        {
            BaslikMetni = Marka.Ad,
            AltMetin = Marka.Slogan,
            SurumMetni = "v" + Surum.Metin,
            LogoResmi = KurulumTema.Logo()
        };
        Controls.Add(baslik);

        var lblBaslik = new Label
        {
            Text = "Kurulum",
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = KurulumTema.Yazi,
            BackColor = KurulumTema.Zemin,
            AutoSize = true,
            Location = new Point(28, 132)
        };
        Controls.Add(lblBaslik);

        string aciklama = "Yaman PC Cleaner bu bilgisayarda yalnızca sizin kullanıcı hesabınız için kurulacak. " +
                          "Yönetici izni gerekmez.";
        if (!string.IsNullOrEmpty(kuruluSurum))
            aciklama += "\nKurulu sürüm: v" + kuruluSurum + ". Kurulum, mevcut dosyaların üzerine yazacak.";
        Controls.Add(new Label
        {
            Text = aciklama,
            ForeColor = KurulumTema.Soluk,
            BackColor = KurulumTema.Zemin,
            AutoSize = false,
            Location = new Point(28, 168),
            Size = new Size(564, 52)
        });

        var kart = new KurulumKarti { Location = new Point(28, 226), Size = new Size(564, 102) };
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
            Size = new Size(524, 22)
        });
        kart.Controls.Add(new Label
        {
            Text = "Yaklaşık 1 MB · Kişisel ayarlar %APPDATA%\\YamanPcCleaner içinde tutulur.",
            Font = KurulumTema.Kucuk,
            ForeColor = KurulumTema.Soluk,
            BackColor = KurulumTema.Panel,
            AutoSize = true,
            Location = new Point(20, 64)
        });
        Controls.Add(kart);

        chkMasaustu.Location = new Point(28, 348);
        chkMasaustu.Width = 420;
        chkMasaustu.Secili = true;
        Controls.Add(chkMasaustu);

        chkAc.Location = new Point(28, 380);
        chkAc.Width = 480;
        chkAc.Secili = true;
        Controls.Add(chkAc);

        Controls.Add(new Panel
        {
            BackColor = KurulumTema.Cizgi,
            Location = new Point(28, 426),
            Size = new Size(564, 1)
        });

        lblDurum.Text = string.IsNullOrEmpty(kuruluSurum)
            ? "Kuruluma hazır."
            : "Kurulu sürüm v" + kuruluSurum + " algılandı.";
        lblDurum.ForeColor = KurulumTema.Soluk;
        lblDurum.BackColor = KurulumTema.Zemin;
        lblDurum.AutoSize = false;
        lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        lblDurum.Bounds = new Rectangle(28, 442, 330, 22);
        Controls.Add(lblDurum);

        btnIptal.Bounds = new Rectangle(362, 436, 90, 40);
        btnIptal.Click += delegate { Close(); };
        Controls.Add(btnIptal);

        btnKur.Bounds = new Rectangle(462, 436, 130, 40);
        if (!string.IsNullOrEmpty(kuruluSurum))
            btnKur.Text = kuruluSurum == Surum.Metin ? "Yeniden kur" : "Güncelle";
        btnKur.Click += Kur_Click;
        Controls.Add(btnKur);

        AcceptButton = btnKur;
        CancelButton = btnIptal;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        KurulumTema.KoyuBaslik(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (kuruluyor && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }

    void Kur_Click(object sender, EventArgs e)
    {
        if (kuruldu) { Close(); return; }
        if (kuruluyor) return;

        if (KurulumBilgi.UygulamaAcik())
        {
            var cevap = MessageBox.Show(this,
                "Yaman PC Cleaner şu anda açık. Kuruluma devam etmek için kapatılsın mı?",
                Marka.Ad, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (cevap != DialogResult.Yes)
            {
                lblDurum.Text = "Kurulum için önce uygulamayı kapat.";
                return;
            }
            KurulumBilgi.UygulamayiKapat(true);
        }

        kuruluyor = true;
        chkMasaustu.Enabled = false;
        chkAc.Enabled = false;
        btnKur.Enabled = false;
        btnIptal.Enabled = false;
        lblDurum.ForeColor = KurulumTema.Soluk;
        lblDurum.Text = "Kuruluyor...";
        Cursor = Cursors.WaitCursor;

        bool masaustu = chkMasaustu.Secili;
        ThreadPool.QueueUserWorkItem(delegate(object durum)
        {
            string hata = null;
            try { KurulumBilgi.Kur(masaustu); }
            catch (Exception ex)
            {
                hata = ex.Message;
                KurulumBilgi.Log("Kurulum hatası: " + ex);
            }
            try { BeginInvoke(new Action(delegate { KurulumBitti(hata); })); }
            catch { }
        });
    }

    void KurulumBitti(string hata)
    {
        kuruluyor = false;
        Cursor = Cursors.Default;

        if (hata != null)
        {
            lblDurum.ForeColor = KurulumTema.Kirmizi;
            lblDurum.Text = "Kurulum tamamlanamadı.";
            chkMasaustu.Enabled = true;
            chkAc.Enabled = true;
            btnKur.Enabled = true;
            btnIptal.Enabled = true;
            MessageBox.Show(this, "Kurulum tamamlanamadı.\n\n" + hata,
                Marka.Ad + " Kurulum", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        kuruldu = true;
        lblDurum.ForeColor = KurulumTema.Yesil;
        lblDurum.Text = "Kurulum tamamlandı. Yaman PC Cleaner kullanıma hazır.";
        btnKur.Text = "Kapat";
        btnKur.Enabled = true;
        btnIptal.Visible = false;

        if (chkAc.Secili)
        {
            try { Process.Start(KurulumBilgi.Exe); }
            catch (Exception e) { KurulumBilgi.Log("Uygulama başlatılamadı: " + e.Message); }
            Close();
        }
    }
}

static class Kurulum
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
                if (KurulumBilgi.UygulamaAcik()) KurulumBilgi.UygulamayiKapat(true);
                KurulumBilgi.Kur(false);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                KurulumBilgi.Log("Sessiz kurulum hatası: " + e);
                Environment.Exit(1);
            }
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new KurulumFormu());
    }
}
