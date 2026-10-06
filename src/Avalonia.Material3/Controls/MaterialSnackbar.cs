using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;

namespace Avalonia.Material3.Controls;

/// <summary>Host-driven feedback, not a global queue or an implicit undo service.
/// Null Duration means 4 seconds without an action, indefinite with one.</summary>
public class MaterialSnackbar : ContentControl
{
    public static readonly StyledProperty<object?> ActionContentProperty = AvaloniaProperty.Register<MaterialSnackbar, object?>(nameof(ActionContent));
    public static readonly StyledProperty<IDataTemplate?> ActionTemplateProperty = AvaloniaProperty.Register<MaterialSnackbar, IDataTemplate?>(nameof(ActionTemplate));
    public static readonly StyledProperty<ICommand?> ActionCommandProperty = AvaloniaProperty.Register<MaterialSnackbar, ICommand?>(nameof(ActionCommand));
    public static readonly StyledProperty<object?> ActionParameterProperty = AvaloniaProperty.Register<MaterialSnackbar, object?>(nameof(ActionParameter));
    public static readonly StyledProperty<object?> ActionResultProperty = AvaloniaProperty.Register<MaterialSnackbar, object?>(nameof(ActionResult));
    public static readonly StyledProperty<bool> IsActionEnabledProperty = AvaloniaProperty.Register<MaterialSnackbar, bool>(nameof(IsActionEnabled), true);
    public static readonly StyledProperty<bool> ActionOnNewLineProperty = AvaloniaProperty.Register<MaterialSnackbar, bool>(nameof(ActionOnNewLine));
    public static readonly StyledProperty<string> DismissTextProperty = AvaloniaProperty.Register<MaterialSnackbar, string>(nameof(DismissText), "Dismiss", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<TimeSpan?> DurationProperty = AvaloniaProperty.Register<MaterialSnackbar, TimeSpan?>(nameof(Duration), validate: value => value is null || value == Timeout.InfiniteTimeSpan || value >= TimeSpan.Zero && value <= TimeSpan.FromDays(49));
    public static readonly DirectProperty<MaterialSnackbar, bool> IsOpenProperty = AvaloniaProperty.RegisterDirect<MaterialSnackbar, bool>(nameof(IsOpen), surface => surface.IsOpen);
    public static readonly DirectProperty<MaterialSnackbar, bool> CanInvokeActionProperty = AvaloniaProperty.RegisterDirect<MaterialSnackbar, bool>(nameof(CanInvokeAction), surface => surface.CanInvokeAction);
    private bool _isOpen;
    private bool _canInvokeAction;
    private MaterialFeedbackLifetime? _lifetime;
    private Button? _action, _dismiss;
    private static readonly ConditionalWeakTable<MaterialOverlayHost, Slot> Slots = new();
    private sealed class Slot { public MaterialSnackbar? Current; }
    public object? ActionContent { get => GetValue(ActionContentProperty); set => SetValue(ActionContentProperty, value); }
    public IDataTemplate? ActionTemplate { get => GetValue(ActionTemplateProperty); set => SetValue(ActionTemplateProperty, value); }
    public ICommand? ActionCommand { get => GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public object? ActionParameter { get => GetValue(ActionParameterProperty); set => SetValue(ActionParameterProperty, value); }
    public object? ActionResult { get => GetValue(ActionResultProperty); set => SetValue(ActionResultProperty, value); }
    public bool IsActionEnabled { get => GetValue(IsActionEnabledProperty); set => SetValue(IsActionEnabledProperty, value); }
    public bool ActionOnNewLine { get => GetValue(ActionOnNewLineProperty); set => SetValue(ActionOnNewLineProperty, value); }
    public string DismissText { get => GetValue(DismissTextProperty); set => SetValue(DismissTextProperty, value); }
    public TimeSpan? Duration { get => GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public bool IsOpen => _isOpen;
    public bool CanInvokeAction => _canInvokeAction;
    public MaterialOverlaySession? Session { get; private set; }
    public event EventHandler? ActionInvoked;
    public MaterialSnackbar() => AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
    protected override Type StyleKeyOverride => typeof(MaterialSnackbar);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialFeedbackAutomationPeer(this, "Snackbar", AutomationControlType.Pane);
    public MaterialOverlaySession Show(MaterialOverlayHost host, TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default, MaterialOverlayOptions? options = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsOpen) throw new InvalidOperationException("This Snackbar is already presented.");
        var slot = Slots.GetOrCreateValue(host);
        if (slot.Current is { IsOpen: true } old && !old.Dismiss())
            throw new InvalidOperationException("The current Snackbar is covered or its close was vetoed. Await its completion before replacing it.");
        var session = host.Show(this, options ?? new MaterialOverlayOptions
        {
            IsModal = false, ShowScrim = false, TakeFocus = false, RestoreFocus = false,
            Placement = MaterialOverlayPlacement.Bottom, Margin = new Thickness(12), CloseOnEscape = true
        });
        slot.Current = this; Session = session; SetAndRaise(IsOpenProperty, ref _isOpen, true);
        if (ActionCommand is { } command) command.CanExecuteChanged += CommandChanged;
        _lifetime = new MaterialFeedbackLifetime(host, session, Duration ?? (ActionContent is null ? TimeSpan.FromSeconds(4) : Timeout.InfiniteTimeSpan), timeProvider ?? TimeProvider.System, cancellationToken);
        _lifetime.StateChanged += (_, _) => RefreshAction();
        RefreshAction();
        session.Completed += (_, _) =>
        {
            _lifetime?.Dispose(); _lifetime = null; Session = null;
            if (ActionCommand is { } command) command.CanExecuteChanged -= CommandChanged;
            if (slot.Current == this) slot.Current = null;
            SetAndRaise(IsOpenProperty, ref _isOpen, false);
            RefreshAction();
        };
        return session;
    }
    public bool InvokeAction()
    {
        if (_lifetime?.IsEnding == true || !IsActionEnabled || !IsEffectivelyEnabled || ActionContent is null ||
            Session is not { IsTop: true } session || ActionCommand?.CanExecute(ActionParameter) == false) return false;
        if (!session.Close(ActionResult, () => ActionCommand?.Execute(ActionParameter))) return false;
        ActionInvoked?.Invoke(this, EventArgs.Empty);
        return true;
    }
    public bool Dismiss() => Session?.Dismiss() ?? false;
    private void RefreshAction() => SetAndRaise(CanInvokeActionProperty, ref _canInvokeAction,
        IsOpen && Session is { IsTop: true } && _lifetime?.IsEnding != true && IsActionEnabled && ActionContent is not null && ActionCommand?.CanExecute(ActionParameter) != false);
    private void CommandChanged(object? sender, EventArgs e) => Avalonia.Threading.Dispatcher.UIThread.Post(RefreshAction);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActionCommandProperty)
        {
            if (change.OldValue is ICommand old) old.CanExecuteChanged -= CommandChanged;
            if (IsOpen && ActionCommand is { } current) current.CanExecuteChanged += CommandChanged;
        }
        if (change.Property == ActionCommandProperty || change.Property == ActionParameterProperty || change.Property == IsActionEnabledProperty || change.Property == ActionContentProperty) RefreshAction();
    }
    private void ActionClick(object? sender, RoutedEventArgs e) => InvokeAction();
    private void DismissClick(object? sender, RoutedEventArgs e) => Dismiss();
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_action is not null) _action.Click -= ActionClick;
        if (_dismiss is not null) _dismiss.Click -= DismissClick;
        base.OnApplyTemplate(e);
        _action = e.NameScope.Find<Button>("PART_ActionButton"); _dismiss = e.NameScope.Find<Button>("PART_DismissButton");
        if (_action is not null) _action.Click += ActionClick;
        if (_dismiss is not null) _dismiss.Click += DismissClick;
        RefreshAction();
    }
}

internal sealed class MaterialFeedbackAutomationPeer : ControlAutomationPeer
{
    private readonly ContentControl _owner;
    private readonly string _kind;
    private readonly AutomationControlType _type;
    public MaterialFeedbackAutomationPeer(ContentControl owner, string kind, AutomationControlType type) : base(owner)
    {
        _owner = owner; _kind = kind; _type = type;
        owner.PropertyChanged += (_, change) =>
        {
            if (change.Property == ContentControl.ContentProperty)
            {
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, change.OldValue, GetName());
            }
        };
    }
    protected override string? GetNameCore() => base.GetNameCore() ?? _owner.Content as string ?? _kind;
    protected override string GetClassNameCore() => _kind;
    protected override AutomationControlType GetAutomationControlTypeCore() => _type;
}
