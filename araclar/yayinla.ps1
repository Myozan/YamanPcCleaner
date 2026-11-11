# Yeni sürümü derler, imzalar ve GitHub Release olarak yayınlar.
# Güncelleme kanalı GitHub'dır: uygulama yalnızca bu deponun "latest release" kaydına bakar.
# Kullanım:
#   powershell -File araclar\yayinla.ps1 -Notlar "Sürüm notları" -Token $env:GITHUB_TOKEN
#   powershell -File araclar\yayinla.ps1 -Notlar notlar.md
# Token yoksa ve GitHub CLI (gh) kuruluysa onun oturumu kullanılır.
param(
    [Parameter(Mandatory = $true)][string]$Notlar,
    [string]$Depo = 'Myozan/YamanPcCleaner',
    [string]$Token = $env:GITHUB_TOKEN
)
$ErrorActionPreference = 'Stop'

Write-Host 'HATIRLATMA: src\Arayuz\Hakkinda.cs özetini ve src\Cekirdek\SurumBilgisi.cs sürümünü güncelledin mi?' -ForegroundColor Yellow

$kok = Split-Path -Parent $PSScriptRoot

# Sürüm: src\Cekirdek\SurumBilgisi.cs -> Metin
$kaynak = Get-Content -LiteralPath (Join-Path $kok 'src\Cekirdek\SurumBilgisi.cs') -Raw -Encoding UTF8
if ($kaynak -notmatch 'Metin\s*=\s*"(\d+\.\d+\.\d+)"') { throw 'src\Cekirdek\SurumBilgisi.cs içinde sürüm bulunamadı.' }
$surum = $Matches[1]
$etiket = "v$surum"
Write-Host "Sürüm: $etiket  ·  Depo: $Depo"
if (-not $Token -and -not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub token bulunamadı. $env:GITHUB_TOKEN ayarla ya da GitHub CLI (gh) kur.'
}

# Derle
$exe = Join-Path $kok 'YamanPcCleaner.exe'
if (Test-Path -LiteralPath $exe) { Remove-Item -LiteralPath $exe -Force }
& cmd /c "`"$(Join-Path $kok 'derle.cmd')`""
if (-not (Test-Path -LiteralPath $exe)) { throw 'Derleme başarısız: exe oluşmadı.' }
$dosyaSurumu = (Get-Item -LiteralPath $exe).VersionInfo.FileVersion
if ($dosyaSurumu -ne "$surum.0") { throw "Exe sürümü ($dosyaSurumu) src\Cekirdek\SurumBilgisi.cs ($surum) ile uyuşmuyor." }

# İmzala
& (Join-Path $PSScriptRoot 'imzala.ps1') -Exe $exe
$imza = $exe + '.imza'
if (-not (Test-Path -LiteralPath $imza)) { throw 'İmza dosyası oluşmadı.' }

# Sürüm notları: dosya yolu ya da düz metin
$notDosyasi = Join-Path ([IO.Path]::GetTempPath()) ("yaman-notlar-" + [Guid]::NewGuid().ToString('N') + '.md')
try {
    if (Test-Path -LiteralPath $Notlar -PathType Leaf) {
        $metin = Get-Content -LiteralPath $Notlar -Raw -Encoding UTF8
    } else {
        $metin = $Notlar
    }
    [IO.File]::WriteAllText($notDosyasi, $metin, (New-Object System.Text.UTF8Encoding($false)))

    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($gh -and -not $Token) {
        # GitHub CLI oturumu varsa en kısa yol.
        $ErrorActionPreference = 'Continue'
        & gh release view $etiket --repo $Depo 2>&1 | Out-Null
        $varMi = ($LASTEXITCODE -eq 0)
        $ErrorActionPreference = 'Stop'
        if ($varMi) { throw "$etiket sürümü GitHub'da zaten var. src\Cekirdek\SurumBilgisi.cs içinde sürümü artır." }
        & gh release create $etiket $exe $imza --repo $Depo --title $etiket --notes-file $notDosyasi
        if ($LASTEXITCODE -ne 0) { throw 'gh release create başarısız.' }
    } else {
        # GitHub REST API: gh kurulu olmasa da token ile yayınlar.
        function Github($yontem, $adres, $govdeDosyasi) {
            $argumanlar = @('-sS', '-X', $yontem,
                '-H', "Authorization: Bearer $Token",
                '-H', 'Accept: application/vnd.github+json',
                '-H', 'X-GitHub-Api-Version: 2022-11-28')
            if ($govdeDosyasi) { $argumanlar += @('--data-binary', "@$govdeDosyasi") }
            $cikti = & curl.exe @argumanlar $adres
            if ($LASTEXITCODE -ne 0) { throw "GitHub API isteği başarısız: $yontem $adres" }
            return $cikti
        }

        $kod = & curl.exe -sS -o NUL -w '%{http_code}' -H "Authorization: Bearer $Token" `
            -H 'Accept: application/vnd.github+json' "https://api.github.com/repos/$Depo/releases/tags/$etiket"
        if ($kod -eq '200') { throw "$etiket sürümü GitHub'da zaten var. src\Cekirdek\SurumBilgisi.cs içinde sürümü artır." }
        if ($kod -ne '404') { throw "Depo ya da sürüm denetlenemedi (HTTP $kod). Depo adı: $Depo" }

        $jsonYolu = Join-Path ([IO.Path]::GetTempPath()) ("yaman-surum-" + [Guid]::NewGuid().ToString('N') + '.json')
        try {
            $govde = @{ tag_name = $etiket; name = $etiket; body = $metin; draft = $false; prerelease = $false } | ConvertTo-Json -Depth 4
            [IO.File]::WriteAllText($jsonYolu, $govde, (New-Object System.Text.UTF8Encoding($false)))
            $yanit = Github 'POST' "https://api.github.com/repos/$Depo/releases" $jsonYolu | ConvertFrom-Json
            if (-not $yanit.id) { throw 'Sürüm kaydı oluşturulamadı.' }

            foreach ($dosya in @($exe, $imza)) {
                $ad = [IO.Path]::GetFileName($dosya)
                $adres = "https://uploads.github.com/repos/$Depo/releases/$($yanit.id)/assets?name=$ad"
                $yukleme = & curl.exe -sS -X POST -H "Authorization: Bearer $Token" `
                    -H 'Accept: application/vnd.github+json' -H 'Content-Type: application/octet-stream' `
                    --data-binary "@$dosya" $adres
                if ($LASTEXITCODE -ne 0) { throw "Yükleme başarısız: $ad" }
                if (-not (($yukleme | ConvertFrom-Json).id)) { throw "Yükleme reddedildi: $ad -> $yukleme" }
                Write-Host "Yüklendi: $ad"
            }
        } finally {
            if (Test-Path -LiteralPath $jsonYolu) { Remove-Item -LiteralPath $jsonYolu -Force }
        }
    }
} finally {
    if (Test-Path -LiteralPath $notDosyasi) { Remove-Item -LiteralPath $notDosyasi -Force }
}
Write-Host "Yayınlandı: $etiket  ·  https://github.com/$Depo/releases/tag/$etiket"
