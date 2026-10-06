using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Package-only, independently hostable selection and split-action gallery.</summary>
public sealed class ButtonGroupsPage : StackPanel
{
    public MaterialSegmentedButtonGroup TimeRange { get; } = new();
    public MaterialButtonGroup MediaTypes { get; } = new() { Variant = MaterialButtonGroupVariant.Connected, SelectionMode = MaterialGroupSelectionMode.Multiple };
    public MaterialSplitButton PrimarySplit { get; } = new();
    public TextBlock SelectionResult { get; } = new() { Text = "Range: Day" };
    public TextBlock ActionResult { get; } = new() { Text = "Saved: 0" };
    public MaterialButton DisableButton { get; } = new() { Content = "Enable / disable main" };
    public MaterialButton ModeButton { get; } = new() { Content = "Light / dark" };
    public MaterialButton FontButton { get; } = new() { Content = "100 / 200% text" };
    public MaterialButton MotionButton { get; } = new() { Content = "Reduce motion", IsToggle = true };
    public IReadOnlyList<MaterialSplitButton> SplitVariants => _splits;
    private readonly List<MaterialSplitButton> _splits = [];
    private readonly StackPanel _options = new() { IsVisible = false, Spacing = 4 };
    public ButtonGroupsPage(MaterialTheme theme)
    {
        Spacing = 12; Margin = new Thickness(16);
        Children.Add(new TextBlock { Text = "Button groups, segmented options & split actions", TextWrapping = TextWrapping.Wrap, FontSize = 24 });
        var settings = new WrapPanel { Orientation = Orientation.Horizontal, Children = { ModeButton, FontButton, MotionButton, DisableButton } };
        Children.Add(settings);
        var wrappingLabel = new Avalonia.Controls.Templates.FuncDataTemplate<object>((value, _) => new TextBlock { Text = value?.ToString(), TextWrapping = TextWrapping.Wrap });
        foreach (var button in settings.Children.OfType<MaterialButton>()) button.ContentTemplate = wrappingLabel;
        MotionButton.IsChecked = theme.Motion.ReduceMotion;
        DisableButton.Click += (_, _) => PrimarySplit.MainButton.IsEnabled = !PrimarySplit.MainButton.IsEnabled;
        AutomationProperties.SetAutomationId(DisableButton, "group-disable");
        ModeButton.Click += (_, _) => { if (TopLevel.GetTopLevel(this) is Window window) window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark; };
        FontButton.Click += (_, _) => theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 2 : 1 };
        MotionButton.Click += (_, _) => theme.Motion = theme.Motion with { ReduceMotion = MotionButton.IsChecked };
        AutomationProperties.SetAutomationId(ModeButton, "group-mode");
        AutomationProperties.SetAutomationId(FontButton, "group-font");
        AutomationProperties.SetAutomationId(SelectionResult, "group-result");
        AutomationProperties.SetAutomationId(ActionResult, "split-result");
        AutomationProperties.SetName(TimeRange, "Time range");
        foreach (var label in new[] { "Day", "Week", "Month" }) TimeRange.Children.Add(Option(label));
        TimeRange.SelectionChanged += (_, _) => SelectionResult.Text = "Range: " + TimeRange.SelectedItem?.Content;
        Children.Add(TimeRange); Children.Add(SelectionResult);
        foreach (var label in new[] { "Photos", "Videos", "Audio" }) MediaTypes.Children.Add(Option(label));
        var mediaResult = new TextBlock { Text = "Media: none", TextWrapping = TextWrapping.Wrap };
        MediaTypes.SelectionChanged += (_, _) => mediaResult.Text = "Media: " + string.Join(", ", MediaTypes.SelectedItems.Select(item => item.Content));
        Children.Add(MediaTypes); Children.Add(mediaResult);
        PrimarySplit.MainButton.Content = "Save document";
        AutomationProperties.SetAutomationId(PrimarySplit.MainButton, "split-main");
        AutomationProperties.SetAutomationId(PrimarySplit.SecondaryButton, "split-secondary");
        AutomationProperties.SetName(PrimarySplit.SecondaryButton, "Save options");
        var saved = 0;
        PrimarySplit.MainButton.Command = new ActionCommand(_ => ActionResult.Text = "Saved: " + ++saved);
        PrimarySplit.PropertyChanged += (_, e) => { if (e.Property == MaterialSplitButton.IsExpandedProperty) _options.IsVisible = PrimarySplit.IsExpanded; };
        var destination = new MaterialSegmentedButtonGroup();
        foreach (var label in new[] { "Local", "Cloud" }) destination.Children.Add(Option(label));
        destination.SelectionChanged += (_, _) => PrimarySplit.MainButton.Content = "Save to " + destination.SelectedItem?.Content;
        _options.Children.Add(new TextBlock { Text = "Choose a destination (Escape returns focus)", TextWrapping = TextWrapping.Wrap });
        _options.Children.Add(destination);
        Children.Add(PrimarySplit); Children.Add(_options); Children.Add(ActionResult);
        KeyDown += (_, e) => { if (e.Key == Key.Escape && PrimarySplit.IsExpanded) { PrimarySplit.IsExpanded = false; PrimarySplit.SecondaryButton.Focus(NavigationMethod.Directional); e.Handled = true; } };
        foreach (var variant in Enum.GetValues<MaterialButtonGroupVariant>())
        {
            foreach (var mode in Enum.GetValues<MaterialGroupSelectionMode>())
            {
                AddHeading($"{variant} — {mode}");
                var group = new MaterialButtonGroup { Variant = variant, SelectionMode = mode };
                foreach (var label in new[] { "Create", "Edit", "Share", "Disabled" }) group.Children.Add(new MaterialGroupButton { Content = label, IsEnabled = label != "Disabled", Variant = MaterialButtonVariant.Tonal });
                Children.Add(group);
            }
        }
        AddHeading("Segmented single & multiple (selected, icons, disabled)");
        foreach (var mode in new[] { MaterialGroupSelectionMode.Single, MaterialGroupSelectionMode.Multiple })
        {
            var group = new MaterialSegmentedButtonGroup { SelectionMode = mode };
            group.Children.Add(new MaterialGroupButton { Content = "List", LeadingIcon = "≡", IsChecked = true });
            group.Children.Add(new MaterialGroupButton { Content = "Grid", LeadingIcon = "▦" });
            group.Children.Add(new MaterialGroupButton { Content = "Disabled", IsEnabled = false });
            Children.Add(group);
        }
        AddHeading("Vertical & RTL connected options");
        var vertical = new MaterialButtonGroup { Variant = MaterialButtonGroupVariant.Connected, Orientation = Orientation.Vertical, SelectionMode = MaterialGroupSelectionMode.Single };
        vertical.Children.Add(Option("Top")); vertical.Children.Add(Option("Bottom")); Children.Add(vertical);
        var rtl = new MaterialButtonGroup { Variant = MaterialButtonGroupVariant.Connected, FlowDirection = FlowDirection.RightToLeft, SelectionMode = MaterialGroupSelectionMode.Multiple };
        rtl.Children.Add(Option("Start / 开始")); rtl.Children.Add(Option("End / 结束")); Children.Add(rtl);
        AddHeading("All split recipes × XS / S / M / L / XL");
        foreach (var variant in new[] { MaterialButtonVariant.Filled, MaterialButtonVariant.Tonal, MaterialButtonVariant.Elevated, MaterialButtonVariant.Outlined })
        {
            foreach (var size in Enum.GetValues<MaterialButtonSize>())
            {
                var split = new MaterialSplitButton { Variant = variant, Size = size };
                split.MainButton.Content = $"{variant} {size}";
                split.MainButton.LeadingIcon = "+";
                split.MainButton.Command = PrimarySplit.MainButton.Command;
                AutomationProperties.SetName(split.SecondaryButton, $"{variant} {size} options");
                _splits.Add(split); Children.Add(split);
            }
        }
        AddHeading("Non-checkable secondary action and text secondary slot");
        var plain = new MaterialSplitButton { Variant = MaterialButtonVariant.Outlined, SecondaryIsToggle = false };
        plain.MainButton.Content = "Run"; plain.SecondaryButton.UseIconContent = false; plain.SecondaryButton.Content = "Other";
        plain.MainButton.Command = plain.SecondaryButton.Command = PrimarySplit.MainButton.Command;
        Children.Add(plain);
        AddHeading("Dynamic, long and mixed content — narrow-window overflow & reflow");
        var dynamic = new MaterialButtonGroup { Variant = MaterialButtonGroupVariant.Connected, SelectionMode = MaterialGroupSelectionMode.Multiple };
        dynamic.Children.Add(Option("Mixed 中文 — choose a long and descriptive label that can wrap"));
        dynamic.Children.Add(Option("Second option"));
        var change = new MaterialButton { Content = "Add / remove dynamic option", ContentTemplate = wrappingLabel };
        change.Click += (_, _) => { if (dynamic.Children.Count == 2) dynamic.Children.Add(Option("Added at runtime — dynamically chosen content")); else dynamic.Children.RemoveAt(2); };
        Children.Add(change); Children.Add(dynamic);
        var overflow = new MaterialButtonGroup();
        foreach (var label in new[] { "Create a new document", "Duplicate current document", "Export mixed 中文 content" })
        { var button = Option(label); button.Command = PrimarySplit.MainButton.Command; overflow.Children.Add(button); }
        Children.Add(overflow);
        var longSplit = new MaterialSplitButton { Variant = MaterialButtonVariant.Tonal };
        longSplit.MainButton.Content = "Save the updated mixed 中文 document to your chosen destination with a long label";
        longSplit.MainButton.Command = PrimarySplit.MainButton.Command;
        Children.Add(longSplit);
    }
    private void AddHeading(string text) => Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.Bold });
    private static MaterialGroupButton Option(string text) => new() { Content = text };
    private sealed class ActionCommand(Action<object?> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
