namespace Avalonia.Material3.Tokens;

/// <summary>Uniform corner radii, in device-independent units, for the initial small button.</summary>
public sealed record MaterialShapes
{
    public double ButtonCornerRadius { get; init; } = 20;
    public double PressedButtonCornerRadius { get; init; } = 8;

    internal bool IsValid => double.IsFinite(ButtonCornerRadius) && ButtonCornerRadius >= 0
        && double.IsFinite(PressedButtonCornerRadius) && PressedButtonCornerRadius >= 0;
}
