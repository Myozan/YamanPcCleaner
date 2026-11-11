// Yaman PC Cleaner - tasarim dili, renk paleti, logo ve ortak arayuz bilesenleri.
// Yaman PC Cleaner · kurumsal tasarım dili.
// Renk, tipografi, yuvarlatılmış kart/düğme ve sol gezinme çubuğu bileşenleri tek yerde.
// Windows 10/11 ile gelen Segoe UI ve Segoe MDL2 Assets yazı tipleri kullanılır; ek kurulum gerekmez.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

static class Tasarim
{
    public const int YanGenislik = 244;
    public const int UstYukseklik = 66;
    public const int KartYaricap = 10;
    public const int DugmeYaricap = 8;

    public static readonly Font Marka = new Font("Segoe UI Semibold", 14.5f, FontStyle.Bold);
    public static readonly Font MarkaAlt = new Font("Segoe UI", 7.75f);
    public static readonly Font Gezinme = new Font("Segoe UI", 9.5f);
    public static readonly Font GezinmeKalın = new Font("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font GezinmeIkon = new Font("Segoe MDL2 Assets", 11.5f);
    public static readonly Font Sayfa = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold);
    public static readonly Font Alt = new Font("Segoe UI", 8.5f);
    public static readonly Font KartBaslik = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold);
    public static readonly Font Kpi = new Font("Segoe UI Semibold", 20f, FontStyle.Bold);
    public static readonly Font KpiEtiket = new Font("Segoe UI", 7.75f, FontStyle.Bold);
    public static readonly Font Metin = new Font("Segoe UI", 9.25f);
    public static readonly Font Kucuk = new Font("Segoe UI", 8.25f);
    public static readonly Font Dugme = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
    public static readonly Font Mono = new Font("Consolas", 8.75f);

    public static Color Karistir(Color a, Color b, double oran)
    {
        oran = Math.Max(0, Math.Min(1, oran));
        return Color.FromArgb(
            (int)Math.Round(a.R + (b.R - a.R) * oran),
            (int)Math.Round(a.G + (b.G - a.G) * oran),
            (int)Math.Round(a.B + (b.B - a.B) * oran));
    }

    public static GraphicsPath Yuvarlak(Rectangle r, int yaricap)
    {
        var yol = new GraphicsPath();
        int d = Math.Max(2, Math.Min(yaricap * 2, Math.Min(r.Width, r.Height)));
        if (r.Width <= 0 || r.Height <= 0) return yol;
        if (d <= 3) { yol.AddRectangle(r); return yol; }
        yol.AddArc(r.X, r.Y, d, d, 180, 90);
        yol.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        yol.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        yol.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        yol.CloseFigure();
        return yol;
    }

    // Kart: yuvarlak dolgu + 1 px çerçeve. Köşelerin dışı üst panelin rengiyle doldurulur.
    public static void Kart(Graphics g, Rectangle r, Color dolgu, Color cizgi, int yaricap)
    {
        if (r.Width <= 1 || r.Height <= 1) return;
        var eski = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var yol = Yuvarlak(r, yaricap))
        using (var b = new SolidBrush(dolgu)) g.FillPath(b, yol);
        using (var yol = Yuvarlak(r, yaricap))
        using (var p = new Pen(cizgi)) g.DrawPath(p, yol);
        g.SmoothingMode = eski;
    }

    // Sayfa başlığı + açıklama; panelin üstüne yerleştirilir.
    public static Panel Baslik(string baslik, string aciklama, int yukseklik)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = yukseklik, Padding = new Padding(2, 0, 2, 6) };
        var alt = new Label
        {
            Dock = DockStyle.Bottom, Height = 20, Text = aciklama, Font = Alt, ForeColor = Renk.Soluk,
            AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft
        };
        var ust = new Label
        {
            Dock = DockStyle.Fill, Text = baslik, Font = Sayfa, ForeColor = Renk.Yazi,
            AutoEllipsis = true, TextAlign = ContentAlignment.BottomLeft
        };
        p.Controls.Add(ust);
        p.Controls.Add(alt);
        return p;
    }

    public static Label Etiket(string metin, Font font, Color renk)
    {
        return new Label { Text = metin, Font = font, ForeColor = renk, AutoEllipsis = true, AutoSize = false };
    }
}

