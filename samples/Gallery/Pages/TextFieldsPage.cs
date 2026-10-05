using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Public-package text editing, validation, input forms and accessibility demonstrations.</summary>
public sealed class TextFieldsPage : StackPanel
{
    public TextFieldsPage(MaterialTheme? theme = null)
    {
        theme ??= Application.Current?.Styles.OfType<MaterialTheme>().LastOrDefault();
        Spacing = 12;
        Children.Add(new TextBlock
        {
            Text = "Text fields / 文本输入", FontSize = 28, TextWrapping = TextWrapping.Wrap
        });
        Children.Add(new TextBlock
        {
            Text = "Tab / Shift+Tab navigate; arrows, Shift+arrows, Ctrl+A and Ctrl+Z use the native editor. " +
                   "Try a Chinese IME, a narrow window, larger fonts and a screen reader. The native editor owns text, selection and IME; slot actions keep their own input.",
            TextWrapping = TextWrapping.Wrap
        });

        var name = Field("Display name / 显示名称", "Mixed Chinese and Latin; clear and undo are available.");
        name.ShowClearButton = true;
        name.ShowCounter = true;
        name.MaxLength = 40;
        name.PlaceholderText = "中文 Atlas";
        AutomationProperties.SetAutomationId(name, "TextField.DisplayName");

        var email = Field("Email / 邮箱", "Use a valid address containing @.", MaterialTextFieldVariant.Outlined);
        email.PlaceholderText = "name@example.test";
        email.ShowClearButton = true;
        AutomationProperties.SetAutomationId(email, "TextField.Email");
        email.TextChanged += (_, _) => email.ErrorText = string.IsNullOrEmpty(email.Text) || email.Text.Contains('@')
            ? null : "Include an @ sign / 请输入 @";

        var amount = Field("Amount / 金额", "Affixes and icons are not part of Text.", MaterialTextFieldVariant.Outlined);
        amount.PrefixText = "¥";
        amount.SuffixText = "CNY";
        amount.InnerLeftContent = new TextBlock { Text = "◈", FontSize = 24, Width = 24 };
        amount.InnerRightContent = new TextBlock { Text = "✓", FontSize = 24, Width = 24 };
        amount.ShowCounter = true;

        var password = Field("Password / 密码", "Password remains hidden from the automation value provider.");
        password.PasswordChar = '●';
        password.ShowClearButton = true;
        password.PlaceholderText = "At least eight characters";
        AutomationProperties.SetAutomationId(password, "TextField.Password");
        var reveal = new MaterialButton { Content = "◉", FontSize = 24, Padding = new Thickness(12) };
        reveal.Click += (_, _) =>
        {
            password.RevealPassword = !password.RevealPassword;
            reveal.Content = password.RevealPassword ? "○" : "◉";
            AutomationProperties.SetName(reveal, password.RevealPassword ? "Hide password" : "Show password");
        };
        AutomationProperties.SetName(reveal, "Show password");
        password.InnerRightContent = reveal;

        var notes = Field("Notes / 备注", "Enter inserts a newline; Tab leaves the editor. MinLines=3, MaxLines=5.", MaterialTextFieldVariant.Outlined);
        notes.AcceptsReturn = true;
        notes.TextWrapping = TextWrapping.Wrap;
        notes.MinLines = 3;
        notes.MaxLines = 5;
        notes.ShowCounter = true;
        notes.Text = "中文与 Latin 混排\nA second line — select and replace me.";

        var readonlyField = Field("Read-only / 只读", "Can focus, select and copy; cannot type or clear.");
        readonlyField.Text = "Reference / 参考值";
        AutomationProperties.SetAutomationId(readonlyField, "TextField.ReadOnly");
        readonlyField.IsReadOnly = true;
        readonlyField.ShowClearButton = true;

        var disabledFilled = Field("Disabled filled / 禁用填充", "Not in the keyboard focus order.");
        disabledFilled.Text = "Unavailable";
        disabledFilled.IsEnabled = false;
        var disabledOutlined = Field("Disabled outlined / 禁用描边", "Retains its value.", MaterialTextFieldVariant.Outlined);
        disabledOutlined.Text = "Unavailable";
        disabledOutlined.IsEnabled = false;
        var errorFilled = Field("Error filled", "Host-controlled validation.");
        errorFilled.Text = "invalid";
        errorFilled.ErrorText = "Explain the error in text, not only color.";
        var errorOutlined = Field("Error outlined", "Host-controlled validation.", MaterialTextFieldVariant.Outlined);
        errorOutlined.Text = "invalid";
        errorOutlined.ErrorText = "错误反馈 / Correct the value and retry.";

        var placeholderOnly = new MaterialTextField
        {
            PlaceholderText = "No visible label; explicit accessible name", ShowClearButton = true,
            SupportingText = "Use AutomationProperties.Name when a label is intentionally omitted."
        };
        AutomationProperties.SetName(placeholderOnly, "Optional reference");

        var result = new TextBlock { Text = "Ready", TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetAutomationId(result, "TextField.Result");
        var validate = new MaterialButton { Content = "Validate form" };
        AutomationProperties.SetAutomationId(validate, "TextField.Validate");
        validate.Click += (_, _) =>
        {
            name.ErrorText = string.IsNullOrWhiteSpace(name.Text) ? "Display name is required" : null;
            email.ErrorText = email.Text?.Contains('@') == true ? null : "Include an @ sign / 请输入 @";
            result.Text = name.HasError || email.HasError ? "Correct the highlighted fields" : "Form accepted";
            if (name.HasError) name.Focus();
            else if (email.HasError) email.Focus();
        };
        name.TextChanged += (_, _) => { if (!string.IsNullOrWhiteSpace(name.Text)) name.ErrorText = null; };

        var themeButton = new MaterialButton { Content = "Toggle light / dark" };
        themeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        };
        var large = false;
        var fontScale = new MaterialButton { Content = "Use 200% text", IsEnabled = theme is not null };
        fontScale.Click += (_, _) =>
        {
            large = !large;
            if (theme is not null) theme.Typography = theme.Typography with { Scale = large ? 2 : 1 };
            fontScale.Content = large ? "Use 100% text" : "Use 200% text";
        };
        Children.Add(new WrapPanel { Children = { validate, themeButton, fontScale } });
        Children.Add(result);
        foreach (var field in new[] { name, email, amount, password, notes, readonlyField, disabledFilled, disabledOutlined,
                     errorFilled, errorOutlined, placeholderOnly })
            Children.Add(field);
    }

    private static MaterialTextField Field(string label, string supportingText,
        MaterialTextFieldVariant variant = MaterialTextFieldVariant.Filled) => new()
    {
        Label = label, SupportingText = supportingText, Variant = variant, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
    };
}
