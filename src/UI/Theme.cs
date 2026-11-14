using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CouchBrowser.UI;

/// <summary>Colors and drawing helpers shared by all overlays.</summary>
public static class Theme
{
    public static readonly Color Bg = Color.FromArgb(14, 16, 22);
    public static readonly Color PanelBg = Color.FromArgb(28, 31, 40);
    public static readonly Color KeyBg = Color.FromArgb(44, 48, 60);
    public static readonly Color KeyOn = Color.FromArgb(80, 86, 104);
    public static readonly Color Accent = Color.FromArgb(61, 139, 255);
    public static readonly Color Private = Color.FromArgb(160, 100, 255);
    public static readonly Color Normal = Color.FromArgb(61, 220, 132);
    public static readonly Color Dim = Color.FromArgb(150, 156, 170);

    public static Font Font(float px, bool bold = false) =>
        new(bold ? "Segoe UI Semibold" : "Segoe UI", px, GraphicsUnit.Pixel);

    public static void Prep(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    }

    public static void Fill(Graphics g, RectangleF r, float radius, Color color)
    {
        using var brush = new SolidBrush(color);
        radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2);
        if (radius < 1) { g.FillRectangle(brush, r); return; }
        using var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    public static void Text(Graphics g, string text, Font font, Color color, RectangleF r,
                            StringAlignment align = StringAlignment.Near)
    {
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = align,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(text ?? "", font, brush, r, format);
    }
}

/// <summary>Flicker-free, non-focusable control for custom-drawn overlays.</summary>
public class Canvas : Control
{
    public Canvas()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    protected float Dpi => DeviceDpi / 96f;
}