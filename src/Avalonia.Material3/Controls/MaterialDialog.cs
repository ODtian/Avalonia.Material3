using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;

namespace Avalonia.Material3.Controls;

public enum MaterialDialogMode { Basic, FullScreen }

public sealed class MaterialDialogConfirmingEventArgs(object? value) : EventArgs
{
    public object? Value { get; set; } = value;
    public bool Cancel { get; set; }
}

/// <summary>Material basic/full-screen content and actions; presentation and modality are supplied by MaterialOverlayHost.</summary>
[PseudoClasses(":fullscreen", ":icon", ":custom-actions", ":compact-height")]
public class MaterialDialog : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<MaterialDialog, string?>(nameof(Title));
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<MaterialDialog, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<MaterialDialog, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<MaterialDialog, object?>(nameof(Actions));
    public static readonly StyledProperty<IDataTemplate?> ActionsTemplateProperty = AvaloniaProperty.Register<MaterialDialog, IDataTemplate?>(nameof(ActionsTemplate));
    public static readonly StyledProperty<string> ConfirmTextProperty = AvaloniaProperty.Register<MaterialDialog, string>(nameof(ConfirmText), "OK", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> CancelTextProperty = AvaloniaProperty.Register<MaterialDialog, string>(nameof(CancelText), "Cancel", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<object?> ConfirmResultProperty = AvaloniaProperty.Register<MaterialDialog, object?>(nameof(ConfirmResult));
    public static readonly StyledProperty<bool> IsConfirmEnabledProperty = AvaloniaProperty.Register<MaterialDialog, bool>(nameof(IsConfirmEnabled), true);
    public static readonly StyledProperty<MaterialDialogMode> ModeProperty = AvaloniaProperty.Register<MaterialDialog, MaterialDialogMode>(nameof(Mode), validate: value => Enum.IsDefined(value));
    public static readonly DirectProperty<MaterialDialog, bool> IsOpenProperty = AvaloniaProperty.RegisterDirect<MaterialDialog, bool>(nameof(IsOpen), dialog => dialog.IsOpen);
    private bool _isOpen;
    private bool _compactHeight;
    private Control? _basicHeader, _fullHeader, _defaultActions, _customActions;
    private Grid? _layout;
    private readonly List<Button> _confirmButtons = [];
    private readonly List<Button> _cancelButtons = [];
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public IDataTemplate? ActionsTemplate { get => GetValue(ActionsTemplateProperty); set => SetValue(ActionsTemplateProperty, value); }
    public string ConfirmText { get => GetValue(ConfirmTextProperty); set => SetValue(ConfirmTextProperty, value); }
    public string CancelText { get => GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }
    public object? ConfirmResult { get => GetValue(ConfirmResultProperty); set => SetValue(ConfirmResultProperty, value); }
    public bool IsConfirmEnabled { get => GetValue(IsConfirmEnabledProperty); set => SetValue(IsConfirmEnabledProperty, value); }
    public MaterialDialogMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public bool IsOpen => _isOpen;
    public MaterialOverlaySession? Session { get; private set; }
    public event EventHandler<MaterialDialogConfirmingEventArgs>? Confirming;
    protected override Type StyleKeyOverride => typeof(MaterialDialog);
    internal void UseTimePickerTemplate() => PseudoClasses.Set(":time-picker", true);
    internal void UseDatePickerTemplate() => PseudoClasses.Set(":date-picker", true);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialDialogAutomationPeer(this);

    /// <summary>Present once; default full-screen placement follows Mode. Host options can explicitly override placement.</summary>
    public MaterialOverlaySession Show(MaterialOverlayHost host, MaterialOverlayOptions? options = null)
    {
        if (IsOpen) throw new InvalidOperationException("This dialog is already presented.");
        options ??= Mode == MaterialDialogMode.FullScreen ? new() { Placement = MaterialOverlayPlacement.FullScreen, Margin = default } : new();
        var session = host.Show(this, options);
        Session = session;
        SetAndRaise(IsOpenProperty, ref _isOpen, true);
        session.Completed += (_, _) => { Session = null; SetAndRaise(IsOpenProperty, ref _isOpen, false); };
        return session;
    }
    public Task<MaterialOverlayResult> ShowAsync(MaterialOverlayHost host, MaterialOverlayOptions? options = null) => Show(host, options).Completion;
    public bool Confirm()
    {
        if (Session is null || !Session.IsOpen || !IsEffectivelyEnabled || !IsConfirmEnabled) return false;
        var args = new MaterialDialogConfirmingEventArgs(ConfirmResult);
        Confirming?.Invoke(this, args);
        return !args.Cancel && Session is { } session && session.Close(args.Value);
    }
    public bool Cancel() => Session?.Dismiss() ?? false;
    private void ConfirmClicked(object? sender, RoutedEventArgs e) => Confirm();
    private void CancelClicked(object? sender, RoutedEventArgs e) => Cancel();
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        foreach (var button in _confirmButtons) button.Click -= ConfirmClicked;
        foreach (var button in _cancelButtons) button.Click -= CancelClicked;
        _confirmButtons.Clear(); _cancelButtons.Clear();
        base.OnApplyTemplate(e);
        _basicHeader = e.NameScope.Find<Control>("BasicHeader");
        _fullHeader = e.NameScope.Find<Control>("FullHeader");
        _defaultActions = e.NameScope.Find<Control>("DefaultActions");
        _customActions = e.NameScope.Find<Control>("CustomActions");
        _layout = e.NameScope.Find<Grid>("Layout");
        if (_layout is not null) _layout.RowDefinitions = new RowDefinitions(_compactHeight ? "Auto,Auto,Auto" : "Auto,*,Auto");
        foreach (var name in new[] { "PART_ConfirmButton", "PART_FullScreenConfirmButton" })
            if (e.NameScope.Find<Button>(name) is { } button) { _confirmButtons.Add(button); button.Click += ConfirmClicked; }
        foreach (var name in new[] { "PART_CancelButton", "PART_CloseButton" })
            if (e.NameScope.Find<Button>(name) is { } button) { _cancelButtons.Add(button); button.Click += CancelClicked; }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var measured = base.MeasureOverride(availableSize);
        var header = Mode == MaterialDialogMode.Basic ? _basicHeader : _fullHeader;
        var actions = Actions is null ? _defaultActions : _customActions;
        var compact = double.IsFinite(availableSize.Height) && header is not null &&
            header.DesiredSize.Height + (actions is { IsVisible: true } ? actions.DesiredSize.Height : 0) + Padding.Top + Padding.Bottom + 48 > availableSize.Height;
        if (compact != _compactHeight)
        {
            _compactHeight = compact;
            if (_layout is not null) _layout.RowDefinitions = new RowDefinitions(compact ? "Auto,Auto,Auto" : "Auto,*,Auto");
            PseudoClasses.Set(":compact-height", compact);
            measured = base.MeasureOverride(availableSize);
        }
        return Mode == MaterialDialogMode.Basic ? new Size(Math.Min(availableSize.Width, Math.Max(280, measured.Width)), measured.Height) : measured;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ModeProperty) PseudoClasses.Set(":fullscreen", Mode == MaterialDialogMode.FullScreen);
        if (change.Property == IconProperty) PseudoClasses.Set(":icon", Icon is not null);
        if (change.Property == ActionsProperty) PseudoClasses.Set(":custom-actions", Actions is not null);
    }
}

internal sealed class MaterialDialogAutomationPeer : ControlAutomationPeer
{
    private readonly MaterialDialog _dialog;
    public MaterialDialogAutomationPeer(MaterialDialog dialog) : base(dialog)
    {
        _dialog = dialog;
        dialog.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialDialog.TitleProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, change.OldValue, GetName());
            if (change.Property == MaterialDialog.IsOpenProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, null, GetItemStatus());
        };
    }
    protected override string? GetNameCore() => base.GetNameCore() ?? _dialog.Title;
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() ??
        (_dialog.IsOpen ? _dialog.Session?.Options.IsModal == true ? "Modal dialog open" : "Dialog open" : "Dialog closed");
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;
    protected override string GetClassNameCore() => nameof(MaterialDialog);
}
