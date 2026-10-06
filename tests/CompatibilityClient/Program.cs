using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;

AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
var scheme = new MaterialColorScheme(Colors.Red, Colors.White, Colors.Black, Colors.White, Colors.Gray, Colors.Teal);
var (primary, _, surface, _, _, outline) = scheme;
if (primary != Colors.Red || surface != Colors.Black || outline != Colors.Teal ||
    (scheme with { Primary = Colors.Blue }).Primary != Colors.Blue) throw new Exception("Six-color binary contract broke.");
var theme = new MaterialTheme { Typography = new MaterialTypography { Scale = 1.5 },
    Shapes = new MaterialShapes { ButtonCornerRadius = 20 }, Motion = new MaterialMotion { ReduceMotion = true } };
Application.Current!.Styles.Add(theme);
var window = new CompatFixture.CompatibilityWindow();
window.Show();
var button = window.FindControl<MaterialButton>("InitialButton")!;
if (button.Background is not ISolidColorBrush { Color: var light } || light != Color.Parse("#6750A4"))
    throw new Exception("Initial light resource/XAML contract broke.");
window.RequestedThemeVariant = ThemeVariant.Dark;
if (button.Background is not ISolidColorBrush { Color: var dark } || dark != Color.Parse("#D0BCFF"))
    throw new Exception("Initial dark resource/XAML contract broke.");
window.Close();
Console.WriteLine("PASS immutable-old compiled client: ctor/deconstruct/with, compiled XAML, theme/resources.");

namespace CompatFixture
{
    public partial class CompatibilityWindow : Window
    {
        public CompatibilityWindow() => AvaloniaXamlLoader.Load(this);
    }
}
