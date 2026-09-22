---
feature: all-sensors-visible
status: delivered
updated: 2026-09-13
branch: fix/all-sensors-visible
commits: 60590e2..HEAD
---

# 全量可读指标可见（含 CPU 温度）

## Report

**What was built** — 采集层不再丢弃 `SensorKind.Other` 与 `0°C` 温度，仅过滤 NaN/非法范围（-50…200）。列表 Default/Compact 均展示快照中的全部指标，处理器温度优先展示，避免「没有 CPU 温度」。ACPI 兜底与峰值统计与采样使用同一温度有效范围。

**Verification** — `dotnet build -c Debug -p:Platform=x64 -p:GenerateAppxPackageOnBuild=false -p:EnableMsixTooling=false` → **PASS**（0 error；3 条 AsciiChart 既有 warning）。审阅子代理：Spec T1–T3 通过；无 critical。

**Journey log** — 1) 温度 `<=0` 过滤会误杀首帧/低端读数；2) Compact 的 catch-all 与「全量显示」需求一致，已写入设计而非当 bug；3) 温度有效范围曾散落三处，已统一为 `[-50,200]`。

## [S1] Problem

用户看不到 CPU 温度，且要求 PawnIO/LHM **全部可读项目**都出现。旧逻辑丢弃 `Other`、丢弃温度 `<=0`/`>=150`，Compact 还会截断。

## [S2] Design

1. 每个有限数值 LHM 传感器均产出读数；未知类型 → `SensorKind.Other`（保留）。
2. 温度仅丢弃非有限值与 `<-50` / `>200`。
3. Default/Compact **全量显示**；Compact 仅将处理器温度排前。
4. 其余 UI/阈值/Dock 语义不变。

## [S3] Out of Scope

- 不改 asciichart / live-data 写盘 / 版本发布
- 不新增驱动或管理员权限

## Tasks

- [x] T1: 放开 SensorService 对 Other 与低温值的丢弃 — acceptance: 有限值均进入 Snapshot (covers: S2)
- [x] T2: FilterForLayout 全量显示、保留 CPU 温度 — acceptance: 列表项数 == Snapshot (covers: S2; depends: T1)
- [x] T3: 构建验证 — acceptance: 编译 0 error (covers: S2; depends: T2)
