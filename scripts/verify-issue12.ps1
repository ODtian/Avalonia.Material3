#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-issue12-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
$previousScreenshots = $env:M3_ISSUE12_SCREENSHOTS
$previousNodeReuse = $env:MSBUILDDISABLENODEREUSE
$env:MSBUILDDISABLENODEREUSE = '1'
$results = Join-Path $root 'artifacts/TestResults/issue12'
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
    foreach ($file in 'Directory.Build.props', 'global.json', 'NuGet.Config') { Copy-Item (Join-Path $root $file) $sandbox }
    Copy-ConsumerTree 'samples'
    Copy-ConsumerTree 'tests/PackageConsumption.Tests'
    Copy-ConsumerTree 'tests/ReferenceVectors'
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'tests/Avalonia.Material3.Tests') | Out-Null
    Copy-Item (Join-Path $root 'tests/Avalonia.Material3.Tests/*.cs') (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    $env:M3_ISSUE12_SCREENSHOTS = Join-Path $root 'artifacts/screenshots/issue12'
    Write-Host "Isolated issue #12 package consumer (no src): $sandbox"
    Invoke-Dotnet @('test', (Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'), '-c', 'Release', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    Invoke-Dotnet @('build', (Join-Path $sandbox 'samples/ProgressHost/ProgressHost.csproj'), '-c', 'Release')
    if ($DesktopSmoke) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-progress-smoke.ps1') -Executable (Join-Path $sandbox 'samples/ProgressHost/bin/Release/net10.0/ProgressHost.exe') -Screenshots $env:M3_ISSUE12_SCREENSHOTS
        if ($LASTEXITCODE -ne 0) { throw "Windows progress smoke failed ($LASTEXITCODE)." }
    }
    Write-Host 'PASS: source, freshly packed library, compiled progress gallery, all linked scenarios and independent ProgressHost.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:M3_ISSUE12_SCREENSHOTS = $previousScreenshots
    $env:MSBUILDDISABLENODEREUSE = $previousNodeReuse
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        # Avalonia's collector outlives its build and locks its own DLL. Only stop collectors
        # loaded from this uniquely-owned sandbox; never global compilers or another ticket.
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
