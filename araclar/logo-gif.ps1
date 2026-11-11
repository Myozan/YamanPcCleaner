# Yaman PC Cleaner - açılış ekranı için animasyonlu logo (kaynaklar\logo.gif).
# Logonun çevresinde dönen tarama halkası ve nabız gibi parlayan ışık; koyu zeminle uyumludur.
# Yeniden üretmek için: powershell -NoProfile -ExecutionPolicy Bypass -File araclar\logo-gif.ps1
param(
    [string]$Cikti = (Join-Path $PSScriptRoot '..\kaynaklar\logo.gif')
)

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$genislik = 220
$yukseklik = 220
$kareSayisi = 40
$gecikme = 7          # 1/100 saniye: 70 ms

$zemin = New-Object Windows.Media.SolidColorBrush ([Windows.Media.Color]::FromRgb(10, 16, 28))
$vurgu = [Windows.Media.Color]::FromRgb(96, 165, 250)
$vurguKoyu = [Windows.Media.Color]::FromRgb(59, 130, 246)
$halkaSoluk = New-Object Windows.Media.SolidColorBrush ([Windows.Media.Color]::FromArgb(46, 59, 130, 246))
$isik = New-Object Windows.Media.SolidColorBrush ([Windows.Media.Color]::FromArgb(34, 59, 130, 246))

$logoYolu = Join-Path $PSScriptRoot '..\kaynaklar\logo-256.png'
$logo = New-Object Windows.Media.Imaging.BitmapImage
$logo.BeginInit()
$logo.CacheOption = [Windows.Media.Imaging.BitmapCacheOption]::OnLoad
$logo.UriSource = New-Object Uri $logoYolu
$logo.EndInit()
$logo.Freeze()

$kodlayici = New-Object Windows.Media.Imaging.GifBitmapEncoder
$merkez = 110.0
$yaricap = 79.0

for ($i = 0; $i -lt $kareSayisi; $i++) {
    $t = $i / [double]$kareSayisi
    $aci = $t * 360.0
    $nabiz = 0.5 + 0.5 * [Math]::Sin(2 * [Math]::PI * $t)

    $gorunum = New-Object Windows.Media.DrawingVisual
    $dc = $gorunum.RenderOpen()
    $dc.DrawRectangle($zemin, $null, (New-Object Windows.Rect(0, 0, $genislik, $yukseklik)))

    # Yumuşak ışık: logonun arkasında nabız gibi büyüyüp küçülen daire.
    $isikYaricap = 92.0 + 10.0 * $nabiz
    $firca = New-Object Windows.Media.RadialGradientBrush
    $firca.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.Color]::FromArgb([byte](38 + 26 * $nabiz), 59, 130, 246), 0.0)))
    $firca.GradientStops.Add((New-Object Windows.Media.GradientStop ([Windows.Media.Color]::FromArgb(0, 59, 130, 246), 1.0)))
    $dc.DrawEllipse($firca, $null, (New-Object Windows.Point($merkez, $merkez)), $isikYaricap, $isikYaricap)

    # Soluk tam halka: tarama çemberinin oturduğu zemin.
    $tamHalka = New-Object Windows.Media.Pen($halkaSoluk, 2.0)
    $dc.DrawEllipse($null, $tamHalka, (New-Object Windows.Point($merkez, $merkez)), $yaricap, $yaricap)

    # Dönen taranan yay (100 derece) ve ucundaki parlak nokta.
    $bas = ($aci - 90.0) * [Math]::PI / 180.0
    $son = ($aci - 90.0 + 100.0) * [Math]::PI / 180.0
    $basNokta = New-Object Windows.Point(($merkez + $yaricap * [Math]::Cos($bas)), ($merkez + $yaricap * [Math]::Sin($bas)))
    $sonNokta = New-Object Windows.Point(($merkez + $yaricap * [Math]::Cos($son)), ($merkez + $yaricap * [Math]::Sin($son)))
    $yay = New-Object Windows.Media.StreamGeometry
    $baglam = $yay.Open()
    $baglam.BeginFigure($basNokta, $false, $false)
    $baglam.ArcTo($sonNokta, (New-Object Windows.Size($yaricap, $yaricap)), 0, $false, [Windows.Media.SweepDirection]::Clockwise, $true, $false)
    $baglam.Close()
    $yay.Freeze()

    $yayFirca = New-Object Windows.Media.SolidColorBrush ([Windows.Media.Color]::FromArgb([byte](190 + 55 * $nabiz), 96, 165, 250))
    $yayKalem = New-Object Windows.Media.Pen($yayFirca, 3.4)
    $yayKalem.StartLineCap = [Windows.Media.PenLineCap]::Round
    $yayKalem.EndLineCap = [Windows.Media.PenLineCap]::Round
    $dc.DrawGeometry($null, $yayKalem, $yay)

    $noktaFirca = New-Object Windows.Media.SolidColorBrush ([Windows.Media.Color]::FromArgb(255, 191, 219, 254))
    $dc.DrawEllipse($noktaFirca, $null, $sonNokta, 3.6, 3.6)

    # Logo: halkanın ortasında, hafifçe nabız gibi büyüyüp küçülür.
    $boy = 106.0 + 3.0 * $nabiz
    $dc.DrawImage($logo, (New-Object Windows.Rect(($merkez - $boy / 2), ($merkez - $boy / 2), $boy, $boy)))
    $dc.Close()

    $hedef = New-Object Windows.Media.Imaging.RenderTargetBitmap($genislik, $yukseklik, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $hedef.Render($gorunum)

    $meta = New-Object Windows.Media.Imaging.BitmapMetadata('gif')
    $meta.SetQuery('/grctlext/Delay', [uint16]$gecikme)
    $meta.SetQuery('/grctlext/Disposal', [byte]2)
    $kare = [Windows.Media.Imaging.BitmapFrame]::Create($hedef, $null, $meta, $null)
    $kodlayici.Frames.Add($kare)
}

