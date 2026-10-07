using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// AppBar.kt TwoRowsTopAppBar keeps two separately measured title/subtitle rows.
internal sealed class MaterialAppBarTitleLanes : Panel
{
    private sealed class Lane
    {
        internal readonly TextBlock Title = MaterialPickerSupport.Text("TitleLarge");
        internal readonly TextBlock Subtitle = MaterialPickerSupport.Text("LabelMedium", "OnSurfaceVariant");
        internal readonly ContentPresenter Custom = new() { IsVisible = false };
        internal readonly StackPanel Content = new();
        internal readonly ScrollViewer View;
        internal Lane()
        {
            Content.Children.Add(new Grid { Children = { Title, Custom } }); Content.Children.Add(Subtitle);
            View = new ScrollViewer { Content = Content, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }
    }
    private readonly Lane _small = new(), _large = new();
    private readonly ContentPresenter _shared = new();
    private readonly ScrollViewer _sharedView;
    private MaterialTopAppBar? _owner;
    private readonly List<IDisposable> _roles = [];
    private static readonly SplineEasing TopAlpha = new(.8, 0, .8, .15);
    internal double SmallHeight => _small.View.DesiredSize.Height;
    internal double LargeHeight => _large.View.DesiredSize.Height;
    private double _smallWidth, _largeWidth;
    private bool _sharedTitle;

