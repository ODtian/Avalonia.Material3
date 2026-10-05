using Avalonia.Animation.Easings;

namespace Avalonia.Material3.Tokens;

public sealed record MaterialCubicBezier(double X1, double Y1, double X2, double Y2)
{
    internal bool IsValid => double.IsFinite(X1) && X1 is >= 0 and <= 1 && double.IsFinite(Y1)
        && double.IsFinite(X2) && X2 is >= 0 and <= 1 && double.IsFinite(Y2);
    public SplineEasing ToEasing()
    {
        if (!IsValid) throw new ArgumentException("Invalid cubic bezier coordinates.");
        return new(X1, Y1, X2, Y2);
    }
}

/// <summary>Unit-mass spring parameters. IsInstant tells component animation drivers to snap, not integrate.</summary>
public sealed record MaterialSpring(double DampingRatio, double Stiffness)
{
    public bool IsInstant { get; init; }
    internal bool IsValid => double.IsFinite(DampingRatio) && DampingRatio > 0 && double.IsFinite(Stiffness) && Stiffness > 0;
}

public sealed record MaterialSpringScheme
{
    public MaterialSpring DefaultSpatial { get; init; } = new(0.8, 380);
    public MaterialSpring FastSpatial { get; init; } = new(0.6, 800);
    public MaterialSpring SlowSpatial { get; init; } = new(0.8, 200);
    public MaterialSpring DefaultEffects { get; init; } = new(1, 1600);
    public MaterialSpring FastEffects { get; init; } = new(1, 3800);
    public MaterialSpring SlowEffects { get; init; } = new(1, 800);
    public static MaterialSpringScheme Expressive { get; } = new();
    public static MaterialSpringScheme Standard { get; } = new()
    {
        DefaultSpatial = new(0.9, 700), FastSpatial = new(0.9, 1400), SlowSpatial = new(0.9, 300)
    };
    public IEnumerable<KeyValuePair<string, MaterialSpring>> GetSprings()
    {
        yield return new(nameof(DefaultSpatial), DefaultSpatial); yield return new(nameof(FastSpatial), FastSpatial);
        yield return new(nameof(SlowSpatial), SlowSpatial); yield return new(nameof(DefaultEffects), DefaultEffects);
        yield return new(nameof(FastEffects), FastEffects); yield return new(nameof(SlowEffects), SlowEffects);
    }
    internal bool IsValid => GetSprings().All(pair => pair.Value is { IsValid: true });
}

