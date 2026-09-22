---
feature: all-sensors-visible
status: designed
updated: 2026-09-13
branch: fix/all-sensors-visible
commits: 
---

# 全量可读指标可见（含 CPU 温度）

## Report

## [S1] Problem

用户在扩展里看不到 **CPU 温度**，且未展示 PawnIO/LHM 能读到的全部指标。当前 `SensorService` 会丢弃 `SensorKind.Other`，并丢弃温度 `<=0` 或 `>=150` 的读数；`SensorsListPage` 的 Compact 布局还会 `Take(2)` 截断列表。

## [S2] Design

1. **采集**：`SampleFromLibreHardwareMonitor` 对每个有限数值传感器都产出读数；未知 `SensorType` 映射为 `SensorKind.Other` 且**不再跳过**。
2. **温度过滤**：仅丢弃 NaN/Infinity 与明显非法温度（`< -50` 或 `> 200`）；允许 `0` 及真实范围内的值（避免首帧 0°C 被滤掉导致「没有 CPU 温度」）。
3. **展示**：Default 布局列出**全部** `SensorReading`；Compact 仍按分组瘦身但**不丢 CPU 温度**，且每组温度至少保留 3 条（或全部不足 3 条）。
4. **单元/标签**：`SensorKind.Other` 显示数值与可选单位；分组仍按 `MapGroup`。
5. 列表 Title 仍为短数值，SubTitle 为名称，不改 Dock 合并占用与阈值语义。

Out of scope: 直接加载 PawnIO 模块 blob 的 ioctl 协议；改 WinGet/画廊。

## [S3] Out of Scope

- 不修改 asciichart / StickyYRange / live-data.js 按需写盘
- 不改变版本号与发布渠道
- 不新增管理员权限或驱动安装

## Tasks

- [ ] T1: 放开 SensorService 对 Other 与低温值的丢弃 — acceptance: 有限值传感器均进入 Snapshot，含 0°C 与 Other 类型 (covers: S2)
- [ ] T2: 调整 FilterForLayout，Default 全量、Compact 不丢 CPU 温度 — acceptance: Default 列表项数 == Snapshot 项数 (covers: S2; depends: T1)
- [ ] T3: 构建验证 — acceptance: Release/Debug 编译 0 error (covers: S2; depends: T2)