// Sol gezinme zemini: dikey gradyan ve sağ kenarda yumuşak vurgu çizgisi.
class YanPanel : Panel
{
    public YanPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Renk.Yan;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (var b = new LinearGradientBrush(ClientRectangle, Renk.Yan,
            Tasarim.Karistir(Renk.Yan, Renk.Arka, 0.62), 90f))
            e.Graphics.FillRectangle(b, ClientRectangle);
        using (var p = new Pen(Color.FromArgb(90, Renk.Vurgu)))
            e.Graphics.DrawLine(p, Width - 1, 0, Width - 1, Height);
    }
}

// Yuvarlatılmış kart paneli: altındaki zeminin rengini köşelerde korur, ince çerçeve çizer.
class KartPaneli : Panel
{
    public int Yaricap = Tasarim.KartYaricap;
    public bool Cerceve = true;
    public Color Dolgu = Renk.Panel;

    public KartPaneli()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Renk.Panel;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent != null ? Parent.BackColor : Renk.Arka);
        Tasarim.Kart(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), Dolgu,
            Cerceve ? Renk.Cizgi : Dolgu, Yaricap);
    }
}

// Bölüm ayırıcı: soldan sağa sönen vurgu çizgisi.
class VurguCizgisi : Panel
{
    public VurguCizgisi()
    {
        Height = 1;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Renk.Cizgi;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (var b = new LinearGradientBrush(ClientRectangle, Renk.Vurgu, Renk.Cizgi, 0f))
            e.Graphics.FillRectangle(b, ClientRectangle);
    }
}

// Küçük durum etiketi: yuvarlak zemin, ince çerçeve ve tek satır metin.
class Kapsul : Control
{
    public Color Dolgu = Renk.PanelUzerinde;
    public Color Cizgi = Renk.Cizgi;
    public Color YaziRengi = Renk.Soluk;
    public Font Yazi = Tasarim.Kucuk;

