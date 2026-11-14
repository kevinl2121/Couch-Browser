using Microsoft.Web.WebView2.Core;

namespace CouchBrowser.Web;

/// <summary>
/// Loads unpacked browser extensions from %LOCALAPPDATA%\CouchBrowser\Extensions\<name>\.
/// Only Manifest V3 extensions work (the WebView2/Edge engine has dropped MV2).
/// </summary>
public static class Extensions
{
    public static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CouchBrowser", "Extensions");

    public static async Task EnsureInstalledAsync(CoreWebView2Profile profile)
    {
        if (!Directory.Exists(Folder)) return;

        var dirs = Directory.GetDirectories(Folder)
            .Where(d => File.Exists(Path.Combine(d, "manifest.json")))
            .ToList();
        if (dirs.Count == 0) return;

        // Private profiles start empty every session; the normal profile keeps installs.
        // Reinstall only when something is missing, since reinstalling can reset settings.
        var installed = await profile.GetBrowserExtensionsAsync();
        if (installed.Count >= dirs.Count) return;

        foreach (var dir in dirs)
        {
            try { await profile.AddBrowserExtensionAsync(dir); }
            catch { /* skip extensions that fail to load (e.g. MV2) */ }
        }
    }
}