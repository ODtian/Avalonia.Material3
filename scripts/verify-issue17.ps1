#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path $root ('artifacts/issue17-consumer-' + [guid]::NewGuid().ToString('N'))
$results = Join-Path $root 'artifacts/TestResults/issue17'
$previousPackages = $env:NUGET_PACKAGES
$previousNodeReuse = $env:MSBUILDDISABLENODEREUSE
$env:MSBUILDDISABLENODEREUSE = '1'
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-File([string]$relative) {
    $destination = Join-Path $sandbox $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    Copy-Item (Join-Path $root $relative) $destination
}
function Copy-OrdinarySubtree([string]$relative) {
    Get-ChildItem (Join-Path $root $relative) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        ForEach-Object { Copy-File ([IO.Path]::GetRelativePath($root, $_.FullName)) }
}
Push-Location $root
try {
    if ($DesktopSmoke -and -not $IsWindows) { throw '-DesktopSmoke requires Windows and serialized desktop ownership.' }
    New-Item -ItemType Directory -Force $results, (Join-Path $sandbox 'feed') | Out-Null
    Invoke-Dotnet @('test', 'tests/Avalonia.Material3.Tests/Avalonia.Material3.Tests.csproj', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $results)
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '--no-restore', '-o', (Join-Path $sandbox 'feed'))
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-File $file }
    foreach ($sample in 'Gallery', 'StandaloneHost') {
        foreach ($file in "$sample.csproj", 'Program.cs', 'App.axaml', 'App.axaml.cs', 'MainWindow.axaml', 'MainWindow.axaml.cs') { Copy-File "samples/$sample/$file" }
    }
    foreach ($sample in 'TextFieldHost', 'AppChromeHost') {
        foreach ($file in "$sample.csproj", 'Program.cs') { Copy-File "samples/$sample/$file" }
    }
    Copy-OrdinarySubtree 'samples/Gallery/Pages'
    Copy-OrdinarySubtree 'tests/PackageConsumption.Tests'
    Copy-OrdinarySubtree 'tests/ReferenceVectors'
    Get-ChildItem (Join-Path $root 'tests/Avalonia.Material3.Tests') -Filter '*.cs' -File | ForEach-Object { Copy-File "tests/Avalonia.Material3.Tests/$($_.Name)" }
    $config = Join-Path $sandbox 'package-feed.config'
    # Public generated feed configuration only: no repository-root discovery/private configuration copy.
    [IO.File]::WriteAllText($config, @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="local" value="feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><packageSource key="local"><package pattern="Avalonia.Material3" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
'@)
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    Write-Host "Fresh issue #17 package sandbox (no src): $sandbox"
    $packageTests = Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'
    $hostProject = Join-Path $sandbox 'samples/AppChromeHost/AppChromeHost.csproj'
    Invoke-Dotnet @('restore', $packageTests, '--configfile', $config)
    Invoke-Dotnet @('test', $packageTests, '-c', 'Release', '--no-restore', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    Invoke-Dotnet @('restore', $hostProject, '--configfile', $config)
    Invoke-Dotnet @('build', $hostProject, '-c', 'Release', '--no-restore')
    if ($DesktopSmoke) {
        & powershell.exe -NoProfile -File (Join-Path $PSScriptRoot 'windows-appchrome-smoke.ps1') -Executable (Join-Path $sandbox 'samples/AppChromeHost/bin/Release/net10.0/AppChromeHost.exe') -Screenshots (Join-Path $root 'artifacts/screenshots/issue17')
        if ($LASTEXITCODE -ne 0) { throw "Desktop app-chrome smoke failed ($LASTEXITCODE)." }
    }
    Write-Host 'PASS: source suite, new pack, isolated package suite including compiled Gallery, package-only AppChromeHost.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:MSBUILDDISABLENODEREUSE = $previousNodeReuse
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        if ($IsWindows) {
            $tag = Split-Path $sandbox -Leaf
            Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe' AND CommandLine LIKE '%$tag%'" |
                Where-Object { $_.CommandLine -match 'Avalonia.BuildServices.Collector' } |
                ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue }
        }
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force; break }
            catch { if ($attempt -eq 19) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
