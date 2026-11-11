// Sağlık sekmesi: bellek bekçisinin durumu (ayarları Ayarlar sekmesinde), sürekli büyüyen (sızıntı şüphesi olan) programlar,
// son 7 günün çökme geçmişi ve kullanıcının yardım alacağı kişiye göndereceği destek raporu. Hiçbir ayarı değiştirmez.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

class SaglikSekmesi : Panel
{
    // Ciddi olaylar için okunaklı kırmızı (Renk.Kirmizi koyu zeminde biraz sert duruyor).
    static readonly Color AcikKirmizi = Color.FromArgb(239, 83, 80);

    const string BosSizinti = "Şu an şüpheli büyüme yok. Bekçi 30 saniyede bir ölçer; bir şeyin sürekli büyüdüğünü " +
                              "söyleyebilmek için bir programın en az yarım saat boyunca büyüdüğünü görmek gerekir.";
    internal const string OnemsizAciklama = "Önemsiz kayıt: kesme noktası (0x80000003); çoğu zaman zararsız, özete sayılmadı.";
    const string RaporBilgi ="Destek raporu bilgisayarın durumunu tek bir metin dosyasında toplar; sorunu uzaktan anlamak için.";

    readonly BellekBekcisi bekci;
    readonly Label lblBekci = new Label();
    readonly KoyuListe sizintiListesi = new KoyuListe(), cokmeListesi = new KoyuListe();
    readonly Label lblSizintiBos = new Label(), lblOzet = new Label(), lblDurum = new Label();
    readonly Button btnYenile, btnRapor;
    readonly Panel sizintiKutu;
    readonly System.Windows.Forms.Timer tazeleyici = new System.Windows.Forms.Timer { Interval = 30000 };
    readonly Font kalin;
    bool cokmeYuklendi, cokmeOkunuyor, raporHazirlaniyor;

    // AnaForm kurar: "Ayarlar" düğmesi Ayarlar sekmesini açar.
    public Action AyarlaraGit;

    public SaglikSekmesi(BellekBekcisi bekci)
    {
        this.bekci = bekci;
        Dock = DockStyle.Fill;
        BackColor = Renk.Arka;
        kalin = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        // 1) BELLEK BEKÇİSİ: yalnızca durum; ayarları Ayarlar sekmesinde.
        var kartDis = new Panel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(12, 12, 12, 4) };
        var kart = new Panel { Dock = DockStyle.Fill, BackColor = Renk.Panel, Padding = new Padding(16, 8, 10, 8) };
        var kartBaslik = new Label
        {
            Dock = DockStyle.Top, Height = 20, ForeColor = Renk.Soluk, Text = "BELLEK BEKÇİSİ",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        lblBekci.Dock = DockStyle.Fill;
        lblBekci.AutoEllipsis = true;
        lblBekci.ForeColor = Renk.Yazi;
        var btnAyarlar = Dugme("Ayarlar", false);
        btnAyarlar.Width = 110;
        btnAyarlar.Click += delegate { if (AyarlaraGit != null) AyarlaraGit(); };
        new ToolTip().SetToolTip(btnAyarlar, "Bekçi, kısayol ve ses ayarları");
        kart.Controls.Add(lblBekci);
        kart.Controls.Add(kartBaslik);
        kart.Controls.Add(btnAyarlar);
        kartDis.Controls.Add(kart);

        // 2) SÜREKLİ BÜYÜYEN PROGRAMLAR
        var sizintiBaslik = Baslik("SÜREKLİ BÜYÜYEN PROGRAMLAR · sızıntı şüphesi",
            "Kullandığı bellek dakikalardır hiç düşmeden artan programlar. Programların (oyunlar dahil) \"bellek yetersiz\" diye kapanmasının sık sebeplerinden biri.");
        sizintiBaslik.Dock = DockStyle.Top;
        sizintiBaslik.Height = 46;
        sizintiBaslik.Padding = new Padding(14, 10, 12, 0);

        sizintiListesi.CheckBoxes = false;
        sizintiListesi.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        sizintiListesi.Columns.Add("Program", 300);
        sizintiListesi.Columns.Add("Şu an", 90, HorizontalAlignment.Right);
        sizintiListesi.Columns.Add("Artış", 100, HorizontalAlignment.Right);
        sizintiListesi.Columns.Add("Süre", 80, HorizontalAlignment.Right);
        sizintiListesi.Columns.Add("Hız", 100, HorizontalAlignment.Right);
        sizintiListesi.Dock = DockStyle.Fill;
        sizintiListesi.Resize += delegate { SutunDoldur(sizintiListesi, 0); };

        lblSizintiBos.Dock = DockStyle.Top;
        lblSizintiBos.Height = 46;
        lblSizintiBos.Padding = new Padding(10, 6, 10, 0);
        lblSizintiBos.BackColor = Renk.Panel;
        lblSizintiBos.ForeColor = Renk.Soluk;
        lblSizintiBos.Text = BosSizinti;

        sizintiKutu = new Panel { Dock = DockStyle.Top, Height = 132, Padding = new Padding(12, 0, 12, 0) };
        sizintiKutu.Controls.Add(sizintiListesi);
        sizintiKutu.Controls.Add(lblSizintiBos);

        var sizintiNot = new Label
        {
            Dock = DockStyle.Top, Height = 26, Padding = new Padding(14, 5, 12, 0), ForeColor = Renk.Soluk, AutoEllipsis = true,
            Text = "Sızıntı yapan programı kapatmak belleği hemen geri verir (Sistem sekmesi → sağ tık → kapat)."
        };

        // 3) ÇÖKME GEÇMİŞİ
        var cokmeSerit = new Panel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(14, 12, 12, 6) };
        btnYenile = Dugme("Yenile", false);
        btnYenile.Width = 100;
        btnYenile.Click += delegate { CokmeleriOku(); };
        cokmeSerit.Controls.Add(Baslik("ÇÖKME GEÇMİŞİ · son 7 gün",
            "Windows'un kaydettiği program çökmeleri, donmalar, mavi ekranlar ve \"bellek yetersiz\" uyarıları."));
        cokmeSerit.Controls.Add(btnYenile);

