@echo off
rem Yaman PC Cleaner - kurulum paketi derleme betigi.
rem 1) uygulamayi derler  2) kaldiriciyi derler  3) setup.exe icine ikisini de gomer.
rem setup.exe tek dosyadir; kurulum icin baska bir sey gerekmez.
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [HATA] .NET Framework 4.x C# derleyicisi bulunamadi.
    exit /b 1
)

call derle.cmd
if errorlevel 1 exit /b 1

set "ORTAK=kurulum\KurulumTema.cs kurulum\KurulumIslemleri.cs src\Cekirdek\Marka.cs src\Cekirdek\SurumBilgisi.cs"

echo.
echo [1/2] Kaldirici.exe derleniyor...
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /warn:4 ^
    /out:kurulum\Kaldirici.exe ^
    /win32icon:kaynaklar\YamanPcCleaner.ico ^
    /resource:kaynaklar\logo-kucuk.png,logo.png ^
    /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
    %ORTAK% kurulum\Kaldirici.cs
if errorlevel 1 goto hata

echo [2/2] setup.exe derleniyor...
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /warn:4 ^
    /out:setup.exe ^
    /win32icon:kaynaklar\YamanPcCleaner.ico ^
    /resource:YamanPcCleaner.exe,YamanPcCleaner.exe ^
    /resource:kurulum\Kaldirici.exe,Kaldirici.exe ^
    /resource:kaynaklar\logo-kucuk.png,logo.png ^
    /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
    %ORTAK% kurulum\Kurulum.cs
if errorlevel 1 goto hata

echo.
echo [TAMAM] setup.exe olusturuldu.
exit /b 0

:hata
echo.
echo [HATA] Kurulum paketi derlenemedi.
exit /b 1
