// Tepsi simgesinde canlı değer ve üstüne gelince açılan mini özet.
// Simge: seçilen değer (RAM, sanal bellek ya da işlemci yüzdesi) renkli bir kutuda; "Logo" seçilirse eski logo.
// Özet: fare simgenin üstüne gelince küçük, odak çalmayan bir pencere (RAM, sanal bellek, işlemci, son sistem puanı);
// fare uzaklaşınca kapanır. Önde tam ekran oyun varken açılmaz.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class TepsiGostergesi
{
    public const string Ayar = "tepsi_gosterge";
    public static readonly string[] Adlar = { "Logo", "RAM %", "Sanal bellek %", "İşlemci %" };

    public static int Secim
    {
        get { int n = Ayarlar.OkuSayi(Ayar, 1); return n >= 0 && n < Adlar.Length ? n : 1; }
        set { Ayarlar.YazSayi(Ayar, value); }
    }

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr h);

    // Sayılı simge. Çağıran eski simgeyi Bırak ile serbest bırakır.
    public static Icon Sayi(int deger, Size boyut)
    {
        deger = Math.Max(0, Math.Min(99, deger));
        int w = Math.Max(16, boyut.Width), h = Math.Max(16, boyut.Height);
        using (var bmp = new Bitmap(w, h))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                var renk = deger >= 90 ? Renk.Kirmizi : deger >= 75 ? Renk.Turuncu : Color.FromArgb(46, 125, 50);
                int r = Math.Max(3, w / 4);
                using (var yol = new GraphicsPath())
                {
                    yol.AddArc(0, 0, r * 2, r * 2, 180, 90);
                    yol.AddArc(w - 1 - r * 2, 0, r * 2, r * 2, 270, 90);
                    yol.AddArc(w - 1 - r * 2, h - 1 - r * 2, r * 2, r * 2, 0, 90);
                    yol.AddArc(0, h - 1 - r * 2, r * 2, r * 2, 90, 90);
                    yol.CloseFigure();
                    using (var b = new SolidBrush(renk)) g.FillPath(b, yol);
                }
                string metin = deger.ToString();
                float punto = h * (metin.Length > 1 ? 0.66f : 0.74f);
                using (var f = new Font("Segoe UI", punto, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var bicim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(metin, f, Brushes.White, new RectangleF(-1, 0, w + 2, h + 1), bicim);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    public static void Birak(Icon i)
    {
        if (i == null) return;
        try { IntPtr h = i.Handle; i.Dispose(); DestroyIcon(h); } catch { }
    }
}

// Fare tepsi simgesinin üstündeyken görünen küçük özet.
class TepsiOzeti : Form
{
    static readonly Font BaslikYazi = new Font("Segoe UI", 9f, FontStyle.Bold);
    static readonly Font Yazi = new Font("Segoe UI", 9f);
    static readonly Font KucukYazi = new Font("Segoe UI", 8f);

    double ram, sanal, cpu = -1;
    int puan = -1;
    Point capa;
    readonly Timer izleyici = new Timer { Interval = 150 };
    readonly Timer gecikme = new Timer { Interval = 350 };

    public TepsiOzeti()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Renk.Panel;
        Size = new Size(250, 150);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        // Fare simgeden (ya da pencereden) uzaklaşınca kapan.
        izleyici.Tick += delegate
        {
            var p = Cursor.Position;
            bool yakin = Math.Abs(p.X - capa.X) <= 20 && Math.Abs(p.Y - capa.Y) <= 20;
            if (!yakin && !Bounds.Contains(p)) Gizle();
        };
        gecikme.Tick += delegate
        {
            gecikme.Stop();
            var p = Cursor.Position;
            if (Math.Abs(p.X - capa.X) <= 12 && Math.Abs(p.Y - capa.Y) <= 12) Ac();
        };
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x08000000 /* NOACTIVATE */ | 0x00000080 /* TOOLWINDOW */ | 0x00000008 /* TOPMOST */;
            cp.ClassStyle |= 0x00020000;   // CS_DROPSHADOW
            return cp;
        }
    }

    public void Degerler(double ram, double sanal, double cpu, int puan)
    {
        this.ram = ram; this.sanal = sanal; this.cpu = cpu; this.puan = puan;
        if (Visible) Invalidate();
    }

    // Fare simgenin üstünde hareket etti: kısa bir beklemeden sonra (fare durduysa) aç.
    public void FareGeldi()
    {
        if (TamEkran.Var()) return;
        var p = Cursor.Position;
        if (Visible) { if (Math.Abs(p.X - capa.X) > 20 || Math.Abs(p.Y - capa.Y) > 20) capa = p; return; }
        capa = p;
        gecikme.Stop();
        gecikme.Start();
    }

    void Ac()
    {
        var alan = Screen.FromPoint(capa).WorkingArea;
        int x = capa.X - Width / 2, y = capa.Y - Height - 18;
        if (capa.Y < alan.Top) y = alan.Top + 8;                     // görev çubuğu üstte
        if (capa.X < alan.Left) x = alan.Left + 8;                   // solda
        if (capa.X > alan.Right) x = alan.Right - Width - 8;         // sağda
        x = Math.Max(alan.Left + 4, Math.Min(x, alan.Right - Width - 4));
        y = Math.Max(alan.Top + 4, Math.Min(y, alan.Bottom - Height - 4));
        Location = new Point(x, y);
        Show();
        izleyici.Start();
        Invalidate();
    }

    public void Gizle()
    {
        izleyici.Stop();
        gecikme.Stop();
        if (Visible) Hide();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        using (var p = new Pen(Renk.Cizgi)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        var logo = Logo.Resim();
        if (logo != null) g.DrawImage(logo, new Rectangle(12, 10, 18, 18));
        TextRenderer.DrawText(g, "Yaman PC Cleaner", BaslikYazi, new Point(34, 11), Renk.Yazi);
        if (puan >= 0)
            TextRenderer.DrawText(g, "Puan " + puan, BaslikYazi, new Rectangle(0, 11, Width - 12, 18), PuanHalkasi.PuanRengi(puan),
                TextFormatFlags.Right | TextFormatFlags.NoPrefix);
        int y = 38;
        Cubuk(g, "RAM", ram, ref y);
        Cubuk(g, "Sanal bellek", sanal, ref y);
        if (cpu >= 0) Cubuk(g, "İşlemci", cpu, ref y);
        TextRenderer.DrawText(g, "Tıkla: aç  ·  Sağ tık: menü", KucukYazi, new Point(12, Height - 22), Renk.Soluk);
    }

    void Cubuk(Graphics g, string ad, double oran, ref int y)
    {
        oran = Math.Max(0, Math.Min(1, oran));
        TextRenderer.DrawText(g, ad, Yazi, new Point(12, y), Renk.Soluk);
        TextRenderer.DrawText(g, "%" + (int)Math.Round(oran * 100), Yazi, new Rectangle(0, y, Width - 12, 18), Renk.Yazi, TextFormatFlags.Right);
        var cubuk = new Rectangle(12, y + 19, Width - 24, 5);
        using (var b = new SolidBrush(Renk.Cizgi)) g.FillRectangle(b, cubuk);
        var renk = oran >= 0.9 ? Renk.Kirmizi : oran >= 0.7 ? Renk.Turuncu : Renk.Yesil;
        using (var b = new SolidBrush(renk)) g.FillRectangle(b, new Rectangle(cubuk.X, cubuk.Y, (int)(cubuk.Width * oran), cubuk.Height));
        y += 31;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { izleyici.Dispose(); gecikme.Dispose(); }
        base.Dispose(disposing);
    }
}
