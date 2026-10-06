#requires -Version 7.2
param([switch]$DesktopSmoke, [switch]$KeepSandbox, [string]$BaselinePackage)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/consumer.ps1"
$root = Split-Path $PSScriptRoot -Parent
$run = Join-Path $root ('artifacts/release-' + [guid]::NewGuid().ToString('N'))
$sandbox = Join-Path $run 'consumer'
$oldPackages = $env:NUGET_PACKAGES
New-Item -ItemType Directory -Force "$run/packages", "$run/results" | Out-Null
Push-Location $root
try {
    $version = [string](([xml](Get-Content Directory.Build.props -Raw)).Project.PropertyGroup.Material3Version)
    $commit = (git rev-parse HEAD).Trim()
    Invoke-CheckedDotnet test tests/Avalonia.Material3.Tests -c Release --logger 'trx;LogFileName=source.trx' --results-directory "$run/results"
    $packArgs = @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', "$run/packages", "-p:RepositoryCommit=$commit")
    if ($BaselinePackage) {
        if ((Get-FileHash $BaselinePackage -Algorithm SHA256).Hash -ne 'C52D2E60A9E508ACF7EA1915EFBD5A84E7508AD39D8662453793759AC44174B6') { throw 'Immutable preview.1 baseline hash mismatch.' }
        $packArgs += '-p:EnablePackageValidation=true', "-p:PackageValidationBaselinePath=$BaselinePackage", '-p:ApiCompatEnableRuleCannotChangeParameterName=true'
    }
    Invoke-CheckedDotnet @packArgs
    $package = "$run/packages/Avalonia.Material3.$version.nupkg"
    $hash = (Get-FileHash $package -Algorithm SHA256).Hash
    Assert-PackageIdentity $package $version $commit
    New-PackageConsumer $root $sandbox $package $hash
    $env:NUGET_PACKAGES = "$sandbox/packages"
    Invoke-CheckedDotnet test "$sandbox/tests/PackageConsumption.Tests" -c Release --logger 'trx;LogFileName=package.trx' --results-directory "$run/results"
    Assert-ConsumerAssets $sandbox $version $package $hash
    Assert-ScenarioParity "$run/results/source.trx" "$run/results/package.trx" "$run/scenarios.json" "$root/tests/PackageOnlyScenarios.txt"
    @{ commit=$commit; version=$version; package=$package; sha256=$hash; sandbox=$sandbox; sdk=(& dotnet --version); scenarioManifest="$run/scenarios.json"; baseline=$BaselinePackage } | ConvertTo-Json | Set-Content "$run/manifest.json" -Encoding utf8
    if ($DesktopSmoke) {
        if (!$IsWindows) { throw 'Native UIA requires Windows.' }
        foreach ($hostName in 'Gallery', 'StandaloneHost') {
            & powershell.exe -NoProfile -File "$PSScriptRoot/windows-gallery-smoke.ps1" -Executable "$sandbox/samples/$hostName/bin/Release/net10.0/$hostName.exe" -Evidence "$run/native-$hostName"
            if ($LASTEXITCODE -ne 0) { throw "Native UIA failed: $hostName" }
        }
    }
    Write-Host "PASS source/package exact identity parity: $run/manifest.json"
}
finally {
    $env:NUGET_PACKAGES = $oldPackages
    Stop-ConsumerCollectors $sandbox
    Pop-Location
    if (!$KeepSandbox -and (Test-Path $sandbox)) { Remove-Item $sandbox -Recurse -Force }
    Write-Host "Evidence retained: $run"
}
