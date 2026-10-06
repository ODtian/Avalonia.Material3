param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class FloatingActionsSmokeWindow {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
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
    try { $graphics.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bitmap.Size); $bitmap.Save((Join-Path $Screenshots $name), [System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
$process = Start-Process $Executable -ArgumentList '--smoke' -PassThru
try {
    $handle = Wait-For { $process.Refresh(); if ($process.HasExited) { throw 'FloatingActionsHost exited before its window.' }; if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle } } 'desktop window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    [FloatingActionsSmokeWindow]::SetForegroundWindow($handle) | Out-Null
    $fab = Wait-For { Find-Id $window 'floating-native-fab' } 'FAB'
    $menu = Wait-For { Find-Id $window 'floating-native-menu' } 'menu'
    $toolbar = Wait-For { Find-Id $window 'floating-native-toolbar' } 'toolbar'
    $result = Wait-For { Find-Id $window 'floating-native-result' } 'result'
    $disable = Wait-For { Find-Id $window 'floating-native-disable' } 'disable'
    $mode = Wait-For { Find-Id $window 'floating-native-mode' } 'mode'
    if ($fab.Current.Name -ne 'Create document' -or $fab.Current.ControlType -ne [System.Windows.Automation.ControlType]::Button) { throw 'Wrong FAB name/role.' }
    if ($menu.Current.Name -ne 'Creation actions' -or $menu.Current.ControlType -ne [System.Windows.Automation.ControlType]::Menu) { throw 'Wrong menu name/role.' }
    if ($toolbar.Current.ControlType -ne [System.Windows.Automation.ControlType]::ToolBar) { throw 'Wrong toolbar role.' }
    $invoke = $fab.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    Wait-For { $result.Current.Name -eq 'Created 1' } 'FAB Invoke feedback' | Out-Null
    $rect = $fab.Current.BoundingRectangle
    [FloatingActionsSmokeWindow]::SetCursorPos([int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2)) | Out-Null
    [FloatingActionsSmokeWindow]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    [FloatingActionsSmokeWindow]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    Wait-For { $result.Current.Name -eq 'Created 2' } 'native mouse feedback' | Out-Null
    $expansion = $menu.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    if ($expansion.Current.ExpandCollapseState -ne [System.Windows.Automation.ExpandCollapseState]::Collapsed) { throw 'Menu initially expanded.' }
    $expansion.Expand()
    $document = Wait-For { Find-Id $window 'floating-native-Document' } 'expanded action'
    Wait-For { $document.Current.HasKeyboardFocus -and $expansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'expanded state/focus' | Out-Null
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Save-Window $window 'm3-04-desktop-light-expanded.png'
    [System.Windows.Forms.SendKeys]::SendWait('{DOWN}')
    $folder = Wait-For { Find-Id $window 'floating-native-Folder' } 'folder'
    Wait-For { $folder.Current.HasKeyboardFocus } 'arrow navigation' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Wait-For { $result.Current.Name -eq 'Folder selected' -and $expansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed } 'menu action and collapse' | Out-Null
    $expandName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Expand actions')
    $toggle = $menu.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $expandName)
    Wait-For { $toggle -and $toggle.Current.HasKeyboardFocus } 'focus return to menu anchor' | Out-Null
    $toggle.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $expansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'anchor Invoke expansion' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
    Wait-For { $expansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed } 'Escape collapse' | Out-Null
    $toolbarExpansion = $toolbar.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $toolbarExpansion.Collapse()
    Wait-For { $toolbarExpansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed } 'toolbar collapse' | Out-Null
    $toolbarFab = Wait-For { Find-Id $window 'floating-native-toolbar-fab' } 'hosted toolbar FAB'
    $fabExpansion = $toolbarFab.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    if ($fabExpansion.Current.ExpandCollapseState -ne [System.Windows.Automation.ExpandCollapseState]::Collapsed -or $toolbarFab.Current.BoundingRectangle.Width -lt 80) { throw 'Toolbar did not collapse to its enlarged disclosure FAB.' }
    $toolbarFab.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $toolbarExpansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'hosted FAB Invoke reopens toolbar' | Out-Null
    $fabExpansion.Collapse()
    $toolbarExpansion.Expand()
    Wait-For { $toolbarExpansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'toolbar expansion' | Out-Null
    $disable.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { -not $fab.Current.IsEnabled -and -not $menu.Current.IsEnabled -and -not $toolbar.Current.IsEnabled } 'disabled semantics' | Out-Null
    foreach ($operation in @({ $invoke.Invoke() }, { $expansion.Expand() }, { $toolbarExpansion.Expand() })) {
        $rejected = $false
        try { & $operation } catch { $rejected = $true; Write-Host "Disabled provider rejected: $($_.Exception.Message)" }
        if (-not $rejected) { throw 'Disabled provider did not reject.' }
    }
    if ($result.Current.Name -ne 'Folder selected') { throw 'Disabled operation changed feedback.' }
    $disable.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $mode.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $expansion.Expand()
    Save-Window $window 'm3-04-desktop-dark-expanded.png'
    Write-Host 'PASS Windows: package FAB/Menu/ToolBar name and roles, Invoke/native mouse, ExpandCollapse, arrow/Enter/Escape, anchor focus return, disabled provider rejection and light/dark screenshots.'
}
finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
