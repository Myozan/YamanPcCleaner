// Temizlik maddesi modeli.

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

class Temizlik
{
    public string Anahtar, Ad, Aciklama;
    public bool Yonetici, Varsayilan, Derin;
    public string Ozel;                                     // "geridonusum", "hazirda", "windows": klasör silme yerine özel işlem
    public List<string> Klasorler = new List<string>();     // içleri silinir, klasörlerin kendisi kalır
    public List<string> Dosyalar = new List<string>();
    public string[] Surecler = new string[0];               // biri açıksa bu madde atlanır (dosyaları kilitli / bozulabilir)
    public int EnAzGun;                                     // bundan yeni dosyalara dokunulmaz
    public long Boyut = -1;                                 // -1: yönetici izni olmadan okunamıyor
    public string AcikSurec;
}
