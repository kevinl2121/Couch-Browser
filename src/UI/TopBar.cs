namespace CouchBrowser.UI;

/// <summary>Tab strip, private/normal indicator, URL and button hints.</summary>
public sealed class TopBar : Canvas
{
    public IReadOnlyList<BrowserTab> Tabs = Array.Empty<BrowserTab>();
    public int ActiveIndex = -1;

    const string Hint = "LB/RB tabs    X address    Y type    R-stick cursor    Menu options";

    public static int HeightFor(float s) => (int)(78 * s);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; Theme.Prep(g);
        float s = Dpi, pad = 12 * s, tabH = 38 * s, right = 300 * s, w = Width;
        g.Clear(Theme.Bg);
        using var fTab = Theme.Font(16 * s, bold: true);
        using var fSmall = Theme.Font(13 * s);

        float avail = w - pad * 2 - right;
        float tw = Tabs.Count == 0 ? 0 : Math.Max(70 * s, Math.Min(260 * s, avail / Tabs.Count));
        for (int i = 0; i < Tabs.Count; i++)
        {
            var t = Tabs[i];
            bool isActive = i == ActiveIndex;
            var r = new RectangleF(pad + i * tw, pad, tw - 6 * s, tabH);
            Theme.Fill(g, r, 8 * s, isActive ? Theme.KeyBg : Theme.PanelBg);

            var dot = new RectangleF(r.X + 10 * s, r.Y + tabH / 2 - 5 * s, 10 * s, 10 * s);
            using (var brush = new SolidBrush(t.Private ? Theme.Private : Theme.Normal)) g.FillEllipse(brush, dot);

            Theme.Text(g, t.Title, fTab, isActive ? Color.White : Theme.Dim,
                       new RectangleF(dot.Right + 8 * s, r.Y, Math.Max(1, r.Width - 34 * s), tabH));
        }

        if (ActiveIndex < 0 || ActiveIndex >= Tabs.Count) return;
        var at = Tabs[ActiveIndex];
        Theme.Text(g, at.Private ? "PRIVATE" : "NORMAL", fTab, at.Private ? Theme.Private : Theme.Normal,
                   new RectangleF(w - right, pad, right - pad, tabH), StringAlignment.Far);

        float y2 = pad + tabH + 2 * s, hintW = 560 * s;
        Theme.Text(g, at.Url, fSmall, Theme.Dim, new RectangleF(pad, y2, Math.Max(1, w - 2 * pad - hintW), 22 * s));
        Theme.Text(g, Hint, fSmall, Theme.Dim, new RectangleF(w - hintW - pad, y2, hintW, 22 * s), StringAlignment.Far);
    }
}