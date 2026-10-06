#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-issue18-' + [guid]::NewGuid().ToString('N'))
$oldPackages = $env:NUGET_PACKAGES
$oldScreenshots = $env:M3_ISSUE18_SCREENSHOTS
$oldReuse = $env:MSBUILDDISABLENODEREUSE
$env:MSBUILDDISABLENODEREUSE = '1'
$results = Join-Path $root 'artifacts/TestResults/issue18'
function Run-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)" }
}
function Copy-PublicTree([string]$relative) {
    Get-ChildItem (Join-Path $root $relative) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
            $target = Join-Path $sandbox ([IO.Path]::GetRelativePath($root, $_.FullName))
            New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
            Copy-Item $_.FullName $target
        }
}
Push-Location $root
try {
    if ($DesktopSmoke -and -not $IsWindows) { throw 'Interactive Windows desktop required.' }
    New-Item -ItemType Directory -Force $results, (Join-Path $sandbox 'artifacts/packages') | Out-Null
    $env:M3_ISSUE18_SCREENSHOTS = Join-Path $root 'artifacts/screenshots/issue18'
    Run-Dotnet @('test', 'tests/Avalonia.Material3.Tests', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $results)
    Run-Dotnet @('pack', 'src/Avalonia.Material3', '-c', 'Release', '-o', (Join-Path $sandbox 'artifacts/packages'))
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-Item (Join-Path $root $file) $sandbox }
    $config = Join-Path $sandbox 'public-feed.config'
    $feed = [System.Security.SecurityElement]::Escape((Join-Path $sandbox 'artifacts/packages'))
    [IO.File]::WriteAllText($config, @"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear/><add key="ticket" value="$feed"/><add key="public" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><clear/><packageSource key="ticket"><package pattern="Avalonia.Material3"/></packageSource><packageSource key="public"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
"@)
    # Only explicitly public ordinary trees; never read/copy repository configuration or a library source tree.
    foreach ($tree in 'samples/Gallery', 'samples/StandaloneHost', 'samples/TextFieldHost', 'samples/PickersHost', 'tests/PackageConsumption.Tests', 'tests/ReferenceVectors') { Copy-PublicTree $tree }
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'tests/Avalonia.Material3.Tests') | Out-Null
    Copy-Item (Join-Path $root 'tests/Avalonia.Material3.Tests/*.cs') (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    Write-Host "Fresh issue #18 package consumer (no library src): $sandbox"
    Run-Dotnet @('restore', (Join-Path $sandbox 'tests/PackageConsumption.Tests'), '--configfile', $config)
    Run-Dotnet @('test', (Join-Path $sandbox 'tests/PackageConsumption.Tests'), '-c', 'Release', '--no-restore', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    Run-Dotnet @('restore', (Join-Path $sandbox 'samples/PickersHost'), '--configfile', $config)
    Run-Dotnet @('build', (Join-Path $sandbox 'samples/PickersHost'), '-c', 'Release', '--no-restore')
    if ($DesktopSmoke) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-pickers-smoke.ps1') `
            -Executable (Join-Path $sandbox 'samples/PickersHost/bin/Release/net10.0/PickersHost.exe') -Screenshots $env:M3_ISSUE18_SCREENSHOTS
        if ($LASTEXITCODE -ne 0) { throw "Native pickers smoke failed ($LASTEXITCODE)" }
    }
    Write-Host 'PASS source, fresh package/cache/feed, all linked package scenarios, actual gallery and independent PickersHost.'
}
finally {
    $env:NUGET_PACKAGES = $oldPackages
    $env:M3_ISSUE18_SCREENSHOTS = $oldScreenshots
    $env:MSBUILDDISABLENODEREUSE = $oldReuse
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        if ($IsWindows) {
            Get-CimInstance Win32_Process | Where-Object {
                $_.Name -eq 'dotnet.exe' -and $_.CommandLine -and $_.CommandLine.Contains($sandbox) -and $_.CommandLine.Contains('Avalonia.BuildServices.Collector.dll')
            } | ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue }
        }
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force; break }
            catch { if ($attempt -eq 19) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
