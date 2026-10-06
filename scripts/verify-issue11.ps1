#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-issue11-' + [guid]::NewGuid().ToString('N'))
$results = Join-Path $root 'artifacts/TestResults/issue11'
$previousPackages = $env:NUGET_PACKAGES
$previousScreenshots = $env:M3_ISSUE11_SCREENSHOTS
$previousNodeReuse = $env:MSBUILDDISABLENODEREUSE
$env:MSBUILDDISABLENODEREUSE = '1'
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-OrdinaryFile([string]$relative) {
    $destination = Join-Path $sandbox $relative
    New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
    Copy-Item (Join-Path $root $relative) $destination
}
function Copy-OrdinaryTree([string]$relative) {
    Get-ChildItem (Join-Path $root $relative) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs', '.csproj', '.json', '.axaml' } |
        ForEach-Object { Copy-OrdinaryFile ([IO.Path]::GetRelativePath($root, $_.FullName)) }
}
Push-Location $root
try {
    if ($DesktopSmoke -and -not $IsWindows) { throw '-DesktopSmoke requires an interactive Windows desktop.' }
    New-Item -ItemType Directory -Force $results, (Join-Path $sandbox 'artifacts/packages') | Out-Null
    Invoke-Dotnet @('test', 'tests/Avalonia.Material3.Tests/Avalonia.Material3.Tests.csproj', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $results)
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', (Join-Path $sandbox 'artifacts/packages'))
    # No root listing, repository NuGet.Config, credential data or .git content is read/copied.
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-OrdinaryFile $file }
    Copy-OrdinaryTree 'samples/Gallery/Pages'
    foreach ($sample in 'Gallery', 'StandaloneHost') {
        foreach ($file in "$sample.csproj", 'Program.cs', 'App.axaml', 'App.axaml.cs', 'MainWindow.axaml', 'MainWindow.axaml.cs') { Copy-OrdinaryFile "samples/$sample/$file" }
    }
    foreach ($sample in 'TextFieldHost', 'SearchHost') {
        foreach ($file in "$sample.csproj", 'Program.cs') { Copy-OrdinaryFile "samples/$sample/$file" }
    }
    Copy-OrdinaryTree 'tests/PackageConsumption.Tests'
    Copy-OrdinaryTree 'tests/ReferenceVectors'
    Get-ChildItem (Join-Path $root 'tests/Avalonia.Material3.Tests') -Filter '*.cs' -File |
        ForEach-Object { Copy-OrdinaryFile "tests/Avalonia.Material3.Tests/$($_.Name)" }
    $feed = (Join-Path $sandbox 'artifacts/packages') -replace '\\', '/'
    $sourceConfig = Join-Path $sandbox 'public-sources.config'
    # A newly generated, credential-free consumer-only source declaration; existing configs remain untouched.
    @"
<configuration>
  <packageSources><clear /><add key="ticket" value="$feed" /><add key="public" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><packageSource key="ticket"><package pattern="Avalonia.Material3" /></packageSource><packageSource key="public"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
"@ | Set-Content $sourceConfig
    $env:NUGET_PACKAGES = Join-Path $sandbox 'packages'
    $env:M3_ISSUE11_SCREENSHOTS = Join-Path $root 'artifacts/screenshots/issue11'
    if (Test-Path (Join-Path $sandbox 'src')) { throw 'Isolation failure: library source was copied.' }
    Write-Host "Isolated issue #11 package consumer (no src): $sandbox"
    $project = Join-Path $sandbox 'tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj'
    Invoke-Dotnet @('restore', $project, '--configfile', $sourceConfig)
    Invoke-Dotnet @('test', $project, '-c', 'Release', '--no-restore', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $results)
    $hostProject = Join-Path $sandbox 'samples/SearchHost/SearchHost.csproj'
    Invoke-Dotnet @('restore', $hostProject, '--configfile', $sourceConfig)
    Invoke-Dotnet @('build', $hostProject, '-c', 'Release', '--no-restore')
    if ($DesktopSmoke) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'windows-search-smoke.ps1') -Executable (Join-Path $sandbox 'samples/SearchHost/bin/Release/net10.0/SearchHost.exe') -Screenshots $env:M3_ISSUE11_SCREENSHOTS
        if ($LASTEXITCODE -ne 0) { throw "Windows search smoke failed ($LASTEXITCODE)." }
    }
    Write-Host 'PASS: source, fresh versioned package, actual compiled Gallery page, full linked suite and independent SearchHost.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:M3_ISSUE11_SCREENSHOTS = $previousScreenshots
    $env:MSBUILDDISABLENODEREUSE = $previousNodeReuse
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
