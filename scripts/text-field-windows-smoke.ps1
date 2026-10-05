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
function Save-Window {
    param($Window, [string]$Name)
    $rect = $Window.Current.BoundingRectangle
    $bitmap = New-Object Drawing.Bitmap([int]$rect.Width, [int]$rect.Height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bitmap.Size)
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
    [Windows.Forms.SendKeys]::SendWait('Atlas')
    Start-Sleep -Milliseconds 300
    $nameValue = $name.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    Write-Host "Name value after typing: '$($nameValue.Current.Value)'; help: '$($name.Current.HelpText)'"
    Wait-For { $nameValue.Current.Value -eq 'Atlas' } 'native desktop typing' | Out-Null
    [Windows.Forms.SendKeys]::SendWait('{TAB}')
    $clear = Wait-For { Find-By $name $nameProperty 'Clear text' } 'clear action'
    Wait-For { $clear.Current.HasKeyboardFocus } 'Tab to clear' | Out-Null
    [Windows.Forms.SendKeys]::SendWait(' ')
    Wait-For { $nameValue.Current.Value -eq '' -and $name.Current.HasKeyboardFocus } 'Space clears and returns editor focus' | Out-Null
    [Windows.Forms.SendKeys]::SendWait('Atlas')
    $email.SetFocus()
    [Windows.Forms.SendKeys]::SendWait('atlas')
    $emailValue = $email.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    Wait-For { $email.Current.HelpText -like '*Include an @ sign*' } 'accessible validation feedback' | Out-Null
    [Windows.Forms.SendKeys]::SendWait('@example.test')
    Wait-For { $emailValue.Current.Value -eq 'atlas@example.test' -and $email.Current.HelpText -like '*Use a valid address*' } 'native correction restores helper text' | Out-Null
    $validate.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Wait-For { Find-By $window $nameProperty 'Form accepted' } 'form feedback' | Out-Null
    $password = Find-By $window $id 'TextField.Password'
    $password.SetFocus()
    [Windows.Forms.SendKeys]::SendWait('secret123')
    $passwordValue = $password.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    if ($passwordValue.Current.Value -ne '') { throw 'Password exposed through ValuePattern.' }
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
    Start-Sleep -Milliseconds 300
    Save-Window $window 'text-fields-desktop-large-narrow.png'
    Write-Host 'PASS Windows desktop: Edit role, native typing, Tab/Space clear, validation/correction and help text, password non-disclosure, read-only state, light/dark and large/narrow screenshots.'
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
