#requires -Version 7.2
param([switch]$DesktopSmoke, [switch]$KeepSandbox, [string]$BaselinePackage, [string]$Manifest)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/consumer.ps1"
$root = Split-Path $PSScriptRoot -Parent
# Reject bad requested archives before any source test/copy/pack work.
if ($BaselinePackage) { Assert-ImmutableBaselineArchive $BaselinePackage }
# An existing exact-HEAD gate may be reused by component entrypoints/publishing, never a stale/global pack.
if ($Manifest) {
    $verified = Get-VerifiedConsumerManifest $root $Manifest
    if ($BaselinePackage) { Assert-SdkBaselineReuse $verified $BaselinePackage }
    if ($DesktopSmoke) { throw 'Use component/published smoke with the verified manifest, not -DesktopSmoke reuse.' }
    Write-Host "PASS revalidated source/package/Ordinal/exact artifact gate: $Manifest"
    Write-Output $verified
    return
}
$run = Join-Path $root ('artifacts/r-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$sandbox = Join-Path $run 'consumer'
$oldPackages = $env:NUGET_PACKAGES
New-Item -ItemType Directory -Force "$run/packages", "$run/results" | Out-Null
Push-Location $root
try {
    $version = [string](([xml](Get-Content Directory.Build.props -Raw)).Project.PropertyGroup.Material3Version)
    $commit = (git rev-parse HEAD).Trim()
    Invoke-CheckedDotnet test tests/Avalonia.Material3.Tests -c Release --logger 'trx;LogFileName=source.trx' --results-directory "$run/results" | Tee-Object "$run/source.log"
    $packArgs = @('pack', 'src/Avalonia.Material3/Avalonia.Material3.csproj', '-c', 'Release', '-o', "$run/packages", "-p:RepositoryCommit=$commit", "-bl:$run/package-api.binlog")
    if ($BaselinePackage) {
        $packArgs += '-p:EnablePackageValidation=true', "-p:PackageValidationBaselinePath=$BaselinePackage", '-p:ApiCompatEnableRuleCannotChangeParameterName=true'
    }
    Invoke-CheckedDotnet @packArgs | Tee-Object "$run/package-api.log"
    $package = "$run/packages/Avalonia.Material3.$version.nupkg"
    $hash = (Get-FileHash $package -Algorithm SHA256).Hash
    Assert-PackageIdentity $package $version $commit
    New-PackageConsumer $root $sandbox $package $hash
    $env:NUGET_PACKAGES = "$sandbox/packages"
    Invoke-CheckedDotnet test "$sandbox/tests/PackageConsumption.Tests" -c Release --logger 'trx;LogFileName=package.trx' --results-directory "$run/results" | Tee-Object "$run/package.log"
    Assert-ConsumerAssets $sandbox $version $package $hash
    Assert-ScenarioParity "$run/results/source.trx" "$run/results/package.trx" "$run/scenarios.json" "$root/tests/PackageOnlyScenarios.txt"
    $sdk = & dotnet --version
    # Written only AFTER the actual SDK pack/API gate succeeds with these explicit arguments.
    $sdkBaseline = if ($BaselinePackage) {
        @{ outcome='Passed'; baselineSha256=(Get-FileHash $BaselinePackage -Algorithm SHA256).Hash;
           enablePackageValidation=$true; cannotChangeParameterName=$true; commit=$commit; packageSha256=$hash; sdk=$sdk;
           log=@{ path="$run/package-api.log"; sha256=(Get-FileHash "$run/package-api.log" -Algorithm SHA256).Hash };
           binlog=@{ path="$run/package-api.binlog"; sha256=(Get-FileHash "$run/package-api.binlog" -Algorithm SHA256).Hash } }
    } else { $null }
    @{ commit=$commit; version=$version; package=$package; sha256=$hash; sandbox=$sandbox; sdk=$sdk; scenarioManifest="$run/scenarios.json"; baseline=$BaselinePackage; sdkBaseline=$sdkBaseline } | ConvertTo-Json -Depth 5 | Set-Content "$run/manifest.json" -Encoding utf8
    if ($DesktopSmoke) {
        if (!$IsWindows) { throw 'Native UIA requires Windows.' }
        foreach ($hostName in 'Gallery', 'StandaloneHost') {
            & powershell.exe -NoProfile -File "$PSScriptRoot/windows-gallery-smoke.ps1" -Executable "$sandbox/samples/$hostName/bin/Release/net10.0/$hostName.exe" -Evidence "$run/native-$hostName"
            if ($LASTEXITCODE -ne 0) { throw "Native UIA failed: $hostName" }
        }
    }
    Write-Host "PASS source/package exact identity parity: $run/manifest.json"
    Write-Output ([pscustomobject]@{ manifest="$run/manifest.json"; sandbox=$sandbox })
}
finally {
    $env:NUGET_PACKAGES = $oldPackages
    Stop-ConsumerCollectors $sandbox
    Pop-Location
    if (!$KeepSandbox -and (Test-Path $sandbox)) { Remove-Item $sandbox -Recurse -Force }
    Write-Host "Evidence retained: $run"
}
