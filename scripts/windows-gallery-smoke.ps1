param([Parameter(Mandatory=$true)][string]$Executable, [Parameter(Mandatory=$true)][string]$Evidence, [int]$ColdStarts=5)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class GalleryNativeWindow {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int width, int height, uint flags);
}
'@
New-Item -ItemType Directory -Force $Evidence | Out-Null
$id = [System.Windows.Automation.AutomationElement]::AutomationIdProperty
$name = [System.Windows.Automation.AutomationElement]::NameProperty
function Wait($condition, $description) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt 25) { $probe = & $condition; if ($probe) { return $probe }; Start-Sleep -Milliseconds 80 }
    throw "Timed out: $description"
}
function Foreground {
    # Temporarily keep ONLY the owned test window above unrelated topmost desktop apps.
    # Closing the owned window in finally removes this per-window state.
    [GalleryNativeWindow]::SetWindowPos($handle,[IntPtr](-1),0,0,0,0,0x0043) | Out-Null
    [GalleryNativeWindow]::SetForegroundWindow($handle) | Out-Null
    if ([GalleryNativeWindow]::GetForegroundWindow() -ne $handle) {
        # Windows may deny a background automation caller's activation. Raise ONLY our
        # window and give its inert upper-left header a real native mouse click.
        # No foreground-lock/keyboard-layout/global setting is changed.
        [GalleryNativeWindow]::SetWindowPos($handle,[IntPtr](-1),0,0,0,0,0x0043) | Out-Null
        $bounds=$window.Current.BoundingRectangle
        [GalleryNativeWindow]::SetCursorPos([int]($bounds.X + [Math]::Min(100,$bounds.Width / 4)), [int]($bounds.Y + 12)) | Out-Null
        [GalleryNativeWindow]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
        [GalleryNativeWindow]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
    }
    Wait { [GalleryNativeWindow]::GetForegroundWindow() -eq $handle } 'actual foreground window' | Out-Null
}
function Find($property, $value) { $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition($property, [string]$value))) }
function Invoke($element) { if (!$element) { throw 'Missing native action' }; $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Keys($text) { [Windows.Forms.SendKeys]::SendWait($text); Start-Sleep -Milliseconds 150 }
function ById($value) { Wait { Find $id $value } $value }
function ByName($value) { Wait { Find $name $value } $value }
function ByPrefix($prefix) {
    Wait {
        $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition) |
            Where-Object { $_.Current.Name -like ($prefix + '*') -and $_.Current.IsEnabled } | Select-Object -First 1
    } $prefix
}
function Capture($file) {
    if ([GalleryNativeWindow]::GetForegroundWindow() -ne $handle) { Write-Host "Skipped non-owned foreground capture: $file"; return }
    $r=$window.Current.BoundingRectangle; $b=New-Object Drawing.Bitmap([int]$r.Width,[int]$r.Height); $g=[Drawing.Graphics]::FromImage($b)
    try { $g.CopyFromScreen([int]$r.X,[int]$r.Y,0,0,$b.Size); $b.Save((Join-Path $Evidence $file),[Drawing.Imaging.ImageFormat]::Png) } finally { $g.Dispose(); $b.Dispose() }
}
$timings = @(); $observations = @()
for ($iteration=0; $iteration -lt $ColdStarts; $iteration++) {
    $timer=[Diagnostics.Stopwatch]::StartNew(); $process=Start-Process $Executable -PassThru
    try {
        $handle=Wait { $process.Refresh(); if ($process.HasExited) { throw 'Published host exited before readiness' }; if ($process.MainWindowHandle -ne 0) { $process.MainWindowHandle } } 'window'
        $window=[System.Windows.Automation.AutomationElement]::FromHandle($handle)
        $action=ById 'ActionButton'; if (!$action.Current.IsEnabled) { throw 'Initial action is not enabled' }
        $timer.Stop(); $timings += $timer.Elapsed.TotalMilliseconds
        Foreground
        Invoke $action; ByName 'Action completed (1)' | Out-Null
        if ($iteration -gt 0) { continue }
        $theme=ById 'ThemeButton'; $action.SetFocus(); [Windows.Forms.SendKeys]::SendWait('{TAB}')
        Wait { $theme.Current.HasKeyboardFocus } 'actual Tab focus' | Out-Null
        [Windows.Forms.SendKeys]::SendWait(' '); Wait { $theme.Current.Name -eq 'Use light theme' } 'Space theme action' | Out-Null
        Capture 'initial-dark.png'
        $pages=@('ThemeTokens','ExpressiveButtons','FloatingActions','ButtonGroups','SelectionForm','TextFields','SearchChips','ContentHierarchy','SliderSettings','ProgressFeedback','Dialogs','SecondaryFeedback','Sheets','ContentNavigation','AppChrome','DateTimePickers','CarouselRefresh')
        foreach ($page in $pages) {
            Wait { (ById 'GalleryPage').Current.Name -like "*$page" } "route $page" | Out-Null
            $observations += "visited:$page"
            Write-Host "Native visit: $page"
            switch ($page) {
                'ExpressiveButtons' {
                    Invoke (ByName 'Submit and continue'); ByName 'Action completed (1): confirmed' | Out-Null
                    Invoke (ByName 'Favorite'); ByName 'Favorite: selected' | Out-Null
                    $observations += 'buttons:command-parameter-and-selected-icon-result'
                }
                'FloatingActions' {
                    $menu=ByName 'Creation choices'; $expansion=$menu.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                    $expansion.Expand(); Wait { $expansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'delayed FAB menu template' | Out-Null
                    Invoke (ByPrefix 'Document '); Wait { $expansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed } 'FAB action collapsed menu' | Out-Null
                    Capture 'fab-menu-action-result.png'
                    $toolbar=ByName 'Floating Horizontal Standard editing tools'
                    $toolbarExpansion=$toolbar.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                    $toolbarExpansion.Collapse(); $toolbarExpansion.Expand()
                    Wait { $toolbarExpansion.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded } 'toolbar disclosure template' | Out-Null
                    $observations += 'fab-toolbar:delayed-menu-action-collapse-and-toolbar-disclosure'
                }
                'ButtonGroups' {
                    Invoke (ById 'split-secondary'); ByName 'Cloud' | Out-Null
                    (ByName 'Cloud').GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
                    Invoke (ById 'split-main'); Wait { (ById 'split-result').Current.Name -eq 'Saved: 1' } 'split action result' | Out-Null
                    $observations += 'groups:split-secondary-destination-and-independent-main-result'
                }
                'SearchChips' {
                    $queryEditor=ById 'QueryEditor'; $queryValue=$queryEditor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
                    $queryValue.SetValue('publishedquery')
                    (ById 'OpenAccessFilter').GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
                    Invoke (ByName 'Submit query')
                    Wait { (ById 'QueryResult').Current.Name -like 'Submitted 1*publishedquery' } 'explicit query/token snapshot result' | Out-Null
                    $observations += 'search-chips:native-editor-token-toggle-and-explicit-result'
                }
                'ContentHierarchy' {
                    $card=ByName 'Elevated collection card'
                    Write-Host "Native card: $($card.Current.ClassName), $($card.Current.ControlType.ProgrammaticName), Invoke=$($card.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::IsInvokePatternAvailableProperty))"
                    Invoke $card; ByName 'Activated card-Elevated' | Out-Null
                    $observations += 'content:interactive-card-command-result'
                }
                'ContentNavigation' {
                    (ById 'navigation-main-1').GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
                    Invoke (ById 'navigation-mark-read')
                    Wait { (ById 'navigation-status').Current.Name -eq 'Library badge cleared; content action completed' } 'actual selected page/badge action result' | Out-Null
                    $observations += 'navigation:real-selected-content-and-badge-action'
                }
                'CarouselRefresh' {
                    Invoke (ById 'PhotographRefresh')
                    Wait { (ById 'BrowseResult').Current.Name -like '*photographs updated*' } 'real host refresh/image-result completion' | Out-Null
                    $observations += 'carousel-refresh:native-invoke-and-image-update-result'
                }
                'SelectionForm' {
                    $autosave=ByName 'Auto save'
                    $autosave.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
                    $post=ByName 'Delivery by post'
                    $post.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
                    Invoke (ByName 'Save input')
                    Wait { (ById 'SelectionStatus').Current.Name -like '*"AutoSave":true*' -and (ById 'SelectionStatus').Current.Name -like '*"Delivery":"Post"*' } 'compiled two-way model/serializer result' | Out-Null
                    Invoke (ByName 'Reset input'); Invoke (ByName 'Restore input')
                    Wait { $autosave.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On } 'restored compiled model binding' | Out-Null
                    $observations += 'form:compiled-two-way-toggle-radio-save-reset-restore'
                }
                'ProgressFeedback' {
                    Invoke (ById 'StartButton'); Wait { (ById 'TaskResult').Current.Name -like '*completed*' -or (ById 'TaskResult').Current.Name -like '*Completed*' } 'native task completion' | Out-Null
                }
                'SliderSettings' {
                    $slider=ByName 'Brightness'
                    $range=$slider.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)
                    $range.SetValue(72); Wait { $range.Current.Value -eq 72 } 'native settings RangeValue' | Out-Null
                    $observations += 'slider:real-native-range-value-change'
                }
                'Dialogs' {
                    Invoke (ByName 'Edit basic'); ByName 'Edit details' | Out-Null
                    Wait { !$action.Current.IsEnabled } 'whole-window background disabled' | Out-Null
                    $rejected=$false; try { Invoke $action } catch { $rejected=$true }
                    if (!$rejected) { throw 'Cached background Invoke was accepted' }
                    $next=Find $id 'GalleryNext'; if ($next -and $next.Current.IsEnabled) { throw 'Modal exposed enabled aggregate navigation' }
                    Foreground
                    $editor=ById 'DialogEditor'; $editor.SetFocus()
                    Wait { $editor.Current.HasKeyboardFocus } 'native dialog editor focus before typing' | Out-Null
                    Keys '^a'; Keys 'published'; Keys '{ENTER}'
                    $value=$editor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
                    Wait { $value.Current.Value -eq 'published' } 'native edit/preedit commit without submission' | Out-Null
                    $dialog=ByName 'Edit details'
                    if ($dialog.Current.ControlType -ne [System.Windows.Automation.ControlType]::Window -or $dialog.Current.ItemStatus -ne 'Modal dialog open') { throw 'Published dialog lost native role/state' }
                    Capture 'dialog-root-modal.png'
                    for ($tab=0; $tab -lt 7; $tab++) {
                        [Windows.Forms.SendKeys]::SendWait('{TAB}'); Start-Sleep -Milliseconds 100
                        $focused=[System.Windows.Automation.AutomationElement]::FocusedElement
                        $inside=$false
                        for ($parent=0; $focused -and $parent -lt 80; $parent++) {
                            if (($focused.GetRuntimeId() -join ',') -eq ($dialog.GetRuntimeId() -join ',')) { $inside=$true; break }
                            $focused=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($focused)
                        }
                        if (!$inside) { throw 'Actual native Tab escaped whole-window modal' }
                    }
                    Invoke (ByName 'Save'); ByName 'Confirmed: published' | Out-Null
                    Wait { $action.Current.IsEnabled } 'modal restored background' | Out-Null
                    $observations += 'dialog:native-edit-confirm-result-whole-window-modal'
                }
                'SecondaryFeedback' {
                    foreach ($menu in @('MenuStandard','MenuVibrant','MenuLegacy','MenuSegmented','MenuSegmentedVibrant')) {
                        Invoke (ById $menu); Start-Sleep -Milliseconds 200; Capture ($menu + '-open.png'); [Windows.Forms.SendKeys]::SendWait('{ESC}')
                        Start-Sleep -Milliseconds 150
                    }
                    Invoke (ById 'TooltipRich'); Start-Sleep -Milliseconds 200; [Windows.Forms.SendKeys]::SendWait('{ESC}')
                    Invoke (ById 'SnackbarEntry'); Invoke (ByName 'Undo'); ByName 'Snackbar: undo performed' | Out-Null
                    $observations += 'feedback:all-menu-recipes-rich-tooltip-snackbar-action'
                }
                'Sheets' {
                    Invoke (ByName 'Open modal bottom'); Invoke (ByName 'Expand sheet'); Start-Sleep -Milliseconds 500
                    Capture 'sheet-expanded-root-modal.png'
                    Invoke (ByName 'Host back'); Wait { !$action.Current.IsEnabled } 'collapse-first retained modality' | Out-Null
                    Invoke (ByName 'Host back'); Wait { $action.Current.IsEnabled } 'second Back closed sheet' | Out-Null
                    $observations += 'sheet:expanded-collapse-back-close-without-route'
                }
                'AppChrome' {
                    Invoke (ById 'chrome-details'); Invoke (ById 'chrome-save'); Invoke (ById 'chrome-back')
                    for ($recipe=0; $recipe -lt 8; $recipe++) { Invoke (ById 'chrome-recipe'); Start-Sleep -Milliseconds 80 }
                    Invoke (ById 'chrome-navigation'); Start-Sleep -Milliseconds 200
                    # AppChrome's documented host policy keeps the drawer open across narrow
                    # transfer only when its focus is inside; establish that public precondition.
                    Foreground
                    $homeItem=ById 'chrome-destination-home'; $homeItem.SetFocus()
                    Wait { $homeItem.Current.HasKeyboardFocus } 'focused standard drawer before responsive transfer' | Out-Null
                    [GalleryNativeWindow]::SetWindowPos($handle,[IntPtr]::Zero,80,60,500,700,0x0040) | Out-Null
                    Wait { !$action.Current.IsEnabled } 'responsive drawer covers aggregate header/nav' | Out-Null
                    Invoke (ById 'chrome-modal'); ByName 'Navigation confirmation' | Out-Null
                    Capture 'drawer-nested-root-modal.png'
                    Invoke (ById 'chrome-dialog-back')
                    Wait { !$action.Current.IsEnabled -and !(Find $name 'Navigation confirmation') } 'nested top-first back retained drawer modality' | Out-Null
                    # Native Invoke returns before posted focus restoration; wait for the real return
                    # target/foreground before sending OS keys, rather than weakening dismissal checks.
                    Foreground
                    $footer=ById 'chrome-modal'; $footer.SetFocus()
                    Wait { $footer.Current.HasKeyboardFocus } 'actual focus return inside drawer' | Out-Null
                    [Windows.Forms.SendKeys]::SendWait('{ESC}'); Wait { $action.Current.IsEnabled } 'drawer Escape restores whole-window background' | Out-Null
                    [GalleryNativeWindow]::SetWindowPos($handle,[IntPtr]::Zero,80,60,1000,800,0x0040) | Out-Null
                    $observations += 'chrome:details-save-return-eight-recipes-responsive-root-modal-nested-back'
                }
                'DateTimePickers' {
                    foreach ($entry in @('DateSingleEntry','DateRangeEntry','DateInputEntry','DateRangeInputEntry','Clock12Entry','Clock24Entry','Time12Entry','Time24Entry')) {
                        Invoke (ById $entry); Wait { !$action.Current.IsEnabled } "modal $entry" | Out-Null
                        [Windows.Forms.SendKeys]::SendWait('{ESC}'); Wait { $action.Current.IsEnabled } "dismiss $entry" | Out-Null
                    }
                    $observations += 'pickers:all-eight-delayed-modal-forms'
                }
            }
            Capture ($page + '.png')
            if ($page -ne $pages[-1]) { Invoke (ById 'GalleryNext') }
        }
        # Reach header controls even if below the header viewport via UIA ScrollItem/focus.
        foreach ($setting in @('GalleryFont','GalleryFont','GallerySeed','GalleryPlatform','GalleryShape','GalleryMotion','GalleryWindow')) { Invoke (ById $setting) }
        Capture 'narrow-font200.png'
        Invoke (ById 'GalleryPrevious'); Invoke (ById 'GalleryNext')
        Invoke (ById 'GalleryNext')
        foreach ($page in $pages) {
            Wait { (ById 'GalleryPage').Current.Name -like "*$page" } "large-font narrow route $page" | Out-Null
            Capture ($page + '-narrow-font200.png')
            if ($page -ne $pages[-1]) { Invoke (ById 'GalleryNext') }
        }
        $observations += 'runtime:font200-seed-platform-shape-motion-window-reactivation'
    }
    catch {
        if ($window) { Capture 'failure-observation.png' }
        $observedEditor=$null; if ($value) { try { $observedEditor=$value.Current.Value } catch { } }
        @{ executable=$Executable; failed=$_.Exception.Message; visited=$observations; editorValue=$observedEditor } | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $Evidence 'native-failure.json') -Encoding UTF8
        throw
    }
    finally { if (!$process.HasExited) { $process.CloseMainWindow() | Out-Null; if (!$process.WaitForExit(4000)) { $process.Kill(); $process.WaitForExit() } }; $process.Dispose() }
}
@{ executable=$Executable; sha256=(Get-FileHash $Executable -Algorithm SHA256).Hash; method='fresh-process Start-Process to native UIA enabled ActionButton; warm OS file cache; no machine reboot/cache flush; milliseconds'; coldStarts=$timings; nativeObservations=$observations; speech='NOT tested'; physicalTouch='NOT tested' } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $Evidence 'native.json') -Encoding UTF8
Write-Host "PASS published native gallery $Executable; all pages, delayed forms, real UIA/keys and $ColdStarts fresh processes"
