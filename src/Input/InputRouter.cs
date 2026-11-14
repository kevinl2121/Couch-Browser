using CouchBrowser.UI;
using CouchBrowser.Web;

namespace CouchBrowser.Input;

/// <summary>All button mappings live here: menu, keyboard and page browsing.</summary>
public sealed class InputRouter
{
    const int HoldMs = 700;          // hold B to close tab
    const int ExitHoldMs = 800;      // hold View + Menu to exit

    readonly BrowserForm host;
    readonly Controller pad;
    readonly KeyboardPanel keyboard;
    readonly MenuPanel menu;
    readonly PageBridge page;

    bool cursorOn;
    double curX, curY;
    long triggerAt;
    long exitComboSince;
    long backDownAt;
    bool backHoldUsed;
    int tick;

    public InputRouter(BrowserForm host, Controller pad, KeyboardPanel keyboard, MenuPanel menu, PageBridge page)
    {
        this.host = host; this.pad = pad; this.keyboard = keyboard; this.menu = menu; this.page = page;
    }

    public void ResetCursor()
    {
        if (!cursorOn) return;
        cursorOn = false;
        page.HideCursor();
    }

    public void Tick()
    {
        tick++;
        if (!host.IsForeground) { pad.Pause(); return; }
        if (!pad.Update()) return;
        if (ExitComboHeld()) return;

        if (menu.Visible) MenuInput();
        else if (keyboard.Visible) KeyboardInput();
        else PageInput();
    }

    /// <summary>Hold View + Menu for ~1 second to exit the app (works on any screen).</summary>
    bool ExitComboHeld()
    {
        ushort b = pad.Pad.Buttons;
        bool held = (b & XInput.Back) != 0 && (b & XInput.Start) != 0;
        if (!held) { exitComboSince = 0; return false; }

        long now = Environment.TickCount64;
        if (exitComboSince == 0) exitComboSince = now;
        else if (now - exitComboSince >= ExitHoldMs) host.Close();
        return true;
    }

    // ---------- Menu ----------
    void MenuInput()
    {
        if (pad.Direction == "up") menu.Step(-1);
        else if (pad.Direction == "down") menu.Step(1);

        if (pad.Pressed(XInput.A)) menu.RunSelected();
        else if (pad.Pressed(XInput.B) || pad.Pressed(XInput.Start)) menu.Visible = false;
    }

    // ---------- On-screen keyboard ----------
    void KeyboardInput()
    {
        if (pad.Direction != null) keyboard.Nav(pad.Direction);
        if (pad.Pressed(XInput.A)) TypeKey(keyboard.Selected.Value);
        if (pad.Pressed(XInput.X)) TypeKey("{BKSP}");
        if (pad.Pressed(XInput.Y)) TypeKey(" ");
        if (pad.Pressed(XInput.LB)) TypeKey("{SHIFT}");
        if (pad.Pressed(XInput.Start)) TypeKey("{ENTER}");
        if (pad.Pressed(XInput.B)) keyboard.Visible = false;
    }

    void TypeKey(string value)
    {
        switch (value)
        {
            case "{SHIFT}":
                keyboard.Shift = !keyboard.Shift;
                break;
            case "{LAYOUT}":
                keyboard.ToggleLayout();
                break;
            case "{BKSP}":
                keyboard.Backspace();
                if (!keyboard.AddressMode) page.Backspace();
                break;
            case "{ENTER}":
                keyboard.Visible = false;
                if (keyboard.AddressMode)
                {
                    string url = BrowserForm.ResolveInput(keyboard.Buffer);
                    if (url != null) host.Navigate(url);
                }
                else page.Enter();
                break;
            default:
                string text = keyboard.Apply(value);
                keyboard.Buffer += text;
                if (!keyboard.AddressMode) page.InsertText(text);
                break;
        }
        keyboard.Invalidate();
    }

    // ---------- Browsing ----------
    void PageInput()
    {
        var (vw, vh) = host.ViewportCss();
        long now = Environment.TickCount64;

        if (pad.Direction != null) { ResetCursor(); _ = MoveFocus(pad.Direction, vh); }
        if (pad.Pressed(XInput.A)) _ = Click();
        if (pad.Pressed(XInput.X)) host.ShowKeyboard(addressMode: true);
        if (pad.Pressed(XInput.Y)) host.ShowKeyboard(addressMode: false);
        if (pad.Pressed(XInput.LB)) host.NextTab(-1);
        if (pad.Pressed(XInput.RB)) host.NextTab(1);
        if (pad.Pressed(XInput.Start)) host.OpenMenu();
        if (pad.Pressed(XInput.Back)) host.Navigate(BrowserForm.Home);

        // B: tap = back (on release), hold = close tab
        if (pad.Pressed(XInput.B)) { backDownAt = now; backHoldUsed = false; }
        if (pad.Held(XInput.B) && !backHoldUsed && backDownAt != 0 && now - backDownAt >= HoldMs)
        {
            backHoldUsed = true;
            host.CloseTab();
        }
        if (pad.Released(XInput.B))
        {
            if (!backHoldUsed && backDownAt != 0) host.GoBack();
            backDownAt = 0;
        }

        // Triggers: page up / page down, repeating while held
        int trig = pad.Pad.RightTrigger > 120 ? 1 : pad.Pad.LeftTrigger > 120 ? -1 : 0;
        if (trig == 0) triggerAt = 0;
        else if (now >= triggerAt)
        {
            bool first = triggerAt == 0;
            Scroll(trig * vh * 0.8, vw, vh);
            triggerAt = now + (first ? 400 : 200);
        }

        // Left stick: smooth scroll
        double ly = Controller.Axis(pad.Pad.LY);
        if (ly != 0 && tick % 2 == 0) Scroll(-ly * 60, vw, vh);

        // Right stick: free cursor
        double rx = Controller.Axis(pad.Pad.RX), ry = Controller.Axis(pad.Pad.RY);
        if (rx != 0 || ry != 0)
        {
            if (!cursorOn)
            {
                cursorOn = true;
                curX = vw / 2;
                curY = vh / 2;
                page.ClearFocus();
            }
            curX = Math.Clamp(curX + rx * 22, 1, Math.Max(2, vw - 1));
            curY = Math.Clamp(curY - ry * 22, 1, Math.Max(2, vh - 1));
            page.ShowCursor(curX, curY);
        }
    }

    void Scroll(double dy, double vw, double vh)
    {
        double x = cursorOn ? curX : vw / 2, y = cursorOn ? curY : vh / 2;
        page.Wheel(x, y, dy);
    }

    async Task MoveFocus(string dir, double vh)
    {
        bool moved = await page.MoveFocus(dir);
        if (!moved && (dir == "up" || dir == "down"))
        {
            var (vw, _) = host.ViewportCss();
            Scroll((dir == "down" ? 1 : -1) * vh * 0.4, vw, vh);
        }
    }

    async Task Click()
    {
        try
        {
            double x, y;
            if (cursorOn) { x = curX; y = curY; }
            else
            {
                var point = await page.FocusedPoint();
                if (point == null) { await MoveFocus("down", host.ViewportCss().H); return; }
                (x, y) = point.Value;
            }
            await page.Click(x, y);
            await Task.Delay(250);
            if (await page.FocusedIsEditable()) host.ShowKeyboard(addressMode: false);
        }
        catch { }
    }
}