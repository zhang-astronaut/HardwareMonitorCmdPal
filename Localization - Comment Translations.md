# Localization - Services Comment Translations Specification (zh-Hans, en-US, ja-JP)

**Product**: Hardware Monitor for PowerToys Command Palette  
**Company**: MyronTech Solutions  
**Author**: Nathan Myron (with assistance from Gemini via Google Antigravity)  
**Target Directory**: `HardwareMonitorExtension/Services`  

---

## Overview

This specification details all multi-lingual comment additions across the 10 service files in `HardwareMonitorExtension/Services`. In accordance with project conventions, each original Chinese (`zh-Hans`) comment is supplemented directly underneath with its English (`en-US`) and Japanese (`ja-JP`) equivalents.

---

## 1. PinnedSensorStore.cs

### Comment 1.1 (Line 5)
```csharp
/// <summary>用户固定到 Dock 的读数 Id 列表（JSON 文件持久化）。</summary>
/// <summary>List of reading IDs pinned to the Dock by the user (persisted via JSON file).</summary>
/// <summary>ユーザーが Dock に固定した測定値の Id リスト（JSON ファイルで永続化）。</summary>
```

### Comment 1.2 (Lines 9–17)
```csharp
    /// <summary>
    /// 当前默认固定方案的版本号。提高它会让仍在使用旧方案的安装
    /// 在下次启动时重新应用新默认（Dock 空间有限，默认方案需要随设计演进）。
    /// </summary>
    /// <summary>
    /// Version number of the current default pinning scheme. Incrementing this causes
    /// installations still using an older scheme to re-apply the new defaults on the
    /// next launch (dock space is limited, and default schemes need to evolve with design changes).
    /// </summary>
    /// <summary>
    /// 現在のデフォルト固定スキームのバージョン番号。これを増やすと、古いスキームを使用している環境で
    /// 次回起動時に新しいデフォルトが再適用されます（Dock のスペースは限られており、設計の進化に合わせて調整が必要です）。
    /// </summary>
```

### Comment 1.3 (Lines 33–34)
```csharp
    /// <summary>存储的固定列表是否仍来自旧版默认方案。</summary>
    /// <summary>Indicates whether the stored pinned list still originates from an older default scheme.</summary>
    /// <summary>保存されている固定リストが古いバージョンのデフォルト スキームによるものかどうか。</summary>
```

### Comment 1.4 (Lines 95–104)
```csharp
    /// <summary>
    /// 默认固定方案 v2（精简版）：只保留 Dock 上最有价值的 4 项——
    /// CPU 封装温度、CPU 占用、GPU 温度、GPU 占用。
    /// 其余读数（存储/主板/内存温度、风扇、功率估算等）在列表页全量可查，不占 Dock。
    /// </summary>
    /// <summary>
    /// Default pinning scheme v2 (streamlined version): retains only the 4 highest-value items on the Dock—
    /// CPU Package temperature, CPU load, GPU temperature, and GPU load.
    /// All other readings (storage/motherboard/memory temperatures, fans, power estimates, etc.) are fully viewable on the list page without taking up Dock space.
    /// </summary>
    /// <summary>
    /// デフォルト固定スキーム v2（簡易版）：Dock 上で最も価値の高い 4 項目のみを保持——
    /// CPU パッケージ温度、CPU 使用率、GPU 温度、GPU 使用率。
    /// その他の測定値（ストレージ/マザーボード/メモリの温度、ファン、消費電力推定など）はリスト ページで全件確認可能で、Dock を占有しません。
    /// </summary>
```

### Comment 1.5 (Lines 123–124)
```csharp
            // CPU 封装温度优先；GPU 取首条温度
            // CPU Package temperature takes priority; for GPU, take the first temperature reading
            // CPU パッケージ温度を優先。GPU は最初の温度測定値を取得
```

### Comment 1.6 (Lines 131–132)
```csharp
            // 机器缺 GPU/读数异常时兜底，保证 Dock 不空
            // Fallback when the machine lacks a GPU or readings are abnormal, ensuring the Dock is not left empty
            // GPU が存在しない、または測定値が異常な場合のフォールバック。Dock が空にならないように保証
```

