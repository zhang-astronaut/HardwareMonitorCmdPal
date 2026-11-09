// Hardware Monitor · 数据看板 / Dashboard
// 优先读取扩展写出的 live-data.js（window.__HW_LIVE__）；无真实快照时用本地演示数据。
// Extensible LOCALES registry, browser auto-detect, dynamic perspective hover switching,
// and bi-directional group normalization.

(() => {
  "use strict";

  const LOCALES = {
    "en-US": {
      code: "en-US",
      nativeName: "English (US)",
      names: {
        "en-US": "English (US)",
        "zh-CN": "英语 (美国)",
        "ja-JP": "英語 (米国)",
      },
      desc: "English (US)",
      strings: {
        metaInit: "Real-time sampling · Preparing...",
        metaLive: "Live data · PawnIO/LHM",
        metaDemo: "Demo data (no live-data.js detected)",
        metaPaused: "Paused",
        metaUpdated: "Updated",
        metaItems: "items",
        metaStale: "Live data paused (keep extension running) · Last ",
        stress: "Stress",
        pause: "Pause",
        resume: "Resume",
        dockTitle: "Command Palette · Dock",
        allSensors: "All Sensors",
        groupStorage: "Storage",
        groupMisc: "Motherboard / Fan / Memory",
        detailedMetrics: "Detailed Metrics",
        tableNote: "Click row to switch primary chart",
        thSensor: "Sensor",
        thGroup: "Group",
        thKind: "Kind",
        thCurrent: "Current",
        thSource: "Source",
        pin: "Pin",
        pinned: "Pinned",
        points: "pts",
        kpi: {
          cpuTemp: "CPU Temp",
          cpuPower: "CPU Power",
          cpuLoad: "CPU Load",
          gpuTemp: "GPU Temp",
          diskTemp: "SSD Temp",
          fan: "Fan",
          motherboard: "Motherboard",
          memory: "Memory",
        },
        groups: {
          Processor: "Processor",
          Graphics: "Graphics",
          Storage: "Storage",
          Motherboard: "Motherboard",
          Memory: "Memory",
          Battery: "Battery",
          Network: "Network",
          Other: "Other",
        },
      },
    },
    "zh-CN": {
      code: "zh-CN",
      nativeName: "简体中文",
      names: {
        "en-US": "Chinese (Simplified)",
        "zh-CN": "简体中文",
        "ja-JP": "中国語 (簡体字)",
      },
      desc: "Chinese (Simplified)",
      strings: {
        metaInit: "实时采样 · 准备中…",
        metaLive: "真实数据 · PawnIO/LHM",
        metaDemo: "演示数据（未检测到 live-data.js）",
        metaPaused: "已暂停",
        metaUpdated: "更新",
        metaItems: "项",
        metaStale: "真实数据已停更（请保持扩展运行）· 最后 ",
        stress: "压测",
        pause: "暂停",
        resume: "恢复",
        dockTitle: "命令面板 · Dock",
        allSensors: "全部读数",
        groupStorage: "存储",
        groupMisc: "主板 / 风扇 / 内存",
        detailedMetrics: "明细表",
        tableNote: "点击行可切换主图",
        thSensor: "指标",
        thGroup: "分组",
        thKind: "类型",
        thCurrent: "当前值",
        thSource: "来源",
        pin: "固定",
        pinned: "已固定",
        points: "点",
        kpi: {
          cpuTemp: "CPU 温",
          cpuPower: "CPU 功率",
          cpuLoad: "CPU 占用",
          gpuTemp: "GPU 温",
          diskTemp: "SSD 温",
          fan: "风扇",
          motherboard: "主板",
          memory: "内存",
        },
        groups: {
          Processor: "处理器",
          Graphics: "显卡",
          Storage: "存储",
          Motherboard: "主板",
          Memory: "内存",
          Battery: "电池",
          Network: "网络",
          Other: "其他",
        },
      },
    },
    "ja-JP": {
      code: "ja-JP",
      nativeName: "日本語",
      names: {
        "en-US": "Japanese",
        "zh-CN": "日语",
        "ja-JP": "日本語",
      },
      desc: "Japanese",
      strings: {
        metaInit: "リアルタイム サンプリング · 準備中…",
        metaLive: "リアルデータ · PawnIO/LHM",
        metaDemo: "デモ データ（live-data.js 未検出）",
        metaPaused: "一時停止中",
        metaUpdated: "更新",
        metaItems: "項目",
        metaStale: "リアルデータ更新停止（拡張機能を実行したままにしてください）· 最終 ",
        stress: "負荷テスト",
        pause: "一時停止",
        resume: "再開",
        dockTitle: "コマンド パレット · Dock",
        allSensors: "すべての測定値",
        groupStorage: "ストレージ",
        groupMisc: "マザーボード / ファン / メモリ",
        detailedMetrics: "詳細テーブル",
        tableNote: "行をクリックしてメイン グラフを切り替え",
        thSensor: "センサー",
        thGroup: "グループ",
        thKind: "種類",
        thCurrent: "現在値",
        thSource: "ソース",
        pin: "固定",
        pinned: "固定済み",
        points: "件",
        kpi: {
          cpuTemp: "CPU 温度",
          cpuPower: "CPU 電力",
          cpuLoad: "CPU 使用率",
          gpuTemp: "GPU 温度",
          diskTemp: "SSD 温度",
          fan: "ファン",
          motherboard: "マザーボード",
          memory: "メモリ",
        },
        groups: {
          Processor: "プロセッサ",
          Graphics: "グラフィックス",
          Storage: "ストレージ",
          Motherboard: "マザーボード",
          Memory: "メモリ",
          Battery: "バッテリー",
          Network: "ネットワーク",
          Other: "その他",
        },
      },
    },
  };

  function detectLocale() {
    const saved = localStorage.getItem("hw_locale");
    if (saved && LOCALES[saved]) return saved;
    const nav = ((navigator.languages && navigator.languages[0]) || navigator.language || "").toLowerCase();
    if (nav.startsWith("zh")) return "zh-CN";
    if (nav.startsWith("ja")) return "ja-JP";
    return "en-US";
  }

  function canonicalGroup(g) {
    const s = String(g || "").trim();
    switch (s) {
      case "プロセッサ":
      case "处理器":
      case "Processor":
      case "CPU":
        return "Processor";
      case "グラフィックス":
      case "显卡":
      case "Graphics":
      case "GPU":
        return "Graphics";
      case "ストレージ":
      case "存储":
      case "Storage":
      case "Disk":
        return "Storage";
      case "マザーボード":
      case "主板":
      case "Motherboard":
        return "Motherboard";
      case "メモリ":
      case "内存":
      case "Memory":
      case "RAM":
        return "Memory";
      case "バッテリー":
      case "电池":
      case "Battery":
        return "Battery";
      case "ネットワーク":
      case "网络":
      case "Network":
        return "Network";
      default:
        return "Other";
    }
  }

  function localizedGroupName(g, locale) {
    const key = canonicalGroup(g);
    const loc = LOCALES[locale] || LOCALES["en-US"];
    return (loc.strings.groups && loc.strings.groups[key]) || key;
  }

  const N = 90;
  const TH = { warm: 70, hot: 80, crit: 90 };
  const COLORS = {
    cpu: "#5ad1ff", load: "#7dcfff", power: "#f7768e",
    gpu: "#c792ea", hot: "#bb9af7", gload: "#9a88f5",
    disk: "#f6c177", act: "#e0af68", rw: "#ff9e64",
    fan: "#9ece6a", board: "#f7768e", mem: "#7dcfff",
    freq: "#c0caf5", volt: "#73daca",
  };

  function mk(id, group, name, short, kind, unit, value, source) {
    return { id, group, name, short, kind, unit, value, source, history: [], drift: kind === "temperature" ? 0.8 : 1.5 };
  }

  const demoSensors = (locale) => {
    const isZh = locale && locale.startsWith("zh");
    const isJa = locale && locale.startsWith("ja");
    return [
      mk("cpu-pkg", "Processor", isZh ? "CPU 封装温度" : isJa ? "CPU パッケージ温度" : "CPU Package Temperature", isZh ? "CPU 封装" : isJa ? "CPU パッケージ" : "CPU Package", "temperature", "°C", 48, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("cpu-core", "Processor", isZh ? "CPU 核心均温" : isJa ? "CPU コア平均温度" : "CPU Core Average", isZh ? "CPU 核心" : isJa ? "CPU コア" : "CPU Core", "temperature", "°C", 46, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("cpu-load", "Processor", isZh ? "CPU 总占用" : isJa ? "CPU 合計使用率" : "CPU Total Load", "CPU", "load", "%", 18, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("cpu-ppt", "Processor", isZh ? "CPU 封装功率" : isJa ? "CPU パッケージ電力" : "CPU Package Power", isZh ? "CPU 功率" : isJa ? "CPU 電力" : "CPU Power", "power", "W", 28, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("cpu-freq", "Processor", isZh ? "CPU 核心频率" : isJa ? "CPU クロック周波数" : "CPU Frequency", isZh ? "CPU 频率" : isJa ? "CPU クロック" : "CPU Clock", "clock", "MHz", 3200, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("gpu-core", "Graphics", isZh ? "GPU 核心温度" : isJa ? "GPU コア温度" : "GPU Core Temperature", isZh ? "GPU 核心" : isJa ? "GPU コア" : "GPU Core", "temperature", "°C", 44, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("gpu-hot", "Graphics", isZh ? "GPU Hot Spot" : isJa ? "GPU ホットスポット" : "GPU Hot Spot", isZh ? "GPU 热点" : isJa ? "GPU ホットスポット" : "GPU Hotspot", "temperature", "°C", 52, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("gpu-load", "Graphics", isZh ? "GPU 核心占用" : isJa ? "GPU コア使用率" : "GPU Core Load", "GPU", "load", "%", 12, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("gpu-power", "Graphics", isZh ? "GPU 功耗" : isJa ? "GPU 消費電力" : "GPU Power", isZh ? "GPU 功率" : isJa ? "GPU 電力" : "GPU Power", "power", "W", 22, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("ssd-temp", "Storage", isZh ? "SSD 温度" : isJa ? "SSD 温度" : "SSD Temperature", isZh ? "SSD 温度" : isJa ? "SSD 温度" : "SSD Temp", "temperature", "°C", 37, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("ssd-read", "Storage", isZh ? "SSD 读取速率" : isJa ? "SSD 読み取り速度" : "SSD Read Rate", isZh ? "SSD 读" : isJa ? "SSD 読取" : "SSD Read", "data", "MB/s", 12, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("mb-temp", "Motherboard", isZh ? "主板温度" : isJa ? "マザーボード温度" : "Motherboard Temperature", isZh ? "主板" : isJa ? "マザーボード" : "Motherboard", "temperature", "°C", 35, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("cpu-fan", "Motherboard", isZh ? "CPU 风扇转速" : isJa ? "CPU ファン回転数" : "CPU Fan Speed", isZh ? "CPU 风扇" : isJa ? "CPU ファン" : "CPU Fan", "fan", "RPM", 900, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
      mk("mem-load", "Memory", isZh ? "内存占用" : isJa ? "メモリ使用率" : "Memory Load", isZh ? "内存" : isJa ? "メモリ" : "Memory", "load", "%", 42, isZh ? "demo · 模拟" : isJa ? "demo · シミュレーション" : "demo · Simulated"),
    ];
  };

  const initialLocale = detectLocale();
  const state = {
    currentLocale: initialLocale,
    committedLocale: initialLocale,
    sensors: demoSensors(initialLocale),
    live: false,
    lastLiveTs: 0,
    selected: null,
    pinned: ["cpu-pkg", "cpu-load", "cpu-ppt", "gpu-core", "ssd-temp", "mem-load"],
    compact: false,
    paused: false,
    stress: false,
    ms: 1000,
    timer: null,
    last: null,
  };
  state.selected = "cpu-pkg";

  const $ = (id) => document.getElementById(id);
  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const randn = () => {
    let u = 0, v = 0;
    while (!u) u = Math.random();
    while (!v) v = Math.random();
    return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
  };

  function normalizeKind(k) {
    const s = String(k || "").toLowerCase();
    if (s.includes("temp")) return "temperature";
    if (s.includes("load") || s === "level") return "load";
    if (s.includes("power")) return "power";
    if (s.includes("fan")) return "fan";
    if (s.includes("clock")) return "clock";
    if (s.includes("data") || s.includes("throughput")) return "data";
    if (s.includes("volt")) return "voltage";
    return s || "other";
  }

  function shortOf(s) {
    if (s.short) return s.short;
    const n = s.name || s.id || "Sensor";
    return n.length > 10 ? n.slice(0, 10) : n;
  }

  function applyLive() {
    const live = window.__HW_LIVE__;
    if (!live || !Array.isArray(live.sensors) || live.sensors.length === 0) return false;

    const next = live.sensors.map((s) => ({
      id: s.id,
      group: s.group || "Other",
      name: s.name || s.id,
      short: shortOf(s),
      kind: normalizeKind(s.kind),
      unit: s.unit || "",
      value: Number(s.value) || 0,
      source: s.source || "PawnIO/LHM",
      history: Array.isArray(s.history) ? s.history.map(Number) : [],
      drift: 0,
    }));

    // 保留本地已积累的历史（若 live 历史更短则拼接）
    const prev = new Map(state.sensors.map((x) => [x.id, x]));
    for (const s of next) {
      const old = prev.get(s.id);
      if (old && old.history.length > s.history.length) {
        const tail = old.history.slice(0, old.history.length - s.history.length);
        s.history = tail.concat(s.history).slice(-N);
      }
      if (s.history.length < 2) {
        // 补齐最短绘图所需点
        while (s.history.length < 2) s.history.unshift(s.value);
      }
      if (s.history.length > N) s.history = s.history.slice(-N);
    }

    state.sensors = next;
    state.live = true;
    state.lastLiveTs = Date.now();
    state.last = live.timestamp ? new Date(live.timestamp) : new Date();
    if (!state.sensors.some((s) => s.id === state.selected))
      state.selected = state.sensors[0]?.id || null;
    if (state.pinned.length === 0)
      state.pinned = state.sensors.filter((s) => s.kind === "temperature").slice(0, 6).map((s) => s.id);
    return true;
  }

  function reloadLive() {
    if (state.paused) return;
    const tag = document.createElement("script");
    tag.src = "live-data.js?t=" + Date.now();
    tag.onload = () => {
      if (applyLive()) render();
      // 若扩展停止写入超过 8s，标记为过期但仍显示最后快照
      if (state.live && Date.now() - state.lastLiveTs > 8000) {
        const loc = LOCALES[state.currentLocale] || LOCALES["en-US"];
        const t = loc.strings;
        const timeStr = state.last ? state.last.toLocaleTimeString(state.currentLocale, { hour12: false }) : "—";
        $("metaLine").textContent = `${t.metaStale}${timeStr}`;
      }
    };
    tag.onerror = () => { /* 无真实文件，继续演示 */ };
    document.head.appendChild(tag);
  }

  function fmt(s) {
    switch (s.kind) {
      case "temperature": return `${Math.round(s.value)}°C`;
      case "load": return `${Math.round(s.value)}%`;
      case "clock": return `${Math.round(s.value)} MHz`;
      case "power": return s.value >= 100 ? `${Math.round(s.value)} W` : `${s.value.toFixed(1)} W`;
      case "fan": return `${Math.round(s.value)} RPM`;
      case "data": return `${s.value.toFixed(1)} ${s.unit || ""}`.trim();
      case "voltage": return `${s.value.toFixed(2)} V`;
      default: return `${s.value.toFixed(1)}${s.unit ? " " + s.unit : ""}`;
    }
  }

  function heat(v) {
    if (v >= TH.crit) return "crit";
    if (v >= TH.hot) return "hot";
    if (v >= TH.warm) return "warn";
    return "";
  }

  function targets() {
    const cpuLoad = state.sensors.find((s) => s.id === "cpu-load")?.value || 18;
    const stress = state.stress ? 1 : 0;
    return {
      "cpu-pkg": 42 + stress * 30 + cpuLoad * 0.25,
      "cpu-core": 40 + stress * 28 + cpuLoad * 0.22,
      "cpu-load": stress ? 78 : 16,
      "cpu-ppt": 12 + 65 * (cpuLoad / 100) + stress * 20,
      "cpu-freq": stress ? 4800 : 3200,
      "gpu-core": 40 + stress * 28,
      "gpu-hot": 48 + stress * 30,
      "gpu-load": stress ? 82 : 10,
      "gpu-power": stress ? 140 : 22,
      "ssd-temp": 36 + stress * 4,
      "ssd-read": stress ? 380 : 12,
      "mb-temp": 34 + stress * 6,
      "cpu-fan": 700 + cpuLoad * 12 + stress * 400,
      "mem-load": 42,
    };
  }

  function sampleDemo() {
    if (state.paused || state.live) return;
    const t = targets();
    for (const s of state.sensors) {
      const target = t[s.id] ?? s.value;
      const next = s.value + (target - s.value) * 0.22 + randn() * s.drift * 0.35;
      s.value = s.kind === "load" ? clamp(next, 0, 100)
        : s.kind === "temperature" ? clamp(next, 28, 105)
        : s.kind === "fan" ? clamp(next, 400, 4000)
        : s.kind === "clock" ? clamp(next, 800, 6000)
        : Math.max(0, next);
      s.history.push(s.value);
      if (s.history.length > N) s.history.shift();
    }
    state.last = new Date();
  }

  function setupCanvas(c, h) {
    const dpr = window.devicePixelRatio || 1;
    const w = c.clientWidth || 360;
    const hh = h || 110;
    c.width = Math.floor(w * dpr);
    c.height = Math.floor(hh * dpr);
    const ctx = c.getContext("2d");
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, hh);
    return { ctx, w, h: hh };
  }

  const stickyY = new Map(); // id -> {min,max} 只增不减

  function stickyRange(id, pts) {
    if (!pts || pts.length < 2) return null;
    const finite = pts.filter((v) => Number.isFinite(v));
    if (!finite.length) return null;
    let mn = Math.min(...finite);
    let mx = Math.max(...finite);
    if (mx - mn < 1e-9) { mn -= 0.5; mx += 0.5; }

    const old = stickyY.get(id);
    if (!old) {
      // 首次：做一次视觉留白
      const span = Math.max(mx - mn, 1);
      const pad = span * 0.04;
      const range = { min: mn - pad, max: mx + pad };
      stickyY.set(id, range);
      return range;
    }

    // 仅在触碰边界时外扩；未触碰则原样返回（不缩、不叠 padding）
    let lo = old.min;
    let hi = old.max;
    if (mn < lo) lo = mn;
    if (mx > hi) hi = mx;
    if (hi - lo < 1) {
      const mid = (lo + hi) / 2;
      lo = mid - 0.5;
      hi = mid + 0.5;
    }
    const range = { min: lo, max: hi };
    stickyY.set(id, range);
    return range;
  }

  function drawSeries(ctx, pts, color, w, h, pad, forced) {
    if (!pts || pts.length < 2) return;
    const data = pts.slice(-N);
    let mn, mx;
    if (forced) {
      mn = forced.min;
      mx = forced.max;
    } else {
      mn = Math.min(...data);
      mx = Math.max(...data);
      if (mx - mn < 1) mx = mn + 1;
    }
    if (mx - mn < 1e-9) mx = mn + 1;
    const xAt = (i) => pad + (i / (N - 1)) * (w - pad * 2);
    const yAt = (v) => pad + (1 - (v - mn) / (mx - mn)) * (h - pad * 2);

    ctx.beginPath();
    data.forEach((v, i) => {
      const x = xAt(i + (N - data.length));
      const y = yAt(v);
      if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    });
    const x0 = xAt(N - data.length), x1 = xAt(N - 1);
    ctx.lineTo(x1, h - pad);
    ctx.lineTo(x0, h - pad);
    ctx.closePath();
    const g = ctx.createLinearGradient(0, pad, 0, h - pad);
    g.addColorStop(0, color + "44");
    g.addColorStop(1, color + "05");
    ctx.fillStyle = g;
    ctx.fill();

    ctx.beginPath();
    data.forEach((v, i) => {
      const x = xAt(i + (N - data.length));
      const y = yAt(v);
      if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    });
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.8;
    ctx.lineJoin = "round";
    ctx.stroke();
    ctx.beginPath();
    ctx.arc(x1, yAt(data[data.length - 1]), 3, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();
  }

  function grid(ctx, w, h) {
    ctx.strokeStyle = "rgba(255,255,255,.05)";
    ctx.lineWidth = 1;
    for (let i = 0; i <= 3; i++) {
      const y = (h * i) / 3;
      ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(w, y); ctx.stroke();
    }
  }

  function drawMain() {
    const s = state.sensors.find((x) => x.id === state.selected) || state.sensors[0];
    if (!s) return;
    const g = setupCanvas($("mainChart"), 240);
    grid(g.ctx, g.w, g.h);
    const color = s.kind === "temperature" ? COLORS.cpu : s.kind === "power" ? COLORS.power : COLORS.load;
    const forced = stickyRange(s.id, s.history);
    drawSeries(g.ctx, s.history, color, g.w, g.h, 8, forced);
    $("chartTitle").textContent = s.name;
    const mn = Math.min(...s.history), mx = Math.max(...s.history);
    const loc = LOCALES[state.currentLocale] || LOCALES["en-US"];
    $("chartStats").textContent = `${fmt(s)} · ${mn.toFixed(0)}–${mx.toFixed(0)} · ${s.history.length} ${loc.strings.points}`;
    $("chartLegend").innerHTML = `<span><i style="background:${color}"></i>${s.short}</span><span>${s.source}</span>`;
  }

  function drawMini(canvasId, ids) {
    const g = setupCanvas($(canvasId), 110);
    grid(g.ctx, g.w, g.h);
    const palette = [COLORS.cpu, COLORS.load, COLORS.power];
    ids.forEach((id, i) => {
      const s = state.sensors.find((x) => x.id === id);
      if (!s) return;
      drawSeries(g.ctx, s.history, palette[i % palette.length], g.w, g.h, 4);
    });
  }

  function rowHtml(s) {
    const lv = s.kind === "temperature" ? heat(s.value) : "";
    const grp = localizedGroupName(s.group, state.currentLocale);
    return `<div class="row ${lv} ${s.id === state.selected ? "sel" : ""}" data-id="${s.id}">
      <div>
        <div class="name">${s.short}</div>
        <div class="sub">${grp} · ${s.source}</div>
      </div>
      <div class="val">${fmt(s)}</div>
    </div>`;
  }

  function pick(id) { return state.sensors.find((s) => s.id === id); }
  function pickByGroup(group, kind, n) {
    const targetCanon = canonicalGroup(group);
    return state.sensors.filter((s) => canonicalGroup(s.group) === targetCanon && (!kind || s.kind === kind)).slice(0, n);
  }

  function applyI18n(locale) {
    const loc = LOCALES[locale] || LOCALES["en-US"];
    document.querySelectorAll("[data-i18n]").forEach((el) => {
      const key = el.getAttribute("data-i18n");
      if (loc.strings[key]) {
        el.textContent = loc.strings[key];
      }
    });
    $("currentLangLabel").textContent = loc.nativeName;
    document.documentElement.lang = locale;
  }

  function render() {
    const loc = LOCALES[state.currentLocale] || LOCALES["en-US"];
    const t = loc.strings;

    const cpuT = pick("cpu-pkg") || state.sensors.find((s) => s.kind === "temperature" && canonicalGroup(s.group) === "Processor");
    const cpuP = pick("cpu-ppt") || state.sensors.find((s) => s.kind === "power" && canonicalGroup(s.group) === "Processor");
    const cpuL = pick("cpu-load") || state.sensors.find((s) => s.kind === "load" && canonicalGroup(s.group) === "Processor");
    const gpuT = pick("gpu-core") || pickByGroup("Graphics", "temperature")[0];
    const diskT = pick("ssd-temp") || pickByGroup("Storage", "temperature")[0];
    const fan = pick("cpu-fan") || state.sensors.find((s) => s.kind === "fan");
    const mb = pick("mb-temp") || pickByGroup("Motherboard", "temperature")[0];
    const mem = pick("mem-load") || pickByGroup("Memory", "load")[0];

    const kpis = [
      [t.kpi.cpuTemp, cpuT, COLORS.cpu],
      [t.kpi.cpuPower, cpuP, COLORS.power],
      [t.kpi.cpuLoad, cpuL, COLORS.load],
      [t.kpi.gpuTemp, gpuT, COLORS.gpu],
      [t.kpi.diskTemp, diskT, COLORS.disk],
      [t.kpi.fan, fan, COLORS.fan],
      [t.kpi.motherboard, mb, COLORS.board],
      [t.kpi.memory, mem, COLORS.mem],
    ];
    $("kpis").innerHTML = kpis.map(([label, s, c]) =>
      `<div class="kpi"><b style="color:${c}">${s ? fmt(s) : "—"}</b><small>${label}</small></div>`
    ).join("");

    const mode = state.live ? t.metaLive : t.metaDemo;
    const lastTime = state.last ? `${t.metaUpdated} ${state.last.toLocaleTimeString(state.currentLocale, { hour12: false })} · ` : "";
    $("metaLine").textContent = `${lastTime}${state.sensors.length} ${t.metaItems} · ${mode}${state.paused ? " · " + t.metaPaused : ""}`;
    $("listCount").textContent = String(state.sensors.length);
    $("dockMode").textContent = state.compact ? "Compact" : "Default";

    const pinIds = state.pinned.filter((id) => state.sensors.some((s) => s.id === id));
    if (pinIds.length === 0) pinIds.push(...state.sensors.filter((s) => s.kind === "temperature").slice(0, 6).map((s) => s.id));
    state.pinned = pinIds;

    $("dock").innerHTML = state.pinned
      .map((id) => pick(id))
      .filter(Boolean)
      .map((s) => {
        const lv = s.kind === "temperature" ? heat(s.value) : "";
        return `<div class="band ${lv} ${s.id === state.selected ? "sel" : ""}" data-id="${s.id}">
          <div class="n">${s.short}</div>
          <div class="v">${fmt(s)}</div>
        </div>`;
      }).join("");

    let list = state.sensors;
    if (state.compact) {
      const allowedGroups = new Set(["Processor", "Graphics", "Storage", "Motherboard", "Memory"]);
      list = state.sensors.filter((s) =>
        ["temperature", "load", "power"].includes(s.kind) &&
        allowedGroups.has(canonicalGroup(s.group))
      ).slice(0, 16);
    }
    $("sensorList").innerHTML = list.map(rowHtml).join("");

    drawMain();

    const fillRows = (elId, items) => {
      $(elId).innerHTML = items.filter(Boolean).map((s) =>
        `<div class="r"><span>${s.short}</span><span>${fmt(s)}</span></div>`
      ).join("");
    };

    const cpuItems = pickByGroup("Processor").slice(0, 6);
    drawMini("cpuMini", cpuItems.map((s) => s.id));
    fillRows("cpuRows", cpuItems);
    $("cpuNow").textContent = cpuT ? fmt(cpuT) : "—";

    const gpuItems = pickByGroup("Graphics").slice(0, 6);
    drawMini("gpuMini", gpuItems.map((s) => s.id));
    fillRows("gpuRows", gpuItems);
    $("gpuNow").textContent = gpuT ? fmt(gpuT) : "—";

    const diskItems = pickByGroup("Storage").slice(0, 6);
    drawMini("diskMini", diskItems.map((s) => s.id));
    fillRows("diskRows", diskItems);
    $("diskNow").textContent = diskT ? fmt(diskT) : "—";

    const miscItems = [
      mb, fan, mem,
      ...state.sensors.filter((s) => {
        const cg = canonicalGroup(s.group);
        return cg === "Motherboard" || cg === "Memory";
      }).slice(0, 6)
    ].filter(Boolean);
    const miscUnique = [];
    const seen = new Set();
    for (const s of miscItems) { if (!seen.has(s.id)) { seen.add(s.id); miscUnique.push(s); } }
    drawMini("miscMini", miscUnique.map((s) => s.id));
    fillRows("miscRows", miscUnique.slice(0, 8));
    $("miscNow").textContent = fan ? fmt(fan) : "—";

    $("dataTable").querySelector("tbody").innerHTML = state.sensors.map((s) => {
      const pinned = state.pinned.includes(s.id);
      const grp = localizedGroupName(s.group, state.currentLocale);
      return `<tr data-id="${s.id}">
        <td>${s.name}</td>
        <td>${grp}</td>
        <td>${s.kind}</td>
        <td class="mono">${fmt(s)}</td>
        <td style="color:var(--muted);font-size:11px">${s.source}</td>
        <td><button type="button" class="pin ${pinned ? "on" : ""}" data-pin="${s.id}">${pinned ? t.pinned : t.pin}</button></td>
      </tr>`;
    }).join("");
  }

  function setLocalePerspective(locale) {
    if (!LOCALES[locale]) return;
    state.currentLocale = locale;
    applyI18n(locale);
    const menu = $("langMenu");
    if (menu) {
      menu.querySelectorAll(".dropdown-item").forEach((el) => {
        el.classList.toggle("active", el.dataset.locale === state.committedLocale);
        const code = el.dataset.locale;
        const descEl = el.querySelector(".opt-desc");
        if (descEl && LOCALES[code] && LOCALES[code].names) {
          descEl.textContent = LOCALES[code].names[locale] || LOCALES[code].desc;
        }
      });
    }
    if (!state.live) {
      const demo = demoSensors(locale);
      const histMap = new Map(state.sensors.map((s) => [s.id, s.history]));
      for (const d of demo) {
        if (histMap.has(d.id)) {
          d.history = histMap.get(d.id);
        }
      }
      state.sensors = demo;
    }
    render();
  }

  function closeDropdown() {
    $("langDropdown").classList.remove("open");
    $("langBtn").setAttribute("aria-expanded", "false");
    setLocalePerspective(state.committedLocale);
  }

  function initLangDropdown() {
    const menu = $("langMenu");
    menu.innerHTML = Object.keys(LOCALES).map((code) => {
      const loc = LOCALES[code];
      const active = code === state.committedLocale;
      const initialDesc = (loc.names && loc.names[state.committedLocale]) || loc.desc;
      return `<div class="dropdown-item ${active ? "active" : ""}" data-locale="${code}">
        <span class="opt-native">${loc.nativeName}</span>
        <span class="opt-desc">${initialDesc}</span>
      </div>`;
    }).join("");

    $("langBtn").addEventListener("click", (e) => {
      e.stopPropagation();
      const dd = $("langDropdown");
      const isOpen = dd.classList.toggle("open");
      $("langBtn").setAttribute("aria-expanded", String(isOpen));
    });

    document.addEventListener("click", (e) => {
      if (!$("langDropdown").contains(e.target)) {
        closeDropdown();
      }
    });

    menu.addEventListener("mouseover", (e) => {
      const item = e.target.closest(".dropdown-item[data-locale]");
      if (item) {
        setLocalePerspective(item.dataset.locale);
      }
    });

    menu.addEventListener("mouseleave", () => {
      setLocalePerspective(state.committedLocale);
    });

    menu.addEventListener("click", (e) => {
      const item = e.target.closest(".dropdown-item[data-locale]");
      if (item) {
        const locCode = item.dataset.locale;
        state.committedLocale = locCode;
        localStorage.setItem("hw_locale", locCode);
        setLocalePerspective(locCode);
        closeDropdown();
      }
    });
  }

  function tick() {
    if (state.live) reloadLive();
    else sampleDemo();
    render();
  }

  function restart() {
    if (state.timer) clearInterval(state.timer);
    state.timer = setInterval(tick, state.live ? 1500 : state.ms);
  }

  $("sizeSeg").addEventListener("click", (e) => {
    const b = e.target.closest("button[data-size]");
    if (!b) return;
    state.compact = b.dataset.size === "compact";
    [...$("sizeSeg").querySelectorAll("button")].forEach((x) => x.classList.toggle("active", x === b));
    render();
  });
  $("stressBtn").addEventListener("click", () => {
    state.stress = !state.stress;
    $("stressBtn").classList.toggle("on", state.stress);
  });
  $("pauseBtn").addEventListener("click", () => {
    state.paused = !state.paused;
    $("pauseBtn").classList.toggle("on", state.paused);
    const loc = LOCALES[state.currentLocale] || LOCALES["en-US"];
    $("pauseBtn").textContent = state.paused ? loc.strings.resume : loc.strings.pause;
    render();
  });
  $("sensorList").addEventListener("click", (e) => {
    const row = e.target.closest(".row[data-id]");
    if (!row) return;
    state.selected = row.dataset.id;
    render();
  });
  $("dock").addEventListener("click", (e) => {
    const band = e.target.closest(".band[data-id]");
    if (!band) return;
    state.selected = band.dataset.id;
    render();
  });
  $("dataTable").addEventListener("click", (e) => {
    const pin = e.target.closest("[data-pin]");
    if (pin) {
      const id = pin.dataset.pin;
      state.pinned = state.pinned.includes(id)
        ? state.pinned.filter((x) => x !== id)
        : [...state.pinned, id];
      render();
      return;
    }
    const tr = e.target.closest("tr[data-id]");
    if (tr) { state.selected = tr.dataset.id; render(); }
  });
  window.addEventListener("resize", () => render());

  initLangDropdown();
  setLocalePerspective(state.committedLocale);

  // 启动：先试 live，再演示 seed
  if (applyLive()) {
    restart();
  } else {
    for (let i = 0; i < 45; i++) sampleDemo();
    render();
    restart();
  }
  // 额外再拉一次 live（script 已同步加载时 applyLive 已成功）
  setTimeout(reloadLive, 200);
})();
