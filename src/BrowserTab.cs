using CouchBrowser.Web;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CouchBrowser;

/// <summary>One browser tab: a WebView2 in private or normal mode.</summary>
public sealed class BrowserTab : IDisposable
{
    public WebView2 View { get; } = new() { DefaultBackgroundColor = Color.Black, Visible = false };
    public bool Private { get; }
    public string Title { get; private set; } = "New tab";
    public string Url { get; private set; }
    public bool Ready { get; private set; }

    public CoreWebView2 Core => Ready ? View.CoreWebView2 : null;
    public bool IsVideoFullscreen => Ready && View.CoreWebView2.ContainsFullScreenElement;

    public event Action Changed;
    public event Action FullscreenChanged;
    public event Action<string> NewWindow;

    public BrowserTab(bool isPrivate, string url)
    {
        Private = isPrivate;
        Url = url;
    }

    public async Task InitAsync(CoreWebView2Environment env, string navScript)
    {
        var opts = env.CreateCoreWebView2ControllerOptions();
        opts.ProfileName = "Couch";
        opts.IsInPrivateModeEnabled = Private;
        await View.EnsureCoreWebView2Async(env, opts);

        var core = View.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        await core.AddScriptToExecuteOnDocumentCreatedAsync(navScript);

        core.DocumentTitleChanged += (_, _) =>
        {
            Title = string.IsNullOrWhiteSpace(core.DocumentTitle) ? "New tab" : core.DocumentTitle;
            Changed?.Invoke();
        };
        core.SourceChanged += (_, _) => { Url = core.Source; Changed?.Invoke(); };
        core.NewWindowRequested += (_, e) => { e.Handled = true; NewWindow?.Invoke(e.Uri); };
        core.ContainsFullScreenElementChanged += (_, _) => FullscreenChanged?.Invoke();

        // Ad blocker and any other extensions in %LOCALAPPDATA%\CouchBrowser\Extensions
        try { await Extensions.EnsureInstalledAsync(core.Profile); } catch { }

        Ready = true;
        core.Navigate(Url);
    }

    public void Dispose() => View.Dispose();
}