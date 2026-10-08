using System.Runtime.InteropServices;

namespace SmartContextMenu.Native.Structs
{
    /// <summary>UpdateLayeredWindow 使用的 BLENDFUNCTION。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }
}