### Comment 1.7 (Line 153)
```csharp
            // v2 格式：{"version":2,"ids":[...]}；v1 为纯数组
            // v2 format: {"version":2,"ids":[...]}; v1 is a plain array
            // v2 形式：{"version":2,"ids":[...]}、v1 はプレーンな配列
```

---

## 2. SensorModels.cs

### Comment 2.1 (Line 46)
```csharp
/// <summary>统一读数模型。Id 使用 LHM Identifier，保证固定项跨刷新稳定。</summary>
/// <summary>Unified reading model. Id uses LHM Identifier to ensure pinned items remain stable across refreshes.</summary>
/// <summary>統合測定値モデル。Id には LHM Identifier を使用し、更新をまたいでも固定項目の同一性を維持します。</summary>
```

### Comment 2.2 (Line 82)
```csharp
    /// <summary>列表标题：数值优先、固定宽度右对齐风格，名称在副标题。</summary>
    /// <summary>List title: value-first, fixed-width right-aligned style; name in subtitle.</summary>
    /// <summary>リスト タイトル：数値優先、固定幅右揃えスタイル、名称はサブタイトルに表示。</summary>
```

### Comment 2.3 (Line 91)
```csharp
    /// <summary>Dock 标题：只放关键数值（±占用），避免长名称截断。</summary>
    /// <summary>Dock title: only key values (± load) to prevent long names from truncating.</summary>
    /// <summary>Dock タイトル：長い名称の省略を防ぐため、主要な数値（±使用率）のみを表示。</summary>
```

### Comment 2.4 (Line 101)
```csharp
    /// <summary>Dock Compact 隐藏副标题时，加 1–2 字分组前缀。</summary>
    /// <summary>Dock Compact: adds a 1-2 character group prefix when subtitle is hidden.</summary>
    /// <summary>Dock Compact：サブタイトルを非表示にする際、1〜2 文字のグループ プレフィックスを付加。</summary>
```

### Comment 2.5 (Line 161)
```csharp
/// <summary>按传感器 Id 的历史环缓冲（内存，不落盘）。</summary>
/// <summary>Historical circular buffer by sensor Id (in-memory, no disk persistence).</summary>
/// <summary>センサー Id ごとの履歴リング バッファー（インメモリ、ディスク書き込みなし）。</summary>
```

---

## 3. SensorService.cs

### Comment 3.1 (Lines 10–14)
```csharp
/// <summary>
/// 全量枚举 LibreHardwareMonitor 传感器（CPU/GPU/存储/主板等）。
/// 说明：PawnIO 在普通权限下设备 ACL 拒绝访问（实测 0x80070005），
/// CPU 真实温度改由 SYSTEM 助手（TempHelperServer）提供，见 MergeElevatedCpuTemp。
/// </summary>
/// <summary>
/// Fully enumerates LibreHardwareMonitor sensors (CPU, GPU, storage, motherboard, etc.).
/// Note: Under normal user permissions, PawnIO device ACL denies access (measured 0x80070005);
/// authentic CPU temperature is provided by the SYSTEM helper (TempHelperServer), see MergeElevatedCpuTemp.
/// </summary>
/// <summary>
/// LibreHardwareMonitor センサー（CPU/GPU/ストレージ/マザーボードなど）を全件列挙。
/// 説明：通常権限では PawnIO デバイスの ACL によりアクセスが拒否されるため（実測 0x80070005）、
/// CPU の実際の温度は SYSTEM ヘルパー（TempHelperServer）から取得します（MergeElevatedCpuTemp 参照）。
/// </summary>
```

### Comment 3.2 (Line 97)
```csharp
                        // 驱动异常时尝试重开
                        // Attempt to re-open on driver exception
                        // ドライバー例外発生時に再オープンを試行
```

