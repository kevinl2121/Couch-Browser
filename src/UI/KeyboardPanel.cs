namespace CouchBrowser.UI;

/// <summary>Controller-driven on-screen keyboard (letters and symbols layouts).</summary>
public sealed class KeyboardPanel : Canvas
{
    public record Key(string Label, string Value, float Width = 1f);

    static Key[] Chars(string s) => s.Select(ch => new Key(ch.ToString(), ch.ToString())).ToArray();

    static readonly Key[][] Letters =
    {
        Chars("1234567890"),
        Chars("qwertyuiop"),
        Chars("asdfghjkl-"),
        new[] { new Key("Shift", "{SHIFT}", 1.5f) }.Concat(Chars("zxcvbnm"))
            .Append(new Key("Del", "{BKSP}", 1.5f)).ToArray(),
        new[] { new Key("?123", "{LAYOUT}", 1.5f), new Key("@", "@"), new Key("space", " ", 4f),
                new Key(".", "."), new Key(".com", ".com", 1.5f), new Key("Go", "{ENTER}") }
    };

    static readonly Key[][] Symbols =
    {
        Chars("1234567890"),
        Chars("!@#$%^&*()"),
        Chars("-_=+[]{}\\|"),
        Chars("/?;:'\",~`").Append(new Key("Del", "{BKSP}", 1.5f)).ToArray(),
        new[] { new Key("ABC", "{LAYOUT}", 1.5f), new Key("<", "<"), new Key("space", " ", 4f),
                new Key(">", ">"), new Key(".com", ".com", 1.5f), new Key("Go", "{ENTER}") }
    };

    public bool AddressMode { get; private set; }
    public string Buffer = "";
    public bool Shift;
    bool symbols;
    int row = 1, col;

    Key[][] Rows => symbols ? Symbols : Letters;
    public Key Selected => Rows[row][Math.Min(col, Rows[row].Length - 1)];

    public static int HeightFor(float s) => (int)(s * (16 + 54 + 5 * (56 + 8) + 34 + 12));

    public void ResetFor(bool addressMode)
    {
        AddressMode = addressMode;
        Buffer = "";
        Shift = false;
        symbols = false;
        row = 1; col = 0;
        Invalidate();
    }

    public void ToggleLayout()
    {
        symbols = !symbols;
        col = Math.Min(col, Rows[row].Length - 1);
    }

    public void Backspace()
    {
        if (Buffer.Length > 0) Buffer = Buffer.Substring(0, Buffer.Length - 1);
    }

    /// <summary>Applies one-shot Shift to a typed letter.</summary>
    public string Apply(string value)
    {
        if (Shift && value.Length == 1 && char.IsLetter(value[0]))
        {
            Shift = false;
            return value.ToUpperInvariant();
        }
        return value;
    }

    float RowUnits(int r) => Rows[r].Sum(k => k.Width);
    float MaxUnits => Rows.Max(r => r.Sum(k => k.Width));

    float CenterUnits(int r, int c)
    {
        float x = (MaxUnits - RowUnits(r)) / 2;
        for (int i = 0; i < c; i++) x += Rows[r][i].Width;
        return x + Rows[r][c].Width / 2;
    }

    public void Nav(string dir)
    {
        var rows = Rows;
        col = Math.Min(col, rows[row].Length - 1);
        if (dir == "left") col = (col - 1 + rows[row].Length) % rows[row].Length;
        else if (dir == "right") col = (col + 1) % rows[row].Length;
        else
        {
            float cx = CenterUnits(row, col);
            row = dir == "up" ? (row - 1 + rows.Length) % rows.Length : (row + 1) % rows.Length;
            int best = 0; float bestDist = float.MaxValue;
            for (int i = 0; i < rows[row].Length; i++)
            {
                float d = Math.Abs(CenterUnits(row, i) - cx);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            col = best;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; Theme.Prep(g);
        float s = Dpi, pad = 16 * s, header = 54 * s, keyH = 56 * s, gap = 8 * s;
        g.Clear(Theme.PanelBg);
        using var fBig = Theme.Font(22 * s);
        using var fKey = Theme.Font(20 * s, bold: true);
        using var fHint = Theme.Font(15 * s);

        // Text field
        var field = new RectangleF(pad, pad, Width - 2 * pad, header - 8 * s);
        Theme.Fill(g, field, 10 * s, Theme.Bg);
        string placeholder = AddressMode ? "Search Google or type a website" : "Typing into the page";
        string shown = Buffer.Length > 60 ? "..." + Buffer.Substring(Buffer.Length - 60) : Buffer;
        Theme.Text(g, Buffer.Length == 0 ? placeholder : shown + "|", fBig,
                   Buffer.Length == 0 ? Theme.Dim : Color.White, RectangleF.Inflate(field, -12 * s, 0));

        // Keys
        float unit = (Width - 2 * pad) / MaxUnits, y = pad + header;
        for (int r = 0; r < Rows.Length; r++)
        {
            float x = pad + (MaxUnits - RowUnits(r)) / 2 * unit;
            for (int c = 0; c < Rows[r].Length; c++)
            {
                var k = Rows[r][c];
                var rect = new RectangleF(x + gap / 2, y, k.Width * unit - gap, keyH);
                bool selected = r == row && c == Math.Min(col, Rows[r].Length - 1);
                bool on = k.Value == "{SHIFT}" && Shift;
                Theme.Fill(g, rect, 8 * s, selected ? Theme.Accent : on ? Theme.KeyOn : Theme.KeyBg);
                string label = Shift && k.Label.Length == 1 && char.IsLetter(k.Label[0])
                    ? k.Label.ToUpperInvariant() : k.Label;
                Theme.Text(g, label, fKey, Color.White, rect, StringAlignment.Center);
                x += k.Width * unit;
            }
            y += keyH + gap;
        }

        Theme.Text(g, "A type     X delete     Y space     LB shift     Menu go     B close", fHint, Theme.Dim,
                   new RectangleF(pad, y, Width - 2 * pad, 34 * s), StringAlignment.Center);
    }
}