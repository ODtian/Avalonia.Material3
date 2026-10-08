using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

namespace Avalonia.Material3.Controls;

/// <summary>Two separately focusable actions. The host owns secondary content/menu; expansion never invokes the main command.</summary>
public class MaterialSplitButton : Panel
{
    public static readonly StyledProperty<MaterialButtonSize> SizeProperty = AvaloniaProperty.Register<MaterialSplitButton, MaterialButtonSize>(nameof(Size), MaterialButtonSize.Small, validate: Enum.IsDefined);
    public static readonly StyledProperty<MaterialButtonVariant> VariantProperty = AvaloniaProperty.Register<MaterialSplitButton, MaterialButtonVariant>(nameof(Variant), validate: value => Enum.IsDefined(value) && value != MaterialButtonVariant.Text);
    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<MaterialSplitButton, bool>(nameof(IsExpanded), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> SecondaryIsToggleProperty = AvaloniaProperty.Register<MaterialSplitButton, bool>(nameof(SecondaryIsToggle), true);
    public MaterialButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public MaterialButtonVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public bool SecondaryIsToggle { get => GetValue(SecondaryIsToggleProperty); set => SetValue(SecondaryIsToggleProperty, value); }
    public MaterialSplitButtonPart MainButton { get; } = new();
    public MaterialSplitButtonPart SecondaryButton { get; } = new() { IsSecondary = true, Content = new MaterialSymbolSource("expand_more"), IsToggle = true };
    private bool _updating;
    public MaterialSplitButton()
    {
        Children.Add(MainButton); Children.Add(SecondaryButton);
        AutomationProperties.SetName(SecondaryButton, "More actions");
        SecondaryButton.PropertyChanged += (_, e) =>
        {
            if (e.Property == MaterialButton.IsCheckedProperty && !_updating)
                SetCurrentValue(IsExpandedProperty, SecondaryButton.IsChecked);
        };
        MainButton.HorizontalAlignment = SecondaryButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IsExpandedProperty || e.Property == SecondaryIsToggleProperty)
        {
            _updating = true;
            SecondaryButton.SetCurrentValue(MaterialButton.IsToggleProperty, SecondaryIsToggle);
            SecondaryButton.SetCurrentValue(MaterialButton.IsCheckedProperty, IsExpanded && SecondaryIsToggle);
            _updating = false;
        }
        if (e.Property == SizeProperty || e.Property == VariantProperty)
        {
            foreach (var button in new[] { MainButton, SecondaryButton })
            {
                button.SetCurrentValue(MaterialButton.SizeProperty, Size);
                button.SetCurrentValue(MaterialButton.VariantProperty, Variant);
            }
            InvalidateMeasure();
        }
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && IsExpanded)
        {
            SetCurrentValue(IsExpandedProperty, false); SecondaryButton.Focus(NavigationMethod.Directional); e.Handled = true;
        }
        else if (e.Key == Key.F4 || (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {
            if (SecondaryButton.IsEffectivelyEnabled) ControlAutomationPeer.CreatePeerForElement(SecondaryButton)!.GetProvider<IInvokeProvider>()!.Invoke();
            SecondaryButton.Focus(NavigationMethod.Directional); e.Handled = true;
        }
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        SecondaryButton.Measure(new Size(Math.Max(48, availableSize.Width - 2 - MainButton.MinimumReadableWidth), double.PositiveInfinity));
        MainButton.Measure(new Size(Math.Max(48, availableSize.Width - SecondaryButton.DesiredSize.Width - 2), double.PositiveInfinity));
        return new Size(MainButton.DesiredSize.Width + SecondaryButton.DesiredSize.Width + 2, Math.Max(MainButton.DesiredSize.Height, SecondaryButton.DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var secondaryWidth = SecondaryButton.DesiredSize.Width;
        var mainWidth = Math.Max(48, finalSize.Width - secondaryWidth - 2);
        var height = Math.Max(MainButton.DesiredSize.Height, SecondaryButton.DesiredSize.Height);
        MainButton.SetSharedHeight(height); SecondaryButton.SetSharedHeight(height);
        // Avalonia mirrors at the flow-direction boundary, including asymmetric shapes and slots.
        MainButton.Arrange(new Rect(0, 0, mainWidth, height));
        SecondaryButton.Arrange(new Rect(mainWidth + 2, 0, secondaryWidth, height));
        return finalSize;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SplitGroupPeer(this);
    private sealed class SplitGroupPeer(MaterialSplitButton owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}

/// <summary>A stable split-button action surface supporting the MaterialButton content, slots, command and template contract.</summary>
public partial class MaterialSplitButtonPart : MaterialButton
{
    internal bool IsSecondary { get; init; }
    protected override AutomationPeer OnCreateAutomationPeer() => IsSecondary ? new SplitSecondaryPeer(this) : base.OnCreateAutomationPeer();
    private sealed class SplitSecondaryPeer : ButtonAutomationPeer, IExpandCollapseProvider
    {
        private readonly MaterialSplitButtonPart _button;
        private volatile bool _expandable;
        public SplitSecondaryPeer(MaterialSplitButtonPart button) : base(button)
        {
            _button = button; _expandable = button.IsToggle;
            button.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsToggleProperty) _expandable = e.GetNewValue<bool>();
                if (e.Property == IsCheckedProperty)
                    RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                        e.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                        e.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            };
        }
        public bool ShowsMenu => _button.Flyout is not null;
        public ExpandCollapseState ExpandCollapseState => _button.IsChecked ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public void Expand() { EnsureEnabled(); if (!_button.IsChecked) Invoke(); }
        public void Collapse() { EnsureEnabled(); if (_button.IsChecked) Invoke(); }
        protected override object? GetProviderCore(Type providerType) => providerType == typeof(IExpandCollapseProvider) && !_expandable ? null : base.GetProviderCore(providerType);
    }
}
