param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class M3PickerNative {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr insert, int x, int y, int cx, int cy, uint flags);
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
function Find-Id($scope, [string]$id) {
    $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)))
}
function Find-Name($scope, [string]$name) {
    $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)))
}
function Invoke-Action($control) { $control.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Toggle-Action($control) { $control.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle() }
function Keys([string]$text) { [System.Windows.Forms.SendKeys]::SendWait($text); Start-Sleep -Milliseconds 150 }
function Edit($control, [string]$text) { $control.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($text); Start-Sleep -Milliseconds 150 }
function Value($control) { $control.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value }
function Mouse-Click($control) {
    $r = $control.Current.BoundingRectangle
    [M3PickerNative]::SetCursorPos([int]($r.X + $r.Width/2), [int]($r.Y + $r.Height/2)) | Out-Null
    [M3PickerNative]::mouse_event(2,0,0,0,[UIntPtr]::Zero); [M3PickerNative]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
}
function Save-Window([string]$name) {
    Start-Sleep -Milliseconds 200
    $r = $window.Current.BoundingRectangle
    $bounds = [System.Drawing.Rectangle]::FromLTRB([int]$r.Left,[int]$r.Top,[int]$r.Right,[int]$r.Bottom)
    $visible = [System.Drawing.Rectangle]::Intersect($bounds, [System.Windows.Forms.Screen]::FromHandle($handle).WorkingArea)
    $bitmap = New-Object System.Drawing.Bitmap($visible.Width, $visible.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try { $graphics.CopyFromScreen($visible.X,$visible.Y,0,0,$bitmap.Size); $bitmap.Save((Join-Path $Screenshots $name),[System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
$process = Start-Process $Executable -PassThru
try {
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    $handle = Wait-For { $process.Refresh(); if ($process.HasExited) { throw 'PickersHost exited.' }; if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle } } 'window'
    [M3PickerNative]::SetForegroundWindow($handle) | Out-Null
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    Invoke-Action (Wait-For { Find-Id $window 'DateInputEntry' } 'date entry')
    $start = Wait-For { Find-Id $window 'DateStartInput' } 'native date input'
    if ($start.Current.ControlType -ne [System.Windows.Automation.ControlType]::Edit) { throw 'Date editor is not native Edit.' }
    Edit $start '02/29/2023'
    if ((Find-Name $window 'OK').Current.IsEnabled) { throw 'Invalid leap date allowed confirmation.' }
    if ($start.Current.HelpText -notmatch 'valid date') { throw 'Native date error was not exposed as HelpText.' }
    $start.SetFocus(); Keys '^a'; Keys '02/28/2024'; Keys '{ENTER}'
    Wait-For { (Value $start) -eq '02/28/2024' } 'native typing/preedit commit' | Out-Null
    if (-not (Find-Name $window 'OK')) { throw 'Editor Enter submitted the form.' }
    Save-Window 'm3-17-desktop-date-input.png'
    Invoke-Action (Find-Name $window 'OK')
    Wait-For { (Find-Id $window 'PickerResult').Current.Name -eq 'Confirmed date: 2024-02-28' } 'saved date' | Out-Null
    Invoke-Action (Find-Id $window 'DateInputEntry')
    $start = Wait-For { Find-Id $window 'DateStartInput' } 'reopened input'
    Edit $start '02/29/2024'; Keys '{ESC}'
    Invoke-Action (Wait-For { Find-Id $window 'DateInputEntry' } 'entry restored after Escape')
    Wait-For { (Value (Find-Id $window 'DateStartInput')) -eq '02/28/2024' } 'cancel did not commit draft' | Out-Null
    Invoke-Action (Find-Name $window 'Cancel')
    Invoke-Action (Find-Id $window 'DateRangeInputEntry')
    $end = Wait-For { Find-Id $window 'DateEndInput' } 'range end'
    Edit $end '02/28/2024'
    if ((Find-Name $window 'OK').Current.IsEnabled) { throw 'Reversed range allowed confirmation.' }
    Edit $end '03/02/2024'; Invoke-Action (Find-Name $window 'OK')
    Wait-For { (Find-Id $window 'PickerResult').Current.Name -eq 'Confirmed range: 2024-02-29..2024-03-02' } 'range result' | Out-Null
    Invoke-Action (Find-Id $window 'DateSingleEntry')
    Mouse-Click (Wait-For { Find-Name $window 'Enter date' } 'visible calendar/input mode action')
    Wait-For { Find-Id $window 'DateStartInput' } 'mode switch native editor' | Out-Null
    Invoke-Action (Find-Name $window 'Cancel')
    Invoke-Action (Find-Id $window 'Clock24Entry')
    Toggle-Action (Wait-For { Find-Name $window '23 Hour' } 'inner 24h hour action')
    $zero = Wait-For { Find-Name $window '0 Minute' } 'minute dial'
    Mouse-Click $zero; $zero.SetFocus(); Keys '{LEFT}'
    Save-Window 'm3-17-desktop-clock24.png'
    Invoke-Action (Find-Name $window 'OK')
    Wait-For { (Find-Id $window 'PickerResult').Current.Name -eq 'Confirmed time: 23:59' } 'clock native keyboard result' | Out-Null
    Invoke-Action (Find-Id $window 'Clock12Entry')
    Toggle-Action (Wait-For { Find-Name $window '12 Hour' } '12h analog action')
    Toggle-Action (Find-Name $window 'AM'); Invoke-Action (Find-Name $window 'OK')
    Wait-For { (Find-Id $window 'PickerResult').Current.Name -eq 'Confirmed time: 00:59' } '12h clock period result' | Out-Null
    Invoke-Action (Find-Id $window 'Time24Entry')
    $hour = Wait-For { Find-Id $window 'TimeHourInput' } 'native time hour'
    Edit $hour '99'
    if ((Find-Name $window 'OK').Current.IsEnabled) { throw 'Invalid hour allowed confirmation.' }
    if ($hour.Current.HelpText -notmatch 'valid hour') { throw 'Native hour error was not exposed as HelpText.' }
    $hour.SetFocus(); Keys '^a'; Keys '23'; Keys '{ENTER}'
    $minute = Find-Id $window 'TimeMinuteInput'; $minute.SetFocus(); Keys '^a'; Keys '59'; Keys '{ENTER}'
    Wait-For { (Value $minute) -eq '59' } 'native minute commit' | Out-Null
    Invoke-Action (Find-Name $window 'OK')
    Wait-For { (Find-Id $window 'PickerResult').Current.Name -eq 'Confirmed time: 23:59' } 'keyboard time result' | Out-Null
    Invoke-Action (Find-Id $window 'Time12Entry')
    Edit (Wait-For { Find-Id $window 'TimeHourInput' } '12h hour') '12'
    Edit (Find-Id $window 'TimeMinuteInput') '00'
    Toggle-Action (Find-Name $window 'AM')
    Invoke-Action (Find-Name $window 'OK')
    Wait-For { (Find-Id $window 'PickerResult').Current.Name -eq 'Confirmed time: 00:00' } 'midnight period result' | Out-Null
    Invoke-Action (Find-Id $window 'PickerTheme'); Invoke-Action (Find-Id $window 'PickerFont')
    Invoke-Action (Find-Id $window 'Time24Entry')
    Wait-For { Find-Id $window 'TimeHourInput' } 'scaled editor' | Out-Null
    [M3PickerNative]::SetWindowPos($handle,[IntPtr]::Zero,60,40,360,700,0x0040) | Out-Null
    Save-Window 'm3-17-desktop-input-dark-font200-narrow.png'
    Keys '{ESC}'
    Wait-For { (Find-Id $window 'PickerResult').Current.Name -eq 'Escape' } 'scaled cancellation' | Out-Null
    Write-Host 'PASS native package pickers: real Edit/Value typing and validation; leap/range correction; confirm/cancel host results; actual 24h inner clock and mouse minute + native arrow; 12h midnight; theme/font/resize.'
}
catch { if ($window) { Save-Window 'm3-17-desktop-failure.png' }; Write-Host $_.ScriptStackTrace; throw }
finally {
    if (-not $process.HasExited) { $process.CloseMainWindow() | Out-Null; if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() } }
    $process.Dispose()
}
