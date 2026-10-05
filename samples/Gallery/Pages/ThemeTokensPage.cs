using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Standalone public-package theme demonstration. Install the supplied theme before displaying this page.</summary>
public sealed class ThemeTokensPage : UserControl
{
    public MaterialButton SeedButton { get; } = new() { Content = "种子色 / Seed" };
    public MaterialButton ModeButton { get; } = new() { Content = "明暗 / Light–dark" };
    public MaterialButton PlatformButton { get; } = new() { Content = "平台调色 / Platform colors" };
    public MaterialButton ScaleButton { get; } = new() { Content = "字体 100/150/200% / Text scale" };
    public MaterialButton ShapeButton { get; } = new() { Content = "形状 / Shape" };
    public MaterialButton MotionButton { get; } = new() { Content = "减少动效 / Reduce motion" };
    public MaterialButton PreviewButton { get; } = new() { Content = "继续 / Continue" };
    public TextBlock MixedText { get; } = new()
    {
        Text = "中文与 Latin 123 混排：在窄窗口与 200% 字体下，长文本应自动换行。The quick brown fox jumps over the lazy dog. 宿主提供字体回退，不把缺字当作完整跨平台验证。",
        TextWrapping = TextWrapping.Wrap
    };

    public ThemeTokensPage(MaterialTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var stack = new StackPanel { Spacing = 12, Margin = new Thickness(16), MaxWidth = 900 };
        var title = new TextBlock { Text = "完整设计令牌 / Theme tokens", TextWrapping = TextWrapping.Wrap };
        title.Classes.Add("m3-headline-small");
        stack.Children.Add(title);
        var actions = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        foreach (var button in new[] { SeedButton, ModeButton, PlatformButton, ScaleButton, ShapeButton, MotionButton })
            actions.Children.Add(button);
        stack.Children.Add(actions);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        status.Classes.Add("m3-body-medium");
        stack.Children.Add(status);
        stack.Children.Add(PreviewButton);
        stack.Children.Add(new MaterialButton { Content = "不可用 / Disabled", IsEnabled = false });
        MixedText.Classes.Add("m3-body-large-emphasized");
        stack.Children.Add(MixedText);
        var seeds = new[] { "#006C4C", "#6750A4", "#FF0000", "#0000FF" };
        var seedIndex = 0;
        SeedButton.Click += (_, _) => { theme.SeedColor = Color.Parse(seeds[seedIndex++ % seeds.Length]); UpdateStatus(); };
        ModeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
            UpdateStatus();
        };
        PlatformButton.Click += (_, _) =>
        {
            // A deterministic host adapter sample, not an assertion that Windows supplies Android wallpaper palettes.
            theme.DynamicColors = theme.DynamicColors is null
                ? MaterialDynamicColors.FromPalette(new(new(145, 40), new(145, 16), new(205, 24), new(145, 4), new(145, 8), new(25, 84)))
                : null;
            UpdateStatus();
        };
        ScaleButton.Click += (_, _) =>
        {
            theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 1.5 : theme.Typography.Scale == 1.5 ? 2 : 1 };
            UpdateStatus();
        };
        ShapeButton.Click += (_, _) =>
        {
            theme.Shapes = theme.Shapes with { ButtonCornerRadius = theme.Shapes.ButtonCornerRadius == 20 ? 12 : 20, CornerMedium = theme.Shapes.CornerMedium == 12 ? 18 : 12 };
            UpdateStatus();
        };
        MotionButton.Click += (_, _) => { theme.Motion = theme.Motion with { ReduceMotion = !theme.Motion.ReduceMotion }; UpdateStatus(); };

        foreach (var role in Enum.GetValues<MaterialColorRole>())
        {
            var swatch = new Border { Height = 32, MinWidth = 48 };
            swatch.Bind(Border.BackgroundProperty, swatch.GetResourceObservable($"M3.{role}Brush"));
            var row = new StackPanel { Spacing = 4 };
            var label = new TextBlock { Text = role.ToString(), TextWrapping = TextWrapping.Wrap };
            label.Classes.Add("m3-label-medium");
            row.Children.Add(label);
            row.Children.Add(swatch);
            stack.Children.Add(row);
        }
        foreach (var role in Enum.GetValues<MaterialTypeRole>())
        {
            var text = new TextBlock { Text = $"{role}: 中文 / Latin Aa 123", TextWrapping = TextWrapping.Wrap };
            text.Bind(TextBlock.FontFamilyProperty, text.GetResourceObservable($"M3.{role}FontFamily"));
            text.Bind(TextBlock.FontSizeProperty, text.GetResourceObservable($"M3.{role}FontSize"));
            text.Bind(TextBlock.FontWeightProperty, text.GetResourceObservable($"M3.{role}FontWeight"));
            text.Bind(TextBlock.LineHeightProperty, text.GetResourceObservable($"M3.{role}LineHeight"));
            text.Bind(TextBlock.LetterSpacingProperty, text.GetResourceObservable($"M3.{role}LetterSpacing"));
            text.Bind(TextBlock.ForegroundProperty, text.GetResourceObservable("M3.OnSurfaceBrush"));
            stack.Children.Add(text);
        }
        foreach (var radius in theme.Shapes.GetRadii())
        {
            var shape = new Border { Height = 56, Padding = new Thickness(12), Child = new TextBlock { Text = radius.Key } };
            shape.Bind(Border.BackgroundProperty, shape.GetResourceObservable("M3.SurfaceContainerHighBrush"));
            shape.Bind(Border.CornerRadiusProperty, shape.GetResourceObservable($"M3.Shape.{radius.Key}"));
            stack.Children.Add(shape);
        }
        for (var level = 0; level <= 5; level++)
        {
            var surface = new Border { Padding = new Thickness(16), Margin = new Thickness(8), Child = new TextBlock { Text = $"Elevation {level}" } };
            surface.Bind(Border.BackgroundProperty, surface.GetResourceObservable("M3.SurfaceContainerBrush"));
            surface.Bind(Border.BoxShadowProperty, surface.GetResourceObservable($"M3.Elevation.Shadow{level}"));
            stack.Children.Add(surface);
        }
        Content = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = stack };
        UpdateStatus();

        void UpdateStatus()
        {
            var scheme = theme.DynamicColors?.Light ?? (theme.SeedColor is { } seed ? MaterialColorScheme.FromSeed(seed) : theme.LightColorScheme);
            status.Text = $"Platform override: {theme.DynamicColors is not null}; seed: {theme.SeedColor}; scale: {theme.Typography.Scale:P0}; reduce motion: {theme.Motion.ReduceMotion}. "
                + $"Light primary contrast: {MaterialColorContrast.Ratio(scheme.OnPrimary, scheme.Primary):F2}:1 (normal text needs 4.5:1). "
                + "Hover, press and keyboard-focus the preview. All 49 colors, 30 type roles, shapes and elevation surfaces below use live resources. Spring tokens are parameters; component animation drivers own playback.";
        }
    }
}
