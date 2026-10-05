#requires -Version 7.2
param([switch]$KeepSandbox)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$evidence = Join-Path $root 'artifacts/issue-7'
$sandbox = Join-Path ([IO.Path]::GetTempPath()) ('m3-content-consumer-' + [guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
function Invoke-Dotnet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    $Arguments += '--disable-build-servers'
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}
function Copy-ConsumerTree([string]$relativePath) {
    Get-ChildItem (Join-Path $root $relativePath) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($root, $_.FullName)
            $destination = Join-Path $sandbox $relative
            New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
            Copy-Item $_.FullName $destination
        }
}
Push-Location $root
try {
    New-Item -ItemType Directory -Force $evidence, "$sandbox/artifacts/packages", "$sandbox/tests/Avalonia.Material3.Tests" | Out-Null
    Invoke-Dotnet @('test', 'tests/Avalonia.Material3.Tests', '-c', 'Release', '--logger', 'trx;LogFileName=source.trx', '--results-directory', $evidence)
    # Never reuse an existing package/cache: pack straight into this consumer's fresh feed.
    Invoke-Dotnet @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', "$sandbox/artifacts/packages")
    foreach ($file in 'Directory.Build.props', 'global.json') { Copy-Item (Join-Path $root $file) $sandbox }
    # A credential-free config: only this freshly packed library feed and public dependencies.
    # Reuse official dependency archives as an offline feed, never the Material3 package/cache.
    New-Item -ItemType Directory -Force "$sandbox/dependencies" | Out-Null
    Get-ChildItem "$root/artifacts/nuget" -Filter '*.nupkg' -Recurse -File |
        Where-Object { $_.Name -notlike 'avalonia.material3.*' } |
        ForEach-Object { Copy-Item $_.FullName "$sandbox/dependencies" -Force }
    @'
<configuration>
  <packageSources>
    <clear />
    <add key="fresh-material3" value="artifacts/packages" />
    <add key="dependencies" value="dependencies" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="fresh-material3"><package pattern="Avalonia.Material3" /></packageSource>
    <packageSource key="dependencies"><package pattern="*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
'@ | Set-Content "$sandbox/NuGet.Config" -Encoding utf8
    Copy-ConsumerTree 'samples'
    Copy-ConsumerTree 'tests/PackageConsumption.Tests'
    Copy-ConsumerTree 'tests/ReferenceVectors'
    # Supply linked scenario sources without changing the package project's explicit Compile contract.
    Get-ChildItem "$root/tests/Avalonia.Material3.Tests" -Filter '*.cs' -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
            $relative = [IO.Path]::GetRelativePath("$root/tests/Avalonia.Material3.Tests", $_.FullName)
            $destination = Join-Path "$sandbox/tests/Avalonia.Material3.Tests" $relative
            New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
            Copy-Item $_.FullName $destination
        }
    if (Test-Path "$sandbox/src") { throw 'Isolated consumer must not contain library source.' }
    $env:NUGET_PACKAGES = "$sandbox/packages"
    Write-Host "Isolated content package consumer: $sandbox"
    Invoke-Dotnet @('test', "$sandbox/tests/PackageConsumption.Tests/PackageConsumption.Tests.csproj", '-c', 'Release', '--logger', 'trx;LogFileName=package.trx', '--results-directory', $evidence)
    $package = Get-ChildItem "$sandbox/artifacts/packages/Avalonia.Material3.*.nupkg" | Select-Object -First 1
    Copy-Item $package.FullName $evidence -Force
    Get-FileHash $package.FullName -Algorithm SHA256 | Format-List
    Write-Host 'PASS: content scenarios and gallery page consume a freshly packed version in a source-free directory and new cache.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    Pop-Location
    if ($KeepSandbox) { Write-Host "Sandbox kept: $sandbox" }
    elseif (Test-Path $sandbox) {
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            try { Remove-Item $sandbox -Recurse -Force -ErrorAction Stop; break }
            catch { if ($attempt -eq 19) { throw }; Start-Sleep -Milliseconds 500 }
        }
    }
}