    public MaterialAppBarTitleLanes()
    {
        UseLayoutRounding = false;
        _sharedView = new ScrollViewer { Content = _shared, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, IsVisible = false };
        Children.Add(_small.View); Children.Add(_large.View); Children.Add(_sharedView);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs args)
    {
        base.OnAttachedToVisualTree(args);
        _owner = this.GetVisualAncestors().OfType<MaterialTopAppBar>().FirstOrDefault();
        if (_owner is not null) { _owner.PropertyChanged += OwnerChanged; Rebuild(); }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs args)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        _owner = null; foreach (var binding in _roles) binding.Dispose(); _roles.Clear();
        base.OnDetachedFromVisualTree(args);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialTopAppBar.CurrentBackgroundProperty) return;
        if (change.Property == MaterialTopAppBar.TitleProperty || change.Property == MaterialTopAppBar.SubtitleProperty
            || change.Property == MaterialTopAppBar.TitleTemplateProperty || change.Property == MaterialTopAppBar.VariantProperty)
            Rebuild();
        else if (change.Property == MaterialTopAppBar.CollapsedFractionProperty)
        { UpdateAlpha(); InvalidateArrange(); }
        else if (change.Property == MaterialTopAppBar.CenterTitleProperty)
        { UpdateAlignment(); InvalidateMeasure(); }
    }
    private void Rebuild()
    {
        if (_owner is not { } owner) return;
        foreach (var binding in _roles) binding.Dispose(); _roles.Clear();
        var role = owner.Variant switch
        {
            MaterialTopAppBarVariant.Medium => "HeadlineSmall",
            MaterialTopAppBarVariant.Large or MaterialTopAppBarVariant.MediumFlexible => "HeadlineMedium",
            MaterialTopAppBarVariant.LargeFlexible => "DisplaySmall", _ => "TitleLarge"
        };
        BindRole(_small, "TitleLarge", "LabelMedium");
        BindRole(_large, role, owner.Variant == MaterialTopAppBarVariant.LargeFlexible ? "TitleMedium" : "LabelLarge");
        _small.Title.Text = _large.Title.Text = owner.Title;
        var hasSubtitle = owner.Variant is MaterialTopAppBarVariant.MediumFlexible or MaterialTopAppBarVariant.LargeFlexible
            && !string.IsNullOrEmpty(owner.Subtitle);
        _small.Subtitle.Text = _large.Subtitle.Text = owner.Subtitle;
        _small.Subtitle.IsVisible = _large.Subtitle.IsVisible = hasSubtitle;
        _small.Custom.Content = _large.Custom.Content = _shared.Content = null;
        _small.Custom.UpdateChild(); _large.Custom.UpdateChild(); _shared.UpdateChild();
        var first = owner.TitleTemplate?.Build(owner.Title);
        var second = owner.IsTwoRow ? owner.TitleTemplate?.Build(owner.Title) : null;
        _sharedTitle = first is not null && ReferenceEquals(first, second);
        _sharedView.IsVisible = _sharedTitle;
        _small.Title.IsVisible = _large.Title.IsVisible = owner.TitleTemplate is null;
        _small.Custom.IsVisible = _large.Custom.IsVisible = owner.TitleTemplate is not null && !_sharedTitle;
        if (_sharedTitle) _shared.Content = first;
        else { _small.Custom.Content = first; _large.Custom.Content = second; }
        UpdateAlignment(); UpdateAlpha(); InvalidateMeasure();
    }
    private void UpdateAlignment()
    {
        if (_owner is not { } owner) return;
        var centered = owner.CenterTitle || owner.Variant == MaterialTopAppBarVariant.CenterAligned;
        foreach (var lane in new[] { _small, _large })
        {
            lane.Title.HorizontalAlignment = lane.Subtitle.HorizontalAlignment = lane.Custom.HorizontalAlignment =
                centered ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        }
    }
    private void BindRole(Lane lane, string titleRole, string subtitleRole)
    {
        void Bind(AvaloniaObject target, AvaloniaProperty property, string key) =>
            _roles.Add(target.Bind(property, this.GetResourceObservable("M3." + key)));
        Bind(lane.Title, TextBlock.FontFamilyProperty, titleRole + "FontFamily");
        Bind(lane.Title, TextBlock.FontSizeProperty, titleRole + "FontSize");
        Bind(lane.Title, TextBlock.FontWeightProperty, titleRole + "FontWeight");
        Bind(lane.Title, TextBlock.LineHeightProperty, titleRole + "LineHeight");
        Bind(lane.Title, TextBlock.LetterSpacingProperty, titleRole + "LetterSpacing");
        Bind(lane.Custom, ContentPresenter.FontFamilyProperty, titleRole + "FontFamily");
        Bind(lane.Custom, ContentPresenter.FontSizeProperty, titleRole + "FontSize");
        Bind(lane.Custom, ContentPresenter.FontWeightProperty, titleRole + "FontWeight");
        Bind(lane.Custom, ContentPresenter.LineHeightProperty, titleRole + "LineHeight");
        Bind(lane.Custom, ContentPresenter.LetterSpacingProperty, titleRole + "LetterSpacing");
        Bind(lane.Subtitle, TextBlock.FontFamilyProperty, subtitleRole + "FontFamily");
        Bind(lane.Subtitle, TextBlock.FontSizeProperty, subtitleRole + "FontSize");
        Bind(lane.Subtitle, TextBlock.FontWeightProperty, subtitleRole + "FontWeight");
        Bind(lane.Subtitle, TextBlock.LineHeightProperty, subtitleRole + "LineHeight");
        Bind(lane.Subtitle, TextBlock.LetterSpacingProperty, subtitleRole + "LetterSpacing");
        if (_owner is { } owner)
        {
            _roles.Add(lane.Title.Bind(TextBlock.ForegroundProperty, owner.GetObservable(MaterialTopAppBar.ForegroundProperty)));
            _roles.Add(lane.Custom.Bind(ContentPresenter.ForegroundProperty, owner.GetObservable(MaterialTopAppBar.ForegroundProperty)));
        }
    }
    private void UpdateAlpha()
    {
        if (_owner is not { } owner) return;
        var fraction = owner.IsTwoRow ? owner.CollapsedFraction : 1;
        _small.View.Opacity = owner.IsTwoRow ? TopAlpha.Ease(fraction) : 1;
        _large.View.Opacity = owner.IsTwoRow ? 1 - fraction : 0;
        _sharedView.Opacity = owner.IsTwoRow ? Math.Max(_small.View.Opacity, _large.View.Opacity) : 1;
        // Source semantics follow fraction0.5 independently of the two painted alpha curves.
        AutomationProperties.SetAccessibilityView(_small.Title, fraction >= .5 ? AccessibilityView.Content : AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_small.Subtitle, fraction >= .5 ? AccessibilityView.Content : AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_large.Title, fraction < .5 ? AccessibilityView.Content : AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_large.Subtitle, fraction < .5 ? AccessibilityView.Content : AccessibilityView.Raw);
    }
    internal void MeasureLanes(double smallWidth, double largeWidth, double budget)
    {
        _smallWidth = smallWidth; _largeWidth = largeWidth;
        _small.View.Measure(new Size(smallWidth, Math.Max(28, budget)));
        _large.View.Measure(new Size(largeWidth, Math.Max(28, budget)));
        if (_sharedTitle)
        {
            _sharedView.Measure(new Size(largeWidth, Math.Max(28, budget)));
            _small.View.Measure(new Size(smallWidth, Math.Max(28, budget)));
        }
    }
    internal double ExpandedContentHeight => Math.Max(LargeHeight, _sharedTitle ? _sharedView.DesiredSize.Height : 0);
    internal double CollapsedContentHeight => Math.Max(SmallHeight, _sharedTitle ? _sharedView.DesiredSize.Height : 0);
    protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width,
        _owner?.IsTwoRow == true ? ExpandedContentHeight : CollapsedContentHeight);
    internal void ArrangeLanes(Size size, double topHeight, double start, double end, bool centered)
    {
        if (_owner is not { } owner) return;
        var smallHeight = _small.View.DesiredSize.Height;
        var largeHeight = _large.View.DesiredSize.Height;
        var smallWidth = centered ? Math.Min(_smallWidth, _small.Content.DesiredSize.Width) : _smallWidth;
        var smallX = centered ? Math.Clamp((size.Width - smallWidth) / 2, start, start + Math.Max(0, size.Width - start - end) - smallWidth) : start;
        _small.View.Arrange(new Rect(smallX, (topHeight - smallHeight) / 2, smallWidth, smallHeight));
        var fullBody = Math.Max(0, owner.ExpandedHeight - topHeight);
        var liveBody = owner.IsTwoRow ? Math.Max(0, size.Height - topHeight) : 0;
        var padding = Math.Clamp(owner.TitleBottomPadding - Descender(_large.View), 0, Math.Max(0, fullBody - largeHeight));
        var largeY = size.Height - largeHeight - padding;
        _large.View.Arrange(new Rect(16, largeY, _largeWidth, largeHeight));
        _large.View.Clip = new RectangleGeometry(new Rect(0, topHeight - largeY, _largeWidth, liveBody));
        if (_sharedTitle)
        {
            var height = _sharedView.DesiredSize.Height;
            var sharedPadding = Math.Clamp(owner.TitleBottomPadding - Descender(_sharedView), 0, Math.Max(0, fullBody - height));
            var expandedY = size.Height - height - sharedPadding;
            var collapsedY = (topHeight - height) / 2;
            var progress = owner.IsTwoRow ? owner.CollapsedFraction : 1;
            _sharedView.Arrange(new Rect(16 + (smallX - 16) * progress, expandedY + (collapsedY - expandedY) * progress,
                _largeWidth + (smallWidth - _largeWidth) * progress, height));
        }
    }
    private static double Descender(Control visual)
    {
        var text = visual.GetVisualDescendants().OfType<TextBlock>().LastOrDefault(t => t.IsEffectivelyVisible);
        if (text is null || text.TextLayout.TextLines.Count == 0) return 0;
        var lines = text.TextLayout.TextLines;
        return Math.Max(0, visual.DesiredSize.Height - lines.Take(lines.Count - 1).Sum(line => line.Height) - lines[^1].Baseline);
    }
}
