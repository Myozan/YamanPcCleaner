// Ayarlar sekmesi (şeridin son sekmesi, dişli simgeli): programın ayarları tek yerde. Solda bölümler, sağda satırlar;
// her satırın sağında anahtar, liste ya da düğme. Değişiklik hemen uygulanır ve kaydedilir (ayarlar.txt); "Kaydet" yok.
// Bekçi, kısayol ve ses ayarları eskiden Sağlık sekmesindeydi; Sağlık artık yalnızca durumlarını özetler.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// Hareketli geçişler: göstergelerin kayması, puanın sayarak gelmesi, optimizasyon sonuç sayaçları, anahtarların kayması.
// Kapalıysa değerler hemen yerine oturur. Sık okunduğu için bellekte tutulur.
static class Hareket
{
    public const string Ayar = "hareketler";
    static int acik = -1;

    public static bool Acik
    {
        get { if (acik < 0) acik = Ayarlar.Oku(Ayar, true) ? 1 : 0; return acik == 1; }
        set { acik = value ? 1 : 0; Ayarlar.Yaz(Ayar, value); }
    }
}

// Pencere açılınca hangi sekmeyle başlansın: Genel Bakış, Sistem ya da en son açık kalan sekme.
static class AcilisSekmesi
{
    public const string Ayar = "acilis_sekmesi", AyarSon = "son_sekme";
    public static readonly string[] Adlar = { "Genel Bakış", "Sistem", "Son açık sekme" };

    public static int Secim
    {
        get { int n = Ayarlar.OkuSayi(Ayar, 0); return n >= 0 && n < Adlar.Length ? n : 0; }
        set { Ayarlar.YazSayi(Ayar, value); }
    }

    public static int Sekme(int sekmeSayisi)
    {
        int s = Secim;
        if (s == 1) return 1;
        if (s == 2) { int n = Ayarlar.OkuSayi(AyarSon, 0); return n >= 0 && n < sekmeSayisi ? n : 0; }
        return 0;
    }

    public static void Hatirla(int n) { if (Secim == 2) Ayarlar.YazSayi(AyarSon, n); }
}

// Kalemle çizilen dişli (yazı tipine bağlı değil): halka + sekiz diş.
static class Cark
{
    public static void Ciz(Graphics g, PointF m, float s, Color renk)
    {
        var eski = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var p = new Pen(renk, s * 0.16f)) g.DrawEllipse(p, m.X - s * 0.25f, m.Y - s * 0.25f, s * 0.5f, s * 0.5f);
        using (var p = new Pen(renk, s * 0.18f))
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                float c = (float)Math.Cos(a), sn = (float)Math.Sin(a);
                g.DrawLine(p, m.X + c * s * 0.31f, m.Y + sn * s * 0.31f, m.X + c * s * 0.48f, m.Y + sn * s * 0.48f);
            }
        g.SmoothingMode = eski;
    }
}

// Sekme şeridindeki "Ayarlar": dişli + yazı; dar pencerede yalnızca dişli (Kisa). Zemini ve üstüne gelme rengini
// Button çizer (Text boş), dişliyi ve yazıyı bu sınıf.
class AyarlarDugmesi : Button
{
    public const string Yazi = "Ayarlar";
    const int Cap = 15, Ara = 6;
    bool kisa;

    public AyarlarDugmesi()
    {
        AccessibleName = Yazi;
        new ToolTip().SetToolTip(this, Yazi);
    }

    public bool Kisa { get { return kisa; } set { if (kisa != value) { kisa = value; Invalidate(); } } }

