param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class M3DialogNative {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr insert, int x, int y, int cx, int cy, uint flags);
}
'@
function Wait-For([scriptblock]$condition, [string]$description) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.Elapsed.TotalSeconds -lt 20) {
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
function Find-Name($window, [string]$name) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Invoke-Control($control) { $control.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Focus-Id {
    try { [System.Windows.Automation.AutomationElement]::FocusedElement.Current.AutomationId }
    catch [System.Runtime.InteropServices.COMException] { return $null }
}
function Keys([string]$text) { [System.Windows.Forms.SendKeys]::SendWait($text); Start-Sleep -Milliseconds 150 }
function Mouse-Click($control) {
    $rect = $control.Current.BoundingRectangle
    [M3DialogNative]::SetCursorPos([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2)) | Out-Null
    [M3DialogNative]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
    [M3DialogNative]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
}
function Save-Window($window, [string]$name) {
    Start-Sleep -Milliseconds 200
    $rect = $window.Current.BoundingRectangle
    $bounds = [System.Drawing.Rectangle]::FromLTRB([int]$rect.X, [int]$rect.Y, [int]$rect.Right, [int]$rect.Bottom)
    $visible = [System.Drawing.Rectangle]::Intersect($bounds, [System.Windows.Forms.Screen]::FromHandle($process.MainWindowHandle).WorkingArea)
    $bitmap = New-Object System.Drawing.Bitmap($visible.Width, $visible.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try { $graphics.CopyFromScreen($visible.X, $visible.Y, 0, 0, $bitmap.Size); $bitmap.Save((Join-Path $Screenshots $name), [System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For {
        $process.Refresh()
        if ($process.HasExited) { throw 'DialogsHost exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'desktop window'
    [M3DialogNative]::SetForegroundWindow($handle) | Out-Null
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    $basic = Wait-For { Find-Id $window 'BasicEntry' } 'basic entry'
    $full = Find-Id $window 'FullScreenEntry'
    $result = Find-Id $window 'DialogResult'
    Invoke-Control $basic
    $dialog = Wait-For { Find-Name $window 'Edit details' } 'basic dialog'
    if ($dialog.Current.ControlType -ne [System.Windows.Automation.ControlType]::Window -or $dialog.Current.ItemStatus -ne 'Modal dialog open') { throw 'Dialog must expose a named Window role and modal open status.' }
    if ($basic.Current.IsEnabled -or (Find-Id $window 'BasicEntry')) { throw 'Modal background is enabled or exposed in native automation navigation.' }
    $rejected = $false
    try { Invoke-Control $basic } catch { $rejected = $true }
    if (-not $rejected) { throw 'Cached native Invoke bypassed modality.' }
    $editor = Find-Id $window 'DialogEditor'
    $value = $editor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    Wait-For { (Focus-Id) -eq 'DialogEditor' } 'initial native editor focus' | Out-Null
    $value.SetValue('')
    Invoke-Control (Find-Name $dialog 'Save')
    if (-not (Find-Name $window 'Edit details') -or $result.Current.Name -ne 'Waiting') { throw 'Validation should keep the dialog open and result unchanged.' }
    [M3DialogNative]::SetForegroundWindow($handle) | Out-Null
    # Enter commits the current native IME preedit if the desktop uses one. It must not save the form.
    # Use one Latin word, avoiding layout-dependent conversion of a Space into candidate acceptance.
    $editor.SetFocus(); Keys '^a'; Keys 'updated'; Keys '{ENTER}'
    Wait-For { $value.Current.Value -eq 'updated' } 'native text editing and preedit commit' | Out-Null
    if (-not (Find-Name $window 'Edit details')) { throw 'Enter in the editor implicitly submitted the dialog.' }
    Save-Window $window 'm3-12-desktop-basic-edit.png'
    Keys '{TAB}'; Wait-For { (Focus-Id) -eq 'PART_CancelButton' } 'Tab to cancel' | Out-Null
    Keys '{TAB}'; Wait-For { (Focus-Id) -eq 'PART_ConfirmButton' } 'Tab to confirm' | Out-Null
    Keys '{TAB}'; Wait-For { (Focus-Id) -eq 'DialogEditor' } 'forward modal focus wrap' | Out-Null
    Keys '+{TAB}'; Wait-For { (Focus-Id) -eq 'PART_ConfirmButton' } 'reverse modal focus wrap' | Out-Null
    Keys '{ENTER}'
    Wait-For { $result.Current.Name -eq 'Confirmed: updated' } 'confirmed host result' | Out-Null
    Wait-For { (Focus-Id) -eq 'BasicEntry' } 'focus return to basic entry' | Out-Null
    Invoke-Control (Find-Id $window 'DialogTheme')
    Mouse-Click $full
    Wait-For { Find-Name $window 'Edit details' } 'full-screen dialog' | Out-Null
    Save-Window $window 'm3-12-desktop-fullscreen-dark.png'
    Keys '{ESC}'
    Wait-For { $result.Current.Name -eq 'Escape' -and (Focus-Id) -eq 'FullScreenEntry' } 'Escape and full-screen entry focus return' | Out-Null
    Invoke-Control (Find-Id $window 'DialogFont')
    Invoke-Control $full
    Wait-For { Find-Name $window 'Edit details' } 'scaled full-screen dialog' | Out-Null
    [M3DialogNative]::SetWindowPos($handle, [IntPtr]::Zero, 80, 60, 360, 600, 0x0040) | Out-Null
    Save-Window $window 'm3-12-desktop-fullscreen-dark-font200-narrow.png'
    $cancel = Find-Name (Find-Name $window 'Edit details') 'Cancel'
    Mouse-Click $cancel
    Wait-For { $result.Current.Name -eq 'Cancelled' } 'native mouse cancel result' | Out-Null
    [M3DialogNative]::SetWindowPos($handle, [IntPtr]::Zero, 60, 40, 900, 700, 0x0040) | Out-Null
    Invoke-Control (Find-Id $window 'DialogFont')
    Invoke-Control (Find-Id $window 'NestedEntry')
    Invoke-Control (Wait-For { Find-Id $window 'NestedLauncher' } 'nested launcher')
    Wait-For { Find-Name $window 'Nested confirmation' } 'nested dialog' | Out-Null
    Keys '{ESC}'
    Wait-For { Find-Name $window 'Outer dialog' } 'outer dialog still open' | Out-Null
    if (Find-Name $window 'Nested confirmation') { throw 'Escape did not remove only the top dialog.' }
    Invoke-Control (Find-Id $window 'ModalBack')
    Wait-For { $result.Current.Name -eq 'Back' -and (Focus-Id) -eq 'NestedEntry' } 'host back and nested stack focus return' | Out-Null
    Invoke-Control (Find-Id $window 'PopupEntry')
    Invoke-Control (Wait-For { Find-Id $window 'PopupAction' } 'generic anchored overlay')
    Wait-For { $result.Current.Name -eq 'Confirmed: Sample selected' -and (Focus-Id) -eq 'PopupEntry' } 'generic overlay result/focus return' | Out-Null
    Write-Host 'PASS Windows native package host: named dialog Window role, filtered/disabled background and rejected cached Invoke, native editor+validation, Tab/Shift+Tab containment, Enter confirm/result/focus return, full-screen mouse opening/cancel and Escape, nested top-only Escape/host back, anchored overlay, light/dark/scaled resize screenshots.'
}
catch {
    Write-Host $_.ScriptStackTrace
    if ($window) { Save-Window $window 'm3-12-desktop-failure-observation.png' }
    throw
}
finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