    public Kapsul(string metin)
    {
        Text = metin;
        Height = 24;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Renk.Panel;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent != null ? Parent.BackColor : Renk.Arka);
        Tasarim.Kart(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), Dolgu, Cizgi, Height / 2);
        TextRenderer.DrawText(e.Graphics, Text, Yazi, new Rectangle(8, 0, Width - 16, Height), YaziRengi,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

// Sol gezinme çubuğundaki öğe: ikon + yazı, seçiliyken vurgu çizgisi ve panel zemini.
class GezinmeDugmesi : Button
{
    public string Ikon;
    public bool Uyari;
    bool secili, fare;

    public GezinmeDugmesi(string metin, string ikon)
    {
        Text = metin;
        Ikon = ikon;
        Height = 42;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = Tasarim.Gezinme;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Renk.Yan;
    }

    public bool Secili
    {
        get { return secili; }
        set { if (secili != value) { secili = value; Invalidate(); } }
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); fare = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); fare = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int sol = 8, sag = Width - 8;
        if (secili)
        {
            using (var yol = Tasarim.Yuvarlak(new Rectangle(sol, 2, sag - sol, Height - 4), 8))
            using (var b = new SolidBrush(Renk.Panel)) g.FillPath(b, yol);
            using (var b = new SolidBrush(Renk.Vurgu)) g.FillRectangle(b, sol, 11, 3, Height - 22);
        }
        else if (fare)
        {
            using (var yol = Tasarim.Yuvarlak(new Rectangle(sol, 2, sag - sol, Height - 4), 8))
            using (var b = new SolidBrush(Renk.YanUzerinde)) g.FillPath(b, yol);
        }
        if (Focused && ShowFocusCues)
            using (var p = new Pen(Renk.Cizgi)) g.DrawRectangle(p, sol, 2, sag - sol - 1, Height - 5);
        var ikonRenk = secili ? Renk.Vurgu : Uyari ? Renk.Turuncu : fare ? Renk.Yazi : Renk.Soluk;
        var yaziRenk = secili ? Renk.Yazi : Uyari ? Renk.Turuncu : fare ? Renk.Yazi : Renk.Soluk;
        TextRenderer.DrawText(g, Ikon, Tasarim.GezinmeIkon, new Rectangle(sol + 12, 0, 26, Height), ikonRenk,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, Text, secili ? Tasarim.GezinmeKalın : Tasarim.Gezinme,
            new Rectangle(sol + 44, 0, sag - sol - 48, Height), yaziRenk,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (Uyari && !secili)
            using (var b = new SolidBrush(Renk.Turuncu)) g.FillEllipse(b, sag - 16, (Height - 7) / 2, 7, 7);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}

// Kurumsal görünümlü düğme: yuvarlak köşe, dolgu / çerçeve / sade varyantları.
class EylemDugmesi : Button
{
    public enum Tur { Birincil, Ikincil, Sade, Tehlike }

    public Tur Gorunum = Tur.Ikincil;
    public string Ikon;
    bool fare, basili;

    public EylemDugmesi(string metin, Tur tur)
    {
        Text = metin;
        Gorunum = tur;
        Height = 34;
        BackColor = Renk.Arka;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Tasarim.Dugme;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); fare = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); fare = false; basili = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); basili = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); basili = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent != null ? Parent.BackColor : Renk.Arka);
        Color dolgu, cizgi, yazi;
        switch (Gorunum)
        {
            case Tur.Birincil:
                dolgu = fare ? Tasarim.Karistir(Renk.Vurgu, Color.White, 0.12) : Renk.Vurgu;
                cizgi = dolgu; yazi = Color.White; break;
            case Tur.Tehlike:
                dolgu = fare ? Tasarim.Karistir(Renk.Kirmizi, Color.White, 0.10) : Renk.Kirmizi;
                cizgi = dolgu; yazi = Color.White; break;
            case Tur.Sade:
                dolgu = fare ? Renk.YanUzerinde : Color.Transparent;
                cizgi = Color.Transparent; yazi = fare ? Renk.Yazi : Renk.Soluk; break;
            default:
                dolgu = fare ? Renk.PanelUzerinde : Renk.Panel;
                cizgi = Renk.Cizgi; yazi = Renk.Yazi; break;
        }
        if (!Enabled)
        {
            dolgu = Tasarim.Karistir(dolgu, Renk.Arka, 0.55);
            cizgi = Tasarim.Karistir(cizgi, Renk.Arka, 0.55);
            yazi = Renk.Soluk;
        }
        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        if (Gorunum == Tur.Birincil || Gorunum == Tur.Tehlike)
        {
            using (var yol = Tasarim.Yuvarlak(r, Tasarim.DugmeYaricap))
            using (var b = new SolidBrush(dolgu)) g.FillPath(b, yol);
        }
        else
        {
            using (var yol = Tasarim.Yuvarlak(r, Tasarim.DugmeYaricap))
            using (var b = new SolidBrush(dolgu)) if (dolgu != Color.Transparent) g.FillPath(b, yol);
            if (cizgi != Color.Transparent)
                using (var yol = Tasarim.Yuvarlak(new Rectangle(0, 0, Width - 1, Height - 1), Tasarim.DugmeYaricap))
                using (var p = new Pen(cizgi)) g.DrawPath(p, yol);
        }
        if (basili) using (var b = new SolidBrush(Color.FromArgb(38, Color.Black))) g.FillRectangle(b, r);
        int ikon = string.IsNullOrEmpty(Ikon) ? 0 : 24;
        var alan = new Rectangle(8, 0, Width - 16, Height);
        if (ikon > 0)
            TextRenderer.DrawText(g, Ikon, Tasarim.GezinmeIkon, new Rectangle(12, 0, 18, Height), yazi,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(8 + ikon, 0, Width - 16 - ikon, Height), yazi,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
static class Renk
{
    // Kurumsal palet: gece laciverti zemin, katmanlı panel, mavi vurgu, zümrüt başarı, amber uyarı.
    public static readonly Color Arka = Color.FromArgb(10, 16, 28);
    public static readonly Color Panel = Color.FromArgb(17, 27, 46);
    public static readonly Color PanelUzerinde = Color.FromArgb(23, 36, 59);
    public static readonly Color Yan = Color.FromArgb(13, 21, 36);
    public static readonly Color YanUzerinde = Color.FromArgb(20, 31, 50);
    public static readonly Color Cizgi = Color.FromArgb(34, 49, 74);
    public static readonly Color Yazi = Color.FromArgb(234, 240, 248);
    public static readonly Color Soluk = Color.FromArgb(147, 166, 194);
    public static readonly Color Yesil = Color.FromArgb(52, 211, 153);
    public static readonly Color Turuncu = Color.FromArgb(245, 165, 36);
    public static readonly Color Kirmizi = Color.FromArgb(240, 82, 82);

    // Vurgu rengi (ana düğmeler): kullanıcı Ayarlar → Görünüm'den seçer, ayar "vurgu_rengi" (sıra).
    // Varsayılan kurumsal mavi; başarı rengiyle (zümrüt) karışmasın diye mavi seçildi.
    public const string AyarVurgu = "vurgu_rengi";
    public static readonly string[] VurguAdlari = { "Kurumsal mavi", "Turkuaz", "Mor", "Amber", "Gül" };
    public static readonly Color[] Vurgular =
    {
        Color.FromArgb(59, 130, 246), Color.FromArgb(20, 184, 166), Color.FromArgb(139, 92, 246),
        Color.FromArgb(245, 158, 11), Color.FromArgb(244, 63, 94)
    };
    const int VarsayilanVurgu = 0;
    static int vurgu = -1;

    public static int VurguSirasi
    {
        get
        {
            if (vurgu < 0) { int n = Ayarlar.OkuSayi(AyarVurgu, VarsayilanVurgu); vurgu = n >= 0 && n < Vurgular.Length ? n : VarsayilanVurgu; }
            return vurgu;
        }
    }

    public static Color Vurgu { get { return Vurgular[VurguSirasi]; } }

    // Yeni vurgu açık pencerelere hemen işlenir: eski vurgu renginde olan her zemin / yazı yeni renge çekilir.
    public static void VurguSec(int n)
    {
        if (n < 0 || n >= Vurgular.Length || n == VurguSirasi) return;
        Color eski = Vurgu;
        vurgu = n;
        Ayarlar.YazSayi(AyarVurgu, n);
        foreach (Form f in Application.OpenForms) { Boya(f, eski, Vurgu); f.Invalidate(true); }
    }

    static void Boya(Control c, Color eski, Color yeni)
    {
        if (c.BackColor.ToArgb() == eski.ToArgb()) c.BackColor = yeni;
        if (c.ForeColor.ToArgb() == eski.ToArgb()) c.ForeColor = yeni;
        foreach (Control alt in c.Controls) Boya(alt, eski, yeni);
    }
}

// Logo exe'ye gömülü: /win32icon Gezgin ve görev çubuğu için, /resource pencere, tepsi ve sekme şeridi için
// (bkz. derle.cmd). Kaynak bulunamazsa Windows'un standart simgesine düşer.
static class Logo
{
    static Icon simge;
    static Image resim;
    static Image gif;

    public static Icon Simge()
    {
        if (simge == null)
            try { using (var s = Kaynak("YamanPcCleaner.ico")) if (s != null) simge = new Icon(s); } catch { }
        return simge ?? SystemIcons.Application;
    }

    // .ico içindeki en yakın boyutu seçer (tepsi için küçük boyut).
    public static Icon Simge(Size boyut)
    {
        try { return new Icon(Simge(), boyut); } catch { return Simge(); }
    }

    public static Image Resim()
    {
        if (resim == null)
            try { using (var s = Kaynak("logo.png")) if (s != null) using (var i = Image.FromStream(s)) resim = new Bitmap(i); } catch { }
        return resim;
    }

    // Açılış ekranındaki animasyonlu logo (kaynaklar\logo.gif, araclar\logo-gif.ps1 ile üretilir).
    // GIF yoksa statik logoya düşer; PictureBox çok kareli GIF'i kendisi oynatır.
    public static Image Gif()
    {
        if (gif == null)
            try { using (var s = Kaynak("logo.gif")) if (s != null) using (var i = Image.FromStream(s)) gif = new Bitmap(i); } catch { }
        return gif ?? Resim();
    }

    static Stream Kaynak(string ad) { return typeof(Logo).Assembly.GetManifestResourceStream(ad); }
}
