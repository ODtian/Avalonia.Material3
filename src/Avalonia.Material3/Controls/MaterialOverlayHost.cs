using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Window-local reusable overlay stack. Content remains in the same visual and theme root.</summary>
[TemplatePart("PART_ContentPresenter", typeof(MaterialOverlayContentPresenter))]
[TemplatePart("PART_OverlayLayer", typeof(Panel))]
public class MaterialOverlayHost : ContentControl
{
    public static readonly DirectProperty<MaterialOverlayHost, int> OpenCountProperty =
        AvaloniaProperty.RegisterDirect<MaterialOverlayHost, int>(nameof(OpenCount), host => host.OpenCount);
    private readonly List<MaterialOverlaySession> _sessions = [];
    private readonly HashSet<MaterialOverlayLayer> _exiting = [];
    private readonly Dictionary<Control, MaterialModalPaintScope> _paintScopes = [];
    private Panel? _layer;
    private MaterialOverlayContentPresenter? _presenter;
    private TopLevel? _root;
    private bool _redirectingFocus;
    private bool _detaching;
    private bool _committingAction;
    private MaterialOverlaySession? _outsidePress;
    public MaterialOverlayHost() => LayoutUpdated += (_, _) =>
    {
        foreach (var session in _sessions) session.Layer.UpdateAnchor();
    };

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _root = TopLevel.GetTopLevel(this);
        _root?.AddHandler(GotFocusEvent, RootGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
        _root?.AddHandler(PointerPressedEvent, RootPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        _root?.AddHandler(PointerReleasedEvent, RootPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _root?.RemoveHandler(GotFocusEvent, RootGotFocus);
        _root?.RemoveHandler(PointerPressedEvent, RootPointerPressed);
        _root?.RemoveHandler(PointerReleasedEvent, RootPointerReleased);
        _outsidePress = null;
        _root = null;
        try { ForceFinishFrom(0, MaterialOverlayCloseReason.HostDetached); }
        finally { ClearExiting(); base.OnDetachedFromVisualTree(e); }
    }

    private void RootGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (_redirectingFocus || _sessions.Count == 0 || e.Source is not Visual visual) return;
        var modalIndex = _sessions.FindLastIndex(session => session.Options.IsModal);
        if (modalIndex < 0 || _sessions.Skip(modalIndex).Any(session => session.Layer.IsVisualAncestorOf(visual))) return;
        _redirectingFocus = true;
        try { FocusContent(_sessions[^1]); }
        finally { _redirectingFocus = false; }
    }
    public int OpenCount => _sessions.Count;
    protected override Type StyleKeyOverride => typeof(MaterialOverlayHost);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialOverlayHostAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        foreach (var scope in _paintScopes.Values) scope.Dispose();
        _paintScopes.Clear();
        ClearExiting();
        if (_layer is not null) _layer.Children.Clear();
        base.OnApplyTemplate(e);
        _layer = e.NameScope.Find<Panel>("PART_OverlayLayer");
        _presenter = e.NameScope.Find<MaterialOverlayContentPresenter>("PART_ContentPresenter");
        foreach (var session in _sessions) _layer?.Children.Add(session.Layer);
        UpdateModality();
    }

