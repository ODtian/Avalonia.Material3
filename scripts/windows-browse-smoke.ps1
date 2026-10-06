param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class BrowseNativeInput {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
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
function Find-Id($window, [string]$id) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Invoke-Control($control) { $control.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Save-Window($window, [string]$name) {
    Start-Sleep -Milliseconds 250
    $rect = $window.Current.BoundingRectangle
    $bounds = [System.Drawing.Rectangle]::FromLTRB([int]$rect.X, [int]$rect.Y, [int]$rect.Right, [int]$rect.Bottom)
    $visible = [System.Drawing.Rectangle]::Intersect($bounds, [System.Windows.Forms.Screen]::FromHandle($process.MainWindowHandle).WorkingArea)
    $bitmap = New-Object System.Drawing.Bitmap($visible.Width, $visible.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($visible.X, $visible.Y, 0, 0, $bitmap.Size)
        $bitmap.Save((Join-Path $Screenshots $name), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
function Drag([int]$x, [int]$y, [int]$dx, [int]$dy) {
    [BrowseNativeInput]::SetCursorPos($x, $y) | Out-Null
    [BrowseNativeInput]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
    try {
        for ($i = 1; $i -le 10; $i++) {
            [BrowseNativeInput]::SetCursorPos(($x + $dx * $i / 10), ($y + $dy * $i / 10)) | Out-Null
            Start-Sleep -Milliseconds 20
        }
    } finally { [BrowseNativeInput]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero) }
}
$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For {
        $process.Refresh()
        if ($process.HasExited) { throw 'BrowseHost exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'desktop window'
    [BrowseNativeInput]::SetForegroundWindow($handle) | Out-Null
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    $carousel = Wait-For { Find-Id $window 'CarouselMultiBrowse' } 'carousel'
    $refresh = Find-Id $window 'PhotographRefresh'
    $result = Find-Id $window 'BrowseResult'
    if ($carousel.Current.ControlType -ne [System.Windows.Automation.ControlType]::List -or $carousel.Current.Name -ne 'Multi-browse photographs') { throw 'Carousel must expose a named list.' }
    $scroll = $carousel.GetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern)
    if (-not $scroll.Current.HorizontallyScrollable) { throw 'Carousel missing native scroll semantics.' }
    $scroll.SetScrollPercent(0, -1)
    $carousel.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{RIGHT}')
    Wait-For { $carousel.Current.ItemStatus -like '2 of 6:*' } 'native right arrow advances' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{END}')
    Wait-For { $carousel.Current.ItemStatus -like '6 of 6:*' } 'native end boundary' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{HOME}')
    Wait-For { $carousel.Current.ItemStatus -like '1 of 6:*' } 'native home boundary' | Out-Null
    Start-Sleep -Milliseconds 250
    $rect = $carousel.Current.BoundingRectangle
    Drag ([int]($rect.X + 220)) ([int]($rect.Y + 80)) -110 0
    Wait-For { $carousel.Current.ItemStatus -like '2 of 6:*' } 'real Win32 mouse browsing' | Out-Null
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Save-Window $window 'm3-18-desktop-browse-light.png'
    $rect = $refresh.Current.BoundingRectangle
    $scale = $carousel.Current.BoundingRectangle.Height / 220
    Drag ([int]($rect.X + 180 * $scale)) ([int]($rect.Y + 35 * $scale)) 0 ([int](200 * $scale))
    Wait-For { $refresh.Current.ItemStatus -eq 'Refreshing' } 'real Win32 pull requests host work' | Out-Null
    Wait-For { $refresh.Current.ItemStatus -eq 'Completed: 6 photographs updated' } 'host returns refreshed images' | Out-Null
    if ($carousel.Current.ItemStatus -notlike '2 of 6:*') { throw 'Refresh lost browse position.' }
    Invoke-Control (Find-Id $window 'BrowseFailure')
    Invoke-Control $refresh
    Wait-For { $refresh.Current.ItemStatus -like 'Failed: Offline*' } 'host failure and accessible error' | Out-Null
    Save-Window $window 'm3-18-desktop-failed.png'
    Invoke-Control (Find-Id $window 'BrowseFailure')
    $refresh.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('{F5}')
    Wait-For { $refresh.Current.ItemStatus -eq 'Completed: 6 photographs updated' } 'native F5 recovery' | Out-Null
    Invoke-Control (Find-Id $window 'BrowseTheme')
    Invoke-Control (Find-Id $window 'BrowseFont')
    Invoke-Control (Find-Id $window 'BrowseWidth')
    Save-Window $window 'm3-18-desktop-320-font200-dark.png'
    Write-Host 'PASS Windows native fresh package: named List/Scroll, focus and Right/Home/End, Win32 mouse drag/pull, host image refresh/position, failure, F5 recovery, theme/font/window changes and screenshots.'
} finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() }
    }
    $process.Dispose()
}
