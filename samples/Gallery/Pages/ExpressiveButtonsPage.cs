using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>A package-only M3-03 demo. The aggregate gallery can host this page inside a ScrollViewer.</summary>
public sealed class ExpressiveButtonsPage : StackPanel
{
    public MaterialButton ActionButton { get; } = new()
    {
        Content = "提交 / Continue", LeadingIcon = Symbols.Create("add"), TrailingIcon = Symbols.Create("arrow_forward"), CommandParameter = "confirmed",
        MaxWidth = 260, ContentTemplate = WrappingLabel
    };
    public MaterialIconButton FavoriteButton { get; } = new()
    {
        Content = Symbols.Create("star"), IconVariant = MaterialIconButtonVariant.Filled, IsToggle = true
    };
    private static readonly IDataTemplate WrappingLabel = new FuncDataTemplate<string>((text, _) =>
        new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });

    public TextBlock Result { get; } = new() { Text = "Waiting", TextWrapping = TextWrapping.Wrap };
    public MaterialButton ThemeButton { get; } = new() { Content = "Dark theme", Variant = MaterialButtonVariant.Text, IsToggle = true };
    public MaterialButton FontScaleButton { get; } = new() { Content = "200% font", Variant = MaterialButtonVariant.Text, IsToggle = true };
    public MaterialButton DisableButton { get; } = new() { Content = "Disable actions", Variant = MaterialButtonVariant.Text, IsToggle = true };
    public MaterialButton ShapeButton { get; } = new() { Content = "Square shape", Variant = MaterialButtonVariant.Text, IsToggle = true };
    public MaterialButton MotionButton { get; } = new() { Content = "Motion: expressive", Variant = MaterialButtonVariant.Text, IsToggle = true, MaxWidth = 260, ContentTemplate = WrappingLabel };

    public ExpressiveButtonsPage()
    {
        Margin = new Thickness(24);
        Spacing = 12;
        Children.Add(new TextBlock { Text = "M3 Expressive — Buttons and icon buttons", FontSize = 24, TextWrapping = TextWrapping.Wrap });
        Children.Add(new TextBlock { Text = "Use mouse, touch, Tab, Enter or Space. Selection is committed before the action. Scroll for every size and width.", TextWrapping = TextWrapping.Wrap });
        var actions = new WrapPanel();
        actions.Children.Add(ActionButton);
        actions.Children.Add(FavoriteButton);
        Children.Add(actions);
        Children.Add(Result);
        var settings = new WrapPanel();
        foreach (var button in new[] { ThemeButton, FontScaleButton, DisableButton, ShapeButton, MotionButton }) settings.Children.Add(button);
        Children.Add(settings);
        var count = 0;
        ActionButton.Command = new DemoCommand(parameter => Result.Text = $"Action completed ({++count}): {parameter}");
        AutomationProperties.SetAutomationId(ActionButton, nameof(ActionButton));
        AutomationProperties.SetAutomationId(FavoriteButton, nameof(FavoriteButton));
        AutomationProperties.SetAutomationId(ThemeButton, nameof(ThemeButton));
        AutomationProperties.SetAutomationId(FontScaleButton, nameof(FontScaleButton));
        AutomationProperties.SetAutomationId(DisableButton, nameof(DisableButton));
        AutomationProperties.SetAutomationId(Result, nameof(Result));
        AutomationProperties.SetAutomationId(MotionButton, nameof(MotionButton));
        AutomationProperties.SetName(ActionButton, "Submit and continue");
        AutomationProperties.SetName(FavoriteButton, "Favorite");
        FavoriteButton.Click += (_, _) =>
        {
            FavoriteButton.Content = Symbols.Create("star", filled: FavoriteButton.IsChecked);
            Result.Text = FavoriteButton.IsChecked ? "Favorite: selected" : "Favorite: unselected";
        };
        ThemeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = ThemeButton.IsChecked ? ThemeVariant.Dark : ThemeVariant.Light;
        };
        FontScaleButton.Click += (_, _) =>
        {
            var theme = Application.Current!.Styles.OfType<MaterialTheme>().FirstOrDefault();
            if (theme is not null) theme.Typography = theme.Typography with { Scale = FontScaleButton.IsChecked ? 2 : 1 };
        };
        DisableButton.Click += (_, _) =>
        {
            ActionButton.IsEnabled = FavoriteButton.IsEnabled = !DisableButton.IsChecked;
        };
        ShapeButton.Click += (_, _) =>
        {
            ActionButton.Shape = FavoriteButton.Shape = ShapeButton.IsChecked ? MaterialButtonShape.Square : MaterialButtonShape.Round;
        };

        MotionButton.IsChecked = Application.Current!.Styles.OfType<MaterialTheme>().FirstOrDefault()?.Motion.ReduceMotion ?? false;
        MotionButton.Content = MotionButton.IsChecked ? "Motion: reduced" : "Motion: expressive";
        MotionButton.Click += (_, _) =>
        {
            var theme = Application.Current!.Styles.OfType<MaterialTheme>().FirstOrDefault();
            if (theme is not null) theme.Motion = theme.Motion with { ReduceMotion = MotionButton.IsChecked };
            MotionButton.Content = MotionButton.IsChecked ? "Motion: reduced" : "Motion: expressive";
        };

        AddHeading("Action recipes: normal / toggle off / toggle on / disabled");
        foreach (var variant in Enum.GetValues<MaterialButtonVariant>())
        {
            var row = new WrapPanel();
            foreach (var state in new[] { "normal", "off", "on", "disabled" })
            {
                var button = new MaterialButton
                {
                    Variant = variant, Content = $"{variant} — {state}", IsToggle = state is "off" or "on",
                    IsChecked = state == "on", IsEnabled = state != "disabled", Margin = new Thickness(2),
                    MaxWidth = 260, ContentTemplate = WrappingLabel
                };
                ConnectResult(button, $"{variant} action");
                row.Children.Add(button);
            }
            Children.Add(row);
        }
        AddHeading("Action sizes: XS / S / M / L / XL — round and square");
        foreach (var size in Enum.GetValues<MaterialButtonSize>())
        {
            var row = new WrapPanel();
            foreach (var shape in Enum.GetValues<MaterialButtonShape>())
            {
                var caption = size switch { MaterialButtonSize.ExtraSmall => "XS", MaterialButtonSize.Small => "S", MaterialButtonSize.Medium => "M", MaterialButtonSize.Large => "L", _ => "XL" };
                var button = new MaterialButton { Size = size, Shape = shape, Content = caption, MaxWidth = 260, ContentTemplate = WrappingLabel, Margin = new Thickness(2) };
                ConnectResult(button, $"{size} {shape}");
                row.Children.Add(button);
            }
            Children.Add(row);
        }
        AddHeading("Icon recipes: normal / toggle off / toggle on / disabled");
        var recipes = new WrapPanel();
        foreach (var variant in Enum.GetValues<MaterialIconButtonVariant>())
            foreach (var state in new[] { "normal", "off", "on", "disabled" })
            {
                var button = new MaterialIconButton
                {
                    IconVariant = variant, Content = Symbols.Create("star", filled: state == "on"), IsToggle = state is "off" or "on",
                    IsChecked = state == "on", IsEnabled = state != "disabled", Margin = new Thickness(4)
                };
                ConnectResult(button, $"{variant} favorite ({state})");
                recipes.Children.Add(button);
            }
        Children.Add(recipes);
        AddHeading("Icon sizes and widths: narrow / default / wide — round and square toggles");
        foreach (var size in Enum.GetValues<MaterialButtonSize>())
        {
            Children.Add(new TextBlock { Text = size.ToString() });
            var row = new WrapPanel();
            foreach (var shape in Enum.GetValues<MaterialButtonShape>())
                foreach (var width in Enum.GetValues<MaterialIconButtonWidth>())
                {
                    var button = new MaterialIconButton
                    {
                        IconVariant = MaterialIconButtonVariant.Tonal, Content = Symbols.Create("star"), IsToggle = true,
                        Size = size, Shape = shape, WidthMode = width, Margin = new Thickness(4)
                    };
                    ConnectResult(button, $"{size} {shape} {width} favorite");
                    row.Children.Add(button);
                }
            Children.Add(row);
        }
        AddHeading("Long text and mixed-language content (grows with host font scale)");
        var longLabel = new MaterialButton
        {
            Content = new TextBlock { Text = "保存修改并继续 / Save changes and continue with this longer accessible label", TextWrapping = TextWrapping.Wrap },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, LeadingIcon = Symbols.Create("add")
        };
        ConnectResult(longLabel, "Save changes and continue");
        Children.Add(longLabel);
    }

    private void AddHeading(string text) => Children.Add(new TextBlock { Text = text, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap });

    private void ConnectResult(MaterialButton button, string name)
    {
        AutomationProperties.SetName(button, name);
        button.Click += (_, _) => Result.Text = button.IsToggle ? $"{name}: {(button.IsChecked ? "selected" : "unselected")}" : $"{name}: completed";
    }

    private sealed class DemoCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