        lblOzet.Dock = DockStyle.Top;
        lblOzet.Height = 42;
        lblOzet.Padding = new Padding(14, 2, 12, 0);
        lblOzet.ForeColor = Renk.Yazi;
        lblOzet.AutoEllipsis = true;
        lblOzet.Text = "Çökme geçmişi okunmadı.";

        cokmeListesi.CheckBoxes = false;
        cokmeListesi.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        cokmeListesi.ShowItemToolTips = true;
        cokmeListesi.Columns.Add("Zaman", 90);
        cokmeListesi.Columns.Add("Ne oldu", 160);
        cokmeListesi.Columns.Add("Program", 150);
        cokmeListesi.Columns.Add("Bellek doluyken", 110);
        cokmeListesi.Columns.Add("Ayrıntı", 300);
        cokmeListesi.Dock = DockStyle.Fill;
        cokmeListesi.Resize += delegate { SutunDoldur(cokmeListesi, 4); };
        cokmeListesi.DoubleClick += delegate { OlayiGoster(); };
        var cokmeKutu = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
        cokmeKutu.Controls.Add(cokmeListesi);

        // 4) Alt satır: destek raporu
        var raporSerit = new Panel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(12, 10, 12, 4) };
        btnRapor = Dugme("Destek raporu oluştur", true);
        btnRapor.Dock = DockStyle.Fill;
        btnRapor.Click += delegate { RaporOlustur(); };
        raporSerit.Controls.Add(btnRapor);

        lblDurum.Dock = DockStyle.Bottom;
        lblDurum.Height = 40;
        lblDurum.Padding = new Padding(14, 0, 12, 0);
        lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        lblDurum.ForeColor = Renk.Soluk;
        lblDurum.Text = RaporBilgi;

        // Dock sırası: önce Fill, sonra Top'lar alttan üste (son eklenen en üstte), en son Bottom'lar.
        Controls.Add(cokmeKutu);
        Controls.Add(lblOzet);
        Controls.Add(cokmeSerit);
        Controls.Add(sizintiNot);
        Controls.Add(sizintiKutu);
        Controls.Add(sizintiBaslik);
        Controls.Add(kartDis);
        Controls.Add(raporSerit);
        Controls.Add(lblDurum);

        BekciyiYaz();
        SizintilariTazele();
        tazeleyici.Tick += delegate { SizintilariTazele(); };
        // Yalnızca sekme ekrandayken tazele; pencere tepsideyken (oyun sırasında) boşuna arayüz işi yapılmasın.
        VisibleChanged += delegate { if (Visible) { SizintilariTazele(); tazeleyici.Start(); } else tazeleyici.Stop(); };
    }

    static Label Baslik(string baslik, string alt)
    {
        return new Label
        {
            Dock = DockStyle.Fill, ForeColor = Renk.Soluk, AutoEllipsis = true,
            Text = baslik + "\n" + alt, Font = new Font("Segoe UI", 8.5f)
        };
    }

    static Button Dugme(string metin, bool vurgu)
    {
        var b = new Button
        {
            Text = metin, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = vurgu ? Renk.Vurgu : Renk.Panel, ForeColor = vurgu ? Color.White : Renk.Yazi,
            Font = new Font("Segoe UI", vurgu ? 11.5f : 9.5f, FontStyle.Bold)
        };
        b.FlatAppearance.BorderSize = vurgu ? 0 : 1;
        b.FlatAppearance.BorderColor = Renk.Cizgi;
        return b;
    }

    // Yerleşim bitince (kaydırma çubukları yerine oturunca) bir kez daha sığdırılır; yoksa yatay çubuk kalabiliyor.
    static void SutunDoldur(ListView l, int sutun)
    {
        Sigdir(l, sutun);
        try { if (l.IsHandleCreated) l.BeginInvoke(new Action(() => Sigdir(l, sutun, true))); } catch { }
    }

    static void Sigdir(ListView l, int sutun, bool zorla = false)
    {
        int kalan = l.ClientSize.Width;
        for (int i = 0; i < l.Columns.Count; i++) if (i != sutun) kalan -= l.Columns[i].Width;
        if (kalan <= 120) return;
        // Yatay çubuk bir kez çıkınca genişlik tam sığsa da kalabiliyor: zorlarken bir piksel oynatılıp yeniden yazılır.
        if (zorla && l.Columns[sutun].Width == kalan) l.Columns[sutun].Width = kalan - 1;
        if (l.Columns[sutun].Width != kalan) l.Columns[sutun].Width = kalan;
    }

    public void Gosterildi()
    {
        BekciyiYaz();   // Ayarlar / Başlangıç sekmesinden ya da Görev Yöneticisi'nden değişmiş olabilir
        SizintilariTazele();
        if (!cokmeYuklendi) CokmeleriOku();
    }

    // Bekçinin durumu tek satırda; değiştirmek için "Ayarlar" düğmesi.
    void BekciyiYaz()
    {
        if (bekci == null) { lblBekci.Text = "Bekçi çalışmıyor."; lblBekci.ForeColor = Renk.Soluk; return; }
        bool arka = false, windows = false;
        try { arka = bekci.ArkaPlandaCalis; windows = arka && bekci.WindowsIleBaslat; } catch { }
        var parca = new List<string>();
        parca.Add(arka ? "Arka planda çalışıyor" : "Pencere kapanınca duruyor (arka planda çalışma kapalı)");
        if (arka) parca.Add(windows ? "Windows ile başlıyor" : "Windows ile başlamıyor");
        parca.Add(bekci.OyunModu ? "oyun modu açık" : "oyun modu kapalı");
        Keys k = bekci.Kisayol;
        parca.Add(k == Keys.None ? "RAM kısayolu kapalı" : "RAM kısayolu " + KisayolAyari.Metin(k) + (bekci.KisayolCalisiyor ? "" : " (başka programda)"));
        lblBekci.Text = string.Join(" · ", parca);
        lblBekci.ForeColor = arka ? Renk.Yazi : Renk.Turuncu;
    }

    // ---- Sızıntı şüphelileri ----

    void SizintilariTazele()
    {
        List<SizintiSupheli> supheliler = null;
        try { if (bekci != null && bekci.Izleyici != null) supheliler = bekci.Izleyici.Supheliler(); } catch { }
        if (supheliler == null) supheliler = new List<SizintiSupheli>();

        sizintiListesi.BeginUpdate();
        try
        {
            sizintiListesi.Items.Clear();
            foreach (var s in supheliler)
            {
                var oge = new ListViewItem(GorunenAd(bekci, s)) { UseItemStyleForSubItems = false };
                oge.SubItems.Add(AnaForm.Boyut(s.Simdi));
                var artis = oge.SubItems.Add("+" + AnaForm.Boyut(s.Artis));
                oge.SubItems.Add(SureYaz(s.Sure));
                oge.SubItems.Add(string.Format("{0:0} MB/dk", s.HizMBDakika));
                foreach (ListViewItem.ListViewSubItem a in oge.SubItems) { a.ForeColor = Renk.Yazi; a.BackColor = Renk.Panel; }
                if (s.Artis >= 2L * 1073741824) artis.ForeColor = Renk.Turuncu;
                oge.ToolTipText = s.Ad + ".exe";
                sizintiListesi.Items.Add(oge);
            }
        }
        finally { sizintiListesi.EndUpdate(); }
        // Şüpheli yokken boş liste yer kaplamasın: yalnızca açıklama kalır, çökme listesine yer açılır.
        bool bos = supheliler.Count == 0;
        lblSizintiBos.Visible = bos;
        sizintiListesi.Visible = !bos;
        sizintiKutu.Height = bos ? lblSizintiBos.Height : 132;
        SutunDoldur(sizintiListesi, 0);
    }

    internal static string GorunenAd(BellekBekcisi bekci, SizintiSupheli s)
    {
        if (!string.IsNullOrEmpty(s.Gorunen)) return s.Gorunen;
        try { if (bekci != null && bekci.Izleyici != null) return bekci.Izleyici.GorunenAd(s.Ad); } catch { }
        return s.Ad;
    }

    internal static string SureYaz(TimeSpan t)
    {
        if (t.TotalMinutes < 1) return "1 dk'dan az";
        if (t.TotalMinutes < 60) return string.Format("{0:0} dk", Math.Floor(t.TotalMinutes));
        return string.Format("{0} sa {1} dk", (int)t.TotalHours, t.Minutes);
    }

    // ---- Çökme geçmişi ----

    async void CokmeleriOku()
    {
        if (cokmeOkunuyor) return;
        cokmeOkunuyor = true;
        cokmeYuklendi = true;
        btnYenile.Enabled = false;
        lblOzet.ForeColor = Renk.Soluk;
        lblOzet.Text = "Windows olay günlüğü okunuyor...";
        List<CokmeOlayi> olaylar = null;
        string ozet = null;
        try
        {
            await Task.Run(() =>
            {
                olaylar = CokmeGecmisi.Oku(7);
                if (olaylar == null) olaylar = new List<CokmeOlayi>();
                ozet = CokmeGecmisi.Ozet(olaylar);
            });
        }
        catch (Exception e)
        {
            olaylar = new List<CokmeOlayi>();
            ozet = "Çökme geçmişi okunamadı: " + e.Message;
        }
        finally
        {
            cokmeOkunuyor = false;
            btnYenile.Enabled = true;
        }
        if (IsDisposed) return;
        CokmeleriGoster(olaylar, ozet);
    }

    void CokmeleriGoster(List<CokmeOlayi> olaylar, string ozet)
    {
        bool bellekte = olaylar.Any(o => o.BellekDoluyken && !CokmeGecmisi.OnemsizMi(o));
        lblOzet.ForeColor = bellekte ? Renk.Turuncu : Renk.Yazi;
        lblOzet.Font = bellekte ? kalin : Font;
        lblOzet.Text = string.IsNullOrEmpty(ozet)
            ? (olaylar.Count == 0 ? "Son 7 günde kayıtlı çökme yok." : olaylar.Count + " olay bulundu.")
            : ozet;

        cokmeListesi.BeginUpdate();
        try
        {
            cokmeListesi.Items.Clear();
            foreach (var o in olaylar)
            {
                // Önemsiz kayıt (kesme noktası): listede kalır ama soluk ve işaretli; özete sayılmaz.
                bool onemsiz = CokmeGecmisi.OnemsizMi(o);
                var oge = new ListViewItem(o.Zaman.ToString("dd.MM HH:mm")) { UseItemStyleForSubItems = false, Tag = o };
                oge.SubItems.Add((o.Tur ?? "") + (onemsiz ? " · önemsiz" : ""));
                oge.SubItems.Add(o.Uygulama ?? "");
                var bellek = oge.SubItems.Add(o.BellekDoluyken ? "Evet" : "–");
                oge.SubItems.Add(TekSatir(o.Ayrinti, 300));
                foreach (ListViewItem.ListViewSubItem a in oge.SubItems)
                {
                    a.BackColor = Renk.Panel;
                    a.ForeColor = onemsiz ? Renk.Soluk : o.Ciddi ? AcikKirmizi : Renk.Yazi;
                }
                if (o.Ciddi) oge.SubItems[1].Font = kalin;   // yalnızca "Ne oldu" kalın; ayrıntı okunaklı kalsın
                if (o.BellekDoluyken && !onemsiz) { bellek.ForeColor = Renk.Turuncu; bellek.Font = kalin; }
                oge.ToolTipText = (onemsiz ? OnemsizAciklama + "\n" : "") +
                    TekSatir(o.Ayrinti, 1000) + (string.IsNullOrEmpty(o.Ayrinti) ? "" : "\n(çift tık: tamamını göster)");
                cokmeListesi.Items.Add(oge);
            }
        }
        finally { cokmeListesi.EndUpdate(); }
        // Dikey kaydırma çubuğu çıkınca liste genişliği daralır ama Resize gelmez: Ayrıntı sütununu yeniden sığdır.
        SutunDoldur(cokmeListesi, 4);
    }

    void OlayiGoster()
    {
        if (cokmeListesi.SelectedItems.Count == 0) return;
        var o = cokmeListesi.SelectedItems[0].Tag as CokmeOlayi;
        if (o == null) return;
        var metin = new StringBuilder();
        metin.AppendLine(o.Zaman.ToString("dd.MM.yyyy HH:mm:ss") + "  ·  " + (o.Tur ?? ""));
        if (!string.IsNullOrEmpty(o.Uygulama)) metin.AppendLine("Program: " + o.Uygulama);
        if (o.BellekDoluyken) metin.AppendLine("O sırada bellek doluydu.");
        if (CokmeGecmisi.OnemsizMi(o)) metin.AppendLine(OnemsizAciklama);
        if (o.OlayKimligi != 0) metin.AppendLine("Olay kimliği: " + o.OlayKimligi);
        metin.AppendLine();
        metin.Append(string.IsNullOrEmpty(o.Ayrinti) ? "(ayrıntı yok)" : o.Ayrinti);
        MessageBox.Show(FindForm(), metin.ToString(), "Çökme ayrıntısı", MessageBoxButtons.OK,
            o.Ciddi ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    internal static string TekSatir(string metin, int enCok)
    {
        if (string.IsNullOrEmpty(metin)) return "";
        var s = Regex.Replace(metin, @"\s+", " ").Trim();
        return s.Length > enCok ? s.Substring(0, enCok - 1) + "…" : s;
    }

    // ---- Destek raporu ----

    async void RaporOlustur()
    {
        if (raporHazirlaniyor) return;
        raporHazirlaniyor = true;
        btnRapor.Enabled = false;
        btnRapor.Text = "Rapor hazırlanıyor...";
        lblDurum.ForeColor = Renk.Soluk;
        // NVIDIA kartı yoksa NvidiaSurucu.Kontrol internete gitmez; bekleme uyarısı yalnızca NVIDIA'lı bilgisayarda.
        bool nvidia = false;
        try { nvidia = NvidiaSurucu.KartVar(); } catch { }
        lblDurum.Text = nvidia ? "Rapor hazırlanıyor; NVIDIA'nın sitesine de sorulduğu için 10–20 saniye sürebilir." : "Rapor hazırlanıyor...";
        string yol = null, hata = null;
        try { yol = await Task.Run(() => DestekRaporu.Olustur(bekci)); }
        catch (Exception e) { hata = e.Message; }
        finally
        {
            raporHazirlaniyor = false;
            btnRapor.Enabled = true;
            btnRapor.Text = "Destek raporu oluştur";
        }
        if (IsDisposed) return;
        if (yol == null)
        {
            lblDurum.ForeColor = Renk.Turuncu;
            lblDurum.Text = "Rapor kaydedilemedi: " + hata;
            return;
        }
        try { Process.Start("explorer.exe", "/select,\"" + yol + "\""); } catch { }
        string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        bool masaustunde = string.Equals(Path.GetDirectoryName(yol), masaustu.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        lblDurum.ForeColor = Renk.Yesil;
        lblDurum.Text = string.Format("Rapor {0} kaydedildi: {1}. Bu dosyayı sana yardım eden kişiye ya da destek adresine gönder; içinde kişisel dosya ya da şifre yok.",
            masaustunde ? "masaüstüne" : "Belgeler klasörüne", Path.GetFileName(yol));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tazeleyici.Stop();
            tazeleyici.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Kullanıcının yardım alacağı kişiye gönderilecek düz metin rapor. Her bölüm kendi içinde korunur: biri okunamazsa rapor yine oluşur.
// Kullanıcı adı, bilgisayar adı ve kullanıcı klasörü yolu en sonda metinden silinir.
static class DestekRaporu
{
    [DllImport("kernel32.dll")] static extern ulong GetTickCount64();

    const double GB = 1073741824.0;

    public static string Olustur(BellekBekcisi bekci)
    {
        string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        try { return Olustur(bekci, masaustu); }
        catch
        {
            // Masaüstüne yazılamazsa (ör. klasör yönlendirmesi bozuk) Belgeler'e dene.
            return Olustur(bekci, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
    }

    internal static string Olustur(BellekBekcisi bekci, string klasor)
    {
        string metin = Metin(bekci);
        Directory.CreateDirectory(klasor);
        string yol = Path.Combine(klasor, "YamanPcCleaner-rapor-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".txt");
        File.WriteAllText(yol, metin, new UTF8Encoding(true));   // BOM: Not Defteri Türkçeyi doğru göstersin
        return yol;
    }

    internal static string Metin(BellekBekcisi bekci)
    {
        var sb = new StringBuilder();
        Bolum(sb, null, delegate(StringBuilder b) { BaslikYaz(b); });
        Bolum(sb, "SİSTEM", delegate(StringBuilder b) { SistemYaz(b); });
        Bolum(sb, "BELLEK", delegate(StringBuilder b) { BellekYaz(b, bekci); });
        Bolum(sb, "EN ÇOK BELLEK KULLANAN 15 PROGRAM", delegate(StringBuilder b) { ProgramlariYaz(b, bekci); });
        Bolum(sb, "SIZINTI ŞÜPHELİLERİ (sürekli büyüyen programlar)", delegate(StringBuilder b) { SizintilariYaz(b, bekci); });
        Bolum(sb, "ÇÖKME GEÇMİŞİ (son 7 gün)", delegate(StringBuilder b) { CokmeleriYaz(b); });
        Bolum(sb, "BAŞLANGIÇ PROGRAMLARI", delegate(StringBuilder b) { BaslangiciYaz(b); });
        sb.AppendLine("— rapor sonu —");
        return Gizle(sb.ToString());
    }

    static void Bolum(StringBuilder sb, string baslik, Action<StringBuilder> govde)
    {
        if (baslik != null)
        {
            sb.AppendLine(baslik);
            sb.AppendLine(new string('=', baslik.Length));
        }
        var b = new StringBuilder();
        try { govde(b); }
        catch (Exception e) { b.AppendLine("  (okunamadı: " + e.Message + ")"); }
        sb.Append(b);
        sb.AppendLine();
    }

    static void Satir(StringBuilder b, string etiket, string deger)
    {
        b.AppendLine("  " + (etiket + ":").PadRight(26) + " " + deger);
    }

    static string Yuzde(double oran) { return string.Format("%{0:0}", oran * 100); }

    static void BaslikYaz(StringBuilder b)
    {
        b.AppendLine("YAMAN PC CLEANER · DESTEK RAPORU");
        b.AppendLine("Bu rapor Yaman PC Cleaner ile oluşturuldu.");
        b.AppendLine("Oluşturulma: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
        var bu = Assembly.GetExecutingAssembly();
        var v = bu.GetName().Version;
        string surum = v == null || v.ToString() == "0.0.0.0" ? "" : v.ToString() + ", ";
        string tarih = "";
        try { tarih = "derleme " + File.GetLastWriteTime(bu.Location).ToString("dd.MM.yyyy HH:mm"); } catch { }
        b.AppendLine("Program sürümü: " + (surum + tarih).Trim().TrimEnd(','));
        b.AppendLine("İçerik: donanım, bellek durumu, çalışan programların adları ve bellekleri, Windows'un çökme kayıtları,");
        b.AppendLine("başlangıç programları. Kişisel dosya, şifre, kullanıcı adı ya da bilgisayar adı yok.");
    }

    // ---- SİSTEM ----

    static void SistemYaz(StringBuilder b)
    {
        string windows = null;
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                if (k != null)
                {
                    string ad = k.GetValue("ProductName") as string;
                    string surum = k.GetValue("DisplayVersion") as string ?? k.GetValue("ReleaseId") as string;
                    string derleme = k.GetValue("CurrentBuild") as string ?? k.GetValue("CurrentBuildNumber") as string;
                    object ubr = k.GetValue("UBR");
                    int d;
                    // Windows 11'de de ProductName "Windows 10 ..." yazar; 22000 ve sonrası derleme Windows 11'dir.
                    if (ad != null && int.TryParse(derleme, out d) && d >= 22000 && ad.StartsWith("Windows 10"))
                        ad = "Windows 11" + ad.Substring(10);
                    windows = string.Format("{0} {1} (derleme {2}{3})", ad, surum, derleme, ubr is int ? "." + ubr : "").Replace("  ", " ");
                }
            }
        }
        catch { }
        // Environment.OSVersion bu exe'de (manifestsiz .NET Framework) 6.2.9200 der; yalnızca kayıt okunamazsa kullanılır.
        Satir(b, "Windows", (windows ?? Environment.OSVersion.VersionString) + (Environment.Is64BitOperatingSystem ? ", 64 bit" : ", 32 bit"));
        try
        {
            var acik = TimeSpan.FromMilliseconds(GetTickCount64());
            Satir(b, "Açık kalma süresi", string.Format("{0} gün {1} sa {2} dk (son yeniden başlatmadan beri)", acik.Days, acik.Hours, acik.Minutes));
        }
        catch { }

        string islemci = null;
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                if (k != null) islemci = (k.GetValue("ProcessorNameString") as string ?? "").Trim();
        }
        catch { }
        Satir(b, "İşlemci", string.IsNullOrEmpty(islemci) ? "(okunamadı)" : Regex.Replace(islemci, @"\s+", " "));
        Satir(b, "Mantıksal işlemci sayısı", Environment.ProcessorCount.ToString());
        Satir(b, "RAM", string.Format("{0:0.0} GB", Yerel.Bellek().ullTotalPhys / GB));

        b.AppendLine("  Ekran kartları:");
        int kartSayisi = 0;
        var takili = EkranKartlari.Takililar();   // sökülmüş kartların kalıntı girdileri yazılmasın
        using (var sinif = Registry.LocalMachine.OpenSubKey(EkranKartlari.SinifAnahtari))
        {
            if (sinif != null)
                foreach (var alt in sinif.GetSubKeyNames())
                {
                    if (alt.Length != 4 || !alt.All(char.IsDigit)) continue;
                    if (takili != null && !takili.Contains(alt)) continue;
                    try
                    {
                        using (var k = sinif.OpenSubKey(alt))
                        {
                            string ad = k == null ? null : k.GetValue("DriverDesc") as string;
                            if (string.IsNullOrEmpty(ad)) continue;
                            long vram = EkranKartlari.Vram(k);
                            string tarih = k.GetValue("DriverDate") as string;
                            DateTime t;
                            if (DateTime.TryParseExact(tarih, "M-d-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out t))
                                tarih = t.ToString("dd.MM.yyyy");
                            b.AppendLine(string.Format("    • {0} — sürücü {1} ({2}){3}", ad.Trim(),
                                k.GetValue("DriverVersion") as string ?? "?", tarih ?? "tarih yok",
                                vram > 0 ? ", " + AnaForm.Boyut(vram) + " bellek" : ""));
                            kartSayisi++;
                        }
                    }
                    catch { }
                }
        }
        if (kartSayisi == 0) b.AppendLine("    (bulunamadı)");

        b.AppendLine("  Diskler:");
        foreach (var d in DriveInfo.GetDrives())
        {
            try
            {
                if (d.DriveType != DriveType.Fixed || !d.IsReady) continue;
                b.AppendLine(string.Format("    • {0} {1} — toplam {2:0} GB, boş {3:0.0} GB ({4} boş)", d.Name.TrimEnd('\\'), d.DriveFormat,
                    d.TotalSize / GB, d.AvailableFreeSpace / GB, Yuzde((double)d.AvailableFreeSpace / d.TotalSize)));
            }
            catch { }
        }
    }

    // ---- BELLEK ----

    static void BellekYaz(StringBuilder b, BellekBekcisi bekci)
    {
        var m = Yerel.Bellek();
        double ramTop = m.ullTotalPhys / GB, ramKul = (m.ullTotalPhys - m.ullAvailPhys) / GB;
        Satir(b, "RAM kullanımı", string.Format("{0:0.0} / {1:0.0} GB ({2})", ramKul, ramTop, Yuzde(ramKul / ramTop)));
        double sTop = m.ullTotalPageFile / GB, sKul = (m.ullTotalPageFile - m.ullAvailPageFile) / GB;
        Satir(b, "Sanal bellek (RAM+sayfa)", string.Format("{0:0.0} / {1:0.0} GB ({2}){3}", sKul, sTop, Yuzde(sKul / sTop),
            sKul / sTop >= 0.9 ? "  ← NEREDEYSE DOLU" : ""));
        Satir(b, "Sayfa dosyası boyutu", string.Format("şu an yaklaşık {0:0.0} GB", Math.Max(0, sTop - ramTop)));

        var satirlar = SanalBellek.Oku();
        Satir(b, "Sayfa dosyası ayarı", SanalBellek.Anlat(satirlar));
        if (satirlar.Length > 0) Satir(b, "  kayıt defteri değeri", string.Join(" | ", satirlar));
        int ilk, enCok;
        SanalBellek.Hedef(m.ullTotalPhys, out ilk, out enCok);
        Satir(b, "Bu RAM için önerilen", string.Format("{0:0.#}–{1:0.#} GB ({2} sürücüsünde)", ilk / 1024.0, enCok / 1024.0, SanalBellek.Surucu));
        Satir(b, "Program yedeği", SanalBellek.YedekVar ? "var (sanal bellek bu programla büyütülmüş)" : "yok");

        if (bekci != null)
        {
            try
            {
                Satir(b, "Bellek bekçisi", (bekci.ArkaPlandaCalis ? "arka planda çalışıyor" : "arka planda çalışmıyor") +
                    (bekci.WindowsIleBaslat ? ", Windows ile başlıyor" : ", Windows ile başlamıyor"));
            }
            catch { }
        }

        // NVIDIA'ya internetten sorulur; bağlantı takılırsa rapor beklemesin.
        NvidiaBilgi nv = null;
        string nvDurum;
        var gorev = Task.Run(() => NvidiaSurucu.Kontrol());
        if (!gorev.Wait(20000)) nvDurum = "NVIDIA'nın sitesine 20 saniyede ulaşılamadı";
        else
        {
            nv = gorev.Result;
            if (nv == null) nvDurum = "NVIDIA kart yok";
            else
            {
                string son = nv.Hata != null ? "en son sürüm öğrenilemedi: " + nv.Hata
                    : "en son " + nv.SonSurum + (nv.YeniVar ? " → YENİ SÜRÜM VAR" : " → güncel");
                nvDurum = string.Format("{0}, kurulu {1} ({2}); {3}{4}", nv.KartAdi, nv.KuruluSurum, nv.KuruluTarih ?? "tarih yok", son,
                    nv.Uygulama != null ? "; NVIDIA App / GeForce Experience kurulu" : "");
            }
        }
        Satir(b, "NVIDIA sürücüsü", nvDurum);
    }

    // ---- EN ÇOK BELLEK ----

    static void ProgramlariYaz(StringBuilder b, BellekBekcisi bekci)
    {
        var gruplar = new Dictionary<string, long[]>(StringComparer.OrdinalIgnoreCase);   // ad → { bellek, süreç sayısı }
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                long bellek = p.PrivateMemorySize64;
                long[] g;
                if (!gruplar.TryGetValue(p.ProcessName, out g)) gruplar[p.ProcessName] = g = new long[2];
                g[0] += bellek;
                g[1]++;
            }
            catch { }
            finally { p.Dispose(); }
        }
        b.AppendLine("  Bellek = özel bellek (sanal belleği dolduran kısım), aynı adlı süreçler toplanmış.");
        int sira = 0;
        foreach (var g in gruplar.OrderByDescending(x => x.Value[0]).Take(15))
        {
            string gorunen = g.Key;
            try { if (bekci != null && bekci.Izleyici != null) gorunen = bekci.Izleyici.GorunenAd(g.Key) ?? g.Key; } catch { }
            string ad = string.Equals(gorunen, g.Key, StringComparison.OrdinalIgnoreCase) ? g.Key : gorunen + " (" + g.Key + ")";
            b.AppendLine(string.Format("  {0,2}. {1} {2,9}  {3}", ++sira, SagaDoldur(ad, 42), AnaForm.Boyut(g.Value[0]),
                g.Value[1] > 1 ? g.Value[1] + " süreç" : ""));
        }
        b.AppendLine(string.Format("  Toplam: {0} süreç, {1} özel bellek", gruplar.Values.Sum(x => x[1]), AnaForm.Boyut(gruplar.Values.Sum(x => x[0]))));
    }

    static string SagaDoldur(string s, int n)
    {
        if (s.Length > n) s = s.Substring(0, n - 1) + "…";
        return s.PadRight(n);
    }

    // ---- SIZINTI ----

    static void SizintilariYaz(StringBuilder b, BellekBekcisi bekci)
    {
        if (bekci == null || bekci.Izleyici == null) { b.AppendLine("  (bekçi çalışmıyor)"); return; }
        var liste = bekci.Izleyici.Supheliler() ?? new List<SizintiSupheli>();
        if (liste.Count == 0) { b.AppendLine("  yok"); return; }
        foreach (var s in liste)
            b.AppendLine(string.Format("  • {0} ({1}): şu an {2}, son {3} içinde +{4} ({5:0} MB/dk)",
                SaglikSekmesi.GorunenAd(bekci, s), s.Ad, AnaForm.Boyut(s.Simdi), SaglikSekmesi.SureYaz(s.Sure),
                AnaForm.Boyut(s.Artis), s.HizMBDakika));
    }

    // ---- ÇÖKME ----

    static void CokmeleriYaz(StringBuilder b)
    {
        var olaylar = CokmeGecmisi.Oku(7) ?? new List<CokmeOlayi>();
        b.AppendLine("  " + CokmeGecmisi.Ozet(olaylar));
        if (olaylar.Count == 0) return;
        b.AppendLine();
        foreach (var o in olaylar.Take(40))
        {
            var isaret = new List<string>();
            if (o.Ciddi) isaret.Add("CİDDİ");
            if (o.BellekDoluyken) isaret.Add("BELLEK DOLUYKEN");
            if (CokmeGecmisi.OnemsizMi(o)) isaret.Add("ÖNEMSİZ: kesme noktası, özete sayılmadı");
            b.AppendLine(string.Format("  {0}  {1}{2}{3}{4}", o.Zaman.ToString("dd.MM HH:mm"), o.Tur,
                string.IsNullOrEmpty(o.Uygulama) ? "" : " · " + o.Uygulama,
                isaret.Count > 0 ? "  [" + string.Join(", ", isaret) + "]" : "",
                o.OlayKimligi != 0 ? "  (olay " + o.OlayKimligi + ")" : ""));
            string ayrinti = SaglikSekmesi.TekSatir(o.Ayrinti, 400);
            if (ayrinti.Length > 0) b.AppendLine("      " + ayrinti);
        }
        if (olaylar.Count > 40) b.AppendLine(string.Format("  ... ve {0} olay daha", olaylar.Count - 40));
    }

    // ---- BAŞLANGIÇ ----

    static void BaslangiciYaz(StringBuilder b)
    {
        var liste = BaslangicYoneticisi.Listele() ?? new List<BaslangicOgesi>();
        if (liste.Count == 0) { b.AppendLine("  (yok)"); return; }
        b.AppendLine(string.Format("  {0} öğe, {1} tanesi açık.", liste.Count, liste.Count(o => o.Acik)));
        foreach (var o in liste.OrderByDescending(o => o.Acik).ThenBy(o => o.Ad, StringComparer.CurrentCultureIgnoreCase))
            b.AppendLine(string.Format("  [{0}] {1}{2}{3}{4}", o.Acik ? "açık  " : "kapalı", o.Ad,
                string.IsNullOrEmpty(o.Yayinci) ? "" : " — " + o.Yayinci,
                string.IsNullOrEmpty(o.Konum) ? "" : " · " + o.Konum,
                o.CalisanBellek > 0 ? " · şu an " + AnaForm.Boyut(o.CalisanBellek) : ""));
    }

    // ---- GİZLİLİK ----

    // Kullanıcı klasörü yolu ve kullanıcı adı → %KULLANICI%, bilgisayar adı → %BILGISAYAR%.
    internal static string Gizle(string metin)
    {
        try
        {
            string profil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(profil)) profil = Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(profil) && profil.Length > 3)
            {
                profil = profil.TrimEnd('\\');
                // İki geçiş: ilki Türkçe kültürle (ör. Işık/IŞIK, İpek/ipek), ikincisi kültürden bağımsız (tr-TR'de I/i eşleşmez).
                foreach (var secenek in new[] { RegexOptions.IgnoreCase, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant })
                {
                    metin = Regex.Replace(metin, Regex.Escape(profil), "%KULLANICI%", secenek);
                    metin = Regex.Replace(metin, Regex.Escape(profil.Replace('\\', '/')), "%KULLANICI%", secenek);
                }
            }
            metin = Kelime(metin, Environment.UserName, "%KULLANICI%");
            metin = Kelime(metin, Environment.MachineName, "%BILGISAYAR%");
        }
        catch { }
        return metin;
    }

    // Adı yalnızca kelime olarak geçtiği yerde değiştirir ("Ali" adı "Alişan"ı bozmasın).
    static string Kelime(string metin, string ad, string yerine)
    {
        if (string.IsNullOrEmpty(ad) || ad.Length < 2) return metin;
        string desen = @"(?<![\p{L}\p{N}_])" + Regex.Escape(ad) + @"(?![\p{L}\p{N}_])";
        // İkinci geçiş kültürden bağımsız: tr-TR'de "ADMIN" ile "admin" eşleşmez (I → ı).
        metin = Regex.Replace(metin, desen, yerine, RegexOptions.IgnoreCase);
        return Regex.Replace(metin, desen, yerine, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
