#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-issue4-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
$previousScreenshots = $env:M3_ISSUE4_SCREENSHOTS
$previousNodeReuse = $env:MSBUILDDISABLENODEREUSE
$env:MSBUILDDISABLENODEREUSE = '1'
$results = Join-Path $root 'artifacts/TestResults/issue4'
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
    # Only scenario sources: the isolated consumers have no source-library project reference.
    Copy-Item (Join-Path $root 'tests/Avalonia.Material3.Tests/*.cs') (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    $env:M3_ISSUE4_SCREENSHOTS = Join-Path $root 'artifacts/screenshots/issue4'
    Write-Host "Isolated issue #4 package consumer (no src): $sandbox"
    Invoke-Dotnet @('test', (Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'), '-c', 'Release', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    Invoke-Dotnet @('build', (Join-Path $sandbox 'samples/ButtonsHost/ButtonsHost.csproj'), '-c', 'Release')
    if ($DesktopSmoke) {
        $executable = Join-Path $sandbox 'samples/ButtonsHost/bin/Release/net10.0/ButtonsHost.exe'
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-buttons-smoke.ps1') -Executable $executable -Screenshots $env:M3_ISSUE4_SCREENSHOTS
        if ($LASTEXITCODE -ne 0) { throw "Windows buttons smoke failed ($LASTEXITCODE)." }
    }
    Write-Host 'PASS: source, newly packed versioned library, all linked scenarios, ExpressiveButtonsPage and ButtonsHost.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:M3_ISSUE4_SCREENSHOTS = $previousScreenshots
    $env:MSBUILDDISABLENODEREUSE = $previousNodeReuse
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        # Avalonia's short-lived build collector can release its assembly just after dotnet exits.
        # Wait for this sandbox's file handles; never shut down another ticket's build processes.
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force; break }
            catch { if ($attempt -eq 19) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
