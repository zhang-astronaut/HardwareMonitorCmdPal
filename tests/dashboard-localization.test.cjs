// No npm packages, browser profile or running extension required.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const assets = path.join(__dirname, "../HardwareMonitorExtension/Assets");
const source = (file) => fs.readFileSync(path.join(assets, file), "utf8");
const html = source("index.html");

function run(language, live = false, search = `?lang=${language}`, malicious = false) {
  const elements = new Map();
  const translations = [];
  const context2d = new Proxy({}, { get: (_, key) => key === "createLinearGradient"
    ? () => ({ addColorStop() {} }) : () => {} });
  function element() {
    return {
      textContent: "", innerHTML: "", dataset: {}, handlers: {}, clientWidth: 720,
      classList: { toggle() {} }, addEventListener(name, callback) { this.handlers[name] = callback; },
      getContext: () => context2d, querySelectorAll: () => [],
      querySelector() { return this.body ??= element(); },
    };
  }
  for (const match of html.matchAll(/<[^>]+\bid="([^"]+)"[^>]*>/g)) elements.set(match[1], element());
  for (const match of html.matchAll(/<[^>]+\bdata-i18n="([^"]+)"[^>]*>/g)) {
    const id = match[0].match(/\bid="([^"]+)"/);
    const node = id ? elements.get(id[1]) : element();
    node.dataset.i18n = match[1]; translations.push(node);
  }
  const document = {
    title: "", documentElement: { lang: "en" }, head: { appendChild() {} },
    getElementById: (id) => { assert(elements.has(id), `Unknown DOM id ${id}`); return elements.get(id); },
    querySelectorAll: () => translations, createElement: element,
  };
  const liveSensor = { id: "test:cpu", group: "处理器", name: "CPU 封装温度 (Tctl)", short: "CPU 封装",
    kind: "temperature", unit: "°C", value: 54, source: "Elevated helper · LHM", history: [50, 54] };
  const window = { location: { search }, addEventListener() {}, devicePixelRatio: 1 };
  const payload = '\"><img src=x onerror="alert(1)">&\'';
  const injected = { id: payload, group: payload, name: payload, short: payload,
    kind: "data", unit: payload, value: 12, source: payload, history: [10, 12] };
  if (live) window.__HW_LIVE__ = { timestamp: "2026-10-17T00:00:00Z", sensors: malicious ? [liveSensor, injected] : [liveSensor] };
  const context = vm.createContext({ window, document, navigator: { languages: [language] }, URLSearchParams,
    setInterval() { return 1; }, clearInterval() {}, setTimeout(callback) { callback(); }, console });
  for (const file of ["i18n-data.js", "i18n.js", "app.js"]) vm.runInContext(source(file), context, { filename: file });
  const L = window.HardwareMonitorI18n;
  assert.equal(document.title, L.t("AppName"));
  assert.equal(elements.get("stressBtn").textContent, L.t("Stress"));
  for (const node of translations) assert(!node.textContent.includes("{0}"));
  assert.equal(L.label("CPU 封装"), L.t("LabelPackage", "CPU"));
  assert.equal(L.name("ACPI 热区 TEST"), L.t("AcpiZone", "TEST"));
  assert.equal(L.name("Vendor CPU（估算）"), L.t("EstimatedName", "Vendor CPU"));
  assert.equal(L.name("Unmodified Vendor Name"), "Unmodified Vendor Name");
  for (const prototypeKey of ["constructor", "toString", "__proto__", "hasOwnProperty"]) {
    assert.equal(L.name(prototypeKey), prototypeKey);
    assert.equal(L.label(prototypeKey), prototypeKey);
    assert.equal(L.t(prototypeKey), prototypeKey);
  }
  assert.equal(L.kind("temperature"), L.t("KindTemperature"));
  elements.get("pauseBtn").handlers.click();
  assert.equal(elements.get("pauseBtn").textContent, L.t("Resume"));
  assert(elements.get("metaLine").textContent.includes(L.t("PausedSuffix")));
  elements.get("pauseBtn").handlers.click();
  assert.equal(elements.get("pauseBtn").textContent, L.t("Pause"));
  elements.get("sizeSeg").handlers.click({ target: { closest: () => ({ dataset: { size: "compact" } }) } });
  assert.equal(elements.get("dockMode").textContent, L.t("Compact"));
  assert(elements.get("sensorList").innerHTML.includes(L.t("GroupCpu")));
  assert(!elements.get("chartStats").textContent.includes("{0}"));
  const table = elements.get("dataTable").querySelector("tbody").innerHTML;
  assert(table.includes(L.t("KindTemperature")));
  if (live) {
    assert.equal(liveSensor.id, "test:cpu");
    assert.equal(liveSensor.group, "处理器");
    assert.equal(liveSensor.short, "CPU 封装");
    assert.equal(elements.get("chartTitle").textContent, L.t("CpuPackageTctl"));
    assert(table.includes('data-id="test:cpu"'));
    assert(table.includes(L.t("CpuPackageTctl")));
    if (malicious) {
      elements.get("dataTable").handlers.click({ target: { closest: (selector) =>
        selector === "[data-pin]" ? null : { dataset: { id: payload } } } });
      assert.equal(elements.get("chartTitle").textContent, payload, "Vendor text should pass through as text");
      for (const value of [table, ...[...elements.values()].map((el) => el.innerHTML)]) {
        assert(!value.includes("<img"), "Hardware metadata injected an element");
        assert(!value.includes(payload), "Hardware metadata remained unescaped in HTML");
      }
      assert(table.includes("&lt;img"), "Expected escaped vendor metadata");
      assert(table.includes("&quot;"), "Expected escaped attribute quotes");
      assert.equal(injected.id, payload, "Escaping must preserve sensor IDs in the data model");
    }
  } else {
    assert(elements.get("metaLine").textContent.includes(L.t("DemoMode")));
    assert(table.includes(L.t("CpuPackageTemperature")));
    assert(table.includes(L.t("DemoSource")));
  }
  // All rendered owned content should be translated in the neutral English view.
  if (L.language === "en") {
    for (const el of elements.values()) {
      assert(!/[\u3400-\u9fff]/.test(el.innerHTML + el.textContent), "Unexpected Chinese in English dashboard");
    }
    assert(!/[\u3400-\u9fff]/.test(table), "Unexpected Chinese in English table");
  }
  return L;
}

for (const locale of ["en-US", "zh-TW", "zh-HK", "zh-Hant", "zh-CN", "zh-SG", "ja-JP", "fr-FR"]) {
  run(locale); run(locale, true);
  console.log(`PASS ${locale}: demo/live rendering, labels, pause/resume, compact filtering, stable sensor data`);
}
assert.equal(run("fr-FR").language, "en");
assert.equal(run("zh-TW", false, "").language, "zh-Hant");
assert.equal(run("ja-JP", false, "?lang=fr-FR").language, "en");
for (const locale of ["en-US", "zh-TW", "zh-CN", "ja-JP"]) run(locale, true, `?lang=${locale}`, true);
console.log("PASS browser preference selection and explicit unsupported-language fallback");
console.log("PASS prototype-key passthrough and escaped vendor metadata/IDs/units in all HTML sinks");