    public int IcGenislik() { return kisa ? Cap : Cap + Ara + TextRenderer.MeasureText(Yazi, Font).Width; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int w = IcGenislik(), x = (Width - w) / 2;
        Cark.Ciz(e.Graphics, new PointF(x + Cap / 2f, Height / 2f), Cap, ForeColor);
        if (!kisa)
            TextRenderer.DrawText(e.Graphics, Yazi, Font, new Rectangle(x + Cap + Ara, 0, w - Cap - Ara + 4, Height), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

// Aç / kapat anahtarı: açıkken vurgu renginde, topuz kayarak geçer. Boşluk tuşuyla da değişir.
class AnahtarDugmesi : Control
{
    static readonly Color KapaliZemin = Color.FromArgb(62, 67, 77);

    bool secili, fare;
    float konum;
    readonly Timer kayan = new Timer { Interval = 16 };

    public event EventHandler CheckedChanged;

    public AnahtarDugmesi()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Size = new Size(46, 24);
        Cursor = Cursors.Hand;
        TabStop = true;
        kayan.Tick += delegate
        {
            float hedef = secili ? 1 : 0, fark = hedef - konum;
            if (Math.Abs(fark) < 0.04f) { konum = hedef; kayan.Stop(); }
            else konum += fark * 0.3f;
            Invalidate();
        };
        Disposed += delegate { kayan.Dispose(); };
    }

    public bool Checked
    {
        get { return secili; }
        set
        {
            if (secili == value) return;
            secili = value;
            if (Hareket.Acik && Visible && IsHandleCreated) kayan.Start();
            else konum = secili ? 1 : 0;
            Invalidate();
            var h = CheckedChanged;
            if (h != null) h(this, EventArgs.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (!Enabled) return;
        Focus();
        Checked = !Checked;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space && Enabled) { Checked = !Checked; e.Handled = true; }
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); fare = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); fare = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    static Color Karistir(Color a, Color b, float t)
    {
        t = Math.Max(0, Math.Min(1, t));
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(1, 1, Width - 3, Height - 3);
        float cap = r.Height;
        Color zemin = Karistir(KapaliZemin, Renk.Vurgu, konum);
        Color topuz = fare ? Color.White : Renk.Yazi;
        if (!Enabled) { zemin = Karistir(zemin, BackColor, 0.55f); topuz = Renk.Soluk; }
        using (var yol = new GraphicsPath())
        {
            yol.AddArc(r.Left, r.Top, cap, cap, 90, 180);
            yol.AddArc(r.Right - cap, r.Top, cap, cap, 270, 180);
            yol.CloseFigure();
            using (var b = new SolidBrush(zemin)) g.FillPath(b, yol);
            if (Focused && ShowFocusCues) using (var p = new Pen(Renk.Yazi, 1f)) g.DrawPath(p, yol);
        }
        float d = cap - 6, x = r.Left + 3 + konum * (r.Width - 6 - d);
        using (var b = new SolidBrush(topuz)) g.FillEllipse(b, x, r.Top + 3, d, d);
    }
}

// Soldaki bölüm listesinin bir öğesi. Seçiliyken panel zemini ve solda vurgu çizgisi.
class AyarMenuOgesi : Control
{
    static readonly Font Yazi = new Font("Segoe UI", 10f), Kalin = new Font("Segoe UI", 10f, FontStyle.Bold);
    static readonly Color Uzerinde = Color.FromArgb(23, 26, 31);

    bool secili, fare;

    // Fareye basılınca seçilir (sekme gibi; bırakmayı beklemez).
    public event EventHandler Secildi;

    public AyarMenuOgesi(string metin)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Text = metin;
        Dock = DockStyle.Top;
        Height = 40;
        Cursor = Cursors.Hand;
    }

    public bool Secili { get { return secili; } set { if (secili != value) { secili = value; Invalidate(); } } }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); fare = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); fare = false; Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        var h = Secildi;
        if (e.Button == MouseButtons.Left && h != null) h(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        if (secili || fare) using (var b = new SolidBrush(secili ? Renk.Panel : Uzerinde)) g.FillRectangle(b, 0, 2, Width, Height - 4);
        if (secili) using (var b = new SolidBrush(Renk.Vurgu)) g.FillRectangle(b, 0, 10, 3, Height - 20);
        TextRenderer.DrawText(g, Text, secili ? Kalin : Yazi, new Rectangle(16, 0, Width - 20, Height),
            secili || fare ? Renk.Yazi : Renk.Soluk, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}

// Birkaç seçenekten biri: yan yana parçalar, seçili olan vurgu renginde (koyu temada beyaz açılır liste yerine).
// Fareye basınca ya da ←/→ ile seçilir.
class SecimSeridi : Control
{
    static readonly Font Yazi = new Font("Segoe UI", 9f, FontStyle.Bold);
    static readonly Color Zemin = Renk.Panel, Uzerinde = Renk.PanelUzerinde;

    readonly string[] secenekler;
    readonly int[] genislikler;
    int secim = -1, fareParca = -1;

    public event EventHandler SecimDegisti;

    public SecimSeridi(string[] secenekler)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        this.secenekler = secenekler;
        genislikler = secenekler.Select(s => TextRenderer.MeasureText(s, Yazi).Width + 22).ToArray();
        Size = new Size(genislikler.Sum() + 2, 30);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public int SelectedIndex
    {
        get { return secim; }
        set
        {
            if (value < 0 || value >= secenekler.Length || value == secim) return;
            secim = value;
            Invalidate();
            var h = SecimDegisti;
            if (h != null) h(this, EventArgs.Empty);
        }
    }

    int Parca(int x)
    {
        int sol = 1;
        for (int i = 0; i < genislikler.Length; i++) { if (x >= sol && x < sol + genislikler[i]) return i; sol += genislikler[i]; }
        return -1;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        int p = Parca(e.X);
        if (p >= 0) SelectedIndex = p;
    }

    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int p = Parca(e.X); if (p != fareParca) { fareParca = p; Invalidate(); } }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); fareParca = -1; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override bool IsInputKey(Keys k) { return k == Keys.Left || k == Keys.Right || base.IsInputKey(k); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Left && secim > 0) SelectedIndex = secim - 1;
        else if (e.KeyCode == Keys.Right && secim < secenekler.Length - 1) SelectedIndex = secim + 1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        using (var b = new SolidBrush(Zemin)) g.FillRectangle(b, 0, 0, Width - 1, Height - 1);
        int sol = 1;
        for (int i = 0; i < secenekler.Length; i++)
        {
            var r = new Rectangle(sol, 1, genislikler[i], Height - 3);
            if (i == secim) using (var b = new SolidBrush(Renk.Vurgu)) g.FillRectangle(b, r);
            else if (i == fareParca) using (var b = new SolidBrush(Uzerinde)) g.FillRectangle(b, r);
            if (i > 0 && i != secim && i - 1 != secim) using (var p = new Pen(Renk.Cizgi)) g.DrawLine(p, sol, 7, sol, Height - 9);
            TextRenderer.DrawText(g, secenekler[i], Yazi, r, i == secim ? Color.White : i == fareParca ? Renk.Yazi : Renk.Soluk,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            sol += genislikler[i];
        }
        using (var p = new Pen(Focused && ShowFocusCues ? Renk.Yazi : Renk.Cizgi)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
    }
}

