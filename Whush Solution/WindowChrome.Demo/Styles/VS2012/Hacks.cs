using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;

namespace WindowChrome.Demo.Styles.VS2012
{
    internal static class Hacks
    {
        /// <summary>
        /// Requires a Border "PART_WindowContainer" in the window style template.
        /// Container will be positioned to fill an entire working are of the screen when the window is maximized.
        /// </summary>
        /// <param name="wpfWindow"></param>
        public static void FixWpfPositioning(Window wpfWindow)
        {
            wpfWindow.LocationChanged += WindowLocationChanged;
            wpfWindow.SizeChanged += WindowSizeChanged;
        }

        static void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustWindowWorkingZone((Window)sender);
        }

        static void WindowLocationChanged(object sender, EventArgs e)
        {
            AdjustWindowWorkingZone((Window)sender);
        }

        /// <summary>
        /// Requires a Border "PART_TaskBarHotZone" in the window style template.
        /// Expects PART_TaskBarHotZone to generate mouse events only when the taskbar activation is required.
        /// </summary>
        /// <param name="wpfWindow"></param>
        public static void FixWpfMaximizeTaskbar(Window wpfWindow)
        {
            var hotzoneBorder = (Border)wpfWindow.Template.FindName("PART_TaskBarHotZone", wpfWindow);
            hotzoneBorder.MouseMove += HotzoneBorderMouseMove;
        }