/// <summary>Complete duration/easing scales and standard/expressive spatial/effects springs. #2 state transition remains 100ms.</summary>
public sealed record MaterialMotion
{
    public TimeSpan StateLayerDuration { get; init; } = TimeSpan.FromMilliseconds(100);
    public bool ReduceMotion { get; init; }
    public MaterialSpringScheme Springs { get; init; } = MaterialSpringScheme.Expressive;
    public TimeSpan DurationShort1 { get; init; } = TimeSpan.FromMilliseconds(50);
    public TimeSpan DurationShort2 { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan DurationShort3 { get; init; } = TimeSpan.FromMilliseconds(150);
    public TimeSpan DurationShort4 { get; init; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan DurationMedium1 { get; init; } = TimeSpan.FromMilliseconds(250);
    public TimeSpan DurationMedium2 { get; init; } = TimeSpan.FromMilliseconds(300);
    public TimeSpan DurationMedium3 { get; init; } = TimeSpan.FromMilliseconds(350);
    public TimeSpan DurationMedium4 { get; init; } = TimeSpan.FromMilliseconds(400);
    public TimeSpan DurationLong1 { get; init; } = TimeSpan.FromMilliseconds(450);
    public TimeSpan DurationLong2 { get; init; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan DurationLong3 { get; init; } = TimeSpan.FromMilliseconds(550);
    public TimeSpan DurationLong4 { get; init; } = TimeSpan.FromMilliseconds(600);
    public TimeSpan DurationExtraLong1 { get; init; } = TimeSpan.FromMilliseconds(700);
    public TimeSpan DurationExtraLong2 { get; init; } = TimeSpan.FromMilliseconds(800);
    public TimeSpan DurationExtraLong3 { get; init; } = TimeSpan.FromMilliseconds(900);
    public TimeSpan DurationExtraLong4 { get; init; } = TimeSpan.FromMilliseconds(1000);
    public MaterialCubicBezier EasingEmphasized { get; init; } = new(0.2, 0, 0, 1);
    public MaterialCubicBezier EasingEmphasizedAccelerate { get; init; } = new(0.3, 0, 0.8, 0.15);
    public MaterialCubicBezier EasingEmphasizedDecelerate { get; init; } = new(0.05, 0.7, 0.1, 1);
    public MaterialCubicBezier EasingStandard { get; init; } = new(0.2, 0, 0, 1);
    public MaterialCubicBezier EasingStandardAccelerate { get; init; } = new(0.3, 0, 1, 1);
    public MaterialCubicBezier EasingStandardDecelerate { get; init; } = new(0, 0, 0, 1);
    public MaterialCubicBezier EasingLegacy { get; init; } = new(0.4, 0, 0.2, 1);
    public MaterialCubicBezier EasingLegacyAccelerate { get; init; } = new(0.4, 0, 1, 1);
    public MaterialCubicBezier EasingLegacyDecelerate { get; init; } = new(0, 0, 0.2, 1);
    public MaterialCubicBezier EasingLinear { get; init; } = new(0, 0, 1, 1);

    public IEnumerable<KeyValuePair<string, TimeSpan>> GetDurations()
    {
        yield return new(nameof(DurationShort1), DurationShort1); yield return new(nameof(DurationShort2), DurationShort2);
        yield return new(nameof(DurationShort3), DurationShort3); yield return new(nameof(DurationShort4), DurationShort4);
        yield return new(nameof(DurationMedium1), DurationMedium1); yield return new(nameof(DurationMedium2), DurationMedium2);
        yield return new(nameof(DurationMedium3), DurationMedium3); yield return new(nameof(DurationMedium4), DurationMedium4);
        yield return new(nameof(DurationLong1), DurationLong1); yield return new(nameof(DurationLong2), DurationLong2);
        yield return new(nameof(DurationLong3), DurationLong3); yield return new(nameof(DurationLong4), DurationLong4);
        yield return new(nameof(DurationExtraLong1), DurationExtraLong1); yield return new(nameof(DurationExtraLong2), DurationExtraLong2);
        yield return new(nameof(DurationExtraLong3), DurationExtraLong3); yield return new(nameof(DurationExtraLong4), DurationExtraLong4);
    }
    public IEnumerable<KeyValuePair<string, MaterialCubicBezier>> GetEasings()
    {
        yield return new(nameof(EasingEmphasized), EasingEmphasized); yield return new(nameof(EasingEmphasizedAccelerate), EasingEmphasizedAccelerate);
        yield return new(nameof(EasingEmphasizedDecelerate), EasingEmphasizedDecelerate); yield return new(nameof(EasingStandard), EasingStandard);
        yield return new(nameof(EasingStandardAccelerate), EasingStandardAccelerate); yield return new(nameof(EasingStandardDecelerate), EasingStandardDecelerate);
        yield return new(nameof(EasingLegacy), EasingLegacy); yield return new(nameof(EasingLegacyAccelerate), EasingLegacyAccelerate);
        yield return new(nameof(EasingLegacyDecelerate), EasingLegacyDecelerate); yield return new(nameof(EasingLinear), EasingLinear);
    }
    internal bool IsValid => StateLayerDuration >= TimeSpan.Zero && GetDurations().All(pair => pair.Value >= TimeSpan.Zero)
        && GetEasings().All(pair => pair.Value is { IsValid: true }) && Springs is { IsValid: true };
}