### Comment 3.3 (Line 142)
```csharp
            // 多数功率/电流传感器在第二次 Update 后才有值（MSR 采样差分）
            // Most power/current sensors only produce values after the second Update (MSR sampling differential)
            // 多くの電力/電流センサーは、2回目の Update 後に初めて値を取得可能（MSR サンプリング差分）
```

### Comment 3.4 (Line 156)
```csharp
                // 保留全部有限读数（含 Other）；温度仅丢弃明显非法值
                // Retain all finite readings (including Other); only discard blatantly invalid temperature values
                // すべての有限な測定値（Other を含む）を保持。温度は明らかに不正な値のみ破棄
```

### Comment 3.5 (Line 180)
```csharp
        // CPU 占用兜底（若 LHM 未给出）
        // Fallback CPU load (if not provided by LHM)
        // CPU 使用率のフォールバック（LHM から提供されない場合）
```

### Comment 3.6 (Line 200)
```csharp
        // CPU 功率：驱动未提供或长期为 0 时，用 load×TDP 模型估算，避免界面一直 0W
        // CPU power: when driver does not provide or remains 0, estimate using load×TDP model to avoid staying at 0W on UI
        // CPU 電力：ドライバーから取得できない場合や常に 0 の場合、UI が 0W のままになるのを防ぐため load×TDP モデルで推定
```

### Comment 3.7 (Line 202)
```csharp
        // CPU 温度：LHM/PawnIO 普通权限为 0 时，合并登录任务助手写入的真实 Tctl
        // CPU temperature: when LHM/PawnIO reads 0 under normal permissions, merge authentic Tctl written by startup task helper
        // CPU 温度：通常権限で LHM/PawnIO が 0 になる場合、スタートアップ タスク ヘルパーが書き込んだ実際の Tctl を統合
```

### Comment 3.8 (Lines 214–215)
```csharp
            // 修复点：无助手数据时，普通权限下 CPU 温度常读出 0（AMD SMN 需内核驱动），
            // 直接丢弃无效读数，避免界面显示误导性的 0°C。
            // Fix: without helper data, CPU temperature often reads 0 under normal permissions (AMD SMN requires kernel driver);
            // directly discard invalid readings to avoid misleading 0°C on the UI.
            // 修正点：ヘルパー データがない場合、通常権限では CPU 温度が 0 と読み取られることが多いため（AMD SMN にはカーネル ドライバーが必要）、
            // UI に誤解を招く 0°C が表示されないよう、無効な測定値を直接破棄。
```

### Comment 3.9 (Line 223)
```csharp
        // 替换优先级：封装/Tctl/Tdie > 第一个 CPU 温度项 > 新增
        // Replacement priority: Package/Tctl/Tdie > first CPU temperature item > add new
        // 置換優先順位：Package/Tctl/Tdie > 最初の CPU 温度項目 > 新規追加
```

### Comment 3.10 (Line 273)
```csharp
            // 用估算值替换全 0 的功率项
            // Replace all-zero power items with estimated value
            // すべて 0 の電力項目を推定値で置換
```

### Comment 3.11 (Line 499)
```csharp
    /// <summary>优先温度、负载，再按组与名称。</summary>
    /// <summary>Prioritize temperature, load, then group and name.</summary>
    /// <summary>温度、負荷を優先し、次にグループと名称順でソート。</summary>
```

---

## 4. ChartRenderer.cs

### Comment 4.1 (Lines 11–13)
```csharp
/// <summary>
/// 用 GDI+ 生成折线图 PNG。Command Palette 无 Canvas API，ContentPage 用 ImageContent 展示位图。
/// </summary>
/// <summary>
/// Generates line chart PNG via GDI+. Command Palette lacks Canvas API; ContentPage uses ImageContent to display bitmaps.
/// </summary>
/// <summary>
/// GDI+ を使用して折れ線グラフ PNG を生成。Command Palette には Canvas API がないため、ContentPage では ImageContent を使用してビットマップを表示します。
/// </summary>
```

### Comment 4.2 (Line 127)
```csharp
    /// <summary>Dock 用迷你火花线（透明底、细线）。</summary>
    /// <summary>Mini sparkline for Dock (transparent background, fine line).</summary>
    /// <summary>Dock 用ミニ スパークライン（透明背景、細線）。</summary>
```

