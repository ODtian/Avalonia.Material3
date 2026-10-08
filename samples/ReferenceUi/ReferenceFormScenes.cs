using Avalonia;
using Avalonia.Controls;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Layout;
using Avalonia.Automation;
using Avalonia.VisualTree;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Material3.ReferenceUi;

public sealed partial class ReferenceShell
{
    private Control CreateSelection(bool enabled = true)
    {
        var column = SceneColumn();
        column.Children.Add(Text("Checkbox / Radio / Switch", MaterialTypeRole.TitleLarge));
        column.Children.Add(Row(0, SetId(new MaterialCheckBox { IsChecked = false, IsEnabled = enabled }, "checkbox"), Text("Notifications")));
        bool? mixedValue = null;
        var mixed = SetId(new MaterialCheckBox { IsChecked = null, IsEnabled = enabled }, "checkbox-mixed");
        mixed.Click += (_, _) => { mixedValue = mixedValue == true ? false : true; mixed.IsChecked = mixedValue; };
        column.Children.Add(Row(0, mixed, Text("Mixed state")));
        column.Children.Add(Row(0, new MaterialCheckBox { IsChecked = true, IsEnabled = false }, Text("Disabled")));
        for (var index = 0; index < 3; index++) column.Children.Add(Row(0,
            SetId(new MaterialRadioButton { GroupName = "reference-radio", IsChecked = index == 0, IsEnabled = enabled }, "radio-" + index), Text("Choice " + (index + 1))));
        var plain = SetId(new MaterialSwitch { IsChecked = true, IsEnabled = enabled }, "switch");
        var icons = SetId(new MaterialSwitch { IsChecked = true, IsEnabled = enabled, OnIcon = Symbol("check", 16), OffIcon = Symbol("close", 16) }, "switch-icons");
        var synchronizing = false;
        void Synchronize(MaterialSwitch source, MaterialSwitch destination, AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property != MaterialSwitch.IsCheckedProperty || synchronizing) return;
            synchronizing = true; try { destination.IsChecked = source.IsChecked; } finally { synchronizing = false; }
        }
        plain.PropertyChanged += (_, change) => Synchronize(plain, icons, change);
        icons.PropertyChanged += (_, change) => Synchronize(icons, plain, change);
        column.Children.Add(Row(12, plain, Text("Switch"))); column.Children.Add(Row(12, icons, Text("Thumb icons")));
        if (!enabled)
        {
            column.Children.Add(Row(12, SetId(new MaterialSwitch { IsChecked = false, IsEnabled = false }, "switch-off"), Text("Switch off")));
            column.Children.Add(Row(12, SetId(new MaterialSwitch { IsChecked = false, IsEnabled = false, OnIcon = Symbol("check", 16), OffIcon = Symbol("close", 16) }, "switch-off-icons"), Text("Thumb icons off")));
        }
        return Scroll(column);
    }
    private Control CreateSlider()
    {
        var column = SceneColumn();
        column.Children.Add(Text("Slider · continuous", MaterialTypeRole.TitleMedium));
        column.Children.Add(SetId(new MaterialSlider { Minimum = 0, Maximum = 1, Value = .27, LabelFormat = "0.##", Height = 48, MinHeight = 48, ValueLabelVisibility = SliderValueLabelVisibility.Never }, "slider"));
        column.Children.Add(Text("Volume · discrete with marks", MaterialTypeRole.TitleMedium));
        column.Children.Add(SetId(new MaterialSlider { Minimum = 0, Maximum = 100, Value = 70, Step = 10, ShowMarks = true, Height = 48, MinHeight = 48, ValueLabelVisibility = SliderValueLabelVisibility.Never }, "slider-discrete"));
        column.Children.Add(Text("Active hours · ordered range", MaterialTypeRole.TitleMedium));
        column.Children.Add(SetId(new MaterialRangeSlider { Minimum = 0, Maximum = 24, Step = 1, UpperValue = 20, LowerValue = 8, ShowMarks = true, Height = 48, MinHeight = 48, ValueLabelVisibility = SliderValueLabelVisibility.Never }, "slider-range"));
        return Scroll(column);
    }
    private Control CreateFields()
    {
        var column = SceneColumn();
        var outlined = SetId(new MaterialTextField { Variant = MaterialTextFieldVariant.Outlined, Label = "Display name", SupportingText = "Enter a non-empty name", Text = "" }, "field-outlined");
        var amount = SetId(new MaterialTextField { Variant = MaterialTextFieldVariant.Outlined, Label = "Amount / 金额", InnerLeftContent = Symbol("diamond"), InnerRightContent = Symbol("check"), PrefixText = "¥", SuffixText = "CNY", Text = "" }, "field-amount");
        var filled = SetId(new MaterialTextField { Label = "Sheet editor", SupportingText = "Editable draft", Text = "" }, "field-filled");
        var fields = new[] { outlined, amount, filled }; var synchronizing = false;
        foreach (var field in fields)
        {
            field.TextChanged += (_, _) =>
            {
                if (synchronizing) return;
                synchronizing = true;
                try { foreach (var other in fields) if (other != field) other.Text = field.Text; }
                finally { synchronizing = false; }
            };
            column.Children.Add(field);
        }
        return Scroll(column);
    }
    private MaterialDatePicker DatePicker(MaterialDateSelectionMode selection, int start, int? end = null) => new()
    {
        SelectionMode = selection, Culture = _configuration.Culture, DisplayMonth = new DateOnly(2024, 2, 1),
        MinimumDate = new DateOnly(2024, 1, 1), MaximumDate = new DateOnly(2024, 12, 31),
        SelectedDate = new DateOnly(2024, 2, start), RangeEnd = end is { } day ? new DateOnly(2024, 2, day) : null,
        SelectableDate = selection == MaterialDateSelectionMode.Range ? day => day != new DateOnly(2024, 2, 20) : null
    };
    private Control CreateDateSingle()
    {
        var picker = SetId(DatePicker(MaterialDateSelectionMode.Single, 9), "date-picker");
        picker.VerticalAlignment = VerticalAlignment.Top;
        return picker;
    }
    private Control CreateDateRange(string scene)
    {
        var (start, end) = scene switch { "date-range-7-24" => (7, 24), "date-range-9-16" => (9, 16), _ => (10, 12) };
        var picker = SetId(DatePicker(MaterialDateSelectionMode.Range, start, end), "date-range-picker");
        var selected = SetId(Text($"{start} – {end}"), "selected-range"); selected.Margin = new Thickness(16, 0);
        picker.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialDatePicker.SelectedDateProperty || change.Property == MaterialDatePicker.RangeEndProperty)
                selected.Text = $"{picker.SelectedDate?.Day.ToString() ?? "null"} – {picker.RangeEnd?.Day.ToString() ?? "null"}";
        };
        var actions = Row(8,
            Button("Reset 10–12", "reset-range", () => { picker.SelectedDate = new DateOnly(2024, 2, 10); picker.RangeEnd = new DateOnly(2024, 2, 12); }, MaterialButtonVariant.Text),
            Button("Dialog", "open-date-dialog", () => ShowDateDialog(picker), MaterialButtonVariant.Text));
        actions.Margin = new Thickness(12, 0);
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), Children = { actions, selected, picker } };
        Grid.SetRow(selected, 1); Grid.SetRow(picker, 2); return root;
    }
    private void ShowDateDialog(MaterialDatePicker inline)
    {
        var picker = SetId(DatePicker(MaterialDateSelectionMode.Range, inline.SelectedDate?.Day ?? 10, inline.RangeEnd?.Day), "date-range-dialog-picker");
        picker.SelectedDate = inline.SelectedDate; picker.RangeEnd = inline.RangeEnd; picker.DisplayMonth = inline.DisplayMonth; picker.Mode = inline.Mode;
        var synchronizing = false;
        void Sync(MaterialDatePicker source, MaterialDatePicker destination, AvaloniaPropertyChangedEventArgs change)
        {
            if (synchronizing || (change.Property != MaterialDatePicker.SelectedDateProperty && change.Property != MaterialDatePicker.RangeEndProperty && change.Property != MaterialDatePicker.ModeProperty && change.Property != MaterialDatePicker.DisplayMonthProperty)) return;
            synchronizing = true;
            try
            {
                var start = source.SelectedDate; var end = source.RangeEnd;
                if (source.Mode == MaterialDatePickerMode.Input && (start is null || end is { } last && last < start))
                {
                    start = end = null;
                    source.SelectedDate = null; source.RangeEnd = null;
                }
                destination.SelectedDate = start; destination.RangeEnd = end; destination.Mode = source.Mode; destination.DisplayMonth = source.DisplayMonth;
            }
            finally { synchronizing = false; }
        }
        EventHandler<AvaloniaPropertyChangedEventArgs> fromInline = (_, change) => Sync(inline, picker, change);
        EventHandler<AvaloniaPropertyChangedEventArgs> fromDialog = (_, change) => Sync(picker, inline, change);
        inline.PropertyChanged += fromInline; picker.PropertyChanged += fromDialog;
        // The native comparison host closes OK unconditionally and retains shared drafts.
        // The library's MaterialDatePicker.Show remains the validated form entry point.
        var dialog = new MaterialDialog { Content = picker, Padding = default, MaxWidth = 360, MaxHeight = 568,
            Template = new FuncControlTemplate<MaterialDialog>((owner, scope) =>
            {
                var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8, Margin = new Thickness(0, 0, 6, 8) };
                foreach (var confirm in new[] { false, true })
                {
                    var button = new MaterialButton { Content = confirm ? "OK" : "Cancel", Variant = MaterialButtonVariant.Text };
                    AutomationProperties.SetName(button, confirm ? "OK" : "Cancel");
                    button.Click += (_, _) => { if (confirm) owner.Confirm(); else owner.Cancel(); };
                    actions.Children.Add(button);
                }
                var body = new ContentPresenter { Content = owner.Content, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch };
                var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { body, actions } };
                Grid.SetRow(actions, 1);
                var clip = new Border { Child = grid, CornerRadius = new CornerRadius(28), ClipToBounds = true };
                var surface = new Border { Child = clip, CornerRadius = new CornerRadius(28) };
                surface.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
                surface.Bind(Border.BoxShadowProperty, new DynamicResourceExtension("M3.Elevation.Shadow3"));
                return surface;
            }) };
        var session = dialog.Show(Overlay, new MaterialOverlayOptions { Margin = new Thickness(0, _safeArea.Top, 0, _safeArea.Bottom) });
        session.Closed += (_, _) => { inline.PropertyChanged -= fromInline; picker.PropertyChanged -= fromDialog; };
    }
    private Control CreateTime()
    {
        var picker = SetId(new MaterialTimePicker { SelectedTime = new TimeOnly(19, 7), Is24Hour = false, Culture = _configuration.Culture }, "time-picker");
        picker.PropertyChanged += (_, change) => { if (change.Property == MaterialTimePicker.ModeProperty) SetId(picker, picker.Mode == MaterialTimePickerMode.Input ? "time-input" : "time-picker"); };
        var actions = Row(8, Button("Dialog", "open-time-dialog", () => ShowTimeDialog(picker), MaterialButtonVariant.Text),
            Button("Keyboard", "time-input-toggle", () =>
            {
                picker.Mode = picker.Mode == MaterialTimePickerMode.Clock ? MaterialTimePickerMode.Input : MaterialTimePickerMode.Clock;
            }, MaterialButtonVariant.Text));
        actions.Margin = new Thickness(12);
        var box = new Border { Margin = new Thickness(16), Child = picker, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top };
        picker.HorizontalAlignment = HorizontalAlignment.Center;
        return new StackPanel { Children = { actions, box } };
    }
    private void ShowTimeDialog(MaterialTimePicker inline)
    {
        var picker = new MaterialTimePicker { SelectedTime = inline.SelectedTime, Is24Hour = false, Culture = _configuration.Culture, Mode = inline.Mode, ActivePart = inline.ActivePart };
        var synchronizing = false;
        void Sync(MaterialTimePicker source, MaterialTimePicker destination, AvaloniaPropertyChangedEventArgs change)
        {
            if (synchronizing || (change.Property != MaterialTimePicker.SelectedTimeProperty && change.Property != MaterialTimePicker.ModeProperty && change.Property != MaterialTimePicker.ActivePartProperty)) return;
            synchronizing = true;
            try { destination.SelectedTime = source.SelectedTime; destination.Mode = source.Mode; destination.ActivePart = source.ActivePart; }
            finally { synchronizing = false; }
        }
        EventHandler<AvaloniaPropertyChangedEventArgs> fromInline = (_, change) => Sync(inline, picker, change);
        EventHandler<AvaloniaPropertyChangedEventArgs> fromDialog = (_, change) => Sync(picker, inline, change);
        inline.PropertyChanged += fromInline; picker.PropertyChanged += fromDialog;
        var session = picker.Show(Overlay);
        EventHandler? tagMode = null;
        tagMode = (_, _) =>
        {
            var mode = Overlay.GetVisualDescendants().OfType<MaterialIconButton>().FirstOrDefault(button => AutomationProperties.GetName(button) is "Enter time" or "Show clock");
            if (mode is null) return;
            SetId(mode, "time-dialog-mode"); Overlay.LayoutUpdated -= tagMode;
        };
        Overlay.LayoutUpdated += tagMode;
        session.Closed += (_, _) => { Overlay.LayoutUpdated -= tagMode; inline.PropertyChanged -= fromInline; picker.PropertyChanged -= fromDialog; };
    }
}