        static void HotzoneBorderMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var element = (FrameworkElement)sender;
            Hacks.TaskbarActivationCheck(
                element.GetTemplatedWindow(),
                element.PointToScreen(e.GetPosition(element)));
        }

        static void AdjustWindowWorkingZone(Window w)
        {
            if (w.WindowStyle != WindowStyle.None) return;
            var containerBorder = (Border)w.Template.FindName("PART_WindowContainer", w);
            AdjustPadding(w, containerBorder);
        }

        static bool adjustmentInProgress = false;
        static object adjustmentLock = new object();

        static void AdjustPadding(Window window, Border border)
        {
            lock (adjustmentLock)
            {
                if (adjustmentInProgress) return;
                adjustmentInProgress = true;
            }

            window.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                new Action(() =>
                    {
                        // Ignore adjustment requests fired in rapid sequence.
                        // Current implementation won't support multiple windows per AppDomain,
                        // with more than one top-level window simultaneously re-positioned.
                        System.Threading.Thread.Sleep(10);
                        var snap = AdjustPaddingToSnappingArea(window, border);
                        if (snap != SnapSide.None && OnAfterSnapping != null) OnAfterSnapping(snap);
                        adjustmentInProgress = false;
                    }));
        }

        public enum SnapSide
        {
            None,
            Fill,
            LeftHalf,
            RightHalf,
            UpDown,
        }

        public static event Action<SnapSide> OnAfterSnapping;

        static Rect ActualRectOnScreen(this FrameworkElement element)
        {
            return new Rect(element.PointToScreen(new Point(0, 0)),
                element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)));
        }

        static SnapSide AdjustPaddingToSnappingArea(Window window, Border border)
        {
            var screen = ScreenFromWpfWindow(window);
            var outer = border.ActualRectOnScreen();
            var snap = ShouldSnap(screen, window, outer);
            var regularBorder = MaximizedWindowBorder();
            Thickness targetThickness;
            if (snap == SnapSide.None)
            {
                targetThickness = regularBorder;
            }
            else
            {
                var targetRect = GetSnapRect(snap, screen.WorkingArea, outer);
                targetThickness = outer.GetThickessToInnerRect(targetRect);
                if (snap != SnapSide.Fill)
                {
                    // adjust left and right sides to keep custom visual
                    if (snap == SnapSide.UpDown || snap == SnapSide.LeftHalf) targetThickness.Right = regularBorder.Right;
                    if (snap == SnapSide.UpDown || snap == SnapSide.RightHalf) targetThickness.Left = regularBorder.Left;
                }
            }

            if (border.Padding != targetThickness) border.Padding = targetThickness;

            return snap;
        }

        static bool Within(this double value, double position, double negativeDelta, double positiveDelta)
        {
            return value >= position - negativeDelta && value <= position + positiveDelta;
        }

        static Thickness GetThickessToInnerRect(this Rect outerRect, Rect innerRect)
        {
            return new Thickness {
                Left = Math.Max(0, innerRect.Left - outerRect.Left),
                Top = Math.Max(0, innerRect.Top - outerRect.Top),
                Right = Math.Max(0, outerRect.Right - innerRect.Right),
                Bottom = Math.Max(0, outerRect.Bottom - innerRect.Bottom),
            };
        }

        static SnapSide ShouldSnap(Screen screen, Window window, Rect rect)
        {
            // Check if window is close to a snap position on the screen.
            var waSnapping = ShouldSnapToArea(screen.WorkingArea, rect);
            if (waSnapping != SnapSide.None) return waSnapping;
            // also WPF may ignore the WorkingArea and take over an entire screen instead.
            // If this happens, the window may be in a snap position relative to Bounds.
            var boundsSnapping = ShouldSnapToArea(screen.Bounds, rect);
            return boundsSnapping;
        }

        static SnapSide ShouldSnapToArea(System.Drawing.Rectangle area, Rect rect)
        {
            var midX = 0.5 * (area.Left + area.Right);

            bool w = rect.Left.Within(area.Left, 8.1, 1.1);
            bool n = rect.Top.Within(area.Top, 8.1, 1.1);
            bool e = rect.Right.Within(area.Right, 1.1, 8.1);
            bool s = rect.Bottom.Within(area.Bottom, 1.1, 8.1);

            bool wm = rect.Left.Within(midX, 10, 10);
            bool em = rect.Right.Within(midX, 10, 10);

            if (w && n && e && s) return SnapSide.Fill;
            if (w && n && s && em) return SnapSide.LeftHalf;
            if (e && n && s && wm) return SnapSide.RightHalf;
            if (n && s) return SnapSide.UpDown;

            return SnapSide.None;
        }

        static Rect GetSnapRect(SnapSide snap, System.Drawing.Rectangle area, Rect original = new Rect())
        {
            switch (snap)
            {
                case SnapSide.Fill:
                    return new Rect(area.Left, area.Top, area.Width, area.Height);
                case SnapSide.LeftHalf:
                    return new Rect(area.Left, area.Top, Math.Floor(0.5 * area.Width), area.Height);
                case SnapSide.RightHalf:
                    return new Rect(Math.Ceiling(0.5 * (area.Left + area.Right)), area.Top, Math.Floor(0.5 * area.Width), area.Height);
                case SnapSide.UpDown:
                    return new Rect(original.Left, area.Top, original.Width, area.Height);
                default:
                    return original;
            }
        }

        /// <summary>
        /// Returns screen area around the border that is not available to regular windows.
        /// Used to fix an issue when WPF sizes a window based on the entire display size
        /// instead of the working area only.
        /// </summary>
        /// <param name="wpfWindow"></param>
        /// <returns></returns>
        static Thickness ScreenNonWorkingZone(Window wpfWindow)
        {
            var screen = ScreenFromWpfWindow(wpfWindow);
            var b = screen.Bounds;
            var a = screen.WorkingArea;
            return new Thickness(a.Left - b.Left, a.Top - b.Top, b.Right - a.Right, b.Bottom - a.Bottom);
        }

        static Thickness Add(this Thickness a, Thickness b)
        {
            return new Thickness(a.Left + b.Left, a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom);
        }

        static Thickness MaximizedWindowBorder()
        {
            // MAximized WPF window extends past the screen bounds. 
            // This seems to match system metrics below, but needs further verification.
            // 0.5 * (SystemParameters.MaximizedPrimaryScreenHeight - SystemParameters.WorkArea.Height) - SystemParameters.BorderWidth
            // 0.5 * (SystemParameters.MaximizedPrimaryScreenWidth - SystemParameters.WorkArea.Width) - SystemParameters.BorderWidth
            return new Thickness(7, 7, 7, 5);
        }

        static System.Windows.Forms.Screen ScreenFromWpfWindow(Window window)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            return System.Windows.Forms.Screen.FromHandle(helper.Handle);
        }

        static void TaskbarActivationCheck(Window window, Point mousePoint)
        {
            var taskbar = new TaskbarInfo();
            if (!taskbar.AutoHide) return;

            double delta = 2.0;
            var screen = ScreenFromWpfWindow(window);
            if (screen.DeviceName != taskbar.ScreenName) return;

            bool isLeft = Math.Abs(screen.Bounds.Left - mousePoint.X) < delta;
            bool isTop = Math.Abs(screen.Bounds.Top - mousePoint.Y) < delta;
            bool isRight = Math.Abs(screen.Bounds.Right - mousePoint.X) < delta;
            bool isBottom = Math.Abs(screen.Bounds.Bottom - mousePoint.Y) < delta;

            if (isLeft && taskbar.Position == Shell32.AppBarEdge.Left) ShowTaskbar();
            if (isTop && taskbar.Position == Shell32.AppBarEdge.Top) ShowTaskbar();
            if (isRight && taskbar.Position == Shell32.AppBarEdge.Right) ShowTaskbar();
            if (isBottom && taskbar.Position == Shell32.AppBarEdge.Bottom) ShowTaskbar();
        }

        static void ShowTaskbar()
        {
            KeyPress(System.Windows.Forms.Keys.LWin, System.Windows.Forms.Keys.T);
            System.Threading.Thread.Sleep(50);
            KeyPress(System.Windows.Forms.Keys.Escape);
        }

        const int KEYEVENTF_EXTENDEDKEY = 1;
        const int KEYEVENTF_KEYUP = 2;

        static void KeyPress(params System.Windows.Forms.Keys[] keys)
        {
            foreach (var key in keys)
                User32.keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY, 0);
            foreach (var key in keys)
                User32.keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        static class Shell32
        {
            [DllImport("shell32.dll", SetLastError = true)]
            public static extern IntPtr SHAppBarMessage(AppBarMessage dwMessage, [In] ref APPBARDATA pData);

            public enum AppBarMessage : uint
            {
                New = 0,
                Remove = 1,
                QueryPos = 2,
                SetPos = 3,
                GetState = 4,
                GetTaskbarPos = 5,
                Activate = 6,
                GetAutoHideBar = 7,
                SetAutoHideBar = 8,
                WindowPosChanged = 9,
                SetState = 10,
            }

            public enum AppBarEdge : uint
            {
                Left = 0,
                Top = 1,
                Right = 2,
                Bottom = 3,
            }

            [Flags]
            public enum AppBarState : int
            {
                AutoHide = 1,
                AlwaysOnTop = 2,
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct APPBARDATA
            {
                public uint cbSize;
                public IntPtr hWnd;
                public uint uCallbackMessage;
                public AppBarEdge uEdge;
                public RECT rc;
                public int lParam;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
        }

        static class User32
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll")]
            public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        }

        sealed class TaskbarInfo
        {
            private const string ClassName = "Shell_TrayWnd";

            public System.Drawing.Rectangle Bounds { get; private set; }
            public Shell32.AppBarEdge Position { get; private set; }
            public System.Drawing.Point Location { get { return Bounds.Location; } }
            public System.Drawing.Size Size { get { return Bounds.Size; } }
            public bool AutoHide { get; private set; }
            public string ScreenName { get; private set; }

            public TaskbarInfo()
            {
                var taskbarHandle = User32.FindWindow(TaskbarInfo.ClassName, null);
                ScreenName = System.Windows.Forms.Screen.FromHandle(taskbarHandle).DeviceName;
                var data = new Shell32.APPBARDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(Shell32.APPBARDATA));
                data.hWnd = taskbarHandle;
                var result = Shell32.SHAppBarMessage(Shell32.AppBarMessage.GetTaskbarPos, ref data);
                if (result == IntPtr.Zero) throw new InvalidOperationException();
                Position = data.uEdge;
                Bounds = System.Drawing.Rectangle.FromLTRB(data.rc.left, data.rc.top, data.rc.right, data.rc.bottom);
                data.cbSize = (uint)Marshal.SizeOf(typeof(Shell32.APPBARDATA));
                var state = (Shell32.AppBarState)Shell32.SHAppBarMessage(Shell32.AppBarMessage.GetState, ref data).ToInt32();
                AutoHide = (state & Shell32.AppBarState.AutoHide) == Shell32.AppBarState.AutoHide;
            }
        }
    }
}