---

## 5. DashboardHost.cs

### Comment 5.1 (Lines 9–12)
```csharp
/// <summary>
/// 将随包分发的 index.html 释放到用户可写目录并打开（file://），
/// 保证用户双击/一键打开即可，无需额外环境。
/// </summary>
/// <summary>
/// Extracts the bundled index.html into a user-writable directory and opens it (file://),
/// ensuring users can launch in one click without extra dependencies.
/// </summary>
/// <summary>
/// パッケージに同梱されている index.html をユーザー書き込み可能ディレクトリに展開して開く（file://）。
/// 追加の環境設定なしで、ダブルクリックまたはワンクリックで起動できるようにします。
/// </summary>
```

### Comment 5.2 (Line 23)
```csharp
        // 优先从包内 Assets 复制完整看板（含 css/js）
        // Prefer copying full dashboard from bundled Assets (including css/js)
        // パッケージ内の Assets から完全なダッシュボード（css/js を含む）を優先的にコピー
```

### Comment 5.3 (Line 40)
```csharp
                // 可能被占用，沿用旧文件
                // May be locked by another process; retain existing file
                // 他のプロセスで使用中の可能性があるため、既存ファイルをそのまま使用
```

### Comment 5.4 (Line 55)
```csharp
        // 打开后开启一段 live-data.js 写盘窗口；之后靠文件访问时间续期
        // Open a live-data.js write window upon launch; subsequently renewed based on file access time
        // 起動後に一定時間の live-data.js 書き込みウィンドウを開始。以降はファイル アクセス時間に基づいて更新を継続
```

---

## 6. ElevatedTempHelper.cs

### Comment 6.1 (Lines 11–14)
```csharp
/// <summary>
/// CPU Tctl 数据源：优先命名管道（SYSTEM 助手 TempHelperServer，零写盘）；
/// 失败则读 ProgramData 兜底文件。计划任务的注册/重启也在这里。
/// </summary>
/// <summary>
/// CPU Tctl data source: prefers named pipe (SYSTEM helper TempHelperServer, zero disk writes);
/// falls back to reading ProgramData fallback file. Scheduled task registration/restart is also handled here.
/// </summary>
/// <summary>
/// CPU Tctl データソース：名前付きパイプを優先（SYSTEM ヘルパー TempHelperServer、ディスク書き込みなし）。
/// 失敗時は ProgramData のフォールバック ファイルを読み取り。スケジュールされたタスクの登録/再起動もここで処理。
/// </summary>
```

### Comment 6.2 (Lines 42–43)
```csharp
        // 修复点：旧版指向设置目录下不存在的 h.exe（全库无该产物、无管道服务端）。
        // 现改为复用包内主 exe 的 -TempHelper 模式（见 TempHelperServer）。
        // Fix: legacy version pointed to non-existent h.exe in settings dir (no such artifact in repo, no pipe server).
        // Now reuses the packaged main exe in -TempHelper mode (see TempHelperServer).
        // 修正点：旧バージョンでは設定ディレクトリ内に存在しない h.exe を参照していました（リポジトリ内に該当ファイルやパイプ サーバーなし）。
        // パッケージ内のメイン exe を -TempHelper モードで再利用するように変更（TempHelperServer 参照）。
```

### Comment 6.3 (Line 86)
```csharp
    /// <summary>命名管道读 16 字节：double 温度 + long ticks；超时/失败再读文件。</summary>
    /// <summary>Reads 16 bytes from named pipe: double temperature + long ticks; falls back to file on timeout/failure.</summary>
    /// <summary>名前付きパイプから 16 バイト読み取り：double 温度 + long ticks。タイムアウト/失敗時はファイルを読み取り。</summary>
```

### Comment 6.4 (Line 115)
```csharp
                // 带时间戳时校验新鲜度（服务端每轮采样后应答）
                // Validate freshness when timestamp is present (server responds after each sampling round)
                // タイムスタンプが存在する場合は鮮度を検証（サーバーはサンプリング ラウンドごとに応答）
```

