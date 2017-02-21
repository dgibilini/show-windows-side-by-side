using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

namespace SideBySide2
{
    static class Program
    {
        private static int rows;
        private static int cols;
        private static int windowWidth;
        private static int windowHeight;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [STAThread]
        static void Main(string[] args)
        {
            Process[] processes = Process.GetProcesses();
            var windows = new List<Process>();
            foreach (Process p in processes)
            {
                if (p.MainWindowHandle.Equals(new IntPtr(0)) || string.IsNullOrWhiteSpace(p.MainWindowTitle))
                    continue;
#if (DEBUG)
                if (p.MainWindowTitle.Contains("Visual Studio"))
                    continue;
#endif
                var element = AutomationElement.FromHandle(p.MainWindowHandle);
                if (element != null)
                {
                    try
                    {
                        var pattern = element.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
                        if (pattern != null)
                        {
                            var state = pattern.Current.WindowVisualState;
                            if (state.Equals(WindowVisualState.Maximized) || state.Equals(WindowVisualState.Normal))
                                windows.Add(p);
                        }
                    }
                    catch { }
                }
            }
            double num = windows.Count;
            if (num > 0)
            {
                cols = (int)Math.Ceiling(Math.Sqrt(num));
                rows = Math.Max(1, (int)Math.Ceiling(num / cols));
                int displayWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int displayHeight = Screen.PrimaryScreen.WorkingArea.Height;
                windowWidth = displayWidth / cols;
                windowHeight = displayHeight / rows;
                int curRow = 0;
                int curCol = 0;
                double toDo = num;
                foreach (var w in windows)
                {
                    MoveWindow(w.MainWindowHandle, curCol*windowWidth, curRow * windowHeight, windowWidth, windowHeight, true);
                    toDo--;
                    curRow++;
                    if (curRow >= rows)
                    {
                        curRow = 0;
                        rows = Math.Max(1, (int)Math.Ceiling(toDo / (cols - ++curCol)));
                        windowHeight = displayHeight / rows;
                    }
                }
            }
        }
    }
}
