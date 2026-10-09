using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SmartContextMenu.Forms;
using SmartContextMenu.Native;
using SmartContextMenu.Native.Enums;
using SmartContextMenu.Native.Structs;
using SmartContextMenu.Settings;
using static SmartContextMenu.Native.Constants;

namespace SmartContextMenu.Utils
{
    /// <summary>
    /// 维护所有置顶窗口的强调色边框。
    ///
    /// 跟随策略与 PowerToys Always On Top 一致，是「事件驱动 + 定时兜底」两条腿：
    /// 1. 主路径：MainForm 注册的 EVENT_OBJECT_LOCATIONCHANGE / EVENT_SYSTEM_MOVESIZEEND
    ///    WinEvent 钩子，窗口一动就立刻调用 <see cref="UpdateNow"/>，因此拖动时边框不会拖尾。
    /// 2. 兜底：每 100ms 轮询一次，处理钩子可能漏掉的场景（比如窗口被别的程序移动）。
    ///
    /// 位置信息用 DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)，
    /// 它比 GetWindowRect 更适合画边框，因为会排除 Win10+ 那圈不可见的缩放边框。
    /// </summary>
    public class WindowBorderManager : IDisposable
    {
        private const int RefreshInterval = 100;

        private readonly Dictionary<IntPtr, WindowBorderForm> _borders = new Dictionary<IntPtr, WindowBorderForm>();

        /// <summary>
        /// 用户请求过边框的窗口。与 <see cref="_borders"/> 的区别是：
        /// 当设置里把边框关掉时，_borders 会被清空，但这里保留，
        /// 这样用户再打开边框时能立刻恢复，而不必重新置顶一次。
        /// </summary>
        private readonly HashSet<IntPtr> _requested = new HashSet<IntPtr>();
        private readonly Timer _timer;
        private WindowBorderSettings _settings;
        private bool _disposed;

        public WindowBorderManager(WindowBorderSettings settings)
        {
            _settings = settings ?? new WindowBorderSettings();

            _timer = new Timer { Interval = RefreshInterval };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        /// <summary>
        /// 设置变更后调用。关闭时销毁现有边框但保留请求记录；
        /// 重新打开时把请求过的窗口补回边框，否则用户必须重新置顶一次才看得到。
        /// </summary>
        public void ApplySettings(WindowBorderSettings settings)
        {
            _settings = settings ?? new WindowBorderSettings();

            if (!_settings.Enabled)
            {
                DestroyBorders();
                return;
            }

            foreach (var handle in _requested.ToArray())
            {
                EnsureBorder(handle);
            }
        }

        public bool Contains(IntPtr handle) => _borders.ContainsKey(handle);

        public void Add(IntPtr handle)
        {
            if (_disposed || handle == IntPtr.Zero)
            {
                return;
            }

            _requested.Add(handle);
            EnsureBorder(handle);
        }

        public void Remove(IntPtr handle)
        {
            _requested.Remove(handle);

            if (!_borders.TryGetValue(handle, out var border))
            {
                return;
            }

            _borders.Remove(handle);
            DisposeBorder(border);
        }

        /// <summary>销毁所有边框窗口，但保留请求记录（用于临时关闭边框）。</summary>
        private void DestroyBorders()
        {
            foreach (var handle in _borders.Keys.ToArray())
            {
                var border = _borders[handle];
                _borders.Remove(handle);
                DisposeBorder(border);
            }
        }

        /// <summary>彻底清空，连同请求记录一起（用于 Dispose）。</summary>
        public void RemoveAll()
        {
            _requested.Clear();
            DestroyBorders();
        }

        private void EnsureBorder(IntPtr handle)
        {
            if (_disposed || !_settings.Enabled || _borders.ContainsKey(handle))
            {
                return;
            }

            _borders.Add(handle, new WindowBorderForm());

            // 立刻摆一次位置，不用等下一次定时器
            UpdateBorder(handle);
        }

        /// <summary>
        /// 由 WinEvent 钩子调用，用于即时跟随窗口移动/缩放。
        /// 对未跟踪的窗口只是一次字典查询，可以直接返回。
        /// </summary>
        public void UpdateNow(IntPtr handle)
        {
            if (_disposed || !_borders.ContainsKey(handle))
            {
                return;
            }

            UpdateBorder(handle);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_borders.Count == 0)
            {
                return;
            }

            foreach (var handle in _borders.Keys.ToArray())
            {
                UpdateBorder(handle);
            }
        }

