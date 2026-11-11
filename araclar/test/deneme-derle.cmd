@echo off
rem Deneme derlemesi: /define:DENEME ile RAM bosaltma hicbir surece dokunmaz, olanlar %TEMP%\yaman-deneme.log dosyasina yazilir.
rem Kaynaklar src\ altindan otomatik toplanir; yeni dosya eklerken betigi degistirmek gerekmez.
rem Kullanim: deneme-derle.cmd <cikti-klasoru>   (gercek bilgisayarda kisayolu sinamak icin; paylasilmaz)
setlocal
cd /d "%~dp0..\.."

if "%~1"=="" (
    echo [HATA] Cikti klasoru belirtilmedi.
    echo Kullanim: deneme-derle.cmd ^<cikti-klasoru^>
    exit /b 1
)
if not exist "%~1" mkdir "%~1"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [HATA] .NET Framework 4.x C# derleyicisi bulunamadi.
    exit /b 1
)

set "RSP=%TEMP%\yaman-deneme-kaynaklar-%RANDOM%-%RANDOM%.rsp"
if exist "%RSP%" del "%RSP%" >nul 2>&1
for /r "%CD%\src" %%f in (*.cs) do >>"%RSP%" echo "%%f"

"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /define:DENEME /out:"%~1\YamanPcCleaner.exe" ^
    /win32icon:kaynaklar\YamanPcCleaner.ico ^
    /resource:kaynaklar\YamanPcCleaner-kucuk.ico,YamanPcCleaner.ico ^
    /resource:kaynaklar\logo-kucuk.png,logo.png ^
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll /r:System.Web.Extensions.dll ^
    @"%RSP%"

set "KOD=%ERRORLEVEL%"
del "%RSP%" >nul 2>&1
if not "%KOD%"=="0" (
    echo.
    echo [HATA] Deneme derlemesi basarisiz. Kod: %KOD%
    exit /b %KOD%
)
echo.
echo [TAMAM] "%~1\YamanPcCleaner.exe" olusturuldu.
exit /b 0
