// Hardware Monitor · 数据看板
// 优先读取扩展写出的 live-data.js（window.__HW_LIVE__）；无真实快照时用本地演示数据。

(() => {
  "use strict";

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

  const demoSensors = () => [
    mk("cpu-pkg", "处理器", "CPU 封装温度", "CPU 封装", "temperature", "°C", 48, "demo · 模拟"),
    mk("cpu-core", "处理器", "CPU 核心均温", "CPU 核心", "temperature", "°C", 46, "demo · 模拟"),
    mk("cpu-load", "处理器", "CPU 总占用", "CPU", "load", "%", 18, "demo · 模拟"),
    mk("cpu-ppt", "处理器", "CPU 封装功率", "CPU 功率", "power", "W", 28, "demo · 模拟"),
    mk("cpu-freq", "处理器", "CPU 核心频率", "CPU 频率", "clock", "MHz", 3200, "demo · 模拟"),
    mk("gpu-core", "显卡", "GPU 核心温度", "GPU 核心", "temperature", "°C", 44, "demo · 模拟"),
    mk("gpu-hot", "显卡", "GPU Hot Spot", "GPU 热点", "temperature", "°C", 52, "demo · 模拟"),
    mk("gpu-load", "显卡", "GPU 核心占用", "GPU", "load", "%", 12, "demo · 模拟"),
    mk("gpu-power", "显卡", "GPU 功耗", "GPU 功率", "power", "W", 22, "demo · 模拟"),
    mk("ssd-temp", "存储", "SSD 温度", "SSD 温度", "temperature", "°C", 37, "demo · 模拟"),
    mk("ssd-read", "存储", "SSD 读取速率", "SSD 读", "data", "MB/s", 12, "demo · 模拟"),
    mk("mb-temp", "主板", "主板温度", "主板", "temperature", "°C", 35, "demo · 模拟"),
    mk("cpu-fan", "主板", "CPU 风扇转速", "CPU 风扇", "fan", "RPM", 900, "demo · 模拟"),
    mk("mem-load", "内存", "内存占用", "内存", "load", "%", 42, "demo · 模拟"),
  ];

  const state = {
    sensors: demoSensors(),
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
    const n = s.name || s.id || "读数";
    return n.length > 10 ? n.slice(0, 10) : n;
  }

  function applyLive() {
    const live = window.__HW_LIVE__;
    if (!live || !Array.isArray(live.sensors) || live.sensors.length === 0) return false;

    const next = live.sensors.map((s) => ({
      id: s.id,
      group: s.group || "其他",
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
        $("metaLine").textContent = "真实数据已停更（请保持扩展运行）· 最后 " +
          (state.last ? state.last.toLocaleTimeString("zh-CN", { hour12: false }) : "—");
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
    $("chartStats").textContent = `${fmt(s)} · ${mn.toFixed(0)}–${mx.toFixed(0)} · ${s.history.length} 点`;
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
    return `<div class="row ${lv} ${s.id === state.selected ? "sel" : ""}" data-id="${s.id}">
      <div>
        <div class="name">${s.short}</div>
        <div class="sub">${s.group} · ${s.source}</div>
      </div>
      <div class="val">${fmt(s)}</div>
    </div>`;
  }

  function pick(id) { return state.sensors.find((s) => s.id === id); }
  function pickByGroup(group, kind, n) {
    return state.sensors.filter((s) => s.group === group && (!kind || s.kind === kind)).slice(0, n);
  }

  function render() {
    const cpuT = pick("cpu-pkg") || state.sensors.find((s) => s.kind === "temperature");
    const cpuP = pick("cpu-ppt") || state.sensors.find((s) => s.kind === "power");
    const cpuL = pick("cpu-load") || state.sensors.find((s) => s.kind === "load");
    const gpuT = pick("gpu-core") || pickByGroup("显卡", "temperature")[0];
    const diskT = pick("ssd-temp") || pickByGroup("存储", "temperature")[0];
    const fan = pick("cpu-fan") || state.sensors.find((s) => s.kind === "fan");
    const mb = pick("mb-temp") || pickByGroup("主板", "temperature")[0];
    const mem = pick("mem-load") || pickByGroup("内存", "load")[0];

    const kpis = [
      ["CPU 温", cpuT, COLORS.cpu],
      ["CPU 功率", cpuP, COLORS.power],
      ["CPU 占用", cpuL, COLORS.load],
      ["GPU 温", gpuT, COLORS.gpu],
      ["SSD 温", diskT, COLORS.disk],
      ["风扇", fan, COLORS.fan],
      ["主板", mb, COLORS.board],
      ["内存", mem, COLORS.mem],
    ];
    $("kpis").innerHTML = kpis.map(([label, s, c]) =>
      `<div class="kpi"><b style="color:${c}">${s ? fmt(s) : "—"}</b><small>${label}</small></div>`
    ).join("");

    const mode = state.live ? "真实数据 · PawnIO/LHM" : "演示数据（未检测到 live-data.js）";
    $("metaLine").textContent = (state.last ? `更新 ${state.last.toLocaleTimeString("zh-CN", { hour12: false })} · ` : "") +
      `${state.sensors.length} 项 · ${mode}${state.paused ? " · 已暂停" : ""}`;
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
      list = state.sensors.filter((s) =>
        ["temperature", "load", "power"].includes(s.kind) &&
        ["处理器", "显卡", "存储", "主板", "内存"].includes(s.group)
      ).slice(0, 16);
    }
    $("sensorList").innerHTML = list.map(rowHtml).join("");

    drawMain();

    const fillRows = (elId, items) => {
      $(elId).innerHTML = items.filter(Boolean).map((s) =>
        `<div class="r"><span>${s.short}</span><span>${fmt(s)}</span></div>`
      ).join("");
    };

    const cpuItems = pickByGroup("处理器").slice(0, 6);
    drawMini("cpuMini", cpuItems.map((s) => s.id));
    fillRows("cpuRows", cpuItems);
    $("cpuNow").textContent = cpuT ? fmt(cpuT) : "—";

    const gpuItems = pickByGroup("显卡").slice(0, 6);
    drawMini("gpuMini", gpuItems.map((s) => s.id));
    fillRows("gpuRows", gpuItems);
    $("gpuNow").textContent = gpuT ? fmt(gpuT) : "—";

    const diskItems = pickByGroup("存储").slice(0, 6);
    drawMini("diskMini", diskItems.map((s) => s.id));
    fillRows("diskRows", diskItems);
    $("diskNow").textContent = diskT ? fmt(diskT) : "—";

    const miscItems = [
      mb, fan, mem,
      ...state.sensors.filter((s) => s.group === "主板" || s.group === "内存").slice(0, 6)
    ].filter(Boolean);
    const miscUnique = [];
    const seen = new Set();
    for (const s of miscItems) { if (!seen.has(s.id)) { seen.add(s.id); miscUnique.push(s); } }
    drawMini("miscMini", miscUnique.map((s) => s.id));
    fillRows("miscRows", miscUnique.slice(0, 8));
    $("miscNow").textContent = fan ? fmt(fan) : "—";

    $("dataTable").querySelector("tbody").innerHTML = state.sensors.map((s) => {
      const pinned = state.pinned.includes(s.id);
      return `<tr data-id="${s.id}">
        <td>${s.name}</td>
        <td>${s.group}</td>
        <td>${s.kind}</td>
        <td class="mono">${fmt(s)}</td>
        <td style="color:var(--muted);font-size:11px">${s.source}</td>
        <td><button type="button" class="pin ${pinned ? "on" : ""}" data-pin="${s.id}">${pinned ? "已固定" : "固定"}</button></td>
      </tr>`;
    }).join("");
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
    $("pauseBtn").textContent = state.paused ? "恢复" : "暂停";
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
