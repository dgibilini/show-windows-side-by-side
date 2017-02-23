using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

namespace SideBySide2
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y) { this.X = x; this.Y = y; }
        public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    static class Program
    {
        public delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        [DllImport("User32.dll")]
        static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int cmd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("User32.dll")]
        static extern IntPtr WindowFromPoint(POINT p);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [STAThread]
        static void Main()
        {
            var hWndList = new List<IntPtr>();
            bool all = true;
            EnumDelegate filter = delegate (IntPtr hWnd, int lParam)
            {
                StringBuilder sb = new StringBuilder(255);
                int len = GetWindowText(hWnd, sb, sb.Capacity + 1);
                if (IsWindowVisible(hWnd) && !IsIconic(hWnd) && len > 0 && !sb.ToString().Equals("Program Manager"))
                {
                    if (all)
                    {
                        hWndList.Add(hWnd);
                        return true;
                    }

                    RECT rect;
                        GetWindowRect(hWnd, out rect);
                        if (rect.Top != 0 || rect.Left != 0 || rect.Bottom != 0 || rect.Right != 0)
                        {
                            POINT p = new POINT((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
                            ShowWindowAsync(hWnd, 9);
                            SetForegroundWindow(hWnd);
                            IntPtr hWndAux = WindowFromPoint(p);
                            while (!hWndAux.Equals(IntPtr.Zero) && !hWndAux.Equals(hWnd))
                            {
                                hWndAux = GetParent(hWndAux);
                            }
                            if (hWnd.Equals(hWndAux))
                                hWndList.Add(hWnd);
                        }
                }
                return true;
            };

            if (EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero))
            {
                Arrange(hWndList);
                hWndList.Clear();
                all = false;
                if (EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero))
                {
                    Arrange(hWndList);
                }
            }
        }

        private static void Arrange(List<IntPtr> hWndList)
        {
            double num = hWndList.Count;
            if (num > 0)
            {
                int cols = (int)Math.Ceiling(Math.Sqrt(num));
                int rows = Math.Max(1, (int)Math.Ceiling(num / cols));
                int windowWidth = Screen.PrimaryScreen.WorkingArea.Width / cols;
                int windowHeight = Screen.PrimaryScreen.WorkingArea.Height / rows;
                for (int curRow = 0, curCol = 0, i = 0; i < hWndList.Count; i++)
                {
                    MoveWindow(hWndList.ElementAt(i), curCol * windowWidth, curRow++ * windowHeight, windowWidth, windowHeight, true);
                    if (curRow >= rows)
                    {
                        curRow = 0;
                        rows = Math.Max(1, (int)Math.Ceiling((num - (i + 1)) / (cols - ++curCol)));
                        windowHeight = Screen.PrimaryScreen.WorkingArea.Height / rows;
                    }
                }
            }
        }

        //private static void ForceForegroundWindow(IntPtr hWnd)
        //{
        //    uint a;
        //    WinAPI.LockSetForegroundWindow(WinAPI.LSFW_UNLOCK);
        //    WinAPI.AllowSetForegroundWindow(WinAPI.ASFW_ANY);

        //    IntPtr hWndForeground = WinAPI.GetForegroundWindow();
        //    SendKeys.SendWait("{UP}");
        //    if (hWndForeground.ToInt32() != 0)
        //    {
        //        if (hWndForeground != hWnd)
        //        {
        //            uint thread1 = WinAPI.GetWindowThreadProcessId(hWndForeground, out a);
        //            uint thread2 = WinAPI.GetCurrentThreadId();


        //            if (thread1 != thread2)
        //            {
        //                WinAPI.AttachThreadInput(thread1, thread2, true);
        //                WinAPI.LockSetForegroundWindow(WinAPI.LSFW_UNLOCK);
        //                WinAPI.AllowSetForegroundWindow(WinAPI.ASFW_ANY);
        //                WinAPI.BringWindowToTop(hWnd);
        //                if (WinAPI.IsIconic(hWnd))
        //                {
        //                    WinAPI.ShowWindow(hWnd, WinAPI.ShowWindowFlags.SW_SHOWNORMAL);
        //                }
        //                else
        //                {
        //                    WinAPI.ShowWindow(hWnd, WinAPI.ShowWindowFlags.SW_SHOW);
        //                }
        //                WinAPI.SetFocus(hWnd);
        //                WinAPI.AttachThreadInput(thread1, thread2, false);
        //            }
        //            else
        //            {
        //                WinAPI.AttachThreadInput(thread1, thread2, true);
        //                WinAPI.LockSetForegroundWindow(WinAPI.LSFW_UNLOCK);
        //                WinAPI.AllowSetForegroundWindow(WinAPI.ASFW_ANY);
        //                WinAPI.BringWindowToTop(hWnd);
        //                WinAPI.SetForegroundWindow(hWnd);
        //                WinAPI.SetFocus(hWnd);
        //                WinAPI.AttachThreadInput(thread1, thread2, false);

        //            }
        //            if (WinAPI.IsIconic(hWnd))
        //            {
        //                WinAPI.AttachThreadInput(thread1, thread2, true);
        //                WinAPI.LockSetForegroundWindow(WinAPI.LSFW_UNLOCK);
        //                WinAPI.AllowSetForegroundWindow(WinAPI.ASFW_ANY);
        //                WinAPI.BringWindowToTop(hWnd);
        //                WinAPI.ShowWindow(hWnd, WinAPI.ShowWindowFlags.SW_SHOWNORMAL);
        //                WinAPI.SetFocus(hWnd);
        //                WinAPI.AttachThreadInput(thread1, thread2, false);
        //            }
        //            else
        //            {
        //                WinAPI.BringWindowToTop(hWnd);
        //                WinAPI.ShowWindow(hWnd, WinAPI.ShowWindowFlags.SW_SHOW);
        //            }
        //        }
        //        WinAPI.SetForegroundWindow(hWnd);
        //        WinAPI.SetFocus(hWnd);
        //    }
        //    else
        //    {
        //        uint thread1 = WinAPI.GetWindowThreadProcessId(hWndForeground, out a);
        //        uint thread2 = WinAPI.GetCurrentThreadId();
        //        try
        //        {
        //            WinAPI.AttachThreadInput(thread1, thread2, true);
        //        }
        //        catch
        //        {
        //            uint failure = 1;
        //        }
        //        WinAPI.LockSetForegroundWindow(WinAPI.LSFW_UNLOCK);
        //        WinAPI.AllowSetForegroundWindow(WinAPI.ASFW_ANY);
        //        WinAPI.BringWindowToTop(hWnd);
        //        WinAPI.SetForegroundWindow(hWnd);

        //        WinAPI.ShowWindow(hWnd, WinAPI.ShowWindowFlags.SW_SHOW);
        //        WinAPI.SetFocus(hWnd);
        //        WinAPI.AttachThreadInput(thread1, thread2, false);
        //    }
        //}
    }
}