---

## 7. LiveSnapshotWriter.cs

### Comment 7.1 (Lines 11–14)
```csharp
/// <summary>
/// 将采样快照写成 live-data.js，供同目录 index.html 以 script 方式加载。
/// 按需写盘：仅当看板处于活跃状态时才覆盖写文件，避免无看板时的持续磁盘写入。
/// </summary>
/// <summary>
/// Writes sample snapshot to live-data.js for script-based loading by index.html in the same directory.
/// On-demand disk writing: only overwrites when the dashboard is active, avoiding continuous disk writes when idle.
/// </summary>
/// <summary>
/// サンプリング スナップショットを live-data.js に書き込み、同ディレクトリの index.html から script 方式で読み込めるようにします。
/// オンデマンド書き込み：ダッシュボードがアクティブな時のみ上書きし、非アクティブ時の不要なディスク書き込みを防止。
/// </summary>
```

### Comment 7.2 (Line 23)
```csharp
    /// <summary>打开看板后的保活窗口：即使访问时间不可靠也能先写几帧。</summary>
    /// <summary>Keep-alive window after opening dashboard: writes initial frames even if access time is unreliable.</summary>
    /// <summary>ダッシュボード起動後のキープアライブ ウィンドウ：アクセス時間が不確実でも最初の数フレームを確実に書き込みます。</summary>
```

### Comment 7.3 (Line 26)
```csharp
    /// <summary>live-data.js 被读取后多久内视为看板仍在用。</summary>
    /// <summary>Duration within which live-data.js is considered in active use after being read.</summary>
    /// <summary>live-data.js が読み取られた後、ダッシュボードが使用中とみなされる期間。</summary>
```

### Comment 7.4 (Line 38)
```csharp
    /// <summary>用户通过扩展命令打开看板时调用，开启一段写盘窗口。</summary>
    /// <summary>Invoked when user opens dashboard via extension command, opening a disk write window.</summary>
    /// <summary>ユーザーが拡張機能コマンドからダッシュボードを開いた際に呼び出され、書き込みウィンドウを開始します。</summary>
```

### Comment 7.5 (Lines 44–48)
```csharp
    /// <summary>
    /// 看板是否活跃：
    /// 1) 最近通过扩展命令打开过；或
    /// 2) live-data.js 在上次写入之后被读取（script 轮询会触发访问时间）。
    /// </summary>
    /// <summary>
    /// Whether dashboard is active:
    /// 1) Recently opened via extension command; or
    /// 2) live-data.js was read after last write (script polling updates access time).
    /// </summary>
    /// <summary>
    /// ダッシュボードがアクティブかどうか：
    /// 1) 最近拡張機能コマンド経由で開かれた；または
    /// 2) 前回の書き込み以降に live-data.js が読み取られた（script ポーリングによってアクセス時間が更新されます）。
    /// </summary>
```

### Comment 7.6 (Line 62)
```csharp
            // 写入本身可能刷新访问时间；要求访问明显晚于上次写入
            // Writing itself may refresh access time; require access to be noticeably later than last write
            // 書き込み自体がアクセス時間を更新する可能性があるため、前回の書き込みより明確に遅いアクセスを要求
```

### Comment 7.7 (Line 131)
```csharp
            // 看板写失败不影响采样
            // Dashboard write failures do not affect sampling
            // ダッシュボードへの書き込み失敗はサンプリングに影響しません
```

---

## 8. PawnIoLocator.cs

### Comment 8.1 (Lines 7–11)
```csharp
/// <summary>
/// 定位本机 PawnIO 安装目录（仅用于后端标签显示）。
/// 说明：PawnIO 设备 ACL 在普通权限下拒绝访问（0x80070005），
/// 传感器数据不走 PawnIO；CPU 真实温度由 SYSTEM 助手提供，见 TempHelperServer。
/// </summary>
/// <summary>
/// Locates local PawnIO installation directory (used only for backend label display).
/// Note: PawnIO device ACL denies access under normal privileges (0x80070005);
/// sensor data does not route through PawnIO; authentic CPU temp is provided by SYSTEM helper (see TempHelperServer).
/// </summary>
/// <summary>
/// ローカルの PawnIO インストール ディレクトリを特定（バックエンド ラベル表示のみに使用）。
/// 説明：通常権限では PawnIO デバイスの ACL によりアクセスが拒否されるため（0x80070005）、
/// センサー データは PawnIO を経由しません。CPU の実際の温度は SYSTEM ヘルパーから提供されます（TempHelperServer 参照）。
/// </summary>
```

