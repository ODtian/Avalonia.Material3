using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;

namespace Gallery.Pages;

/// <summary>Package-only slider settings demo. Persistence is a host concern, not a control API.</summary>
public sealed class SliderSettingsPage : StackPanel
{
    private readonly string _settingsPath;
    public MaterialSlider Brightness { get; } = new() { Value = 50, LabelFormat = "0'%'", Marks = new double[] { 0, 50, 100 }, ShowMarks = true };
    public MaterialSlider Volume { get; } = new() { Value = 40, Step = 10, ShowMarks = true };
    public MaterialRangeSlider Hours { get; } = new() { Maximum = 24, Step = 1, LowerValue = 8, UpperValue = 18,
        ShowMarks = true, LowerLabel = "Start hour", UpperLabel = "End hour", ValueLabelVisibility = SliderValueLabelVisibility.Always };
    public MaterialSlider Balance { get; } = new() { Minimum = -100, Maximum = 100, Value = 0, CenteredTrack = true };
    public MaterialSlider VerticalLevel { get; } = new() { Orientation = Orientation.Vertical, Height = 160, Width = 120, Value = 50 };
    public MaterialSlider Disabled { get; } = new() { Value = 65, Step = 5, ShowMarks = true, IsEnabled = false };
    public TextBlock Preview { get; } = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    public TextBlock Status { get; } = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    public MaterialButton SaveButton { get; } = new() { Content = "Save settings" };
    public MaterialButton ReopenButton { get; } = new() { Content = "Reopen saved settings" };

    public SliderSettingsPage() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Material3Gallery", "slider-settings.json")) { }
    public SliderSettingsPage(string settingsPath)
    {
        _settingsPath = settingsPath;
        Spacing = 4;
        Margin = new Thickness(16);
        Children.Add(new TextBlock { Text = "Sliders · Settings / 设置", FontSize = 24 });
        Children.Add(new TextBlock { Text = "Adjust continuous, discrete and range settings. Preview updates before saving.", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        AddSetting("Brightness · continuous", Brightness, "Brightness");
        AddSetting("Volume · discrete with marks", Volume, "Volume");
        AddSetting("Active hours · ordered range", Hours, "Active hours");
        AddSetting("Balance · centered track", Balance, "Balance");
        AddSetting("Level · vertical", VerticalLevel, "Vertical level");
        AddSetting("Disabled setting", Disabled, "Disabled setting");
        Children.Add(Preview);
        SaveButton.Margin = new Thickness(0, 0, 8, 8);
        ReopenButton.Margin = new Thickness(0, 0, 8, 8);
        Children.Add(new WrapPanel { Orientation = Orientation.Horizontal, Children = { SaveButton, ReopenButton } });
        Children.Add(Status);
        AutomationProperties.SetLiveSetting(Preview, AutomationLiveSetting.Polite);
        foreach (var slider in new MaterialSlider[] { Brightness, Volume, Hours, Balance, VerticalLevel })
            slider.PropertyChanged += (_, e) =>
            {
                if (e.Property == MaterialSlider.ValueProperty || e.Property == MaterialRangeSlider.UpperValueProperty) UpdatePreview();
            };
        SaveButton.Click += (_, _) => SaveSettings();
        ReopenButton.Click += (_, _) => ReopenSettings();
        ReopenSettings();
    }

    private void AddSetting(string label, MaterialSlider slider, string name)
    {
        Children.Add(new TextBlock { Text = label, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        AutomationProperties.SetName(slider, name);
        Children.Add(slider);
    }

    private void UpdatePreview() => Preview.Text = string.Create(CultureInfo.InvariantCulture,
        $"Brightness {Brightness.Value:0.##}% · Volume {Volume.Value:0}% · Hours {Hours.LowerValue:0}–{Hours.UpperValue:0} · Balance {Balance.Value:0.##} · Level {VerticalLevel.Value:0.##}");

    public void SaveSettings()
    {
        try
        {
            var snapshot = new SliderSettingsSnapshot(Brightness.Value, Volume.Value, Hours.LowerValue, Hours.UpperValue, Balance.Value, VerticalLevel.Value);
            var directory = Path.GetDirectoryName(Path.GetFullPath(_settingsPath))!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(snapshot, SliderSettingsJsonContext.Default.SliderSettingsSnapshot));
            Status.Text = "Saved settings";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Status.Text = $"Unable to save settings: {e.Message}";
        }
    }

    public void ReopenSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var snapshot = JsonSerializer.Deserialize(File.ReadAllText(_settingsPath), SliderSettingsJsonContext.Default.SliderSettingsSnapshot);
                if (snapshot is null || !snapshot.IsValid) throw new JsonException("Invalid slider settings.");
                Brightness.Value = snapshot.Brightness;
                Volume.Value = snapshot.Volume;
                // Expand first so both endpoints can be restored regardless of the current range.
                Hours.LowerValue = Hours.Minimum;
                Hours.UpperValue = snapshot.UpperHour;
                Hours.LowerValue = snapshot.LowerHour;
                Balance.Value = snapshot.Balance;
                VerticalLevel.Value = snapshot.Level;
                Status.Text = "Restored saved settings";
            }
            else Status.Text = "No saved settings yet";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            Status.Text = $"Unable to restore settings: {e.Message}";
        }
        UpdatePreview();
    }
}

public sealed record SliderSettingsSnapshot(double Brightness, double Volume, double LowerHour, double UpperHour, double Balance, double Level)
{
    public bool IsValid => new[] { Brightness, Volume, LowerHour, UpperHour, Balance, Level }.All(double.IsFinite) && LowerHour <= UpperHour;
}

[System.Text.Json.Serialization.JsonSerializable(typeof(SliderSettingsSnapshot))]
internal partial class SliderSettingsJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
