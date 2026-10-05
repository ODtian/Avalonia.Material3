using Avalonia.Controls;

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
    public double ContainerSize => GetContainerSize(Size);
    public double IconSize => GetIconSize(Size);
    protected virtual double GetContainerSize(MaterialFabSize size) => size switch { MaterialFabSize.Small => 40, MaterialFabSize.Medium => 80, MaterialFabSize.Large => 96, _ => 56 };
    // FloatingActionButtonDefaults corrects the generated large icon token (32) to 36 at the pinned commit.
    protected virtual double GetIconSize(MaterialFabSize size) => size switch { MaterialFabSize.Medium => 28, MaterialFabSize.Large => 36, _ => 24 };
    protected override Type StyleKeyOverride => typeof(MaterialFab);

    public MaterialFab() => UpdateSize();
    private void UpdateSize()
    {
        foreach (var size in Enum.GetValues<MaterialFabSize>())
            PseudoClasses.Set(":" + size.ToString().ToLowerInvariant(), Size == size);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeProperty)
        {
            var oldSize = change.GetOldValue<MaterialFabSize>();
            RaisePropertyChanged(ContainerSizeProperty, GetContainerSize(oldSize), ContainerSize);
            RaisePropertyChanged(IconSizeProperty, GetIconSize(oldSize), IconSize);
            UpdateSize();
        }
    }
}