---

## 9. StickyYRange.cs

### Comment 9.1 (Lines 7–12)
```csharp
/// <summary>
/// 每个传感器独立的 Y 轴包络。
/// 规则：
/// - 曲线触碰边界 → 该边界向外扩展；
/// - 曲线未触碰 → 边界保持不变（永不收缩，也不会因 padding 叠加而持续外扩）。
/// </summary>
/// <summary>
/// Independent Y-axis envelope for each sensor.
/// Rules:
/// - Curve touches boundary -> boundary expands outward;
/// - Curve does not touch -> boundary remains unchanged (never contracts, no runaway padding).
/// </summary>
/// <summary>
/// センサーごとに独立した Y 軸エンベロープ。
/// ルール：
/// - 曲線が境界に接触 → その境界を外側に拡張。
/// - 曲線が未接触 → 境界は変更なし（縮小せず、パディングの重複による不要な継続拡張もなし）。
/// </summary>
```

### Comment 9.2 (Line 56)
```csharp
        // 仅当数据真正越过当前边界时才外扩；否则完全不动
        // Only expand outward when data genuinely crosses current boundary; otherwise remain unchanged
        // データが現在の境界を実際に超えた場合のみ外側に拡張し、それ以外は一切変更しません
```

### Comment 9.3 (Lines 70–72)
```csharp
        if (!expanded)
            return old; // 曲线未触碰 → 原样返回，边界不缩也不扩
                        // Curve did not touch -> return original; boundary neither shrinks nor expands
                        // 曲線が未接触 → 元の値を返却（境界は縮小も拡大もしない）

        // 触碰后扩到新极值；只在跨度不足时补最小跨度，不叠加百分比 padding
        // Expand to new extreme upon contact; enforce minimum span only when span is insufficient, without stacking percentage padding
        // 接触後は新しい極値まで拡張。スパンが不足している場合のみ最小スパンを補正し、パーセンテージ パディングの累積は行いません
```

### Comment 9.4 (Line 83)
```csharp
    /// <summary>仅首次见到该序列时做一次视觉留白。</summary>
    /// <summary>Apply visual padding only when the sequence is first observed.</summary>
    /// <summary>シーケンスが初めて検出されたときのみ、視覚的な余白（パディング）を適用します。</summary>
```

### Comment 9.5 (Line 108)
```csharp
/// <summary>进程内共享：列表/详情页共用同一套只增不减包络。</summary>
/// <summary>In-process shared: list and detail pages share the same monotonically expanding envelope.</summary>
/// <summary>プロセス内共有：リスト ページと詳細ページで、単調増加（縮小しない）エンベロープを同一共有。</summary>
```

---

## 10. TempHelperServer.cs

