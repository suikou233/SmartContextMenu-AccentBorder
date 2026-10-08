using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SmartContextMenu.Native;
using static SmartContextMenu.Native.Constants;
using BitmapInfo = SmartContextMenu.Native.Structs.BitmapInfo;
using BitmapInfoHeader = SmartContextMenu.Native.Structs.BitmapInfoHeader;
using BlendFunction = SmartContextMenu.Native.Structs.BlendFunction;
using NativePoint = SmartContextMenu.Native.Structs.Point;
using NativeSize = SmartContextMenu.Native.Structs.NativeSize;

namespace SmartContextMenu.Forms
{
    /// <summary>
    /// 置顶窗口的强调色边框，对齐 PowerToys Always On Top 的视觉效果。
    ///
    /// 实现要点（与 PowerToys 的 WindowBorder + FrameDrawer 对应）：
    /// 1. 边框是一个独立的、不激活、鼠标穿透的置顶分层窗口，不是画在目标窗口上的。
    /// 2. 环形用「外圆角矩形 + 内圆角矩形」两个子路径配合 FillMode.Alternate 填充得到，
    ///    与 PowerToys 用 D2D1_FILL_MODE_ALTERNATE 几何组完全等价。
    /// 3. 内容通过 UpdateLayeredWindow 提交 32bpp 预乘 ARGB 位图，
    ///    因此圆角边缘是真正的抗锯齿半透明，而不是色键抠图产生的硬边。
    /// 4. 几何参数逐项复刻 PowerToys FrameDrawer::ConvertRect：
    ///    外沿从 1px 内缩，内沿从 (thickness + 1)px 内缩，环宽正好等于 thickness；
    ///    外圆角半径 = radius + thickness/2，内圆角半径 = radius - thickness/2。
    /// </summary>
    public class WindowBorderForm : Form
    {
        private const int SW_SHOWNOACTIVATE = 4;

        private Bitmap _surface;
        private int _surfaceWidth;
        private int _surfaceHeight;

        private IntPtr _memoryDc = IntPtr.Zero;
        private IntPtr _dibSection = IntPtr.Zero;
        private IntPtr _dibBits = IntPtr.Zero;
        private IntPtr _previousBitmap = IntPtr.Zero;

        private string _appearanceKey;
        private bool _visible;

