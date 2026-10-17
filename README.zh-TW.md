# HardwareMonitorCmdPal

[English](README.md) · 繁體中文 · [日本語](README.ja.md) · [简体中文](README.zh-CN.md)

PowerToys Command Palette 擴充功能，透過 LibreHardwareMonitor／PawnIO 讀取 CPU、GPU、儲存裝置、主機板等硬體感測器。

- 瀏覽可用的感測器讀值，並將感測器釘選至 Command Palette Dock。
- 切換精簡／標準版面，選擇在 Dock 溫度讀值旁合併顯示使用率。
- 查看近期趨勢（記憶體中最多保留 180 個取樣點）、溫度警示及離線瀏覽器儀表板。

## 語言

擴充功能依 Windows 顯示語言，自動使用**繁體中文、英文、日文或簡體中文**。`zh-TW`、`zh-HK`、`ja-JP`、`zh-CN` 等地區語系使用對應翻譯；不支援的語系回退為英文。變更 Windows 顯示語言後，請重新啟動／重新載入擴充功能，再開啟儀表板。

從擴充功能開啟的儀表板沿用相同語言；直接開啟 HTML 檔案時，則依瀏覽器語言偏好選擇。硬體型號、廠商感測器名稱及技術識別碼保留原文；MSIX 套件與安裝程式的產品名稱維持英文。開發細節見[國際化架構與檢查](docs/localization.md)。

## 安裝與使用

1. 從 [Releases](https://github.com/pingqLIN/HardwareMonitorCmdPal/releases) 下載符合架構的安裝程式，以系統管理員身分執行。安裝程式會註冊 MSIX 擴充功能與簽章憑證。
2. 在 PowerToys 啟用 Command Palette，按 **Win+Alt+Space** 開啟，執行 **Reload Command Palette Extension**，接著開啟「**硬體監視器**」。
3. 在讀值的快顯功能表中釘選／取消釘選，或選擇「檢視趨勢圖」。透過 Dock 編輯器加入「**硬體讀值**」區塊。
4. 選擇「**即時圖表儀表板**」開啟離線瀏覽器畫面。沒有即時快照時會標示為示範資料；「模擬高負載」只會改變模擬資料。

CPU 溫度是否可讀，取決於硬體、驅動程式與權限。現有的「**安裝 CPU 溫度開機取樣（一次）**」命令會要求權限提升，設定開機取樣輔助程式；這是選用功能。硬體未提供 CPU 功率時，讀值可能標示為估計值。

設定提供更新間隔（500–3000 毫秒）、過熱／危險臨界值、預設精簡版面與 Dock 使用率合併顯示。預設臨界值：偏熱 70°C、過熱 80°C、危險 90°C。釘選與設定沿用既有的使用者設定目錄；可透過 Windows 已安裝的應用程式解除安裝。

## 建置與驗證

需要 Windows、.NET 10 SDK、Windows SDK 10.0.26100，以及 Visual Studio 的 Windows／WinUI 開發工具。PawnIO 通常安裝於 `C:\Program Files\PawnIO`；PowerToys 需啟用 Command Palette。

```powershell
dotnet build HardwareMonitorExtension.sln -c Debug -p:Platform=x64
dotnet run --project tests/LocalizationSmoke -p:Platform=x64
node tests/dashboard-localization.test.cjs
powershell -NoProfile -File scripts/Export-DashboardLocales.ps1 -Check
```

使用 Visual Studio 開啟方案並部署封裝的 `HardwareMonitorExtension` 專案。單純建置不會註冊擴充功能；開發用的配置註冊需要 Windows 開發人員模式，已簽署的安裝程式則不需要。

建立既有的簽署 MSIX／EXE 安裝程式時，另需 Inno Setup 6：

```powershell
.\build-exe.ps1
.\build-exe.ps1 -Platforms @("x64", "arm64")
```

封裝腳本管理本機簽章憑證並輸出安裝檔。私人簽章資料不可加入 Git；資訊清單的 Publisher 必須與憑證 Subject 相符。

`Localization/` 提供原生介面與離線儀表板共用的翻譯來源。新增翻譯與測試方式見[國際化說明](docs/localization.md)。自動檢查不代表已驗證實際硬體取樣、權限提升或 Command Palette 的畫面排版；這些項目仍需人工驗收。
