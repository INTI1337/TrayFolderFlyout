namespace TrayFolderFlyout.Models
{
    /// <summary>
    /// One place that owns every size the flyout needs: card, icon, gaps and window padding.
    /// Keeping them together is what makes the three presets look consistently denser instead of
    /// shrinking icons while leaving the surrounding air untouched.
    /// </summary>
    public readonly struct FlyoutMetrics
    {
        public double CardSize { get; init; }
        public double CardSizeIconOnly { get; init; }
        public double IconSize { get; init; }
        public double IconSizeIconOnly { get; init; }
        public double CardMargin { get; init; }
        public double WindowPadding { get; init; }
        public double StripHeight { get; init; }
        public double LabelFontSize { get; init; }
        public double LabelMaxHeight { get; init; }
        public double LabelLineHeight { get; init; }

        /// <summary>Taskbar-height thresholds (in DIP) used when the scale is set to Auto.</summary>
        public static FlyoutIconScale FromTaskbarHeight(double taskbarDip)
        {
            if (taskbarDip >= 46) return FlyoutIconScale.Large;
            if (taskbarDip >= 42) return FlyoutIconScale.Medium;
            return FlyoutIconScale.Small;
        }

        public static FlyoutMetrics For(FlyoutIconScale scale)
        {
            return scale switch
            {
                FlyoutIconScale.Small => new FlyoutMetrics
                {
                    CardSize = 48,
                    CardSizeIconOnly = 40,
                    IconSize = 20,
                    IconSizeIconOnly = 24,
                    CardMargin = 1,
                    WindowPadding = 8,
                    StripHeight = 8,
                    LabelFontSize = 9,
                    LabelMaxHeight = 22,
                    LabelLineHeight = 11
                },
                FlyoutIconScale.Medium => new FlyoutMetrics
                {
                    CardSize = 64,
                    CardSizeIconOnly = 52,
                    IconSize = 26,
                    IconSizeIconOnly = 32,
                    CardMargin = 2,
                    WindowPadding = 10,
                    StripHeight = 9,
                    LabelFontSize = 10,
                    LabelMaxHeight = 26,
                    LabelLineHeight = 12
                },
                _ => new FlyoutMetrics
                {
                    CardSize = 80,
                    CardSizeIconOnly = 64,
                    IconSize = 32,
                    IconSizeIconOnly = 44,
                    CardMargin = 2,
                    WindowPadding = 12,
                    StripHeight = 10,
                    LabelFontSize = 11,
                    LabelMaxHeight = 28,
                    LabelLineHeight = 14
                }
            };
        }
    }
}
