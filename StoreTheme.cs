namespace KznStore;

public static class StoreTheme
{
    public static bool Apply(Window window)
    {
        var dark = true;
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            dark = key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch { }

        var colors = new Dictionary<string, string>
        {
            ["CanvasBrush"] = dark ? "#101216" : "#F2F2F7", ["SidebarBrush"] = dark ? "#15181D" : "#F2F2F7",
            ["SurfaceBrush"] = dark ? "#1D2026" : "#FFFFFF", ["ControlBrush"] = dark ? "#292D35" : "#FFFFFF",
            ["TextBrush"] = dark ? "#F5F5F7" : "#1D1D1F", ["MutedBrush"] = dark ? "#A3AAB7" : "#6C6C70",
            ["AccentBrush"] = dark ? "#70B5FF" : "#0062C4", ["PrimaryActionBrush"] = "#0066D6", ["OnAccentBrush"] = "#FFFFFF",
            ["StrokeBrush"] = dark ? "#414752" : "#D1D1D6", ["DividerBrush"] = dark ? "#343A44" : "#E5E5EA",
            ["HoverBrush"] = dark ? "#252B35" : "#DCDCE3", ["SelectionWashBrush"] = dark ? "#17345C" : "#E5E5EA",
            ["NavIconBrush"] = dark ? "#292F39" : "#E6E6EB", ["NavIconForegroundBrush"] = dark ? "#D1D7E0" : "#48484A",
            ["NavSelectedBrush"] = dark ? "#174A87" : "#0066D6", ["NavSelectedTextBrush"] = "#FFFFFF",
            ["BrandTileBrush"] = dark ? "#34343A" : "#252528", ["BrandMarkBrush"] = "#FFFFFF",
            ["PreviewBrush"] = dark ? "#252D3A" : "#E5EBF6", ["TrackBrush"] = dark ? "#555D6B" : "#D2D2D7"
        };
        foreach (var (name, hex) in colors)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze(); window.Resources[name] = brush;
        }
        if (SystemParameters.HighContrast)
        {
            window.Resources["CanvasBrush"] = SystemColors.WindowBrush;
            window.Resources["TextBrush"] = SystemColors.WindowTextBrush;
            window.Resources["MutedBrush"] = SystemColors.WindowTextBrush;
            window.Resources["AccentBrush"] = SystemColors.HighlightBrush;
        }
        window.Resources["GitHubButtonBrush"] = SystemParameters.HighContrast ? SystemColors.WindowBrush : dark ? Brushes.White : Brushes.Black;
        window.Resources["GitHubButtonForegroundBrush"] = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : dark ? Brushes.Black : Brushes.White;
        window.Resources["GitHubButtonHoverBrush"] = SystemParameters.HighContrast ? SystemColors.HighlightBrush : dark ? new SolidColorBrush(Color.FromRgb(226, 226, 231)) : new SolidColorBrush(Color.FromRgb(38, 40, 45));
        return dark;
    }
}
