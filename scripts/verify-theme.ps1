#requires -Version 7.2
param([switch]$KeepSandbox, [string]$Manifest, [string]$BaselinePackage)
& "$PSScriptRoot/verify-component.ps1" -Component theme @PSBoundParameters
