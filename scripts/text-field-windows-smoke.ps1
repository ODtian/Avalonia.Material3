param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$Screenshots
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class TextFieldSmokeWindow {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
}
'@
function Wait-For {
    param([scriptblock]$Condition, [string]$Description)
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 15) {
        $value = & $Condition
        if ($value) { return $value }
        Start-Sleep -Milliseconds 100
    }
    throw "Timed out: $Description"
}
function Find-By {
    param($Root, $Property, [string]$Value)
    $condition = New-Object System.Windows.Automation.PropertyCondition($Property, $Value)
    $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function Send-CommittedText {
    param([string]$Text)
    [Windows.Forms.SendKeys]::SendWait($Text)
    # A platform IME may keep the ASCII input as preedit. Enter commits it without changing OS language preferences.
    # These fields are single-line and have no default submit action.
    [Windows.Forms.SendKeys]::SendWait('{ENTER}')
}
function Save-Window {
    param($Window, [string]$Name)
    $rect = $Window.Current.BoundingRectangle
    # Record the visible host viewport only; exclude the desktop taskbar if DPI makes the requested host taller than the work area.
    $area = [Windows.Forms.Screen]::FromHandle($handle).WorkingArea
    $left = [Math]::Max([int]$rect.X, $area.Left)
    $top = [Math]::Max([int]$rect.Y, $area.Top)
    $width = [Math]::Min([int]($rect.X + $rect.Width), $area.Right) - $left
    $height = [Math]::Min([int]($rect.Y + $rect.Height), $area.Bottom) - $top
    $bitmap = New-Object Drawing.Bitmap($width, $height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($left, $top, 0, 0, $bitmap.Size)
        $bitmap.Save((Join-Path $Screenshots $Name), [Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}
New-Item -ItemType Directory -Force $Screenshots | Out-Null
$process = Start-Process $Executable -PassThru
try {
    $handle = Wait-For { $process.Refresh(); if ($process.HasExited) { throw 'Host exited.' }; if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle } } 'window'
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($handle)
    [TextFieldSmokeWindow]::SetForegroundWindow($handle) | Out-Null
    $id = [System.Windows.Automation.AutomationElement]::AutomationIdProperty
    $nameProperty = [System.Windows.Automation.AutomationElement]::NameProperty
    $name = Wait-For { Find-By $window $id 'TextField.DisplayName' } 'name editor'
    $email = Wait-For { Find-By $window $id 'TextField.Email' } 'email editor'
    $validate = Wait-For { Find-By $window $id 'TextField.Validate' } 'validate action'
    if ($name.Current.ControlType -ne [System.Windows.Automation.ControlType]::Edit) { throw 'Incorrect editor role.' }
    $validate.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $name.Current.HasKeyboardFocus } 'validation focus returns to first invalid editor' | Out-Null
    $name.SetFocus()
    Start-Sleep -Milliseconds 300
    Write-Host "Foreground: $([TextFieldSmokeWindow]::GetForegroundWindow()) / host: $handle"
    Send-CommittedText 'Atlas'
    Start-Sleep -Milliseconds 300
    $nameValue = $name.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    Write-Host "Name value after typing: '$($nameValue.Current.Value)'; help: '$($name.Current.HelpText)'"
    Wait-For { $nameValue.Current.Value -eq 'Atlas' } 'native desktop typing' | Out-Null
    [Windows.Forms.SendKeys]::SendWait('{TAB}')
    $clear = Wait-For { Find-By $name $nameProperty 'Clear text' } 'clear action'
    Wait-For { $clear.Current.HasKeyboardFocus } 'Tab to clear' | Out-Null
    [Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $nameValue.Current.Value -eq '' -and $name.Current.HasKeyboardFocus } 'Space clears and returns editor focus' | Out-Null
    [Windows.Forms.SendKeys]::SendWait('^z')
    Wait-For { $nameValue.Current.Value -eq 'Atlas' } 'native undo restores the cleared value without slot Space input' | Out-Null
    $clear = Wait-For { Find-By $name $nameProperty 'Clear text' } 'restored clear action'
    $clear.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { $nameValue.Current.Value -eq '' -and $name.Current.HasKeyboardFocus } 'automation clear retains editor focus' | Out-Null
    Send-CommittedText 'Atlas'
    $email.SetFocus()
    Send-CommittedText 'atlas'
    $emailValue = $email.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    Wait-For { $email.Current.HelpText -like '*Include an @ sign*' } 'accessible validation feedback' | Out-Null
    Send-CommittedText '@example.test'
    Wait-For { $emailValue.Current.Value -eq 'atlas@example.test' -and $email.Current.HelpText -like '*Use a valid address*' } 'native correction restores helper text' | Out-Null
    $validate.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { Find-By $window $nameProperty 'Form accepted' } 'form feedback' | Out-Null
    $password = Find-By $window $id 'TextField.Password'
    $password.SetFocus()
    Send-CommittedText 'secret123'
    $passwordValue = $password.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    if ($passwordValue.Current.Value -ne '') { throw 'Password exposed through ValuePattern.' }
    $reveal = Find-By $password $nameProperty 'Show password'
    $reveal.SetFocus()
    [Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $reveal.Current.Name -eq 'Hide password' } 'keyboard password reveal slot action' | Out-Null
    if ($passwordValue.Current.Value -ne '') { throw 'Revealed password exposed through ValuePattern.' }
    $readonly = Find-By $window $id 'TextField.ReadOnly'
    if (-not $readonly.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.IsReadOnly) { throw 'Read-only state missing.' }
    New-Item -ItemType Directory -Force $Screenshots | Out-Null
    $name.SetFocus()
    Start-Sleep -Milliseconds 300
    Save-Window $window 'text-fields-desktop-light.png'
    $theme = Find-By $window $nameProperty 'Toggle light / dark'
    $theme.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 300
    Save-Window $window 'text-fields-desktop-dark.png'
    $font = Find-By $window $nameProperty 'Use 200% text'
    $font.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $transform = $window.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern)
    $transform.Resize(320, 880)
    $name.SetFocus()
    [Windows.Forms.SendKeys]::SendWait('{END}')
    Start-Sleep -Milliseconds 300
    Save-Window $window 'text-fields-desktop-large-narrow.png'
    Write-Host 'PASS Windows desktop: Edit role, native typing, Tab/Space clear and Ctrl+Z restore, validation/correction and help text, password reveal/non-disclosure, read-only state, light/dark and large/narrow screenshots.'
    Write-Host 'Not verified here: actual screen reader announcements or a platform Chinese IME candidate session.'
}
catch {
    if ($window -and -not $process.HasExited) { Save-Window $window 'text-fields-desktop-failure.png' }
    throw
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) { $process.Kill() }
    }
    $process.Dispose()
}
