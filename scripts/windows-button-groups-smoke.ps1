param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Screenshots)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class GroupsSmokeWindow {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
'@
function Wait-For([scriptblock]$condition, [string]$description) {
 $timer=[Diagnostics.Stopwatch]::StartNew()
 while($timer.Elapsed.TotalSeconds -lt 20) { $value=& $condition; if($value){return $value}; Start-Sleep -Milliseconds 100 }
 throw "Timed out: $description"
}
function Find-Id($window,[string]$id) {
 $c=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id)
 $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$c)
}
function Find-Name($window,[string]$name) {
 $c=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,$name)
 $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$c)
}
function Save-Window($window,[string]$name) {
 Start-Sleep -Milliseconds 300
 $r=$window.Current.BoundingRectangle
 $b=New-Object System.Drawing.Bitmap([int]$r.Width,[int]$r.Height)
 $g=[System.Drawing.Graphics]::FromImage($b)
 try { $g.CopyFromScreen([int]$r.X,[int]$r.Y,0,0,$b.Size); $b.Save((Join-Path $Screenshots $name),[System.Drawing.Imaging.ImageFormat]::Png) }
 finally { $g.Dispose(); $b.Dispose() }
}
$process=Start-Process $Executable -PassThru
try {
 $handle=Wait-For { $process.Refresh(); if($process.HasExited){throw 'GroupsHost exited.'}; if($process.MainWindowHandle -ne 0){$process.MainWindowHandle} } 'window'
 $window=[System.Windows.Automation.AutomationElement]::FromHandle($handle)
 [GroupsSmokeWindow]::SetForegroundWindow($handle) | Out-Null
 $day=Wait-For { Find-Name $window 'Day' } 'Day option'
 $week=Wait-For { Find-Name $window 'Week' } 'Week option'
 $range=Wait-For { Find-Name $window 'Time range' } 'selection group'
 $main=Wait-For { Find-Id $window 'split-main' } 'main action'
 $secondary=Wait-For { Find-Id $window 'split-secondary' } 'secondary action'
 $result=Wait-For { Find-Id $window 'split-result' } 'action result'
 $selectionResult=Wait-For { Find-Id $window 'group-result' } 'selection result'
 $selection=$range.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern)
 $dayItem=$day.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
 $weekItem=$week.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
 if($day.Current.ControlType -ne [System.Windows.Automation.ControlType]::RadioButton -or -not $dayItem.Current.IsSelected){throw 'Incorrect single-option role/state.'}
 if(-not $selection.Current.IsSelectionRequired -or $selection.Current.CanSelectMultiple){throw 'Incorrect selection relationship.'}
 if($weekItem.Current.SelectionContainer.Current.Name -ne 'Time range'){throw 'Wrong selection container.'}
 $weekItem.Select()
 Wait-For { $weekItem.Current.IsSelected -and $selectionResult.Current.Name -eq 'Range: Week' } 'UIA selects Week' | Out-Null
 $week.SetFocus()
 [System.Windows.Forms.SendKeys]::SendWait('{LEFT}')
 Wait-For { $day.Current.HasKeyboardFocus } 'arrow focus' | Out-Null
 if(-not $weekItem.Current.IsSelected){throw 'Arrow changed selection.'}
 [System.Windows.Forms.SendKeys]::SendWait(' ')
 Wait-For { $dayItem.Current.IsSelected -and $selectionResult.Current.Name -eq 'Range: Day' } 'Space selection' | Out-Null
 $photos=Wait-For { Find-Name $window 'Photos' } 'multi option'
 if($photos.Current.ControlType -ne [System.Windows.Automation.ControlType]::CheckBox){throw 'Incorrect multi-option role.'}
 $toggle=$photos.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
 $toggle.Toggle(); Wait-For { $toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On } 'multi toggle' | Out-Null
 $mainInvoke=$main.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
 $mainInvoke.Invoke(); Wait-For { $result.Current.Name -eq 'Saved: 1' } 'main Invoke command' | Out-Null
 $r=$main.Current.BoundingRectangle
 [GroupsSmokeWindow]::SetCursorPos([int]($r.X+$r.Width/2),[int]($r.Y+$r.Height/2)) | Out-Null
 [GroupsSmokeWindow]::mouse_event(2,0,0,0,[UIntPtr]::Zero); [GroupsSmokeWindow]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
 Wait-For { $result.Current.Name -eq 'Saved: 2' } 'native mouse action' | Out-Null
 $main.SetFocus(); [System.Windows.Forms.SendKeys]::SendWait('{TAB}')
 Wait-For { $secondary.Current.HasKeyboardFocus } 'Tab to secondary' | Out-Null
 $expand=$secondary.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
 $expand.Expand(); Wait-For { $expand.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'secondary expands' | Out-Null
 if($result.Current.Name -ne 'Saved: 2'){throw 'Secondary invoked main command.'}
 [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
 Wait-For { $expand.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed -and $secondary.Current.HasKeyboardFocus } 'Escape closes and returns focus' | Out-Null
 $disable=Find-Id $window 'group-disable'
 $disable.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
 Wait-For { -not $main.Current.IsEnabled -and $secondary.Current.IsEnabled } 'independent main disabled' | Out-Null
 $blocked=$false
 try { $mainInvoke.Invoke() } catch { $blocked=$true; Write-Host "Disabled cached Invoke rejected: $($_.Exception.Message)" }
 if(-not $blocked -or $result.Current.Name -ne 'Saved: 2'){throw 'Disabled action was invoked.'}
 $disable.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
 New-Item -ItemType Directory -Force $Screenshots | Out-Null
 Save-Window $window 'm3-05-desktop-light.png'
 (Find-Id $window 'group-mode').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
 Save-Window $window 'm3-05-desktop-dark.png'
 (Find-Id $window 'group-font').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
 Save-Window $window 'm3-05-desktop-font200.png'
 Write-Host 'PASS Windows: native RadioButton/CheckBox selection relationships, UIA Select/Toggle, arrow/Space/Tab/Escape, separate Invoke/ExpandCollapse, native mouse command, independent disabled action and screenshots.'
}
finally {
 if(-not $process.HasExited){$process.CloseMainWindow() | Out-Null; if(-not $process.WaitForExit(3000)){$process.Kill();$process.WaitForExit()}}
 $process.Dispose()
}
