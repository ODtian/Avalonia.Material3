using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace StandaloneHost;

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
}
