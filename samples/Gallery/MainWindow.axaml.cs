using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Material3.Themes;
using Avalonia.Styling;

namespace Gallery;

public partial class MainWindow : Window
{
    public static readonly StyledProperty<string> FeedbackProperty =
        AvaloniaProperty.Register<MainWindow, string>(nameof(Feedback), "Waiting");
    public static readonly StyledProperty<string> ThemeCaptionProperty =
        AvaloniaProperty.Register<MainWindow, string>(nameof(ThemeCaption), "Use dark theme");
    private int _actionCount;

    public string Feedback
    {
        get => GetValue(FeedbackProperty);
        private set => SetValue(FeedbackProperty, value);
    }

    public string ThemeCaption
    {
        get => GetValue(ThemeCaptionProperty);
        private set => SetValue(ThemeCaptionProperty, value);
    }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = this;
    }

    private void OnAction(object? sender, RoutedEventArgs args) => Feedback = $"Action completed ({++_actionCount})";

    private void OnTheme(object? sender, RoutedEventArgs args)
    {
        RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        ThemeCaption = RequestedThemeVariant == ThemeVariant.Dark ? "Use light theme" : "Use dark theme";
    }

    private void OnFontScale(object? sender, RoutedEventArgs args)
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 1.5 : 1 };
    }

    private void OnMotion(object? sender, RoutedEventArgs args)
    {
        var theme = Application.Current!.Styles.OfType<MaterialTheme>().Single();
        theme.Motion = theme.Motion with { ReduceMotion = !theme.Motion.ReduceMotion };
    }
}
