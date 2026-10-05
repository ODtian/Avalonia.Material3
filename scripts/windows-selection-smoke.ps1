param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$Screenshots
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SelectionSmokeWindow {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr window);
}
'@

function Wait-For {
    param([scriptblock]$Condition, [string]$Description)
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 15) {
        $found = & $Condition
        if ($found) { return $found }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out: $Description"
}

function Find-By {
    param($Property, [string]$Value)
    $condition = New-Object System.Windows.Automation.PropertyCondition($Property, $Value)
    $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function By-Name {
    param([string]$Value)
    Wait-For { Find-By ([System.Windows.Automation.AutomationElement]::NameProperty) $Value } $Value
}

function Save-Window {
    param([string]$Name)
    $rect = $window.Current.BoundingRectangle
    $bitmap = New-Object System.Drawing.Bitmap([int]$rect.Width, [int]$rect.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bitmap.Size)
        $bitmap.Save((Join-Path $Screenshots $Name), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}

$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For {
        $process.Refresh()
        if ($process.HasExited) { throw 'Selection demo exited before creating a window.' }
        if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle }
    } 'selection desktop window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    [SelectionSmokeWindow]::SetForegroundWindow($handle) | Out-Null
    $checkbox = By-Name 'Notifications'
    $email = By-Name 'Delivery by email'
    $post = By-Name 'Delivery by post'
    $switch = By-Name 'Auto save'
    if ($checkbox.Current.ControlType -ne [System.Windows.Automation.ControlType]::CheckBox) { throw 'Incorrect checkbox role.' }
    if ($post.Current.ControlType -ne [System.Windows.Automation.ControlType]::RadioButton) { throw 'Incorrect radio role.' }
    if ($switch.Current.ControlType -ne [System.Windows.Automation.ControlType]::CheckBox) { throw 'Switch must expose the CheckBox UIA role with TogglePattern.' }
    $checkPattern = $checkbox.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $switchPattern = $switch.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $emailPattern = $email.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $postPattern = $post.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    Wait-For { $checkPattern.Current.ToggleState -eq 'Indeterminate' } 'initial mixed checkbox' | Out-Null
    $checkPattern.Toggle()
    Wait-For { $checkPattern.Current.ToggleState -eq 'Off' } 'checkbox off' | Out-Null
    $checkPattern.Toggle()
    Wait-For { $checkPattern.Current.ToggleState -eq 'On' } 'checkbox on' | Out-Null
    $checkPattern.Toggle()
    Wait-For { $checkPattern.Current.ToggleState -eq 'Indeterminate' } 'checkbox mixed' | Out-Null
    $postPattern.Select()
    Wait-For { $postPattern.Current.IsSelected -and -not $emailPattern.Current.IsSelected } 'exclusive radio selection' | Out-Null
    $switchPattern.Toggle()
    Wait-For { $switchPattern.Current.ToggleState -eq 'On' } 'switch on' | Out-Null

    (By-Name 'Save input').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $status = Wait-For { Find-By ([System.Windows.Automation.AutomationElement]::AutomationIdProperty) 'SelectionStatus' } 'form status'
    $expected = '{"Notifications":null,"Delivery":"Post","AutoSave":true}'
    Wait-For { $status.Current.Name -eq $expected } 'saved public form JSON' | Out-Null
    (By-Name 'Reset input').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $checkPattern.Current.ToggleState -eq 'Off' -and $emailPattern.Current.IsSelected -and $switchPattern.Current.ToggleState -eq 'Off' } 'reset values' | Out-Null
    (By-Name 'Restore input').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $checkPattern.Current.ToggleState -eq 'Indeterminate' -and $postPattern.Current.IsSelected -and $switchPattern.Current.ToggleState -eq 'On' } 'restored values' | Out-Null

    # Native desktop keyboard, not a synthetic headless KeyEventArgs or direct property mutation.
    $email.SetFocus()
    Wait-For { $email.Current.HasKeyboardFocus } 'email keyboard focus' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $emailPattern.Current.IsSelected } 'Space selects email' | Out-Null
    [System.Windows.Forms.SendKeys]::SendWait('{DOWN}')
    Wait-For { $post.Current.HasKeyboardFocus -and $postPattern.Current.IsSelected } 'Down arrow selects and focuses post' | Out-Null

    # Windows PowerShell 5 reads BOM-less UTF-8 as ANSI; keep the script ASCII and construct the label.
    $disabled = By-Name ("Checkbox " + [char]0x2014 + " disabled selected")
    if ($disabled.Current.ControlType -ne [System.Windows.Automation.ControlType]::CheckBox) { throw 'Disabled selection lookup did not find a checkbox peer.' }
    if ($disabled.Current.IsEnabled) { throw 'Disabled checkbox reports enabled.' }
    $disabledPattern = $disabled.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    # Avalonia's Windows backend may surface a generic provider exception rather than
    # System.Windows.Automation.ElementNotEnabledException. Assert rejection and unchanged value.
    $rejected = $false
    try { $disabledPattern.Toggle() }
    catch { $rejected = $true }
    if (-not $rejected) { throw 'Disabled checkbox accepted TogglePattern.' }
    if ($disabledPattern.Current.ToggleState -ne 'On') { throw 'Disabled checkbox changed value.' }

    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    Start-Sleep -Milliseconds 300
    Save-Window 'm3-06-desktop-light.png'
    (By-Name 'Switch light / dark').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 300
    Save-Window 'm3-06-desktop-dark.png'
    $height = $checkbox.Current.BoundingRectangle.Height
    (By-Name 'Font scale 100% / 200%').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $checkbox.Current.BoundingRectangle.Height -gt $height } 'enlarged-font layout' | Out-Null
    Start-Sleep -Milliseconds 300
    Save-Window 'm3-06-desktop-dark-font-200.png'
    Write-Host 'PASS desktop M3-06: UIA names/roles/states/actions, mixed state, exclusive radio, disabled guard, save/reset/restore, native Space/Down and font-growth layout.'
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() }
    }
    $process.Dispose()
}
