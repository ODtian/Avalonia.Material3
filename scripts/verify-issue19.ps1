#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue19 -HostName BrowseHost -SmokeScript windows-browse-smoke.ps1 -ScreenshotVariable M3_ISSUE19_SCREENSHOTS @PSBoundParameters
