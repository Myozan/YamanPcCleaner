@echo off
rem Yaman PC Cleaner - tek exe derleme betigi (Windows'ta hazir gelen .NET Framework 4.x derleyicisi).
rem Kaynaklar src\ altinda moduler olarak tutulur; yeni bir .cs dosyasi eklendiginde betigi degistirmek gerekmez.
rem Logo exe'ye gomulur: /win32icon (tum boyutlar) Gezgin ve gorev cubugu icin; /resource pencere, tepsi ve
rem sekme seridi icin yalnizca kucuk boyutlar (YamanPcCleaner-kucuk.ico 16-96 px, logo-kucuk.png 96 px).
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [HATA] .NET Framework 4.x C# derleyicisi bulunamadi.
    exit /b 1
)

set "RSP=%TEMP%\yaman-kaynaklar-%RANDOM%-%RANDOM%.rsp"
if exist "%RSP%" del "%RSP%" >nul 2>&1
for /r "%CD%\src" %%f in (*.cs) do >>"%RSP%" echo "%%f"

"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /warn:4 ^
    /out:YamanPcCleaner.exe ^
    /win32icon:kaynaklar\YamanPcCleaner.ico ^
    /resource:kaynaklar\YamanPcCleaner-kucuk.ico,YamanPcCleaner.ico ^
    /resource:kaynaklar\logo-kucuk.png,logo.png ^
    /resource:kaynaklar\logo.gif,logo.gif ^
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll /r:System.Web.Extensions.dll ^
    @"%RSP%"

set "KOD=%ERRORLEVEL%"
del "%RSP%" >nul 2>&1
if not "%KOD%"=="0" (
    echo.
    echo [HATA] Derleme basarisiz. Kod: %KOD%
    exit /b %KOD%
)
echo.
echo [TAMAM] YamanPcCleaner.exe olusturuldu.
exit /b 0
