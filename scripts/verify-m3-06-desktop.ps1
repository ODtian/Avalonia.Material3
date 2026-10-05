#requires -Version 7.2
param([switch]$KeepSandbox)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'The M3-06 desktop check requires an interactive Windows desktop.' }
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) ('m3-06-desktop-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES

function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}

function Copy-SourceTree {
    param([string]$RelativePath)
    Get-ChildItem (Join-Path $root $RelativePath) -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($root, $_.FullName)
        $destination = Join-Path $sandbox $relative
        New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
        Copy-Item $_.FullName $destination
    }
}

Push-Location $root
try {
    $feed = Join-Path $sandbox 'artifacts/packages'
    New-Item -ItemType Directory -Force $feed | Out-Null
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', $feed)
    foreach ($file in 'Directory.Build.props', 'global.json', 'NuGet.Config') { Copy-Item (Join-Path $root $file) $sandbox }
    Copy-SourceTree 'samples/SelectionDemo'
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'samples/Gallery/Pages') | Out-Null
    Copy-Item (Join-Path $root 'samples/Gallery/Pages/SelectionFormPage.cs') (Join-Path $sandbox 'samples/Gallery/Pages')
    # No src project is copied. Both demo and gallery page compile against a fresh versioned package cache.
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    Invoke-Dotnet @('build', (Join-Path $sandbox 'samples/SelectionDemo/SelectionDemo.csproj'), '-c', 'Release')
    $executable = Join-Path $sandbox 'samples/SelectionDemo/bin/Release/net10.0/SelectionDemo.exe'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-selection-smoke.ps1') -Executable $executable -Screenshots (Join-Path $root 'artifacts/screenshots')
    if ($LASTEXITCODE -ne 0) { throw "M3-06 desktop automation failed ($LASTEXITCODE)." }
    Write-Host 'PASS M3-06: isolated desktop package consumer and native Windows UI Automation.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) { Remove-Item $sandbox -Recurse -Force }
}
