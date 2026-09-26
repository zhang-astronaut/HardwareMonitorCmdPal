# Admin helper: samples CPU Tctl via LibreHardwareMonitor (elevated) and writes JSON for the extension.
$ErrorActionPreference = 'SilentlyContinue'
$outDir = Join-Path $env:LOCALAPPDATA 'HardwareMonitorExtension'
$out = Join-Path $outDir 'cpu-temp-live.json'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$lhm = @(
  (Join-Path $PSScriptRoot 'LHM.Runtime.dll'),
  (Join-Path $outDir 'LHM.Runtime.dll')
) + (Get-ChildItem -Path (Join-Path $env:USERPROFILE '.nuget\packages\librehardwaremonitorlib') -Filter LHM.Runtime.dll -Recurse -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | ForEach-Object { $_.FullName })

$lhmPath = $lhm | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $lhmPath) {
  @{ cpuTctl = $null; error = 'LHM.Runtime.dll not found' } | ConvertTo-Json | Set-Content $out
  exit 1
}

Add-Type -Path $lhmPath
$computer = New-Object LibreHardwareMonitor.Hardware.Computer
$computer.IsCpuEnabled = $true
$computer.IsGpuEnabled = $true
$computer.IsStorageEnabled = $true
$computer.IsMotherboardEnabled = $true
$computer.Open()

while ($true) {
  $cpuT = $null
  foreach ($hw in $computer.Hardware) {
    $hw.Update()
    foreach ($s in $hw.Sensors) {
      if ($s.SensorType -eq [LibreHardwareMonitor.Hardware.SensorType]::Temperature -and $s.Name -match 'Tctl|Tdie') {
        if ($s.Value -and $s.Value -gt 1) { $cpuT = [double]$s.Value; break }
      }
    }
    if ($cpuT) { break }
    foreach ($sub in $hw.SubHardware) {
      $sub.Update()
      foreach ($s in $sub.Sensors) {
        if ($s.SensorType -eq [LibreHardwareMonitor.Hardware.SensorType]::Temperature -and $s.Name -match 'Tctl|Tdie') {
          if ($s.Value -and $s.Value -gt 1) { $cpuT = [double]$s.Value; break }
        }
      }
      if ($cpuT) { break }
    }
    if ($cpuT) { break }
  }

  if ($cpuT) {
    @{ cpuTctl = [math]::Round($cpuT, 2); ts = (Get-Date).ToUniversalTime().ToString('o') } |
      ConvertTo-Json | Set-Content $out -Encoding UTF8
  }
  Start-Sleep -Milliseconds 1000
}

