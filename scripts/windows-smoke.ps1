param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$Screenshots
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SmokeWindow {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr window);
}
'@

function Wait-For {
    param([scriptblock]$Condition, [string]$Description)
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 15) {
        $value = & $Condition
        if ($value) { return $value }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out: $Description"
}

function Find-By {
    param($Window, $Property, [string]$Value)
    $condition = New-Object System.Windows.Automation.PropertyCondition($Property, $Value)
    $Window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Save-Window {
    param($Window, [string]$Path)
    $rect = $Window.Current.BoundingRectangle
    $bitmap = New-Object System.Drawing.Bitmap([int]$rect.Width, [int]$rect.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bitmap.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}

$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For { $process.Refresh(); if ($process.HasExited) { throw 'Host exited before creating a window.' }; if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle } } 'desktop window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    [SmokeWindow]::SetForegroundWindow($handle) | Out-Null
    $id = [System.Windows.Automation.AutomationElement]::AutomationIdProperty
    $name = [System.Windows.Automation.AutomationElement]::NameProperty
    $action = Wait-For { Find-By $window $id 'ActionButton' } 'action button'
    $theme = Wait-For { Find-By $window $id 'ThemeButton' } 'theme button'
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    $sample = [System.IO.Path]::GetFileNameWithoutExtension($Executable)
    Start-Sleep -Milliseconds 300
    Save-Window $window (Join-Path $Screenshots "$sample-light.png")

    $invoke = $action.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    Wait-For { Find-By $window $name 'Action completed (1)' } 'action feedback' | Out-Null
    $action.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
    Wait-For { $theme.Current.HasKeyboardFocus } 'Tab focus on theme button' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $theme.Current.Name -eq 'Use light theme' } 'Space switches to dark theme' | Out-Null
    if (-not (Find-By $window $name 'Action completed (1)')) { throw 'Theme change lost action feedback.' }
    Start-Sleep -Milliseconds 300
    Save-Window $window (Join-Path $Screenshots "$sample-dark.png")
    Write-Host "PASS desktop $sample`: startup, automation click, feedback, Tab focus, Space theme switch."
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { $process.Kill() }
    }
    $process.Dispose()
}
