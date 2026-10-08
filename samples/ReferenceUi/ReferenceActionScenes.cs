using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;

namespace Material3.ReferenceUi;

public sealed partial class ReferenceShell
{
    private Control CreateRipple()
    {
        var column = SceneColumn(); var count = 0;
        var counter = SetId(Text("Pressed 0"), "ripple-count");
        void Increment() => counter.Text = "Pressed " + ++count;
        column.Children.Add(counter);
        column.Children.Add(Text("Hold and release the official buttons", MaterialTypeRole.TitleMedium));
        foreach (var (label, tag, variant) in new[] {
            ("Filled button", "filled", MaterialButtonVariant.Filled), ("Elevated button", "elevated", MaterialButtonVariant.Elevated),
            ("Tonal button", "tonal", MaterialButtonVariant.Tonal), ("Outlined button", "outlined", MaterialButtonVariant.Outlined), ("Text button", "text", MaterialButtonVariant.Text) })
        {
            var button = Button(label, "ripple-" + tag, Increment, variant, true);
            // This native scene calls the expressive touch overload of each
            // family: SmallContentPadding uses10 DIP per vertical edge.
            button.Padding = new Thickness(16, 10);
            column.Children.Add(button);
        }
        var icon = IconButton("edit", "ripple-icon", "Icon button", Increment);
        var fab = SetId(new MaterialFab { Content = Symbol("add") }, "ripple-fab"); fab.Click += (_, _) => Increment();
        var row = Row(24, icon, fab); icon.VerticalAlignment = fab.VerticalAlignment = VerticalAlignment.Top;
        column.Children.Add(row); return Scroll(column);
    }
    private Control CreateButtons()
    {
        var column = SceneColumn(); var saves = 0;
        var counter = SetId(Text("Saved: 0"), "saved");
        void Save() => counter.Text = "Saved: " + ++saves;
        column.Children.Add(counter);
        column.Children.Add(Text("Unconnected — None", MaterialTypeRole.TitleMedium));
        var actions = SetId(new MaterialButtonGroup(), "button-group");
        foreach (var label in new[] { "Create", "Edit", "Share", "Disabled" })
        {
            var button = new MaterialGroupButton { Content = label, Variant = MaterialButtonVariant.Filled, IsEnabled = label != "Disabled", Padding = new Thickness(24, 8) };
            button.Click += (_, _) => Save(); actions.Children.Add(button);
        }
        column.Children.Add(actions);
        column.Children.Add(Text("Unconnected — Single", MaterialTypeRole.TitleMedium));
        var single = SetId(new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Single }, "button-group-single");
        column.Children.Add(single);
        column.Children.Add(Text("Connected — Single", MaterialTypeRole.TitleMedium));
        var connected = SetId(new MaterialButtonGroup { SelectionMode = MaterialGroupSelectionMode.Single, Variant = MaterialButtonGroupVariant.Connected }, "connected-group");
        column.Children.Add(connected);
        var singleButtons = new List<MaterialGroupButton>(); var connectedButtons = new List<MaterialGroupButton>(); var selected = 0; var synchronizing = false;
        void Select(int index)
        {
            if (synchronizing) return;
            synchronizing = true;
            try
            {
                selected = index;
                for (var choice = 0; choice < 3; choice++)
                {
                    singleButtons[choice].IsChecked = choice == selected;
                    singleButtons[choice].LeadingIcon = Symbol(choice == selected ? "check" : "favorite_border");
                    connectedButtons[choice].IsChecked = choice == selected;
                }
            }
            finally { synchronizing = false; }
        }
        foreach (var (label, index) in new[] { ("Photos", 0), ("Videos", 1), ("Audio", 2) })
        {
            var button = new ReferenceLegacyGroupButton { Content = label, Variant = MaterialButtonVariant.Filled, IsChecked = index == 0, LeadingIcon = Symbol(index == 0 ? "check" : "favorite_border"), Padding = new Thickness(16, 8, 24, 8) };
            button.Click += (_, _) => Select(index); singleButtons.Add(button); single.Children.Add(button);
            var joined = new MaterialGroupButton { Content = label, Variant = MaterialButtonVariant.Filled, IsChecked = index == 0, Padding = new Thickness(16, 10) };
            joined.Click += (_, _) => Select(index); connectedButtons.Add(joined); connected.Children.Add(joined);
        }
        foreach (var (label, size) in new[] { ("Extra small", MaterialButtonSize.ExtraSmall), ("Small", MaterialButtonSize.Small), ("Medium", MaterialButtonSize.Medium), ("Large", MaterialButtonSize.Large), ("Extra large", MaterialButtonSize.ExtraLarge) })
        {
            column.Children.Add(Text("Elevated · " + label, MaterialTypeRole.TitleMedium));
            column.Children.Add(Split(label, size, MaterialButtonVariant.Elevated, Save, false));
        }
        column.Children.Add(Split("Save to Local", MaterialButtonSize.Small, MaterialButtonVariant.Filled, Save));
        var elevated = SetId(new ReferenceContentIconButton { Content = "Elevated", Variant = MaterialButtonVariant.Elevated,
            Padding = new Thickness(16, 10), LeadingIcon = Symbol("add"), HorizontalAlignment = HorizontalAlignment.Left }, "elevated-button");
        elevated.Click += (_, _) => Save(); column.Children.Add(elevated);
        column.Children.Add(new MaterialCard { Variant = MaterialCardVariant.Elevated, Padding = new Thickness(24), Content = Text("Elevated card"), HorizontalAlignment = HorizontalAlignment.Stretch });
        return Scroll(column);
    }
    // ButtonGroup's legacy content lambda renders Icon at its native default24 DIP.
    private sealed class ReferenceLegacyGroupButton : MaterialGroupButton
    {
        protected override double GetIconSize(MaterialButtonSize size) => size == MaterialButtonSize.Small ? 24 : base.GetIconSize(size);
    }
    private sealed class ReferenceContentIconButton : MaterialButton
    {
        protected override double GetIconSize(MaterialButtonSize size) => size == MaterialButtonSize.Small ? 24 : base.GetIconSize(size);
    }
    private MaterialSplitButton Split(string label, MaterialButtonSize size, MaterialButtonVariant variant, Action save, bool fill = true)
    {
        var split = SetId(new MaterialSplitButton { Size = size, Variant = variant, HorizontalAlignment = fill ? HorizontalAlignment.Stretch : HorizontalAlignment.Left }, "split-" + label.Replace(' ', '-'));
        split.MainButton.Content = label; split.MainButton.LeadingIcon = Symbol("add", split.MainButton.IconSize);
        split.MainButton.Click += (_, _) => save();
        SetId(split.SecondaryButton, "split-toggle-" + label.Replace(' ', '-'));
        MaterialOverlaySession? menuSession = null;
        split.SecondaryButton.Click += (_, _) =>
        {
            if (!split.IsExpanded) { menuSession?.Dismiss(); return; }
            var menu = new MaterialMenu { Variant = MaterialMenuVariant.LegacyDropdown };
            foreach (var destination in new[] { "Local", "Cloud", "Shared" })
            {
                var item = new MaterialMenuItem { Content = destination };
                item.Click += (_, _) => { split.IsExpanded = false; save(); }; menu.Items.Add(item);
            }
            menuSession = menu.Show(Overlay, split.SecondaryButton);
            menuSession.Closed += (_, _) => { menuSession = null; split.IsExpanded = false; };
        };
        return split;
    }
}
