#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue18 -HostName PickersHost -SmokeScript windows-pickers-smoke.ps1 -ScreenshotVariable M3_ISSUE18_SCREENSHOTS @PSBoundParameters
