using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Automation;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Avalonia.Material3.Controls;

/// <summary>Template infrastructure; stable content measurement and cached keyline-mask plans.</summary>
public sealed class MaterialCarouselPresenter : Panel
{
    public static readonly StyledProperty<MaterialCarousel?> CarouselProperty = AvaloniaProperty.Register<MaterialCarouselPresenter, MaterialCarousel?>(nameof(Carousel));
    public MaterialCarousel? Carousel { get => GetValue(CarouselProperty); set => SetValue(CarouselProperty, value); }
    private MaterialCarousel? _listening;
    private Rect[] _start = [], _from = [], _presented = [], _measurePlan = [];
    private MaterialCarouselStrategy? _strategy;
    private (MaterialCarouselLayout Layout, Size Size, double Preferred, double Gap, int Count) _strategyInputs;
    private Size _planSize;
    private int _planIndex = -1;
    private bool _planDirty = true, _havePresented, _modeSnapshot, _forceArrange = true;
    private double _contentWidth, _fromContentWidth, _presentedContentWidth;

    public MaterialCarouselPresenter() { ClipToBounds = true; UseLayoutRounding = false; }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Subscribe();
        Rebuild();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Unsubscribe();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != CarouselProperty) return;
        Unsubscribe();
        if (VisualRoot is not null) Subscribe();
        Rebuild();
    }
    private void Subscribe()
    {
        Unsubscribe();
        _listening = Carousel;
        if (_listening is null) return;
        _listening.ContentChanged += Rebuild;
        _listening.LayoutChanged += LayoutChanged;
        _listening.PresentationChanged += FrameChanged;
    }
    private void Unsubscribe()
    {
        if (_listening is null) return;
        _listening.ContentChanged -= Rebuild;
        _listening.LayoutChanged -= LayoutChanged;
        _listening.PresentationChanged -= FrameChanged;
        _listening = null;
    }
    private void FrameChanged()
    {
        if (_modeSnapshot && Carousel?.LayoutProgress >= 1)
        {
            // The larger measurement envelope stays fixed during motion; release it once
            // the smaller recipe is fully presented so wrapping content gets its real width.
            _modeSnapshot = false;
            _forceArrange = true; // Synchronize parked native content with the completed recipe too.
            InvalidateMeasure();
        }
        else InvalidateArrange();
    }
    private void LayoutChanged()
    {
        _modeSnapshot = _havePresented && Carousel?.LayoutProgress < 1 && _presented.Length == Children.Count;
        if (_modeSnapshot)
        {
            Array.Copy(_presented, _from, _presented.Length);
            _fromContentWidth = _presentedContentWidth;
        }
        _planDirty = _forceArrange = true;
        InvalidateMeasure(); // actual layout input changed, never a position-only frame
    }
    private void Buffers(int count)
    {
        if (_start.Length == count) return;
        _start = new Rect[count]; _from = new Rect[count];
        _presented = new Rect[count]; _measurePlan = new Rect[count];
        _havePresented = _modeSnapshot = false;
        _planDirty = _forceArrange = true;
    }
    private void Rebuild()
    {
        if (Carousel is not { } carousel) { Children.Clear(); Buffers(0); return; }
        if (carousel.ItemList.Count == 0)
        {
            Children.Clear(); Children.Add(Text(carousel.EmptyText));
            Buffers(0); InvalidateMeasure(); return;
        }
        var same = Children.Count == carousel.ItemList.Count;
        if (same)
            for (var i = 0; i < Children.Count; i++)
                if (Children[i].Tag != carousel.ItemList[i]) { same = false; break; }
        if (!same)
        {
            Children.Clear();
            foreach (var item in carousel.ItemList)
            {
                var tile = new MaterialCarouselTile { Tag = item, ClipToBounds = true, UseLayoutRounding = false };
                tile.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
                Children.Add(tile);
            }
            _havePresented = false;
        }
        Buffers(Children.Count);
        foreach (var child in Children)
        {
            var tile = (MaterialCarouselTile)child;
            var item = (MaterialCarouselItem)tile.Tag!;
            var signature = (item.Image, item.Title, item.State, item.ErrorMessage, item.Content, carousel.ItemTemplate);
            if (tile.Signature is not ValueTuple<IImage?, string?, MaterialCarouselItemState, string?, object?, Avalonia.Controls.Templates.IDataTemplate?> old || old != signature)
            {
                tile.Child = BuildTile(carousel, item);
                tile.Signature = signature;
            }
        }
        _planDirty = _forceArrange = true;
        InvalidateMeasure();
    }
    private static Control BuildTile(MaterialCarousel owner, MaterialCarouselItem item)
    {
        if (owner.ItemTemplate is { } template) return template.Build(item) ?? new Panel();
        var image = new Image { Source = item.Image, Stretch = Stretch.UniformToFill, IsVisible = item.Image is not null };
        image.Styles.Add(new Style(selector => MaterialModalPaintScope.DisabledPaint(selector.OfType<Image>()))
        { Setters = { new Setter(OpacityProperty, new DynamicResourceExtension("M3.DisabledForegroundOpacity")) } });
        AutomationProperties.SetName(image, item.Title);
        Control? overlay = null;
        if (item.State == MaterialCarouselItemState.Loading)
            overlay = new MaterialLoadingIndicator { IsContained = true, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        else if (item.State == MaterialCarouselItemState.Failed)
        {
            var panel = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8) };
            var error = Text(item.ErrorMessage ?? "Image could not be loaded");
            error.MaxLines = 2; error.TextTrimming = TextTrimming.CharacterEllipsis;
            error.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("M3.ErrorBrush"));
            panel.Children.Add(error);
            var retry = new MaterialButton { Content = "Retry", HorizontalAlignment = HorizontalAlignment.Center };
            AutomationProperties.SetName(retry, "Retry " + item.Title);
            retry.Click += (_, _) => owner.RequestRetry(item);
            panel.Children.Add(retry);
            var backing = new Border { Child = panel, VerticalAlignment = VerticalAlignment.Center };
            backing.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
            overlay = backing;
        }
        var caption = new StackPanel { Margin = new Thickness(12, 4), Tag = "caption" };
        caption.Children.Add(Text(item.Title ?? ""));
        if (item.Content is not null) caption.Children.Add(new ContentControl { Content = item.Content });
        var footer = new Border { Child = caption, Tag = "footer", IsVisible = item.State != MaterialCarouselItemState.Failed };
        footer.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
        return new MaterialCarouselTileContent(image, overlay, footer);
    }
    private static TextBlock Text(string text)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        block.Styles.Add(new Style(selector => MaterialModalPaintScope.DisabledPaint(selector.OfType<TextBlock>()))
        { Setters = { new Setter(OpacityProperty, new DynamicResourceExtension("M3.DisabledForegroundOpacity")) } });
        block.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("M3.OnSurfaceBrush"));
        foreach (var (property, suffix) in new (AvaloniaProperty, string)[] { (TextBlock.FontFamilyProperty, "FontFamily"), (TextBlock.FontSizeProperty, "FontSize"), (TextBlock.FontWeightProperty, "FontWeight"), (TextBlock.LineHeightProperty, "LineHeight"), (TextBlock.LetterSpacingProperty, "LetterSpacing") })
            block.Bind(property, new DynamicResourceExtension("M3.BodyMedium" + suffix));
        return block;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : 400, double.IsFinite(availableSize.Height) ? availableSize.Height : 200);
        if (Carousel is not { } owner || owner.ItemList.Count == 0)
        { foreach (var child in Children) child.Measure(size); return size; }
        Buffers(Children.Count);
        if (_planDirty || _planSize != size)
        {
            FillPositions(size, 0, _measurePlan);
            _contentWidth = owner.Layout == MaterialCarouselLayout.FullScreen ? size.Width : _strategy!.ItemWidth;
        }
        var measureWidth = _modeSnapshot ? Math.Max(_fromContentWidth, _contentWidth) : _contentWidth;
        foreach (var child in Children)
        {
            var tile = (MaterialCarouselTile)child;
            tile.MeasureWidth = measureWidth;
            tile.Measure(size);
        }
        return size;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Carousel is not { } owner || Children.Count == 0) return finalSize;
        if (owner.ItemList.Count == 0) { Children[0].Arrange(new Rect(finalSize)); return finalSize; }
        var position = Math.Clamp(owner.PresentationPosition, 0, Children.Count - 1);
        FillPositions(finalSize, position, _start);
        _planIndex = (int)Math.Floor(position); _planSize = finalSize; _planDirty = false;
        var mode = _modeSnapshot ? owner.LayoutProgress : 1;
        var contentWidth = _modeSnapshot ? _fromContentWidth + (_contentWidth - _fromContentWidth) * mode : _contentWidth;
        var completing = _modeSnapshot && mode >= 1;
        for (var i = 0; i < Children.Count; i++)
        {
            var rect = _start[i];
            if (_modeSnapshot) rect = Mix(_from[i], rect, mode);
            var previous = _presented[i];
            _presented[i] = rect;
            var tile = (MaterialCarouselTile)Children[i];
            // Far-offscreen masks cannot paint or hit this clipped viewport. Keep their native trees
            // parked rather than changing N visual/property graphs on every fractional frame.
            if (!_forceArrange && !completing && !Near(rect, finalSize) && !Near(previous, finalSize)
                && (_modeSnapshot || tile.Bounds.Size == rect.Size)) continue;
            tile.ContentWidth = contentWidth;
            tile.CornerRadius = owner.Layout == MaterialCarouselLayout.FullScreen ? default : owner.CornerRadius;
            tile.Arrange(rect);
        }
        _presentedContentWidth = contentWidth;
        _havePresented = true; _forceArrange = false;
        if (completing) _modeSnapshot = false;
        return finalSize;
    }
    private static bool Near(Rect rect, Size viewport) => rect.Right >= -48 && rect.Left <= viewport.Width + 48 && rect.Bottom >= -48 && rect.Top <= viewport.Height + 48;
    private static Rect Mix(Rect from, Rect to, double t) => new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t,
        from.Width + (to.Width - from.Width) * t, from.Height + (to.Height - from.Height) * t);

    private void FillPositions(Size size, double position, Rect[] result)
    {
        var owner = Carousel!;
        var count = result.Length;
        var width = size.Width; var height = size.Height;
        if (owner.Layout == MaterialCarouselLayout.FullScreen)
        {
            owner.ItemStride = height;
            for (var i = 0; i < count; i++) result[i] = new Rect(0, (i - position) * height, width, height);
            return;
        }
        var inputs = (owner.Layout, size, owner.PreferredItemWidth, owner.ItemSpacing, count);
        if (_strategy is null || inputs != _strategyInputs)
        {
            _strategy = MaterialCarouselStrategy.Create(owner.Layout, width, owner.PreferredItemWidth, owner.ItemSpacing, count);
            _strategyInputs = inputs;
        }
        _strategy.Fill(position, size, result);
        owner.ItemStride = _strategy.ItemWidth + owner.ItemSpacing;
    }
}

