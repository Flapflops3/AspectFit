using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace AspectWindow;

[ModInitializer(nameof(Initialize))]
public static class Mod
{
    private const nuint SubclassId = 0x41535057;
    // A permanent managed root is required while Windows owns this function pointer.
    private static readonly NativeWindow.SubclassProc Callback = WindowProc;
    private static nint _hwnd;
    private static bool _initialized, _enabled, _inSizeMove, _failed;
    private static double _ratio = 16d / 9, _dragRatio;
    private static Rect _start;
    private static int _frameWidth, _frameHeight, _minWidth = 1, _minHeight = 1;
    private static AspectRatioSetting? _lastSetting;
    private static Vector2I _lastSavedSize;

    public static void Initialize()
    {
        if (_initialized || !OperatingSystem.IsWindows()) return;
        _initialized = true;
        // Defer to the engine main thread; SetWindowSubclass is thread-affine.
        Callable.From(Attach).CallDeferred();
    }

    private static void Attach()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            GD.PushError("[AspectWindow] No SceneTree; window hook not installed.");
            return;
        }
        tree.ProcessFrame += Refresh;
        Refresh();
    }

    private static void Refresh()
    {
        if (_failed) return;
        try
        {
            nint handle = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
            if (handle == 0) return;
            if (handle != _hwnd)
            {
                if (_hwnd != 0) NativeWindow.RemoveWindowSubclass(_hwnd, Callback, SubclassId);
                if (!NativeWindow.SetWindowSubclass(handle, Callback, SubclassId, 0))
                    throw new InvalidOperationException("SetWindowSubclass failed on engine thread.");
                _hwnd = handle;
                GD.Print("[AspectWindow] Native window hook installed (v1.0.0).");
            }
            var settings = SaveManager.Instance.SettingsSave;
            _enabled = !settings.Fullscreen && settings.ResizeWindows &&
                DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed;
            var minimum = DisplayServer.WindowGetMinSize();
            _minWidth = Math.Max(1, minimum.X);
            _minHeight = Math.Max(1, minimum.Y);
            if (_inSizeMove) return;
            if (_lastSetting != settings.AspectRatioSetting || _lastSavedSize != settings.WindowSize)
            {
                _ratio = settings.AspectRatioSetting switch
                {
                    AspectRatioSetting.FourByThree => 4d / 3,
                    AspectRatioSetting.SixteenByTen => 16d / 10,
                    AspectRatioSetting.SixteenByNine => 16d / 9,
                    // Matches the game's actual 2580x1080 content scale.
                    AspectRatioSetting.TwentyOneByNine => 2580d / 1080,
                    _ => settings.WindowSize.X > 0 && settings.WindowSize.Y > 0
                        ? (double)settings.WindowSize.X / settings.WindowSize.Y : 16d / 9
                };
                _lastSetting = settings.AspectRatioSetting;
                _lastSavedSize = settings.WindowSize;
                GD.Print($"[AspectWindow] Client aspect ratio: {_ratio:F6} ({settings.AspectRatioSetting}).");
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private static nint WindowProc(nint hwnd, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        // Let Godot handle its messages first, then constrain the final WM_SIZING rect.
        nint result = NativeWindow.DefSubclassProc(hwnd, message, wParam, lParam);
        try
        {
            switch (message)
            {
                case 0x0231: // WM_ENTERSIZEMOVE
                    _inSizeMove = NativeWindow.GetWindowRect(hwnd, out _start);
                    _dragRatio = _ratio;
                    break;
                case 0x0214: // WM_SIZING -- only interactive border resizing
                    if (!_enabled || _failed || NativeWindow.IsZoomed(hwnd) || lParam == 0) break;
                    if (!NativeWindow.GetWindowRect(hwnd, out var outer) ||
                        !NativeWindow.GetClientRect(hwnd, out var client)) break;
                    if (!_inSizeMove) { _start = outer; _dragRatio = _ratio; }
                    // Query each time so a DPI/monitor change doesn't reuse stale frame metrics.
                    _frameWidth = Math.Max(0, outer.Width - client.Width);
                    _frameHeight = Math.Max(0, outer.Height - client.Height);
                    var rect = Marshal.PtrToStructure<Rect>(lParam);
                    rect = ResizeGeometry.Constrain(rect, _start, (int)wParam,
                        _frameWidth, _frameHeight, _dragRatio, _minWidth, _minHeight);
                    Marshal.StructureToPtr(rect, lParam, false);
                    return 1;
                case 0x0232: // WM_EXITSIZEMOVE
                    _inSizeMove = false;
                    break;
                case 0x0082: // WM_NCDESTROY
                    NativeWindow.RemoveWindowSubclass(hwnd, Callback, SubclassId);
                    if (_hwnd == hwnd) _hwnd = 0;
                    _inSizeMove = false;
                    break;
            }
        }
        // Never let a managed exception escape through a native callback.
        catch (Exception error) { Fail(error); }
        return result;
    }

    private static void Fail(Exception error)
    {
        _failed = true;
        _enabled = false;
        GD.PushError($"[AspectWindow] Disabled after error: {error}");
    }
}
