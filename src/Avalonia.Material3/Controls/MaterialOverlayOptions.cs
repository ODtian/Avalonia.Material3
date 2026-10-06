using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

public enum MaterialOverlayPlacement { Center, FullScreen, Bottom, Start, End, Anchor }
public enum MaterialOverlayAnchorPosition { Below, Above, Start, End, Left, Right }

/// <summary>Immutable policy for one presentation. Defaults are modal and deliberately do not light-dismiss.</summary>
public sealed record MaterialOverlayOptions
{
    public bool IsModal { get; init; } = true;
    public bool CloseOnEscape { get; init; } = true;
    public bool CloseOnBack { get; init; } = true;
    public bool CloseOnLightDismiss { get; init; }
    public Control? InitialFocus { get; init; }
    public Control? ReturnFocus { get; init; }
    public bool TakeFocus { get; init; } = true;
    public bool RestoreFocus { get; init; } = true;
    public bool ShowScrim { get; init; } = true;
    public double ScrimOpacity { get; init; } = .32;
    public MaterialOverlayPlacement Placement { get; init; } = MaterialOverlayPlacement.Center;
    public Thickness Margin { get; init; } = new(24);
    public Control? Anchor { get; init; }
    public MaterialOverlayAnchorPosition AnchorPosition { get; init; } = MaterialOverlayAnchorPosition.Below;
    /// <summary>Optional point in Anchor-local DIP coordinates for context presentation.</summary>
    public Point? AnchorPoint { get; init; }
    /// <summary>Host-local DIP offset; anchored presentations prefer below the anchor, then flip above and clamp.</summary>
    public Point Offset { get; init; }

    internal void Validate()
    {
        if (!Enum.IsDefined(Placement) || !Enum.IsDefined(AnchorPosition) ||
            (AnchorPoint is { } point && (!double.IsFinite(point.X) || !double.IsFinite(point.Y))) ||
            !double.IsFinite(ScrimOpacity) || ScrimOpacity < 0 || ScrimOpacity > 1 ||
            !double.IsFinite(Offset.X) || !double.IsFinite(Offset.Y) ||
            new[] { Margin.Left, Margin.Top, Margin.Right, Margin.Bottom }.Any(value => !double.IsFinite(value) || value < 0))
            throw new ArgumentException("Overlay placement, margin, offset and scrim opacity must be valid finite values.");
    }
}

/// <summary>Cancelable completion request. Host detachment is forced and cannot be vetoed.</summary>
public sealed class MaterialOverlayClosingEventArgs(MaterialOverlayResult result) : EventArgs
{
    public MaterialOverlayResult Result { get; } = result;
    public bool Cancel { get; set; }
}
