#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue11 -HostName SearchHost -SmokeScript windows-search-smoke.ps1 -ScreenshotVariable M3_ISSUE11_SCREENSHOTS @PSBoundParameters
