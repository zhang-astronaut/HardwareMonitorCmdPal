# 固定清單持久化回歸驗證

使用 .NET 10 SDK 執行：

```powershell
dotnet run --project tests/PinPersistence/PinPersistence.csproj --configuration Release
```

此主控台驗證直接連結正式版的固定清單儲存、感測器模型與取樣 hub 程式碼，涵蓋空白與自訂清單、舊版檔案、預設項目探索，以及 hub 先收到空快照、之後才取得有效讀數的情況。每次執行使用獨立暫存資料，結束後清除。

SensorService 與 LiveSnapshotWriter 使用固定輸入的替代實作，不會存取硬體、啟動瀏覽器或寫入看板資料。擴充功能完整建置與 Command Palette 畫面驗收仍須另外執行。
