using System.Runtime.InteropServices;

namespace SmartContextMenu.Native.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    struct BitmapInfoHeader
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    /// <summary>
    /// CreateDIBSection 需要的 BITMAPINFO。
    /// 只用于 32bpp BI_RGB，因此不需要调色板项。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct BitmapInfo
    {
        public BitmapInfoHeader bmiHeader;
    }
}
