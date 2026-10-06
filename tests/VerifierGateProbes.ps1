# Public CLI probes. No private gate/helper calls, no simulated SDK-success metadata.
# NoBaselineManifest must come from an ACTUAL successful fresh verify.ps1 run WITHOUT -BaselinePackage.
# Both records must belong to the current clean producer. These probes reuse their exact consumers.
param(
    [Parameter(Mandatory=$true)][string]$Manifest,
    [Parameter(Mandatory=$true)][string]$NoBaselineManifest,
    [Parameter(Mandatory=$true)][string]$BaselinePackage,
    [string]$Root = (Split-Path $PSScriptRoot -Parent),
    [string]$Evidence
)
$ErrorActionPreference = 'Stop'
if (!$Evidence) { $Evidence = Join-Path $Root 'artifacts/baseline-reuse-probes' }
New-Item -ItemType Directory -Force $Evidence | Out-Null
$record = Get-Content $Manifest -Raw | ConvertFrom-Json
$without = Get-Content $NoBaselineManifest -Raw | ConvertFrom-Json
if ($without.baseline -or $without.sdkBaseline) { throw 'The negative fixture must actually lack SDK baseline evidence.' }
function Invoke-Probe([string]$Name, [string]$Entry, [string[]]$Arguments, [bool]$ShouldPass, [string]$Reason) {
    $output = & pwsh -NoProfile -File "$Root/scripts/$Entry" @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | Set-Content (Join-Path $Evidence "$Name.log") -Encoding utf8
    if (($exitCode -eq 0) -ne $ShouldPass -or (!$ShouldPass -and ($output | Out-String) -notmatch $Reason)) {
        throw "Public CLI probe $Name had unexpected exit/reason: $exitCode; $($output | Out-String)"
    }
    Write-Host "PASS public CLI probe $Name (exit=$exitCode)"
}
Invoke-Probe 'missing-archive' 'verify.ps1' @('-Manifest',$Manifest,'-BaselinePackage',(Join-Path $Evidence 'nonexistent-preview1.nupkg')) $false 'baseline archive missing or hash mismatch'
Invoke-Probe 'wrong-archive' 'verify-theme.ps1' @('-Manifest',$Manifest,'-BaselinePackage',$record.package) $false 'baseline archive missing or hash mismatch'
Invoke-Probe 'actual-no-sdk-baseline' 'verify.ps1' @('-Manifest',$NoBaselineManifest,'-BaselinePackage',$BaselinePackage) $false 'lacks the requested successful SDK baseline dimension'
Invoke-Probe 'actual-no-sdk-baseline-forwarded' 'verify-theme.ps1' @('-Manifest',$NoBaselineManifest,'-BaselinePackage',$BaselinePackage) $false 'lacks the requested successful SDK baseline dimension'
Invoke-Probe 'valid-sdk-baseline' 'verify.ps1' @('-Manifest',$Manifest,'-BaselinePackage',$BaselinePackage) $true ''
Invoke-Probe 'valid-sdk-baseline-forwarded' 'verify-theme.ps1' @('-Manifest',$Manifest,'-BaselinePackage',$BaselinePackage) $true ''
@{ producer=$record.commit; manifest=$Manifest; noBaselineManifest=$NoBaselineManifest; baseline=$BaselinePackage;
   probes=6; passed=6; noSimulatedSdkProof=$true } | ConvertTo-Json | Set-Content (Join-Path $Evidence 'probes.json') -Encoding utf8
