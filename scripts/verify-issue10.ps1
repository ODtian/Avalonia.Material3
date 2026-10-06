#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-issue10-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
$previousShots = $env:M3_ISSUE10_SCREENSHOTS
$previousReuse = $env:MSBUILDDISABLENODEREUSE
$env:MSBUILDDISABLENODEREUSE = '1'
$results = Join-Path $root 'artifacts/TestResults/issue10'
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-PublicTree([string]$relative) {
    Get-ChildItem (Join-Path $root $relative) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
            $destination = Join-Path $sandbox ([IO.Path]::GetRelativePath($root, $_.FullName))
            New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
            Copy-Item $_.FullName $destination
        }
}
Push-Location $root
try {
    New-Item -ItemType Directory -Force $results, (Join-Path $sandbox 'artifacts/packages') | Out-Null
    Invoke-Dotnet @('test', 'tests/Avalonia.Material3.Tests', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $results)
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', (Join-Path $sandbox 'artifacts/packages'))
    $packageHash = (Get-ChildItem (Join-Path $sandbox 'artifacts/packages') -Filter 'Avalonia.Material3.*.nupkg' | Get-FileHash -Algorithm SHA256).Hash
    Write-Host "Fresh package SHA256: $packageHash"
    Set-Content (Join-Path $results 'package-sha256.txt') $packageHash
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-Item (Join-Path $root $file) $sandbox }
    # Known public feed definitions only. Never inspect/copy the repository's credential-classified configuration.
    $feedConfig = Join-Path $sandbox 'public-feed.config'
    @'
<configuration>
  <packageSources><clear/><add key="local" value="artifacts/packages"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources>
  <packageSourceMapping><packageSource key="local"><package pattern="Avalonia.Material3"/></packageSource><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping>
</configuration>
'@ | Set-Content $feedConfig -Encoding utf8
    Copy-PublicTree 'samples'
    Copy-PublicTree 'tests/PackageConsumption.Tests'
    Copy-PublicTree 'tests/ReferenceVectors'
    New-Item -ItemType Directory -Force (Join-Path $sandbox 'tests/Avalonia.Material3.Tests') | Out-Null
    Copy-Item (Join-Path $root 'tests/Avalonia.Material3.Tests/*.cs') (Join-Path $sandbox 'tests/Avalonia.Material3.Tests')
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    $env:M3_ISSUE10_SCREENSHOTS = Join-Path $root 'artifacts/screenshots/issue10'
    Write-Host "Fresh package consumer (no library source): $sandbox"
    $consumer = Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'
    Invoke-Dotnet @('restore', $consumer, '--configfile', $feedConfig)
    Invoke-Dotnet @('test', $consumer, '-c', 'Release', '--no-restore', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    $hostProject = Join-Path $sandbox 'samples/ButtonGroupsHost/ButtonGroupsHost.csproj'
    Invoke-Dotnet @('restore', $hostProject, '--configfile', $feedConfig)
    Invoke-Dotnet @('build', $hostProject, '-c', 'Release', '--no-restore')
    if ($DesktopSmoke) {
        if (-not $IsWindows) { throw 'Native smoke requires an interactive Windows desktop.' }
        & powershell.exe -NoProfile -File (Join-Path $PSScriptRoot 'windows-button-groups-smoke.ps1') -Executable (Join-Path $sandbox 'samples/ButtonGroupsHost/bin/Release/net10.0/ButtonGroupsHost.exe') -Screenshots $env:M3_ISSUE10_SCREENSHOTS
        if ($LASTEXITCODE -ne 0) { throw 'Native button groups smoke failed.' }
    }
    Write-Host 'Verified source/package/gallery/host; completing isolated sandbox cleanup.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:M3_ISSUE10_SCREENSHOTS = $previousShots
    $env:MSBUILDDISABLENODEREUSE = $previousReuse
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        for ($attempt = 0; $attempt -lt 240; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force; break }
            catch { if ($attempt -eq 239) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
Write-Host 'PASS: source suite, fresh versioned package suite, compiled ButtonGroupsPage and ButtonGroupsHost; cleanup completed (or sandbox explicitly retained).'
