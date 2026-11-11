// Windows API cagrilari (bellek, pencere, surec, geri donusum).

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

static class Yerel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SHQUERYRBINFO { public int cbSize; public long i64Size, i64NumItems; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SHQueryRecycleBin(string kok, ref SHQUERYRBINFO bilgi);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SHEmptyRecycleBin(IntPtr h, string kok, uint bayrak);

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile,
                     ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    public delegate bool PencereGeri(IntPtr h, IntPtr l);

    [DllImport("kernel32.dll")] public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);
    [DllImport("kernel32.dll")] public static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
    [DllImport("kernel32.dll")] public static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll")] public static extern bool GetProcessTimes(IntPtr h, out long olusma, out long cikis, out long cekirdek, out long kullanici);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern bool QueryFullProcessImageName(IntPtr h, int bayrak, StringBuilder ad, ref int boyut);
    [DllImport("psapi.dll")] public static extern bool EmptyWorkingSet(IntPtr h);
    [DllImport("user32.dll")] public static extern bool EnumWindows(PencereGeri geri, IntPtr l);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int komut);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint komut);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out int pid);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int ozellik, out int deger, int boyut);
    [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr h, int ozellik, ref int deger, int boyut);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] public static extern int SetWindowTheme(IntPtr h, string uygulama, string kimlik);
    [DllImport("uxtheme.dll", EntryPoint = "#135")] static extern int SetPreferredAppMode(int mod);
    [DllImport("uxtheme.dll", EntryPoint = "#133")] static extern bool AllowDarkModeForWindow(IntPtr h, bool izin);

    // Windows 10/11 koyu kaydırma çubukları için uygulama genelinde koyu modu zorlar:
    // sistem açık temada olsa bile programın kendi yüzeyleri ve kaydırma çubukları koyu kalır.
    public static void KoyuBaslat()
    {
        try { SetPreferredAppMode(2 /* ForceDark */); } catch { }
    }

    // Kayıt defterindeki derleme numarasına göre işletim sistemi adı. Manifestsiz .NET Framework
    // exe'de Environment.OSVersion Windows 10/11'de bile 6.2 döndüğü için tek başına kullanılmaz.
    public static string WindowsAdi()
    {
        int derleme = 0;
        try
        {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                if (k != null) int.TryParse(Convert.ToString(k.GetValue("CurrentBuildNumber")), out derleme);
            }
        }
        catch { }
        if (derleme <= 0) derleme = Environment.OSVersion.Version.Build;
        if (derleme >= 22000) return "Windows 11";
        if (derleme >= 10240) return "Windows 10";
        return "Windows " + Environment.OSVersion.Version.Major;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PROCESSENTRY32
    {
        public uint dwSize, cntUsage, th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID, cntThreads, th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
    }

    [DllImport("kernel32.dll")] public static extern IntPtr CreateToolhelp32Snapshot(uint bayrak, uint pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32FirstW")] public static extern bool Process32First(IntPtr h, ref PROCESSENTRY32 e);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32NextW")] public static extern bool Process32Next(IntPtr h, ref PROCESSENTRY32 e);

    // Komut satırı kabukları ve terminaller. Bunlardan başlatılan süreçlerin kaydedilmemiş işi olabilir
    // (derleme, betik, yerel sunucu, uzun süren komut); hangi araç olduğu bilinmese de yapıdan tanınır.
    static readonly HashSet<string> KonsolKabuklari = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "conhost.exe", "OpenConsole.exe", "WindowsTerminal.exe", "cmd.exe", "powershell.exe", "pwsh.exe",
        "bash.exe", "sh.exe", "wsl.exe", "mintty.exe"
    };

    // Konsol süreçleri: kendi konsolu olanlar (çocuğu conhost / OpenConsole) ve atalarından biri kabuk ya da
    // terminal olanlar. Masaüstü kabuğuna (explorer) varınca yukarı çıkılmaz. Okunamazsa boş küme.
    public static HashSet<int> KonsolSurecleri()
    {
        var sonuc = new HashSet<int>();
        var ust = new Dictionary<int, int>();
        var adlar = new Dictionary<int, string>();
        try
        {
            IntPtr h = CreateToolhelp32Snapshot(0x2 /* TH32CS_SNAPPROCESS */, 0);
            if (h == IntPtr.Zero || h == new IntPtr(-1)) return sonuc;
            try
            {
                var e = new PROCESSENTRY32();
                e.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
                for (bool devam = Process32First(h, ref e); devam; devam = Process32Next(h, ref e))
                {
                    ust[(int)e.th32ProcessID] = (int)e.th32ParentProcessID;
                    adlar[(int)e.th32ProcessID] = e.szExeFile ?? "";
                }
            }
            finally { CloseHandle(h); }

            var konsolSahibi = new HashSet<int>();
            foreach (var s in adlar)
            {
                int u;
                if ((s.Value.Equals("conhost.exe", StringComparison.OrdinalIgnoreCase) || s.Value.Equals("OpenConsole.exe", StringComparison.OrdinalIgnoreCase))
                    && ust.TryGetValue(s.Key, out u)) konsolSahibi.Add(u);
            }
            foreach (var pid in adlar.Keys)
            {
                if (konsolSahibi.Contains(pid)) { sonuc.Add(pid); continue; }
                int simdiki = pid;
                for (int adim = 0; adim < 16; adim++)
                {
                    int u;
                    string a;
                    if (!ust.TryGetValue(simdiki, out u) || u == 0 || u == simdiki || !adlar.TryGetValue(u, out a)) break;
                    if (a.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase)) break;
                    if (konsolSahibi.Contains(u) || KonsolKabuklari.Contains(a)) { sonuc.Add(pid); break; }
                    simdiki = u;
                }
            }
        }
        catch { }
        return sonuc;
    }

    public static MEMORYSTATUSEX Bellek()
    {
        var m = new MEMORYSTATUSEX();
        m.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        GlobalMemoryStatusEx(ref m);
        return m;
    }

    // Koyu temada kaydırma çubukları: pencere ve alt denetimlere "DarkMode_Explorer" görünümü uygular.
    public static void KoyuKaydirma(Control kok)
    {
        if (kok == null) return;
        if (kok.IsHandleCreated)
        {
            try { AllowDarkModeForWindow(kok.Handle, true); } catch { }
            try { SetWindowTheme(kok.Handle, "DarkMode_Explorer", null); } catch { }
        }
        foreach (Control alt in kok.Controls) KoyuKaydirma(alt);
    }

    // Görev çubuğunda görünen türden penceresi olan süreçler (görünür, sahipsiz, başlıklı, gizlenmemiş).
    public static HashSet<int> PencereliSurecler()
    {
        var sonuc = new HashSet<int>();
        EnumWindows(delegate(IntPtr h, IntPtr l)
        {
            if (IsWindowVisible(h) && GetWindow(h, 4 /* GW_OWNER */) == IntPtr.Zero && GetWindowTextLength(h) > 0)
            {
                int gizli;
                if (DwmGetWindowAttribute(h, 14 /* DWMWA_CLOAKED */, out gizli, 4) != 0 || gizli == 0)
                {
                    int pid;
                    GetWindowThreadProcessId(h, out pid);
                    sonuc.Add(pid);
                }
            }
            return true;
        }, IntPtr.Zero);
        return sonuc;
    }
}
