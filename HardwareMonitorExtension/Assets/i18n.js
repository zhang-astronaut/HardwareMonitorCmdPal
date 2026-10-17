// Offline localization. i18n-data.js is generated from the managed .resx resources.
(() => {
  "use strict";
  const data = window.HardwareMonitorLocales;
  const supported = (tag) => {
    const name = String(tag || "").toLowerCase().replace(/_/g, "-");
    if (/^zh-(hant|tw|hk|mo)(-|$)/.test(name)) return "zh-Hant";
    if (/^zh-(hans|cn|sg)(-|$)/.test(name)) return "zh-Hans";
    if (name === "ja" || name.startsWith("ja-")) return "ja";
    if (name === "en" || name.startsWith("en-")) return "en";
    return null;
  };
  // Extension-launched dashboards inherit its Windows UI culture. Standalone files
  // use browser preferences. An explicitly unsupported language falls back to English.
  const requested = new URLSearchParams(window.location.search).get("lang");
  const language = requested !== null
    ? supported(requested) || "en"
    : (navigator.languages || [navigator.language]).map(supported).find(Boolean) || "en";
  const locale = { "zh-Hant": "zh-TW", "zh-Hans": "zh-CN", en: "en", ja: "ja" }[language];
  const t = (key, ...args) => {
    const ownString = (dictionary) => Object.hasOwn(dictionary, key) && typeof dictionary[key] === "string"
      ? dictionary[key] : undefined;
    const template = ownString(data.messages[language]) ?? ownString(data.messages.en) ?? String(key);
    return template.replace(/\{(\d+)(?::[^}]+)?\}/g, (match, index) =>
      args[index] === undefined ? match : String(args[index]));
  };
  const name = (raw) => {
    raw = String(raw ?? "");
    if (Object.hasOwn(data.displayKeys, raw)) return t(data.displayKeys[raw]);
    if (raw.startsWith("ACPI 热区 ")) return t("AcpiZone", raw.slice(8));
    if (raw.endsWith("（估算）")) return t("EstimatedName", name(raw.slice(0, -4)));
    return raw;
  };
  const label = (raw) => {
    raw = String(raw ?? "");
    if (Object.hasOwn(data.displayKeys, raw)) return t(data.displayKeys[raw]);
    for (const [suffix, key] of data.labelSuffixes)
      if (raw.endsWith(suffix)) return t(key, raw.slice(0, -suffix.length));
    return raw;
  };
  const kind = (raw) => {
    const key = "Kind" + raw.charAt(0).toUpperCase() + raw.slice(1).toLowerCase();
    return Object.hasOwn(data.messages.en, key) ? t(key) : raw;
  };
  const apply = () => {
    document.documentElement.lang = locale;
    document.title = t("AppName");
    document.querySelectorAll("[data-i18n]").forEach((element) => {
      element.textContent = t(element.dataset.i18n);
    });
  };
  window.HardwareMonitorI18n = Object.freeze({ language, locale, t, name, label, group: name, kind, apply });
})();
