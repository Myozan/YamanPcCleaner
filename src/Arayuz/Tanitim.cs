// İlk açılış tanıtımı (3 adım) ve güncellemeden sonra bir kez "Yenilikler".
// Ayar "son_surum": en son gösterilen sürüm. Yoksa ve program daha önce hiç kullanılmamışsa tanıtım, kullanılmışsa
// (güncelleme ile gelinmişse) yenilikler gösterilir. Önde tam ekran oyun varken açılmaz; pencere bir sonraki
// görünüşünde yeniden dener. Hakkında penceresinden tanıtım yeniden açılabilir.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

static class Tanitim
{
    const string AyarSurum = "son_surum";

    public static void GerekirseGoster(Form sahip, BellekBekcisi bekci)
    {
        string son = Ayarlar.OkuMetin(AyarSurum, null);
        if (son == Surum.Metin) return;
        if (bekci != null && bekci.OyunSuruyor) return;
        // Daha önce kullanılmış: güncellemeyle gelinmiş ya da güncelleme denetimi en az bir kez yapılmış.
        bool eski = !string.IsNullOrEmpty(son) || Guncelleyici.Guncellendi || Ayarlar.OkuMetin(Guncelleyici.AyarSon, null) != null;
        Ayarlar.YazMetin(AyarSurum, Surum.Metin);
        using (var f = new TanitimFormu(eski ? TanitimTuru.Yenilikler : TanitimTuru.Tur, bekci)) f.ShowDialog(sahip);
    }

    public static void TuruGoster(Form sahip, BellekBekcisi bekci)
    {
        using (var f = new TanitimFormu(TanitimTuru.Tur, bekci)) f.ShowDialog(sahip);
    }
}

enum TanitimTuru { Tur, Yenilikler }

class TanitimFormu : KoyuPencere
{
    static readonly Font SayfaBaslik = new Font("Segoe UI", 17f, FontStyle.Bold);
    static readonly Font SayfaYazi = new Font("Segoe UI", 10.5f);
    static readonly Font SimgeYazi = new Font("Segoe UI Semibold", 26f);

    readonly string[][] sayfalar;
    readonly Panel tuval = new Panel();
    readonly Button btnIleri, btnGeri;
    readonly LinkLabel lnkGec;
    readonly bool yenilikler;
    int sayfa;

    public TanitimFormu(TanitimTuru tur, BellekBekcisi bekci) : base(tur == TanitimTuru.Tur ? "Hoş geldin" : "Yenilikler", new Size(580, 420))
    {
        yenilikler = tur == TanitimTuru.Yenilikler;
        if (yenilikler)
            sayfalar = new[] { new[] { "★", "v" + Surum.Metin + " sürümünde neler var", string.Join("\n", Surum.Yenilikler.Select(y => "•  " + y)) } };
        else
        {
            Keys k = bekci != null ? bekci.Kisayol : KisayolAyari.Oku();
            sayfalar = new[]
            {
                new[] { "1", "Sistem durumu",
                    "Program açılınca seni sistem durumu karşılar: 100 üzerinden bir puan ve dikkat isteyen şeyler.\n\n" +
                    "Her önerinin yanında onu düzelteceğin yere götüren bir düğme var. Hiçbir şey kendiliğinden değişmez." },
                new[] { "2", "Sistemi Optimize Et",
                    "Bilgisayar yavaşladığında bir kez bas: arka plan programları kapanır, RAM boşalır, geçici dosyalar silinir.\n\n" +
                    "Ne yapacağını önce gösterir, sonunda ne kazandığını sayarak söyler. Bir şey yavaşsa yanındaki \"Neden yavaş?\"a bas." },
                k != Keys.None
                    ? new[] { "3", KisayolAyari.Metin(k),
                        "Oyunun içindeyken bile bu tuşlara basınca RAM boşalır ve kısa bir bip duyarsın; hiçbir şey kapanmaz.\n\n" +
                        "Pencereyi kapatınca program saatin yanında bekler: simgede RAM doluluğu yazar, üstüne gelince özet açılır." }
                    : new[] { "3", "Saatin yanındaki simge",
                        "Pencereyi kapatınca program saatin yanında bekler: simgede RAM doluluğu yazar, üstüne gelince özet açılır.\n\n" +
                        "Bellek dolmak üzereyken haber verir." }
            };
        }

        tuval.SetBounds(0, 0, 580, 340);
        tuval.Paint += Ciz;
        typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(tuval, true, null);

        btnIleri = Dugme(yenilikler ? "Harika" : "İleri", true);
        btnIleri.SetBounds(420, 356, 136, 42);
        btnIleri.Click += delegate { if (sayfa >= sayfalar.Length - 1) Close(); else { sayfa++; Yenile(); } };
        btnGeri = Dugme("Geri", false);
        btnGeri.SetBounds(310, 356, 100, 42);
        btnGeri.Click += delegate { if (sayfa > 0) { sayfa--; Yenile(); } };
        lnkGec = new LinkLabel
        {
            Text = "Geç", Left = 24, Top = 368, Width = 80, Height = 22, LinkColor = Renk.Soluk, ActiveLinkColor = Renk.Yazi,
            VisitedLinkColor = Renk.Soluk, LinkBehavior = LinkBehavior.HoverUnderline
        };
        lnkGec.LinkClicked += delegate { Close(); };
        AcceptButton = btnIleri;
        Controls.AddRange(new Control[] { tuval, btnIleri, btnGeri, lnkGec });
        Yenile();
    }

    void Yenile()
    {
        bool son = sayfa >= sayfalar.Length - 1;
        btnIleri.Text = yenilikler ? "Harika" : son ? "Başlayalım" : "İleri";
        btnGeri.Visible = !yenilikler && sayfa > 0;
        lnkGec.Visible = !yenilikler && !son;
        tuval.Invalidate();
    }

    void Ciz(object s, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var sf = sayfalar[sayfa];
        // Solda vurgu renginde daire içinde adım numarası (yeniliklerde yıldız).
        var daire = new Rectangle(32, 36, 76, 76);
        using (var b = new SolidBrush(Renk.Vurgu)) g.FillEllipse(b, daire);
        TextRenderer.DrawText(g, sf[0], SimgeYazi, daire, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, sf[1], SayfaBaslik, new Rectangle(132, 40, 420, 36), Renk.Yazi, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, sf[2], yenilikler ? Font : SayfaYazi, new Rectangle(132, 86, 420, 240), yenilikler ? Renk.Yazi : Renk.Soluk,
            TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        if (sayfalar.Length > 1)
        {
            int x = (tuval.Width - (sayfalar.Length * 18 - 8)) / 2;
            for (int i = 0; i < sayfalar.Length; i++)
                using (var b = new SolidBrush(i == sayfa ? Renk.Vurgu : Renk.Cizgi)) g.FillEllipse(b, x + i * 18, 318, 10, 10);
        }
    }
}
