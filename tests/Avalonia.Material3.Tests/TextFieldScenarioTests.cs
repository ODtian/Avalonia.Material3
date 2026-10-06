using System.Runtime.InteropServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class TextFieldScenarioTests
{
    [AvaloniaFact]
    public void Text_input_for_an_interactive_slot_does_not_modify_the_editor_value()
    {
        var slot = new MaterialButton { Content = "Slot action" };
        var invocations = 0;
        slot.Click += (_, _) => invocations++;
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name", Text = "kept", InnerRightContent = slot });
        host.Press(PhysicalKey.Tab);
        host.Press(PhysicalKey.Tab);
        Assert.True(slot.IsFocused);
        host.Press(PhysicalKey.Backspace);
        Assert.Equal("kept", host.Field.Text);
        host.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        host.Window.KeyTextInput(" "); // Desktop Space emits text as well as the key events.
        host.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal(1, invocations);
        Assert.Equal("kept", host.Field.Text);
    }

    [AvaloniaFact]
    public void Keyboard_focus_on_clear_does_not_resize_the_editor_or_clear_target()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name", Text = "value", ShowClearButton = true });
        host.Press(PhysicalKey.Tab);
        host.Capture();
        var clear = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "Clear text");
        var before = clear.Bounds.Size;
        host.Press(PhysicalKey.Tab);
        host.Capture();
        Assert.True(clear.IsFocused);
        Assert.Equal(before, clear.Bounds.Size);
        Assert.Equal(48, clear.Bounds.Width);
    }

    [AvaloniaFact]
    public void Existing_field_follows_semantic_color_shape_and_action_state_tokens()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name", Text = "value", ShowClearButton = true });
        host.Theme.Shapes = new MaterialShapes { CornerExtraSmall = 12 };
        host.Theme.LightColorScheme = MaterialColorScheme.Light with
        {
            SurfaceContainerHighest = Color.Parse("#E8F3EA"), Error = Color.Parse("#004D40")
        };
        host.Theme.States = new MaterialStates { HoverStateLayerOpacity = 0.2 };
        host.Capture();
        Assert.Equal(new CornerRadius(12, 12, 0, 0), host.Field.CornerRadius);
        Assert.Equal(Color.Parse("#E8F3EA"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Field.Background).Color);
        var clear = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "Clear text");
        var point = clear.TranslatePoint(new Point(8, 24), host.Window)!.Value;
        host.Window.MouseMove(point);
        var pixel = host.PixelAt(point);
        // Worked sRGB source-over vector: (200,208,203); allow one 8-bit rasterization unit.
        Assert.InRange(pixel.R, (byte)199, (byte)201);
        Assert.InRange(pixel.G, (byte)207, (byte)209);
        Assert.InRange(pixel.B, (byte)202, (byte)204);
        host.Window.MouseMove(new Point(0, 0));
        host.Field.ErrorText = "Correct the value";
        Assert.Equal(Color.Parse("#004D40"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Field.BorderBrush).Color);
        host.Field.Variant = MaterialTextFieldVariant.Outlined;
        Assert.Equal(new CornerRadius(12), host.Field.CornerRadius);
    }

    [AvaloniaFact]
    public void Existing_editor_and_feedback_follow_full_body_typography_roles()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name", SupportingText = "Details", ShowCounter = true });
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("中文 Atlas");
        host.Theme.Typography = new MaterialTypography
        {
            BodyLarge = new MaterialTypeStyle(20, 28, 0.7, FontWeight.Medium) { FontFamily = new FontFamily("Arial") },
            BodySmall = new MaterialTypeStyle(14, 22, 0.3, FontWeight.Bold) { FontFamily = new FontFamily("Courier New") }
        };
        host.Capture();
        Assert.Equal(new FontFamily("Arial"), host.Field.FontFamily);
        Assert.Equal(20, host.Field.FontSize);
        Assert.Equal(28, host.Field.LineHeight);
        Assert.Equal(0.7, host.Field.LetterSpacing);
        Assert.Equal(FontWeight.Medium, host.Field.FontWeight);
        foreach (var text in host.Window.GetVisualDescendants().OfType<TextBlock>()
                     .Where(text => text.IsEffectivelyVisible && text.Text is "Name" or "Details" or "8"))
        {
            Assert.Equal(14, text.FontSize);
            Assert.Equal(22, text.LineHeight);
            Assert.Equal(new FontFamily("Courier New"), text.FontFamily);
            Assert.Equal(FontWeight.Bold, text.FontWeight);
            Assert.Equal(0.3, text.LetterSpacing);
        }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void Outlined_label_is_centred_on_the_visible_outline_instead_of_shifted_up(double scale)
    {
        using var host = new TextFieldHost(new MaterialTextField { Variant = MaterialTextFieldVariant.Outlined, Label = "Email / 邮箱" });
        host.Theme.Typography = new MaterialTypography { Scale = scale };
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("atlas@example.test");
        host.Capture();
        var label = host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.Text == "Email / 邮箱" && text.IsEffectivelyVisible);
        var labelCentre = label.TranslatePoint(new Point(0, label.Bounds.Height / 2), host.Field)!.Value.Y;
        var outlineTop = Enumerable.Range(0, (int)(host.Field.Bounds.Height / 2))
            .First(y => host.PixelAt(host.Field.TranslatePoint(new Point(host.Field.Bounds.Width - 16, y), host.Window)!.Value)
                == Color.Parse("#6750A4"));
        Assert.InRange(Math.Abs(labelCentre - outlineTop), 0, 1);
        var outlineBottom = Enumerable.Range(outlineTop, (int)host.Field.Bounds.Height - outlineTop)
            .Last(y => host.PixelAt(host.Field.TranslatePoint(new Point(host.Field.Bounds.Width - 16, y), host.Window)!.Value)
                == Color.Parse("#6750A4"));
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        host.Field.RaiseEvent(request);
        var textView = request.Client!.TextViewVisual;
        var editorCentre = textView.TranslatePoint(new Point(0, textView.Bounds.Height / 2), host.Field)!.Value.Y;
        Assert.InRange(Math.Abs(editorCentre - (outlineTop + outlineBottom) / 2d), 0, 1);
    }

    [AvaloniaFact]
    public void Dark_clear_action_hover_uses_the_semantic_foreground_state_layer()
    {
        using var host = new TextFieldHost(new MaterialTextField { Text = "value", ShowClearButton = true });
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Capture();
        var clear = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "Clear text");
        var point = clear.TranslatePoint(new Point(8, 24), host.Window)!.Value;
        host.Window.MouseMove(point);
        Assert.Equal(Color.Parse("#424047"), host.PixelAt(point)); // OnSurfaceVariant at .08 over SurfaceContainerHighest.
    }

    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled)]
    [InlineData(MaterialTextFieldVariant.Outlined)]
    public void Error_feedback_and_trailing_content_follow_the_error_role(MaterialTextFieldVariant variant)
    {
        var trailing = new TextBlock { Text = "!" };
        using var host = new TextFieldHost(new MaterialTextField
        {
            Variant = variant, Label = "Required", Text = "invalid", ErrorText = "Correct the value",
            ShowClearButton = true, ShowCounter = true, InnerRightContent = trailing
        });
        host.Capture();
        Assert.Equal(Color.Parse("#B3261E"), Assert.IsAssignableFrom<ISolidColorBrush>(trailing.Foreground).Color);
        var point = host.Field.TranslatePoint(new Point(5, 30), host.Window)!.Value;
        host.Window.MouseMove(point);
        host.Capture();
        var label = host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.Text == "Required" && text.IsEffectivelyVisible);
        Assert.Equal(Color.Parse("#410E0B"), Assert.IsAssignableFrom<ISolidColorBrush>(label.Foreground).Color);
        var counter = host.Window.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "7");
        Assert.Equal(Color.Parse("#B3261E"), Assert.IsAssignableFrom<ISolidColorBrush>(counter.Foreground).Color);
    }

    [AvaloniaFact]
    public void Host_template_extension_keeps_the_documented_native_editor_parts_and_input()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name" });
        host.Field.Template = new FuncControlTemplate<MaterialTextField>((field, scope) =>
        {
            var presenter = new TextPresenter();
            presenter.Bind(TextPresenter.TextProperty, field.GetObservable(TextBox.TextProperty));
            var scroll = new ScrollViewer { Content = presenter, Theme = new ControlTheme(typeof(ScrollViewer))
            {
                Setters = { new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ScrollViewer>((_, names) =>
                {
                    var content = new ScrollContentPresenter();
                    names.Register("PART_ContentPresenter", content);
                    return content;
                })) }
            }};
            scope.Register("PART_TextPresenter", presenter);
            scope.Register("PART_ScrollViewer", scroll);
            return new Border { Padding = new Thickness(16), Child = scroll };
        });
        host.Capture();
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("中文 Atlas");
        host.Press(PhysicalKey.A, RawInputModifiers.Control);
        Assert.Equal("中文 Atlas", host.Field.SelectedText);
        host.Window.KeyTextInput("Updated");
        Assert.Equal("Updated", host.Field.Text);
    }

    [AvaloniaFact]
    public void Long_single_line_editor_keeps_the_caret_in_its_viewport()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Reference" });
        host.Window.Width = 280;
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("A long mixed-language reference 中文 " + new string('x', 80));
        host.Capture();
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        host.Field.RaiseEvent(request);
        // Avalonia 12's native TextBox client reports its cursor in the TextBox's coordinate space.
        Assert.InRange(request.Client!.CursorRectangle.Right, 0, host.Field.Bounds.Width);
        Assert.Equal(115, host.Field.CaretIndex); // 35 UTF-16 units in the literal prefix, then 80 x characters.
    }

    [AvaloniaFact]
    public void Default_filled_editor_has_16_unit_inset_without_reserving_absent_slots()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name" });
        host.Press(PhysicalKey.Tab);
        host.Capture();
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        host.Field.RaiseEvent(request);
        var client = request.Client!;
        var caretLeft = client.TextViewVisual.TranslatePoint(default, host.Field)!.Value.X;
        Assert.Equal(16, caretLeft);
    }

    [AvaloniaFact]
    public void Unlabelled_editor_shows_placeholder_and_a_visible_clear_icon()
    {
        using var host = new TextFieldHost(new MaterialTextField { PlaceholderText = "Optional reference", ShowClearButton = true });
        Assert.Contains("Optional reference", host.VisibleText());
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("reference");
        host.Capture();
        Assert.DoesNotContain("Optional reference", host.VisibleText());
        var clear = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "Clear text");
        var icon = clear.GetVisualDescendants().OfType<PathIcon>().Single();
        Assert.True(icon.Data!.Bounds.Width > 0 && icon.Data.Bounds.Height > 0);
        var point = clear.TranslatePoint(new Point(24, 24), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        Assert.Equal(string.Empty, host.Field.Text);
        Assert.True(host.Field.IsFocused);
        host.Capture();
        Assert.Contains("Optional reference", host.VisibleText());
    }

    [AvaloniaFact]
    public void Enlarged_outlined_label_does_not_overlap_the_editor_line()
    {
        using var host = new TextFieldHost(new MaterialTextField { Variant = MaterialTextFieldVariant.Outlined, Label = "Name / 名称" });
        host.Theme.Typography = new MaterialTypography { Scale = 2 };
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("中文 Atlas");
        host.Capture();
        var label = host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.IsEffectivelyVisible && text.Text == "Name / 名称");
        var labelBottom = label.TranslatePoint(new Point(0, label.Bounds.Height), host.Window)!.Value.Y;
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        host.Field.RaiseEvent(request);
        var client = request.Client!;
        var editorTop = client.TextViewVisual.TranslatePoint(default, host.Window)!.Value.Y;
        Assert.True(labelBottom <= editorTop, $"Label bottom {labelBottom} overlaps editor top {editorTop}.");
    }

    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled)]
    [InlineData(MaterialTextFieldVariant.Outlined)]
    public void Floating_label_and_material_states_follow_pointer_focus_and_theme(MaterialTextFieldVariant variant)
    {
        using var host = new TextFieldHost(new MaterialTextField { Variant = variant, Label = "Name / 名称" });
        var resting = host.Capture();
        Assert.Equal(16, host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.IsEffectivelyVisible && text.Text == "Name / 名称").FontSize);
        var point = host.Field.TranslatePoint(new Point(host.Field.Bounds.Width / 2, 32), host.Window)!.Value;
        host.Window.MouseMove(point);
        Assert.Equal(Color.Parse("#1D1B20"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Field.BorderBrush).Color);
        Assert.NotEqual(resting, host.Capture());
        var hoveredLabel = host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.IsEffectivelyVisible && text.Text == "Name / 名称");
        Assert.Equal(Color.Parse(variant == MaterialTextFieldVariant.Outlined ? "#1D1B20" : "#49454F"),
            Assert.IsAssignableFrom<ISolidColorBrush>(hoveredLabel.Foreground).Color);
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        Assert.True(host.Field.IsFocused);
        Assert.Equal(variant == MaterialTextFieldVariant.Filled ? new Thickness(0, 0, 0, 2) : new Thickness(2), host.Field.BorderThickness);
        host.Capture();
        Assert.Equal(12, host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.IsEffectivelyVisible && text.Text == "Name / 名称").FontSize);
        host.Field.ErrorText = "Required";
        Assert.Equal(Color.Parse("#B3261E"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Field.BorderBrush).Color);
        var light = host.Capture();
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.NotEqual(light, host.Capture());
        Assert.Equal(Color.Parse("#F2B8B5"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Field.BorderBrush).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Light;
        host.Field.ErrorText = null;
        Assert.Equal(Color.Parse("#6750A4"), Assert.IsAssignableFrom<ISolidColorBrush>(host.Field.BorderBrush).Color);
    }

    [AvaloniaFact]
    public void Touch_editing_and_native_maxlength_keep_counter_in_utf16_units()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name", MaxLength = 3, ShowCounter = true });
        var point = host.Field.TranslatePoint(new Point(3, 32), host.Window)!.Value;
        using var contact = host.Window.TouchBegin(point);
        host.Window.TouchEnd(contact, point);
        Assert.True(host.Field.IsFocused);
        host.Window.KeyTextInput("中😀");
        host.Window.KeyTextInput("ABC");
        Assert.Equal("中😀", host.Field.Text);
        Assert.Equal("3 / 3", host.Field.CounterText);
    }

    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled)]
    [InlineData(MaterialTextFieldVariant.Outlined)]
    public void Disabled_and_readonly_fields_keep_values_but_cannot_be_edited(MaterialTextFieldVariant variant)
    {
        using var host = new TextFieldHost(new MaterialTextField { Variant = variant, Label = "Code", Text = "kept", ShowClearButton = true });
        host.Press(PhysicalKey.Tab);
        host.Field.IsReadOnly = true;
        host.Press(PhysicalKey.A, RawInputModifiers.Control);
        Assert.Equal("kept", host.Field.SelectedText);
        host.Window.KeyTextInput("discarded");
        host.Press(PhysicalKey.Backspace);
        Assert.Equal("kept", host.Field.Text);
        host.Capture();
        Assert.DoesNotContain(host.Window.GetVisualDescendants().OfType<Button>(),
            button => AutomationProperties.GetName(button) == "Clear text" && button.IsEffectivelyVisible);
        host.Field.IsEnabled = false;
        host.Capture();
        var border = Assert.IsAssignableFrom<ISolidColorBrush>(host.Field.BorderBrush);
        Assert.Equal(variant == MaterialTextFieldVariant.Filled ? 0.38 : 0.12, border.Opacity);
        host.Next.Focus();
        host.Press(PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.False(host.Field.IsFocused);
        host.Window.KeyTextInput("discarded");
        Assert.Equal("kept", host.Field.Text);
    }

    [AvaloniaFact]
    public void Native_ime_client_can_compose_cancel_and_commit_without_mutating_uncommitted_text()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Name", PlaceholderText = "Type a name", ShowCounter = true });
        host.Press(PhysicalKey.Tab);
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        host.Field.RaiseEvent(request);
        var client = Assert.IsAssignableFrom<TextInputMethodClient>(request.Client);
        Assert.True(client.SupportsPreedit);
        client.SetPreeditText("zhong", 5);
        host.Capture();
        Assert.True(string.IsNullOrEmpty(host.Field.Text));
        Assert.Equal("0", host.Field.CounterText);
        Assert.DoesNotContain("Type a name", host.VisibleText());
        Assert.True(client.CursorRectangle.Height > 0);
        client.SetPreeditText(null);
        host.Capture();
        Assert.Contains("Type a name", host.VisibleText());
        host.Window.KeyTextInput("中");
        Assert.Equal("中", host.Field.Text);
        Assert.Equal("1", host.Field.CounterText);
        Assert.Equal("中", client.SurroundingText);
        host.Press(PhysicalKey.A, RawInputModifiers.Control);
        host.Press(PhysicalKey.Backspace);
        client.SetPreeditText("cancel", 6);
        client.SetPreeditText(null);
        host.Press(PhysicalKey.Tab);
        host.Capture();
        Assert.Equal(16, host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.Text == "Name" && text.IsEffectivelyVisible).FontSize);
    }

    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled)]
    [InlineData(MaterialTextFieldVariant.Outlined)]
    public void Multiline_and_large_fonts_grow_in_a_narrow_host_without_truncating_feedback(MaterialTextFieldVariant variant)
    {
        using var host = new TextFieldHost(new MaterialTextField
        {
            Variant = variant, Label = "Notes / 备注", SupportingText = "中文 and Latin supporting text wraps in a narrow window.",
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap
        });
        var initialHeight = host.Field.Bounds.Height;
        host.Theme.Typography = new MaterialTypography { Scale = 2 };
        host.Window.Width = 280;
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("中文 line one");
        host.Press(PhysicalKey.Enter);
        host.Window.KeyTextInput("第二行 / second line");
        host.Capture();
        Assert.Equal("中文 line one" + Environment.NewLine + "第二行 / second line", host.Field.Text);
        Assert.Equal(32, host.Field.FontSize);
        Assert.True(host.Field.Bounds.Height > initialHeight);
        var supporting = host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.Text == host.Field.SupportingText);
        Assert.Equal(24, supporting.FontSize);
        Assert.True(supporting.Bounds.Height >= 48);
        Assert.True(supporting.Bounds.Width <= host.Field.Bounds.Width - 32);
        host.Press(PhysicalKey.Tab);
        Assert.True(host.Next.IsFocused);
        host.Press(PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.True(host.Field.IsFocused);
    }

    [AvaloniaFact]
    public void Slots_and_affixes_do_not_become_part_of_the_edited_value()
    {
        var leading = new TextBlock { Text = "€", Width = 24 };
        var trailing = new TextBlock { Text = "✓", Width = 24 };
        using var host = new TextFieldHost(new MaterialTextField
        {
            Label = "Amount", PrefixText = "EUR", SuffixText = "per day",
            InnerLeftContent = leading, InnerRightContent = trailing, ShowClearButton = true
        });
        host.Capture();
        Assert.True(leading.IsEffectivelyVisible && leading.Bounds.Width > 0);
        Assert.True(trailing.IsEffectivelyVisible && trailing.Bounds.Width > 0);
        Assert.DoesNotContain("EUR", host.VisibleText());
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("125");
        host.Capture();
        Assert.Contains("EUR", host.VisibleText());
        Assert.Contains("per day", host.VisibleText());
        Assert.Equal("125", host.Field.Text);
        host.Press(PhysicalKey.A, RawInputModifiers.Control);
        Assert.Equal("125", host.Field.SelectedText);
        Assert.True(leading.IsEffectivelyVisible && trailing.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Native_binding_validation_is_visible_and_clears_after_real_correction()
    {
        using var host = new TextFieldHost(new MaterialTextField { Label = "Code", SupportingText = "Use at least 3 characters" });
        var model = new CodeModel();
        host.Field.Bind(TextBox.TextProperty, new Binding(nameof(CodeModel.Value)) { Source = model, Mode = BindingMode.TwoWay });
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("ab");
        Assert.True(DataValidationErrors.GetHasErrors(host.Field));
        Assert.True(host.Field.HasError);
        Assert.Equal("Code is too short", host.Field.EffectiveSupportingText);
        host.Capture();
        Assert.Contains("Code is too short", host.VisibleText());
        host.Window.KeyTextInput("c");
        Assert.False(host.Field.HasError);
        Assert.Equal("abc", model.Value);
        Assert.Equal("Use at least 3 characters", host.Field.EffectiveSupportingText);
        host.Field.IsError = true;
        Assert.True(host.Field.HasError);
        Assert.Equal("Use at least 3 characters", host.Field.EffectiveSupportingText);
    }

    public sealed class CodeModel
    {
        private string? _value;
        public string? Value
        {
            get => _value;
            set
            {
                if (value is { Length: < 3 }) throw new ArgumentException("Code is too short");
                _value = value;
            }
        }
    }

    [AvaloniaFact]
    public void Automation_exposes_label_feedback_readonly_and_never_discloses_passwords()
    {
        using var host = new TextFieldHost(new MaterialTextField
        {
            Label = "Password / 密码", PasswordChar = '●', SupportingText = "At least 8 characters", ShowCounter = true
        });
        var peer = ControlAutomationPeer.CreatePeerForElement(host.Field)!;
        var value = Assert.IsAssignableFrom<IValueProvider>(peer);
        var emittedValues = new List<object?>();
        peer.PropertyChanged += (_, change) =>
        {
            if (change.Property == ValuePatternIdentifiers.ValueProperty)
            {
                emittedValues.Add(change.OldValue);
                emittedValues.Add(change.NewValue);
            }
        };
        Assert.Equal("Password / 密码", peer.GetName());
        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("secret123");
        Assert.Equal(string.Empty, value.Value);
        host.Field.RevealPassword = true;
        Assert.Equal(string.Empty, value.Value);
        host.Field.ErrorText = "Use a stronger password";
        Assert.Contains("Use a stronger password", peer.GetHelpText());
        Assert.Contains("9", peer.GetHelpText());
        AutomationProperties.SetName(host.Field, "Account secret");
        Assert.Equal("Account secret", peer.GetName());
        host.Field.IsReadOnly = true;
        Assert.True(value.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => value.SetValue("changed"));
        host.Window.KeyTextInput("ignored");
        Assert.Equal("secret123", host.Field.Text);
        host.Field.IsReadOnly = false;
        host.Field.IsEnabled = false;
        Assert.False(peer.IsEnabled());
        Assert.Throws<InvalidOperationException>(() => value.SetValue("changed"));
        Assert.NotEmpty(emittedValues);
        Assert.All(emittedValues, emitted => Assert.Equal(string.Empty, emitted));
    }

    [AvaloniaFact]
    public void Keyboard_user_clears_text_and_counter_then_continues_to_the_next_field()
    {
        using var host = new TextFieldHost(new MaterialTextField
        {
            Label = "Display name", ShowClearButton = true, ShowCounter = true, MaxLength = 10
        });
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("中文 ABC");
        Assert.Equal("6 / 10", host.Field.CounterText);
        Assert.True(host.Field.CanUndo, "The native editor has no undo snapshot after this committed input.");
        host.Capture();
        Assert.Contains("6 / 10", host.VisibleText());
        host.Press(PhysicalKey.Tab);
        var clear = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "Clear text");
        Assert.True(clear.IsFocused);
        Assert.True(clear.Bounds.Width >= 48 && clear.Bounds.Height >= 48);
        host.Press(PhysicalKey.Space);
        Assert.Equal(string.Empty, host.Field.Text);
        Assert.Equal("0 / 10", host.Field.CounterText);
        Assert.True(host.Field.IsFocused);
        Assert.True(host.Field.CanUndo, "The clear action discarded native undo history.");
        host.Press(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.Equal("中文 ABC", host.Field.Text);
        Assert.Equal("6 / 10", host.Field.CounterText);
        host.Press(PhysicalKey.Tab);
        host.Press(PhysicalKey.Space);
        Assert.Equal(string.Empty, host.Field.Text);
        host.Press(PhysicalKey.Tab);
        Assert.True(host.Next.IsFocused);
    }

    [AvaloniaFact]
    public void Editing_validation_replaces_supporting_text_and_recovers_without_losing_the_label()
    {
        using var host = new TextFieldHost(new MaterialTextField
        {
            Label = "Email / 邮箱", SupportingText = "Use an @ sign", PlaceholderText = "name@example.test"
        });
        host.Field.TextChanged += (_, _) => host.Field.ErrorText = host.Field.Text?.Contains('@') == true ? null : "Invalid email";
        host.Press(PhysicalKey.Tab);
        host.Window.KeyTextInput("atlas");
        Assert.True(host.Field.HasError);
        Assert.Equal("Invalid email", host.Field.EffectiveSupportingText);
        host.Capture();
        Assert.Contains("Invalid email", host.VisibleText());
        Assert.DoesNotContain("Use an @ sign", host.VisibleText());
        Assert.Contains("Email / 邮箱", host.VisibleText());
        var invalid = host.Capture();
        host.Window.KeyTextInput("@example.test");
        Assert.False(host.Field.HasError);
        Assert.Equal("Use an @ sign", host.Field.EffectiveSupportingText);
        Assert.NotEqual(invalid, host.Capture());
        Assert.Contains("Use an @ sign", host.VisibleText());
    }

    [AvaloniaTheory]
    [InlineData(MaterialTextFieldVariant.Filled)]
    [InlineData(MaterialTextFieldVariant.Outlined)]
    public void Mixed_language_editor_preserves_native_caret_selection_and_undo(MaterialTextFieldVariant variant)
    {
        using var host = new TextFieldHost(new MaterialTextField { Variant = variant, Label = "名称 / Name" });
        host.Press(PhysicalKey.Tab);
        Assert.True(host.Field.IsFocused);
        host.Window.KeyTextInput("中文 Atlas");
        Assert.Equal("中文 Atlas", host.Field.Text);
        Assert.Equal(8, host.Field.CaretIndex);
        host.Press(PhysicalKey.ArrowLeft, RawInputModifiers.Shift);
        Assert.Equal("s", host.Field.SelectedText);
        host.Window.KeyTextInput("X");
        Assert.Equal("中文 AtlaX", host.Field.Text);
        host.Press(PhysicalKey.Z, RawInputModifiers.Control);
        Assert.Equal("中文 Atlas", host.Field.Text);
        Assert.True(host.Field.Bounds.Height >= 56);
    }
}

internal sealed class TextFieldHost : IDisposable
{
    public MaterialTheme Theme { get; } = new();
    public MaterialTextField Field { get; }
    public MaterialButton Next { get; } = new() { Content = "Next" };
    public Window Window { get; }

    public TextFieldHost(MaterialTextField field)
    {
        Field = field;
        Application.Current!.Styles.Add(Theme);
        Window = new Window
        {
            Width = 440, Height = 360, RequestedThemeVariant = ThemeVariant.Light,
            Content = new StackPanel { Margin = new Thickness(24), Spacing = 12, Children = { Field, Next } }
        };
        Window.Show();
        Capture();
    }

    public void Press(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPressQwerty(key, modifiers);
        Window.KeyReleaseQwerty(key, modifiers);
    }

    public byte[] Capture()
    {
        using var bitmap = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame.");
        using var stream = new MemoryStream();
        bitmap.Save(stream, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    public Color PixelAt(Point point)
    {
        using var bitmap = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame.");
        using var frame = bitmap.Lock();
        var offset = (int)(point.Y * Window.RenderScaling) * frame.RowBytes + (int)(point.X * Window.RenderScaling) * 4;
        var first = Marshal.ReadByte(frame.Address, offset);
        var green = Marshal.ReadByte(frame.Address, offset + 1);
        var third = Marshal.ReadByte(frame.Address, offset + 2);
        var alpha = Marshal.ReadByte(frame.Address, offset + 3);
        if (frame.Format == PixelFormat.Bgra8888) return Color.FromArgb(alpha, third, green, first);
        if (frame.Format == PixelFormat.Rgba8888) return Color.FromArgb(alpha, first, green, third);
        throw new InvalidOperationException($"Unexpected pixel format {frame.Format}.");
    }

    public string[] VisibleText() => Window.GetVisualDescendants().OfType<TextBlock>()
        .Where(text => text.IsEffectivelyVisible && text.Opacity > 0 && text.Bounds.Width > 0)
        .Select(text => text.Text ?? string.Empty).ToArray();

    public void Dispose()
    {
        Window.Close();
        Application.Current!.Styles.Remove(Theme);
    }
}
