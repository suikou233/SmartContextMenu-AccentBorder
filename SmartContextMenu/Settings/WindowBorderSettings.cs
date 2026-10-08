using System;
using System.Drawing;
using SmartContextMenu.Native;

namespace SmartContextMenu.Settings
{
    /// <summary>
    /// 置顶窗口强调色边框的设置。默认值与 PowerToys 的 Always On Top 模块一一对应。
    /// </summary>
    public class WindowBorderSettings : ICloneable
    {
        // PowerToys: AlwaysOnTopProperties.cs
        public const bool DefaultEnabled = true;
        public const bool DefaultUseAccentColor = true;
        public const string DefaultColor = "#0099cc";
        public const int DefaultThickness = 4;
        public const int DefaultOpacity = 100;
        public const bool DefaultRoundCorners = true;

        public const int MinThickness = 1;
        public const int MaxThickness = 20;
        public const int MinOpacity = 0;
        public const int MaxOpacity = 100;

        /// <summary>是否在窗口置顶时显示边框。</summary>
        public bool Enabled { get; set; }

        /// <summary>true 跟随系统强调色；false 使用 <see cref="Color"/>。</summary>
        public bool UseAccentColor { get; set; }

        /// <summary>自定义边框颜色，"#RRGGBB" 形式。</summary>
        public string Color { get; set; }

        /// <summary>边框粗细（96 DPI 下的像素值，实际渲染按窗口 DPI 缩放）。</summary>
        public int Thickness { get; set; }

        /// <summary>边框不透明度，0-100。</summary>
        public int Opacity { get; set; }

        /// <summary>圆角是否跟随目标窗口的圆角设置。</summary>
        public bool RoundCorners { get; set; }

        public WindowBorderSettings()
        {
            Enabled = DefaultEnabled;
            UseAccentColor = DefaultUseAccentColor;
            Color = DefaultColor;
            Thickness = DefaultThickness;
            Opacity = DefaultOpacity;
            RoundCorners = DefaultRoundCorners;
        }

        public object Clone() => MemberwiseClone();

        /// <summary>把 <see cref="Color"/> 解析成 Color，非法值回落到默认色。</summary>
        public System.Drawing.Color ResolveColor()
        {
            try
            {
                return ColorTranslator.FromHtml(Color);
            }
            catch
            {
                return ColorTranslator.FromHtml(DefaultColor);
            }
        }

        /// <summary>当前实际生效的边框颜色：强调色开关打开时读系统强调色。</summary>
        public System.Drawing.Color GetEffectiveColor() =>
            UseAccentColor ? Uxtheme.GetSystemAccentColor() : ResolveColor();

        /// <summary>把厚度/不透明度夹到合法区间。</summary>
        public void Normalize()
        {
            Thickness = Math.Min(Math.Max(Thickness, MinThickness), MaxThickness);
            Opacity = Math.Min(Math.Max(Opacity, MinOpacity), MaxOpacity);
            if (string.IsNullOrWhiteSpace(Color))
            {
                Color = DefaultColor;
            }
        }
    }
}
