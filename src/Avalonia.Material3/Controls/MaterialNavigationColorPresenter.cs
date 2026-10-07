using Avalonia.Controls.Presenters;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

internal sealed class MaterialNavigationColorPresenter : ContentPresenter
{
    public static readonly StyledProperty<IBrush?> TargetBrushProperty = AvaloniaProperty.Register<MaterialNavigationColorPresenter, IBrush?>(nameof(TargetBrush));
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<MaterialNavigationColorPresenter, bool>(nameof(IsSelected));
    public IBrush? TargetBrush { get => GetValue(TargetBrushProperty); set => SetValue(TargetBrushProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    private readonly MaterialMotionBrush _brush;
    private readonly MaterialMotionSettings _motion;
    private IDisposable? _childForeground;
    protected override Type StyleKeyOverride => typeof(MaterialNavigationColorPresenter);
    public MaterialNavigationColorPresenter()
    {
        _brush = new(this, Foreground, paint => SetCurrentValue(ForegroundProperty, paint));
        _motion = new(this, Update);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildProperty)
        {
            _childForeground?.Dispose(); _childForeground = null;
            if (Child is { } child && !child.IsSet(ForegroundProperty))
                _childForeground = child.Bind(ForegroundProperty, this.GetObservable(ForegroundProperty));
        }
        if (change.Property == TargetBrushProperty || change.Property == IsSelectedProperty) Update();
    }
    private void Update()
    {
        if (_brush is null || _motion is null) return;
        if (!_motion.IsAttached) _brush.Snap(TargetBrush);
        else _brush.Set(TargetBrush, TemplatedParent is MaterialNavigationItem { Owner: MaterialTabs } && !IsSelected
            ? _motion.FastEffects : _motion.DefaultEffects);
    }
}
