namespace Avalonia.Material3.Tokens;

/// <summary>State layer alpha, not whole-control opacity. Disabled button alpha preserves the #2 FilledButtonTokens default.</summary>
public sealed record MaterialStates
{
    public double HoverStateLayerOpacity { get; init; } = 0.08;
    public double FocusStateLayerOpacity { get; init; } = 0.10;
    public double PressedStateLayerOpacity { get; init; } = 0.10;
    public double DraggedStateLayerOpacity { get; init; } = 0.16;
    public double DisabledContainerOpacity { get; init; } = 0.12;
    public double DisabledButtonContainerOpacity { get; init; } = 0.10;
    public double DisabledForegroundOpacity { get; init; } = 0.38;

    public IEnumerable<KeyValuePair<string, double>> GetOpacities()
    {
        yield return new(nameof(HoverStateLayerOpacity), HoverStateLayerOpacity);
        yield return new(nameof(FocusStateLayerOpacity), FocusStateLayerOpacity);
        yield return new(nameof(PressedStateLayerOpacity), PressedStateLayerOpacity);
        yield return new(nameof(DraggedStateLayerOpacity), DraggedStateLayerOpacity);
        yield return new(nameof(DisabledContainerOpacity), DisabledContainerOpacity);
        yield return new(nameof(DisabledButtonContainerOpacity), DisabledButtonContainerOpacity);
        yield return new(nameof(DisabledForegroundOpacity), DisabledForegroundOpacity);
    }

    internal bool IsValid => GetOpacities().All(pair => double.IsFinite(pair.Value) && pair.Value is >= 0 and <= 1);
}
