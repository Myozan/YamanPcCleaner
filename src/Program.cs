// Yaman PC Cleaner - uygulama giris noktasi, tek ornek kilidi ve yonetici komutlari.
// YamanPcCleaner — basit sistem izleyici, RAM / işlemci / ekran kartı rahatlatıcı ve disk temizleyici.
// "Sistem" sekmesi: işlemci, ekran kartı, RAM, VRAM, sanal bellek ve ısı göstergeleri; altta kapatılabilir
// programlar (işlemci, bellek, GPU, VRAM kullanımıyla, canlı). "Temizle" işaretlileri kapatır ve süreçlerin
// çalışma kümesini (working set) boşaltır. "Disk" sekmesi: gereksiz dosya temizliği (normal / derin) ve
// depolama analizi. Yönetici izni yalnızca sanal bellek ve Windows kalıntıları için, o an istenir.
// Derleme: derle.cmd  (Windows'taki hazır .NET Framework csc.exe, C# 5)

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

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // "Sanal belleği büyüt / geri al" kendini yönetici olarak bu argümanlarla açar; pencere göstermeden ayarı yazıp çıkar.
        int ilk, enCok;
        if (args.Length == 3 && args[0] == "--sanal-bellek" && int.TryParse(args[1], out ilk) && int.TryParse(args[2], out enCok))
            return ilk >= 1024 && enCok >= ilk ? SanalBellek.Uygula(ilk, enCok) : 1;
        if (args.Length >= 1 && args[0] == "--sanal-bellek-geri")
            return SanalBellek.GeriYukle(args.Skip(1).ToArray());
        if (args.Length == 2 && args[0] == "--nvidia-kur")
            return NvidiaSurucu.Kur(args[1]);
        if (args.Length == 2 && args[0] == "--ag-dns")
            return AgAyarlari.YoneticiDns(args.Skip(1).ToArray());
        if (args.Length == 1 && args[0] == "--ag-sifirla")
            return AgAyarlari.YoneticiSifirla();
        if (args.Length >= 2 && args[0] == "--windows-kayit")
            return WindowsAyarlari.YoneticiYaz(args.Skip(1).ToArray());
        if (args.Length >= 2 && args[0] == "--turbo")
            return TurboAyarlari.YoneticiYaz(args.Skip(1).ToArray());
        if (args.Length >= 1 && args[0] == "--disk-temizle")
            return DiskTemizlik.YoneticiTemizle(args.Skip(1).ToArray());
        if (args.Length == 4 && args[0] == "--baslangic")
            return BaslangicYoneticisi.YoneticiAyarla(args.Skip(1).ToArray());
        if (args.Length == 1 && args[0] == "--disk-omru")
            return DiskSagligi.YoneticiYaz();

        // "--arkaplan": Windows açılışında (Run girdisi) pencere açmadan, yalnızca saatin yanındaki simgeyle başlar.
        bool arkaplan = args.Contains("--arkaplan");

        // "--guncellendi <pid>": güncelleyici yeni sürümü başlattı. Eski örnek kapanmadan mutex alınamaz: önce onu bekle.
        int eskiPid;
        int gi = Array.IndexOf(args, "--guncellendi");
        if (gi >= 0)
        {
            Guncelleyici.Guncellendi = true;
            if (gi + 1 < args.Length && int.TryParse(args[gi + 1], out eskiPid)) GuncellemeMotoru.EskiOrnegiBekle(eskiPid);
        }

        // Tek örnek: program tepside çalışırken yeniden açılırsa ikinci bir pencere/simge çıkmaz; çalışan örneğe
        // "kendini göster" denir. Sinyal önce açılır ki ilk örnek henüz dinlemeye başlamadıysa da kaybolmasın.
        // Çalışan örnek "Yönetici olarak çalıştır" ile açıldıysa olay/mutex yalnızca yöneticiye açıktır: normal
        // açılış erişim reddi alır (çökmesin); sinyal de gönderilemez, kullanıcıya nerede olduğu söylenir.
        EventWaitHandle sinyalNesnesi;
        Mutex kilitNesnesi;
        bool ilkOrnek;
        try
        {
            sinyalNesnesi = new EventWaitHandle(false, EventResetMode.AutoReset, SinyalAdi);
            try { kilitNesnesi = new Mutex(true, KilitAdi, out ilkOrnek); }
            catch (UnauthorizedAccessException) { sinyalNesnesi.Dispose(); throw; }
        }
        catch (UnauthorizedAccessException)
        {
            if (!arkaplan) MessageBox.Show("Program zaten yönetici olarak açık; saatin yanındaki simgesinden açabilirsin.", "Yaman PC Cleaner");
            return 0;
        }
        using (var sinyal = sinyalNesnesi)
        {
            using (var kilit = kilitNesnesi)
            {
                if (!ilkOrnek)
                {
                    if (!arkaplan)
                    {
                        try { AllowSetForegroundWindow(-1 /* ASFW_ANY: çalışan örnek öne gelebilsin */); } catch { }
                        sinyal.Set();
                    }
                    return 0;
                }

                // Önceki güncellemeden kalan .eski / .yeni dosyalar (yalnızca ilk örnek siler).
                GuncellemeMotoru.ArtiklariSil();

                Application.EnableVisualStyles();
                Yerel.KoyuBaslat();
                Application.SetCompatibleTextRenderingDefault(false);
                // Açılış ekranı ana pencereden önce gösterildiği için async devamlarının UI iş parçacığına
                // dönmesini burada garanti et; yoksa DurumPanosu gibi arka plan denetimleri çapraz iş parçacığına düşer.
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

                // Açılış ekranı: ana pencere ilk ölçüm bitene kadar görünmez kalır; böylece düğmelerin
                // ilk boyanmasındaki beyaz kırpışma görünmez. "--arkaplan" açılışında ekran gösterilmez.
                // Ekran, AnaForm kurulmadan önce açılır: ağır pencere kurulumu sırasında da kullanıcı logoyu görür.
                AcilisEkrani acilis = null;
                if (!arkaplan)
                {
                    acilis = new AcilisEkrani();
                    acilis.Show();
                    Application.DoEvents();
                }

                var form = new AnaForm { GizliBasla = arkaplan };
                if (acilis != null && !acilis.IsDisposed)
                {
                    form.Opacity = 0;
                    acilis.Bitti = delegate
                    {
                        try { form.Opacity = 1; form.Refresh(); form.Activate(); } catch { }
                    };
                    form.IlkOlcumBitti += delegate { acilis.HazirOldu(); };
                }

                // Sinyal ancak pencere tutamacı oluşunca dinlenir: önceden gelmiş sinyal de GosterIstegi'nde
                // (IsHandleCreated) atılıp kaybolmasın. Tutamaç yeniden oluşursa ikinci kez kaydedilmez.
                RegisteredWaitHandle bekleyen = null;
                Action dinle = delegate
                {
                    if (bekleyen == null)
                        bekleyen = ThreadPool.RegisterWaitForSingleObject(sinyal,
                            delegate { form.GosterIstegi(); }, null, Timeout.Infinite, false);
                };
                form.HandleCreated += delegate { dinle(); };
                if (form.IsHandleCreated) dinle();
                try { Application.Run(form); }
                finally
                {
                    if (acilis != null) { try { acilis.Dispose(); } catch { } }
                    if (bekleyen != null) bekleyen.Unregister(null);
                    try { kilit.ReleaseMutex(); } catch { }
                }
                return 0;
            }
        }
    }

    const string KilitAdi = "Local\\YamanPcCleaner";
    const string SinyalAdi = "Local\\YamanPcCleaner_goster";

    [DllImport("user32.dll")]
    static extern bool AllowSetForegroundWindow(int surecKimligi);
}
