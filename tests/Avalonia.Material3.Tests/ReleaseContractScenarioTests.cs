using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Material3.Tests;

public class ReleaseContractScenarioTests
{
    [AvaloniaFact]
    public void Reviewed_public_API_and_resource_inventory_is_stable()
    {
        var api = string.Join("\n", typeof(MaterialTheme).Assembly.GetExportedTypes()
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .SelectMany(t => new[] { "TYPE " + t.FullName + " : " + t.BaseType?.FullName }.Concat(
                t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(m => "  " + m.MemberType + " " + m).Order(StringComparer.Ordinal)))) + "\n";
        var theme = new MaterialTheme();
        var keys = theme.Resources.Keys.OfType<string>().Concat(theme.Resources.ThemeDictionaries.Values
            .OfType<Avalonia.Controls.ResourceDictionary>().SelectMany(d => d.Keys.OfType<string>())).Distinct().Order(StringComparer.Ordinal);
        var resources = string.Join("\n", keys) + "\n";
        if (Environment.GetEnvironmentVariable("M3_WRITE_BASELINES") is { } directory)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "public-api.txt"), api);
            File.WriteAllText(Path.Combine(directory, "resource-keys.txt"), resources);
            return; // Explicit maintainer-generation mode, never enabled by ordinary verification.
        }
        Assert.Equal(Read("M3.PublicApi.txt"), api);
        Assert.Equal(Read("M3.ResourceKeys.txt"), resources);
    }

    [AvaloniaFact]
    public void Initial_six_color_constructor_deconstruct_and_with_remain_usable()
    {
        var scheme = new MaterialColorScheme(Colors.Red, Colors.White, Colors.Black, Colors.White, Colors.Gray, Colors.Teal);
        var (primary, onPrimary, surface, onSurface, onSurfaceVariant, outline) = scheme;
        Assert.Equal(Colors.Red, primary); Assert.Equal(Colors.White, onPrimary);
        Assert.Equal(Colors.Black, surface); Assert.Equal(Colors.White, onSurface);
        Assert.Equal(Colors.Gray, onSurfaceVariant); Assert.Equal(Colors.Teal, outline);
        Assert.Equal(Colors.Coral, (scheme with { Primary = Colors.Coral }).Primary);
    }
    private static string Read(string name)
    {
        using var stream = typeof(ReleaseContractScenarioTests).Assembly.GetManifestResourceStream(name);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }
}
