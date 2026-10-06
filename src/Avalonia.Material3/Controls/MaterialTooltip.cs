using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

public enum MaterialTooltipVariant { Plain, Rich }
public enum MaterialTooltipTrigger { Manual, Hover, Keyboard, LongPress }

/// <summary>Plain description or rich actionable tooltip, presented in the same bounded overlay host.</summary>
[PseudoClasses(":rich", ":caret")]
public class MaterialTooltip : ContentControl
{
    public static readonly StyledProperty<MaterialTooltipVariant> VariantProperty = AvaloniaProperty.Register<MaterialTooltip, MaterialTooltipVariant>(nameof(Variant), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<MaterialTooltip, string?>(nameof(Title));
    public static readonly StyledProperty<object?> ActionContentProperty = AvaloniaProperty.Register<MaterialTooltip, object?>(nameof(ActionContent));
    public static readonly StyledProperty<IDataTemplate?> ActionTemplateProperty = AvaloniaProperty.Register<MaterialTooltip, IDataTemplate?>(nameof(ActionTemplate));
    public static readonly StyledProperty<ICommand?> ActionCommandProperty = AvaloniaProperty.Register<MaterialTooltip, ICommand?>(nameof(ActionCommand));
    public static readonly StyledProperty<object?> ActionParameterProperty = AvaloniaProperty.Register<MaterialTooltip, object?>(nameof(ActionParameter));
    public static readonly StyledProperty<object?> ActionResultProperty = AvaloniaProperty.Register<MaterialTooltip, object?>(nameof(ActionResult));
    public static readonly StyledProperty<bool> IsActionEnabledProperty = AvaloniaProperty.Register<MaterialTooltip, bool>(nameof(IsActionEnabled), true);
    public static readonly StyledProperty<bool> IsPersistentProperty = AvaloniaProperty.Register<MaterialTooltip, bool>(nameof(IsPersistent));
    public static readonly StyledProperty<bool> EnableUserInputProperty = AvaloniaProperty.Register<MaterialTooltip, bool>(nameof(EnableUserInput), true);
    public static readonly StyledProperty<bool> ShowOnDisabledProperty = AvaloniaProperty.Register<MaterialTooltip, bool>(nameof(ShowOnDisabled));
    public static readonly StyledProperty<bool> ShowCaretProperty = AvaloniaProperty.Register<MaterialTooltip, bool>(nameof(ShowCaret));
    public static readonly StyledProperty<TimeSpan> ShowDelayProperty = AvaloniaProperty.Register<MaterialTooltip, TimeSpan>(nameof(ShowDelay), TimeSpan.FromMilliseconds(400), validate: value => value >= TimeSpan.Zero && value <= TimeSpan.FromDays(1));
    public static readonly StyledProperty<TimeSpan> LongPressDelayProperty = AvaloniaProperty.Register<MaterialTooltip, TimeSpan>(nameof(LongPressDelay), TimeSpan.FromMilliseconds(500), validate: value => value >= TimeSpan.Zero && value <= TimeSpan.FromDays(1));
    public static readonly StyledProperty<TimeSpan> DurationProperty = AvaloniaProperty.Register<MaterialTooltip, TimeSpan>(nameof(Duration), TimeSpan.FromMilliseconds(1500), validate: value => value >= TimeSpan.Zero && value <= TimeSpan.FromDays(49));
    public static readonly StyledProperty<MaterialOverlayAnchorPosition> PlacementProperty = AvaloniaProperty.Register<MaterialTooltip, MaterialOverlayAnchorPosition>(nameof(Placement), MaterialOverlayAnchorPosition.Above, validate: value => Enum.IsDefined(value));
    public static readonly DirectProperty<MaterialTooltip, bool> IsOpenProperty = AvaloniaProperty.RegisterDirect<MaterialTooltip, bool>(nameof(IsOpen), tip => tip.IsOpen);
    public static readonly DirectProperty<MaterialTooltip, bool> CanInvokeActionProperty = AvaloniaProperty.RegisterDirect<MaterialTooltip, bool>(nameof(CanInvokeAction), tip => tip.CanInvokeAction);
    private bool _isOpen;
    private bool _canInvokeAction;
    private bool _returningFocus;
    private MaterialFeedbackLifetime? _lifetime;
    private Button? _action;
    private static readonly ConditionalWeakTable<MaterialOverlayHost, Slot> Slots = new();
    private sealed class Slot { public MaterialTooltip? Current; }
    public MaterialTooltipVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? ActionContent { get => GetValue(ActionContentProperty); set => SetValue(ActionContentProperty, value); }
    public IDataTemplate? ActionTemplate { get => GetValue(ActionTemplateProperty); set => SetValue(ActionTemplateProperty, value); }
    public ICommand? ActionCommand { get => GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public object? ActionParameter { get => GetValue(ActionParameterProperty); set => SetValue(ActionParameterProperty, value); }
    public object? ActionResult { get => GetValue(ActionResultProperty); set => SetValue(ActionResultProperty, value); }
    public bool IsActionEnabled { get => GetValue(IsActionEnabledProperty); set => SetValue(IsActionEnabledProperty, value); }
    public bool IsPersistent { get => GetValue(IsPersistentProperty); set => SetValue(IsPersistentProperty, value); }
    public bool EnableUserInput { get => GetValue(EnableUserInputProperty); set => SetValue(EnableUserInputProperty, value); }
    public bool ShowOnDisabled { get => GetValue(ShowOnDisabledProperty); set => SetValue(ShowOnDisabledProperty, value); }
    public bool ShowCaret { get => GetValue(ShowCaretProperty); set => SetValue(ShowCaretProperty, value); }
    public TimeSpan ShowDelay { get => GetValue(ShowDelayProperty); set => SetValue(ShowDelayProperty, value); }
    public TimeSpan LongPressDelay { get => GetValue(LongPressDelayProperty); set => SetValue(LongPressDelayProperty, value); }
    public TimeSpan Duration { get => GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public MaterialOverlayAnchorPosition Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }
    public bool IsOpen => _isOpen;
    public bool CanInvokeAction => _canInvokeAction;
    public MaterialOverlaySession? Session { get; private set; }
    public event EventHandler? ActionInvoked;
    public MaterialTooltip() => AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
    protected override Type StyleKeyOverride => typeof(MaterialTooltip);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialFeedbackAutomationPeer(this, "Tooltip", AutomationControlType.ToolTip);
    /// <summary>Attach optional hover, focus, Tab-to-rich-action and Holding input.
    /// Dispose the attachment to remove handlers and the default anchor HelpText relation.</summary>
    public IDisposable Attach(MaterialOverlayHost host, Control anchor, TimeProvider? timeProvider = null) =>
        new Attachment(this, host, anchor, timeProvider ?? TimeProvider.System);
    public MaterialOverlaySession Show(MaterialOverlayHost host, Control anchor, MaterialTooltipTrigger trigger = MaterialTooltipTrigger.Manual,
        TimeProvider? timeProvider = null, CancellationToken cancellationToken = default, MaterialOverlayOptions? options = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enum.IsDefined(trigger)) throw new ArgumentOutOfRangeException(nameof(trigger));
        if (IsOpen) throw new InvalidOperationException("This tooltip is already presented.");
        if (!anchor.IsEffectivelyVisible || !anchor.IsEffectivelyEnabled && !ShowOnDisabled) throw new InvalidOperationException("A tooltip needs a visible, enabled anchor unless ShowOnDisabled is explicit.");
        if (Variant == MaterialTooltipVariant.Plain && ActionContent is not null) throw new InvalidOperationException("Actions belong to rich tooltips.");
        var slot = Slots.GetOrCreateValue(host);
        if (slot.Current is { IsOpen: true } old && !old.Dismiss())
            throw new InvalidOperationException("The current tooltip is covered or its close was vetoed.");
        var spacing = ShowCaret ? 12 : 4;
        var offset = Placement switch
        {
            MaterialOverlayAnchorPosition.Above => new Point(0, -spacing),
            MaterialOverlayAnchorPosition.Below => new Point(0, spacing),
            MaterialOverlayAnchorPosition.Left => new Point(-spacing, 0),
            MaterialOverlayAnchorPosition.Right => new Point(spacing, 0),
            MaterialOverlayAnchorPosition.Start => new Point(host.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? spacing : -spacing, 0),
            _ => new Point(host.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? -spacing : spacing, 0)
        };
        var session = host.Show(this, options ?? new MaterialOverlayOptions
        {
            IsModal = false, ShowScrim = false, TakeFocus = false, RestoreFocus = false,
            Placement = MaterialOverlayPlacement.Anchor, Anchor = anchor, AnchorPosition = Placement, Offset = offset,
            Margin = new Thickness(8), ReturnFocus = anchor
        });
        slot.Current = this; Session = session; SetAndRaise(IsOpenProperty, ref _isOpen, true);
        if (ActionCommand is { } command) command.CanExecuteChanged += CommandChanged;
        var timed = !IsPersistent && ActionContent is null && trigger is MaterialTooltipTrigger.Manual or MaterialTooltipTrigger.LongPress;
        _lifetime = new MaterialFeedbackLifetime(host, session, timed ? Duration : null, timeProvider ?? TimeProvider.System, cancellationToken, anchor, ShowOnDisabled);
        _lifetime.StateChanged += (_, _) => RefreshAction();
        RefreshAction();
        var returnActionFocus = false;
        session.Closing += (_, _) =>
        {
            var focused = TopLevel.GetTopLevel(host)?.FocusManager?.GetFocusedElement() as Visual;
            returnActionFocus = focused is not null && this.IsVisualAncestorOf(focused);
        };
        session.Completed += (_, _) =>
        {
            _lifetime?.Dispose(); _lifetime = null; Session = null;
            if (ActionCommand is { } command) command.CanExecuteChanged -= CommandChanged;
            if (slot.Current == this) slot.Current = null;
            SetAndRaise(IsOpenProperty, ref _isOpen, false);
            RefreshAction();
            if (returnActionFocus && anchor.IsEffectivelyEnabled && anchor.IsEffectivelyVisible &&
                TopLevel.GetTopLevel(anchor) == TopLevel.GetTopLevel(host) && TopLevel.GetTopLevel(host) is not null)
            {
                _returningFocus = true;
                try { anchor.Focus(NavigationMethod.Tab); }
                finally { _returningFocus = false; }
            }
        };
        return session;
    }
    public bool InvokeAction()
    {
        if (_lifetime?.IsEnding == true || !IsActionEnabled || !IsEffectivelyEnabled || Variant != MaterialTooltipVariant.Rich ||
            ActionContent is null || Session is not { IsTop: true } session || ActionCommand?.CanExecute(ActionParameter) == false) return false;
        if (!session.Close(ActionResult, () => ActionCommand?.Execute(ActionParameter))) return false;
        ActionInvoked?.Invoke(this, EventArgs.Empty); return true;
    }
    public bool Dismiss() => Session?.Dismiss() ?? false;
    private void RefreshAction() => SetAndRaise(CanInvokeActionProperty, ref _canInvokeAction,
        IsOpen && Session is { IsTop: true } && _lifetime?.IsEnding != true && IsActionEnabled && Variant == MaterialTooltipVariant.Rich && ActionContent is not null && ActionCommand?.CanExecute(ActionParameter) != false);
    private void CommandChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(RefreshAction);
    private void RequestDismiss() => _lifetime?.RequestDismiss();
    private void ActionClick(object? sender, RoutedEventArgs e) => InvokeAction();
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_action is not null) _action.Click -= ActionClick;
        base.OnApplyTemplate(e);
        _action = e.NameScope.Find<Button>("PART_ActionButton");
        if (_action is not null) _action.Click += ActionClick;
        RefreshAction();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty) PseudoClasses.Set(":rich", Variant == MaterialTooltipVariant.Rich);
        if (change.Property == ShowCaretProperty) PseudoClasses.Set(":caret", ShowCaret);
        if (change.Property == ActionCommandProperty)
        {
            if (change.OldValue is ICommand old) old.CanExecuteChanged -= CommandChanged;
            if (IsOpen && ActionCommand is { } current) current.CanExecuteChanged += CommandChanged;
        }
        if (change.Property == ActionCommandProperty || change.Property == ActionParameterProperty || change.Property == IsActionEnabledProperty || change.Property == ActionContentProperty || change.Property == VariantProperty) RefreshAction();
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        if (Session?.Options.Anchor is { } anchor && anchor.TransformToVisual(this) is { } transform)
        {
            var rect = new Rect(anchor.Bounds.Size).TransformToAABB(transform);
            PseudoClasses.Set(":caret-top", rect.Bottom <= 0);
            PseudoClasses.Set(":caret-bottom", rect.Top >= finalSize.Height);
            PseudoClasses.Set(":caret-left", rect.Right <= 0);
            PseudoClasses.Set(":caret-right", rect.Left >= finalSize.Width);
        }
        return result;
    }
    private sealed class Attachment : IDisposable
    {
        private readonly MaterialTooltip _tip;
        private readonly MaterialOverlayHost _host;
        private readonly Control _anchor;
        private readonly TimeProvider _clock;
        private ITimer? _hover;
        private ITimer? _hold;
        private IPointer? _pointer, _suppressRelease;
        private Point _pressPoint;
        private bool _releasingCapture;
        private readonly TopLevel? _root;
        private IDisposable? _description;
        private bool _disposed;
        public Attachment(MaterialTooltip tip, MaterialOverlayHost host, Control anchor, TimeProvider clock)
        {
            _tip = tip; _host = host; _anchor = anchor; _clock = clock;
            _root = TopLevel.GetTopLevel(host);
            anchor.PointerEntered += Enter; anchor.PointerExited += Exit; anchor.GotFocus += Focus; anchor.LostFocus += Blur;
            anchor.KeyDown += Key; anchor.DetachedFromVisualTree += Detach;
            anchor.Holding += Holding;
            anchor.AddHandler(InputElement.PointerPressedEvent, Pressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            anchor.AddHandler(InputElement.PointerMovedEvent, Moved, RoutingStrategies.Tunnel, handledEventsToo: true);
            anchor.AddHandler(InputElement.PointerReleasedEvent, Released, RoutingStrategies.Tunnel, handledEventsToo: true);
            anchor.PointerCaptureLost += CaptureLost;
            _root?.AddHandler(InputElement.PointerPressedEvent, AdditionalContact, RoutingStrategies.Tunnel, handledEventsToo: true);
            if (AutomationProperties.GetHelpText(anchor) is null && tip.Content is string)
                _description = anchor.Bind(AutomationProperties.HelpTextProperty, new Binding(nameof(Content)) { Source = tip });
        }
        private bool CanShow => !_disposed && !_tip._returningFocus && _tip.EnableUserInput && _anchor.IsEffectivelyVisible &&
            (_anchor.IsEffectivelyEnabled || _tip.ShowOnDisabled) && TopLevel.GetTopLevel(_host) is not null;
        private void Present(MaterialTooltipTrigger trigger)
        {
            if (!CanShow || _tip.IsOpen) return;
            // User-input triggers do not disturb an unrelated covered presentation.
            var slot = Slots.GetOrCreateValue(_host);
            if (slot.Current is { IsOpen: true, Session.IsTop: false }) return;
            _tip.Show(_host, _anchor, trigger, _clock);
        }
        private void Enter(object? sender, PointerEventArgs e)
        {
            if (e.Pointer.Type != PointerType.Mouse) return;
            _hover?.Dispose();
            if (CanShow) _hover = _clock.CreateTimer(_ => Dispatcher.UIThread.Post(() => { if (_anchor.IsPointerOver) Present(MaterialTooltipTrigger.Hover); }), null, _tip.ShowDelay, Timeout.InfiniteTimeSpan);
        }
        private void Exit(object? sender, PointerEventArgs e)
        {
            _hover?.Dispose(); _hover = null;
            if (!_tip.IsPersistent && _tip.ActionContent is null && !_anchor.IsFocused) _tip.RequestDismiss();
        }
        private void Focus(object? sender, FocusChangedEventArgs e)
        {
            if (e.NavigationMethod != NavigationMethod.Pointer) Present(MaterialTooltipTrigger.Keyboard);
        }
        private void Blur(object? sender, RoutedEventArgs e) => Dispatcher.UIThread.Post(() =>
        {
            if (_disposed || _tip.IsPersistent) return;
            var focus = TopLevel.GetTopLevel(_host)?.FocusManager?.GetFocusedElement() as Visual;
            if (focus != _anchor && (focus is null || !_tip.IsVisualAncestorOf(focus))) _tip.RequestDismiss();
        });
        private void Key(object? sender, KeyEventArgs e)
        {
            if (e.Key == Input.Key.Tab && _tip.Session is { IsTop: true } && _tip._action is { IsVisible: true, IsEffectivelyEnabled: true } action && _tip.Variant == MaterialTooltipVariant.Rich)
            { action.Focus(NavigationMethod.Tab); e.Handled = true; }
        }
        private void Holding(object? sender, HoldingRoutedEventArgs e)
        {
            if (CanShow && e.HoldingState == HoldingState.Started)
            { TriggerHold(); e.Handled = true; }
            else if (e.HoldingState == HoldingState.Canceled) CancelHold();
        }
        private void Pressed(object? sender, PointerPressedEventArgs e)
        {
            if (!CanShow || e.Pointer.Type is not (PointerType.Touch or PointerType.Pen)) return;
            if (_pointer is not null && _pointer != e.Pointer) { CancelHold(); return; }
            _pointer = e.Pointer; _pressPoint = e.GetPosition(_anchor);
            var pointer = e.Pointer;
            _hold?.Dispose();
            _hold = _clock.CreateTimer(_ => Dispatcher.UIThread.Post(() =>
            {
                if (!_disposed && _pointer == pointer && CanShow) TriggerHold();
            }), null, _tip.LongPressDelay, Timeout.InfiniteTimeSpan);
        }
        private void AdditionalContact(object? sender, PointerPressedEventArgs e)
        {
            if (_pointer is not null && e.Pointer != _pointer && e.Pointer.Type == PointerType.Touch) CancelHold();
        }
        private void Moved(object? sender, PointerEventArgs e)
        {
            var delta = e.GetPosition(_anchor) - _pressPoint;
            if (_pointer == e.Pointer && delta.X * delta.X + delta.Y * delta.Y > 64) CancelHold();
        }
        private void TriggerHold()
        {
            if (_pointer is not { } pointer) return;
            _suppressRelease = pointer;
            _releasingCapture = true;
            try { pointer.Capture(null); }
            finally { _releasingCapture = false; }
            CancelHold();
            Present(MaterialTooltipTrigger.LongPress);
        }
        private void Released(object? sender, PointerReleasedEventArgs e)
        {
            if (_pointer == e.Pointer) CancelHold();
            if (_suppressRelease == e.Pointer) { e.Handled = true; _suppressRelease = null; }
        }
        private void CaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            if (!_releasingCapture && _pointer == e.Pointer) CancelHold();
        }
        private void CancelHold() { _hold?.Dispose(); _hold = null; _pointer = null; }
        private void Detach(object? sender, VisualTreeAttachmentEventArgs e) => Dispose();
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            _hover?.Dispose(); _hover = null; _description?.Dispose();
            CancelHold(); _suppressRelease = null;
            _anchor.PointerEntered -= Enter; _anchor.PointerExited -= Exit; _anchor.GotFocus -= Focus; _anchor.LostFocus -= Blur;
            _anchor.KeyDown -= Key; _anchor.DetachedFromVisualTree -= Detach; _anchor.Holding -= Holding;
            _anchor.RemoveHandler(InputElement.PointerPressedEvent, Pressed);
            _anchor.RemoveHandler(InputElement.PointerMovedEvent, Moved);
            _anchor.RemoveHandler(InputElement.PointerReleasedEvent, Released);
            _anchor.PointerCaptureLost -= CaptureLost;
            _root?.RemoveHandler(InputElement.PointerPressedEvent, AdditionalContact);
            if (_tip.Session?.Options.Anchor == _anchor) _tip.RequestDismiss();
        }
    }
}
