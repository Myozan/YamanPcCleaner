// Canli gosterge kutusu.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

// MSI Afterburner benzeri tek göstergelik kutu: başlık, alt yazı, değer ve renkli çubuk.
// Canlı gösterge: başlık, değer, son 2 dakikanın grafiği ve doluluk çubuğu. Çubuk yeni değere kayarak gider
// (tek ortak zamanlayıcı; yalnızca hareket eden göstergeler için çalışır).
class Gosterge : Control
{
    static readonly Font BaslikYazi = new Font("Segoe UI", 8.5f, FontStyle.Bold);
    static readonly Font AltYazi = new Font("Segoe UI", 8f);
    static readonly Font DegerYazi = new Font("Segoe UI Semibold", 14f);
    const int GecmisBoyu = 60;   // 2 sn'lik ölçümle son 2 dakika

    static readonly List<Gosterge> kayanlar = new List<Gosterge>();
    static System.Windows.Forms.Timer kaydirici;

    string deger = "–";
    double oran, gorunen;
    readonly double[] gecmis = new double[GecmisBoyu];
    int gecmisSayisi, gecmisBasi;
    public string Alt;

    public Gosterge(string baslik, string alt)
    {
        Text = baslik;
        Alt = alt;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Renk.Arka;
        Dock = DockStyle.Fill;
        Margin = new Padding(5);
    }

    // Her ölçümde bir kez çağrılır: değer yazısı hemen, çubuk kayarak değişir, grafik bir adım ilerler.
    public void Ayarla(string d, double o)
    {
        if (double.IsNaN(o) || double.IsInfinity(o)) o = 0;
        o = Math.Max(0, Math.Min(1, o));
        gecmis[(gecmisBasi + gecmisSayisi) % GecmisBoyu] = o;
        if (gecmisSayisi < GecmisBoyu) gecmisSayisi++; else gecmisBasi = (gecmisBasi + 1) % GecmisBoyu;
        bool ilk = deger == "–" && gorunen == 0;
        deger = d; oran = o;
        if (ilk || !Visible || !Hareket.Acik) gorunen = o;
        else if (Math.Abs(o - gorunen) > 0.002) Kaydir();
        Invalidate();
    }

    void Kaydir()
    {
        if (!kayanlar.Contains(this)) kayanlar.Add(this);
        if (kaydirici == null)
        {
            kaydirici = new System.Windows.Forms.Timer { Interval = 16 };
            kaydirici.Tick += delegate
            {
                foreach (var gs in kayanlar.ToList())
                {
                    double fark = gs.oran - gs.gorunen;
                    if (Math.Abs(fark) < 0.003 || gs.IsDisposed) { gs.gorunen = gs.oran; kayanlar.Remove(gs); }
                    else gs.gorunen += fark * 0.18;
                    if (!gs.IsDisposed) gs.Invalidate();
                }
                if (kayanlar.Count == 0) kaydirici.Stop();
            };
        }
        if (!kaydirici.Enabled) kaydirici.Start();
    }

    static Color Seviye(double o) { return o >= 0.9 ? Renk.Kirmizi : o >= 0.7 ? Renk.Turuncu : Renk.Yesil; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent != null ? Parent.BackColor : BackColor);
        Tasarim.Kart(g, new Rectangle(0, 0, Width - 1, Height - 1), Renk.Panel, Renk.Cizgi, Tasarim.KartYaricap);
        int x = 16, w = Width - 32;
        int dg = TextRenderer.MeasureText(deger, DegerYazi).Width;
        var sol = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        // Grafik: yazıların altındaki şeritte, çubuğun hemen üstünde; en sağdaki nokta en yeni ölçüm.
        var alan = new Rectangle(x, 48, w, Height - 48 - 20);
        if (gecmisSayisi >= 2 && alan.Height >= 8)
        {
            var noktalar = new List<PointF>();
            float adim = (float)alan.Width / (GecmisBoyu - 1);
            for (int i = 0; i < gecmisSayisi; i++)
            {
                double v = gecmis[(gecmisBasi + i) % GecmisBoyu];
                noktalar.Add(new PointF(alan.Right - (gecmisSayisi - 1 - i) * adim, alan.Bottom - (float)(v * alan.Height)));
            }
            var renk = Seviye(oran);
            var eski = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var dolgu = new List<PointF>(noktalar) { new PointF(noktalar[noktalar.Count - 1].X, alan.Bottom), new PointF(noktalar[0].X, alan.Bottom) };
            using (var b = new SolidBrush(Color.FromArgb(38, renk))) g.FillPolygon(b, dolgu.ToArray());
            using (var p = new Pen(Color.FromArgb(150, renk), 1.5f)) g.DrawLines(p, noktalar.ToArray());
            g.SmoothingMode = eski;
        }

        TextRenderer.DrawText(g, Text, BaslikYazi, new Rectangle(x, 11, w - dg - 6, 18), Renk.Soluk, sol);
        if (!string.IsNullOrEmpty(Alt))
            TextRenderer.DrawText(g, Alt, AltYazi, new Rectangle(x, 29, w - dg - 6, 16), Renk.Soluk, sol);
        TextRenderer.DrawText(g, deger, DegerYazi, new Rectangle(x, 8, w, 34), Renk.Yazi,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var cubuk = new Rectangle(x, Height - 16, w, 6);
        using (var yol = Tasarim.Yuvarlak(cubuk, 3))
        using (var b = new SolidBrush(Renk.Cizgi)) g.FillPath(b, yol);
        int dolu = Math.Max(0, Math.Min(cubuk.Width, (int)(cubuk.Width * gorunen)));
        if (dolu > 0)
        {
            var parca = new Rectangle(cubuk.X, cubuk.Y, Math.Max(3, dolu), cubuk.Height);
            var renk = Seviye(gorunen);
            using (var yol = Tasarim.Yuvarlak(parca, 3))
            using (var b = new LinearGradientBrush(parca, Tasarim.Karistir(renk, Color.White, 0.18), renk, 0f))
                g.FillPath(b, yol);
        }
    }
}