        private void UpdateBorder(IntPtr handle)
        {
            if (!_borders.TryGetValue(handle, out var border))
            {
                return;
            }

            // 边框只是装饰，任何异常都不该影响主程序
            try
            {
                UpdateBorderCore(handle, border);
            }
            catch
            {
                border.HideBorder();
            }
        }

        private void UpdateBorderCore(IntPtr handle, WindowBorderForm border)
        {
            // 目标窗口已销毁
            if (!User32.IsWindow(handle))
            {
                Remove(handle);
                return;
            }

            // 窗口已经不再置顶（例如被别的程序取消），边框不该继续留着
            if (!WindowUtils.IsAlwaysOnTop(handle))
            {
                Remove(handle);
                return;
            }

            // 最小化或隐藏时收起边框
            if (User32.IsIconic(handle) || !User32.IsWindowVisible(handle))
            {
                border.HideBorder();
                return;
            }

            if (!TryGetFrameBounds(handle, out var frame))
            {
                border.HideBorder();
                return;
            }

            var scaling = GetScalingFactor(handle);
            var thickness = Math.Max(1, (int)Math.Round(_settings.Thickness * scaling));
            var radius = _settings.RoundCorners ? GetCornerRadius(handle) * scaling : 0f;

            border.SetBorder(
                frame.Left - thickness,
                frame.Top - thickness,
                frame.Width + (thickness * 2),
                frame.Height + (thickness * 2),
                _settings.GetEffectiveColor(),
                thickness,
                _settings.Opacity,
                radius);
        }

        private static bool TryGetFrameBounds(IntPtr handle, out Rect frame)
        {
            frame = default;

            var rect = new Rect();
            var result = Dwmapi.DwmGetWindowAttribute(handle, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(Rect)));
            if (result != 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                return false;
            }

            frame = rect;
            return true;
        }

        /// <summary>对齐 PowerToys ScalingUtils::ScalingFactor，即 dpi / 96。</summary>
        private static float GetScalingFactor(IntPtr handle)
        {
            try
            {
                var dpi = User32.GetDpiForWindow(handle);
                if (dpi > 0)
                {
                    return dpi / 96f;
                }
            }
            catch (EntryPointNotFoundException)
            {
                // Windows 10 1607 之前没有这个导出
            }
            catch (DllNotFoundException)
            {
            }

            return 1f;
        }

        /// <summary>
        /// 对齐 PowerToys WindowCornerUtils::CornersRadius：
        /// ROUND / DEFAULT 取 8，ROUNDSMALL 取 4，其余（含 Windows 10 读取失败）取 0。
        /// </summary>
        private static float GetCornerRadius(IntPtr handle)
        {
            var preference = -1;
            var result = Dwmapi.DwmGetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, out preference, sizeof(int));
            if (result != 0)
            {
                return 0f;
            }

            switch ((DwmWindowCornerPreference)preference)
            {
                case DwmWindowCornerPreference.Round:
                case DwmWindowCornerPreference.Default:
                    return 8f;
                case DwmWindowCornerPreference.RoundSmall:
                    return 4f;
                default:
                    return 0f;
            }
        }

        private static void DisposeBorder(WindowBorderForm border)
        {
            if (border == null)
            {
                return;
            }

            try
            {
                border.HideBorder();
            }
            catch
            {
            }

            border.Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer.Dispose();

            RemoveAll();
        }
    }
}
