// Açılış ekranı: uygulama ilk sistem ölçümünü yaparken animasyonlu logoyu gösterir.
// Ana pencere bu sırada görünmez (Opacity = 0) tutulur; böylece düğmelerin ilk boyanmasındaki beyaz
// kırpışma görünmez. İlk ölçüm bitince ya da en geç 8 saniyede ana pencere yumuşakça açılır.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

class AcilisEkrani : Form
{
    const double EnAzSaniye = 1.8;   // logo en az bu kadar görünsün
    const double EnCokSaniye = 3.5;  // ilk ölçüm uzarsa ana pencere yine açılsın
    const int IlerlemeY = 305;

    readonly Timer zamanlayici = new Timer();
    readonly Stopwatch saat = new Stopwatch();
    readonly PictureBox logo;
    readonly Font baslikYazi = new Font("Segoe UI Semibold", 17f, FontStyle.Bold);
    bool hazir, kapaniyor, bitti;

    // Ana pencereyi görünür yap; Program.Main bağlar. Null olabilir.
    public Action Bitti;

    public AcilisEkrani()
    {
        Text = Marka.Ad + " açılıyor";
        ClientSize = new Size(500, 332);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Renk.Arka;
        AutoScaleMode = AutoScaleMode.None;
        Opacity = 0;   // ilk kare görünmeden yumuşak giriş
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        try { Icon = Logo.Simge(); } catch { }

        logo = new PictureBox
        {
            Image = Logo.Gif(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Bounds = new Rectangle((ClientSize.Width - 200) / 2, 14, 200, 200),
            BackColor = Renk.Arka
        };
        Controls.Add(logo);

        zamanlayici.Interval = 60;
        zamanlayici.Tick += Zamanlayici_Tick;
    }

    // Ana form ilk ölçümü bitirdiğinde çağırır. En az süre dolmadıysa zamanlayıcı bekler.
    public void HazirOldu()
    {
        if (IsDisposed || bitti) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(HazirOldu)); } catch { }
            return;
        }
        hazir = true;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        saat.Start();
        zamanlayici.Start();
    }

    void Zamanlayici_Tick(object sender, EventArgs e)
    {
        if (bitti) return;

        if (kapaniyor)
        {
            Opacity -= 0.14;
            if (Opacity <= 0.01) { Bitir(); return; }
            return;
        }

        if (Opacity < 1) Opacity = Math.Min(1, Opacity + 0.16);

        double sn = saat.Elapsed.TotalSeconds;
        if (sn >= EnCokSaniye) hazir = true;
        if (hazir && sn >= EnAzSaniye) kapaniyor = true;

        Invalidate();
    }

    void Bitir()
    {
        if (bitti) return;
        bitti = true;
        zamanlayici.Stop();
        saat.Stop();
        var isleyici = Bitti;
        Bitti = null;
        if (isleyici != null) { try { isleyici(); } catch { } }
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Kullanıcı açılış ekranını kapatırsa ana pencere görünmez kalırdı; kendi bitişimiz dışında izin verme.
        if (!bitti && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(Renk.Arka)) g.FillRectangle(b, ClientRectangle);

        using (var p = new Pen(Color.FromArgb(74, Renk.Vurgu)))
            g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

        TextRenderer.DrawText(g, Marka.Ad, baslikYazi,
            new Rectangle(0, 222, Width, 32), Renk.Yazi,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, Marka.SloganBuyuk, Tasarim.KpiEtiket,
            new Rectangle(0, 253, Width, 16), Renk.Vurgu,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

        int nokta = (int)(saat.ElapsedMilliseconds / 420) % 4;
        string durum = "Sistem denetleniyor" + new string('.', nokta);
        TextRenderer.DrawText(g, durum, Tasarim.Metin,
            new Rectangle(40, 280, 320, 20), Renk.Soluk,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(g, "v" + Surum.Metin, Tasarim.Kucuk,
            new Rectangle(Width - 130, 310, 90, 16), Renk.Soluk,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

        // İnce ilerleme çizgisi: soldan sağa sürekli akan mavi bant.
        int x = 40, w = Width - 80;
        using (var b = new SolidBrush(Renk.Cizgi)) g.FillRectangle(b, x, IlerlemeY, w, 3);
        double tur = (saat.Elapsed.TotalSeconds % 1.35) / 1.35;
        int parca = 120;
        int px = x - parca + (int)((w + parca) * tur);
        g.SetClip(new Rectangle(x, IlerlemeY, w, 3));
        using (var b = new LinearGradientBrush(new Rectangle(px, IlerlemeY, parca, 3),
            Color.FromArgb(0, Renk.Vurgu), Renk.Vurgu, 0f))
            g.FillRectangle(b, px, IlerlemeY, parca, 3);
        g.ResetClip();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW: kenarsız pencereye hafif gölge
            return cp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            zamanlayici.Dispose();
            baslikYazi.Dispose();
        }
        base.Dispose(disposing);
    }
}
