#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-issue13-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
$previousScreenshots = $env:M3_ISSUE13_SCREENSHOTS
$previousNodeReuse = $env:MSBUILDDISABLENODEREUSE
$env:MSBUILDDISABLENODEREUSE = '1'
$results = Join-Path $root 'artifacts/TestResults/issue13'
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-ConsumerTree([string]$relative) {
    Get-ChildItem (Join-Path $root $relative) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
            $destination = Join-Path $sandbox ([IO.Path]::GetRelativePath($root, $_.FullName))
            New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
            Copy-Item $_.FullName $destination
        }
}
Push-Location $root
try {
    if ($DesktopSmoke -and -not $IsWindows) { throw '-DesktopSmoke requires an interactive Windows desktop.' }
    New-Item -ItemType Directory -Force $results, (Join-Path $sandbox 'artifacts/packages') | Out-Null
    Invoke-Dotnet @('test', 'tests/Avalonia.Material3.Tests/Avalonia.Material3.Tests.csproj', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $results)
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', (Join-Path $sandbox 'artifacts/packages'))
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-Item (Join-Path $root $file) $sandbox }
    # A public-only disposable feed configuration. No repository/private configuration is read or copied.
    $config = Join-Path $sandbox 'public-feed.config'
    $feed = [System.Security.SecurityElement]::Escape((Join-Path $sandbox 'artifacts/packages'))
    [IO.File]::WriteAllText($config, @"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear/><add key="local" value="$feed"/><add key="public" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><clear/><packageSource key="local"><package pattern="Avalonia.Material3"/></packageSource><packageSource key="public"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
"@)
    foreach ($tree in 'samples/Gallery', 'samples/StandaloneHost', 'samples/TextFieldHost', 'samples/DialogsHost', 'tests/PackageConsumption.Tests', 'tests/ReferenceVectors') { Copy-ConsumerTree $tree }
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'tests/Avalonia.Material3.Tests') | Out-Null
    Copy-Item (Join-Path $root 'tests/Avalonia.Material3.Tests/*.cs') (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    $env:M3_ISSUE13_SCREENSHOTS = Join-Path $root 'artifacts/screenshots/issue13'
    Write-Host "Isolated issue #13 package consumer (no library src): $sandbox"
    Invoke-Dotnet @('restore', (Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'), '--configfile', $config)
    Invoke-Dotnet @('test', (Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'), '-c', 'Release', '--no-restore', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    Invoke-Dotnet @('restore', (Join-Path $sandbox 'samples/DialogsHost/DialogsHost.csproj'), '--configfile', $config)
    Invoke-Dotnet @('build', (Join-Path $sandbox 'samples/DialogsHost/DialogsHost.csproj'), '-c', 'Release', '--no-restore')
    if ($DesktopSmoke) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-dialogs-smoke.ps1') -Executable (Join-Path $sandbox 'samples/DialogsHost/bin/Release/net10.0/DialogsHost.exe') -Screenshots $env:M3_ISSUE13_SCREENSHOTS
        if ($LASTEXITCODE -ne 0) { throw "Windows dialogs smoke failed ($LASTEXITCODE)." }
    }
    Write-Host 'PASS: source, fresh library package/feed/cache, compiled dialogs gallery, linked scenarios and independent DialogsHost.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:M3_ISSUE13_SCREENSHOTS = $previousScreenshots
    $env:MSBUILDDISABLENODEREUSE = $previousNodeReuse
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        if ($IsWindows) {
            Get-CimInstance Win32_Process | Where-Object {
                $_.Name -eq 'dotnet.exe' -and $_.CommandLine -and $_.CommandLine.Contains($sandbox) -and
                $_.CommandLine.Contains('Avalonia.BuildServices.Collector.dll')
            } | ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue }
        }
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force; break }
            catch { if ($attempt -eq 19) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
