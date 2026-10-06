param([Parameter(Mandatory = $true)][string]$Executable, [Parameter(Mandatory = $true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class AppChromeSmokeNative {
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
        if ($process.HasExited) { throw 'AppChromeHost exited before opening.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'package host window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    [AppChromeSmokeNative]::SetForegroundWindow($handle) | Out-Null
    $drawer = Wait-For { Find-Id $window 'chrome-drawer' } 'standard drawer'
    $library = Wait-For { Find-Id $window 'chrome-destination-library' } 'library'
    $homeItem = Wait-For { Find-Id $window 'chrome-destination-home' } 'home'
    $status = Wait-For { Find-Id $window 'chrome-status' } 'page status'
    $nav = Wait-For { Find-Id $window 'chrome-navigation' } 'navigation button'
    $top = Wait-For { Find-Id $window 'chrome-top' } 'top app bar'
    $bottom = Wait-For { Find-Id $window 'chrome-bottom' } 'bottom app bar'
    if ($drawer.Current.ControlType -ne [System.Windows.Automation.ControlType]::List -or $library.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) { throw 'Wrong destination roles.' }
    if ($top.Current.ControlType -ne [System.Windows.Automation.ControlType]::ToolBar -or $bottom.Current.ControlType -ne [System.Windows.Automation.ControlType]::ToolBar) { throw 'App bars must be action toolbars, not navigation selectors.' }
    $selection = $drawer.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern)
    if ($selection.Current.CanSelectMultiple -or -not $selection.Current.IsSelectionRequired) { throw 'Wrong drawer selection policy.' }
    $homeItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $homeItem.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{DOWN}')
    Wait-For { $library.Current.HasKeyboardFocus } 'focus-only drawer arrow' | Out-Null
    if (-not $homeItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected) { throw 'Keyboard highlighting navigated prematurely.' }
    [System.Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $status.Current.Name -match 'Page = Library' } 'Space destination activation and visible page'
    Invoke-Element (Find-Id $window 'chrome-save')
    Wait-For { $status.Current.Name -match 'Library saved' } 'page action result' | Out-Null
    Invoke-Element (Find-Id $window 'chrome-details')
    Wait-For { $status.Current.Name -match 'Page = Details' -and $nav.Current.Name -eq 'Return to collection' } 'detail navigation/title/back action' | Out-Null
    $nav.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Wait-For { $status.Current.Name -match 'Page = Library' -and $status.Current.Name -match 'Library saved' } 'Enter return retaining host result' | Out-Null
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Save-Window $window 'm3-16-desktop-standard-library.png'
    $transform = $window.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern)
    $transform.Resize(600, 760)
    Wait-For { -not (Find-Id $window 'chrome-drawer') } 'narrow host chooses closed modal drawer' | Out-Null
    $nav = Wait-For { Find-Id $window 'chrome-navigation' } 'new reachable menu button'
    $nav.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    $drawer = Wait-For { Find-Id $window 'chrome-drawer' } 'modal drawer'
    if ($drawer.Current.ItemStatus -ne 'modal-open') { throw "Wrong modal state: $($drawer.Current.ItemStatus)" }
    $homeItem = Wait-For { Find-Id $window 'chrome-destination-home' } 'modal initial focus'
    Wait-For { $homeItem.Current.HasKeyboardFocus } 'modal initial focus' | Out-Null
    if ($nav.Current.IsEnabled) { throw 'Background input is not gated.' }
    $rejected = $false
    try { Invoke-Element $nav } catch { $rejected = $true }
    if (-not $rejected) { throw 'Cached background Invoke was accepted.' }
    [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
    $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
    if ($focused.Current.AutomationId -eq 'chrome-navigation') { throw 'Tab escaped the modal drawer.' }
    Invoke-Element (Find-Id $window 'chrome-modal')
    Wait-For { Find-Id $window 'chrome-dialog-back' } 'nested dialog above drawer' | Out-Null
    Invoke-Element (Find-Id $window 'chrome-dialog-back')
    Wait-For { (Find-Id $window 'chrome-drawer') -and -not (Find-Id $window 'chrome-dialog-back') } 'back closes top dialog, preserving drawer' | Out-Null
    Save-Window $window 'm3-16-desktop-modal.png'
    [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
    Wait-For { -not (Find-Id $window 'chrome-drawer') -and $nav.Current.HasKeyboardFocus } 'Escape closes drawer and returns focus' | Out-Null
    $rect = $nav.Current.BoundingRectangle
    [AppChromeSmokeNative]::SetCursorPos([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2)) | Out-Null
    [AppChromeSmokeNative]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    [AppChromeSmokeNative]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    Wait-For { Find-Id $window 'chrome-drawer' } 'native mouse opens drawer' | Out-Null
    Invoke-Element (Find-Id $window 'chrome-destination-library')
    Wait-For { -not (Find-Id $window 'chrome-drawer') -and $status.Current.Name -match 'Page = Library' } 'destination closes modal before actual page navigation' | Out-Null
    Invoke-Element (Find-Id $window 'chrome-theme')
    Invoke-Element (Find-Id $window 'chrome-font')
    Invoke-Element (Find-Id $window 'chrome-long')
    Save-Window $window 'm3-16-desktop-dark-font200-long.png'
    for ($i = 0; $i -lt 8; $i++) { Invoke-Element (Find-Id $window 'chrome-recipe'); Start-Sleep -Milliseconds 100 }
    $transform.Resize(1100, 760)
    Wait-For { (Find-Id $window 'chrome-drawer').Current.ItemStatus -eq 'standard-open' } 'wide persistent drawer selection retained' | Out-Null
    if (-not (Find-Id $window 'chrome-destination-library').GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected) { throw 'Adaptive selection was lost.' }
    Save-Window $window 'm3-16-desktop-wide-font200.png'
    Write-Host 'PASS native Windows: package Gallery routing/results, Toolbar/List/Selection/ExpandCollapse, focus-only arrows, Space/Enter, modal gating and cached Invoke rejection, nested back/Escape/focus return, native mouse, resize selection, all recipes, dark/200%/long.'
}
finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
