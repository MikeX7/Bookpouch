using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Libropouch
{
    class ReaderDetector
    {
        public MainWindow Parent;

        public ReaderDetector(MainWindow parent)
        {
            Parent = parent;

            Parent.SourceInitialized += OnSourceInitialized;
        }

        public void OnSourceInitialized(object sender, EventArgs e)
        {
            var windowHandle = (new WindowInteropHelper(Parent)).Handle;
            var src = HwndSource.FromHwnd(windowHandle);
            src.AddHook(WndProc);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle WM_DEVICECHANGE... 
            if (msg == 0x219)
            {
                //InitHead();
                Debug.Write(msg.ToString());
            }

            return IntPtr.Zero;
        }
    }
}
