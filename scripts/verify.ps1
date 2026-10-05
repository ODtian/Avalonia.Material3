#requires -Version 7.2
param(
    [switch]$DesktopSmoke,
    [switch]$KeepSandbox
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$artifacts = Join-Path $root 'artifacts'
$feed = Join-Path $artifacts 'packages'
$results = Join-Path $artifacts 'TestResults'
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) ('m3-consumer-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES

function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}

function Copy-SourceTree {
    param([string]$RelativePath)
    $source = Join-Path $root $RelativePath
    Get-ChildItem $source -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($root, $_.FullName)
        $destination = Join-Path $sandbox $relative
        New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
        Copy-Item $_.FullName $destination
    }
}

Push-Location $root
try {
    if ($DesktopSmoke -and -not $IsWindows) { throw '-DesktopSmoke requires an interactive Windows desktop.' }
    New-Item -ItemType Directory -Force $feed, $results | Out-Null
    # Package consumers are tested below only after a fresh pack, never against a stale same-version cache.
    Invoke-Dotnet @('test', 'tests/Avalonia.Material3.Tests/Avalonia.Material3.Tests.csproj', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $results)
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', $feed)

    # Copy only consumers and tests, never the library project, to a new directory and cache.
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'artifacts/packages') | Out-Null
    Copy-Item (Join-Path $feed '*.nupkg') (Join-Path $sandbox 'artifacts/packages')
    foreach ($file in 'Directory.Build.props', 'global.json', 'NuGet.Config') {
        Copy-Item (Join-Path $root $file) $sandbox
    }
    Copy-SourceTree 'samples'
    Copy-SourceTree 'tests/PackageConsumption.Tests'
    Copy-SourceTree 'tests/ReferenceVectors'
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'tests/Avalonia.Material3.Tests') | Out-Null
    foreach ($file in 'ButtonHost.cs', 'ButtonScenarioTests.cs', 'ContractScenarioTests.cs', 'SliderScenarioTests.cs', 'ThemeScenarioTests.cs', 'TokenReferenceScenarioTests.cs', 'ThemeGalleryScenarioTests.cs', 'ExpressiveButtonScenarioTests.cs', 'SelectionHost.cs', 'SelectionScenarioTests.cs', 'SelectionFormScenarioTests.cs', 'SelectionAdaptationScenarioTests.cs', 'TextFieldScenarioTests.cs', 'ContentScenarioTests.cs') {
        Copy-Item (Join-Path $root "tests/Avalonia.Material3.Tests/$file") (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    }
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    Write-Host "Isolated package consumer: $sandbox"
    Invoke-Dotnet @('test', (Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'), '-c', 'Release', '--logger', 'trx;LogFileName=package-consumption.trx', '--results-directory', $results)

    if ($DesktopSmoke) {
        foreach ($sample in 'Gallery', 'StandaloneHost') {
            $executable = Join-Path $sandbox "samples/$sample/bin/Release/net10.0/$sample.exe"
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-smoke.ps1') -Executable $executable -Screenshots (Join-Path $artifacts 'screenshots')
            if ($LASTEXITCODE -ne 0) { throw "Desktop smoke failed for $sample." }
        }
    }
    Write-Host 'PASS: source scenarios, versioned package, gallery and independent consumer.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        # Build-service processes can release collector DLL handles just after dotnet exits.
        # Retry only this owned sandbox; never shut down another agent's build servers.
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force -ErrorAction Stop; break }
            catch { if ($attempt -eq 19) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
