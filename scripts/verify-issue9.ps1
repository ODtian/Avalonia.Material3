#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue9 -HostName FloatingActionsHost -SmokeScript windows-floating-actions-smoke.ps1 -ScreenshotVariable M3_ISSUE9_SCREENSHOTS @PSBoundParameters