class AyarSekmesi : Panel
{
    static readonly Font BaslikYazi = new Font("Segoe UI", 15f, FontStyle.Bold);
    static readonly Font SatirYazi = new Font("Segoe UI", 10f, FontStyle.Bold);
    static readonly Font AciklamaYazi = new Font("Segoe UI", 9f);
    static readonly Font DugmeYazi = new Font("Segoe UI", 9.5f, FontStyle.Bold);

    readonly BellekBekcisi bekci;
    readonly List<AyarMenuOgesi> menu = new List<AyarMenuOgesi>();
    readonly List<Panel> sayfalar = new List<Panel>();
    readonly Label lblDurum = new Label();
    bool ayarlaniyor;

    // AnaForm kurar: sekmeye git ("Turbo" ...).
    public Action<string> Git;

    // Görünüm
    readonly List<Control> renkKutulari = new List<Control>();
    Label lblVurgu;
    readonly SecimSeridi cmbTepsi = new SecimSeridi(TepsiGostergesi.Adlar), cmbAcilis = new SecimSeridi(AcilisSekmesi.Adlar);
    readonly Panel tepsiOnizleme = new Panel { Size = new Size(46, 30), Margin = new Padding(0, 0, 10, 0) };
    readonly AnahtarDugmesi anHareket = new AnahtarDugmesi();

    // Arka plan ve bekçi
    readonly AnahtarDugmesi anArka = new AnahtarDugmesi(), anWindows = new AnahtarDugmesi(), anOyun = new AnahtarDugmesi();
    Label lblWindows;

    // Kısayol ve ses
    readonly Button btnKisayol = SatirDugmesi("");
    Label lblKisayol;
    bool tusBekleniyor;
    readonly TrackBar trkSes = new TrackBar();
    readonly Label lblSesDeger = new Label();
    readonly Button btnDene = SatirDugmesi("Dene");

    // Sistemi Optimize Et
    readonly AnahtarDugmesi anKapat = new AnahtarDugmesi(), anRam = new AnahtarDugmesi(), anDisk = new AnahtarDugmesi();
    Label lblProfil;

    // Güncelleme
    readonly AnahtarDugmesi anGuncelle = new AnahtarDugmesi();
    readonly Button btnDenetle = SatirDugmesi("Şimdi denetle");
    Label lblDenetim;
    bool denetliyor;

    // Veriler ve yardım
    readonly Button btnSifirla = SatirDugmesi("Sıfırla");
    Label lblKazanc;

