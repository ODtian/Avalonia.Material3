#requires -Version 7.2
param([switch]$KeepSandbox)
# Historical entry point delegates to the authoritative source/package/inventory gate.
& "$PSScriptRoot/verify.ps1" -KeepSandbox:$KeepSandbox
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
