using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Package-only M3-06 form example. Persistence belongs to the host, not the control library.</summary>
public class SelectionFormPage : StackPanel
{
    public SelectionFormModel Form { get; } = new();
    public MaterialCheckBox Notifications { get; } = new() { Content = "通知 / Notifications (three state)", IsThreeState = true };
    public MaterialRadioButton Email { get; } = new() { Content = "Email / 电子邮件" };
    public MaterialRadioButton Post { get; } = new() { Content = "Post / 邮寄" };
    public MaterialSwitch AutoSave { get; } = new() { Content = "Auto save / 自动保存", OnContent = "Saving", OffContent = "Not saving" };
    public MaterialButton SaveButton { get; } = new() { Content = "Save input" };
    public MaterialButton ResetButton { get; } = new() { Content = "Reset input" };
    public MaterialButton RestoreButton { get; } = new() { Content = "Restore input" };
    public MaterialButton ThemeButton { get; } = new() { Content = "Switch light / dark" };
    public MaterialButton FontScaleButton { get; } = new() { Content = "Font scale 100% / 200%" };
    public TextBlock Status { get; } = new() { Text = "Mixed notifications; email; auto save off.", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    public string? SavedJson { get; private set; }
    public IReadOnlyList<ToggleButton> StateExamples { get; } = CreateStateExamples();
    public IReadOnlyList<ToggleButton> LongLabelExamples { get; } = CreateLongLabelExamples();

    public SelectionFormPage() : this(null) { }

    public SelectionFormPage(MaterialTheme? theme)
    {
        Spacing = 8;
        // An explicit unique group keeps two independently hosted forms from sharing radio selection.
        var group = "delivery-" + Guid.NewGuid().ToString("N");
        Email.GroupName = Post.GroupName = group;
        Notifications.Bind(ToggleButton.IsCheckedProperty, new Binding { Path = nameof(Form.Notifications), Mode = BindingMode.TwoWay, Source = Form });
        Email.Bind(ToggleButton.IsCheckedProperty, new Binding { Path = nameof(Form.EmailSelected), Mode = BindingMode.TwoWay, Source = Form });
        Post.Bind(ToggleButton.IsCheckedProperty, new Binding { Path = nameof(Form.PostSelected), Mode = BindingMode.TwoWay, Source = Form });
        AutoSave.Bind(ToggleButton.IsCheckedProperty, new Binding { Path = nameof(Form.AutoSave), Mode = BindingMode.TwoWay, Source = Form });
        AutomationProperties.SetName(Notifications, "Notifications");
        AutomationProperties.SetName(Email, "Delivery by email");
        AutomationProperties.SetName(Post, "Delivery by post");
        AutomationProperties.SetName(AutoSave, "Auto save");
        AutomationProperties.SetAutomationId(Status, "SelectionStatus");
        Children.Add(new TextBlock { Text = "M3-06 — Selection form", FontSize = 24 });
        Children.Add(Notifications);
        Children.Add(Email);
        Children.Add(Post);
        Children.Add(AutoSave);
        Children.Add(SaveButton);
        Children.Add(ResetButton);
        Children.Add(RestoreButton);
        Children.Add(Status);
        Children.Add(ThemeButton);
        Children.Add(FontScaleButton);
        ThemeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        };
        FontScaleButton.Click += (_, _) =>
        {
            var activeTheme = theme ?? Application.Current?.Styles.OfType<MaterialTheme>().LastOrDefault();
            if (activeTheme is not null)
                activeTheme.Typography = activeTheme.Typography with { Scale = activeTheme.Typography.Scale > 1 ? 1 : 2 };
        };
        SaveButton.Click += (_, _) => Save();
        ResetButton.Click += (_, _) => { Form.Apply(new SelectionFormData(false, "Email", false)); Status.Text = "Reset. Saved input is retained."; };
        RestoreButton.Click += (_, _) =>
        {
            if (SavedJson is { } json)
                Restore(json);
            else
                Status.Text = "Save input first.";
        };
        Children.Add(new TextBlock { Text = "States — hover, press, Tab and Space to inspect feedback", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        foreach (var example in StateExamples)
            Children.Add(example);
        Children.Add(new TextBlock { Text = "Long labels / 长标签 — resize the host and enlarge its font" });
        foreach (var example in LongLabelExamples)
            Children.Add(example);
    }

    private static IReadOnlyList<ToggleButton> CreateStateExamples()
    {
        var examples = new List<ToggleButton>();
        foreach (bool? value in new bool?[] { false, true, null })
        {
            var label = value is null ? "mixed" : value == true ? "selected" : "unselected";
            examples.Add(new MaterialCheckBox { Content = $"Checkbox — {label}", IsChecked = value, IsThreeState = true });
            examples.Add(new MaterialCheckBox { Content = $"Checkbox — disabled {label}", IsChecked = value, IsThreeState = true, IsEnabled = false });
            examples.Add(new MaterialCheckBox { Content = $"Checkbox — error {label}", IsChecked = value, IsThreeState = true, IsError = true, ErrorText = "Review this preference / 请检查" });
        }
        foreach (var value in new[] { false, true })
        {
            var label = value ? "selected" : "unselected";
            // Each state comparison is independent; the form above demonstrates a real radio group.
            examples.Add(new MaterialRadioButton { Content = $"Radio — {label}", IsChecked = value, GroupName = Guid.NewGuid().ToString("N") });
            examples.Add(new MaterialRadioButton { Content = $"Radio — disabled {label}", IsChecked = value, IsEnabled = false, GroupName = Guid.NewGuid().ToString("N") });
            examples.Add(new MaterialRadioButton { Content = $"Radio — host error {label}", IsChecked = value, IsError = true, ErrorText = "Select a delivery method", GroupName = Guid.NewGuid().ToString("N") });
            examples.Add(new MaterialSwitch { Content = $"Switch — {label}", IsChecked = value });
            examples.Add(new MaterialSwitch { Content = $"Switch — disabled {label}", IsChecked = value, IsEnabled = false });
            examples.Add(new MaterialSwitch { Content = $"Switch — host error {label}", IsChecked = value, IsError = true, ErrorText = "Review auto save" });
            examples.Add(new MaterialSwitch { Content = $"Switch — thumb icons {label}", IsChecked = value, OnIcon = "✓", OffIcon = "×" });
        }
        return examples.AsReadOnly();
    }

    private static IReadOnlyList<ToggleButton> CreateLongLabelExamples()
    {
        const string label = "长标签 / Long multilingual label: keep notifications available for all shared workspaces, including detailed updates that must remain readable in a narrow window and with enlarged fonts. 请选择适合自己的设置，并在保存后恢复之前的输入。";
        return Array.AsReadOnly<ToggleButton>([
            new MaterialCheckBox { Content = label, IsThreeState = true, IsChecked = null },
            new MaterialRadioButton { Content = label, GroupName = Guid.NewGuid().ToString("N") },
            new MaterialSwitch { Content = label }
        ]);
    }

    public void Save()
    {
        SavedJson = JsonSerializer.Serialize(Form.Snapshot, SelectionFormJsonContext.Default.SelectionFormData);
        Status.Text = SavedJson;
    }

    public void Restore(string json)
    {
        var data = JsonSerializer.Deserialize(json, SelectionFormJsonContext.Default.SelectionFormData)
            ?? throw new ArgumentException("The saved form must be an object.", nameof(json));
        Form.Apply(data);
        Status.Text = "Restored saved input.";
    }
}

/// <summary>Example host-owned data contract; null represents the checkbox's mixed state.</summary>
public sealed record SelectionFormData(bool? Notifications, string Delivery, bool AutoSave);

public sealed class SelectionFormModel : INotifyPropertyChanged
{
    private bool? _notifications;
    private string _delivery = "Email";
    private bool _autoSave;

    public bool? Notifications { get => _notifications; set { if (_notifications == value) return; _notifications = value; Changed(); } }
    public bool AutoSave { get => _autoSave; set { if (_autoSave == value) return; _autoSave = value; Changed(); } }
    public string Delivery
    {
        get => _delivery;
        set
        {
            if (value is not ("Email" or "Post"))
                throw new ArgumentException("Delivery must be Email or Post.", nameof(value));
            if (_delivery == value) return;
            _delivery = value;
            Changed();
            Changed(nameof(EmailSelected));
            Changed(nameof(PostSelected));
        }
    }
    public bool? EmailSelected { get => Delivery == "Email"; set { if (value == true) Delivery = "Email"; } }
    public bool? PostSelected { get => Delivery == "Post"; set { if (value == true) Delivery = "Post"; } }
    public SelectionFormData Snapshot => new(Notifications, Delivery, AutoSave);

    public void Apply(SelectionFormData data)
    {
        // Validate the complete input before changing any observable field.
        if (data.Delivery is not ("Email" or "Post"))
            throw new ArgumentException("Delivery must be Email or Post.", nameof(data));
        Notifications = data.Notifications;
        Delivery = data.Delivery;
        AutoSave = data.AutoSave;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

[JsonSerializable(typeof(SelectionFormData))]
internal partial class SelectionFormJsonContext : JsonSerializerContext;
