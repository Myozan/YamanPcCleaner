// Yaman PC Cleaner · logo ve uygulama ikonu üreticisi.
// Dış görsel servisine ihtiyaç duymaz: rozet ve "Y" monogramı GDI+ ile vektörel çizilir,
// ardından çok boyutlu .ico ve arayüzde kullanılan küçük PNG üretilir.
// Kullanım: LogoUretici.exe <cikti-klasoru>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

static class LogoUretici
{
    const int Tuval = 1024;

    static int Main(string[] args)
    {
        string cikti = args.Length > 0 ? args[0] : "kaynaklar";
        Directory.CreateDirectory(cikti);

        var logo = Ciz(Tuval);
        logo.Save(Path.Combine(cikti, "logo-kaynak.png"), ImageFormat.Png);
        Olcekle(logo, 512).Save(Path.Combine(cikti, "logo-512.png"), ImageFormat.Png);
        Olcekle(logo, 256).Save(Path.Combine(cikti, "logo-256.png"), ImageFormat.Png);

        var boyutlar = new[] { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };
        var pngler = new Dictionary<int, byte[]>();
        foreach (var s in boyutlar)
            using (var ms = new MemoryStream()) { Olcekle(logo, s).Save(ms, ImageFormat.Png); pngler[s] = ms.ToArray(); }

        IcoYaz(Path.Combine(cikti, "YamanPcCleaner.ico"), boyutlar, pngler);
        IcoYaz(Path.Combine(cikti, "YamanPcCleaner-kucuk.ico"), new[] { 16, 20, 24, 32, 40, 48, 64, 96 }, pngler);
        File.WriteAllBytes(Path.Combine(cikti, "logo-kucuk.png"), pngler[96]);

        // Önizleme: koyu ve açık zeminde küçük boyutlar okunuyor mu?
        foreach (var zemin in new[] { Color.FromArgb(10, 16, 28), Color.White, Color.FromArgb(59, 130, 246) })
        {
            var onizleme = new Bitmap(16 + 24 + 32 + 48 + 64 + 128 + 256 + 8 * 9, 272);
            using (var g = Graphics.FromImage(onizleme))
            {
                g.Clear(zemin);
                int x = 8;
                foreach (var s in new[] { 16, 24, 32, 48, 64, 128, 256 })
                {
                    g.DrawImage(Olcekle(logo, s), x, 8 + (256 - s) / 2, s, s);
                    x += s + 8;
                }
            }
            onizleme.Save(Path.Combine(cikti, "onizleme-" + zemin.R + ".png"), ImageFormat.Png);
        }
        Console.WriteLine("Logo üretildi: " + Path.GetFullPath(cikti));
        return 0;
    }

    static Bitmap Ciz(int boyut)
    {
        var b = new Bitmap(boyut, boyut, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(b))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            var rozet = new Rectangle(70, 70, boyut - 140, boyut - 140);
            int yaricap = 210;

            using (var yol = Yuvarlak(rozet, yaricap))
            {
                using (var firca = new LinearGradientBrush(rozet,
                    Color.FromArgb(37, 99, 235), Color.FromArgb(34, 211, 238), 52f))
                {
                    var karisim = new ColorBlend(3);
                    karisim.Colors = new[] { Color.FromArgb(37, 99, 235), Color.FromArgb(59, 130, 246), Color.FromArgb(45, 212, 191) };
                    karisim.Positions = new[] { 0f, 0.52f, 1f };
                    firca.InterpolationColors = karisim;
                    g.FillPath(firca, yol);
                }

                // İç çerçeve: cam kenarı hissi.
                using (var ic = Yuvarlak(new Rectangle(rozet.X + 12, rozet.Y + 12, rozet.Width - 24, rozet.Height - 24), yaricap - 10))
                using (var kalem = new Pen(Color.FromArgb(85, 255, 255, 255), 10))
                    g.DrawPath(kalem, ic);

                // Üst solda yumuşak ışık.
                g.SetClip(yol);
                using (var isik = new LinearGradientBrush(new Rectangle(rozet.X, rozet.Y, rozet.Width, rozet.Height / 2),
                    Color.FromArgb(58, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                    g.FillRectangle(isik, rozet.X, rozet.Y, rozet.Width, rozet.Height / 2);
                g.ResetClip();
            }

            // "Y" monogramı: üç kalın, yuvarlak uçlu çizgi.
            float olcek = boyut / 1024f;
            using (var kalem = new Pen(Color.White, 104 * olcek))
            {
                kalem.StartCap = LineCap.Round;
                kalem.EndCap = LineCap.Round;
                kalem.LineJoin = LineJoin.Round;
                var yol = new GraphicsPath();
                yol.AddLine(300 * olcek, 330 * olcek, 512 * olcek, 566 * olcek);
                yol.AddLine(512 * olcek, 566 * olcek, 724 * olcek, 330 * olcek);
                yol.AddLine(512 * olcek, 566 * olcek, 512 * olcek, 770 * olcek);
                g.DrawPath(kalem, yol);
                yol.Dispose();
            }

            // Sağ üstte küçük parıltı: temizlik ve performans vurgusu.
            float cx = 786 * olcek, cy = 226 * olcek, r = 62 * olcek;
            using (var isik = new SolidBrush(Color.FromArgb(64, 255, 255, 255)))
                g.FillEllipse(isik, cx - r * 1.8f, cy - r * 1.8f, r * 3.6f, r * 3.6f);
            using (var beyaz = new SolidBrush(Color.White))
                g.FillPath(beyaz, Yildiz(cx, cy, r, r * 0.22f));
        }
        return b;
    }

    static GraphicsPath Yildiz(float cx, float cy, float dis, float ic)
    {
        var p = new GraphicsPath();
        p.AddPolygon(new[]
        {
            new PointF(cx, cy - dis), new PointF(cx + ic, cy - ic), new PointF(cx + dis, cy),
            new PointF(cx + ic, cy + ic), new PointF(cx, cy + dis), new PointF(cx - ic, cy + ic),
            new PointF(cx - dis, cy), new PointF(cx - ic, cy - ic)
        });
        p.CloseFigure();
        return p;
    }

    static GraphicsPath Yuvarlak(Rectangle r, int yaricap)
    {
        var p = new GraphicsPath();
        int d = Math.Max(2, Math.Min(yaricap * 2, Math.Min(r.Width, r.Height)));
        if (r.Width <= 0 || r.Height <= 0) return p;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Bitmap Olcekle(Bitmap b, int s)
    {
        var o = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(o))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            using (var ia = new ImageAttributes())
            {
                ia.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(b, new Rectangle(0, 0, s, s), 0, 0, b.Width, b.Height, GraphicsUnit.Pixel, ia);
            }
        }
        return o;
    }

    static void IcoYaz(string yol, int[] boyutlar, Dictionary<int, byte[]> pngler)
    {
        using (var fs = File.Create(yol))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((short)0); bw.Write((short)1); bw.Write((short)boyutlar.Length);
            int ofset = 6 + 16 * boyutlar.Length;
            foreach (int s in boyutlar)
            {
                bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)(s >= 256 ? 0 : s));
                bw.Write((byte)0); bw.Write((byte)0); bw.Write((short)1); bw.Write((short)32);
                bw.Write(pngler[s].Length); bw.Write(ofset);
                ofset += pngler[s].Length;
            }
            foreach (int s in boyutlar) bw.Write(pngler[s]);
        }
    }
}
