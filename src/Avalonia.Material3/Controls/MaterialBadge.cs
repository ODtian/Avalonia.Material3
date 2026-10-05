using System.Globalization;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Material3.Controls;

/// <summary>A decorative activity dot (Count=null) or a count badge. Give the associated target a meaningful accessible name.</summary>
public class MaterialBadge : TemplatedControl
{
    public static readonly StyledProperty<int?> CountProperty = AvaloniaProperty.Register<MaterialBadge, int?>(nameof(Count), validate: value => value is null or >= 0);
    public static readonly StyledProperty<int> MaximumDisplayedCountProperty = AvaloniaProperty.Register<MaterialBadge, int>(nameof(MaximumDisplayedCount), 99, validate: value => value > 0);
    public static readonly StyledProperty<bool> ShowZeroProperty = AvaloniaProperty.Register<MaterialBadge, bool>(nameof(ShowZero));
    public static readonly DirectProperty<MaterialBadge, string?> DisplayTextProperty = AvaloniaProperty.RegisterDirect<MaterialBadge, string?>(nameof(DisplayText), badge => badge.DisplayText);
    private string? _displayText;
    public int? Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public int MaximumDisplayedCount { get => GetValue(MaximumDisplayedCountProperty); set => SetValue(MaximumDisplayedCountProperty, value); }
    public bool ShowZero { get => GetValue(ShowZeroProperty); set => SetValue(ShowZeroProperty, value); }
    public string? DisplayText => _displayText;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CountProperty || change.Property == MaximumDisplayedCountProperty || change.Property == ShowZeroProperty)
        {
            var label = Count is { } count ? (count > MaximumDisplayedCount ? $"{MaximumDisplayedCount}+" : count.ToString(CultureInfo.CurrentCulture)) : null;
            SetAndRaise(DisplayTextProperty, ref _displayText, label);
            PseudoClasses.Set(":count", Count.HasValue);
            PseudoClasses.Set(":empty", Count == 0 && !ShowZero);
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BadgePeer(this);
    private sealed class BadgePeer(MaterialBadge owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string? GetNameCore() => base.GetNameCore() ?? owner.Count?.ToString(CultureInfo.CurrentCulture) ?? "New activity";
    }
}
