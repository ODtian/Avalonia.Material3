using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>A standalone public-package demo; the aggregate gallery can host this page without library source references.</summary>
public sealed class ContentHierarchyPage : StackPanel
{
    public TextBlock Result { get; } = new() { Text = "Waiting", TextWrapping = TextWrapping.Wrap };
    public MaterialList List { get; } = new();
    public IReadOnlyList<MaterialListItem> Rows { get; }
    public MaterialButton ThemeButton { get; } = new() { Content = "Light / Dark" };
    public MaterialButton FontButton { get; } = new() { Content = "Font 100% / 150%" };
    public MaterialButton WidthButton { get; } = new() { Content = "Width 320 / wide" };

    public ContentHierarchyPage()
    {
        Spacing = 12;
        Margin = new Thickness(16);
        Children.Add(new TextBlock { Text = "Cards, lists, badges & dividers / 内容层级", FontSize = 24, TextWrapping = TextWrapping.Wrap });
        Children.Add(new TextBlock
        {
            Text = "Select an entry or use its own action. Alt+Enter expands; Alt+Right reveals; Alt+Up/Down reorders. Swipe left reveals, never deletes. Drag ≡ or invoke it for named move actions.",
            TextWrapping = TextWrapping.Wrap
        });
        Children.Add(new WrapPanel { Children = { ThemeButton, FontButton, WidthButton } });
        Children.Add(Result);
        ThemeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        };
        FontButton.Click += (_, _) =>
        {
            var theme = Application.Current?.Styles.OfType<MaterialTheme>().LastOrDefault();
            if (theme is not null) theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 1.5 : 1 };
        };
        WidthButton.Click += (_, _) => List.MaxWidth = double.IsPositiveInfinity(List.MaxWidth) ? 320 : double.PositiveInfinity;

        foreach (var variant in Enum.GetValues<MaterialCardVariant>())
        {
            var card = new MaterialCard
            {
                Variant = variant, Title = $"{variant} card / 标准卡片", Overline = "Collection / 合集",
                SupportingContent = "Reusable content, independent image and action slots. 中文与 English 混排。",
                Image = Preview(56), IsInteractive = variant != MaterialCardVariant.Filled,
                IsSelectable = variant != MaterialCardVariant.Filled,
                Trailing = Action("Open", $"card-{variant}")
            };
            AutomationProperties.SetName(card, $"{variant} collection card");
            card.Command = new DemoCommand(p => Result.Text = $"Activated {p}");
            card.CommandParameter = $"card-{variant}";
            Children.Add(card);
        }
        Children.Add(new MaterialDivider { InsetStart = 16, InsetEnd = 16 });
        Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Activity" }, new MaterialBadge(),
                new MaterialDivider { Orientation = Orientation.Vertical, Height = 24 },
                new TextBlock { Text = "Unread" }, new MaterialBadge { Count = 123 }
            }
        });

        var first = Row("entry-1", "One line / 一行", MaterialListLines.One);
        first.Leading = new TextBlock { Text = "●", FontSize = 24 };
        first.Trailing = new MaterialBadge { Count = 3 };
        var second = Row("entry-2", "Two lines / 带图片的条目", MaterialListLines.Two);
        second.Image = Preview(56);
        second.SupportingContent = "Supporting content / 辅助说明";
        second.Trailing = Action("Open", "entry-2");
        second.IsExpandable = true;
        second.ExpandedContent = new TextBlock { Text = "Expanded detail belongs to entry-2. 此内容可增长，不会改变其他条目的选择。", TextWrapping = TextWrapping.Wrap };
        var third = Row("entry-3", "很长的多语言标题 / A longer heading with variable height, 中文 English 日本語 and punctuation", MaterialListLines.Three);
        third.IsExpressive = true;
        third.Overline = "Featured / 推荐";
        third.Image = Preview(56);
        third.SupportingContent = string.Concat(Enumerable.Repeat("长内容不会被截断。Supporting text remains readable in a narrow window and at 150% font scale. ", 4));
        third.IsExpandable = true;
        third.ExpandedContent = "More content / 更多内容";
        third.IsRevealEnabled = true;
        third.RevealedActions = Action("Archive", "entry-3", "Archived");
        third.IsReorderEnabled = true;
        var disabled = Row("entry-4", "Disabled selected / 禁用选中", MaterialListLines.One);
        disabled.IsSelected = true;
        disabled.IsEnabled = false;
        Rows = new[] { first, second, third, disabled };
        foreach (var row in Rows)
        {
            List.Children.Add(row);
            row.Activated += (_, _) =>
            {
                foreach (var other in Rows.Where(other => other != row && other.IsEnabled)) other.IsSelected = false;
            };
        }
        List.ItemReordered += (_, e) => Result.Text = $"Reordered {e.Item.Tag}: {e.OldIndex} → {e.NewIndex}";
        Children.Add(List);
        Children.Add(new MaterialDivider());
    }

    private MaterialListItem Row(string id, string title, MaterialListLines lines)
    {
        var row = new MaterialListItem { Tag = id, Title = title, Lines = lines, IsSelectable = true, CommandParameter = id };
        row.Command = new DemoCommand(p => Result.Text = $"Selected {p}");
        AutomationProperties.SetName(row, $"{id}: {title}");
        return row;
    }
    private MaterialButton Action(string label, string id, string verb = "Opened")
    {
        var button = new MaterialButton { Content = label, CommandParameter = id, Command = new DemoCommand(p => Result.Text = $"{verb} {p}") };
        AutomationProperties.SetName(button, $"{label} {id}");
        return button;
    }
    private static Image Preview(double size)
    {
        var image = new Image
        {
            Width = size, Height = size, Stretch = Stretch.UniformToFill,
            Source = new DrawingImage
            {
                Drawing = new GeometryDrawing { Geometry = Geometry.Parse("M 0,0 L 56,0 L 56,56 L 0,56 Z"), Brush = Brushes.SeaGreen }
            }
        };
        AutomationProperties.SetName(image, "Generic image preview / 图片");
        return image;
    }
    private sealed class DemoCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
