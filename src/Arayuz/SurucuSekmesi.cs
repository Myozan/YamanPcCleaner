// Surucu merkezi sekmesi.

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

class SurucuSekmesi : Panel
{
    public event Action<bool> GuncellemeDurumu;   // sekme düğmesinde "yeni" işareti için

    readonly Label lblKart = new Label(), lblKurulu = new Label(), lblSon = new Label(), lblSonuc = new Label(), lblDurum = new Label();
    readonly Gosterge gIndirme = new Gosterge("İNDİRME", "Dosya İndirilenler klasörüne kaydediliyor");
    readonly Panel indirmeKutusu = new Panel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(8, 6, 8, 6), Visible = false };
    readonly Button btnKur, btnKontrol, btnNotlar, btnIptal;
    NvidiaBilgi bilgi;
    WebClient indirici;
    DateTime indirmeBasi;
    bool calisiyor, kontrolEdildi;

    public SurucuSekmesi()
    {
        Dock = DockStyle.Fill;
        BackColor = Renk.Arka;

        var kart = new Panel { Dock = DockStyle.Top, Height = 176, BackColor = Renk.Panel, Padding = new Padding(16, 12, 16, 10) };
        var kartDis = new Panel { Dock = DockStyle.Top, Height = 192, Padding = new Padding(12, 12, 12, 4) };
        var baslik = new Label { Dock = DockStyle.Top, Height = 20, ForeColor = Renk.Soluk, Text = "EKRAN KARTI SÜRÜCÜSÜ · NVIDIA", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        lblKart.Font = new Font("Segoe UI Semibold", 15f);
        lblKart.Height = 36;
        lblSonuc.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
        lblSonuc.Height = 34;
        foreach (var l in new[] { lblKurulu, lblSon }) { l.Height = 24; l.ForeColor = Renk.Yazi; }
        foreach (var l in new[] { lblSonuc, lblSon, lblKurulu, lblKart }) { l.Dock = DockStyle.Top; l.AutoEllipsis = true; kart.Controls.Add(l); }
        kart.Controls.Add(baslik);
        kartDis.Controls.Add(kart);

        var dugmeler = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(12, 8, 12, 4) };
        btnKur = DiskSekmesiDugme("İndir ve kur", true);
        btnKur.Dock = DockStyle.Fill;
        btnKur.Click += Kur_Click;
        btnIptal = DiskSekmesiDugme("İptal", false);
        btnIptal.Width = 110;
        btnIptal.Visible = false;
        btnIptal.Click += delegate { if (indirici != null) indirici.CancelAsync(); };
        btnNotlar = DiskSekmesiDugme("Sürüm notları", false);
        btnNotlar.Width = 140;
        btnNotlar.Click += delegate { if (bilgi != null && bilgi.DetayAdresi != null) try { Process.Start(bilgi.DetayAdresi); } catch { } };
        btnKontrol = DiskSekmesiDugme("Yeniden kontrol et", false);
        btnKontrol.Width = 160;
        btnKontrol.Click += delegate { Kontrol(); };
        dugmeler.Controls.Add(btnKur);
        foreach (var b in new[] { btnIptal, btnNotlar, btnKontrol })
        {
            dugmeler.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
            dugmeler.Controls.Add(b);
        }

        indirmeKutusu.Controls.Add(gIndirme);

        var aciklama = new Label
        {
            Dock = DockStyle.Top, Height = 150, Padding = new Padding(16, 10, 16, 0), ForeColor = Renk.Soluk,
            Text = "Nasıl çalışır?\n" +
                   "•  Kurulu sürüm, NVIDIA'nın sitesindeki en son Game Ready sürücüsüyle karşılaştırılır.\n" +
                   "•  NVIDIA App (ya da GeForce Experience) kuruluysa güncellemeyi o yapar; bu program ayrıca indirmez, iki kez kurulmaz.\n" +
                   "•  NVIDIA App yoksa sürücü yalnızca NVIDIA'nın sitesinden indirilir; imzası NVIDIA değilse çalıştırılmaz.\n" +
                   "•  Kurmadan önce sistem geri yükleme noktası istenir; sonra NVIDIA'nın kendi kurulum ekranı açılır.\n" +
                   "•  Kurulumda ekran birkaç kez kararabilir: önce oyunları kapat. Sorun olursa Aygıt Yöneticisi'nden \"Sürücüyü geri al\"."
        };

        lblDurum.Dock = DockStyle.Bottom;
        lblDurum.Height = 34;
        lblDurum.Padding = new Padding(14, 0, 12, 0);
        lblDurum.TextAlign = ContentAlignment.MiddleLeft;
        lblDurum.ForeColor = Renk.Soluk;

        Controls.Add(aciklama);
        Controls.Add(indirmeKutusu);
        Controls.Add(dugmeler);
        Controls.Add(kartDis);
        Controls.Add(lblDurum);
        Goster();
    }

    static Button DiskSekmesiDugme(string metin, bool vurgu)
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

    public void Gosterildi() { if (!kontrolEdildi) Kontrol(); }

    // Genel Bakış için: son denetimin sonucu (denetim sürerken ya da hiç yapılmadıysa Denetlendi false).
    public bool Denetlendi { get { return kontrolEdildi && !calisiyor; } }
    public NvidiaBilgi SonBilgi { get { return bilgi; } }

    public async void Kontrol()
    {
        if (calisiyor || indirici != null) return;
        calisiyor = true;
        kontrolEdildi = true;
        lblDurum.Text = "NVIDIA'ya soruluyor...";
        Goster();
        bilgi = await Task.Run(() => NvidiaSurucu.Kontrol());
        calisiyor = false;
        lblDurum.Text = "";
        Goster();
        if (GuncellemeDurumu != null) GuncellemeDurumu(bilgi != null && bilgi.YeniVar);
    }

    void Goster()
    {
        btnKontrol.Enabled = !calisiyor && indirici == null;
        btnNotlar.Enabled = bilgi != null && bilgi.DetayAdresi != null;
        btnKur.Enabled = false;
        btnKur.Text = "İndir ve kur";
        btnKur.BackColor = Renk.Panel;
        btnKur.ForeColor = Renk.Soluk;
        if (calisiyor && bilgi == null) { lblKart.Text = "Kontrol ediliyor..."; lblKurulu.Text = lblSon.Text = lblSonuc.Text = ""; return; }
        if (!kontrolEdildi) { lblKart.Text = "Henüz kontrol edilmedi"; lblKurulu.Text = lblSon.Text = lblSonuc.Text = ""; return; }
        if (bilgi == null)
        {
            lblKart.Text = "NVIDIA ekran kartı bulunamadı";
            lblKurulu.Text = "Bu bölüm yalnızca NVIDIA kartlar için.";
            lblSon.Text = "Intel ya da AMD için üreticinin kendi güncelleme aracını kullan.";
            lblSonuc.Text = "";
            return;
        }
        lblKart.Text = bilgi.KartAdi;
        lblKurulu.Text = string.Format("Kurulu sürüm:   {0}   ({1})", bilgi.KuruluSurum, bilgi.KuruluTarih);
        if (bilgi.Hata != null)
        {
            lblSon.Text = "En son sürüm:   öğrenilemedi";
            lblSonuc.Text = bilgi.Hata;
            lblSonuc.ForeColor = Renk.Turuncu;
            return;
        }
        DateTime t;
        string tarih = DateTime.TryParseExact(bilgi.SonTarih, "ddd MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out t)
            ? t.ToString("dd.MM.yyyy") : bilgi.SonTarih;
        lblSon.Text = string.Format("En son sürüm:   {0}   ({1} · {2})", bilgi.SonSurum, tarih, bilgi.Boyut);
        bool yeni = bilgi.YeniVar;
        lblSonuc.Text = yeni ? "Yeni sürüm var: " + bilgi.SonSurum : "Sürücün güncel.";
        lblSonuc.ForeColor = yeni ? Renk.Turuncu : Renk.Yesil;
        btnKur.Enabled = yeni && indirici == null;
        if (!yeni) btnKur.Text = "Güncelleme gerekmiyor";
        else if (bilgi.Uygulama != null) btnKur.Text = Path.GetFileNameWithoutExtension(bilgi.Uygulama) + "'te güncelle";
        btnKur.BackColor = btnKur.Enabled ? Renk.Vurgu : Renk.Panel;
        btnKur.ForeColor = btnKur.Enabled ? Color.White : Renk.Soluk;
    }

    async void Kur_Click(object sender, EventArgs e)
    {
        if (bilgi == null || !bilgi.YeniVar) return;

        // NVIDIA App / GeForce Experience varsa güncellemeyi o yapsın: aynı sürücü iki yoldan inmesin.
        if (bilgi.Uygulama != null)
        {
            try { Process.Start(new ProcessStartInfo(bilgi.Uygulama) { UseShellExecute = true }); } catch { }
            MessageBox.Show(FindForm(), string.Format("{0} açılıyor. \"Sürücüler\" bölümünde {1} sürümü için \"İndir\"e, sonra \"Hızlı kurulum\"a bas.\n\n" +
                "Bitince buraya dönüp \"Yeniden kontrol et\"e basarsan güncel olduğunu görürsün.",
                Path.GetFileNameWithoutExtension(bilgi.Uygulama), bilgi.SonSurum), "Sürücü güncelleme", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Uri adres;
        if (!Uri.TryCreate(bilgi.IndirmeAdresi, UriKind.Absolute, out adres) || adres.Scheme != Uri.UriSchemeHttps
            || !adres.Host.EndsWith(".nvidia.com", StringComparison.OrdinalIgnoreCase))
        {
            lblDurum.Text = "İndirme adresi NVIDIA'ya ait görünmüyor; hiçbir şey indirilmedi.";
            return;
        }
        string klasor = IndirilenlerKlasoru();
        string yol = Path.Combine(klasor, Path.GetFileName(adres.LocalPath));

        if (MessageBox.Show(FindForm(), string.Format(
                "NVIDIA {0} sürücüsü indirilecek ({1}) ve kurulumu başlatılacak.\n\n" +
                "•  Dosya İndirilenler klasörüne kaydedilir.\n" +
                "•  İmzası NVIDIA'ya ait değilse çalıştırılmaz.\n" +
                "•  Kurmadan önce sistem geri yükleme noktası istenir.\n" +
                "•  Kurulumda ekran birkaç kez kararabilir; önce oyunları kapat.\n\n" +
                "Windows yönetici izni isteyecek. Devam edilsin mi?", bilgi.SonSurum, bilgi.Boyut),
                "Sürücü güncelleme", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        Directory.CreateDirectory(klasor);
        if (!File.Exists(yol) && !await Indir(adres, yol)) return;

        btnKur.Enabled = btnKontrol.Enabled = false;
        lblDurum.Text = "İmza kontrol ediliyor...";
        if (!await Task.Run(() => NvidiaSurucu.ImzaNvidia(yol)))
        {
            try { File.Delete(yol); } catch { }
            Goster();
            lblDurum.Text = "";
            MessageBox.Show(FindForm(), "İndirilen dosyanın imzası NVIDIA'ya ait değil ya da dosya bozuk; silindi. Hiçbir şey kurulmadı.",
                "Sürücü güncelleme", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Pencere tepsideyken (oyun sırasında) indirme bittiyse yönetici izni ve kurulum oyunun ortasında açılmasın.
        var f = FindForm();
        if (f == null || !f.Visible)
        {
            Goster();
            lblDurum.Text = "İndirme bitti; kurmak için \"İndir ve kur\"a yeniden bas.";
            return;
        }

        lblDurum.Text = "Geri yükleme noktası oluşturuluyor, ardından NVIDIA kurulumu açılacak...";
        int kod = -1;
        try
        {
            var bilgiP = new ProcessStartInfo(Application.ExecutablePath, "--nvidia-kur \"" + yol + "\"") { UseShellExecute = true, Verb = "runas" };
            var p = Process.Start(bilgiP);
            await Task.Run(() => { p.WaitForExit(); kod = p.ExitCode; p.Dispose(); });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            lblDurum.Text = "Yönetici izni verilmedi; kurulum başlamadı. Dosya İndirilenler klasöründe duruyor.";
            Goster();
            return;
        }
        Goster();
        lblDurum.Text = "";
        string metin;
        if (kod == 0 || kod == 3)
        {
            metin = "NVIDIA kurulum ekranı açıldı. \"NVIDIA Grafik Sürücüsü\"nü seç ve \"Hızlı\" ile devam et. " +
                    "Bitince bilgisayarı yeniden başlat, sonra buradan \"Yeniden kontrol et\"e bas.";
            if (kod == 3) metin += "\n\nGeri yükleme noktası oluşturulamadı (Sistem Geri Yükleme kapalı olabilir). " +
                                   "Sorun çıkarsa Aygıt Yöneticisi'nden \"Sürücüyü geri al\" kullanılabilir.";
        }
        else if (kod == 2) metin = "Dosyanın imzası doğrulanamadı; kurulum başlatılmadı.";
        else metin = "NVIDIA kurulumu başlatılamadı. Dosya İndirilenler klasöründe; elle açabilirsin.";
        MessageBox.Show(FindForm(), metin, "Sürücü güncelleme", MessageBoxButtons.OK, kod == 0 || kod == 3 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    [DllImport("shell32.dll")]
    static extern int SHGetKnownFolderPath(ref Guid kimlik, uint bayrak, IntPtr belirtec, out IntPtr yol);

    const string IndirilenlerKimligi = "{374DE290-123F-4565-9164-39C4925E467B}";   // FOLDERID_Downloads

    // Kullanıcının gerçek İndirilenler klasörü: taşınmış (D:\İndirilenler) ya da OneDrive / ilkeyle yönlendirilmiş
    // olabilir. Önce Known Folder API, sonra User Shell Folders kaydı, en son profil\Downloads.
    static string IndirilenlerKlasoru()
    {
        IntPtr p = IntPtr.Zero;
        try
        {
            var kimlik = new Guid(IndirilenlerKimligi);
            if (SHGetKnownFolderPath(ref kimlik, 0, IntPtr.Zero, out p) == 0)
            {
                string s = Marshal.PtrToStringUni(p);
                if (!string.IsNullOrEmpty(s)) return s;
            }
        }
        catch { }
        finally { if (p != IntPtr.Zero) Marshal.FreeCoTaskMem(p); }
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))
            {
                string s = k == null ? null : k.GetValue(IndirilenlerKimligi, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
                if (!string.IsNullOrEmpty(s)) return Environment.ExpandEnvironmentVariables(s);
            }
        }
        catch { }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    // .part dosyasına indirir, bitince asıl adına taşır; yarım dosya asla "hazır" sanılmaz.
    Task<bool> Indir(Uri adres, string yol)
    {
        var sonuc = new TaskCompletionSource<bool>();
        string gecici = yol + ".part";
        indirici = new WebClient();
        indirici.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 YamanPcCleaner";
        indirmeBasi = DateTime.UtcNow;
        indirmeKutusu.Visible = btnIptal.Visible = true;
        gIndirme.Ayarla("Başlıyor...", 0);
        Goster();
        lblDurum.Text = "İndiriliyor...";
        indirici.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs e)
        {
            double mb = e.BytesReceived / 1048576.0, top = e.TotalBytesToReceive / 1048576.0;
            double hiz = mb / Math.Max(0.5, (DateTime.UtcNow - indirmeBasi).TotalSeconds);
            gIndirme.Ayarla(string.Format("{0:0} / {1:0} MB · {2:0.0} MB/sn", mb, top, hiz), top > 0 ? mb / top : 0);
        };
        indirici.DownloadFileCompleted += delegate(object s, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            indirici.Dispose();
            indirici = null;
            indirmeKutusu.Visible = btnIptal.Visible = false;
            bool tamam = false;
            if (e.Cancelled || e.Error != null)
            {
                try { File.Delete(gecici); } catch { }
                lblDurum.Text = e.Cancelled ? "İndirme iptal edildi." : "İndirme başarısız: " + e.Error.Message;
            }
            else
            {
                try { if (File.Exists(yol)) File.Delete(yol); File.Move(gecici, yol); tamam = true; }
                catch (Exception x) { lblDurum.Text = "İndirilen dosya kaydedilemedi: " + x.Message; }
            }
            Goster();
            sonuc.SetResult(tamam);
        };
        try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; indirici.DownloadFileAsync(adres, gecici); }
        catch (Exception x)
        {
            indirici.Dispose(); indirici = null;
            indirmeKutusu.Visible = btnIptal.Visible = false;
            lblDurum.Text = "İndirme başlatılamadı: " + x.Message;
            Goster();
            sonuc.SetResult(false);
        }
        return sonuc.Task;
    }

    public void Kapaniyor() { if (indirici != null) indirici.CancelAsync(); }
}
