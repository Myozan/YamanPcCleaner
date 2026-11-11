// Kurulum ve kaldırma pencerelerinin ortak görsel dili: renkler, başlık, kart, düğme ve onay kutusu.
// Uygulamanın koyu temasıyla aynı paleti kullanır; harici bir görsel kitaplığa ihtiyaç duymaz.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class KurulumTema
{
    public static readonly Color Zemin = Color.FromArgb(8, 13, 24);
    public static readonly Color Panel = Color.FromArgb(15, 26, 44);
    public static readonly Color PanelUst = Color.FromArgb(21, 36, 59);
    public static readonly Color Cizgi = Color.FromArgb(34, 52, 79);
    public static readonly Color Yazi = Color.FromArgb(234, 241, 251);
    public static readonly Color Soluk = Color.FromArgb(147, 167, 196);
    public static readonly Color Vurgu = Color.FromArgb(59, 130, 246);
    public static readonly Color VurguAcik = Color.FromArgb(96, 165, 250);
    public static readonly Color Yesil = Color.FromArgb(52, 211, 153);
    public static readonly Color Kirmizi = Color.FromArgb(240, 82, 82);
    public static readonly Color KirmiziKoyu = Color.FromArgb(127, 29, 29);

    public static readonly Font Baslik = new Font("Segoe UI Semibold", 19f);
    public static readonly Font AltBaslik = new Font("Segoe UI", 9.5f);
    public static readonly Font Govde = new Font("Segoe UI", 9.5f);
    public static readonly Font Kucuk = new Font("Segoe UI", 8.75f);
    public static readonly Font Etiket = new Font("Segoe UI Semibold", 8f);
    public static readonly Font Dugme = new Font("Segoe UI Semibold", 9.5f);

    public static GraphicsPath Yuvarlak(Rectangle r, int yaricap)
    {
        var yol = new GraphicsPath();
        int d = Math.Min(yaricap * 2, Math.Min(r.Width, r.Height));
        if (d <= 1) { yol.AddRectangle(r); return yol; }
        yol.AddArc(r.X, r.Y, d, d, 180, 90);
        yol.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        yol.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        yol.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        yol.CloseFigure();
        return yol;
    }

    public static void Kart(Graphics g, Rectangle r, Color dolgu, Color cizgi, int yaricap)
    {
        using (var yol = Yuvarlak(r, yaricap))
        {
            using (var b = new SolidBrush(dolgu)) g.FillPath(b, yol);
            if (cizgi.A > 0) using (var p = new Pen(cizgi)) g.DrawPath(p, yol);
        }
    }

    public static Image Logo()
    {
        try
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png"))
            {
                if (s == null) return null;
                using (var b = new Bitmap(s)) return new Bitmap(b);
            }
        }
        catch { return null; }
    }

    public static Color Karistir(Color a, Color b, double oran)
    {
        if (oran < 0) oran = 0;
        if (oran > 1) oran = 1;
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * oran),
            (int)(a.R + (b.R - a.R) * oran),
            (int)(a.G + (b.G - a.G) * oran),
            (int)(a.B + (b.B - a.B) * oran));
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr h, int ozellik, ref int deger, int boyut);

    // Windows 10/11 başlık çubuğunu koyu yapar; desteklenmiyorsa sessizce devam eder.
    public static void KoyuBaslik(Form form)
    {
        try
        {
            int koyu = 1;
            DwmSetWindowAttribute(form.Handle, 20, ref koyu, 4);
        }
        catch { }
    }
}

// Kurulum penceresinin üst şeridi: logo, ürün adı, slogan ve sürüm rozeti.
class BaslikPaneli : Panel
{
    public string BaslikMetni = "";
    public string AltMetin = "";
    public string SurumMetni = "";
    public Image LogoResmi;

    public BaslikPaneli()
    {
        Dock = DockStyle.Top;
        Height = 112;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = KurulumTema.Zemin;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new LinearGradientBrush(ClientRectangle, KurulumTema.Panel, KurulumTema.Zemin, 0f))
            g.FillRectangle(b, ClientRectangle);

        if (LogoResmi != null)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(LogoResmi, new Rectangle(28, 26, 56, 56));
        }

        TextRenderer.DrawText(g, BaslikMetni, KurulumTema.Baslik,
            new Rectangle(100, 20, 430, 38), KurulumTema.Yazi,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, AltMetin, KurulumTema.AltBaslik,
            new Rectangle(102, 57, 430, 20), KurulumTema.VurguAcik,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

        if (!string.IsNullOrEmpty(SurumMetni))
        {
            Size olcu = TextRenderer.MeasureText(g, SurumMetni, KurulumTema.Kucuk);
            int genislik = Math.Max(56, olcu.Width + 18);
            var rozet = new Rectangle(102, 80, genislik, 22);
            KurulumTema.Kart(g, rozet, KurulumTema.PanelUst, KurulumTema.Cizgi, 11);
            TextRenderer.DrawText(g, SurumMetni, KurulumTema.Kucuk, rozet, KurulumTema.Soluk,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }

        using (var b = new LinearGradientBrush(new Rectangle(0, Height - 1, Math.Max(1, Width), 1),
            KurulumTema.Vurgu, KurulumTema.Cizgi, 0f))
            g.FillRectangle(b, 0, Height - 1, Width, 1);
    }
}

