param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SearchNativeInput {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
}
'@
function Wait-For([scriptblock]$condition, [string]$description) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.Elapsed.TotalSeconds -lt 25) {
        $value = & $condition
        if ($value) { return $value }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out: $description"
}
function Find-Id($parent, [string]$id) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Find-Name($parent, [string]$name) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Invoke-Control($element) { $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Keys([string]$keys) { [System.Windows.Forms.SendKeys]::SendWait($keys); Start-Sleep -Milliseconds 150 }
function Native-Click($element) {
    $rect = $element.Current.BoundingRectangle
    [SearchNativeInput]::SetCursorPos([int]($rect.X + $rect.Width/2), [int]($rect.Y + $rect.Height/2)) | Out-Null
    [SearchNativeInput]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    [SearchNativeInput]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200
}
function Save-Window([string]$name) {
    Start-Sleep -Milliseconds 200
    $rect = $window.Current.BoundingRectangle
    $bounds = [System.Drawing.Rectangle]::FromLTRB([int]$rect.X, [int]$rect.Y, [int]$rect.Right, [int]$rect.Bottom)
    $visible = [System.Drawing.Rectangle]::Intersect($bounds, [System.Windows.Forms.Screen]::FromHandle($process.MainWindowHandle).WorkingArea)
    $bitmap = New-Object System.Drawing.Bitmap($visible.Width, $visible.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try { $graphics.CopyFromScreen($visible.X, $visible.Y, 0, 0, $bitmap.Size); $bitmap.Save((Join-Path $Screenshots $name), [System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
# ASCII source is intentional: Windows PowerShell 5.1 otherwise treats UTF-8 without BOM as ANSI.
$atlasLabel = 'Atlas / ' + [char]0x56FE + [char]0x96C6
$openLabel = 'Open access / ' + [char]0x5F00 + [char]0x653E
$queryLabel = 'Find documents / ' + [char]0x67E5 + [char]0x627E + [char]0x6587 + [char]0x6863
$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For { $process.Refresh(); if ($process.HasExited) { throw 'SearchHost exited before showing its window.' }; if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle } } 'desktop window'
    [SearchNativeInput]::SetForegroundWindow($handle) | Out-Null
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    $query = Wait-For { Find-Id $window 'QuerySearch' } 'query group'
    $editor = Wait-For { Find-Id $window 'QueryEditor' } 'native editor'
    $result = Find-Id $window 'QueryResult'
    if ($editor.Current.ControlType -ne [System.Windows.Automation.ControlType]::Edit -or $editor.Current.Name -ne $queryLabel) { throw 'Native editor role/name mismatch.' }
    $value = $editor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $value.SetValue('Atlas')
    $editor.SetFocus()
    Keys '^a{BACKSPACE}'
    Wait-For { $value.Current.Value -eq '' } 'native selected-text deletion' | Out-Null
    Keys '^z'
    Wait-For { $value.Current.Value -eq 'Atlas' } 'native undo restoration' | Out-Null
    if ($result.Current.Name -ne 'Waiting for explicit query') { throw 'Editing implicitly submitted a query.' }
    Keys '{DOWN}{ENTER}'
    Wait-For { Find-Name $query "Remove $atlasLabel" } 'keyboard accepted token' | Out-Null
    if ($value.Current.Value -ne '' -or $result.Current.Name -ne 'Waiting for explicit query') { throw 'Candidate acceptance submitted or did not clear the editor.' }
    $filter = Find-Id $window 'OpenAccessFilter'
    $toggle = $filter.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $toggle.Toggle()
    Wait-For { Find-Name $query "Remove $openLabel" } 'filter added token' | Out-Null
    if ($toggle.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) { throw 'Filter Toggle state mismatch.' }
    Invoke-Control (Find-Name $query "Remove $atlasLabel")
    Wait-For { -not (Find-Name $query "Remove $atlasLabel") } 'removed Atlas token' | Out-Null
    $value.SetValue('final')
    $submit = Find-Name $query 'Submit query'
    Invoke-Control $submit
    Wait-For { $result.Current.Name -eq "Submitted 1 (SubmitButton): [$openLabel] + final" } 'complete explicit query snapshot' | Out-Null
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Save-Window 'm3-08-desktop-light.png'
    Native-Click $submit
    Wait-For { $result.Current.Name -eq "Submitted 2 (SubmitButton): [$openLabel] + final" } 'native mouse explicit submission' | Out-Null
    Invoke-Control (Find-Id $window 'ErrorButton')
    Wait-For { $editor.Current.ItemStatus -eq 'Invalid' -and $editor.Current.HelpText -like '*Candidate source unavailable*' } 'live accessible host error' | Out-Null
    Invoke-Control $submit
    if ($result.Current.Name -notlike 'Submitted 2*') { throw 'Error query was submitted.' }
    Invoke-Control (Find-Id $window 'ErrorButton')
    $editor.SetFocus()
    Keys '{ENTER}'
    Wait-For { $result.Current.Name -eq "Submitted 3 (Keyboard): [$openLabel] + final" } 'keyboard explicit intent' | Out-Null
    Invoke-Control (Find-Id $window 'EnabledButton')
    if ($editor.Current.IsEnabled) { throw 'Disabled query still advertises enabled editor.' }
    $disabledRejected = $false
    try { Invoke-Control $submit } catch { $disabledRejected = $true }
    if (-not $disabledRejected -or $result.Current.Name -notlike 'Submitted 3*') { throw 'Disabled cached submit did not reject without mutation.' }
    Invoke-Control (Find-Id $window 'EnabledButton')
    Invoke-Control (Find-Id $window 'ThemeButton')
    Save-Window 'm3-08-desktop-dark.png'
    Invoke-Control (Find-Id $window 'FontButton')
    [SearchNativeInput]::MoveWindow($handle, 100, 80, 360, 850, $true) | Out-Null
    (Find-Id $window 'ThemeButton').SetFocus()
    $editor.SetFocus()
    Keys '{TAB}'
    Save-Window 'm3-08-desktop-narrow-font200.png'
    Write-Host 'PASS Windows fresh package: Edit/Value, native selection/delete/undo, keyboard candidate acceptance, filter Toggle, named token removal, explicit UIA/mouse/Enter intents and immutable host result, error HelpText/status, disabled rejection, dark/200%-narrow captures.'
} finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
