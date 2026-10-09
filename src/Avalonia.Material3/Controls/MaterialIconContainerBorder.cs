namespace Avalonia.Material3.Controls;

// The visual size receives incoming constraints before the separate touch target.
internal sealed class MaterialIconContainerBorder : MaterialShapeBorder
{
    public static readonly StyledProperty<double> NominalWidthProperty =
        AvaloniaProperty.Register<MaterialIconContainerBorder, double>(nameof(NominalWidth));
    public double NominalWidth { get => GetValue(NominalWidthProperty); set => SetValue(NominalWidthProperty, value); }
    private double _limit = double.PositiveInfinity;
    protected override Type StyleKeyOverride => typeof(MaterialShapeBorder);
    internal void SetVisualConstraint(double limit)
    {
        if (_limit == limit) return;
        _limit = limit; UpdateWidth();
    }
    private void UpdateWidth()
    { SetCurrentValue(MinWidthProperty, Math.Min(NominalWidth, _limit)); SetCurrentValue(MaxWidthProperty, _limit); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NominalWidthProperty) UpdateWidth();
    }
}
