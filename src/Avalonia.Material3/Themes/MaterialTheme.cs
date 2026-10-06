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

    public static readonly StyledProperty<MaterialElevation> ElevationProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialElevation>(nameof(Elevation), new(), validate: value => value is { IsValid: true });
    public static readonly StyledProperty<MaterialStates> StatesProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialStates>(nameof(States), new(), validate: value => value is { IsValid: true });

    public MaterialElevation Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    public MaterialStates States { get => GetValue(StatesProperty); set => SetValue(StatesProperty, value); }

    public static readonly StyledProperty<Color?> SeedColorProperty =
        AvaloniaProperty.Register<MaterialTheme, Color?>(nameof(SeedColor));

    public static readonly StyledProperty<MaterialDynamicColors?> DynamicColorsProperty =
        AvaloniaProperty.Register<MaterialTheme, MaterialDynamicColors?>(nameof(DynamicColors),
            validate: value => value is null or { IsValid: true });

    /// <summary>Optional brand seed. DynamicColors takes precedence; null restores explicit schemes.</summary>
    public Color? SeedColor { get => GetValue(SeedColorProperty); set => SetValue(SeedColorProperty, value); }
    public MaterialDynamicColors? DynamicColors { get => GetValue(DynamicColorsProperty); set => SetValue(DynamicColorsProperty, value); }

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
        Add(MaterialTypographyStyles.Create());
        UpdateColorInputs();
        UpdateTypography();
        UpdateShapes();
        UpdateElevation();
        UpdateStates();
        UpdateMotion();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LightColorSchemeProperty || change.Property == DarkColorSchemeProperty
            || change.Property == SeedColorProperty || change.Property == DynamicColorsProperty)
            UpdateColorInputs();
        else if (change.Property == TypographyProperty)
            UpdateTypography();
        else if (change.Property == ShapesProperty)
            UpdateShapes();
        else if (change.Property == ElevationProperty)
            UpdateElevation();
        else if (change.Property == StatesProperty)
        {
            UpdateStates();
            UpdateColorInputs();
        }
        else if (change.Property == MotionProperty)
            UpdateMotion();
    }

    private void UpdateTypography()
    {
        Resources["M3.FontFamily"] = Typography.FontFamily;
        foreach (var role in Enum.GetValues<MaterialTypeRole>())
        {
            var style = Typography.Get(role);
            Resources[$"M3.{role}FontFamily"] = Typography.GetFontFamily(role);
            Resources[$"M3.{role}FontSize"] = style.FontSize * Typography.Scale;
            Resources[$"M3.{role}LineHeight"] = style.LineHeight * Typography.Scale;
            Resources[$"M3.{role}LetterSpacing"] = style.LetterSpacing * Typography.Scale;
            Resources[$"M3.{role}FontWeight"] = style.FontWeight;
        }
    }

    private void UpdateShapes()
    {
        foreach (var radius in Shapes.GetRadii())
            Resources[$"M3.Shape.{radius.Key}"] = new CornerRadius(radius.Value);
        Resources["M3.Shape.CornerExtraSmallTop"] = new CornerRadius(Shapes.CornerExtraSmall, Shapes.CornerExtraSmall, 0, 0);
        Resources["M3.Shape.CornerLargeTop"] = new CornerRadius(Shapes.CornerLarge, Shapes.CornerLarge, 0, 0);
        Resources["M3.Shape.CornerExtraLargeTop"] = new CornerRadius(Shapes.CornerExtraLarge, Shapes.CornerExtraLarge, 0, 0);
        Resources["M3.Shape.CornerLargeStart"] = new CornerRadius(Shapes.CornerLarge, 0, 0, Shapes.CornerLarge);
        Resources["M3.Shape.CornerLargeEnd"] = new CornerRadius(0, Shapes.CornerLarge, Shapes.CornerLarge, 0);
        Resources["M3.ButtonCornerRadius"] = new CornerRadius(Shapes.ButtonCornerRadius);
        Resources["M3.PressedButtonCornerRadius"] = new CornerRadius(Shapes.PressedButtonCornerRadius);
        Resources["M3.ButtonFocusCornerRadius"] = new CornerRadius(Shapes.ButtonCornerRadius + 5);
        Resources["M3.PressedButtonFocusCornerRadius"] = new CornerRadius(Shapes.PressedButtonCornerRadius + 5);
    }

    private void UpdateElevation()
    {
        for (var level = 0; level <= 5; level++)
        {
            Resources[$"M3.Elevation.Level{level}"] = Elevation.GetLevel(level);
            Resources[$"M3.Elevation.Shadow{level}"] = Elevation.GetShadow(level);
        }
    }

    private void UpdateStates()
    {
        foreach (var opacity in States.GetOpacities()) Resources[$"M3.{opacity.Key}"] = opacity.Value;
    }

    private void UpdateMotion()
    {
        Resources["M3.StateLayerDuration"] = Motion.ReduceMotion ? TimeSpan.Zero : Motion.StateLayerDuration;
        Resources["M3.ReduceMotion"] = Motion.ReduceMotion;
        // Pinned LoadingIndicator component-local morph spring, not the scheme's SlowSpatial pair.
        Resources["M3.Motion.LoadingMorph"] = new MaterialSpring(0.6, 200) { IsInstant = Motion.ReduceMotion };
        foreach (var duration in Motion.GetDurations())
            Resources[$"M3.Motion.{duration.Key}"] = Motion.ReduceMotion ? TimeSpan.Zero : duration.Value;
        foreach (var easing in Motion.GetEasings()) Resources[$"M3.Motion.{easing.Key}"] = easing.Value.ToEasing();
        foreach (var spring in Motion.Springs.GetSprings())
            Resources[$"M3.Motion.{spring.Key}"] = spring.Value with { IsInstant = spring.Value.IsInstant || Motion.ReduceMotion };
    }

    private void UpdateColorInputs()
    {
        var light = DynamicColors?.Light ?? (SeedColor is { } seed ? MaterialColorScheme.FromSeed(seed) : LightColorScheme);
        var dark = DynamicColors?.Dark ?? (SeedColor is { } darkSeed ? MaterialColorScheme.FromSeed(darkSeed, true) : DarkColorScheme);
        UpdateColors(ThemeVariant.Light, light);
        UpdateColors(ThemeVariant.Dark, dark);
    }

    private void UpdateColors(ThemeVariant variant, MaterialColorScheme scheme)
    {
        var colors = new ResourceDictionary();
        foreach (var role in Enum.GetValues<MaterialColorRole>())
        {
            colors[$"M3.{role}Color"] = scheme.Get(role);
            colors[$"M3.{role}Brush"] = new ImmutableSolidColorBrush(scheme.Get(role));
        }
        colors["M3.DisabledContainerBrush"] = new ImmutableSolidColorBrush(scheme.OnSurface, States.DisabledButtonContainerOpacity);
        colors["M3.DisabledSurfaceContainerBrush"] = new ImmutableSolidColorBrush(scheme.OnSurface, States.DisabledContainerOpacity);
        colors["M3.DisabledForegroundBrush"] = new ImmutableSolidColorBrush(scheme.OnSurfaceVariant, States.DisabledForegroundOpacity);
        Resources.ThemeDictionaries[variant] = colors;
    }
}
