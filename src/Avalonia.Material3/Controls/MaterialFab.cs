using Avalonia.Controls;
using Avalonia.Automation.Peers;

namespace Avalonia.Material3.Controls;

/// <summary>A floating primary action. Content/ContentTemplate is the icon slot; Button owns input, commands and Invoke.</summary>
public class MaterialFab : Button
{
    public static readonly DirectProperty<MaterialFab, double> ContainerSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialFab, double>(nameof(ContainerSize), control => control.ContainerSize);
    public static readonly DirectProperty<MaterialFab, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialFab, double>(nameof(IconSize), control => control.IconSize);
    public static readonly StyledProperty<MaterialFabSize> SizeProperty =
        AvaloniaProperty.Register<MaterialFab, MaterialFabSize>(nameof(Size), validate: value => Enum.IsDefined(value));
    public MaterialFabSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public static readonly DirectProperty<MaterialFab, MaterialFabSize> PresentedSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialFab, MaterialFabSize>(nameof(PresentedSize), control => control.PresentedSize);
    /// <summary>Effective geometry. A standard FAB grows to Medium while its whole-toolbar disclosure is collapsed.</summary>
    public MaterialFabSize PresentedSize => Size == MaterialFabSize.Standard && ToolbarExpansion is { SurfaceIsExpanded: false } ? MaterialFabSize.Medium : Size;
    public double ContainerSize => GetContainerSize(PresentedSize);
    public double IconSize => GetIconSize(PresentedSize);
    private MaterialFabSize _lastPresentedSize;
    private MaterialToolbar? _toolbarExpansion;
    internal event EventHandler? ToolbarExpansionChanged;
    internal MaterialToolbar? ToolbarExpansion
    {
        get => _toolbarExpansion;
        set
        {
            if (_toolbarExpansion == value) return;
            if (_toolbarExpansion is not null) _toolbarExpansion.PropertyChanged -= ToolbarStateChanged;
            _toolbarExpansion = value;
            if (_toolbarExpansion is not null) _toolbarExpansion.PropertyChanged += ToolbarStateChanged;
            UpdateSize();
            ToolbarExpansionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private void ToolbarStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialToolbar.IsExpandedProperty || e.Property == MaterialToolbar.CollapseBehaviorProperty || e.Property == MaterialToolbar.FloatingActionProperty)
            UpdateSize();
    }
    protected virtual double GetContainerSize(MaterialFabSize size) => size switch { MaterialFabSize.Small => 40, MaterialFabSize.Medium => 80, MaterialFabSize.Large => 96, _ => 56 };
    // FloatingActionButtonDefaults corrects the generated large icon token (32) to 36 at the pinned commit.
    protected virtual double GetIconSize(MaterialFabSize size) => size switch { MaterialFabSize.Medium => 28, MaterialFabSize.Large => 36, _ => 24 };
    protected override Type StyleKeyOverride => typeof(MaterialFab);

    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialFabAutomationPeer(this);
    protected override void OnClick()
    {
        if (!IsEffectivelyEnabled) return;
        if (ToolbarExpansion is { SurfaceIsExpanded: false } toolbar)
        {
            toolbar.SetCurrentValue(MaterialToolbar.IsExpandedProperty, true);
            return;
        }
        base.OnClick();
    }
    public MaterialFab() { DataTemplates.Add(MaterialSymbolTemplate.Instance); UpdateSize(); }
    private void UpdateSize()
    {
        MaterialFabMotion.SetGeometryAnimated(this, this is MaterialExpansionButton || ToolbarExpansion is not null);
        var old = _lastPresentedSize;
        _lastPresentedSize = PresentedSize;
        RaisePropertyChanged(PresentedSizeProperty, old, PresentedSize);
        RaisePropertyChanged(ContainerSizeProperty, GetContainerSize(old), ContainerSize);
        RaisePropertyChanged(IconSizeProperty, GetIconSize(old), IconSize);
        foreach (var size in Enum.GetValues<MaterialFabSize>())
            PseudoClasses.Set(":" + size.ToString().ToLowerInvariant(), Size == size);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeProperty)
            UpdateSize();
    }
}
