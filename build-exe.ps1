# Build EXE installer for WinGet (official CmdPal publish flow)
param(
    [string]$ExtensionName = "HardwareMonitorExtension",
    [string]$Configuration = "Release",
    [string]$Version = "0.1.0.0",
    [string[]]$Platforms = @("x64")
)

$ErrorActionPreference = "Stop"

if (-not $env:ProgramFiles) { $env:ProgramFiles = 'C:\Program Files' }
if (-not $env:LOCALAPPDATA) { $env:LOCALAPPDATA = Join-Path $env:USERPROFILE 'AppData\Local' }
if (-not $env:DOTNET_ROOT) { $env:DOTNET_ROOT = 'C:\Program Files\dotnet' }
$env:Path = "C:\Program Files\dotnet;$env:Path"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ProjectDir "$ExtensionName\$ExtensionName.csproj"
$Inno = @(
    'C:\Users\zhang\AppData\Local\Programs\Inno Setup 6\ISCC.exe',
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

Write-Host "Project: $ProjectFile"
Write-Host "Inno: $Inno"
Write-Host "Version: $Version Platforms: $($Platforms -join ',')"

foreach ($p in @("bin", "obj")) {
    $path = Join-Path $ProjectDir $p
    if (Test-Path $path) { Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue }
}

$setupTemplate = Get-Content (Join-Path $ProjectDir 'setup.iss') -Raw

foreach ($Platform in $Platforms) {
    $runtime = "win-$Platform"
    $publishDir = Join-Path $ProjectDir "bin\$Configuration\$runtime\publish"
    Write-Host "=== Publish $Platform ==="

    & dotnet publish $ProjectFile `
        --configuration $Configuration `
        --runtime $runtime `
        --self-contained true `
        -p:Platform=$Platform `
        -p:WindowsPackageType=None `
        -p:PublishSingleFile=true `
        --output $publishDir

    if ($LASTEXITCODE -ne 0) { throw "publish failed for $Platform" }

    $fileCount = (Get-ChildItem $publishDir -Recurse -File -ErrorAction SilentlyContinue).Count
    Write-Host "Published $fileCount files"

    $script = $setupTemplate -replace '#define AppVersion ".*"', "#define AppVersion `"$Version`""
    $script = $script -replace 'OutputBaseFilename=(.*?)\{#AppVersion\}', "OutputBaseFilename=`$1{#AppVersion}-$Platform"
    $script = $script -replace 'Source: "bin\\Release\\win-x64\\publish', "Source: `"bin\Release\$runtime\publish"

    if ($Platform -eq 'arm64') {
        $script = $script -replace '(\[Setup\][^\[]*)(MinVersion=)', "`$1ArchitecturesAllowed=arm64`r`nArchitecturesInstallIn64BitMode=arm64`r`n`$2"
    } else {
        $script = $script -replace '(\[Setup\][^\[]*)(MinVersion=)', "`$1ArchitecturesAllowed=x64compatible`r`nArchitecturesInstallIn64BitMode=x64compatible`r`n`$2"
    }

    $iss = Join-Path $ProjectDir "setup-$Platform.iss"
    Set-Content -Path $iss -Value $script -Encoding UTF8

    if (-not $Inno) { throw 'Inno Setup not found' }
    & $Inno $iss
    if ($LASTEXITCODE -ne 0) { throw "Inno failed $LASTEXITCODE" }
    Write-Host "Installer OK for $Platform"
}

Get-ChildItem (Join-Path $ProjectDir "bin\$Configuration\installer") -ErrorAction SilentlyContinue | Select-Object Name, Length
