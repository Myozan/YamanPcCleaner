// Disk temizligi ve depolama analizi sekmesi.

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

class Dugum
{
    public string Ad, Yol;
    public long Boyut, DosyaBoyutu;   // DosyaBoyutu: yalnızca bu klasörün kendi dosyaları
    public bool Klasor = true;
    public Dugum Ust;
    public List<Dugum> Alt = new List<Dugum>();
}

class DiskSekmesi : Panel
{
    public const string DerinUyari =
        "Derin temizlik daha çok yer açar ama bedeli var. Açmadan önce oku:\n\n" +
        "•  Oyunlar ilk açılışta takılır ya da uzun yüklenir: ekran kartı ve Steam shader önbellekleri silinir, " +
        "oyunlar onları yeniden derler (1–2 oyun oturumu sürer).\n" +
        "•  Eski Windows'a dönülemez: Windows.old, güncelleme yedekleri ve eski sürücü paketleri silinir; " +
        "güncelleme ya da sürücü geri alma seçeneği kaybolur.\n" +
        "•  Yeni geçici dosyalar da silinir: o sırada bir kurulum ya da güncelleme sürüyorsa yarıda kalabilir. " +
        "Kurulum yaparken çalıştırma.\n" +
        "•  Çökme kayıtları silinir: bir oyunun ya da mavi ekranın sebebini sonradan araştırmak zorlaşır.\n" +
        "•  \"Hazırda bekletme dosyası\"nı işaretlersen hazırda bekletme ve Hızlı Başlatma kapanır; bilgisayar biraz geç açılır.\n" +
        "•  Silinenler geri dönüşüm kutusuna gitmez, geri getirilemez.\n\n" +
        "Silinmeyenler: belgeler, resimler, İndirilenler, oyunlar ve oyun kayıtları, şifreler, tarayıcı geçmişi ve çerezler.\n\n" +
        "Derin mod açılsın mı?";

