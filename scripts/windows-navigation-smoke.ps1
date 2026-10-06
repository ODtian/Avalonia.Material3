param([Parameter(Mandatory = $true)][string]$Executable, [Parameter(Mandatory = $true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class NavigationSmokeNative {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
'@
function Wait-For([scriptblock]$condition, [string]$description) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 25) {
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
function Invoke-Element($element) { $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
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
        if ($process.HasExited) { throw 'NavigationHost exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'package host window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    [NavigationSmokeNative]::SetForegroundWindow($handle) | Out-Null
    $tabs = Wait-For { Find-Id $window 'navigation-main' } 'tabs'
    $homeItem = Wait-For { Find-Id $window 'navigation-main-0' } 'home'
    $library = Wait-For { Find-Id $window 'navigation-main-1' } 'library'
    $status = Wait-For { Find-Id $window 'navigation-status' } 'status'
    if ($tabs.Current.ControlType -ne [System.Windows.Automation.ControlType]::Tab -or $library.Current.ControlType -ne [System.Windows.Automation.ControlType]::TabItem) { throw 'Wrong native tab roles.' }
    if ($library.Current.Name -ne 'Library 资料, 3 unread') { throw "Wrong native label/badge name: $($library.Current.Name)" }
    $selection = $tabs.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern)
    $selected = $library.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    if ($selection.Current.CanSelectMultiple -or -not $selection.Current.IsSelectionRequired) { throw 'Wrong selection contract.' }
    $library.SetFocus()
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Save-Window $window 'm3-15-desktop-light.png'
    $selected.Select()
    Wait-For { $selected.Current.IsSelected -and $status.Current.Name -match 'selected index = 1' } 'native selection/content state' | Out-Null
    $content = Wait-For { Find-Id $window 'main-content-1' } 'selected real content'
    if ($content.Current.IsOffscreen -or $content.Current.Name -notmatch 'Library.*content') { throw 'Selected page is not visible actual content.' }
    $homeItem.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{RIGHT}')
    Wait-For { $library.Current.HasKeyboardFocus -and $selected.Current.IsSelected } 'arrow selection/focus' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $status.Current.Name -match 'invocation #1' } 'Space repeat invocation' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Wait-For { $status.Current.Name -match 'invocation #2' } 'Enter repeat invocation' | Out-Null
    $rect = $library.Current.BoundingRectangle
    [NavigationSmokeNative]::SetCursorPos([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2)) | Out-Null
    [NavigationSmokeNative]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    [NavigationSmokeNative]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    Wait-For { $status.Current.Name -match 'invocation #3' } 'native mouse invocation' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
    $read = Wait-For { Find-Id $window 'navigation-mark-read' } 'page action'
    Wait-For { $read.Current.HasKeyboardFocus } 'single header Tab exit into selected page' | Out-Null
    Invoke-Element $read
    Wait-For { $library.Current.Name -eq 'Library 资料' -and $status.Current.Name -match 'badge cleared' } 'live badge accessible name and content action' | Out-Null
    $invoke = $library.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    Invoke-Element (Find-Id $window 'navigation-disable')
    Wait-For { -not $library.Current.IsEnabled } 'disabled semantics' | Out-Null
    $blocked = $false
    try { $invoke.Invoke() } catch { $blocked = $true; Write-Host "Disabled native Invoke rejected: $($_.Exception.Message)" }
    if (-not $blocked -or -not $selected.Current.IsSelected) { throw 'Disabled invocation was not rejected or selection was lost.' }
    Invoke-Element (Find-Id $window 'navigation-disable')
    Invoke-Element (Find-Id $window 'navigation-mode')
    Save-Window $window 'm3-15-desktop-dark-library.png'
    $railLibrary = Wait-For { Find-Id $window 'navigation-responsive-1' } 'responsive rail library'
    if ($railLibrary.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) { throw 'Wrong native rail item role.' }
    $railSelected = $railLibrary.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $railSelected.Select()
    $railLibrary.SetFocus()
    $transform = $window.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern)
    $expandedPixels = $railLibrary.Current.BoundingRectangle.Width
    $transform.Resize(400, 900)
    Wait-For { $railLibrary.Current.HasKeyboardFocus -and $railSelected.Current.IsSelected -and -not $railLibrary.Current.IsOffscreen -and [Math]::Abs($railLibrary.Current.BoundingRectangle.Width - $expandedPixels * 96 / 220) -lt 2 } 'narrow collapsed rail retains focus/selection' | Out-Null
    Save-Window $window 'm3-15-desktop-narrow-focused-rail.png'
    $transform.Resize(1100, 900)
    Wait-For { $railLibrary.Current.HasKeyboardFocus -and $railSelected.Current.IsSelected -and -not $railLibrary.Current.IsOffscreen -and [Math]::Abs($railLibrary.Current.BoundingRectangle.Width - $expandedPixels) -lt 2 } 'wide expanded rail retains focus/selection' | Out-Null
    Invoke-Element (Find-Id $window 'navigation-font')
    Invoke-Element (Find-Id $window 'navigation-long')
    Save-Window $window 'm3-15-desktop-font200-long.png'
    Write-Host 'PASS native Windows: fresh package Gallery, Tab/ListItem names and badges, selection patterns, real content, arrows/Tab/Space/Enter, native mouse, disabled Invoke, resize focus/selection, light/dark/font/long screenshots.'
}
finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
