namespace SmartContextMenu.Native.Enums
{
    /// <summary>
    /// DWM_WINDOW_CORNER_PREFERENCE，通过
    /// DwmGetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ...) 读取。
    /// 仅 Windows 11 支持，Windows 10 上调用会返回 E_INVALIDARG。
    /// </summary>
    enum DwmWindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }
}
