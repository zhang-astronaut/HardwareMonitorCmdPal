# HardwareMonitorExtension — PowerToys コマンド パレット拡張機能

**PawnIO / LibreHardwareMonitor** を使用して CPU、GPU、およびディスクの温度と使用率を読み取ります。以下の機能をサポートしています:

- コマンド パレットのトップレベル コマンド + **Dock ショートカット温度バンド** (`GetDockBands`)
- Compact / Default のデュアル密度レイアウト（Title 自体に温度が含まれているため、Dock Compact で Subtitle が非表示になっても視認可能）
- 定期更新 + 温度レベル別の色分けマーカー（`⚠` / `▲` / `🔥`）
- 温度履歴: Markdown ミニ スパークライン（180 ポイントのインメモリ リング バッファー）

ドキュメント リファレンス:

- [拡張機能の仕組み](https://learn.microsoft.com/ja-jp/windows/powertoys/command-palette/extensibility-overview)
- [拡張機能の開発](https://learn.microsoft.com/ja-jp/windows/powertoys/command-palette/extension-development)
- [拡張機能の作成](https://learn.microsoft.com/ja-jp/windows/powertoys/command-palette/creating-an-extension)
- [コマンドの追加](https://learn.microsoft.com/ja-jp/windows/powertoys/command-palette/adding-commands)
- [拡張機能の公開](https://learn.microsoft.com/ja-jp/windows/powertoys/command-palette/publish-extension)

## ローカル依存関係

| コンポーネント | パス / 説明 |
| --- | --- |
| PawnIO | `C:\Program Files\PawnIO` (`PawnIOLib.dll`) |
| PowerToys | コマンド パレットが有効になっている必要があります (`Win+Alt+Space`) |
| .NET SDK | 公式テンプレートに準拠: `net10.0-windows10.0.26100.0` |
| Visual Studio | WinUI / Windows App SDK ワークロードおよび開発者モードが有効であること |

## プロジェクト構造（公式テンプレートに準拠）

```
HardwareMonitorExtension/
├─ Directory.Build.props
├─ Directory.Packages.props
├─ nuget.config
├─ HardwareMonitorExtension.sln
└─ HardwareMonitorExtension/
   ├─ HardwareMonitorExtension.csproj
   ├─ app.manifest
   ├─ Package.appxmanifest          # com.microsoft.commandpalette + COM CLSID
   ├─ Program.cs                    # -RegisterProcessAsComServer
   ├─ HardwareMonitorExtension.cs   # IExtension + [Guid]
   ├─ HardwareMonitorCommandsProvider.cs
   ├─ SensorHub.cs
   ├─ HardwareMonitorSettingsManager.cs
   ├─ Assets/                       # StoreLogo、Web ダッシュボードなど
   ├─ Localization/                 # ローカライズ サブシステム (en-US, zh-Hans)
   ├─ Services/
   │  ├─ PawnIoLocator.cs           # レジストリ検索 + P/Invoke
   │  ├─ SensorModels.cs
   │  └─ SensorService.cs           # LHM + PawnIO 検出 + ACPI フォールバック
   ├─ Pages/
   │  ├─ SensorsListPage.cs         # ListPage + RaiseItemsChanged
   │  └─ SensorChartPage.cs         # ContentPage + MarkdownContent
   ├─ Dock/
   │  └─ PinnedSensorsDockBand.cs   # GetDockBands → ショートカット バー
   └─ Properties/PublishProfiles/
```

CLSID（3箇所すべてで統一）: `3F8A2C91-6B4E-4D2A-9C17-8E5F0A7B4D63`

## インストール（ユーザー向け）

Releases から `HardwareMonitorExtension-Setup-<Version>-x64.exe` をダウンロードし、管理者として実行します:

1. インストーラーが署名証明書をローカル マシンの `TrustedPeople` 証明書ストアに自動インポートします。
2. `Add-AppxPackage` により MSIX 拡張機能を自動登録します（**開発者モードは不要**）。
3. コマンド パレットで `Reload` と入力 → **Reload Command Palette Extension** を選択します。
4. 「Hardware Monitor」を開き、Dock 編集モードまたは右クリックのコンテキスト メニューから **Hardware Readings** をショートカット バーに固定します。

アンインストール: Windows の「インストールされているアプリ」またはコントロール パネルから本プログラムをアンインストールすると、MSIX パッケージと証明書が自動的に削除されます。

## ローカル デプロイ（開発向け）

1. Visual Studio で `HardwareMonitorExtension.sln` を開きます。
2. 構成を **Debug | x64** に設定します。
3. **ビルド → HardwareMonitorExtension の配置** を実行します（「パッケージ化されていない」構成で実行しないでください）。
4. コマンド パレットで `Reload` と入力 → **Reload Command Palette Extension** を選択します。

コマンド ライン（.NET 10 SDK + Windows SDK 10.0.26100 が必要）:

```powershell
cd HardwareMonitorExtension
dotnet build -c Debug -p:Platform=x64
Add-AppxPackage -Register "bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AppxManifest.xml"
# レイアウト登録 (-Register) に署名は不要ですが、開発者モードが必要です。ビルド単体ではパッケージは登録されません。
```

## インストーラーのビルド（メンテナー向け）

```powershell
.\build-exe.ps1            # x64。マルチアーキテクチャ ビルドの場合は -Platforms @("x64","arm64") を追加
```

スクリプトは以下の順序で実行されます: 自己署名証明書の確認（`cert/HardwareMonitor.pfx`。Subject はマニフェストの Publisher `CN=HardwareMonitor` と一致する必要があります。`*.pfx` は gitignore されておりコミットされません）→ 署名済み MSIX のビルド → Inno Setup によるインストーラーのコンパイル（出力先: `bin/Release/installer`）。インストーラーには MSIX と証明書公開鍵が埋め込まれており、インストール後に自動登録されます。

注意: MSIX はパッケージ ID の `Publisher` と署名証明書の Subject が完全に一致している必要があります。証明書を変更した場合は、`Package.appxmanifest` も同期して更新する必要があります。

## データ パイプライン

```
PawnIOLib (インストール済み)
   └─ SensorHub (定期サンプリング)
        ├─ SensorsListPage       RaiseItemsChanged
        ├─ SensorChartPage       Markdown スパークライン
        └─ PinnedSensorsDockBand Title 更新 (PropChanged)
```

デフォルトのしきい値: やや高温 70°C / 高温 80°C / 危険 90°C（拡張機能の設定ページで設定可能）。

## 公開

[PUBLISH.md](../PUBLISH.md) を参照: Inno Setup EXE + WinGet (`windows-commandpalette-extension` タグ) + 拡張機能カタログのエントリ。
