using System.Runtime.InteropServices;
using CouchBrowser.Input;
using CouchBrowser.UI;
using CouchBrowser.Web;
using Microsoft.Web.WebView2.Core;

namespace CouchBrowser;

/// <summary>Main fullscreen window: owns tabs, overlays and layout.</summary>
public sealed class BrowserForm : Form
{
    public const string Home = "https://www.google.com";

    static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CouchBrowser", "Data");

    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();

    readonly bool startPrivate;
    readonly string startUrl;
    readonly string navScript = PageBridge.LoadNavScript();
    readonly List<BrowserTab> tabs = new();
    readonly TopBar topBar = new();
    readonly KeyboardPanel keyboard = new() { Visible = false };
    readonly MenuPanel menu = new() { Visible = false };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    readonly PageBridge page;
    readonly InputRouter input;
    CoreWebView2Environment env;
    int active = -1;
    bool videoFullscreen;

    public BrowserForm(bool startPrivate, string startUrl)
    {
        this.startPrivate = startPrivate;
        this.startUrl = startUrl;
        Text = "Couch Browser";
        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.None;

        page = new PageBridge(() => ActiveTab?.Core);
        input = new InputRouter(this, new Controller(), keyboard, menu, page);
        topBar.Tabs = tabs;

        Controls.Add(menu);
        Controls.Add(keyboard);
        Controls.Add(topBar);
        timer.Tick += (_, _) => input.Tick();
        WindowState = FormWindowState.Maximized;
    }

    // ---------- State the input router needs ----------
    internal BrowserTab ActiveTab => active >= 0 && active < tabs.Count ? tabs[active] : null;
    internal bool IsForeground => IsHandleCreated && GetForegroundWindow() == Handle;
    float S => DeviceDpi / 96f;

    internal (double W, double H) ViewportCss()
    {
        var t = ActiveTab;
        if (t == null || t.View.Width == 0) return (1280, 720);
        double scale = S * t.View.ZoomFactor;
        return (t.View.Width / scale, t.View.Height / scale);
    }

    // ---------- Startup, shutdown & layout ----------
    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            var envOptions = new CoreWebView2EnvironmentOptions { AreBrowserExtensionsEnabled = true };
            env = await CoreWebView2Environment.CreateAsync(null, DataDir, envOptions);
            await OpenTab(startPrivate, startUrl);
            timer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not start the web engine (WebView2):\n\n" + ex.Message, "Couch Browser");
            Close();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        base.OnFormClosed(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        DoLayout();
    }

    void DoLayout()
    {
        if (ClientSize.Width == 0) return;
        float s = S;
        int w = ClientSize.Width, h = ClientSize.Height;
        int bar = videoFullscreen ? 0 : TopBar.HeightFor(s);

        topBar.Visible = bar > 0;
        topBar.ActiveIndex = active;
        topBar.SetBounds(0, 0, w, Math.Max(bar, 1));
        foreach (var t in tabs) t.View.SetBounds(0, bar, w, h - bar);

        int kw = (int)Math.Min(w * 0.85, 1150 * s), kh = KeyboardPanel.HeightFor(s);
        keyboard.SetBounds((w - kw) / 2, h - kh - (int)(24 * s), kw, kh);

        var ms = MenuPanel.SizeFor(Math.Max(menu.Items.Count, 1), s);
        menu.SetBounds((w - ms.Width) / 2, (h - ms.Height) / 2, ms.Width, ms.Height);
        topBar.Invalidate();
    }

