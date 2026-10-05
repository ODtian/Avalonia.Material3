using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Shared content slots and independent primary activation for cards and list items.</summary>
public class MaterialContentItem : Button
{
    public static readonly StyledProperty<object?> OverlineProperty = AvaloniaProperty.Register<MaterialContentItem, object?>(nameof(Overline));
    public static readonly StyledProperty<IDataTemplate?> OverlineTemplateProperty = AvaloniaProperty.Register<MaterialContentItem, IDataTemplate?>(nameof(OverlineTemplate));
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<MaterialContentItem, object?>(nameof(Title));
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty = AvaloniaProperty.Register<MaterialContentItem, IDataTemplate?>(nameof(TitleTemplate));
    public static readonly StyledProperty<object?> SupportingContentProperty = AvaloniaProperty.Register<MaterialContentItem, object?>(nameof(SupportingContent));
    public static readonly StyledProperty<IDataTemplate?> SupportingContentTemplateProperty = AvaloniaProperty.Register<MaterialContentItem, IDataTemplate?>(nameof(SupportingContentTemplate));
    public static readonly StyledProperty<object?> ImageProperty = AvaloniaProperty.Register<MaterialContentItem, object?>(nameof(Image));
    public static readonly StyledProperty<IDataTemplate?> ImageTemplateProperty = AvaloniaProperty.Register<MaterialContentItem, IDataTemplate?>(nameof(ImageTemplate));
    public static readonly StyledProperty<object?> LeadingProperty = AvaloniaProperty.Register<MaterialContentItem, object?>(nameof(Leading));
    public static readonly StyledProperty<IDataTemplate?> LeadingTemplateProperty = AvaloniaProperty.Register<MaterialContentItem, IDataTemplate?>(nameof(LeadingTemplate));
    public static readonly StyledProperty<object?> TrailingProperty = AvaloniaProperty.Register<MaterialContentItem, object?>(nameof(Trailing));
    public static readonly StyledProperty<IDataTemplate?> TrailingTemplateProperty = AvaloniaProperty.Register<MaterialContentItem, IDataTemplate?>(nameof(TrailingTemplate));
    public static readonly StyledProperty<bool> IsSelectableProperty = AvaloniaProperty.Register<MaterialContentItem, bool>(nameof(IsSelectable));
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<MaterialContentItem, bool>(nameof(IsSelected));
    public static readonly StyledProperty<bool> IsInteractiveProperty = AvaloniaProperty.Register<MaterialContentItem, bool>(nameof(IsInteractive), true);

    public static readonly RoutedEvent<RoutedEventArgs> ActivatedEvent = RoutedEvent.Register<MaterialContentItem, RoutedEventArgs>(nameof(Activated), RoutingStrategies.Direct);
    /// <summary>Primary activation only. Unlike the inherited bubbling Click event, nested actions never raise it.</summary>
    public event EventHandler<RoutedEventArgs>? Activated
    {
        add => AddHandler(ActivatedEvent, value);
        remove => RemoveHandler(ActivatedEvent, value);
    }
    public MaterialContentItem() => PseudoClasses.Set(":interactive", IsInteractive);

    public object? Overline { get => GetValue(OverlineProperty); set => SetValue(OverlineProperty, value); }
    public IDataTemplate? OverlineTemplate { get => GetValue(OverlineTemplateProperty); set => SetValue(OverlineTemplateProperty, value); }
    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public IDataTemplate? TitleTemplate { get => GetValue(TitleTemplateProperty); set => SetValue(TitleTemplateProperty, value); }
    public object? SupportingContent { get => GetValue(SupportingContentProperty); set => SetValue(SupportingContentProperty, value); }
    public IDataTemplate? SupportingContentTemplate { get => GetValue(SupportingContentTemplateProperty); set => SetValue(SupportingContentTemplateProperty, value); }
    public object? Image { get => GetValue(ImageProperty); set => SetValue(ImageProperty, value); }
    public IDataTemplate? ImageTemplate { get => GetValue(ImageTemplateProperty); set => SetValue(ImageTemplateProperty, value); }
    public object? Leading { get => GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }
    public IDataTemplate? LeadingTemplate { get => GetValue(LeadingTemplateProperty); set => SetValue(LeadingTemplateProperty, value); }
    public object? Trailing { get => GetValue(TrailingProperty); set => SetValue(TrailingProperty, value); }
    public IDataTemplate? TrailingTemplate { get => GetValue(TrailingTemplateProperty); set => SetValue(TrailingTemplateProperty, value); }
    public bool IsSelectable { get => GetValue(IsSelectableProperty); set => SetValue(IsSelectableProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public bool IsInteractive { get => GetValue(IsInteractiveProperty); set => SetValue(IsInteractiveProperty, value); }

    protected override void OnClick()
    {
        if (!IsInteractive || !IsEffectivelyEnabled) return;
        if (IsSelectable) SetCurrentValue(IsSelectedProperty, !IsSelected);
        base.OnClick();
        RaiseEvent(new RoutedEventArgs(ActivatedEvent));
    }

    // Nested actions keep their own pointer capture, command, and keyboard behavior.
    protected bool IsNestedInput(RoutedEventArgs e) => e.Source is Visual source && source != this &&
        source.GetSelfAndVisualAncestors().TakeWhile(v => v != this).OfType<Control>().Any(c => c.Focusable);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (IsInteractive && !IsNestedInput(e)) base.OnPointerPressed(e);
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (IsInteractive && !IsNestedInput(e)) base.OnPointerReleased(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsInteractive && !IsNestedInput(e)) base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (IsInteractive && !IsNestedInput(e)) base.OnKeyUp(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty) PseudoClasses.Set(":selected", IsSelected);
        if (change.Property == IsInteractiveProperty)
        {
            PseudoClasses.Set(":interactive", IsInteractive);
            SetCurrentValue(FocusableProperty, IsInteractive);
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialContentItemAutomationPeer(this);
}

internal class MaterialContentItemAutomationPeer : ButtonAutomationPeer, IToggleProvider
{
    private readonly MaterialContentItem _owner;
    public MaterialContentItemAutomationPeer(MaterialContentItem owner) : base(owner)
    {
        _owner = owner;
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == MaterialContentItem.IsSelectedProperty)
                RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, (bool)e.OldValue! ? ToggleState.On : ToggleState.Off, (bool)e.NewValue! ? ToggleState.On : ToggleState.Off);
        };
    }
    protected override string? GetNameCore() => base.GetNameCore() ?? (_owner.Title is TextBlock text ? text.Text : _owner.Title?.ToString());
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() ?? _owner.SupportingContent as string;
    protected override AutomationControlType GetAutomationControlTypeCore() => _owner.IsInteractive ? AutomationControlType.Button : AutomationControlType.Group;
    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(IInvokeProvider) && !_owner.IsInteractive) return null;
        if (providerType == typeof(IToggleProvider) && (!_owner.IsInteractive || !_owner.IsSelectable)) return null;
        return base.GetProviderCore(providerType);
    }
    public ToggleState ToggleState => _owner.IsSelected ? ToggleState.On : ToggleState.Off;
    public void Toggle() => Invoke();
}
