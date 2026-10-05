#requires -Version 7.2
param([switch]$KeepSandbox)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-sliders-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-SourceTree([string]$relative) {
    $source = Join-Path $root $relative
    Get-ChildItem $source -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
        $target = Join-Path $sandbox ([IO.Path]::GetRelativePath($root, $_.FullName))
        New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
        Copy-Item $_.FullName $target
    }
}
try {
    New-Item -ItemType Directory -Force "$sandbox/feed", "$root/artifacts/TestResults" | Out-Null
    Invoke-Dotnet @('test', "$root/tests/Avalonia.Material3.Tests/Avalonia.Material3.Tests.csproj", '-c', 'Release', '--logger', 'trx;LogFileName=sliders-source.trx', '--results-directory', "$root/artifacts/TestResults")
    Invoke-Dotnet @('pack', "$root/src/Avalonia.Material3/Avalonia.Material3.csproj", '-c', 'Release', '-o', "$sandbox/feed")
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-Item (Join-Path $root $file) $sandbox }
    @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="ticket-feed" value="feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping>
    <packageSource key="ticket-feed"><package pattern="Avalonia.Material3" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
'@ | Set-Content (Join-Path $sandbox 'NuGet.Config')
    Copy-SourceTree 'samples'
    Copy-SourceTree 'tests/PackageConsumption.Tests'
    New-Item -ItemType Directory -Force "$sandbox/tests/Avalonia.Material3.Tests" | Out-Null
    # Copy scenario sources only: no source library, no library ProjectReference.
    Copy-Item "$root/tests/Avalonia.Material3.Tests/*.cs" "$sandbox/tests/Avalonia.Material3.Tests"
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    Write-Host "Fresh package-only slider consumer: $sandbox"
    Invoke-Dotnet @('test', "$sandbox/tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj", '-c', 'Release', '--logger', 'trx;LogFileName=sliders-package.trx', '--results-directory', "$root/artifacts/TestResults")
    Write-Host 'PASS: source suite, fresh versioned package, gallery save/reopen and slider input/automation scenarios.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) { Remove-Item $sandbox -Recurse -Force }
}
