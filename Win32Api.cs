using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace WindowSwitcher;

public static class Win32Api
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern long GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr GetClassLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    public const int SW_SHOWNORMAL = 1;

    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;


    public const int SW_RESTORE = 9;

    public const int HOTKEY_ID = 1;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_NOREPEAT = 0x4000;
    public const uint VK_SPACE = 0x20;

    public const int GWL_EXSTYLE = -20;
    public const long WS_EX_TOOLWINDOW = 0x00000080L;
    public const long WS_EX_NOACTIVATE = 0x08000000L;

    public const uint GW_OWNER = 4;

    public const uint WM_CLOSE = 0x0010;
    public const uint WM_GETICON = 0x007F;

    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int GCL_HICON = -14;

    public const int ABM_NEW = 0x00000000;
    public const int ABM_REMOVE = 0x00000001;
    public const int ABM_QUERYPOS = 0x00000002;
    public const int ABM_SETPOS = 0x00000003;

    public const int ABE_LEFT = 0;

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public const long WS_EX_APPWINDOW = 0x00040000L;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [DllImport("shell32.dll")]
    private static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    public static bool IsAltTabWindow(IntPtr hWnd)
    {
        if (!IsWindowVisible(hWnd)) return false;

        var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

        if ((exStyle & WS_EX_TOOLWINDOW) != 0) return false;
        if ((exStyle & WS_EX_NOACTIVATE) != 0) return false;

        var owner = GetWindow(hWnd, GW_OWNER);
        if (owner != IntPtr.Zero) return false;

        return true;
    }

    public static BitmapSource? GetWindowIcon(IntPtr hWnd)
    {
        try
        {
            var hIcon = SendMessage(hWnd, WM_GETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);

            if (hIcon == IntPtr.Zero)
                hIcon = SendMessage(hWnd, WM_GETICON, (IntPtr)ICON_BIG, IntPtr.Zero);

            if (hIcon == IntPtr.Zero)
                hIcon = GetClassLong(hWnd, GCL_HICON);

            if (hIcon != IntPtr.Zero)
            {
                var bs = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bs.Freeze();
                return bs;
            }
        }
        catch
        {
        }

        return null;
    }

    public static void RegisterAppBar(IntPtr hwnd)
    {
        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = hwnd
        };

        SHAppBarMessage(ABM_NEW, ref data);
    }

    public static void UnregisterAppBar(IntPtr hwnd)
    {
        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = hwnd
        };

        SHAppBarMessage(ABM_REMOVE, ref data);
    }

    public static RECT SetAppBarPosition(
        IntPtr hwnd,
        int edge,
        int left,
        int top,
        int right,
        int bottom)
    {
        var width = right - left;

        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = hwnd,
            uEdge = edge,
            rc = new RECT
            {
                left = left,
                top = top,
                right = right,
                bottom = bottom
            }
        };

        SHAppBarMessage(ABM_QUERYPOS, ref data);

        // 左端 AppBar なので、左位置は固定して幅だけ維持する
        data.rc.left = left;
        data.rc.right = left + width;

        SHAppBarMessage(ABM_SETPOS, ref data);

        return data.rc;
    }
}