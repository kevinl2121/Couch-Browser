namespace CouchBrowser.UI;

/// <summary>The Menu-button overlay: a vertical list of actions.</summary>
public sealed class MenuPanel : Canvas
{
    public List<(string Label, Action Run)> Items = new();
    public int Sel;

    public static Size SizeFor(int count, float s) => new((int)(560 * s), (int)(s * (48 + 50 + count * 58)));

    public void Open(List<(string Label, Action Run)> items)
    {
        Items = items;
        Sel = 0;
        Visible = true;
        BringToFront();
        Invalidate();
    }

    public void Step(int delta)
    {
        if (Items.Count == 0) return;
        Sel = (Sel + delta + Items.Count) % Items.Count;
        Invalidate();
    }

    public void RunSelected()
    {
        if (Items.Count == 0) return;
        var item = Items[Sel];
        Visible = false;
        item.Run();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; Theme.Prep(g);
        float s = Dpi, pad = 24 * s;
        g.Clear(Theme.PanelBg);
        using var fTitle = Theme.Font(22 * s, bold: true);
        using var fItem = Theme.Font(19 * s);

        Theme.Text(g, "Menu", fTitle, Color.White, new RectangleF(pad, pad, Width - 2 * pad, 40 * s));
        float y = pad + 50 * s;
        for (int i = 0; i < Items.Count; i++)
        {
            var r = new RectangleF(pad, y, Width - 2 * pad, 50 * s);
            if (i == Sel) Theme.Fill(g, r, 8 * s, Theme.Accent);
            Theme.Text(g, Items[i].Label, fItem, Color.White, RectangleF.Inflate(r, -14 * s, 0));
            y += 58 * s;
        }
    }
}