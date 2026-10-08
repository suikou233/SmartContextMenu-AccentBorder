using System.Runtime.InteropServices;

namespace SmartContextMenu.Native.Structs
{
    /// <summary>
    /// Win32 SIZE 结构。
    /// 特意不叫 Size，是为了避免和 System.Drawing.Size 产生歧义引用。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct NativeSize
    {
        public int cx;
        public int cy;

        public NativeSize(int cx, int cy)
        {
            this.cx = cx;
            this.cy = cy;
        }
    }
}