        public WindowBorderForm()
        {
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint, true);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Size = new System.Drawing.Size(1, 1);
            Text = string.Empty;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= (int)(WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT);
                return parameters;
            }
        }

        /// <summary>内容全部由 UpdateLayeredWindow 提供，不参与 WinForms 的绘制流程。</summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }

        /// <summary>
        /// 把边框摆到目标位置。宽度/高度是边框窗口自身的尺寸（已含边框粗细的外扩）。
        /// </summary>
        public void SetBorder(int x, int y, int width, int height, System.Drawing.Color color, int thickness, int opacityPercent, float cornerRadius)
        {
            if (width <= 0 || height <= 0 || thickness <= 0 || opacityPercent <= 0)
            {
                HideBorder();
                return;
            }

            EnsureHandle();

            var appearanceKey = string.Concat(color.ToArgb().ToString(), "|", thickness.ToString(), "|", opacityPercent.ToString(), "|", cornerRadius.ToString("F1"));

            var sizeChanged = _surface == null || _surfaceWidth != width || _surfaceHeight != height;
            var contentChanged = sizeChanged || _appearanceKey != appearanceKey;

            if (sizeChanged)
            {
                RecreateSurface(width, height);
                if (!HasSurface)
                {
                    // GDI 申请失败，放弃这一帧；绝不能带着空指针继续
                    HideBorder();
                    return;
                }
            }

            if (contentChanged)
            {
                RenderRing(color, thickness, opacityPercent, cornerRadius);
                _appearanceKey = appearanceKey;
            }

            if (!_visible)
            {
                User32.ShowWindow(Handle, SW_SHOWNOACTIVATE);
                _visible = true;
                contentChanged = true;
            }

            if (contentChanged)
            {
                // 内容变了：重新提交整张位图（UpdateLayeredWindow 同时负责位置与尺寸）
                User32.SetWindowPos(Handle, User32.HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOREDRAW);
                Present(x, y, width, height);
            }
            else
            {
                // 只是移动：挪一下窗口就行，位图内容由系统保留，不必重传
                // 拖动时 EVENT_OBJECT_LOCATIONCHANGE 触发非常频繁，这条路径是关键
                User32.SetWindowPos(Handle, User32.HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOREDRAW);
            }
        }

        public void HideBorder()
        {
            if (!_visible || Handle == IntPtr.Zero)
            {
                return;
            }

            User32.ShowWindow(Handle, SW_HIDE);
            _visible = false;
        }

        private void EnsureHandle()
        {
            if (Handle == IntPtr.Zero)
            {
                // 触发句柄创建，让 CreateParams 里的扩展样式生效
                var unused = Handle;

                // 不出现在 Aero Peek 里，与 PowerToys 一致
                var enabled = 1;
                Dwmapi.DwmSetWindowAttribute(Handle, DWMWA_EXCLUDED_FROM_PEEK, ref enabled, sizeof(int));
            }
        }

        /// <summary>
        /// 重建绘图表面。任何一步失败都必须把 <see cref="_surface"/> 置空并释放干净，
        /// 否则后续 Marshal.Copy 会往空指针写，直接 AccessViolation 打崩进程。
        /// </summary>
        private void RecreateSurface(int width, int height)
        {
            ReleaseSurface();

            _memoryDc = Gdi32.CreateCompatibleDC(IntPtr.Zero);
            if (_memoryDc == IntPtr.Zero)
            {
                return;
            }

            var info = new BitmapInfo
            {
                bmiHeader = new BitmapInfoHeader
                {
                    biSize = Marshal.SizeOf(typeof(BitmapInfoHeader)),
                    biWidth = width,
                    // 负数表示顶朝下的 DIB，行序与 GDI+ 一致，省去翻转
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = BI_RGB,
                    biSizeImage = width * height * 4
                }
            };

            _dibSection = Gdi32.CreateDIBSection(_memoryDc, ref info, DIB_RGB_COLORS, out _dibBits, IntPtr.Zero, 0);
            if (_dibSection == IntPtr.Zero || _dibBits == IntPtr.Zero)
            {
                // GDI 资源耗尽等，放弃这一帧
                ReleaseSurface();
                return;
            }

            _previousBitmap = Gdi32.SelectObject(_memoryDc, _dibSection);

            _surface = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _surfaceWidth = width;
            _surfaceHeight = height;
        }

        /// <summary>绘图表面是否可用。</summary>
        private bool HasSurface => _surface != null && _memoryDc != IntPtr.Zero && _dibBits != IntPtr.Zero;

        private void ReleaseSurface()
        {
            if (_memoryDc != IntPtr.Zero)
            {
                if (_previousBitmap != IntPtr.Zero)
                {
                    Gdi32.SelectObject(_memoryDc, _previousBitmap);
                    _previousBitmap = IntPtr.Zero;
                }

                if (_dibSection != IntPtr.Zero)
                {
                    Gdi32.DeleteObject(_dibSection);
                    _dibSection = IntPtr.Zero;
                }

                Gdi32.DeleteDC(_memoryDc);
                _memoryDc = IntPtr.Zero;
            }

            _dibBits = IntPtr.Zero;

            if (_surface != null)
            {
                _surface.Dispose();
                _surface = null;
            }

            _surfaceWidth = 0;
            _surfaceHeight = 0;
        }

        private void RenderRing(System.Drawing.Color color, int thickness, int opacityPercent, float cornerRadius)
        {
            using (var graphics = Graphics.FromImage(_surface))
            {
                graphics.Clear(System.Drawing.Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var alpha = (int)Math.Round(opacityPercent * 255.0 / 100.0);
                var half = thickness / 2f;

                using (var path = new GraphicsPath(FillMode.Alternate))
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(alpha, color.R, color.G, color.B)))
                {
                    // 外环：从 1px 内缩，半径放大半个线宽
                    var outer = new RectangleF(1f, 1f, _surfaceWidth - 2f, _surfaceHeight - 2f);
                    AddRoundedRectangle(path, outer, cornerRadius + half);

                    // 内环：从 (thickness + 1)px 内缩，半径收缩半个线宽
                    var innerWidth = _surfaceWidth - (thickness * 2f) - 2f;
                    var innerHeight = _surfaceHeight - (thickness * 2f) - 2f;
                    if (innerWidth > 0f && innerHeight > 0f)
                    {
                        var inner = new RectangleF(thickness + 1f, thickness + 1f, innerWidth, innerHeight);
                        AddRoundedRectangle(path, inner, Math.Max(cornerRadius - half, 0f));
                    }

                    graphics.FillPath(brush, path);
                }
            }

            CopySurfaceToDib();
        }

        private static void AddRoundedRectangle(GraphicsPath path, RectangleF rectangle, float radius)
        {
            if (radius <= 0f)
            {
                path.AddRectangle(rectangle);
                return;
            }

            // D2D 的圆角半径 R 对应 GDI+ 圆弧的直径 2R
            var diameter = Math.Min(radius * 2f, Math.Min(rectangle.Width, rectangle.Height));

            path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180f, 90f);
            path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270f, 90f);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
        }

        private void CopySurfaceToDib()
        {
            if (!HasSurface || _surfaceWidth <= 0 || _surfaceHeight <= 0)
            {
                return;
            }

            var width = _surfaceWidth;
            var height = _surfaceHeight;
            var stride = width * 4;

            var data = _surface.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var row = new byte[stride];
                for (var y = 0; y < height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, stride);

                    // UpdateLayeredWindow 要求预乘 alpha；GDI+ 输出的是非预乘 BGRA
                    for (var i = 0; i < stride; i += 4)
                    {
                        var a = row[i + 3];
                        if (a == 255)
                        {
                            continue;
                        }

                        if (a == 0)
                        {
                            row[i] = 0;
                            row[i + 1] = 0;
                            row[i + 2] = 0;
                            continue;
                        }

                        row[i] = (byte)(row[i] * a / 255);
                        row[i + 1] = (byte)(row[i + 1] * a / 255);
                        row[i + 2] = (byte)(row[i + 2] * a / 255);
                    }

                    Marshal.Copy(row, 0, IntPtr.Add(_dibBits, y * stride), stride);
                }
            }
            finally
            {
                _surface.UnlockBits(data);
            }
        }

        private void Present(int x, int y, int width, int height)
        {
            if (_memoryDc == IntPtr.Zero)
            {
                return;
            }

            var screenDc = User32.GetDC(IntPtr.Zero);
            try
            {
                var destination = new NativePoint(x, y);
                var size = new NativeSize(width, height);
                var source = new NativePoint(0, 0);
                var blend = new BlendFunction
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AC_SRC_ALPHA
                };

                User32.UpdateLayeredWindow(Handle, screenDc, ref destination, ref size, _memoryDc, ref source, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                User32.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseSurface();
            }

            base.Dispose(disposing);
        }
    }
}
