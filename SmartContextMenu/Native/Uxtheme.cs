using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SmartContextMenu.Native
{
    /// <summary>
    /// 通过 uxtheme.dll 的未公开导出序号读取 Windows 系统强调色。
    ///
    /// PowerToys 的 Always On Top 模块使用 WinRT 的
    /// Windows.UI.ViewManagement.UISettings.GetColorValue(UIColorType.Accent)，
    /// 但该项目目标框架是 .NET Framework 4.0，没有 WinRT 互操作支持。
    ///
    /// 实测（Windows 11）：本类的三个序号调用返回值与上述 WinRT API 逐位一致，
    /// 因此可以做到与 PowerToys 像素级相同的强调色，而无需提升目标框架。
    /// 对比数据：WinRT / 本类 = #97E6DD，注册表 DWM\AccentColor = #99E7DE（差 1-2 色阶，不可用）。
    /// </summary>
    static class Uxtheme
    {
        // 未公开导出，按序号导入。95/96/98 这组序号在 Windows 8 ~ Windows 11 上保持稳定。
        [DllImport("uxtheme.dll", EntryPoint = "#95")]
        private static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);

        [DllImport("uxtheme.dll", EntryPoint = "#96")]
        private static extern uint GetImmersiveColorTypeFromName(IntPtr pName);

        [DllImport("uxtheme.dll", EntryPoint = "#98")]
        private static extern int GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        private const string ImmersiveSystemAccent = "ImmersiveSystemAccent";

        /// <summary>强调色取不到时的兜底值，与 PowerToys 的 frame-color 默认值一致。</summary>
        public static readonly Color DefaultAccentColor = ColorTranslator.FromHtml("#0099cc");

        private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(1);
        private static Color _cachedColor = DefaultAccentColor;
        private static DateTime _cachedAt = DateTime.MinValue;

        /// <summary>
        /// 读取系统强调色（“设置 - 个性化 - 颜色 - 强调色”）。
        /// 任何一步失败都回落到 <see cref="DefaultAccentColor"/>，绝不抛异常。
        /// 边框每 100ms 刷新一次，这里做 1 秒缓存，既省调用又能及时跟上系统改色。
        /// </summary>
        public static Color GetSystemAccentColor()
        {
            var now = DateTime.UtcNow;
            if (now - _cachedAt < CacheLifetime)
            {
                return _cachedColor;
            }

            _cachedColor = ReadSystemAccentColor();
            _cachedAt = now;
            return _cachedColor;
        }

        private static Color ReadSystemAccentColor()
        {
            var namePointer = IntPtr.Zero;

            try
            {
                var colorSet = GetImmersiveUserColorSetPreference(false, false);

                namePointer = Marshal.StringToHGlobalUni(ImmersiveSystemAccent);
                var colorType = GetImmersiveColorTypeFromName(namePointer);
                if (colorType == 0 || colorType == uint.MaxValue)
                {
                    return DefaultAccentColor;
                }

                var value = GetImmersiveColorFromColorSetEx((uint)colorSet, colorType, false, 0);

                // 返回值为 0xAABBGGRR
                var red = (int)(value & 0xFF);
                var green = (int)((value >> 8) & 0xFF);
                var blue = (int)((value >> 16) & 0xFF);
                return Color.FromArgb(red, green, blue);
            }
            catch
            {
                return DefaultAccentColor;
            }
            finally
            {
                if (namePointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(namePointer);
                }
            }
        }
    }
}
