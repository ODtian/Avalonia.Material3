using System.Globalization;

namespace Material3.ReferenceUi;

/// <summary>The same explicit launch inputs as the pinned official Android reference.</summary>
public sealed record ReferenceConfiguration
{
    public string Scene { get; init; } = "home";
    public bool Dark { get; init; }
    public string Palette { get; init; } = "classic";
    public string? Locale { get; init; } = "en-US";
    public bool ExpressiveButtons { get; init; } = true;
    public bool CheckboxM3 { get; init; } = true;
    public CultureInfo Culture => string.IsNullOrWhiteSpace(Locale) ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(Locale);
    /// <summary>Release migration branches absent from the component library; requested values remain explicit.</summary>
    public IReadOnlyList<string> UnsupportedVariants => CheckboxM3 && ExpressiveButtons ? [] :
        new[] { CheckboxM3 ? null : "checkboxM3=false (legacy M2 paint)", ExpressiveButtons ? null : "expressiveButtons=false (stable button shape overload)" }.OfType<string>().ToArray();
    public void Validate()
    {
        if (UnsupportedVariants.Count > 0) throw new ArgumentException("The Avalonia reference supports checkboxM3=true and expressiveButtons=true. Native-only research branches: " + string.Join(", ", UnsupportedVariants));
    }

    public static ReferenceConfiguration FromArguments(string[] arguments)
    {
        var configuration = new ReferenceConfiguration();
        for (var index = 0; index < arguments.Length; index++)
        {
            var pair = arguments[index].TrimStart('-').Split('=', 2);
            var key = pair[0]; var value = pair.Length == 2 ? pair[1] : index + 1 < arguments.Length ? arguments[++index] : "true";
            configuration = key switch
            {
                "scene" => configuration with { Scene = value }, "palette" => configuration with { Palette = value },
                "locale" => configuration with { Locale = value }, "dark" => configuration with { Dark = bool.Parse(value) },
                "expressiveButtons" => configuration with { ExpressiveButtons = bool.Parse(value) },
                "checkboxM3" => configuration with { CheckboxM3 = bool.Parse(value) }, _ => configuration
            };
        }
        return configuration;
    }
}
