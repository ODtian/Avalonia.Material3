#requires -Version 7.2
param([switch]$KeepSandbox)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-theme-consumer-' + [guid]::NewGuid().ToString('N'))
$oldPackages = $env:NUGET_PACKAGES
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-ConsumerTree([string]$path) {
    Get-ChildItem (Join-Path $root $path) -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($root, $_.FullName)
        $target = Join-Path $sandbox $relative
        New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
        Copy-Item $_.FullName $target
    }
}
Push-Location $root
try {
    $results = Join-Path $root 'artifacts/TestResults/theme'
    New-Item -ItemType Directory -Force $results, (Join-Path $sandbox 'artifacts/packages') | Out-Null
    Invoke-Dotnet @('test','tests/Avalonia.Material3.Tests','-c','Release','--logger','trx;LogFileName=theme-source.trx','--results-directory',$results)
    Invoke-Dotnet @('pack','src/Avalonia.Material3/Avalonia.Material3.csproj','-c','Release','-o',(Join-Path $sandbox 'artifacts/packages'))
    foreach ($file in 'Directory.Build.props', 'global.json', 'NuGet.Config') { Copy-Item (Join-Path $root $file) $sandbox }
    Copy-ConsumerTree 'samples'
    Copy-ConsumerTree 'tests/PackageConsumption.Tests'
    Copy-ConsumerTree 'tests/ReferenceVectors'
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'tests/Avalonia.Material3.Tests') | Out-Null
    foreach ($file in 'ButtonHost.cs','ButtonScenarioTests.cs','ButtonGroupScenarioTests.cs','ButtonGroupMatrixScenarioTests.cs','ContractScenarioTests.cs','ThemeScenarioTests.cs','TokenReferenceScenarioTests.cs','ThemeGalleryScenarioTests.cs','ExpressiveButtonScenarioTests.cs','SliderScenarioTests.cs','SelectionHost.cs','SelectionScenarioTests.cs','SelectionFormScenarioTests.cs','SelectionAdaptationScenarioTests.cs','TextFieldScenarioTests.cs','ContentScenarioTests.cs','ProgressScenarioTests.cs','CarouselRefreshScenarioTests.cs','CarouselGalleryScenarioTests.cs','ProgressMatrixScenarioTests.cs','ProgressGalleryScenarioTests.cs','SearchChipScenarioTests.cs','SearchGalleryScenarioTests.cs','SearchAdaptationScenarioTests.cs','FloatingActionScenarioTests.cs','FloatingActionContractTests.cs','FloatingActionsGalleryTests.cs','DialogScenarioTests.cs','DialogGalleryScenarioTests.cs','NavigationScenarioTests.cs','NavigationMatrixScenarioTests.cs','NavigationGalleryScenarioTests.cs') {
        Copy-Item (Join-Path $root "tests/Avalonia.Material3.Tests/$file") (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    }
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    Write-Host "Isolated theme/package consumer (no library src): $sandbox"
    Invoke-Dotnet @('test',(Join-Path $sandbox 'tests/PackageConsumption.Tests'),'-c','Release','--logger','trx;LogFileName=theme-package.trx','--results-directory',$results)
    Write-Host 'PASS: full theme scenarios and Gallery page through a freshly packed versioned NuGet package.'
}
finally {
    $env:NUGET_PACKAGES = $oldPackages
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) { Remove-Item $sandbox -Recurse -Force }
}
