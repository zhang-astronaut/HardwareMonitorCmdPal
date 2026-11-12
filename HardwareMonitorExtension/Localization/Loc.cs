using System.Globalization;
using System.IO;

namespace HardwareMonitorExtension.Localization;

public record LanguageInfo(string Code, string NativeName);

public static class Loc
{
    private static readonly Dictionary<string, ILocalizationProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private static ILocalizationProvider _current = null!;
    private static readonly List<LanguageInfo> _supportedLanguages = [];

    public static ILocalizationProvider Current => _current;
    public static IReadOnlyList<LanguageInfo> SupportedLanguages => _supportedLanguages;

    static Loc()
    {
        InitializeProviders();
        _current = ResolveFromCulture(CultureInfo.CurrentUICulture);
    }

    public static void InitializeProviders()
    {
        _providers.Clear();
        _supportedLanguages.Clear();

        var baseDir = Path.Combine(AppContext.BaseDirectory, "Localization");
        if (!Directory.Exists(baseDir))
        {
            var fallbackDevDir = Path.Combine(Directory.GetCurrentDirectory(), "Localization");
            if (Directory.Exists(fallbackDevDir))
            {
                baseDir = fallbackDevDir;
            }
        }

        JsonLocalizationProvider? defaultEn = null;

        // 1. Establish en-US baseline
        var enDir = Path.Combine(baseDir, "en-US");
        var enFile = Path.Combine(enDir, "en-US.json");
        if (!File.Exists(enFile) && Directory.Exists(enDir))
        {
            enFile = Directory.GetFiles(enDir, "*.json").FirstOrDefault();
        }

        if (enFile != null && File.Exists(enFile))
        {
            try
            {
                defaultEn = new JsonLocalizationProvider(File.ReadAllText(enFile), "en-US");
                _providers["en-US"] = defaultEn;
            }
            catch { }
        }

        // 2. Scan all language subfolders
        if (Directory.Exists(baseDir))
        {
            foreach (var dir in Directory.GetDirectories(baseDir))
            {
                var folderName = Path.GetFileName(dir);
                var targetFile = Path.Combine(dir, $"{folderName}.json");
                if (!File.Exists(targetFile))
                {
                    targetFile = Directory.GetFiles(dir, "*.json").FirstOrDefault();
                }

                if (targetFile == null || !File.Exists(targetFile)) continue;

                try
                {
                    var content = File.ReadAllText(targetFile);
                    var provider = new JsonLocalizationProvider(content, folderName, defaultEn);
                    _providers[provider.LocaleCode] = provider;
                    _providers[folderName] = provider;
                }
                catch { }
            }
        }

        // 3. Register unique supported languages
        var uniqueProviders = _providers.Values.DistinctBy(p => p.LocaleCode).OrderBy(p => p.LocaleCode);
        foreach (var p in uniqueProviders)
        {
            _supportedLanguages.Add(new LanguageInfo(p.LocaleCode, p.NativeName));
        }

        if (_providers.TryGetValue("en-US", out var en))
        {
            _current = en;
        }
        else if (_providers.Count > 0)
        {
            _current = _providers.Values.First();
        }
        else
        {
            _current = new JsonLocalizationProvider("{}", "en-US");
        }
    }

    public static void SetCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName) || cultureName.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            _current = ResolveFromCulture(CultureInfo.CurrentUICulture);
            return;
        }

        if (_providers.TryGetValue(cultureName, out var match))
        {
            _current = match;
            return;
        }

        try
        {
            _current = ResolveFromCulture(new CultureInfo(cultureName));
        }
        catch
        {
            _current = _providers.TryGetValue("en-US", out var fallback) ? fallback : _providers.Values.First();
        }
    }

    private static ILocalizationProvider ResolveFromCulture(CultureInfo culture)
    {
        var name = culture.Name;
        if (_providers.TryGetValue(name, out var exact))
            return exact;

        var lang = culture.TwoLetterISOLanguageName;
        var prefixMatch = _providers.FirstOrDefault(kvp => kvp.Key.StartsWith(lang, StringComparison.OrdinalIgnoreCase));
        if (prefixMatch.Value != null)
            return prefixMatch.Value;

        return _providers.TryGetValue("en-US", out var fallback) ? fallback : _providers.Values.First();
    }
}
