#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue4 -HostName ButtonsHost -SmokeScript windows-buttons-smoke.ps1 -ScreenshotVariable M3_ISSUE4_SCREENSHOTS @PSBoundParameters
