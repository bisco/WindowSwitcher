using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WindowSwitcher;

public class WindowInfo : INotifyPropertyChanged
{
    public IntPtr Handle { get; init; }
    public string Title { get; init; } = "";
    public BitmapSource? Icon { get; init; }

    private Brush _foreground = Brushes.White;
    public Brush Foreground
    {
        get => _foreground;
        set { _foreground = value; OnChanged(nameof(Foreground)); }
    }

    protected void OnChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class MainWindow : Window
{
    private readonly ObservableCollection<WindowInfo> _windows = new();
    private HwndSource? _hwndSource;
    private bool _expanded;
    private bool _contextMenuOpen;

    private static readonly Brush ActiveBrush   = new SolidColorBrush(Color.FromRgb(0xcd, 0xd6, 0xf4));
    private static readonly Brush InactiveBrush = new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86));

    private const double CollapsedWidth = 48;
    private const double ExpandedWidth = 220;
    private DateTime? _hoverStart;
    public MainWindow()
    {
        InitializeComponent();

        WindowList.ItemsSource = _windows;
        Width = CollapsedWidth;
        RefreshList();

        var listTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        listTimer.Tick += (_, _) => RefreshList();
        listTimer.Start();

        var activeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        activeTimer.Tick += (_, _) => RefreshActive();
        activeTimer.Start();
        var hoverTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };

        hoverTimer.Tick += (_, _) =>
        {
            if (_contextMenuOpen)
                return;

            if (IsMouseOver)
            {
                _hoverStart ??= DateTime.Now;

                if (!_expanded &&
                    DateTime.Now - _hoverStart > TimeSpan.FromMilliseconds(300))
                {
                    _expanded = true;
                    AnimateWidth(ExpandedWidth);
                }
            }
            else
            {
                _hoverStart = null;

                if (_expanded)
                {
                    _expanded = false;
                    AnimateWidth(CollapsedWidth);
                }
            }
        };

        hoverTimer.Start();
    }

    private void RegisterAppBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        Win32Api.RegisterAppBar(hwnd);

        var screenLeft = Win32Api.GetSystemMetrics(Win32Api.SM_XVIRTUALSCREEN);
        var screenTop = Win32Api.GetSystemMetrics(Win32Api.SM_YVIRTUALSCREEN);
        var screenHeight = Win32Api.GetSystemMetrics(Win32Api.SM_CYVIRTUALSCREEN);

        var width = (int)CollapsedWidth;

        var rc = Win32Api.SetAppBarPosition(
            hwnd,
            Win32Api.ABE_LEFT,
            screenLeft,
            screenTop,
            screenLeft + width,
            screenTop + screenHeight);

        ApplyPixelBounds(rc);
    }

    private void ApplyPixelBounds(Win32Api.RECT rc)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget == null)
            return;

        var transform = source.CompositionTarget.TransformFromDevice;

        var topLeft = transform.Transform(new Point(rc.left, rc.top));
        var bottomRight = transform.Transform(new Point(rc.right, rc.bottom));

        Left = topLeft.X;
        Top = topLeft.Y;
        Width = bottomRight.X - topLeft.X;
        Height = bottomRight.Y - topLeft.Y;
    }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        _hwndSource?.AddHook(WndProc);

        var hwnd = new WindowInteropHelper(this).Handle;
        bool ok = Win32Api.RegisterHotKey(hwnd, Win32Api.HOTKEY_ID,
            Win32Api.MOD_CONTROL | Win32Api.MOD_SHIFT | Win32Api.MOD_NOREPEAT,
            Win32Api.VK_SPACE);

        if (!ok) MessageBox.Show("ホットキーの登録に失敗しました。");
        HideFromAltTab();
        RegisterAppBar();
    }

    protected override void OnClosed(EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        Win32Api.UnregisterHotKey(hwnd, Win32Api.HOTKEY_ID);
        Win32Api.UnregisterAppBar(hwnd);

        _hwndSource?.RemoveHook(WndProc);
        base.OnClosed(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == Win32Api.HOTKEY_ID)
        {
            ToggleVisibility();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ToggleVisibility()
    {
        if (IsVisible) Hide();
        else { Show(); Activate(); }
    }

    private void AnimateWidth(double width)
    {
        if (width > CollapsedWidth)
            Topmost = true;

        var animation = new DoubleAnimation
        {
            To = width,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            }
        };

        if (width <= CollapsedWidth)
        {
            animation.Completed += (_, _) =>
            {
                Topmost = false;
            };
        }

        BeginAnimation(WidthProperty, animation);
    }

    private void RefreshList()
    {
        var current = new List<WindowInfo>();

        Win32Api.EnumWindows((hWnd, _) =>
        {
            if (!Win32Api.IsAltTabWindow(hWnd)) return true;

            var len = Win32Api.GetWindowTextLength(hWnd);
            if (len == 0) return true;

            var sb = new StringBuilder(len + 1);
            Win32Api.GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();

            if (string.IsNullOrWhiteSpace(title)) return true;

            if (title == "Switcher") return true;
            if (title == "Windows 入力エクスペリエンス") return true;
            if (title == "Windows Input Experience") return true;
            var icon = Win32Api.GetWindowIcon(hWnd);
            current.Add(new WindowInfo { Handle = hWnd, Title = title, Icon = icon });
            return true;
        }, IntPtr.Zero);

        for (int i = _windows.Count - 1; i >= 0; i--)
            if (!current.Exists(w => w.Handle == _windows[i].Handle))
                _windows.RemoveAt(i);

        foreach (var w in current)
            if (!_windows.Any(x => x.Handle == w.Handle))
                _windows.Add(w);

        RefreshActive();
    }

    private void RefreshActive()
    {
        var active = Win32Api.GetForegroundWindow();
        foreach (var w in _windows)
            w.Foreground = w.Handle == active ? ActiveBrush : InactiveBrush;
    }

    private void WindowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WindowList.SelectedItem is not WindowInfo info) return;

        // 最小化されている場合だけ復元する
        if (Win32Api.IsIconic(info.Handle))
            Win32Api.ShowWindow(info.Handle, Win32Api.SW_RESTORE);

        Win32Api.SetForegroundWindow(info.Handle);

        WindowList.SelectedItem = null;
    }

    private void WindowList_PreviewMouseRightButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe &&
            fe.DataContext is WindowInfo info)
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(0x31, 0x32, 0x44)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(0x45, 0x47, 0x5a))
            };

            menu.Opened += (_, _) =>
            {
                _contextMenuOpen = true;
            };

            menu.Closed += (_, _) =>
            {
                _contextMenuOpen = false;

                if (!IsMouseOver)
                {
                    _expanded = false;
                    AnimateWidth(CollapsedWidth);
                }
            };

            var item = new MenuItem
            {
                Header = "このウィンドウを終了",
                Foreground = new SolidColorBrush(
                    Color.FromRgb(0xf3, 0x8b, 0xa8)),
                Tag = info
            };

            item.Click += CloseWindow_Click;

            menu.Items.Add(item);
            menu.IsOpen = true;

            e.Handled = true;
        }
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.Tag is WindowInfo info)
            Win32Api.SendMessage(info.Handle, Win32Api.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void HideFromAltTab()
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        var exStyle = Win32Api.GetWindowLongPtr(hwnd, Win32Api.GWL_EXSTYLE).ToInt64();

        exStyle |= Win32Api.WS_EX_TOOLWINDOW;
        exStyle &= ~Win32Api.WS_EX_APPWINDOW;

        Win32Api.SetWindowLongPtr(hwnd, Win32Api.GWL_EXSTYLE, new IntPtr(exStyle));
    }

}