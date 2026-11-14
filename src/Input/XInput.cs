using System.Runtime.InteropServices;

namespace CouchBrowser.Input;

/// <summary>Raw access to Xbox controllers through Windows' XInput.</summary>
public static class XInput
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Gamepad
    {
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short LX, LY, RX, RY;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct State { public uint Packet; public Gamepad Pad; }

    [DllImport("xinput1_4.dll")] static extern uint XInputGetState(uint index, out State state);

    public static bool TryGet(uint index, out Gamepad pad)
    {
        uint r = XInputGetState(index, out var s);
        pad = s.Pad;
        return r == 0;
    }

    public const ushort Up = 0x0001, Down = 0x0002, Left = 0x0004, Right = 0x0008,
        Start = 0x0010, Back = 0x0020, LB = 0x0100, RB = 0x0200,
        A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;
}

/// <summary>Tracks one controller: button presses, D-pad repeat and stick values.</summary>
public sealed class Controller
{
    int index = -1;
    long nextScan, repeatAt;
    ushort prev, cur;
    string heldDir;
    bool resync = true;

    public XInput.Gamepad Pad { get; private set; }

    /// <summary>"up", "down", "left", "right" on press and on auto-repeat; otherwise null.</summary>
    public string Direction { get; private set; }

    /// <summary>Ignore whatever is held when we come back (e.g. after alt-tab).</summary>
    public void Pause() => resync = true;

    /// <summary>True only on the frame the button goes down.</summary>
    public bool Pressed(ushort button) => (cur & button) != 0 && (prev & button) == 0;

    /// <summary>True only on the frame the button comes up.</summary>
    public bool Released(ushort button) => (cur & button) == 0 && (prev & button) != 0;

    /// <summary>True every frame the button is down.</summary>
    public bool Held(ushort button) => (cur & button) != 0;

    public bool Update()
    {
        if (!Read(out var pad)) { resync = true; return false; }
        Pad = pad;
        if (resync)
        {
            prev = cur = pad.Buttons;
            heldDir = DirOf(cur);
            Direction = null;
            resync = false;
            return true;
        }
        prev = cur;
        cur = pad.Buttons;
        Direction = Repeat(DirOf(cur));
        return true;
    }

    /// <summary>Stick value in -1..1 with dead zone and a smooth curve.</summary>
    public static double Axis(short v)
    {
        double x = Math.Max(-1.0, v / 32767.0), a = Math.Abs(x);
        const double dz = 0.24;
        if (a < dz) return 0;
        double n = (a - dz) / (1 - dz);
        return Math.Sign(x) * n * n;
    }

    bool Read(out XInput.Gamepad pad)
    {
        pad = default;
        long now = Environment.TickCount64;
        if (index >= 0)
        {
            if (XInput.TryGet((uint)index, out pad)) return true;
            index = -1;
            nextScan = now + 1000;
            return false;
        }
        if (now < nextScan) return false;
        nextScan = now + 1000;
        for (int i = 0; i < 4; i++)
            if (XInput.TryGet((uint)i, out pad)) { index = i; return true; }
        return false;
    }

    static string DirOf(ushort b) =>
        (b & XInput.Up) != 0 ? "up" : (b & XInput.Down) != 0 ? "down" :
        (b & XInput.Left) != 0 ? "left" : (b & XInput.Right) != 0 ? "right" : null;

    string Repeat(string d)
    {
        long now = Environment.TickCount64;
        if (d == null) { heldDir = null; return null; }
        if (d != heldDir) { heldDir = d; repeatAt = now + 380; return d; }
        if (now >= repeatAt) { repeatAt = now + 85; return d; }
        return null;
    }
}