#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue17 -HostName AppChromeHost -SmokeScript windows-appchrome-smoke.ps1 -ScreenshotVariable M3_ISSUE17_SCREENSHOTS @PSBoundParameters
