#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-issue9-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
$previousNodeReuse = $env:MSBUILDDISABLENODEREUSE
$previousScreenshots = $env:M3_ISSUE9_SCREENSHOTS
$env:M3_ISSUE9_SCREENSHOTS = Join-Path $root 'artifacts/screenshots/issue9'
$env:MSBUILDDISABLENODEREUSE = '1'
$results = Join-Path $root 'artifacts/TestResults/issue9'
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-ConsumerTree([string]$relative) {
    Get-ChildItem (Join-Path $root $relative) -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
        $destination = Join-Path $sandbox ([IO.Path]::GetRelativePath($root, $_.FullName))
        New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
        Copy-Item $_.FullName $destination
    }
}
Push-Location $root
try {
    if ($DesktopSmoke -and -not $IsWindows) { throw '-DesktopSmoke requires an interactive Windows desktop.' }
    New-Item -ItemType Directory -Force $results, (Join-Path $sandbox 'artifacts/packages') | Out-Null
    Invoke-Dotnet @('test', 'tests/Avalonia.Material3.Tests', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $results)
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', (Join-Path $sandbox 'artifacts/packages'))
    foreach ($file in 'Directory.Build.props', 'global.json', 'NuGet.Config') { Copy-Item (Join-Path $root $file) $sandbox }
    Copy-ConsumerTree 'samples'
    Copy-ConsumerTree 'tests/PackageConsumption.Tests'
    Copy-ConsumerTree 'tests/ReferenceVectors'
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'tests/Avalonia.Material3.Tests') | Out-Null
    Copy-Item (Join-Path $root 'tests/Avalonia.Material3.Tests/*.cs') (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    Write-Host "Isolated issue #9 consumer (no src, fresh feed/cache): $sandbox"
    Invoke-Dotnet @('test', (Join-Path $sandbox 'tests/PackageConsumption.Tests'), '-c', 'Release', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    Invoke-Dotnet @('build', (Join-Path $sandbox 'samples/FloatingActionsHost'), '-c', 'Release')
    if ($DesktopSmoke) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-floating-actions-smoke.ps1') -Executable (Join-Path $sandbox 'samples/FloatingActionsHost/bin/Release/net10.0/FloatingActionsHost.exe') -Screenshots (Join-Path $root 'artifacts/screenshots/issue9')
        if ($LASTEXITCODE -ne 0) { throw "Windows floating actions smoke failed ($LASTEXITCODE)." }
    }
    Write-Host 'PASS: source, fresh versioned package, all linked scenarios, compiled FloatingActionsPage and independent package host.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:MSBUILDDISABLENODEREUSE = $previousNodeReuse
    $env:M3_ISSUE9_SCREENSHOTS = $previousScreenshots
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        # Avalonia's build collector may retain its own cache DLL beyond the final native test.
        # Retry only this owned sandbox; never shut down shared compilers or other agents' processes.
        for ($attempt = 0; $attempt -lt 120; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force -ErrorAction Stop; break }
            catch { if ($attempt -eq 119) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