    readonly TableLayoutPanel suruculer = new TableLayoutPanel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(8, 8, 8, 0), RowCount = 1 };
    readonly KoyuListe temizlikListesi = new KoyuListe();
    readonly KoyuListe analizListesi = new KoyuListe();
    readonly CheckBox chkDerin = new CheckBox();
    readonly Button btnTemizle, btnTara, btnAnaliz, btnUst, btnDosyalar, btnAlgilayici;
    readonly Label lblYol = new Label(), lblDurum = new Label();
    readonly System.Windows.Forms.Timer ilerleme = new System.Windows.Forms.Timer { Interval = 400 };
    readonly Dictionary<string, bool> secimler = new Dictionary<string, bool>();
    List<Temizlik> temizlikler = new List<Temizlik>();
    bool ilkGosterim = true, calisiyor, dolduruluyor, derinDegisiyor, dosyaGorunumu;
    Dugum kok, simdiki;
    List<Dugum> buyukDosyalar = new List<Dugum>();
    int taranan;
    string analizSurucu = SanalBellek.Surucu;   // "C:"; üstteki disk göstergesine tıklayınca değişir

    public DiskSekmesi()
    {
        Dock = DockStyle.Fill;
        BackColor = Renk.Arka;

        var temizBaslik = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(14, 8, 12, 2) };
        var baslik1 = Baslik("GEREKSİZ DOSYALAR", "İşaretlileri siler. Turuncular derin moddan gelir. Açık programın önbelleği atlanır.");
        chkDerin.Text = "Derin temizlik modu";
        chkDerin.AutoSize = true;
        chkDerin.Dock = DockStyle.Right;
        chkDerin.ForeColor = Renk.Turuncu;
        chkDerin.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        chkDerin.CheckedChanged += DerinDegisti;
        temizBaslik.Controls.Add(baslik1);
        temizBaslik.Controls.Add(chkDerin);

        temizlikListesi.Columns.Add("Temizlenecek", 280);
        temizlikListesi.Columns.Add("Açıklama", 300);
        temizlikListesi.Columns.Add("Boyut", 90, HorizontalAlignment.Right);
        temizlikListesi.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        temizlikListesi.Dock = DockStyle.Fill;
        temizlikListesi.Resize += delegate { SutunDoldur(temizlikListesi, 1); };
        temizlikListesi.ItemCheck += delegate(object s, ItemCheckEventArgs e)
        {
            if (dolduruluyor) return;
            var t = (Temizlik)temizlikListesi.Items[e.Index].Tag;
            if (t.AcikSurec != null && e.NewValue == CheckState.Checked)
            {
                e.NewValue = CheckState.Unchecked;
                lblDurum.Text = string.Format("Önce {0} programını kapat, sonra \"Yeniden tara\"ya bas.", t.AcikSurec);
            }
        };
        temizlikListesi.ItemChecked += delegate(object s, ItemCheckedEventArgs e)
        {
            if (dolduruluyor) return;
            secimler[((Temizlik)e.Item.Tag).Anahtar] = e.Item.Checked;
            ToplamiYaz();
        };
        var temizKutu = new Panel { Dock = DockStyle.Top, Height = 290, Padding = new Padding(12, 0, 12, 0) };
        temizKutu.Controls.Add(temizlikListesi);

        var temizSerit = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(12, 8, 12, 4) };
        btnTemizle = Dugme("Seçilenleri temizle", 0, true);
        btnTemizle.Dock = DockStyle.Fill;
        btnTemizle.Click += Temizle_Click;
        btnTara = Dugme("Yeniden tara", 150, false);
        btnTara.Click += async delegate { await TaraAsync(); };
        temizSerit.Controls.Add(btnTemizle);
        temizSerit.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
        temizSerit.Controls.Add(btnTara);

        var analizSerit = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(14, 14, 12, 6) };
        btnAnaliz = Dugme(analizSurucu + " diskini tara", 140, false);
        btnAnaliz.Click += Analiz_Click;
        btnUst = Dugme("↑ Üst klasör", 110, false);
        btnUst.Enabled = false;
        btnUst.Click += delegate { if (simdiki != null && simdiki.Ust != null) { simdiki = simdiki.Ust; AnaliziGoster(); } };
        btnDosyalar = Dugme("En büyük dosyalar", 150, false);
        btnDosyalar.Enabled = false;
        btnDosyalar.Click += delegate { dosyaGorunumu = !dosyaGorunumu; AnaliziGoster(); };
        btnAlgilayici = Dugme("Depolama Algılayıcısı", 170, false);
        btnAlgilayici.Click += delegate { try { Process.Start("ms-settings:storagesense"); } catch { } };
        analizSerit.Controls.Add(Baslik("DEPOLAMA ANALİZİ", "Hiçbir şey silmez; yer kaplayanı gösterir."));
        foreach (var b in new[] { btnAnaliz, btnUst, btnDosyalar, btnAlgilayici })
        {
            analizSerit.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 6 });
            analizSerit.Controls.Add(b);
        }

        lblYol.Dock = DockStyle.Top;
        lblYol.Height = 24;
        lblYol.Padding = new Padding(14, 4, 12, 0);
        lblYol.ForeColor = Renk.Soluk;
        lblYol.AutoEllipsis = true;
        lblYol.Text = BosYolMetni();

        analizListesi.CheckBoxes = false;
        analizListesi.Columns.Add("Ad", 400);
        analizListesi.Columns.Add("Boyut", 100, HorizontalAlignment.Right);
        analizListesi.Columns.Add("Oran", 70, HorizontalAlignment.Right);
        analizListesi.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        analizListesi.Dock = DockStyle.Fill;
        analizListesi.Resize += delegate { SutunDoldur(analizListesi, 0); };
        analizListesi.DoubleClick += delegate { AnalizOgesiniAc(); };
        var menu = new ContextMenuStrip();
        var mGoster = new ToolStripMenuItem("Gezgin'de göster");
        mGoster.Click += delegate { GezgindeGoster(); };
        menu.Items.Add(mGoster);
        menu.Opening += delegate(object s, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = analizListesi.SelectedItems.Count == 0 || analizListesi.SelectedItems[0].Tag == null;
        };
        analizListesi.ContextMenuStrip = menu;
        var analizKutu = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
        analizKutu.Controls.Add(analizListesi);

        lblDurum.Dock = DockStyle.Bottom;
        lblDurum.Height = 34;
        lblDurum.Padding = new Padding(14, 0, 12, 0);
        lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        lblDurum.ForeColor = Renk.Soluk;

        ilerleme.Tick += delegate { lblDurum.Text = string.Format("{0} diski taranıyor... {1:N0} dosya", analizSurucu, taranan); };

        Controls.Add(analizKutu);
        Controls.Add(lblYol);
        Controls.Add(analizSerit);
        Controls.Add(temizSerit);
        Controls.Add(temizKutu);
        Controls.Add(temizBaslik);
        Controls.Add(suruculer);
        Controls.Add(lblDurum);

        // Küçük pencerede temizlik listesi kısalır; depolama analizine her zaman en az ~180 px kalsın.
        Resize += delegate
        {
            int sabit = suruculer.Height + temizBaslik.Height + temizSerit.Height + analizSerit.Height + lblYol.Height + lblDurum.Height;
            temizKutu.Height = Math.Max(130, Math.Min(290, ClientSize.Height - sabit - 180));
        };
    }

    static Label Baslik(string baslik, string alt)
    {
        return new Label
        {
            Dock = DockStyle.Fill, ForeColor = Renk.Soluk, AutoEllipsis = true,
            Text = baslik + "\n" + alt, Font = new Font("Segoe UI", 8.5f)
        };
    }

    static Button Dugme(string metin, int genislik, bool vurgu)
    {
        var b = new Button
        {
            Text = metin, Dock = DockStyle.Right, Width = genislik, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
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
        Sigdir(l, sutun, false);
        try { if (l.IsHandleCreated) l.BeginInvoke(new Action(() => Sigdir(l, sutun, true))); } catch { }
    }

    static void Sigdir(ListView l, int sutun, bool zorla)
    {
        int kalan = l.ClientSize.Width;
        for (int i = 0; i < l.Columns.Count; i++) if (i != sutun) kalan -= l.Columns[i].Width;
        if (kalan <= 120) return;
        // Yatay çubuk bir kez çıkınca genişlik tam sığsa da kalabiliyor: zorlarken bir piksel oynatılıp yeniden yazılır.
        if (zorla && l.Columns[sutun].Width == kalan) l.Columns[sutun].Width = kalan - 1;
        if (l.Columns[sutun].Width != kalan) l.Columns[sutun].Width = kalan;
    }

    public async void Gosterildi()
    {
        if (!ilkGosterim) return;
        ilkGosterim = false;
        SuruculeriGoster();
        await TaraAsync();
    }

    void SuruculeriGoster()
    {
        var diskler = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady).Take(4).ToList();
        // Seçili disk artık yoksa (çıkarılmış) sistem diskine dön.
        if (!diskler.Any(d => d.Name.TrimEnd('\\').Equals(analizSurucu, StringComparison.OrdinalIgnoreCase))) analizSurucu = SanalBellek.Surucu;
        suruculer.SuspendLayout();
        suruculer.Controls.Clear();
        suruculer.ColumnStyles.Clear();
        suruculer.ColumnCount = Math.Max(1, diskler.Count);
        for (int i = 0; i < diskler.Count; i++)
        {
            var d = diskler[i];
            suruculer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / diskler.Count));
            const double GB = 1073741824.0;
            double top = d.TotalSize / GB, bos = d.AvailableFreeSpace / GB;
            string ad = string.IsNullOrEmpty(d.VolumeLabel) ? "Yerel disk" : d.VolumeLabel;
            string harf = d.Name.TrimEnd('\\');
            var g = new Gosterge(harf + " DİSKİ", string.Format("{0} · boş {1:0.0} GB", ad, bos));
            g.Ayarla(string.Format("{0:0} / {1:0} GB", top - bos, top), (top - bos) / top);
            if (diskler.Count > 1)
            {
                // Birden çok disk varsa göstergeye tıklayınca depolama analizi o diski tarar.
                g.Tag = new[] { harf, g.Alt };
                g.Cursor = Cursors.Hand;
                g.Click += delegate { if (!calisiyor) { analizSurucu = harf; AnalizSurucusunuYaz(); } };
            }
            suruculer.Controls.Add(g, i, 0);
        }
        suruculer.ResumeLayout();
        AnalizSurucusunuYaz();
    }

    string BosYolMetni()
    {
        return "\"" + analizSurucu + " diskini tara\"ya bas: en çok yer kaplayan klasörler burada listelenir." +
               (suruculer.Controls.Count > 1 ? " Başka bir disk için üstteki göstergesine tıkla." : "");
    }

    // Düğme, açıklama ve disk göstergelerindeki "analiz için seçili" işareti seçili diske göre yazılır.
    void AnalizSurucusunuYaz()
    {
        btnAnaliz.Text = analizSurucu + " diskini tara";
        if (kok == null) lblYol.Text = BosYolMetni();
        foreach (var g in suruculer.Controls.OfType<Gosterge>())
        {
            var t = g.Tag as string[];
            if (t == null) continue;
            g.Alt = t[1] + (t[0].Equals(analizSurucu, StringComparison.OrdinalIgnoreCase) ? " · analiz için seçili" : "");
            g.Invalidate();
        }
    }

    // Başka yerden (optimize düğmeleri) temizlik yapıldıktan sonra; sekme hiç açılmadıysa bir şey yapmaz.
    public async void Yenile()
    {
        if (ilkGosterim || calisiyor) return;
        SuruculeriGoster();
        await TaraAsync();
    }

    // Temizlik / tarama sürüyor mu (optimize düğmeleri aynı klasörlere aynı anda dokunmasın).
    public bool Calisiyor { get { return calisiyor; } }

    public static long BosAlan()
    {
        long t = 0;
        foreach (var d in DriveInfo.GetDrives())
            try { if (d.DriveType == DriveType.Fixed && d.IsReady) t += d.AvailableFreeSpace; } catch { }
        return t;
    }

    void Mesgul(bool mesgul, string mesaj)
    {
        calisiyor = mesgul;
        foreach (var b in new Control[] { btnTemizle, btnTara, btnAnaliz, chkDerin }) b.Enabled = !mesgul;
        btnUst.Enabled = !mesgul && !dosyaGorunumu && simdiki != null && simdiki.Ust != null;
        btnDosyalar.Enabled = !mesgul && kok != null;
        if (mesaj != null) lblDurum.Text = mesaj;
    }

    async Task TaraAsync()
    {
        if (calisiyor) return;
        Mesgul(true, "Gereksiz dosyalar hesaplanıyor...");
        bool derin = chkDerin.Checked;
        temizlikler = await Task.Run(() =>
        {
            var l = DiskTemizlik.Liste(derin);
            foreach (var t in l) DiskTemizlik.Hesapla(t);
            return l;
        });
        ListeyiDoldur();
        Mesgul(false, null);
        ToplamiYaz();
    }

    void ListeyiDoldur()
    {
        dolduruluyor = true;
        temizlikListesi.BeginUpdate();
        temizlikListesi.Items.Clear();
        foreach (var t in temizlikler)
        {
            var oge = new ListViewItem(t.Ad + (t.Yonetici ? "  (yönetici)" : "")) { Tag = t };
            oge.SubItems.Add(t.AcikSurec != null ? string.Format("{0} açık; kapatıp \"Yeniden tara\"ya bas", t.AcikSurec) : t.Aciklama);
            oge.SubItems.Add(t.Boyut < 0 ? "?" : AnaForm.Boyut(t.Boyut));
            bool sec;
            if (!secimler.TryGetValue(t.Anahtar, out sec)) sec = t.Varsayilan;
            oge.Checked = sec && t.AcikSurec == null;
            oge.ForeColor = t.AcikSurec != null ? Renk.Soluk : t.Derin ? Renk.Turuncu : Renk.Yazi;
            temizlikListesi.Items.Add(oge);
        }
        temizlikListesi.EndUpdate();
        dolduruluyor = false;
        SutunDoldur(temizlikListesi, 1);
    }

    List<Temizlik> Secililer()
    {
        return temizlikListesi.Items.Cast<ListViewItem>().Where(o => o.Checked).Select(o => (Temizlik)o.Tag)
            .Where(t => t.AcikSurec == null).ToList();
    }

    void ToplamiYaz()
    {
        if (calisiyor) return;
        var secili = Secililer();
        long toplam = secili.Where(t => t.Boyut > 0).Sum(t => t.Boyut);
        string ek = secili.Any(t => t.Boyut < 0) ? " (+ boyutu yönetici izniyle görünenler)" : "";
        lblDurum.Text = string.Format("Seçilenlerle yaklaşık {0} boşalır{1}.", AnaForm.Boyut(toplam), ek);
    }

    void DerinDegisti(object sender, EventArgs e)
    {
        if (derinDegisiyor) return;
        if (chkDerin.Checked && MessageBox.Show(FindForm(), DerinUyari, "Derin temizlik modu",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            derinDegisiyor = true;
            chkDerin.Checked = false;
            derinDegisiyor = false;
            return;
        }
        var bekleme = TaraAsync();
    }

    async void Temizle_Click(object sender, EventArgs e)
    {
        var secili = Secililer();
        if (secili.Count == 0) { lblDurum.Text = "Hiçbir şey seçilmedi."; return; }

        long toplam = secili.Where(t => t.Boyut > 0).Sum(t => t.Boyut);
        var metin = new StringBuilder("Şunlar kalıcı olarak silinecek (geri dönüşüm kutusuna gitmez):\n\n");
        foreach (var t in secili)
            metin.AppendFormat("•  {0}{1}\n", t.Ad, t.Boyut > 0 ? "  —  " + AnaForm.Boyut(t.Boyut) : "");
        metin.AppendFormat("\nYaklaşık {0} boşalacak.", AnaForm.Boyut(toplam));
        if (secili.Any(t => t.Yonetici)) metin.Append(" Windows kalıntıları için Windows bir kez yönetici izni isteyecek.");
        if (secili.Any(t => t.Ozel == "windows")) metin.Append("\n\nWindows'un kendi temizlik aracı da çalışacak; 10–30 dakika sürebilir, bu sırada kapatma.");
        if (secili.Any(t => t.Derin)) metin.Append("\n\nDerin mod açık: oyunlar ilk açılışta takılabilir, eski Windows'a dönülemez.");
        metin.Append("\n\nDevam edilsin mi?");
        if (MessageBox.Show(FindForm(), metin.ToString(), "Disk temizliği", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        Mesgul(true, "Temizleniyor...");
        long once = BosAlan();
        await Task.Run(() => { foreach (var t in secili.Where(x => !x.Yonetici)) { try { DiskTemizlik.Temizle(t); } catch { } } });

        string not = "";
        var yonetici = secili.Where(t => t.Yonetici).Select(t => t.Anahtar).ToList();
        if (yonetici.Count > 0)
        {
            lblDurum.Text = secili.Any(t => t.Ozel == "windows")
                ? "Windows kalıntıları temizleniyor; Windows'un aracı uzun sürebilir..."
                : "Windows kalıntıları temizleniyor...";
            try
            {
                var bilgi = new ProcessStartInfo(Application.ExecutablePath,
                    "--disk-temizle " + (chkDerin.Checked ? "derin " : "normal ") + string.Join(" ", yonetici))
                {
                    UseShellExecute = true, Verb = "runas"
                };
                var p = Process.Start(bilgi);
                await Task.Run(() => { p.WaitForExit(); p.Dispose(); });
            }
            catch (System.ComponentModel.Win32Exception) { not = " Yönetici izni verilmediği için Windows kalıntıları atlandı."; }
        }

        long bosalan = Math.Max(0, BosAlan() - once);
        Kazanc.Ekle(0, bosalan);
        Mesgul(false, null);
        SuruculeriGoster();
        await TaraAsync();
        lblDurum.ForeColor = Renk.Yesil;
        lblDurum.Text = string.Format("Temizlik bitti: {0} yer açıldı. Kullanımdaki dosyalar atlandı.{1}", AnaForm.Boyut(bosalan), not);
        var bekle = new System.Windows.Forms.Timer { Interval = 10000 };
        bekle.Tick += delegate { bekle.Dispose(); lblDurum.ForeColor = Renk.Soluk; };
        bekle.Start();
    }

    async void Analiz_Click(object sender, EventArgs e)
    {
        Mesgul(true, analizSurucu + " diski taranıyor...");
        taranan = 0;
        ilerleme.Start();
        string kokYol = analizSurucu + "\\";
        var dosyalar = new List<Dugum>();
        var sonuc = await Task.Run(() => KlasorTara(new DirectoryInfo(kokYol), null, dosyalar));
        ilerleme.Stop();
        kok = simdiki = sonuc;
        buyukDosyalar = dosyalar.OrderByDescending(f => f.Boyut).Take(50).ToList();
        dosyaGorunumu = false;
        Mesgul(false, string.Format("{0:N0} dosya tarandı. Klasöre çift tıkla: içine gir. Sağ tık: Gezgin'de göster. " +
                                    "Yönetici izni olmadan bazı sistem klasörleri okunamaz.", taranan));
        AnaliziGoster();
    }

    // Bağlantı noktalarına girmez (aksi hâlde aynı klasörler iki kez sayılır ya da döngüye girer).
    Dugum KlasorTara(DirectoryInfo d, Dugum ust, List<Dugum> dosyalar)
    {
        var n = new Dugum { Ad = ust == null ? d.FullName : d.Name, Yol = d.FullName, Ust = ust };
        try
        {
            foreach (var o in d.EnumerateFileSystemInfos())
            {
                if ((o.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                var alt = o as DirectoryInfo;
                if (alt != null)
                {
                    var c = KlasorTara(alt, n, dosyalar);
                    n.Alt.Add(c);
                    n.Boyut += c.Boyut;
                    continue;
                }
                long b = ((FileInfo)o).Length;
                n.DosyaBoyutu += b;
                n.Boyut += b;
                Interlocked.Increment(ref taranan);
                if (b >= 100L * 1048576) dosyalar.Add(new Dugum { Ad = o.Name, Yol = o.FullName, Boyut = b, Klasor = false, Ust = n });
            }
        }
        catch { }   // okunamayan klasör: o ana kadar sayılanla devam
        return n;
    }

    void AnaliziGoster()
    {
        if (kok == null) return;
        analizListesi.BeginUpdate();
        analizListesi.Items.Clear();
        long tam;
        if (dosyaGorunumu)
        {
            lblYol.Text = "En büyük dosyalar (100 MB üstü), " + kok.Yol + " içinde";
            tam = kok.Boyut;
            foreach (var f in buyukDosyalar) AnalizSatiri(f.Yol, f.Boyut, tam, f);
        }
        else
        {
            lblYol.Text = simdiki.Yol + "   ·   " + AnaForm.Boyut(simdiki.Boyut);
            tam = simdiki.Boyut;
            var satirlar = simdiki.Alt.Select(a => new KeyValuePair<long, Dugum>(a.Boyut, a)).ToList();
            satirlar.Add(new KeyValuePair<long, Dugum>(simdiki.DosyaBoyutu, null));
            foreach (var s in satirlar.OrderByDescending(x => x.Key))
                if (s.Key > 0) AnalizSatiri(s.Value == null ? "(bu klasördeki dosyalar)" : s.Value.Ad, s.Key, tam, s.Value);
        }
        analizListesi.EndUpdate();
        SutunDoldur(analizListesi, 0);
        btnDosyalar.Text = dosyaGorunumu ? "Klasör görünümü" : "En büyük dosyalar";
        btnUst.Enabled = !calisiyor && !dosyaGorunumu && simdiki.Ust != null;
    }

    void AnalizSatiri(string ad, long boyut, long tam, Dugum d)
    {
        var oge = new ListViewItem(ad) { Tag = d };
        oge.SubItems.Add(AnaForm.Boyut(boyut));
        oge.SubItems.Add(tam > 0 ? string.Format("%{0:0}", 100.0 * boyut / tam) : "");
        if (d != null && !d.Klasor) oge.ForeColor = Renk.Soluk;
        analizListesi.Items.Add(oge);
    }

    void AnalizOgesiniAc()
    {
        if (analizListesi.SelectedItems.Count == 0) return;
        var d = analizListesi.SelectedItems[0].Tag as Dugum;
        if (d == null) return;
        if (d.Klasor && !dosyaGorunumu) { simdiki = d; AnaliziGoster(); }
        else GezgindeGoster();
    }

    void GezgindeGoster()
    {
        if (analizListesi.SelectedItems.Count == 0) return;
        var d = analizListesi.SelectedItems[0].Tag as Dugum;
        if (d == null) return;
        try { Process.Start("explorer.exe", d.Klasor ? "\"" + d.Yol + "\"" : "/select,\"" + d.Yol + "\""); } catch { }
    }
}
