#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue13 -HostName DialogsHost -SmokeScript windows-dialogs-smoke.ps1 -ScreenshotVariable M3_ISSUE13_SCREENSHOTS @PSBoundParameters
