#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue16 -HostName SheetsHost -SmokeScript windows-sheets-smoke.ps1 -ScreenshotVariable M3_ISSUE16_SCREENSHOTS @PSBoundParameters