# Sonsuz döngü (NETSCAPE2.0 uzantısı).
$ilkMeta = $kodlayici.Frames[0].Metadata
try {
    $ilkMeta.SetQuery('/appext/Application', [Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0'))
    $ilkMeta.SetQuery('/appext/Data', [byte[]](1, 0, 0))
} catch { }

$hedefYol = [IO.Path]::GetFullPath($Cikti)
$akim = [IO.File]::Create($hedefYol)
try { $kodlayici.Save($akim) } finally { $akim.Close() }

# GifBitmapEncoder kare gecikmesini metadata'dan yazmıyor; her Graphic Control Extension bloğunu
# 70 ms gecikme ve "arka plana dön" (2) ile damgala.
$bayt = [IO.File]::ReadAllBytes($hedefYol)
for ($i = 0; $i -lt $bayt.Length - 7; $i++) {
    if ($bayt[$i] -eq 0x21 -and $bayt[$i + 1] -eq 0xF9 -and $bayt[$i + 2] -eq 0x04) {
        $bayt[$i + 3] = [byte](($bayt[$i + 3] -band 0xE3) -bor (2 -shl 2))
        $bayt[$i + 4] = [byte](($gecikme -shr 0) -band 0xFF)
        $bayt[$i + 5] = [byte](($gecikme -shr 8) -band 0xFF)
    }
}
[IO.File]::WriteAllBytes($hedefYol, $bayt)

# GifBitmapEncoder sonsuz döngü bloğunu yazmıyor; NETSCAPE2.0 uzantısını ilk görüntüden önce ekle.
$bayt = [IO.File]::ReadAllBytes($hedefYol)
if (-not [Text.Encoding]::ASCII.GetString($bayt).Contains('NETSCAPE2.0')) {
    $packed = $bayt[10]
    $renkTablosu = 0
    if (($packed -band 0x80) -ne 0) { $renkTablosu = 3 * [int][Math]::Pow(2, ($packed -band 7) + 1) }
    $konum = 13 + $renkTablosu
    $ek = [byte[]](0x21, 0xFF, 0x0B) + [Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0') + [byte[]](0x03, 0x01, 0x00, 0x00, 0x00)
    $yeni = New-Object byte[] ($bayt.Length + $ek.Length)
    [Array]::Copy($bayt, 0, $yeni, 0, $konum)
    [Array]::Copy($ek, 0, $yeni, $konum, $ek.Length)
    [Array]::Copy($bayt, $konum, $yeni, $konum + $ek.Length, $bayt.Length - $konum)
    [IO.File]::WriteAllBytes($hedefYol, $yeni)
}

$bilgi = Get-Item -LiteralPath $hedefYol
Write-Output ("[TAMAM] {0} olusturuldu ({1:N0} KB, {2} kare)." -f $bilgi.Name, ($bilgi.Length / 1KB), $kareSayisi)
