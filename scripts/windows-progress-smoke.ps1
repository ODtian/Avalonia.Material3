param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
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
function Invoke-Control($control) { $control.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Save-Window($window, [string]$name) {
    Start-Sleep -Milliseconds 200
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
$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For {
        $process.Refresh()
        if ($process.HasExited) { throw 'ProgressHost exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'desktop window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    $start = Wait-For { Find-Id $window 'StartButton' } 'start action'
    $pause = Find-Id $window 'PauseButton'
    $result = Find-Id $window 'TaskResult'
    $progress = Find-Id $window 'Progress0'
    $unknown = Find-Id $window 'Progress2'
    if ($progress.Current.ControlType -ne [System.Windows.Automation.ControlType]::ProgressBar -or $progress.Current.Name -ne 'Standard - determinate linear import progress'.Replace('-', [char]0x2014)) { throw "Progress role/name mismatch: $($progress.Current.Name)" }
    $range = $progress.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)
    if (-not $range.Current.IsReadOnly -or $range.Current.Minimum -ne 0 -or $range.Current.Maximum -ne 1) { throw 'Progress must be a read-only0..1 range.' }
    $readOnly = $false
    try { $range.SetValue(.5) } catch { $readOnly = $true }
    if (-not $readOnly -or $range.Current.Value -ne 0) { throw 'UIA must not drive the task.' }
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Invoke-Control $start
    Wait-For { $range.Current.Value -gt 0 -and $progress.Current.ItemStatus -like 'Running*' } 'live host progress' | Out-Null
    $providerHidden = $false
    try { $unknown.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern) | Out-Null } catch { $providerHidden = $true }
    if (-not $providerHidden -or $unknown.Current.ItemStatus -ne 'Running, indeterminate') { throw 'Indeterminate feedback must not advertise a numeric task value.' }
    Invoke-Control $pause
    Wait-For { $progress.Current.ItemStatus -like 'Paused*' } 'paused status' | Out-Null
    $pausedValue = $range.Current.Value
    Start-Sleep -Milliseconds 700
    if ($range.Current.Value -ne $pausedValue) { throw 'Host demo did not pause its task.' }
    Save-Window $window 'm3-11-desktop-paused.png'
    Invoke-Control $pause
    Wait-For { $result.Current.Name -eq 'Completed: 12 documents imported' } 'completed task result' | Out-Null
    if ($range.Current.Value -ne 1 -or $progress.Current.ItemStatus -ne 'Completed: 12 documents imported') { throw 'Terminal semantics did not converge.' }
    Invoke-Control (Find-Id $window 'ThemeButton')
    Invoke-Control (Find-Id $window 'MotionButton')
    Save-Window $window 'm3-11-desktop-completed-dark.png'
    Invoke-Control (Find-Id $window 'FailureButton')
    Invoke-Control $start
    Wait-For { $result.Current.Name -like 'Failed: Connection lost*' } 'failed task result' | Out-Null
    if ($progress.Current.ItemStatus -notlike 'Failed: Connection lost*') { throw 'Failure is missing from accessible status.' }
    Save-Window $window 'm3-11-desktop-failed-dark.png'
    Write-Host 'PASS Windows native package host: ProgressBar name/role, read-only RangeValue and rejected mutation, unknown provider absence, live progress, host pause, success/failure ItemStatus and visible result screenshots.'
} finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() }
    }
    $process.Dispose()
}
