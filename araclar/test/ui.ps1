param([string[]]$Adimlar)
# Adımlar: tikla|Metin, bekle|sn, foto|ad, dialogfoto|Başlık|ad, dialogtikla|Başlık|Düğme, tus|Metin|vk
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class UI { [StructLayout(LayoutKind.Sequential)] public struct R { public int L,T,Ri,B; }
public delegate bool CB(IntPtr h, IntPtr l);
[DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr p, CB cb, IntPtr l);
[DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint f);
[DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
[DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string c, string t);
[DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
public static IntPtr Sinif(IntPtr p, string c) { IntPtr s = IntPtr.Zero; EnumChildWindows(p, (h,l) => { var b = new StringBuilder(256); GetClassName(h,b,256); if (b.ToString().Contains(c) && IsWindowVisible(h)) { s=h; return false; } return true; }, IntPtr.Zero); return s; }
public static IntPtr Bul(IntPtr p, string t) { IntPtr s = IntPtr.Zero; EnumChildWindows(p, (h,l) => { var b = new StringBuilder(256); GetWindowText(h,b,256); if (b.ToString()==t) { s=h; return false; } return true; }, IntPtr.Zero); return s; }
public static IntPtr BulBoyut(IntPtr p, int w, int h) { IntPtr s = IntPtr.Zero; EnumChildWindows(p, (wnd,l) => { if (!IsWindowVisible(wnd)) return true; var r = new R(); GetWindowRect(wnd, out r); if (r.Ri-r.L==w && r.B-r.T==h) { s=wnd; return false; } return true; }, IntPtr.Zero); return s; }
public static void TiklaHandle(IntPtr h, int w, int yuk) { IntPtr lp = (IntPtr)(((yuk/2) << 16) | ((w/2) & 0xFFFF)); PostMessage(h, 0x201, (IntPtr)1, lp); PostMessage(h, 0x202, IntPtr.Zero, lp); }
[StructLayout(LayoutKind.Sequential)] public struct P { public int X,Y; }
[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
[DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref P p);
[DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h, ref P p);
[DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(P p);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
public static void EkranTikla(IntPtr ana, int x, int y) { var r = new R(); GetWindowRect(ana, out r); SetCursorPos(r.L + x, r.T + y);
  mouse_event(0x0002, 0, 0, 0, IntPtr.Zero); mouse_event(0x0004, 0, 0, 0, IntPtr.Zero); }
public static IntPtr Tikla(IntPtr ana, int x, int y) { var p = new P { X=x, Y=y }; ClientToScreen(ana, ref p);
  IntPtr h = WindowFromPoint(p); if (h == IntPtr.Zero) return h;
  ScreenToClient(h, ref p); IntPtr lp = (IntPtr)((p.Y << 16) | (p.X & 0xFFFF));
  PostMessage(h, 0x201, (IntPtr)1, lp); PostMessage(h, 0x202, IntPtr.Zero, lp); return h; }
public static string Liste(IntPtr p) { var sb = new StringBuilder(); EnumChildWindows(p, (h,l) => { var b = new StringBuilder(256); GetClassName(h,b,256); var t = new StringBuilder(256); GetWindowText(h,t,256); var r = new R(); GetWindowRect(h, out r);
  if (IsWindowVisible(h) && r.Ri > r.L) sb.AppendLine(string.Format("{0} [{1}] '{2}' {3},{4} {5}x{6}", h, b, t, r.L, r.T, r.Ri-r.L, r.B-r.T)); return true; }, IntPtr.Zero); return sb.ToString(); } }
'@
$dir = Join-Path $PSScriptRoot "goruntuler"; New-Item -ItemType Directory -Force $dir | Out-Null
function Shot($h, $name) { $r = New-Object UI+R; [UI]::GetWindowRect($h, [ref]$r) | Out-Null
 $bmp = New-Object System.Drawing.Bitmap ($r.Ri-$r.L), ($r.B-$r.T); $g = [System.Drawing.Graphics]::FromImage($bmp); $dc = $g.GetHdc(); [UI]::PrintWindow($h, $dc, 2) | Out-Null; $g.ReleaseHdc($dc)
 $bmp.Save("$dir\$name.png") }
$p = Start-Process ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\YamanPcCleaner.exe"))) -PassThru
Start-Sleep -Seconds 5; $p.Refresh(); $ana = $p.MainWindowHandle
foreach ($a in $Adimlar) {
  $x = $a.Split('|')
  switch ($x[0]) {
    'tikla' { $h = [UI]::Bul($ana, $x[1]); "tikla $($x[1]) -> $h"; [UI]::PostMessage($h, 0xF5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }
    'kaydir' { $h = [UI]::Sinif($ana, "SysListView32"); "kaydir -> $h"; [UI]::PostMessage($h, 0x115, [IntPtr]7, [IntPtr]::Zero) | Out-Null }
    'formfoto' { $d = [UI]::FindWindow([NullString]::Value, $x[1]); "form $($x[1]) -> $d"; if ($d -ne [IntPtr]::Zero) { Shot $d $x[2] } }
    'formtikla' { $d = [UI]::FindWindow([NullString]::Value, $x[1]); $b = [UI]::Bul($d, $x[2]); "formtikla $($x[2]) -> $b"; [UI]::PostMessage($b, 0xF5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }
    'bekle' { Start-Sleep -Seconds ([int]$x[1]) }
    'foto' { $p.Refresh(); "yanit veriyor: $($p.Responding)"; [UI]::ShowWindow($ana, 9) | Out-Null; Start-Sleep -Milliseconds 500; Shot $ana $x[1] }
    'boyut' { [UI]::SetWindowPos($ana, [IntPtr]::Zero, 0, 0, [int]$x[1], [int]$x[2], 6) | Out-Null; "boyut $($x[1])x$($x[2])"; Start-Sleep -Milliseconds 700 }
    'tiklagoreli' { $h = [UI]::Tikla($ana, [int]$x[1], [int]$x[2]); "tiklagoreli $($x[1]),$($x[2]) -> $h" }
    'tiklaekran' { [UI]::EkranTikla($ana, [int]$x[1], [int]$x[2]); "tiklaekran $($x[1]),$($x[2])" }
    'liste' { [UI]::Liste($ana) }
    'dialogfoto' { $d = [UI]::FindWindow("#32770", $x[1]); "dialog $($x[1]) -> $d"; if ($d -ne [IntPtr]::Zero) { Shot $d $x[2] } }
    'dialogtikla' { $d = [UI]::FindWindow("#32770", $x[1]); $b = [UI]::Bul($d, $x[2]); "dialogtikla $($x[2]) -> $b"; [UI]::PostMessage($b, 0xF5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }
  }
}
Stop-Process -Id $p.Id

