using System.Globalization;

namespace HardwareMonitorExtension.Localization;

public static class Loc
{
    private static readonly EnUsLocalizationProvider EnUs = new();
    private static readonly ZhHansLocalizationProvider ZhHans = new();
    private static readonly JaJpLocalizationProvider JaJp = new();
    private static ILocalizationProvider _current = ResolveFromCulture(CultureInfo.CurrentUICulture);

    public static ILocalizationProvider Current => _current;

    public static void SetCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName) || cultureName.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            _current = ResolveFromCulture(CultureInfo.CurrentUICulture);
            return;
        }

        try
        {
            _current = ResolveFromCulture(new CultureInfo(cultureName));
        }
        catch
        {
            _current = EnUs;
        }
    }

    private static ILocalizationProvider ResolveFromCulture(CultureInfo culture)
    {
        var name = culture.Name.ToLowerInvariant();
        if (name.StartsWith("zh"))
            return ZhHans;
        if (name.StartsWith("ja"))
            return JaJp;
        return EnUs;
    }
}
