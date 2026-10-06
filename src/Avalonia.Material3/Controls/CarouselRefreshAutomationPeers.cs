using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Material3.Controls;

internal sealed class CarouselAutomationPeer : ControlAutomationPeer, IScrollProvider
{
    private readonly MaterialCarousel _owner;
    public CarouselAutomationPeer(MaterialCarousel owner) : base(owner)
    {
        _owner = owner;
        owner.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialCarousel.PositionDescriptionProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, change.OldValue, change.NewValue);
            if (change.Property == MaterialCarousel.CurrentIndexProperty)
            {
                RaisePropertyChangedEvent(ScrollPatternIdentifiers.HorizontalScrollPercentProperty, null, HorizontalScrollPercent);
                RaisePropertyChangedEvent(ScrollPatternIdentifiers.VerticalScrollPercentProperty, null, VerticalScrollPercent);
            }
        };
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
    protected override string? GetNameCore() => base.GetNameCore() ?? "Image carousel";
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() ?? _owner.PositionDescription;
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() ?? (_owner.Layout == MaterialCarouselLayout.FullScreen ? "Use Up and Down to browse; Home and End for boundaries." : "Use Left and Right to browse; Home and End for boundaries.");
    public bool HorizontallyScrollable => _owner.Layout != MaterialCarouselLayout.FullScreen && _owner.ItemList.Count > 1;
    public bool VerticallyScrollable => _owner.Layout == MaterialCarouselLayout.FullScreen && _owner.ItemList.Count > 1;
    private double Percent => _owner.ItemList.Count <= 1 ? 0 : 100d * _owner.CurrentIndex / (_owner.ItemList.Count - 1);
    public double HorizontalScrollPercent => HorizontallyScrollable ? Percent : -1;
    public double VerticalScrollPercent => VerticallyScrollable ? Percent : -1;
    public double HorizontalViewSize => HorizontallyScrollable ? Math.Min(100, 100d / _owner.ItemList.Count) : 100;
    public double VerticalViewSize => VerticallyScrollable ? 100d / _owner.ItemList.Count : 100;
    public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
    {
        EnsureEnabled();
        var amount = _owner.Layout == MaterialCarouselLayout.FullScreen ? verticalAmount : horizontalAmount;
        if (amount is ScrollAmount.SmallIncrement or ScrollAmount.LargeIncrement) _owner.MoveNext();
        else if (amount is ScrollAmount.SmallDecrement or ScrollAmount.LargeDecrement) _owner.MovePrevious();
    }
    public void SetScrollPercent(double horizontalPercent, double verticalPercent)
    {
        EnsureEnabled();
        var percent = _owner.Layout == MaterialCarouselLayout.FullScreen ? verticalPercent : horizontalPercent;
        if (percent == -1) return;
        if (!double.IsFinite(percent) || percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(horizontalPercent));
        _owner.SetCurrentValue(MaterialCarousel.CurrentIndexProperty, (int)Math.Round((_owner.ItemList.Count - 1) * percent / 100, MidpointRounding.AwayFromZero));
    }
}

internal sealed class RefreshAutomationPeer : ControlAutomationPeer, IInvokeProvider
{
    private readonly MaterialPullToRefresh _owner;
    public RefreshAutomationPeer(MaterialPullToRefresh owner) : base(owner)
    {
        _owner = owner;
        owner.PropertyChanged += (_, change) =>
        {
            if (change.Property == MaterialPullToRefresh.StatusDescriptionProperty)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, change.OldValue, change.NewValue);
        };
    }
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    protected override string? GetNameCore() => base.GetNameCore() ?? "Refresh content";
    protected override string? GetItemStatusCore() => base.GetItemStatusCore() ?? _owner.StatusDescription;
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() ?? "Pull downward at the beginning of the content, or press F5 to refresh.";
    public void Invoke() { EnsureEnabled(); _owner.RequestRefresh(); }
}