    // ---------- Tabs ----------
    async Task OpenTab(bool isPrivate, string url, int insertAt = -1)
    {
        var tab = new BrowserTab(isPrivate, url);
        tab.Changed += () => topBar.Invalidate();
        tab.NewWindow += u => _ = OpenTab(tab.Private, u, tabs.IndexOf(tab) + 1);
        tab.FullscreenChanged += () =>
        {
            if (tab != ActiveTab) return;
            videoFullscreen = tab.IsVideoFullscreen;
            DoLayout();
        };

        if (insertAt < 0 || insertAt > tabs.Count) insertAt = tabs.Count;
        tabs.Insert(insertAt, tab);
        Controls.Add(tab.View);
        DoLayout();

        try { await tab.InitAsync(env, navScript); }
        catch (Exception ex)
        {
            RemoveTab(tab);
            MessageBox.Show("Could not open a tab:\n\n" + ex.Message, "Couch Browser");
            return;
        }
        SelectTab(tabs.IndexOf(tab));
    }

    void RemoveTab(BrowserTab tab)
    {
        tabs.Remove(tab);
        Controls.Remove(tab.View);
        tab.Dispose();
    }

    void SelectTab(int i)
    {
        if (tabs.Count == 0) return;
        input.ResetCursor();
        active = ((i % tabs.Count) + tabs.Count) % tabs.Count;
        for (int k = 0; k < tabs.Count; k++) tabs[k].View.Visible = k == active;
        keyboard.Visible = false;
        menu.Visible = false;
        videoFullscreen = ActiveTab.IsVideoFullscreen;
        DoLayout();
        ActiveTab.View.Focus();
    }

    internal void NextTab(int delta) => SelectTab(active + delta);

    internal void CloseTab()
    {
        var t = ActiveTab; if (t == null) return;
        int i = active;
        RemoveTab(t);
        if (tabs.Count == 0) { active = -1; _ = OpenTab(true, Home); return; }
        SelectTab(Math.Min(i, tabs.Count - 1));
    }

    async void SwitchMode()
    {
        try
        {
            var t = ActiveTab; if (t == null) return;
            int i = active, before = tabs.Count;
            await OpenTab(!t.Private, string.IsNullOrEmpty(t.Url) ? Home : t.Url, i + 1);
            if (tabs.Count == before) return;
            RemoveTab(t);
            SelectTab(i);
        }
        catch { }
    }

    void Zoom(double delta)
    {
        var t = ActiveTab; if (t == null) return;
        t.View.ZoomFactor = Math.Clamp(t.View.ZoomFactor + delta, 0.5, 3.0);
    }

    // ---------- Actions used by the input router ----------
    internal void OpenMenu()
    {
        var t = ActiveTab; if (t == null) return;
        var items = new List<(string Label, Action Run)>
        {
            ("New private tab", () => { _ = OpenTab(true, Home); }),
            ("New normal tab", () => { _ = OpenTab(false, Home); }),
            (t.Private ? "Switch this tab to normal mode" : "Switch this tab to private mode", () => SwitchMode()),
            ("Reload", () => t.Core?.Reload()),
            ("Zoom in", () => Zoom(0.1)),
            ("Zoom out", () => Zoom(-0.1)),
            ("Close tab", () => CloseTab()),
            ("Exit Couch Browser", () => Close()),
        };
        menu.Items = items;
        DoLayout();
        menu.Open(items);
    }

    internal void ShowKeyboard(bool addressMode)
    {
        keyboard.ResetFor(addressMode);
        keyboard.Visible = true;
        keyboard.BringToFront();
    }

    internal void Navigate(string url) => ActiveTab?.Core?.Navigate(url);

    internal void GoBack()
    {
        var c = ActiveTab?.Core; if (c == null) return;
        if (videoFullscreen) { page.ExitFullscreen(); return; }
        if (c.CanGoBack) c.GoBack();
    }

    /// <summary>Turns typed text into a URL, or a Google search.</summary>
    internal static string ResolveInput(string s)
    {
        s = s.Trim();
        if (s.Length == 0) return null;
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return s;
        if (!s.Contains(' ') && s.Contains('.') && !s.EndsWith(".")) return "https://" + s;
        return "https://www.google.com/search?q=" + Uri.EscapeDataString(s);
    }
}