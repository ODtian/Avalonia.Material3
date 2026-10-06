#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue12 -HostName ProgressHost -SmokeScript windows-progress-smoke.ps1 -ScreenshotVariable M3_ISSUE12_SCREENSHOTS @PSBoundParameters
