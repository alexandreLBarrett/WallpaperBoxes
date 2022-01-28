using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperHighlight
{
    public static class NativeMethods
    {
        public const Int32 MONITOR_DEFAULTTOPRIMERTY = 0x00000001;
        public const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;


        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, Int32 flags);


        [DllImport("user32.dll")]
        public static extern Boolean GetMonitorInfo(IntPtr hMonitor, NativeMonitorInfo lpmi);

        [DllImport("user32.dll")]
        public static extern Boolean EnumDisplayMonitors(IntPtr hdc, IntPtr clip, Monitorenumproc func, IntPtr screensArray);

        public delegate bool Monitorenumproc(IntPtr hMonitor, IntPtr hdc, ref NativeRectangle rect, IntPtr screensArray);

        [DllImport("user32.dll")]
        public static extern Boolean EnumDisplayDevices(IntPtr lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

        [DllImport("user32.dll")]
        public static extern Boolean EnumDisplayDevices(string lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;

            public DISPLAY_DEVICE(int flags)
            {
                cb = 0;
                StateFlags = flags;
                DeviceName = new string((char)32, 32);
                DeviceString = new string((char)32, 128);
                DeviceID = new string((char)32, 128);
                DeviceKey = new string((char)32, 128);
                cb = Marshal.SizeOf(this);
            }
        }

        public struct NativeRectangle
        {
            public Int32 Left;
            public Int32 Top;
            public Int32 Right;
            public Int32 Bottom;

            public NativeRectangle(Int32 left, Int32 top, Int32 right, Int32 bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class NativeMonitorInfo
        {
            public Int32 Size = Marshal.SizeOf(typeof(NativeMonitorInfo));
            public NativeRectangle Monitor;
            public NativeRectangle Work;
            public Int32 Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            public NativeMonitorInfo()
            {
                DeviceName = new string((char)32, 32);
            }
        }

        public static bool IterateScreenSizes(IntPtr hMonitor, IntPtr hdc, ref NativeRectangle rect, IntPtr screensArray)
        {
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            GCHandle handle2 = (GCHandle)screensArray;
            List<Screen> screenList = handle2.Target as List<Screen>;
            screenList.Add(new Screen {
                Width = width,
                Height = height,
                OffsetX = rect.Left,
                OffsetY = rect.Top
            });

            return true;
        }
    }

    public static class ObjectHandleExtensions
    {
        public static IntPtr ToIntPtr(this object target)
        {
            return GCHandle.Alloc(target).ToIntPtr();
        }

        public static GCHandle ToGcHandle(this object target)
        {
            return GCHandle.Alloc(target);
        }

        public static IntPtr ToIntPtr(this GCHandle target)
        {
            return GCHandle.ToIntPtr(target);
        }
    }
}
