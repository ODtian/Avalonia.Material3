#requires -Version 7.2
param([switch]$KeepSandbox, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
if (!$Evidence) { $Evidence = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/screenshots' }
& "$PSScriptRoot/verify-component.ps1" -Component m3-06 -HostName SelectionDemo -SmokeScript windows-selection-smoke.ps1 -DesktopSmoke -Evidence $Evidence -KeepSandbox:$KeepSandbox -Manifest $Manifest -BaselinePackage $BaselinePackage
