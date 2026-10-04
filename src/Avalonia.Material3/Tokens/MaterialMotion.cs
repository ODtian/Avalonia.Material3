namespace Avalonia.Material3.Tokens;

/// <summary>Initial state-layer transition input. Expressive spring schemes follow in later tickets.</summary>
public sealed record MaterialMotion
{
    public TimeSpan StateLayerDuration { get; init; } = TimeSpan.FromMilliseconds(100);
    public bool ReduceMotion { get; init; }

    internal bool IsValid => StateLayerDuration >= TimeSpan.Zero;
}
