#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue10 -HostName ButtonGroupsHost -SmokeScript windows-button-groups-smoke.ps1 -ScreenshotVariable M3_ISSUE10_SCREENSHOTS @PSBoundParameters
