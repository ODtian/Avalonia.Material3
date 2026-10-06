param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class M3FeedbackNative {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
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
function Find-Property($window, $property, [string]$value) {
    $condition = New-Object System.Windows.Automation.PropertyCondition($property, $value)
    $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Find-Id($window, [string]$id) { Find-Property $window ([System.Windows.Automation.AutomationElement]::AutomationIdProperty) $id }
function Find-Name($window, [string]$name) { Find-Property $window ([System.Windows.Automation.AutomationElement]::NameProperty) $name }
function Invoke-Control($control) {
    if (-not $control) { throw 'Control not present.' }
    $control.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}
function Focus-Id {
    try { [System.Windows.Automation.AutomationElement]::FocusedElement.Current.AutomationId }
    catch [System.Runtime.InteropServices.COMException] { return $null }
}
function Keys([string]$text) { [System.Windows.Forms.SendKeys]::SendWait($text); Start-Sleep -Milliseconds 150 }
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
        if ($process.HasExited) { throw 'SecondaryFeedbackHost exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'desktop window'
    [M3FeedbackNative]::SetForegroundWindow($handle) | Out-Null
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    $entry = Wait-For { Find-Id $window 'MenuStandard' } 'standard menu entry'
    $result = Find-Id $window 'FeedbackResult'
    Invoke-Control $entry
    $copy = Wait-For { Find-Id $window 'MenuCopy' } 'initial menu action'
    if ($copy.Current.ControlType -ne [System.Windows.Automation.ControlType]::MenuItem) { throw 'Menu row must expose a native MenuItem role.' }
    Wait-For { (Focus-Id) -eq 'MenuCopy' } 'initial row focus' | Out-Null
    Keys '{DOWN}'; Wait-For { (Focus-Id) -eq 'MenuExport' } 'skip disabled row' | Out-Null
    $export = Find-Id $window 'MenuExport'
    $expand = $export.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $expand.Expand()
    Wait-For { (Focus-Id) -eq 'MenuPdf' } 'submenu initial focus' | Out-Null
    Save-Window $window 'm3-13-desktop-submenu.png'
    Keys '{ESC}'; Wait-For { (Focus-Id) -eq 'MenuExport' } 'deepest Escape focus return' | Out-Null
    $expand.Expand()
    Invoke-Control (Wait-For { Find-Id $window 'MenuPdf' } 'nested PDF action')
    Wait-For { $result.Current.Name -eq 'Menu: pdf' -and (Focus-Id) -eq 'MenuStandard' } 'nested host result and focus return' | Out-Null
    Invoke-Control $entry
    $check = Wait-For { Find-Id $window 'MenuCheck' } 'checkable row'
    $toggle = $check.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $toggle.Toggle()
    if ($toggle.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On -or $check.Current.ItemStatus -ne 'Checked') { throw 'Native toggle did not expose checked state.' }
    Keys '{ESC}'
    Invoke-Control (Find-Id $window 'MenuVibrant')
    Save-Window $window 'm3-13-desktop-vibrant.png'
    Keys '{ESC}'
    Invoke-Control (Find-Id $window 'SnackbarEntry')
    $snackbar = Wait-For { Find-Id $window 'FeedbackSnackbar' } 'Snackbar live message'
    if (-not $snackbar.Current.Name.StartsWith('Changes saved')) { throw 'Snackbar message name missing.' }
    if ($snackbar.Current.ControlType -ne [System.Windows.Automation.ControlType]::Pane) { throw 'Snackbar must expose a named native Pane.' }
    Save-Window $window 'm3-13-desktop-snackbar.png'
    Invoke-Control (Find-Name $window 'Undo')
    Wait-For { $result.Current.Name -eq 'Snackbar: undo performed' } 'Snackbar host action' | Out-Null
    Invoke-Control (Find-Id $window 'TooltipPlain')
    $tip = Wait-For { Find-Id $window 'FeedbackPlainTooltip' } 'plain Tooltip'
    if (-not $tip.Current.Name.StartsWith('Opens related commands')) { throw 'Tooltip description name missing.' }
    if ($tip.Current.ControlType -ne [System.Windows.Automation.ControlType]::ToolTip) { throw 'Tooltip must expose ToolTip role.' }
    Save-Window $window 'm3-13-desktop-plain-tooltip.png'
    Keys '{ESC}'
    $rich = Find-Id $window 'TooltipRich'; $rich.SetFocus()
    Wait-For { Find-Name $window 'Learn more' } 'rich tooltip action' | Out-Null
    Save-Window $window 'm3-13-desktop-rich-tooltip.png'
    Keys '{TAB}'; Keys '{ENTER}'
    Wait-For { $result.Current.Name -eq 'Tooltip: learn more performed' -and (Focus-Id) -eq 'TooltipRich' } 'rich keyboard action and conditional focus return' | Out-Null
    Invoke-Control (Find-Id $window 'FeedbackTheme'); Invoke-Control (Find-Id $window 'FeedbackFont'); Invoke-Control (Find-Id $window 'FeedbackRtl')
    [M3FeedbackNative]::SetWindowPos($handle, [IntPtr]::Zero, 80, 60, 360, 650, 0x0040) | Out-Null
    Invoke-Control (Find-Id $window 'MenuSegmented')
    Wait-For { Find-Name $window 'Comfortable' } 'segmented menu at narrow scaled RTL' | Out-Null
    Save-Window $window 'm3-13-desktop-segmented-dark-font200-rtl.png'
    Keys '{ESC}'
    Invoke-Control (Find-Id $window 'SnackbarTimed')
    Wait-For { Find-Id $window 'FeedbackSnackbar' } 'timed feedback visible' | Out-Null
    Wait-For { $result.Current.Name -eq 'Snackbar: Cancelled' } 'production timer dismissal' | Out-Null
    Write-Host 'PASS Windows native package host: MenuItem/ToolTip/Pane names and roles; Invoke/Toggle/ExpandCollapse; disabled-row keyboard skip; deepest Escape and nested result/focus return; Snackbar action and actual timeout; rich Tab/Enter action/focus return; narrow dark 200% RTL segmented rendering.'
}
catch {
    Write-Host $_.ScriptStackTrace
    if ($window) { Save-Window $window 'm3-13-desktop-failure-observation.png' }
    throw
}
finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