// Yuvarlatılmış kart zemini; altındaki pencere rengini köşelerde korur.
class KurulumKarti : Panel
{
    public KurulumKarti()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = KurulumTema.Panel;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent != null ? Parent.BackColor : KurulumTema.Zemin);
        KurulumTema.Kart(g, new Rectangle(0, 0, Width - 1, Height - 1),
            KurulumTema.Panel, KurulumTema.Cizgi, 12);
    }
}

enum DugmeTuru { Birincil, Ikincil, Tehlike }

// Kurulumun kendi çizilen düğmesi: yuvarlak köşe, fare ve basılı durumu.
class KurulumDugmesi : Button
{
    public DugmeTuru Tur;
    bool fare, basili;

    public KurulumDugmesi(string metin, DugmeTuru tur)
    {
        Text = metin;
        Tur = tur;
        Height = 40;
        Width = 112;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Font = KurulumTema.Dugme;
        BackColor = KurulumTema.Zemin;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent != null ? Parent.BackColor : KurulumTema.Zemin);

        Color dolgu, cizgi, yazi;
        if (!Enabled)
        {
            dolgu = KurulumTema.Panel;
            cizgi = KurulumTema.Cizgi;
            yazi = KurulumTema.Soluk;
        }
        else if (Tur == DugmeTuru.Birincil)
        {
            dolgu = basili ? KurulumTema.Karistir(KurulumTema.Vurgu, Color.Black, 0.18)
                 : fare ? KurulumTema.VurguAcik : KurulumTema.Vurgu;
            cizgi = dolgu;
            yazi = Color.White;
        }
        else if (Tur == DugmeTuru.Tehlike)
        {
            dolgu = basili ? KurulumTema.Karistir(KurulumTema.KirmiziKoyu, Color.Black, 0.15)
                 : fare ? KurulumTema.Karistir(KurulumTema.KirmiziKoyu, KurulumTema.Kirmizi, 0.25)
                 : KurulumTema.KirmiziKoyu;
            cizgi = fare ? KurulumTema.Kirmizi : KurulumTema.Karistir(KurulumTema.KirmiziKoyu, Color.White, 0.12);
            yazi = Color.White;
        }
        else
        {
            dolgu = basili ? KurulumTema.Panel : fare ? KurulumTema.Karistir(KurulumTema.PanelUst, KurulumTema.Vurgu, 0.16) : KurulumTema.PanelUst;
            cizgi = fare ? KurulumTema.Vurgu : KurulumTema.Cizgi;
            yazi = KurulumTema.Yazi;
        }

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var yol = KurulumTema.Yuvarlak(r, 9))
        {
            using (var b = new SolidBrush(dolgu)) g.FillPath(b, yol);
            using (var p = new Pen(cizgi)) g.DrawPath(p, yol);
        }
        TextRenderer.DrawText(g, Text, Font, r, yazi,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(4, 4, Width - 9, Height - 9), yazi, dolgu);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); fare = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); fare = false; basili = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { basili = true; Invalidate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); basili = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
}

// Kurulum seçenekleri: sistem onay kutusu yerine koyu temaya uyan küçük kutu.
class OnayKutusu : Control
{
    bool secili = true;

    public bool Secili
    {
        get { return secili; }
        set { if (secili != value) { secili = value; Invalidate(); } }
    }

    public OnayKutusu(string metin)
    {
        Text = metin;
        Height = 28;
        Width = 340;
        TabStop = true;
        Cursor = Cursors.Hand;
        Font = KurulumTema.Govde;
        ForeColor = KurulumTema.Yazi;
        BackColor = KurulumTema.Zemin;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent != null ? Parent.BackColor : KurulumTema.Zemin);

        int boy = 18;
        var kutu = new Rectangle(0, (Height - boy) / 2, boy, boy);
        using (var yol = KurulumTema.Yuvarlak(kutu, 5))
        {
            using (var b = new SolidBrush(secili ? KurulumTema.Vurgu : KurulumTema.PanelUst)) g.FillPath(b, yol);
            using (var p = new Pen(secili ? KurulumTema.Vurgu : KurulumTema.Cizgi)) g.DrawPath(p, yol);
        }
        if (secili)
        {
            using (var p = new Pen(Color.White, 2f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, new Point[]
                {
                    new Point(kutu.Left + 4, kutu.Top + 9),
                    new Point(kutu.Left + 8, kutu.Top + 13),
                    new Point(kutu.Left + 14, kutu.Top + 5)
                });
            }
        }

        TextRenderer.DrawText(g, Text, Font, new Rectangle(27, 0, Math.Max(0, Width - 27), Height),
            Enabled ? KurulumTema.Yazi : KurulumTema.Soluk,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, new Rectangle(25, 2, Math.Max(0, Width - 27), Height - 4),
                KurulumTema.Yazi, KurulumTema.Zemin);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (!Enabled) return;
        Secili = !Secili;
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space && Enabled)
        {
            Secili = !Secili;
            e.Handled = true;
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
