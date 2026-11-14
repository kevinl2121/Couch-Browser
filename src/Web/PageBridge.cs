using System.Globalization;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace CouchBrowser.Web;

/// <summary>Talks to the current page: script calls plus trusted mouse/keyboard events.</summary>
public sealed class PageBridge
{
    readonly Func<CoreWebView2> core;

    public PageBridge(Func<CoreWebView2> core) => this.core = core;

    public static string LoadNavScript()
    {
        using var stream = typeof(PageBridge).Assembly.GetManifestResourceStream("CouchBrowser.nav.js")
            ?? throw new InvalidOperationException("Embedded nav.js is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    static string Json(object o) => JsonSerializer.Serialize(o);
    static string Num(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);

    public async Task<string> RunJs(string script)
    {
        var c = core(); if (c == null) return "null";
        try { return await c.ExecuteScriptAsync(script); } catch { return "null"; }
    }

    async Task Send(string method, params object[] payloads)
    {
        var c = core(); if (c == null) return;
        try { foreach (var p in payloads) await c.CallDevToolsProtocolMethodAsync(method, Json(p)); }
        catch { }
    }

    // ---------- D-pad focus ----------
    public async Task<bool> MoveFocus(string dir) =>
        await RunJs($"window.__couch ? __couch.move('{dir}') : false") == "true";

    public void ClearFocus() => _ = RunJs("window.__couch && __couch.clearFocus()");

    public async Task<(double X, double Y)?> FocusedPoint()
    {
        string r = await RunJs("window.__couch ? __couch.rect() : null");
        if (string.IsNullOrEmpty(r) || r == "null") return null;
        using var doc = JsonDocument.Parse(r);
        return (doc.RootElement.GetProperty("x").GetDouble(), doc.RootElement.GetProperty("y").GetDouble());
    }

    public async Task<bool> FocusedIsEditable() =>
        await RunJs("window.__couch ? __couch.editable() : false") == "true";

    // ---------- Mouse ----------
    public Task Click(double x, double y) => Send("Input.dispatchMouseEvent",
        new { type = "mouseMoved", x, y },
        new { type = "mousePressed", x, y, button = "left", clickCount = 1 },
        new { type = "mouseReleased", x, y, button = "left", clickCount = 1 });

    public void ShowCursor(double x, double y)
    {
        _ = RunJs($"window.__couch && __couch.cursor({Num(x)},{Num(y)})");
        _ = Send("Input.dispatchMouseEvent", new { type = "mouseMoved", x, y });
    }

    public void HideCursor() => _ = RunJs("window.__couch && __couch.hideCursor()");

    public void Wheel(double x, double y, double dy) =>
        _ = Send("Input.dispatchMouseEvent", new { type = "mouseWheel", x, y, deltaX = 0, deltaY = dy });

    // ---------- Keyboard ----------
    public void InsertText(string text) => _ = Send("Input.insertText", new { text });

    public void Backspace() => _ = Send("Input.dispatchKeyEvent",
        new { type = "rawKeyDown", windowsVirtualKeyCode = 8, key = "Backspace", code = "Backspace" },
        new { type = "keyUp", windowsVirtualKeyCode = 8, key = "Backspace", code = "Backspace" });

    public void Enter() => _ = Send("Input.dispatchKeyEvent",
        new { type = "keyDown", windowsVirtualKeyCode = 13, key = "Enter", code = "Enter", text = "\r", unmodifiedText = "\r" },
        new { type = "keyUp", windowsVirtualKeyCode = 13, key = "Enter", code = "Enter" });

    public void ExitFullscreen() => _ = RunJs("document.exitFullscreen && document.exitFullscreen()");
}