internal sealed class MaterialCarouselTile : Border
{
    internal object? Signature;
    private double _measureWidth;
    internal double MeasureWidth
    {
        get => _measureWidth;
        set { if (_measureWidth != value) { _measureWidth = value; InvalidateMeasure(); } }
    }
    internal double ContentWidth;
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(new Size(MeasureWidth, availableSize.Height));
        return new Size(MeasureWidth, availableSize.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is MaterialCarouselTileContent content) content.MaskWidth = finalSize.Width;
        Child?.Arrange(new Rect((finalSize.Width - ContentWidth) / 2, 0, ContentWidth, finalSize.Height));
        return finalSize;
    }
}

// Separate image arrangement from a reserved caption lane. No visibility/measure mutation in Arrange.
internal sealed class MaterialCarouselTileContent : Panel
{
    private readonly Control _image;
    private readonly Control? _overlay;
    private readonly MaterialInputScope _footer;
    private double _captionWidth;
    private MaterialModalPaintScope? _footerGate;
    internal double MaskWidth;
    internal MaterialCarouselTileContent(Control image, Control? overlay, Border footer)
    {
        _image = image; _overlay = overlay; _footer = new MaterialInputScope(footer);
        Children.Add(image);
        if (overlay is not null) Children.Add(overlay);
        Children.Add(_footer);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        _captionWidth = availableSize.Width;
        _image.Measure(availableSize);
        _overlay?.Measure(availableSize);
        _footer.Measure(new Size(_captionWidth, double.PositiveInfinity));
        return availableSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        _image.Arrange(new Rect(finalSize));
        _overlay?.Arrange(new Rect(finalSize));
        _footer.Opacity = Math.Clamp((MaskWidth - 116) / 8, 0, 1);
        MaterialModalPaintScope.SetInputEnabled(ref _footerGate, _footer, MaskWidth >= 120 || VisualRoot is null);
        _footer.IsHitTestVisible = MaskWidth >= 120;
        _footer.Arrange(new Rect((finalSize.Width - MaskWidth) / 2, Math.Max(0, finalSize.Height - _footer.DesiredSize.Height), _captionWidth, _footer.DesiredSize.Height));
        return finalSize;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { MaterialModalPaintScope.SetInputEnabled(ref _footerGate, _footer, true); base.OnDetachedFromVisualTree(e); }
}
