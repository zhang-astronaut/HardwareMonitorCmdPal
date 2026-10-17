using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HardwareMonitorExtension.Localization;

var directory = new DirectoryInfo(AppContext.BaseDirectory);
while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "HardwareMonitorExtension/Localization")))
    directory = directory.Parent;
var resources = Path.Combine(directory?.FullName ?? "", "HardwareMonitorExtension/Localization");
if (!Directory.Exists(resources)) throw new InvalidOperationException("Cannot find source resources.");
var files = Directory.GetFiles(resources, "Strings*.resx");
var neutral = Read(Path.Combine(resources, "Strings.resx"));
foreach (var file in files)
{
    var localized = Read(file);
    Check(localized.Keys.Order().SequenceEqual(neutral.Keys.Order()), $"Resource keys differ: {file}");
    foreach (var (key, value) in localized)
    {
        Check(!string.IsNullOrWhiteSpace(value), $"Empty resource: {file} / {key}");
        Check(Placeholders(value).SequenceEqual(Placeholders(neutral[key])), $"Format placeholders differ: {file} / {key}");
        _ = string.Format(CultureInfo.InvariantCulture, value, Enumerable.Repeat<object>(42, 10).ToArray());
    }
}

foreach (var (culture, appName, label) in new[]
{
    ("en-US", "Hardware Monitor", "CPU package"),
    ("zh-TW", "硬體監視器", "CPU 封裝"),
    ("zh-HK", "硬體監視器", "CPU 封裝"),
    ("zh-Hant", "硬體監視器", "CPU 封裝"),
    ("zh-CN", "硬件监视器", "CPU 封装"),
    ("zh-SG", "硬件监视器", "CPU 封装"),
    ("zh-Hans", "硬件监视器", "CPU 封装"),
    ("ja-JP", "ハードウェアモニター", "CPU パッケージ"),
    ("fr-FR", "Hardware Monitor", "CPU package"),
})
{
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
    Check(Text.Get("AppName") == appName, $"Culture fallback failed: {culture}");
    Check(Text.DisplayLabel("CPU 封装") == label, $"Display label failed: {culture}");
    Check(Text.DisplayName("CPU 封装功率（估算）") == Text.Get("CpuPackagePowerEstimated"), $"Estimated name failed: {culture}");
    Check(Text.DisplayName("ACPI 热区 _TZ.THRM") == Text.Format("AcpiZone", "_TZ.THRM"), $"ACPI name failed: {culture}");
    Check(Text.DisplayName("Vendor Sensor X") == "Vendor Sensor X", "Vendor name changed");
    Check(Text.DisplayName("Vendor CPU（估算）") == Text.Format("EstimatedName", "Vendor CPU"), "Estimated suffix failed");
    Check(Text.DisplayGroup("处理器") == Text.Get("GroupCpu"), "Group display failed");
    Check(Text.DisplayGroup("Vendor Group") == "Vendor Group", "Unknown group changed");
    Check(Text.Get("UnknownKey") == "UnknownKey", "Missing-key fallback failed");
    Check(!Text.Format("DockSettingsCount", 3).Contains("{0}"), "Dock count format failed");
    Console.WriteLine($"PASS {culture}: {appName} / {label}");
}
Console.WriteLine($"PASS {files.Length} resource sets, {neutral.Count} keys each; formats, display mapping, region fallback and neutral fallback.");

static Dictionary<string, string> Read(string path) => XDocument.Load(path).Root!.Elements("data")
    .ToDictionary(e => e.Attribute("name")!.Value, e => e.Element("value")!.Value);
static string[] Placeholders(string value) => Regex.Matches(value, @"\{\d+(?:[^}]*)\}")
    .Select(m => m.Value).Order().ToArray();
static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
