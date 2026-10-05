param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$Screenshots
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ButtonsSmokeWindow {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
}
'@
function Wait-For([scriptblock]$condition, [string]$description) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 20) {
        $value = & $condition
        if ($value) { return $value }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out: $description"
}
function Find-Id($window, [string]$id) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Save-Window($window, [string]$name) {
    Start-Sleep -Milliseconds 300
    $rect = $window.Current.BoundingRectangle
    $bitmap = New-Object System.Drawing.Bitmap([int]$rect.Width, [int]$rect.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bitmap.Size)
        $bitmap.Save((Join-Path $Screenshots $name), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For {
        $process.Refresh()
        if ($process.HasExited) { throw 'ButtonsHost exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'desktop window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    [ButtonsSmokeWindow]::SetForegroundWindow($handle) | Out-Null
    $action = Wait-For { Find-Id $window 'ActionButton' } 'action'
    $favorite = Wait-For { Find-Id $window 'FavoriteButton' } 'favorite'
    $result = Wait-For { Find-Id $window 'Result' } 'result'
    $disable = Wait-For { Find-Id $window 'DisableButton' } 'disable'
    $theme = Wait-For { Find-Id $window 'ThemeButton' } 'theme'
    $font = Wait-For { Find-Id $window 'FontScaleButton' } 'font scale'
    if ($favorite.Current.Name -ne 'Favorite' -or $favorite.Current.ControlType -ne [System.Windows.Automation.ControlType]::Button) { throw 'Incorrect icon name/role.' }
    $toggle = $favorite.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    if ($toggle.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::Off) { throw 'Expected initially unselected icon.' }
    $action.SetFocus()
    Wait-For { $action.Current.HasKeyboardFocus } 'action focus' | Out-Null
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Save-Window $window 'm3-03-desktop-light.png'
    $action.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $result.Current.Name -eq 'Action completed (1): confirmed' } 'command feedback' | Out-Null
    $toggle.Toggle()
    Wait-For { $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On -and $result.Current.Name -eq 'Favorite: selected' } 'UIA selected state and feedback' | Out-Null
    $action.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
    Wait-For { $favorite.Current.HasKeyboardFocus } 'Tab to icon' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::Off -and $result.Current.Name -eq 'Favorite: unselected' } 'Space deselects' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Wait-For { $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On } 'Enter selects' | Out-Null
    $disable.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { -not $action.Current.IsEnabled -and -not $favorite.Current.IsEnabled } 'disabled action and icon semantics' | Out-Null
    if ($toggle.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) { throw 'Disabling lost selection.' }
    $disable.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $theme.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Save-Window $window 'm3-03-desktop-dark-selected.png'
    $font.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Save-Window $window 'm3-03-desktop-dark-font200.png'
    Write-Host 'PASS Windows desktop: package host, real UIA name/role/Toggle/disabled state, Invoke command result, Tab/Space/Enter and light/dark/font screenshots.'
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() }
    }
    $process.Dispose()
}