    public MaterialOverlaySession Show(Control content, MaterialOverlayOptions? options = null)
    {
        options ??= new();
        Dispatcher.UIThread.VerifyAccess();
        ArgumentNullException.ThrowIfNull(content);
        if (_committingAction) throw new InvalidOperationException("A committing overlay action cannot change the presentation stack.");
        if (_detaching || TopLevel.GetTopLevel(this) is null) throw new InvalidOperationException("Attach the overlay host before showing content.");
        options.Validate();
        if (options.Placement == MaterialOverlayPlacement.Anchor && (options.Anchor is null || !this.IsVisualAncestorOf(options.Anchor)))
            throw new ArgumentException("An anchored overlay requires an attached anchor inside this host.", nameof(options));
        ApplyTemplate();
        if (_layer is null || _presenter is null) throw new InvalidOperationException("The host template must provide a MaterialOverlayContentPresenter PART_ContentPresenter and Panel PART_OverlayLayer.");
        if (content.Parent is not null || content.GetVisualParent() is not null) throw new InvalidOperationException("Overlay content must be unparented.");
        var previousFocus = options.ReturnFocus ?? TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        var scrim = new Border { Opacity = options.ShowScrim ? options.ScrimOpacity : 0, IsVisible = options.IsModal };
        scrim.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.ScrimBrush"));
        var container = new Border { Child = content, Focusable = true, FlowDirection = FlowDirection };
        KeyboardNavigation.SetTabNavigation(container, options.IsModal ? KeyboardNavigationMode.Cycle : KeyboardNavigationMode.Continue);
        var layer = new MaterialOverlayLayer(this, options, scrim, container) { FlowDirection = Avalonia.Media.FlowDirection.LeftToRight };
        layer.Children.Add(scrim); layer.Children.Add(layer.PresentationRoot);
        layer.UpdateAnchor();
        var session = new MaterialOverlaySession(this, content, options, previousFocus, layer);
        var oldCount = OpenCount;
        _sessions.Add(session);
        if (options.Anchor is { } anchor)
        {
            session.AnchorDetachedHandler = (_, _) =>
            {
                var index = _sessions.IndexOf(session);
                if (index < 0) return;
                // Children cannot outlive the presentation they were opened from.
                ForceFinishFrom(index, MaterialOverlayCloseReason.AnchorDetached);
            };
            anchor.DetachedFromVisualTree += session.AnchorDetachedHandler;
        }
        _layer.Children.Add(layer);
        UpdateModality();
        RaisePropertyChanged(OpenCountProperty, oldCount, OpenCount);
        Dispatcher.UIThread.Post(() => { if (session.IsOpen && (options.IsModal || options.TakeFocus) && _sessions[^1] == session) FocusContent(session); }, DispatcherPriority.Loaded);
        return session;
    }

    private static void FocusContent(MaterialOverlaySession session)
    {
        var target = session.Options.InitialFocus;
        if (target is null || !target.Focusable || !(target == session.Content || session.Content.IsVisualAncestorOf(target)) || !target.IsEffectivelyEnabled || !target.IsEffectivelyVisible)
            target = session.Content.GetVisualDescendants().OfType<Control>().Prepend(session.Content)
                .FirstOrDefault(control => control.Focusable && control.IsEffectivelyEnabled && control.IsEffectivelyVisible);
        (target ?? session.Layer.Container).Focus(NavigationMethod.Tab);
    }

    internal bool IsTop(MaterialOverlaySession session) => _sessions.Count > 0 && _sessions[^1] == session;
    internal bool Finish(MaterialOverlaySession session, MaterialOverlayResult result, Action? action = null)
    {
        Dispatcher.UIThread.VerifyAccess();
        var forced = result.Reason is MaterialOverlayCloseReason.HostDetached or MaterialOverlayCloseReason.AnchorDetached;
        if ((_committingAction && !forced) || _sessions.Count == 0 || _sessions[^1] != session) return false;
        if (!forced && !session.CanFinish(result)) return false;
        // A closing callback may itself complete the presentation.
        if (!session.IsOpen || _sessions.Count == 0 || _sessions[^1] != session) return false;
        if (action is not null)
        {
            _committingAction = true;
            try { action(); }
            finally { _committingAction = false; }
            if (!session.IsOpen || _sessions.Count == 0 || _sessions[^1] != session) return false;
        }
        var oldCount = OpenCount;
        _sessions.RemoveAt(_sessions.Count - 1);
        try
        {
            if (session.Options.Anchor is { } anchor && session.AnchorDetachedHandler is { } handler)
            {
                anchor.DetachedFromVisualTree -= handler;
                session.AnchorDetachedHandler = null;
            }
            if (!forced && session.Layer.Presentation?.FreezeExit(session.Content, () => RemoveExiting(session.Layer)) == true)
                _exiting.Add(session.Layer);
            else
            {
                _layer?.Children.Remove(session.Layer);
                session.Layer.Container.Child = null;
                session.Layer.Presentation?.Dispose();
            }
            UpdateModality();
            RaisePropertyChanged(OpenCountProperty, oldCount, OpenCount);
            if (!forced && session.Options.RestoreFocus)
            {
                if (session.ReturnFocus is { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } previous && TopLevel.GetTopLevel(previous) == TopLevel.GetTopLevel(this)) previous.Focus(NavigationMethod.Tab);
                else if (_sessions.Count > 0) FocusContent(_sessions[^1]);
                else if (Content is Control control)
                    control.GetVisualDescendants().OfType<Control>().Prepend(control)
                        .FirstOrDefault(candidate => candidate.Focusable && candidate.IsEffectivelyEnabled && candidate.IsEffectivelyVisible)?.Focus(NavigationMethod.Tab);
            }
        }
        finally { session.Complete(result); }
        return true;
    }
    private void RemoveExiting(MaterialOverlayLayer layer)
    {
        _exiting.Remove(layer); _layer?.Children.Remove(layer);
        layer.Container.Child = null; layer.Presentation?.Dispose();
    }
    private void ClearExiting()
    {
        foreach (var layer in _exiting.ToArray()) RemoveExiting(layer);
    }

