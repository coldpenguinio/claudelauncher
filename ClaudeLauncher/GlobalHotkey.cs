using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ClaudeLauncher;

public class GlobalHotkey : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // Virtual key codes
    public const uint VK_C = 0x43;

    private readonly int _id;
    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private bool _disposed;

    public event EventHandler? HotkeyPressed;

    public GlobalHotkey(uint modifiers, uint key)
    {
        _id = GetHashCode();

        // Create a hidden window to receive hotkey messages
        var parameters = new HwndSourceParameters("ClaudeLauncherHotkey")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
        _hwnd = _source.Handle;

        if (!RegisterHotKey(_hwnd, _id, modifiers | MOD_NOREPEAT, key))
        {
            throw new InvalidOperationException("Could not register hotkey. It may be in use by another application.");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        UnregisterHotKey(_hwnd, _id);
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