    public AyarSekmesi(BellekBekcisi bekci)
    {
        this.bekci = bekci;
        Dock = DockStyle.Fill;
        BackColor = Renk.Arka;

        // Sağ taraf: sayfalar ve altta durum satırı.
        var icerik = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 16, 4) };
        lblDurum.Dock = DockStyle.Bottom;
        lblDurum.Height = 34;
        lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        lblDurum.ForeColor = Renk.Soluk;
        lblDurum.AutoEllipsis = true;

        var bolumler = new[]
        {
            Tuple.Create("Görünüm", GorunumSayfasi()),
            Tuple.Create("Arka plan ve bekçi", BekciSayfasi()),
            Tuple.Create("Kısayol ve ses", KisayolSayfasi()),
            Tuple.Create("Sistemi Optimize Et", HazirlaSayfasi()),
            Tuple.Create("Güncelleme", GuncellemeSayfasi()),
            Tuple.Create("Veriler ve yardım", DigerSayfasi())
        };
        foreach (var b in bolumler) { sayfalar.Add(b.Item2); icerik.Controls.Add(b.Item2); }
        icerik.Controls.Add(lblDurum);

        // Sol taraf: başlık ve bölüm listesi.
        var sol = new Panel { Dock = DockStyle.Left, Width = 214, Padding = new Padding(12, 14, 10, 12) };
        var baslik = new Panel { Dock = DockStyle.Top, Height = 50 };
        baslik.Paint += delegate(object s, PaintEventArgs e)
        {
            Cark.Ciz(e.Graphics, new PointF(26, 22), 20, Renk.Yazi);
            TextRenderer.DrawText(e.Graphics, "Ayarlar", BaslikYazi, new Point(44, 7), Renk.Yazi);
        };
        var not = new Label
        {
            Dock = DockStyle.Bottom, Height = 44, ForeColor = Renk.Soluk, Font = AciklamaYazi, Padding = new Padding(4, 0, 0, 0),
            Text = "Değişiklikler hemen uygulanır ve kaydedilir."
        };
        for (int i = bolumler.Length - 1; i >= 0; i--)
        {
            int n = i;
            var oge = new AyarMenuOgesi(bolumler[i].Item1);
            oge.Secildi += delegate { Sec(n); };
            menu.Insert(0, oge);
            sol.Controls.Add(oge);
        }
        sol.Controls.Add(baslik);
        sol.Controls.Add(not);
        var cizgi = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Renk.Cizgi };

        Controls.Add(icerik);
        Controls.Add(cizgi);
        Controls.Add(sol);

        Kazanc.Degisti += KazanciYaz;
        Disposed += delegate { Kazanc.Degisti -= KazanciYaz; };
        Sec(0);
        Tazele();
    }

    public void Gosterildi()
    {
        Tazele();
        lblDurum.Text = "";
    }

    void Sec(int n)
    {
        for (int i = 0; i < sayfalar.Count; i++)
        {
            sayfalar[i].Visible = i == n;
            menu[i].Secili = i == n;
        }
    }

    void Tamam(string metin) { lblDurum.ForeColor = Renk.Soluk; lblDurum.Text = metin; }
    void Olmadi(string metin) { lblDurum.ForeColor = Renk.Turuncu; lblDurum.Text = metin; }

    // ---- Kalıplar ----

    // Başlık, açıklama ve satırlar; satırlar yukarıdan aşağı verilen sırayla.
    Panel Sayfa(string baslik, string alt, params Control[] satirlar)
    {
        var s = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false, Padding = new Padding(0, 0, 6, 0) };
        for (int i = satirlar.Length - 1; i >= 0; i--) s.Controls.Add(satirlar[i]);
        s.Controls.Add(new Label { Dock = DockStyle.Top, Height = 34, ForeColor = Renk.Soluk, Text = alt, AutoEllipsis = true });
        s.Controls.Add(new Label { Dock = DockStyle.Top, Height = 34, Font = BaslikYazi, ForeColor = Renk.Yazi, Text = baslik });
        return s;
    }

    static Panel Satir(string baslik, string aciklama, Control sag)
    {
        Label a;
        return Satir(baslik, aciklama, sag, out a);
    }

    // Panel zeminli satır: solda kalın başlık ve soluk açıklama (iki satıra kadar), sağda dikey ortalı denetim.
    static Panel Satir(string baslik, string aciklama, Control sag, out Label lblAciklama)
    {
        var dis = new Panel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(0, 0, 0, 6) };
        var ic = new Panel { Dock = DockStyle.Fill, BackColor = Renk.Panel };
        var b = new Label { Text = baslik, Font = SatirYazi, ForeColor = Renk.Yazi, AutoEllipsis = true, Left = 16, Top = 12, Height = 22 };
        var a = new Label { Text = aciklama, Font = AciklamaYazi, ForeColor = Renk.Soluk, AutoEllipsis = true, Left = 16, Top = 35, Height = 32 };
        ic.Controls.Add(b);
        ic.Controls.Add(a);
        if (sag != null) ic.Controls.Add(sag);
        EventHandler yerlestir = delegate
        {
            int sagPay = sag == null ? 0 : sag.Width + 28;
            b.Width = a.Width = Math.Max(80, ic.ClientSize.Width - 32 - sagPay);
            if (sag != null) { sag.Left = ic.ClientSize.Width - 16 - sag.Width; sag.Top = (ic.ClientSize.Height - sag.Height) / 2; }
        };
        ic.Resize += yerlestir;
        if (sag != null) sag.Resize += yerlestir;
        dis.Controls.Add(ic);
        lblAciklama = a;
        return dis;
    }

    static FlowLayoutPanel Yan(params Control[] denetimler)
    {
        var f = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        foreach (var d in denetimler) f.Controls.Add(d);
        return f;
    }

    static Button SatirDugmesi(string metin)
    {
        var b = new Button
        {
            Text = metin, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, AutoSize = true, MinimumSize = new Size(104, 30), Height = 30,
            BackColor = Renk.Arka, ForeColor = Renk.Yazi, Font = DugmeYazi, Margin = Padding.Empty
        };
        b.FlatAppearance.BorderColor = Renk.Cizgi;
        return b;
    }

    // ---- Görünüm ----

    Panel GorunumSayfasi()
    {
        var renkler = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        for (int i = 0; i < Renk.Vurgular.Length; i++)
        {
            int n = i;
            // Renk zemine değil çizime verilir: vurgu değişince zemin rengi eşleşen her şey yeniden boyanıyor (Renk.VurguSec).
            var k = new Panel { Size = new Size(32, 32), Margin = new Padding(i == 0 ? 0 : 6, 0, 0, 0), Cursor = Cursors.Hand };
            k.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(Renk.Vurgular[n])) e.Graphics.FillEllipse(b, 5, 5, 21, 21);
                if (Renk.VurguSirasi == n) using (var p = new Pen(Renk.Yazi, 2f)) e.Graphics.DrawEllipse(p, 2, 2, 27, 27);
            };
            k.Click += delegate
            {
                Renk.VurguSec(n);
                VurguyuYaz();
                Tamam("Tamam: vurgu rengi " + Renk.VurguAdlari[n].ToLower() + ".");
            };
            new ToolTip().SetToolTip(k, Renk.VurguAdlari[n]);
            renkKutulari.Add(k);
            renkler.Controls.Add(k);
        }

        tepsiOnizleme.Paint += delegate(object s, PaintEventArgs e) { TepsiOnizle(e.Graphics); };
        new ToolTip().SetToolTip(tepsiOnizleme, "Saatin yanında böyle görünür");
        cmbTepsi.SecimDegisti += delegate
        {
            if (ayarlaniyor || cmbTepsi.SelectedIndex < 0) return;
            TepsiGostergesi.Secim = cmbTepsi.SelectedIndex;
            if (bekci != null) bekci.SimgeyiTazele();
            tepsiOnizleme.Invalidate();
            Tamam("Tamam: tepsi simgesinde " + (cmbTepsi.SelectedIndex == 0 ? "logo görünür." : TepsiGostergesi.Adlar[cmbTepsi.SelectedIndex] + " yazar."));
        };
        anHareket.CheckedChanged += delegate
        {
            if (ayarlaniyor) return;
            Hareket.Acik = anHareket.Checked;
            Tamam(anHareket.Checked ? "Tamam: göstergeler ve sayılar kayarak değişir." : "Tamam: değerler hareketsiz, hemen yerine oturur.");
        };
        cmbAcilis.SecimDegisti += delegate
        {
            if (ayarlaniyor || cmbAcilis.SelectedIndex < 0) return;
            AcilisSekmesi.Secim = cmbAcilis.SelectedIndex;
            Tamam("Tamam: pencere açılınca " + (cmbAcilis.SelectedIndex == 2 ? "en son açık kalan sekme" : AcilisSekmesi.Adlar[cmbAcilis.SelectedIndex] + " sekmesi") + " gösterilir.");
        };

        var vurgu = Satir("Vurgu rengi", "", renkler, out lblVurgu);
        return Sayfa("Görünüm", "Programın rengi, saatin yanındaki simge ve açılış sekmesi.",
            vurgu,
            Satir("Tepsi simgesinde", "Saatin yanındaki simgede ne yazsın. Rengi doluluğa göre yeşil, turuncu, kırmızı olur; üstüne gelince küçük bir özet açılır.",
                Yan(tepsiOnizleme, cmbTepsi)),
            Satir("Hareketli geçişler", "Göstergeler kayarak değişir, sistem puanı ve kazançlar sayarak gelir. Kapatırsan değerler hemen yerine oturur.", anHareket),
            Satir("Açılınca gösterilecek sekme", "Pencere her açıldığında bu sekmeyle başlar. \"Son açık sekme\": en son hangisinde kaldıysan.", cmbAcilis));
    }

    void VurguyuYaz()
    {
        foreach (var k in renkKutulari) k.Invalidate();
        if (lblVurgu != null)
            lblVurgu.Text = "Ana düğmeler, seçili bölüm çizgisi ve anahtarlar bu renkte. Şu an: " + Renk.VurguAdlari[Renk.VurguSirasi] + ".";
    }

    // Görev çubuğuna benzeyen küçük zemin üstünde simgenin şu anki hâli.
    void TepsiOnizle(Graphics g)
    {
        g.Clear(tepsiOnizleme.BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(Color.FromArgb(32, 33, 38))) g.FillRectangle(b, 0, 0, tepsiOnizleme.Width, tepsiOnizleme.Height);
        int secim = TepsiGostergesi.Secim, deger = bekci != null ? bekci.SimgeDegeri(secim) : -1;
        var yer = new Rectangle((tepsiOnizleme.Width - 22) / 2, (tepsiOnizleme.Height - 22) / 2, 22, 22);
        if (deger < 0)
        {
            var logo = Logo.Resim();
            if (logo != null) g.DrawImage(logo, yer);
            return;
        }
        var simge = TepsiGostergesi.Sayi(deger, yer.Size);
        try { g.DrawIcon(simge, yer); }
        finally { TepsiGostergesi.Birak(simge); }
    }

    // ---- Arka plan ve bekçi ----

    Panel BekciSayfasi()
    {
        anArka.CheckedChanged += delegate { ArkaPlanDegisti(); };
        anWindows.CheckedChanged += delegate { WindowsDegisti(); };
        anOyun.CheckedChanged += delegate
        {
            if (ayarlaniyor || bekci == null) return;
            bekci.OyunModu = anOyun.Checked;
            Tamam(anOyun.Checked
                ? "Tamam: tam ekran oyun sırasında pencere açılmaz, bellek uyarıları sesle de gelir."
                : "Tamam: oyun modu kapalı; uyarılar yalnızca balonla gelir.");
        };
        return Sayfa("Arka plan ve bekçi", "Pencere kapalıyken de bellek bekçisi saatin yanında çalışır.",
            Satir("Pencereyi kapatınca arka planda çalış",
                "Saatin yanında simge kalır; sanal bellek dolmak üzereyken ya da bir program durmadan büyürken haber verir. Tamamen kapatmak: simgeye sağ tık → Çıkış.", anArka),
            Satir("Windows açılınca başlat", "", anWindows, out lblWindows),
            Satir("Oyun modu",
                "Önde tam ekran oyun varken pencere açılmaz, bellek uyarıları sesle de gelir (Windows oyunda balonları çoğu zaman gizler).", anOyun));
    }

    void BekciyiYaz()
    {
        if (bekci == null)
        {
            anArka.Checked = anWindows.Checked = anOyun.Checked = false;
            anArka.Enabled = anWindows.Enabled = anOyun.Enabled = false;
            return;
        }
        anOyun.Checked = bekci.OyunModu;
        bool arka = false, windows = false;
        try { arka = bekci.ArkaPlandaCalis; windows = bekci.WindowsIleBaslat; } catch { }
        anArka.Checked = arka;
        anWindows.Checked = arka && windows;
        anWindows.Enabled = arka;
        lblWindows.Text = arka
            ? "Windows açılınca bekçi pencere açmadan, saatin yanında başlar."
            : "Önce \"arka planda çalış\"ı aç; arka planda çalışmayan bekçi Windows ile başlayamaz.";
    }

    void ArkaPlanDegisti()
    {
        if (ayarlaniyor || bekci == null) return;
        try
        {
            bekci.ArkaPlandaCalis = anArka.Checked;
            // Arka planda çalışmayan bekçinin Windows ile açılmasının anlamı yok: o da kapanır.
            if (!anArka.Checked && bekci.WindowsIleBaslat) bekci.WindowsIleBaslat = false;
            Tamam(anArka.Checked
                ? "Tamam: pencereyi kapatınca program saatin yanında çalışmaya devam eder."
                : "Tamam: pencereyi kapatınca program tamamen kapanır, bekçi de durur.");
        }
        catch (Exception e) { Olmadi("Ayar kaydedilemedi: " + e.Message); }
        Yeniden(BekciyiYaz);
    }

    void WindowsDegisti()
    {
        if (ayarlaniyor || bekci == null) return;
        try
        {
            bool istenen = anWindows.Checked && anArka.Checked;
            bekci.WindowsIleBaslat = istenen;
            // Setter hataları yutar: yazılıp yazılmadığı kayıt defterinden yeniden okunur.
            bool olan = bekci.WindowsIleBaslat;
            if (olan != istenen) Olmadi("Ayar kaydedilemedi (güvenlik programı engellemiş olabilir).");
            else Tamam(olan ? "Tamam: Windows açılınca bekçi pencere açmadan, saatin yanında başlar." : "Tamam: Windows açılınca bekçi kendiliğinden başlamaz.");
        }
        catch (Exception e) { Olmadi("Ayar kaydedilemedi: " + e.Message); }
        Yeniden(BekciyiYaz);
    }

    // Denetimleri ayardan yeniden doldurur; olay işleyicileri bu sırada bir şey yazmaz.
    void Yeniden(Action doldur)
    {
        bool onceki = ayarlaniyor;
        ayarlaniyor = true;
        try { doldur(); }
        finally { ayarlaniyor = onceki; }
    }

    // ---- Kısayol ve ses ----

    Panel KisayolSayfasi()
    {
        btnKisayol.MinimumSize = new Size(130, 30);
        btnKisayol.Click += delegate { TusBekle(); };
        btnKisayol.PreviewKeyDown += delegate(object s, PreviewKeyDownEventArgs e) { if (tusBekleniyor) e.IsInputKey = true; };
        btnKisayol.KeyDown += KisayolTusu;
        btnKisayol.Leave += delegate { if (tusBekleniyor) TusBeklemeyiBitir(null); };

        trkSes.Minimum = 0;
        trkSes.Maximum = 100;
        trkSes.SmallChange = 5;
        trkSes.LargeChange = 10;
        trkSes.TickStyle = TickStyle.None;
        trkSes.AutoSize = false;
        trkSes.Size = new Size(160, 30);
        trkSes.BackColor = Renk.Panel;
        trkSes.Margin = new Padding(0, 2, 0, 0);
        trkSes.ValueChanged += delegate
        {
            SesDegeriniYaz();
            if (!ayarlaniyor) Ses.Duzey = trkSes.Value;
        };
        lblSesDeger.AutoSize = false;
        lblSesDeger.Size = new Size(54, 30);
        lblSesDeger.TextAlign = ContentAlignment.MiddleLeft;
        lblSesDeger.ForeColor = Renk.Soluk;
        lblSesDeger.Margin = Padding.Empty;
        btnDene.MinimumSize = new Size(70, 30);
        btnDene.Click += delegate { Ses.Cal(SesTuru.Tamam, trkSes.Value); };

        return Sayfa("Kısayol ve ses", "Oyundan çıkmadan RAM boşaltma ve iş bitince çalan ses.",
            Satir("RAM boşaltma kısayolu", "", btnKisayol, out lblKisayol),
            Satir("Bildirim sesi", "İş bitince ve bellek uyarılarında çalan kısa ses. En sola çekersen kapanır.", Yan(trkSes, lblSesDeger, btnDene)));
    }

    void SesDegeriniYaz()
    {
        lblSesDeger.Text = trkSes.Value == 0 ? "kapalı" : "%" + trkSes.Value;
        btnDene.Enabled = trkSes.Value > 0;
    }

    void KisayolunuYaz()
    {
        if (tusBekleniyor) return;
        Keys k = bekci != null ? bekci.Kisayol : Keys.None;
        btnKisayol.Text = KisayolAyari.Metin(k);
        if (bekci == null)
        {
            btnKisayol.Enabled = false;
            lblKisayol.ForeColor = Renk.Soluk;
            lblKisayol.Text = "Bekçi çalışmıyor.";
        }
        else if (!bekci.KisayolCalisiyor)
        {
            lblKisayol.ForeColor = Renk.Turuncu;
            lblKisayol.Text = "Bu tuşları başka bir program kullanıyor. Düğmeye tıkla, başka tuşlar seç.";
        }
        else
        {
            lblKisayol.ForeColor = Renk.Soluk;
            lblKisayol.Text = k == Keys.None
                ? "Kapalı. Düğmeye tıkla ve Ctrl ya da Alt ile birlikte bir tuşa bas."
                : "Her yerde, oyunda da RAM'i boşaltır; bitince kısa bir ses çalar. Değiştirmek için düğmeye tıkla.";
        }
    }

    // Kısayol düğmesine tıklandı: eski kısayol geçici olarak bırakılır (yoksa yakalarken RAM boşaltma başlar).
    void TusBekle()
    {
        if (bekci == null || tusBekleniyor) return;
        tusBekleniyor = true;
        bekci.KisayoluBirak();
        btnKisayol.Text = "Tuşlara bas…";
        btnKisayol.ForeColor = Renk.Turuncu;
        lblKisayol.ForeColor = Renk.Soluk;
        lblKisayol.Text = "Ctrl ya da Alt ile birlikte bir tuşa bas. Esc: vazgeç · Delete: kısayolu kapat.";
        btnKisayol.Focus();
    }

    void KisayolTusu(object s, KeyEventArgs e)
    {
        if (!tusBekleniyor) return;
        e.Handled = e.SuppressKeyPress = true;
        Keys tus = e.KeyCode;
        if (tus == Keys.ControlKey || tus == Keys.Menu || tus == Keys.ShiftKey || tus == Keys.LWin || tus == Keys.RWin)
        {
            // Yalnızca değiştirici basılı: o ana kadar basılanlar gösterilir.
            string on = (e.Control ? "Ctrl+" : "") + (e.Alt ? "Alt+" : "") + (e.Shift ? "Shift+" : "");
            btnKisayol.Text = on.Length > 0 ? on + "…" : "Tuşlara bas…";
            return;
        }
        if (tus == Keys.Escape && e.Modifiers == Keys.None) { TusBeklemeyiBitir(null); return; }
        if ((tus == Keys.Delete || tus == Keys.Back) && e.Modifiers == Keys.None) { TusBeklemeyiBitir(Keys.None); return; }
        if (!KisayolAyari.Gecerli(e.KeyData))
        {
            btnKisayol.Text = "Tuşlara bas…";
            lblKisayol.ForeColor = Renk.Turuncu;
            lblKisayol.Text = "Ctrl ya da Alt ile birlikte bas.";
            return;
        }
        TusBeklemeyiBitir(e.KeyData);
    }

    // yeni == null: vazgeçildi, eski kısayol geri alınır.
    void TusBeklemeyiBitir(Keys? yeni)
    {
        tusBekleniyor = false;
        btnKisayol.ForeColor = Renk.Yazi;
        if (bekci == null) return;
        if (yeni == null)
        {
            bekci.KisayoluGeriAl();
            KisayolunuYaz();
            return;
        }
        bool oldu = bekci.KisayolAyarla(yeni.Value);
        KisayolunuYaz();
        if (oldu)
            Tamam(yeni.Value == Keys.None
                ? "Tamam: kısayol kapatıldı. RAM'i tepsi simgesine sağ tık → \"RAM boşalt\" ile de boşaltabilirsin."
                : "Tamam: " + KisayolAyari.Metin(yeni.Value) + " her yerde (oyunda da) RAM'i boşaltır.");
        else
        {
            lblKisayol.ForeColor = Renk.Turuncu;
            lblKisayol.Text = KisayolAyari.Metin(yeni.Value) + " başka bir programda. Düğmeye tıkla, başka tuşlar seç.";
        }
    }

    // ---- Sistemi Optimize Et ----

    Panel HazirlaSayfasi()
    {
        anKapat.CheckedChanged += delegate { HazirlikYaz(OyunaHazirlaFormu.AyarKapat, anKapat, "arka plan programları kapatılır", "arka plan programları kapatılmaz"); };
        anRam.CheckedChanged += delegate { HazirlikYaz(OyunaHazirlaFormu.AyarRam, anRam, "RAM boşaltılır", "RAM boşaltılmaz"); };
        anDisk.CheckedChanged += delegate { HazirlikYaz(OyunaHazirlaFormu.AyarDisk, anDisk, "geçici dosyalar temizlenir", "geçici dosyalara dokunulmaz"); };
        var btnTurbo = SatirDugmesi("Turbo'ya git");
        btnTurbo.Click += delegate { if (Git != null) Git("Turbo"); };
        return Sayfa("Sistemi Optimize Et", "\"Sistemi Optimize Et\" penceresi bu seçimlerle açılır; orada da tek tek değiştirebilirsin.",
            Satir("Arka plan programlarını kapat", "Sistem sekmesinde işaretli programlar kapatılır. Kaydedilmemiş işi olan pencere kapanmaz.", anKapat),
            Satir("RAM'i boşalt", "Programların o an kullanmadığı belleği geri alır. Oyun açıksa ona dokunulmaz.", anRam),
            Satir("Geçici dosyaları temizle", "Yönetici izni istemeyen, risksiz temizlik (Otomatik optimize ile aynı).", anDisk),
            Satir("Oyun profili", "", btnTurbo, out lblProfil));
    }

    void HazirlikYaz(string ayar, AnahtarDugmesi a, string acik, string kapali)
    {
        if (ayarlaniyor) return;
        Ayarlar.Yaz(ayar, a.Checked);
        Tamam("Tamam: sistem optimizasyonunda " + (a.Checked ? acik : kapali) + ".");
    }

    void HazirligiYaz()
    {
        anKapat.Checked = Ayarlar.Oku(OyunaHazirlaFormu.AyarKapat, true);
        anRam.Checked = Ayarlar.Oku(OyunaHazirlaFormu.AyarRam, true);
        anDisk.Checked = Ayarlar.Oku(OyunaHazirlaFormu.AyarDisk, true);
        bool profil = OyunProfili.Acik;
        lblProfil.ForeColor = profil ? Renk.Yesil : Renk.Soluk;
        lblProfil.Text = profil
            ? "Açık: tam ekran oyun açılınca güç modu En iyi performans olur, oyun kapanınca eski hâline döner."
            : "Kapalı. Açarsan oyun açılınca güç modu En iyi performans olur; Turbo sekmesinden açılır.";
    }

    // ---- Güncelleme ----

    Panel GuncellemeSayfasi()
    {
        anGuncelle.CheckedChanged += delegate
        {
            if (ayarlaniyor) return;
            Ayarlar.Yaz(Guncelleyici.AyarDenetle, anGuncelle.Checked);
            Tamam(anGuncelle.Checked
                ? "Tamam: günde bir kez yeni sürüme bakılır; yeni sürüm varsa sorulur."
                : "Tamam: kendiliğinden bakılmaz; istediğinde \"Şimdi denetle\"ye bas.");
        };
        btnDenetle.Click += delegate { Denetle(); };
        return Sayfa("Güncelleme", "Yeni sürüm GitHub'dan iner; imzası doğrulanmadan kurulmaz.",
            Satir("Güncellemeleri kendiliğinden denetle", "Günde en çok bir kez bakar. Yeni sürüm varsa sorar; sormadan kurmaz, oyun sırasında sormaz.", anGuncelle),
            Satir("Sürüm " + Surum.Metin, "", btnDenetle, out lblDenetim));
    }

    void DenetimiYaz()
    {
        var son = Guncelleyici.SonDenetim;
        lblDenetim.ForeColor = Renk.Soluk;
        lblDenetim.Text = "Son denetim: " + (son == null ? "henüz yapılmadı" : son.Value.ToString("d MMMM yyyy HH:mm")) + ".";
    }

    async void Denetle()
    {
        var g = Guncelleyici.Ornek;
        if (denetliyor || g == null) return;
        denetliyor = true;
        btnDenetle.Enabled = false;
        lblDenetim.ForeColor = Renk.Soluk;
        lblDenetim.Text = "Denetleniyor...";
        DenetimYaniti y;
        try { y = await g.SimdiDenetle(); }
        catch (Exception e) { y = new DenetimYaniti { Sonuc = DenetimSonucu.Hata, Hata = GuncellemeMotoru.KisaNeden(e) }; }
        denetliyor = false;
        if (IsDisposed) return;
        btnDenetle.Enabled = true;
        DenetimiYaz();
        switch (y.Sonuc)
        {
            case DenetimSonucu.Guncel:
                lblDenetim.ForeColor = Renk.Yesil;
                lblDenetim.Text = "Güncelsin. " + lblDenetim.Text;
                break;
            case DenetimSonucu.Yeni:
                lblDenetim.ForeColor = Renk.Turuncu;
                lblDenetim.Text = "Yeni sürüm var: v" + Guncelleyici.SurumMetni(y.Bilgi.Surum) + ".";
                g.FormuAc(y.Bilgi, FindForm());
                break;
            case DenetimSonucu.Eksik:
                lblDenetim.ForeColor = Renk.Turuncu;
                lblDenetim.Text = "Yeni sürüm var ama dosyaları eksik; biraz sonra yeniden dene.";
                break;
            case DenetimSonucu.YayinYok:
                lblDenetim.ForeColor = Renk.Soluk;
                lblDenetim.Text = "Henüz GitHub yayını yok.";
                break;
            default:
                lblDenetim.ForeColor = Renk.Turuncu;
                lblDenetim.Text = "Denetlenemedi: " + y.Hata;
                break;
        }
    }

    // ---- Veriler ve yardım ----

    Panel DigerSayfasi()
    {
        btnSifirla.Click += delegate
        {
            if (MessageBox.Show(FindForm(), "Bugüne kadarki toplam kazanç (açılan yer, boşaltılan RAM, temizlik sayısı) sıfırlansın mı?\n\nHiçbir dosyaya dokunulmaz; yalnızca sayaç sıfırlanır.",
                    "Toplam kazanç", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            Kazanc.Sifirla();
            Tamam("Tamam: toplam kazanç sıfırlandı.");
        };
        var btnTanitim = SatirDugmesi("Göster");
        btnTanitim.Click += delegate { Tanitim.TuruGoster(FindForm(), bekci); };
        var btnKlasor = SatirDugmesi("Klasörü aç");
        btnKlasor.Click += delegate
        {
            try
            {
                Directory.CreateDirectory(Ayarlar.Klasor);
                Process.Start("explorer.exe", "\"" + Ayarlar.Klasor + "\"");
            }
            catch (Exception e) { Olmadi("Klasör açılamadı: " + e.Message); }
        };
        var btnHakkinda = SatirDugmesi("Hakkında");
        btnHakkinda.Click += delegate { Hakkinda.Goster(); };
        return Sayfa("Veriler ve yardım", "Toplam kazanç, tanıtım ve programın kendi dosyaları.",
            Satir("Toplam kazanç", "", btnSifirla, out lblKazanc),
            Satir("Tanıtım", "İlk açılıştaki kısa tanıtımı yeniden gösterir.", btnTanitim),
            Satir("Ayar dosyaları", "Ayarlar ve geri alma yedekleri bu klasörde durur (%APPDATA%\\YamanPcCleaner).", btnKlasor),
            Satir("Yaman PC Cleaner v" + Surum.Metin, "Özellikler, açık kaynak bağlantısı ve lisans.", btnHakkinda));
    }

    void KazanciYaz()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { try { BeginInvoke(new Action(KazanciYaz)); } catch { } return; }
        bool bos = Kazanc.IsSayisi == 0;
        lblKazanc.Text = bos
            ? "Henüz temizlik yapılmadı. Her temizlikten sonra açılan yer ve boşaltılan RAM burada birikir."
            : string.Format("Diskte {0} yer açıldı, RAM'de {1} boşaltıldı · {2} temizlik. Sıfırlamak yalnızca sayacı siler.",
                AnaForm.Boyut(Kazanc.DiskBayt), AnaForm.Boyut(Kazanc.RamBayt), Kazanc.IsSayisi.ToString("N0"));
        btnSifirla.Enabled = !bos;
    }

    // ---- Hepsi ----

    void Tazele()
    {
        Yeniden(delegate
        {
            VurguyuYaz();
            cmbTepsi.SelectedIndex = TepsiGostergesi.Secim;
            tepsiOnizleme.Invalidate();
            anHareket.Checked = Hareket.Acik;
            cmbAcilis.SelectedIndex = AcilisSekmesi.Secim;
            BekciyiYaz();
            trkSes.Value = Ses.Duzey;
            SesDegeriniYaz();
            KisayolunuYaz();
            HazirligiYaz();
            anGuncelle.Checked = Guncelleyici.Acik;
            if (!denetliyor) DenetimiYaz();
            KazanciYaz();
        });
    }
}
