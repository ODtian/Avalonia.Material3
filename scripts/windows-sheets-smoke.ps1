param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class M3SheetNative {
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
function Find-Name($root, [string]$name) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Invoke-Control($control) { $control.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 150 }
function Has-Result([string]$reason) {
    # Avalonia's native TextBlock peer names itself from visible text, not necessarily its attached Name.
    if (Find-Name $window "Sheet closed: $reason; no application value was implicitly committed.") { return $true }
    $result = Find-Name $window 'Sheet result'
    return $result -and $result.Current.ItemStatus -eq $reason
}
function Focus-Name { try { [System.Windows.Automation.AutomationElement]::FocusedElement.Current.Name } catch { $null } }
function Focus-InSheet($sheet) {
    $current = [System.Windows.Automation.AutomationElement]::FocusedElement
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $sheetId = [string]::Join(',', $sheet.GetRuntimeId())
    for ($depth = 0; $current -and $depth -lt 64; $depth++) {
        if ([string]::Join(',', $current.GetRuntimeId()) -eq $sheetId) { return $true }
        $current = $walker.GetParent($current)
    }
    return $false
}
function Keys([string]$text) { [System.Windows.Forms.SendKeys]::SendWait($text); Start-Sleep -Milliseconds 150 }
function Mouse-Drag($control, [double]$dx, [double]$dy) {
    $rect = $control.Current.BoundingRectangle
    $x = $rect.X + $rect.Width / 2; $y = $rect.Y + $rect.Height / 2
    [M3SheetNative]::SetCursorPos([int]$x,[int]$y) | Out-Null
    [M3SheetNative]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
    for ($i = 1; $i -le 10; $i++) { [M3SheetNative]::SetCursorPos([int]($x + $dx * $i / 10),[int]($y + $dy * $i / 10)) | Out-Null; Start-Sleep -Milliseconds 30 }
    [M3SheetNative]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 400
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
    $hwnd = Wait-For {
        $process.Refresh()
        if ($process.HasExited) { throw 'SheetsHost exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'desktop window'
    [M3SheetNative]::SetForegroundWindow($hwnd) | Out-Null
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    $mainBefore = (Wait-For { Find-Name $window 'Sheet main content' } 'main content').Current.BoundingRectangle.Width
    Invoke-Control (Find-Name $window 'Standard side')
    $standardSide = Wait-For { Find-Name $window 'Standard side information' } 'standard side'
    $mainAfter = (Find-Name $window 'Sheet main content').Current.BoundingRectangle.Width
    if ([Math]::Abs($mainBefore - $mainAfter - $standardSide.Current.BoundingRectangle.Width) -gt 2) { throw 'Standard side did not actually reserve coplanar layout space.' }
    Save-Window $window 'm3-14-desktop-standard-side.png'
    Invoke-Control (Find-Name $window 'Standard bottom')
    $entry = Find-Name $window 'Open modal bottom'
    Invoke-Control $entry
    $sheet = Wait-For { Find-Name $window 'Modal bottom information' } 'modal bottom'
    if ($sheet.Current.ControlType -ne [System.Windows.Automation.ControlType]::Pane -or $sheet.Current.ItemStatus -ne 'partially expanded; modal') { throw 'Named Pane role/state missing.' }
    if ($entry.Current.IsEnabled -or (Find-Name $window 'Open modal bottom')) { throw 'Modal background remains enabled/exposed.' }
    $rejected = $false
    try { Invoke-Control $entry } catch { $rejected = $true }
    if (-not $rejected) { throw 'Cached native Invoke bypassed modality.' }
    $handle = Find-Name $sheet 'Resize information panel'
    $expansion = $handle.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $handle.SetFocus(); Keys '{UP}'
    Wait-For { $expansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'keyboard expansion' | Out-Null
    Start-Sleep -Milliseconds 600
    Mouse-Drag $handle 0 170
    Wait-For { $sheet.Current.ItemStatus -eq 'partially expanded; modal' } 'native handle collapse' | Out-Null
    $expansion.Expand(); Start-Sleep -Milliseconds 600
    $editor = Find-Name $sheet 'Sheet editor'
    $value = $editor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    [M3SheetNative]::SetForegroundWindow($hwnd) | Out-Null
    $editor.SetFocus(); Keys '^a'; Keys 'native'; Keys '{ENTER}'
    Wait-For { $value.Current.Value -eq 'native' } 'native editor/preedit commit' | Out-Null
    if (-not (Find-Name $window 'Modal bottom information')) { throw 'Child editor Enter submitted the sheet.' }
    Invoke-Control (Find-Name $sheet 'Child action')
    $handle.SetFocus()
    for ($i = 0; $i -lt 12; $i++) {
        Keys '{TAB}'
        if (-not (Focus-InSheet $sheet)) { throw "Native Tab focus escaped the sheet: $(Focus-Name)" }
    }
    Save-Window $window 'm3-14-desktop-modal-bottom-edit.png'
    Invoke-Control (Find-Name $sheet 'Nested dialog')
    Wait-For { Find-Name $window 'Nested sheet dialog' } 'nested dialog' | Out-Null
    Keys '{ESC}'
    Wait-For { Find-Name $window 'Modal bottom information' } 'underlying sheet after nested Escape' | Out-Null
    Invoke-Control (Find-Name $sheet 'Host back')
    Wait-For { $sheet.Current.ItemStatus -eq 'partially expanded; modal' } 'collapse-first host back' | Out-Null
    Invoke-Control (Find-Name $sheet 'Host back')
    Wait-For { (Focus-Name) -eq 'Open modal bottom' } 'native focus return' | Out-Null
    if (-not (Has-Result 'Back')) { throw 'Back result missing.' }
    Invoke-Control (Find-Name $window 'Dark theme')
    Invoke-Control (Find-Name $window '200% font')
    Invoke-Control (Find-Name $window 'Open modal side')
    $side = Wait-For { Find-Name $window 'Modal side information' } 'modal side'
    [M3SheetNative]::SetWindowPos($hwnd,[IntPtr]::Zero,80,60,400,420,0x0040) | Out-Null
    (Find-Name $side 'Resize information panel').SetFocus()
    for ($i = 0; $i -lt 24 -and (Focus-Name) -ne 'Dismiss sheet'; $i++) { Keys '{TAB}' }
    if ((Focus-Name) -ne 'Dismiss sheet') { throw 'Scaled side dismissal was not reachable by keyboard.' }
    $dismissBounds = (Find-Name $side 'Dismiss sheet').Current.BoundingRectangle
    $windowBounds = $window.Current.BoundingRectangle
    if ($dismissBounds.Top -lt $windowBounds.Top -or $dismissBounds.Bottom -gt $windowBounds.Bottom) { throw 'Scaled side focused dismissal remains clipped.' }
    Save-Window $window 'm3-14-desktop-modal-side-dark-font200-resized.png'
    Keys '{ESC}'
    Wait-For { Has-Result 'Escape' } 'resized side Escape' | Out-Null
    [M3SheetNative]::SetWindowPos($hwnd,[IntPtr]::Zero,60,40,900,700,0x0040) | Out-Null
    Invoke-Control (Find-Name $window '200% font')
    Invoke-Control (Find-Name $window 'Open modal side')
    $side = Wait-For { Find-Name $window 'Modal side information' } 'side drag presentation'
    Mouse-Drag (Find-Name $side 'Resize information panel') ($side.Current.BoundingRectangle.Width * .7) 0
    Wait-For { Has-Result 'Cancelled' } 'native side drag dismissal' | Out-Null
    Write-Host 'PASS Windows fresh-package sheets: actual coplanar space; Pane state/ExpandCollapse; blocked cached background Invoke; native drag, editor/IME commit without submit, child action, modal Tab containment, nested top-only Escape, collapse-first host back/result/focus return, side drag, dark/200%/resize screenshots.'
}
catch {
    Write-Host $_.ScriptStackTrace
    if ($value) {
        $observed = $value.Current.Value
        Write-Host "Observed native editor value: '$observed'; chars: $([string]::Join(',', @($observed.ToCharArray() | ForEach-Object { [int]$_ }))); focus: $(Focus-Name)"
    }
    if ($window) { Save-Window $window 'm3-14-desktop-failure-observation.png' }
    throw
}
finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
