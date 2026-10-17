# HardwareMonitorCmdPal

[English](README.md) · [繁體中文](README.zh-TW.md) · 日本語 · [简体中文](README.zh-CN.md)

LibreHardwareMonitor／PawnIO を使い、CPU、GPU、ストレージ、マザーボードなどのセンサーを読み取る PowerToys Command Palette 拡張機能です。

- 利用可能な測定値を一覧表示し、センサーを Command Palette の Dock にピン留めできます。
- コンパクト表示／標準表示を切り替え、Dock の温度表示に使用率を併記できます。
- 最近の推移（メモリ内に最大 180 サンプル）、温度の警告、オフラインのブラウザーダッシュボードを表示します。

## 言語

Windows の表示言語に合わせて、**日本語、英語、繁体字中国語、簡体字中国語**を選択します。`ja-JP`、`zh-TW`、`zh-HK`、`zh-CN` などの地域別の言語にも対応し、未対応の言語では英語を表示します。Windows の表示言語を変更した後は、拡張機能を再起動／再読み込みし、ダッシュボードを開き直してください。

拡張機能から開いたダッシュボードも同じ言語を使います。HTML ファイルを直接開く場合は、ブラウザーの言語設定に従います。ハードウェアの型番、メーカー由来のセンサー名、技術的な識別子は元の表記を保ちます。MSIX パッケージとインストーラーの製品名は英語のままです。[ローカライズの設計と検証](docs/localization.md)も参照してください。

## インストールと使用方法

1. [Releases](https://github.com/pingqLIN/HardwareMonitorCmdPal/releases) から対応するアーキテクチャのインストーラーをダウンロードし、管理者として実行します。MSIX 拡張機能と署名証明書が登録されます。
2. PowerToys で Command Palette を有効にし、**Win+Alt+Space** で開きます。**Reload Command Palette Extension** を実行し、「**ハードウェアモニター**」を開きます。
3. 測定値のコンテキストメニューから Dock へのピン留め／解除や「推移を表示」を選択します。Dock エディターで「**ハードウェアの測定値**」を追加します。
4. 「**ライブダッシュボード**」を開くと、オフラインのブラウザー画面を表示します。実測スナップショットがない場合はデモデータであることを表示します。「高負荷をシミュレーション」はデモデータだけを変更します。

CPU 温度を取得できるかどうかは、ハードウェア、ドライバー、権限によって異なります。既存の「**起動時の CPU 温度測定を設定（初回のみ）**」コマンドは権限の昇格を要求し、起動時の測定ヘルパーを設定します。この機能は任意です。CPU 電力が提供されない場合は、推定値として表示することがあります。

設定では更新間隔（500～3000 ミリ秒）、高温／危険温度のしきい値、既定のコンパクト表示、Dock への使用率の併記を変更できます。既定値は注意 70°C、高温 80°C、危険 90°C です。ピン留めと設定は既存のユーザー別設定ディレクトリに保存します。アンインストールは Windows のインストール済みアプリから行います。

## ビルドと検証

Windows、.NET 10 SDK、Windows SDK 10.0.26100、Windows／WinUI 開発ツールを備えた Visual Studio が必要です。PawnIO の通常のインストール先は `C:\Program Files\PawnIO` です。PowerToys の Command Palette を有効にしてください。

```powershell
dotnet build HardwareMonitorExtension.sln -c Debug -p:Platform=x64
dotnet run --project tests/LocalizationSmoke -p:Platform=x64
node tests/dashboard-localization.test.cjs
powershell -NoProfile -File scripts/Export-DashboardLocales.ps1 -Check
```

Visual Studio でソリューションを開き、パッケージ形式の `HardwareMonitorExtension` プロジェクトを配置します。ビルドだけでは拡張機能は登録されません。開発用の配置登録には Windows の開発者モードが必要ですが、署名済みインストーラーでは不要です。

既存の署名済み MSIX／EXE インストーラーを作成するには、Inno Setup 6 も必要です。

```powershell
.\build-exe.ps1
.\build-exe.ps1 -Platforms @("x64", "arm64")
```

パッケージ作成スクリプトはローカルの署名証明書を管理し、インストーラーを出力します。秘密の署名情報を Git に追加しないでください。マニフェストの Publisher は証明書の Subject と一致する必要があります。

`Localization/` はネイティブ UI とダッシュボードが共有する翻訳元です。翻訳の追加とテストについては[ローカライズの説明](docs/localization.md)を参照してください。自動チェックでは、実際のセンサー測定、権限の昇格、Command Palette の画面レイアウトまでは確認できません。これらは手動で検証する必要があります。
