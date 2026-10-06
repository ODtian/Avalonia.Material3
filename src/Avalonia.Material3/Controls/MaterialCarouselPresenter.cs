using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Automation;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Avalonia.Material3.Controls;

/// <summary>Template infrastructure; layout algorithms are an Avalonia projection of keyline masking.</summary>
public sealed class MaterialCarouselPresenter : Panel
{
    public static readonly StyledProperty<MaterialCarousel?> CarouselProperty = AvaloniaProperty.Register<MaterialCarouselPresenter, MaterialCarousel?>(nameof(Carousel));
    public MaterialCarousel? Carousel { get => GetValue(CarouselProperty); set => SetValue(CarouselProperty, value); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Carousel is { } owner) { owner.PresentationChanged -= Rebuild; owner.PresentationChanged += Rebuild; }
        Rebuild();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Carousel is { } owner) owner.PresentationChanged -= Rebuild;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != CarouselProperty) return;
        if (change.OldValue is MaterialCarousel old) old.PresentationChanged -= Rebuild;
        if (Carousel is { } carousel) carousel.PresentationChanged += Rebuild;
        Rebuild();
    }
    private void Rebuild()
    {
        if (Carousel is not { } carousel) { Children.Clear(); return; }
        if (carousel.ItemList.Count == 0)
        {
            Children.Clear();
            Children.Add(Text(carousel.EmptyText));
            InvalidateMeasure();
            return;
        }
        if (Children.Count != carousel.ItemList.Count || Children.Where((b, i) => b.Tag != carousel.ItemList[i]).Any())
        {
            Children.Clear();
            foreach (var item in carousel.ItemList)
            {
                var border = new Border { Tag = item, ClipToBounds = true };
                border.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
                Children.Add(border);
            }
        }
        foreach (var border in Children.OfType<Border>())
        {
            border.CornerRadius = carousel.Layout == MaterialCarouselLayout.FullScreen ? default : carousel.CornerRadius;
            var item = (MaterialCarouselItem)border.Tag!;
            // Reuse each tile while dragging; host state/template changes rebuild only its content.
            var signature = (item.Image, item.Title, item.State, item.ErrorMessage, item.Content, carousel.ItemTemplate);
            if (border.Child?.Tag is not ValueTuple<IImage?, string?, MaterialCarouselItemState, string?, object?, Avalonia.Controls.Templates.IDataTemplate?> old || old != signature)
            {
                var content = BuildTile(carousel, item);
                content.Tag = signature;
                border.Child = content;
            }
        }
        InvalidateMeasure();
    }
    private static Control BuildTile(MaterialCarousel owner, MaterialCarouselItem item)
    {
        if (owner.ItemTemplate is { } template) return template.Build(item) ?? new Panel();
        var grid = new Grid();
        var image = new Image { Source = item.Image, Stretch = Stretch.UniformToFill, IsVisible = item.Image is not null };
        image.Styles.Add(new Style(selector => selector.OfType<Image>().Class(":disabled"))
        {
            Setters = { new Setter(OpacityProperty, new DynamicResourceExtension("M3.DisabledForegroundOpacity")) }
        });
        AutomationProperties.SetName(image, item.Title);
        grid.Children.Add(image);
        if (item.State == MaterialCarouselItemState.Loading)
            grid.Children.Add(new MaterialLoadingIndicator { IsContained = true, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        else if (item.State == MaterialCarouselItemState.Failed)
        {
            var panel = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8) };
            var error = Text(item.ErrorMessage ?? "Image could not be loaded");
            error.MaxLines = 2;
            error.TextTrimming = TextTrimming.CharacterEllipsis;
            error.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("M3.ErrorBrush"));
            panel.Children.Add(error);
            var retry = new MaterialButton { Content = "Retry", HorizontalAlignment = HorizontalAlignment.Center };
            AutomationProperties.SetName(retry, "Retry " + item.Title);
            retry.Click += (_, _) => owner.RequestRetry(item);
            panel.Children.Add(retry);
            var backing = new Border { Child = panel, VerticalAlignment = VerticalAlignment.Center };
            backing.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
            grid.Children.Add(backing);
        }
        var caption = new StackPanel { Margin = new Thickness(12, 4), Tag = "caption" };
        caption.Children.Add(Text(item.Title ?? ""));
        if (item.Content is not null) caption.Children.Add(new ContentControl { Content = item.Content });
        var footer = new Border { Child = caption, VerticalAlignment = VerticalAlignment.Bottom, Tag = "footer", IsVisible = item.State != MaterialCarouselItemState.Failed };
        footer.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerHighBrush"));
        grid.Children.Add(footer);
        return grid;
    }
    private static TextBlock Text(string text)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        block.Styles.Add(new Style(selector => selector.OfType<TextBlock>().Class(":disabled"))
        {
            Setters = { new Setter(OpacityProperty, new DynamicResourceExtension("M3.DisabledForegroundOpacity")) }
        });
        block.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("M3.OnSurfaceBrush"));
        foreach (var (property, suffix) in new (AvaloniaProperty, string)[] { (TextBlock.FontFamilyProperty, "FontFamily"), (TextBlock.FontSizeProperty, "FontSize"), (TextBlock.FontWeightProperty, "FontWeight"), (TextBlock.LineHeightProperty, "LineHeight"), (TextBlock.LetterSpacingProperty, "LetterSpacing") })
            block.Bind(property, new DynamicResourceExtension("M3.BodyMedium" + suffix));
        return block;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : 400, double.IsFinite(availableSize.Height) ? availableSize.Height : 200);
        var contentWidth = Carousel?.ItemList.Count > 0 ? Positions(size, 0).Max(r => r.Width) : size.Width;
        foreach (var border in Children.OfType<Border>())
            if (border.Child is { } content) { content.Width = contentWidth; content.HorizontalAlignment = HorizontalAlignment.Center; }
        foreach (var child in Children) child.Measure(size);
        return size;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Carousel is not { } carousel || Children.Count == 0) return finalSize;
        if (carousel.ItemList.Count == 0) { Children[0].Arrange(new Rect(finalSize)); return finalSize; }
        var position = Math.Clamp(carousel.PresentationPosition, 0, Children.Count - 1);
        var start = (int)Math.Floor(position);
        var rectangles = Positions(finalSize, start);
        var target = Positions(finalSize, Math.Min(start + 1, Children.Count - 1));
        var progress = position - start;
        for (var i = 0; i < Children.Count; i++)
        {
            var from = rectangles[i];
            var to = target[i];
            if (Children[i] is Border { Child: Grid grid })
                foreach (var footer in grid.Children.Where(c => c.Tag is "footer"))
                    footer.IsVisible = ((MaterialCarouselItem)Children[i].Tag!).State != MaterialCarouselItemState.Failed && from.Width + (to.Width - from.Width) * progress >= 120;
            Children[i].Arrange(new Rect(from.X + (to.X - from.X) * progress, from.Y + (to.Y - from.Y) * progress,
                from.Width + (to.Width - from.Width) * progress, from.Height + (to.Height - from.Height) * progress));
        }
        return finalSize;
    }
    private Rect[] Positions(Size size, int index)
    {
        var owner = Carousel!;
        var count = Children.Count;
        var result = new Rect[count];
        var width = size.Width;
        var height = size.Height;
        var gap = owner.ItemSpacing;
        if (owner.Layout == MaterialCarouselLayout.FullScreen)
        {
            for (var i = 0; i < count; i++) result[i] = new Rect(0, (i - index) * height, width, height);
            return result;
        }
        if (owner.Layout == MaterialCarouselLayout.Uncontained)
        {
            var itemWidth = Math.Min(owner.PreferredItemWidth, width);
            var offset = Math.Min(index * (itemWidth + gap), Math.Max(0, count * (itemWidth + gap) - gap - width));
            for (var i = 0; i < count; i++) result[i] = new Rect(i * (itemWidth + gap) - offset, 0, itemWidth, height);
            return result;
        }
        double[] widths;
        if (width < 128 || count == 1) widths = [width];
        else if (owner.Layout == MaterialCarouselLayout.Hero)
        {
            var sideCount = Math.Min(count - 1, index > 0 && index < count - 1 ? 2 : 1);
            var large = Math.Max(48, width - sideCount * (48 + gap));
            widths = sideCount == 2 ? [48, large, 48] : index == count - 1 ? [48, large] : [large, 48];
        }
        else
        {
            var largeCount = Math.Max(1, (int)Math.Floor((width - 48 - gap) / (owner.PreferredItemWidth + gap)));
            largeCount = Math.Min(largeCount, count);
            var smallCount = Math.Min(2, count - largeCount);
            var medium = Math.Min(owner.PreferredItemWidth / 2, Math.Max(48, width / 4));
            var large = (width - (smallCount == 2 ? medium + 48 : smallCount * 48) - (largeCount + smallCount - 1) * gap) / largeCount;
            if (large < medium && smallCount == 2) { smallCount = 1; large = (width - 48 - largeCount * gap) / largeCount; }
            var largeWidths = Enumerable.Repeat(large, largeCount);
            var leadingPeeks = Math.Clamp(index - (count - largeCount - smallCount) - largeCount + 1, 0, smallCount);
            widths = leadingPeeks switch
            {
                2 => new[] { 48d, medium }.Concat(largeWidths).ToArray(),
                1 => new[] { 48d }.Concat(largeWidths).Concat(smallCount == 2 ? [medium] : Array.Empty<double>()).ToArray(),
                _ => largeWidths.Concat(smallCount == 2 ? [medium, 48] : smallCount == 1 ? [48d] : Array.Empty<double>()).ToArray()
            };
        }
        var focal = owner.Layout == MaterialCarouselLayout.Hero && index > 0 ? 1 : index == count - 1 ? widths.Length - 1 : 0;
        var start = Math.Clamp(index - focal, 0, count - widths.Length);
        var x = 0d;
        for (var i = start; i < start + widths.Length; i++)
        {
            var w = widths[i - start];
            result[i] = new Rect(x, 0, w, height);
            x += w + gap;
        }
        for (var i = start - 1; i >= 0; i--) result[i] = new Rect((i - start) * (48 + gap), 0, 48, height);
        for (var i = start + widths.Length; i < count; i++) result[i] = new Rect(x + (i - start - widths.Length) * (48 + gap), 0, 48, height);
        // Like Uncontained, arrange logical rectangles; Avalonia mirrors all horizontal forms once.
        return result;
    }
}