### Comment 10.1 (Lines 8–14)
```csharp
/// <summary>
/// CPU 温度助手服务端（由 SYSTEM 计划任务以 -TempHelper 参数启动本 exe）。
/// SYSTEM 权限可加载 LHM 内核驱动，从而读到 AMD SMN / Intel MSR 的真实 CPU 温度；
/// 通过命名管道 HardwareMonitorTemp 供普通权限的扩展进程读取（16 字节：double 温度 + long ticks），
/// 并落盘 ProgramData 兜底文件，供管道不可用时读取。
/// 修复点：旧版计划任务指向不存在的 h.exe，且全库没有管道服务端，助手链路整体断裂。
/// </summary>
/// <summary>
/// CPU temperature helper server (started by SYSTEM scheduled task with -TempHelper argument).
/// SYSTEM privilege allows loading LHM kernel driver to read authentic CPU temperature via AMD SMN / Intel MSR;
/// exposed via named pipe HardwareMonitorTemp for unprivileged extension process (16 bytes: double temp + long ticks),
/// and persisted to ProgramData fallback file for reading when pipe is unavailable.
/// Fix: legacy scheduled task pointed to non-existent h.exe and lacked a pipe server, breaking helper integration.
/// </summary>
/// <summary>
/// CPU 温度ヘルパー サーバー（SYSTEM スケジュール タスクにより本 exe が -TempHelper 引数付きで起動）。
/// SYSTEM 権限で LHM カーネル ドライバーをロードし、AMD SMN / Intel MSR から実際の CPU 温度を読み取り可能。
/// 名前付きパイプ HardwareMonitorTemp 経由で通常権限の拡張機能プロセスに提供（16 バイト：double 温度 + long ticks）。
/// パイプが利用できない場合に備えて ProgramData のフォールバック ファイルにも書き込みます。
/// 修正点：旧バージョンのスケジュール タスクは存在しない h.exe を参照し、パイプ サーバーも存在せず、連携全体が機能していませんでした。
/// </summary>
```

### Comment 10.2 (Line 20)
```csharp
        if (!isNew) return; // 已有实例在跑
                            // Another instance is already running
                            // 既に実行中のインスタンスが存在します
```

### Comment 10.3 (Line 22)
```csharp
        // 只开 CPU，最小化 SYSTEM 常驻进程的资源占用
        // Enable only CPU to minimize resource footprint of the resident SYSTEM process
        // 常駐 SYSTEM プロセスのリソース消費を最小限に抑えるため、CPU のみ有効化
```

### Comment 10.4 (Line 63)
```csharp
            // 1) 采样（每轮一次，约等于客户端轮询频率）
            // 1) Sample (once per round, approximately matching client polling frequency)
            // 1) サンプリング（ラウンドごとに 1 回、クライアントのポーリング頻度と概ね一致）
```

### Comment 10.5 (Line 76)
```csharp
                // 采样失败保留上次值；兜底文件带时间戳，客户端会校验新鲜度
                // Retain previous value on sample failure; fallback file includes timestamp for client-side freshness check
                // サンプリング失敗時は前回の値を保持。フォールバック ファイルにはタイムスタンプが含まれ、クライアント側で鮮度を検証
```

### Comment 10.6 (Line 79)
```csharp
            // 2) 每轮新建管道实例：避免断连后的 broken 状态导致忙循环
            // 2) Create new pipe instance each round: avoids busy wait loop caused by broken state after disconnection
            // 2) ラウンドごとに新しいパイプ インスタンスを作成：切断後の破損状態でビジー ループに陥るのを防止
```

### Comment 10.7 (Line 101)
```csharp
                // 客户端中断/超时：丢弃本轮，继续采样
                // Client disconnect / timeout: discard current round and continue sampling
                // クライアント切断/タイムアウト：このラウンドを破棄してサンプリングを継続
```

### Comment 10.8 (Line 108)
```csharp
            // 3) 每轮至少 500ms，杜绝任何路径下的忙循环
            // 3) At least 500ms per round to eliminate busy wait loops on all code paths
            // 3) 各ラウンド最低 500ms を確保し、すべてのコード パスでビジー ループを防止
```

### Comment 10.9 (Line 114)
```csharp
    /// <summary>取 CPU 封装类温度（Tctl/Tdie/Package 优先），全部无效时取最高核心温度。</summary>
    /// <summary>Retrieves CPU package temperature (prefers Tctl/Tdie/Package); falls back to highest core temperature if all are invalid.</summary>
    /// <summary>CPU パッケージ系温度を取得（Tctl/Tdie/Package 優先）。すべて無効な場合は最高コア温度を取得。</summary>
```

### Comment 10.10 (Line 155)
```csharp
            // 写盘失败不影响管道
            // Disk write failure does not affect named pipe
            // ディスク書き込みの失敗は名前付きパイプに影響しません
```
