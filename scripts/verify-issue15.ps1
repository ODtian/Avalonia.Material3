#requires -Version 7.2
param([switch]$KeepSandbox, [switch]$DesktopSmoke, [string]$Manifest, [string]$BaselinePackage, [string]$Evidence)
& "$PSScriptRoot/verify-component.ps1" -Component issue15 -HostName SecondaryFeedbackHost -SmokeScript windows-feedback-smoke.ps1 -ScreenshotVariable M3_ISSUE15_SCREENSHOTS @PSBoundParameters
