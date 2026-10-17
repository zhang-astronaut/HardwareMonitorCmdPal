# Localization

Managed UI uses `HardwareMonitorExtension.Localization.Text` and standard .NET `ResourceManager` satellite assemblies. `Strings.resx` is neutral English; `Strings.zh-Hant.resx`, `Strings.ja.resx` and `Strings.zh-Hans.resx` provide Traditional Chinese, Japanese and Simplified Chinese. `CultureInfo.CurrentUICulture` selects the Windows UI language. Regional cultures such as `zh-TW`, `zh-HK`, `ja-JP` and `zh-CN` inherit matching parent resources. Unsupported cultures and missing localized keys fall back through the .NET culture hierarchy to English. Unknown neutral keys return the key for diagnosis.

After changing Windows display language, restart/reload the extension to rebuild cached commands, settings and Dock labels, then open the dashboard again. There is no application language setting or new persisted preference.

## Display boundaries

Use `Text.Get("Key")` for static UI text and `Text.Format("Key", args)` for complete messages. Keep placeholders and format specifiers consistent across all four resource files. `Text.Format` uses the user's current numeric culture.

Use `Text.DisplayGroup`, `Text.DisplayLabel` and `Text.DisplayName` when rendering. They map owned sensor labels and preserve unknown hardware/vendor text. Do not translate `SensorReading.Group`, `ShortLabel`, names used in matching, sensor IDs, pin JSON, settings keys, live JSON field names or helper protocol identifiers. Sampling, filtering, default pins and CPU matching still use the original values.

MSIX package/COM identity and installer product branding retain the existing English names. `.resx` satellite resources do not supply a Windows PRI map for localized manifest metadata; this change does not introduce a separate PRI packaging pipeline. Vendor/model names and technical sensor/source identifiers keep their supplied spelling.

## Offline dashboard

`Assets/i18n-data.js` is generated from the same `.resx` files and `Text.cs` display mappings. `Assets/i18n.js` implements display translation, regional selection and English fallback. It uses local scripts without `fetch`, a server, CDN or npm dependency. HTML interpolation escapes translated and hardware-supplied text at insertion boundaries; translation functions return plain text.

Opening through the extension adds `?lang=<Windows UI culture>` to the file URL. Direct opening uses supported browser language preferences. An explicitly unsupported language falls back to English. Dashboard timestamps use the selected locale. Test with `?lang=en-US`, `?lang=zh-TW`, `?lang=ja-JP`, `?lang=zh-CN` or `?lang=fr-FR` (English fallback). Reopening from the extension refreshes the language and bundled assets.

After changing resources or managed display mappings, regenerate the committed offline catalog:

```powershell
powershell -NoProfile -File scripts/Export-DashboardLocales.ps1
```

The generator reads XML with .NET and writes UTF-8 without a BOM. Python and external packages are not required.

## Verification

Run from the repository root with .NET 10 and Node.js:

```powershell
dotnet run --project tests/LocalizationSmoke -p:Platform=x64
node tests/dashboard-localization.test.cjs
powershell -NoProfile -File scripts/Export-DashboardLocales.ps1 -Check
dotnet build HardwareMonitorExtension.sln -c Debug -p:Platform=x64
```

The managed smoke executable compiles the actual helper and satellite resources independently of Command Palette. It checks key parity, nonempty translations, placeholders, display mappings, vendor passthrough, regional inheritance and unsupported-culture fallback. The Node smoke test executes demo and live JSON through a DOM/canvas fixture in English, Traditional Chinese, Simplified Chinese, Japanese and unsupported French. It checks pause/resume, compact filtering, stable IDs, prototype-like labels and escaped HTML metadata/units. These checks do not install the extension or run its privileged helper.

Release acceptance must also inspect actual Command Palette lists, context menus, settings, Dock titles and trend pages in supported Windows UI languages. Open the offline dashboard to check clipping, fonts, actions and live updates. Automated DOM fixtures cannot prove rendered layout or privileged sensor sampling.