    private void ForceFinishFrom(int index, MaterialOverlayCloseReason reason)
    {
        var wasDetaching = _detaching;
        _detaching = true;
        var errors = new List<Exception>();
        try
        {
            while (_sessions.Count > index)
            {
                try { Finish(_sessions[^1], new(reason)); }
                catch (Exception error) { errors.Add(error); }
            }
        }
        finally { _detaching = wasDetaching; }
        if (reason == MaterialOverlayCloseReason.AnchorDetached && _sessions.Count > 0) FocusContent(_sessions[^1]);
        if (errors.Count > 0) throw new AggregateException("Overlay teardown completed; host observers failed.", errors);
    }

    private void UpdateModality()
    {
        var modalIndex = _sessions.FindLastIndex(session => session.Options.IsModal);
        var blocked = new HashSet<Control>();
        if (_presenter is not null && modalIndex >= 0) blocked.Add(_presenter);
        for (var index = 0; index < modalIndex; index++) blocked.Add(_sessions[index].Layer);
        foreach (var root in _paintScopes.Keys.Where(root => !blocked.Contains(root)).ToArray())
        { _paintScopes[root].Dispose(); _paintScopes.Remove(root); }
        foreach (var root in blocked)
            if (!_paintScopes.ContainsKey(root)) _paintScopes.Add(root, new MaterialModalPaintScope(root));
    }

    private void RootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _outsidePress = null;
        if (_sessions.Count > 0 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is Visual source)
        {
            var top = _sessions[^1];
            if (source != top.Content && !top.Content.IsVisualAncestorOf(source)) _outsidePress = top;
        }
    }

    private void RootPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pressed = _outsidePress;
        _outsidePress = null;
        if (pressed is null || _sessions.Count == 0 || _sessions[^1] != pressed || e.InitialPressMouseButton != MouseButton.Left) return;
        var position = e.GetPosition(pressed.Content);
        if (!new Rect(pressed.Content.Bounds.Size).Contains(position) && pressed.Options.CloseOnLightDismiss)
            pressed.Dismiss(MaterialOverlayCloseReason.LightDismiss);
        if (pressed.Options.IsModal) e.Handled = true;
    }

    /// <summary>Forward the platform back action here. Returns whether it completed the top presentation.</summary>
    public bool RequestBack()
    {
        Dispatcher.UIThread.VerifyAccess();
        return _sessions.Count > 0 && _sessions[^1].Options.CloseOnBack && _sessions[^1].Dismiss(MaterialOverlayCloseReason.Back);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && _sessions.Count > 0 && e.Key == Key.Escape)
        {
            if (_sessions[^1].Options.CloseOnEscape) _sessions[^1].Dismiss(MaterialOverlayCloseReason.Escape);
            e.Handled = true;
        }
        else if (!e.Handled && _sessions.Count > 0 && e.Key == Key.BrowserBack) { RequestBack(); e.Handled = true; }
    }
}
