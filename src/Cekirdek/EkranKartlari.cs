// Ekran karti ve VRAM bilgisini kayit defterinden okur.

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

// Ekran kartları kayıt defterindeki Display sınıfı anahtarından okunur. Bu anahtarda sökülmüş / değiştirilmiş
// kartların girdileri de kalır; yalnızca şu an takılı (DIGCF_PRESENT) aygıtların sürücü anahtarları sayılır.
static class EkranKartlari
{
    public const string SinifAnahtari = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVINFO_DATA { public uint cbSize; public Guid ClassGuid; public uint DevInst; public IntPtr Reserved; }

    [DllImport("setupapi.dll", SetLastError = true)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid sinif, IntPtr numaralayici, IntPtr pencere, uint bayrak);
    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInfo(IntPtr kume, uint sira, ref SP_DEVINFO_DATA veri);
    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool SetupDiGetDeviceRegistryProperty(IntPtr kume, ref SP_DEVINFO_DATA veri, uint ozellik, out uint tur, byte[] tampon, uint boyut, out uint gereken);
    [DllImport("setupapi.dll")]
    static extern bool SetupDiDestroyDeviceInfoList(IntPtr kume);

    // Takılı ekran bağdaştırıcılarının sınıf anahtarındaki alt anahtar adları ("0000", "0001"). SPDRP_DRIVER değeri
    // "{4d36e968-...}\0000" biçimindedir. Okunamazsa null: çağıran tüm girdileri kullanır.
    public static HashSet<string> Takililar()
    {
        try
        {
            var sinif = new Guid("4d36e968-e325-11ce-bfc1-08002be10318");
            IntPtr kume = SetupDiGetClassDevs(ref sinif, IntPtr.Zero, IntPtr.Zero, 0x2 /* DIGCF_PRESENT */);
            if (kume == IntPtr.Zero || kume == new IntPtr(-1)) return null;
            var sonuc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var veri = new SP_DEVINFO_DATA();
                veri.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                var tampon = new byte[1024];
                for (uint i = 0; SetupDiEnumDeviceInfo(kume, i, ref veri); i++)
                {
                    uint tur, gereken;
                    if (!SetupDiGetDeviceRegistryProperty(kume, ref veri, 9 /* SPDRP_DRIVER */, out tur, tampon, (uint)tampon.Length, out gereken)) continue;
                    string s = Encoding.Unicode.GetString(tampon, 0, (int)Math.Min(gereken, (uint)tampon.Length)).TrimEnd('\0');
                    int b = s.LastIndexOf('\\');
                    if (b >= 0 && b + 1 < s.Length) sonuc.Add(s.Substring(b + 1));
                }
            }
            finally { SetupDiDestroyDeviceInfoList(kume); }
            return sonuc.Count > 0 ? sonuc : null;
        }
        catch { return null; }
    }

    // Sürücünün yazdığı VRAM boyutu (bayt); yoksa 0.
    public static long Vram(RegistryKey k)
    {
        object q = k.GetValue("HardwareInformation.qwMemorySize");
        object d = k.GetValue("HardwareInformation.MemorySize");
        if (q is long) return (long)q;
        if (d is int) return (uint)(int)d;
        if (d is byte[] && ((byte[])d).Length >= 4) return BitConverter.ToUInt32((byte[])d, 0);
        return 0;
    }
}
