#requires -Version 7.2
param(
    [Parameter(Mandatory=$true)][string]$Component,
    [string]$HostName, [string]$SmokeScript, [string]$ScreenshotVariable,
    [switch]$DesktopSmoke, [switch]$KeepSandbox,
    [string]$Manifest, [string]$BaselinePackage, [string]$Evidence
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/consumer.ps1"
$root = Split-Path $PSScriptRoot -Parent
if ($DesktopSmoke -and (!$IsWindows -or !$SmokeScript)) { throw 'This component smoke requires Windows and its native verifier.' }
if (!$Evidence) { $Evidence = Join-Path $root "artifacts/screenshots/$Component" }
$oldPackages = $env:NUGET_PACKAGES
$oldScreenshots = if ($ScreenshotVariable) { [Environment]::GetEnvironmentVariable($ScreenshotVariable) }
$created = !$Manifest
$verified = $null
try {
    if ($ScreenshotVariable) { [Environment]::SetEnvironmentVariable($ScreenshotVariable, $Evidence) }
    # ONE authoritative source/package tree and inventory; no per-component copy lists/feed/cache.
    & "$PSScriptRoot/verify.ps1" -KeepSandbox -Manifest $Manifest -BaselinePackage $BaselinePackage | ForEach-Object {
        if ($_ -is [pscustomobject] -and $_.manifest) { $verified = $_ } else { Write-Host $_ }
    }
    if (!$verified) { throw 'Authoritative consumer gate did not produce a manifest.' }
    $record = Get-VerifiedConsumerManifest $root $verified.manifest
    $env:NUGET_PACKAGES = "$($record.sandbox)/packages"
    if ($HostName) {
        Invoke-CheckedDotnet build "$($record.sandbox)/samples/$HostName/$HostName.csproj" -c Release
        Assert-ConsumerAssets $record.sandbox $record.version $record.package $record.sha256
    }
    if ($DesktopSmoke) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/$SmokeScript" `
            -Executable "$($record.sandbox)/samples/$HostName/bin/Release/net10.0/$HostName.exe" -Screenshots $Evidence
        if ($LASTEXITCODE -ne 0) { throw "Native component smoke failed: $Component ($LASTEXITCODE)." }
    }
    Write-Host "PASS $Component authoritative gate; native smoke executed=$([bool]$DesktopSmoke); manifest=$($verified.manifest)"
}
finally {
    $env:NUGET_PACKAGES = $oldPackages
    if ($ScreenshotVariable) { [Environment]::SetEnvironmentVariable($ScreenshotVariable, $oldScreenshots) }
    if ($verified) {
        Stop-ConsumerCollectors $verified.sandbox
        # Reused exact manifest is caller-owned: never remove its consumer/cache.
        if ($created -and !$KeepSandbox -and (Test-Path $verified.sandbox)) { Remove-Item $verified.sandbox -Recurse -Force }
    }
}
