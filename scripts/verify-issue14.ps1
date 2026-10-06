#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue14 -HostName NavigationHost -SmokeScript windows-navigation-smoke.ps1 -ScreenshotVariable M3_ISSUE14_SCREENSHOTS @PSBoundParameters
