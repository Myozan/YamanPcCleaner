// Koyu temali liste ve sutun siralayicisi.

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

class KoyuListe : ListView
{
    public KoyuListe()
    {
        DoubleBuffered = true;
        OwnerDraw = true;
        View = View.Details;
        CheckBoxes = true;
        FullRowSelect = true;
        BorderStyle = BorderStyle.None;
        BackColor = Renk.Panel;
        ForeColor = Renk.Yazi;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try { Yerel.SetWindowTheme(Handle, "DarkMode_Explorer", null); } catch { }  // koyu kaydırma çubuğu
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        using (var b = new SolidBrush(Renk.Arka)) e.Graphics.FillRectangle(b, e.Bounds);
        var hiza = e.Header.TextAlign == HorizontalAlignment.Right ? TextFormatFlags.Right : TextFormatFlags.Left;
        var r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, e.Header.Text, Font, r, Renk.Soluk,
            hiza | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    // Seçili satır elle çizilir: Windows'un açık mavi seçim zemininde soluk (kapalı / önemsiz) yazılar okunmuyordu.
    // Koyu mavi zemin; soluk yazı seçiliyken açılır, renkli (turuncu, kırmızı) yazılar korunur.
    static readonly Color SecimZemini = Color.FromArgb(40, 66, 102);
    static readonly Color SecimSoluk = Color.FromArgb(196, 203, 214);

    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        if (!e.Item.Selected) { e.DrawDefault = true; return; }
        using (var b = new SolidBrush(SecimZemini)) e.Graphics.FillRectangle(b, e.Bounds);
    }

    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        if (!e.Item.Selected || e.Header == null) { e.DrawDefault = true; return; }
        var r = e.Bounds;
        if (e.ColumnIndex == 0) r.Width = Math.Min(r.Width, e.Header.Width);   // ilk sütunun sınırı bütün satırı kaplıyor
        using (var b = new SolidBrush(SecimZemini)) e.Graphics.FillRectangle(b, r);
        int sol = 6;
        if (e.ColumnIndex == 0 && CheckBoxes)
        {
            var boyut = CheckBoxRenderer.GetGlyphSize(e.Graphics, System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
            CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(r.X + 4, r.Y + (r.Height - boyut.Height) / 2),
                e.Item.Checked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
            sol = 4 + boyut.Width + 4;
        }
        bool ogeStili = e.Item.UseItemStyleForSubItems || e.SubItem == null;
        Color renk = ogeStili ? e.Item.ForeColor : e.SubItem.ForeColor;
        if (renk.ToArgb() == Renk.Soluk.ToArgb()) renk = SecimSoluk;
        else if (renk.ToArgb() == Renk.Yazi.ToArgb()) renk = Color.White;
        Font yazi = (ogeStili ? e.Item.Font : e.SubItem.Font) ?? Font;
        var hiza = e.Header.TextAlign == HorizontalAlignment.Right ? TextFormatFlags.Right
                 : e.Header.TextAlign == HorizontalAlignment.Center ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left;
        var metinAlani = new Rectangle(r.X + sol, r.Y, Math.Max(0, r.Width - sol - 6), r.Height);
        TextRenderer.DrawText(e.Graphics, e.SubItem != null ? e.SubItem.Text : e.Item.Text, yazi, metinAlani, renk,
            hiza | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
    }
}

class Siralayici : IComparer
{
    public int Sutun = 3;
    public bool Azalan = true;

    public int Compare(object x, object y)
    {
        var ox = (ListViewItem)x; var oy = (ListViewItem)y;
        var a = (Grup)ox.Tag; var b = (Grup)oy.Tag;
        int s;
        switch (Sutun)
        {
            case 0: s = string.Compare(ox.Text, oy.Text, StringComparison.CurrentCultureIgnoreCase); break;
            case 1: s = a.Pencereli.CompareTo(b.Pencereli); break;
            case 2: s = a.Cpu.CompareTo(b.Cpu); break;
            case 4: s = a.Gpu.CompareTo(b.Gpu); break;
            case 5: s = a.Vram.CompareTo(b.Vram); break;
            default: s = a.Bellek.CompareTo(b.Bellek); break;
        }
        if (s == 0) s = string.Compare(a.Ad, b.Ad, StringComparison.OrdinalIgnoreCase);
        return Azalan ? -s : s;
    }
}
