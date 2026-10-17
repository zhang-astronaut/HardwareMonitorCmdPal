# Pin persistence regression harness

Run with the .NET 10 SDK:

```powershell
dotnet run --project tests/PinPersistence/PinPersistence.csproj --configuration Release
```

This console harness links the production pin store, sensor models, and sampling hub. It checks persisted empty/custom selections, legacy files, default discovery, and a hub receiving an empty snapshot before usable readings. Temporary fixtures are isolated per run and deleted afterward.

SensorService and LiveSnapshotWriter are deterministic stand-ins, so these checks do not access hardware, launch a browser, or write dashboard files. Full extension build and Command Palette acceptance remain separate checks.
