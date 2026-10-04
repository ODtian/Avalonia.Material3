using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace Avalonia.Material3.Themes;

/// <summary>Installs the Material 3 resources and control themes. Update inputs on the UI thread.</summary>
public class MaterialTheme : Styles
{
    public static readonly StyledProperty<MaterialColorScheme> LightColorSchemeProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialColorScheme>(nameof(LightColorScheme), MaterialColorScheme.Light,
            validate: value => value is not null);

    public static readonly StyledProperty<MaterialColorScheme> DarkColorSchemeProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialColorScheme>(nameof(DarkColorScheme), MaterialColorScheme.Dark,
            validate: value => value is not null);

    public static readonly StyledProperty<MaterialTypography> TypographyProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialTypography>(nameof(Typography), new(),
            validate: value => value is { IsValid: true });

    public static readonly StyledProperty<MaterialShapes> ShapesProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialShapes>(nameof(Shapes), new(),
            validate: value => value is { IsValid: true });

    public static readonly StyledProperty<MaterialMotion> MotionProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialMotion>(nameof(Motion), new(),
            validate: value => value is { IsValid: true });

    public MaterialTypography Typography
    {
        get => GetValue(TypographyProperty);
        set => SetValue(TypographyProperty, value);
    }

    public MaterialShapes Shapes
    {
        get => GetValue(ShapesProperty);
        set => SetValue(ShapesProperty, value);
    }

    public MaterialMotion Motion
    {
        get => GetValue(MotionProperty);
        set => SetValue(MotionProperty, value);
    }

    public MaterialColorScheme LightColorScheme
    {
        get => GetValue(LightColorSchemeProperty);
        set => SetValue(LightColorSchemeProperty, value);
    }

    public MaterialColorScheme DarkColorScheme
    {
        get => GetValue(DarkColorSchemeProperty);
        set => SetValue(DarkColorSchemeProperty, value);
    }

    public MaterialTheme()
    {
        AvaloniaXamlLoader.Load(this);
        UpdateColors(ThemeVariant.Light, LightColorScheme);
        UpdateColors(ThemeVariant.Dark, DarkColorScheme);
        UpdateTypography();
        UpdateShapes();
        UpdateMotion();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LightColorSchemeProperty)
            UpdateColors(ThemeVariant.Light, LightColorScheme);
        else if (change.Property == DarkColorSchemeProperty)
            UpdateColors(ThemeVariant.Dark, DarkColorScheme);
        else if (change.Property == TypographyProperty)
            UpdateTypography();
        else if (change.Property == ShapesProperty)
            UpdateShapes();
        else if (change.Property == MotionProperty)
            UpdateMotion();
    }

    private void UpdateTypography()
    {
        Resources["M3.FontFamily"] = Typography.FontFamily;
        Resources["M3.LabelLargeFontSize"] = 14 * Typography.Scale;
        Resources["M3.BodyLargeFontSize"] = 16 * Typography.Scale;
    }

    private void UpdateShapes()
    {
        Resources["M3.ButtonCornerRadius"] = new CornerRadius(Shapes.ButtonCornerRadius);
        Resources["M3.PressedButtonCornerRadius"] = new CornerRadius(Shapes.PressedButtonCornerRadius);
        Resources["M3.ButtonFocusCornerRadius"] = new CornerRadius(Shapes.ButtonCornerRadius + 4);
    }

    private void UpdateMotion() =>
        Resources["M3.StateLayerDuration"] = Motion.ReduceMotion ? TimeSpan.Zero : Motion.StateLayerDuration;

    private void UpdateColors(ThemeVariant variant, MaterialColorScheme scheme)
    {
        Resources.ThemeDictionaries[variant] = new ResourceDictionary
        {
            ["M3.PrimaryBrush"] = new ImmutableSolidColorBrush(scheme.Primary),
            ["M3.OnPrimaryBrush"] = new ImmutableSolidColorBrush(scheme.OnPrimary),
            ["M3.SurfaceBrush"] = new ImmutableSolidColorBrush(scheme.Surface),
            ["M3.OnSurfaceBrush"] = new ImmutableSolidColorBrush(scheme.OnSurface),
            ["M3.OnSurfaceVariantBrush"] = new ImmutableSolidColorBrush(scheme.OnSurfaceVariant),
            ["M3.OutlineBrush"] = new ImmutableSolidColorBrush(scheme.Outline),
            ["M3.DisabledContainerBrush"] = new ImmutableSolidColorBrush(scheme.OnSurface, 0.1),
            ["M3.DisabledForegroundBrush"] = new ImmutableSolidColorBrush(scheme.OnSurfaceVariant, 0.38)
        };
    }
}
