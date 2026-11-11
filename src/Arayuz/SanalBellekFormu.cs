// Sanal bellek secim penceresi.

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

// "Sanal bellek ayarı" seçim penceresi. Sonuç: Secim = "boyut" (IlkMB / EnCokMB), "otomatik" (Windows yönetsin)
// ya da "geri" (programdan önceki ayar). Boyutlar sistem sürücüsündeki sayfa dosyası içindir.
class SanalBellekFormu : Form
{
    public string Secim;
    public int IlkMB, EnCokMB;

    readonly FlowLayoutPanel akis = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
        Padding = new Padding(16, 4, 16, 0)
    };
    readonly List<RadioButton> secenekler = new List<RadioButton>();
    readonly Dictionary<RadioButton, int[]> boyutlar = new Dictionary<RadioButton, int[]>();
    RadioButton rbElle, rbOtomatik, rbGeri;
    NumericUpDown sayIlk, sayEnCok;
    readonly long ramMB, suankiMB, bosMB;

    public SanalBellekFormu(string[] satirlar, long ramMB, long suankiMB, long bosMB, int hedefIlk, int hedefEnCok, string[] yedek)
    {
        this.ramMB = ramMB; this.suankiMB = suankiMB; this.bosMB = bosMB;
        Text = "Sanal bellek ayarı";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Renk.Arka;
        ForeColor = Renk.Yazi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(580, 690);
        Icon = Logo.Simge();

        var ust = new Label
        {
            Dock = DockStyle.Top, Height = 92, Padding = new Padding(16, 12, 16, 0), ForeColor = Renk.Soluk,
            Text = string.Format("Şu anki ayar: {0}\nRAM {1:0.#} GB + sayfa dosyası {2:0.#} GB = sanal bellek sınırı {3:0.#} GB\n" +
                                 "Sınır dolunca programlar \"bellek yetersiz\" deyip kapanır. Bir boyut seç; yeniden başlatınca geçerli olur.",
                                 SanalBellek.Anlat(satirlar), ramMB / 1024.0, suankiMB / 1024.0, (ramMB + suankiMB) / 1024.0)
        };

        // Önerilen en üstte; hazır boyutlardan onunla aynı olan tekrar eklenmez.
        var hazir = new List<int[]> { new[] { hedefIlk / 1024, hedefEnCok / 1024 } };
        foreach (var b in new[] { new[] { 8, 16 }, new[] { 16, 32 }, new[] { 24, 48 }, new[] { 32, 64 } })
            if (b[0] != hazir[0][0]) hazir.Add(b);
        int elleIlk = SanalBellek.ElleIlkMB(satirlar);
        for (int i = 0; i < hazir.Count; i++)
        {
            int ilk = hazir[i][0] * 1024;
            int enCok = (int)Math.Max(ilk, Math.Min(hazir[i][1] * 1024L, (bosMB - 5120) / 1024 * 1024));
            bool yerVar = ilk <= bosMB - 10240;
            string aciklama;
            Color renk = Renk.Yazi;
            if (!yerVar) aciklama = string.Format("{0} diskinde yer yok (en az {1} GB boş yer gerekir).", SanalBellek.Surucu, ilk / 1024 + 10);
            else
            {
                aciklama = string.Format("Sınır {0:0.#} GB olur, gerekirse {1:0.#} GB'a çıkar. Diskte {2} GB yer kaplar.",
                    (ramMB + ilk) / 1024.0, (ramMB + enCok) / 1024.0, ilk / 1024);
                if (ilk < suankiMB - 512) { aciklama += " Şu ankinden küçük: sınır düşer."; renk = Renk.Turuncu; }
                if (ilk == elleIlk) aciklama += " (Şu anki ayar bu.)";
            }
            var rb = Secenek(string.Format("{0} GB, gerekirse {1} GB{2}", ilk / 1024, enCok / 1024, i == 0 ? "  (önerilen)" : ""),
                aciklama, yerVar, renk);
            boyutlar[rb] = new[] { ilk, enCok };
        }

        rbElle = Secenek("Elle gir", "", true, Renk.Yazi);
        var elle = rbElle.Parent;
        sayIlk = Sayi(hedefIlk / 1024, new Point(20, 30));
        sayEnCok = Sayi(hedefEnCok / 1024, new Point(196, 30));
        elle.Controls.Add(sayIlk);
        elle.Controls.Add(new Label { Text = "GB'tan başlasın,", AutoSize = true, Location = new Point(84, 33), ForeColor = Renk.Soluk });
        elle.Controls.Add(sayEnCok);
        elle.Controls.Add(new Label { Text = "GB'a kadar büyüsün", AutoSize = true, Location = new Point(260, 33), ForeColor = Renk.Soluk });
        foreach (var l in elle.Controls.OfType<Label>().Where(x => x.Text == "").ToList()) elle.Controls.Remove(l);
        sayIlk.ValueChanged += delegate { rbElle.Checked = true; if (sayEnCok.Value < sayIlk.Value) sayEnCok.Value = sayIlk.Value; };
        sayEnCok.ValueChanged += delegate { rbElle.Checked = true; };

        bool otomatikZaten = satirlar.Length > 0 && satirlar.All(s => s.Trim().StartsWith("?:"));
        rbOtomatik = Secenek("Windows otomatik yönetsin",
            otomatikZaten ? "Şu an zaten böyle."
                          : "Boyutu Windows ayarlar ve gerektikçe büyütür; büyürken anlık \"bellek yetersiz\" hatası görülebilir.",
            !otomatikZaten, Renk.Yazi);

        if (yedek != null)
            rbGeri = Secenek("Programdan önceki ayara dön", "Önceki ayar: " + SanalBellek.Anlat(yedek), true, Renk.Yazi);

        // Sanal belleği küçültmeyen ilk seçenek seçili gelir.
        var ilkSecilecek = secenekler.FirstOrDefault(r => r.Enabled && (!boyutlar.ContainsKey(r) || boyutlar[r][0] >= suankiMB - 512))
                        ?? secenekler.FirstOrDefault(r => r.Enabled);
        if (ilkSecilecek != null) ilkSecilecek.Checked = true;

        var not = new Label
        {
            Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(16, 6, 16, 0), ForeColor = Renk.Soluk,
            Font = new Font("Segoe UI", 8.75f), Text = "Windows yönetici izni isteyecek. Ayar bilgisayar yeniden başlayınca geçerli olur."
        };
        var alt = new Panel { Dock = DockStyle.Bottom, Height = 66, Padding = new Padding(16, 10, 16, 14) };
        var btnUygula = new EylemDugmesi("Uygula", EylemDugmesi.Tur.Birincil)
        {
            Dock = DockStyle.Fill, Ikon = "\uE73E"
        };
        btnUygula.Click += Uygula_Click;
        var btnVazgec = new EylemDugmesi("Vazgeç", EylemDugmesi.Tur.Ikincil)
        {
            Dock = DockStyle.Right, Width = 120, DialogResult = DialogResult.Cancel
        };
        alt.Controls.Add(btnUygula);
        alt.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
        alt.Controls.Add(btnVazgec);
        AcceptButton = btnUygula;
        CancelButton = btnVazgec;

        Controls.Add(akis);
        Controls.Add(ust);
        Controls.Add(not);
        Controls.Add(alt);
    }

    void Uygula_Click(object sender, EventArgs e)
    {
        var rb = secenekler.FirstOrDefault(r => r.Checked);
        if (rb == null) return;
        if (rb == rbOtomatik) Secim = "otomatik";
        else if (rb == rbGeri) Secim = "geri";
        else
        {
            Secim = "boyut";
            if (rb == rbElle)
            {
                IlkMB = (int)sayIlk.Value * 1024;
                EnCokMB = (int)Math.Max(sayIlk.Value, sayEnCok.Value) * 1024;
                if (IlkMB > bosMB - 10240 || EnCokMB > bosMB - 5120)
                {
                    MessageBox.Show(this, string.Format("{0} diskinde bu kadar yer yok. Kullanılabilir yer yaklaşık {1:0} GB; " +
                        "başlangıç boyutu bundan en az 10 GB, en büyük boyut en az 5 GB küçük olmalı.", SanalBellek.Surucu, bosMB / 1024.0),
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else { IlkMB = boyutlar[rb][0]; EnCokMB = boyutlar[rb][1]; }
            if (IlkMB < suankiMB - 512 && MessageBox.Show(this, string.Format(
                    "Bu ayar sanal bellek sınırını {0:0.#} GB'tan {1:0.#} GB'a düşürür. Oyunlarda \"bellek yetersiz\" hatası artabilir.\n\nYine de uygulansın mı?",
                    (ramMB + suankiMB) / 1024.0, (ramMB + IlkMB) / 1024.0), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        }
        DialogResult = DialogResult.OK;
    }

    RadioButton Secenek(string baslik, string aciklama, bool etkin, Color renk)
    {
        var p = new Panel { Width = 530, Height = 62, Margin = new Padding(0, 0, 0, 4) };
        var r = new RadioButton
        {
            Text = baslik, AutoSize = true, Location = new Point(0, 4), Enabled = etkin, AutoCheck = false,
            ForeColor = Renk.Yazi, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        // Her seçenek ayrı panelde olduğu için gruplamayı burada yapıyoruz.
        r.Click += delegate { r.Checked = true; };
        r.CheckedChanged += delegate { if (r.Checked) foreach (var x in secenekler) if (x != r) x.Checked = false; };
        var l = new Label
        {
            Text = aciklama, Location = new Point(20, 28), Size = new Size(510, 32), ForeColor = etkin ? (renk == Renk.Yazi ? Renk.Soluk : renk) : Renk.Soluk,
            Font = new Font("Segoe UI", 8.75f)
        };
        p.Controls.Add(r);
        p.Controls.Add(l);
        akis.Controls.Add(p);
        secenekler.Add(r);
        return r;
    }

    NumericUpDown Sayi(int deger, Point yer)
    {
        return new NumericUpDown
        {
            Minimum = 1, Maximum = 256, Value = Math.Max(1, Math.Min(256, deger)), Location = yer, Width = 58,
            BackColor = Renk.Panel, ForeColor = Renk.Yazi, BorderStyle = BorderStyle.FixedSingle
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try { int koyu = 1; Yerel.DwmSetWindowAttribute(Handle, 20, ref koyu, 4); } catch { }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Yerel.KoyuKaydirma(this);
    }
}
