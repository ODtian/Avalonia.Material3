using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.Material3.Controls;

/// <summary>A decorative horizontal or vertical line; insets apply along the line, not to its thickness.</summary>
public class MaterialDivider : TemplatedControl
{
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<MaterialDivider, Orientation>(nameof(Orientation), validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<MaterialDivider, double>(nameof(StrokeThickness), 1, validate: value => double.IsFinite(value) && value > 0);
    public static readonly StyledProperty<double> InsetStartProperty = AvaloniaProperty.Register<MaterialDivider, double>(nameof(InsetStart), validate: value => double.IsFinite(value) && value >= 0);
    public static readonly StyledProperty<double> InsetEndProperty = AvaloniaProperty.Register<MaterialDivider, double>(nameof(InsetEnd), validate: value => double.IsFinite(value) && value >= 0);
    public static readonly DirectProperty<MaterialDivider, Thickness> LineInsetProperty = AvaloniaProperty.RegisterDirect<MaterialDivider, Thickness>(nameof(LineInset), divider => divider.LineInset);
    private Thickness _lineInset;
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double StrokeThickness { get => GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public double InsetStart { get => GetValue(InsetStartProperty); set => SetValue(InsetStartProperty, value); }
    public double InsetEnd { get => GetValue(InsetEndProperty); set => SetValue(InsetEndProperty, value); }
    public Thickness LineInset => _lineInset;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OrientationProperty || change.Property == InsetStartProperty || change.Property == InsetEndProperty)
        {
            PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
            SetAndRaise(LineInsetProperty, ref _lineInset, Orientation == Orientation.Horizontal ? new(InsetStart, 0, InsetEnd, 0) : new(0, InsetStart, 0, InsetEnd));
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new DividerPeer(this);
    private sealed class DividerPeer(MaterialDivider owner) : ControlAutomationPeer(owner)
    {
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
