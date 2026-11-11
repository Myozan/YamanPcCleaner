// Full optimize secenek penceresi.

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

// Full optimize seçenekleri: her işin altında ne yaptığı ve bedeli yazar.
class OptimizeFormu : Form
{
    public readonly CheckBox Programlar, Ram, Disk, Derin, GeriDonusum, Sanal, Hazirda;
    readonly FlowLayoutPanel akis = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
        Padding = new Padding(16, 4, 16, 0)
    };

    public OptimizeFormu(int programSayisi, string sanalAciklama, bool sanalGerekli, long hazirdaBoyut)
    {
        Text = "Full optimize";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Renk.Arka;
        ForeColor = Renk.Yazi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(600, 680);
        Icon = Logo.Simge();

        var ust = new Label
        {
            Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 14, 16, 0), ForeColor = Renk.Soluk,
            Text = "Yapılacakları seç ve \"Hepsini yap\"a bas; hepsi sırayla, tek seferde yapılır.\n" +
                   "Yönetici gerektirenler için Windows yalnızca bir kez izin ister."
        };

        Programlar = Secenek("Arka plan programlarını kapat",
            string.Format("Sistem sekmesinde işaretli {0} program kapanır. Açık pencereler, oyunlar ve sesli sohbet işaretsiz gelir.", programSayisi),
            programSayisi > 0, programSayisi > 0, Renk.Yazi);
        Ram = Secenek("RAM'i boşalt", "Programların o an kullanmadığı bellek geri alınır; çalışan oyuna dokunulmaz.", true, true, Renk.Yazi);
        Disk = Secenek("Gereksiz dosyaları temizle",
            "Geçici dosyalar, tarayıcı / Discord / Steam / Epic önbellekleri, Windows Update kalıntıları. Kişisel dosyalar, şifreler ve geçmiş silinmez.",
            true, true, Renk.Yazi);
        Derin = Secenek("Derin temizlik",
            "Shader önbellekleri, Windows.old, günlükler ve çökme kayıtları da silinir. Oyunlar ilk açılışta takılır, eski Windows'a dönülemez.",
            false, true, Renk.Turuncu);
        GeriDonusum = Secenek("Geri dönüşüm kutusunu boşalt", "Çöpe atılmış dosyalar kalıcı olarak silinir.", false, true, Renk.Yazi);
        Sanal = Secenek("Sanal belleği büyüt", sanalAciklama, sanalGerekli, sanalGerekli, Renk.Yazi);
        Hazirda = Secenek("Hazırda bekletmeyi kapat",
            hazirdaBoyut > 0
                ? string.Format("{0} yer açar; hazırda bekletme ve Hızlı Başlatma kapanır, bilgisayar biraz geç açılır.", AnaForm.Boyut(hazirdaBoyut))
                : "Bu bilgisayarda zaten kapalı.",
            false, hazirdaBoyut > 0, Renk.Turuncu);

        Derin.CheckedChanged += delegate
        {
            if (!Derin.Checked) return;
            if (MessageBox.Show(this, DiskSekmesi.DerinUyari, "Derin temizlik", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) Derin.Checked = false;
            else Disk.Checked = true;
        };
        Disk.CheckedChanged += delegate { if (!Disk.Checked) Derin.Checked = false; };

        var alt = new Panel { Dock = DockStyle.Bottom, Height = 66, Padding = new Padding(16, 10, 16, 14) };
        var btnYap = new EylemDugmesi("Hepsini yap", EylemDugmesi.Tur.Birincil)
        {
            Dock = DockStyle.Fill, DialogResult = DialogResult.OK, Ikon = "\uE768"
        };
        var btnVazgec = new EylemDugmesi("Vazgeç", EylemDugmesi.Tur.Ikincil)
        {
            Dock = DockStyle.Right, Width = 120, DialogResult = DialogResult.Cancel
        };
        alt.Controls.Add(btnYap);
        alt.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
        alt.Controls.Add(btnVazgec);
        AcceptButton = btnYap;
        CancelButton = btnVazgec;

        Controls.Add(akis);
        Controls.Add(ust);
        Controls.Add(alt);
    }

    CheckBox Secenek(string baslik, string aciklama, bool isaretli, bool etkin, Color renk)
    {
        var p = new Panel { Width = 550, Height = 72, Margin = new Padding(0, 0, 0, 4) };
        var c = new CheckBox
        {
            Text = baslik, AutoSize = true, Location = new Point(0, 4), Checked = isaretli, Enabled = etkin,
            ForeColor = renk, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        var l = new Label
        {
            Text = aciklama, Location = new Point(20, 28), Size = new Size(530, 40), ForeColor = Renk.Soluk,
            Font = new Font("Segoe UI", 8.75f)
        };
        p.Controls.Add(c);
        p.Controls.Add(l);
        akis.Controls.Add(p);
        return c;
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
