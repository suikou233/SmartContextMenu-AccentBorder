using System;
using System.Runtime.InteropServices;
using SmartContextMenu.Native.Structs;

namespace SmartContextMenu.Native
{
    static class Dwmapi
    {
        [DllImport("dwmapi.dll")]
        public static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DwmBlurBehind blurBehind);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

        /// <summary>读取标量型 DWM 属性（如 DWMWA_WINDOW_CORNER_PREFERENCE）。</summary>
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }
}
