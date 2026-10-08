using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3.ReferenceUi;

public sealed partial class ReferenceShell
{
    private Control CreateFab()
    {
        var root = new Grid { Margin = new Thickness(16) };
        var column = new StackPanel { Spacing = 16, VerticalAlignment = VerticalAlignment.Top };
        var fabs = Row(16,
            new MaterialFab { Size = MaterialFabSize.Small, Content = Symbol("edit") },
            new MaterialFab { Content = Symbol("edit") },
            new MaterialFab { Size = MaterialFabSize.Large, Content = Symbol("edit", 36) });
        foreach (var fab in fabs.Children) fab.VerticalAlignment = VerticalAlignment.Top;
        column.Children.Add(fabs);
        column.Children.Add(new MaterialExtendedFab { Icon = Symbol("add"), Content = "Create", HorizontalAlignment = HorizontalAlignment.Left });
        var toolbar = SetId(new MaterialToolbar { Orientation = Orientation.Horizontal, IsExpanded = true, HorizontalAlignment = HorizontalAlignment.Left }, "floating-toolbar");
        toolbar.LeadingItems.Add(IconButton("menu", "toolbar-menu", "Menu", () => { }));
        toolbar.Items.Add(IconButton("edit", "toolbar-edit", "Edit", () => { }));
        toolbar.Items.Add(IconButton("share", "toolbar-share", "Share", () => { }));
        toolbar.TrailingItems.Add(IconButton("more_vert", "toolbar-more", "More", () => { }));
        column.Children.Add(Button("Toggle toolbar", "toolbar-toggle", () => toolbar.IsExpanded = !toolbar.IsExpanded, MaterialButtonVariant.Text)); column.Children.Add(toolbar);
        root.Children.Add(column);
        var menu = SetId(new MaterialFabMenu { Anchor = MaterialActionAnchor.BottomEnd, ExpandLabel = "Create actions", CollapseLabel = "Create actions" }, "fab-menu");
        menu.Loaded += (_, _) => { if (menu.GetVisualDescendants().OfType<MaterialFab>().SingleOrDefault() is { } trigger) SetId(trigger, "fab-menu-toggle"); };
        foreach (var (label, icon) in new[] { ("Reply", "reply"), ("Reply all", "people"), ("Forward", "forward") })
        {
            var item = new MaterialFabMenuItem { Content = label, LeadingIcon = Symbol(icon) }; item.Click += (_, _) => menu.IsExpanded = false; menu.Items.Add(item);
        }
        root.Children.Add(menu); return root;
    }
    private Control CreateProgress()
    {
        var column = SceneColumn();
        column.Children.Add(Text("Determinate", MaterialTypeRole.TitleMedium));
        column.Children.Add(new MaterialLinearProgressIndicator { Value = .42 });
        column.Children.Add(new MaterialCircularProgressIndicator { Value = .42, HorizontalAlignment = HorizontalAlignment.Left });
        column.Children.Add(Text("Indeterminate", MaterialTypeRole.TitleMedium));
        column.Children.Add(SetId(new MaterialLinearProgressIndicator { IsIndeterminate = true }, "linear-progress"));
        column.Children.Add(SetId(new MaterialCircularProgressIndicator { IsIndeterminate = true, HorizontalAlignment = HorizontalAlignment.Left }, "circular-progress"));
        column.Children.Add(Text("Expressive", MaterialTypeRole.TitleMedium));
        column.Children.Add(SetId(new MaterialLinearProgressIndicator { IsIndeterminate = true, IsExpressive = true }, "wavy-progress"));
        column.Children.Add(SetId(new MaterialCircularProgressIndicator { IsIndeterminate = true, IsExpressive = true, HorizontalAlignment = HorizontalAlignment.Left }, "circular-wavy-progress"));
        column.Children.Add(SetId(new MaterialLoadingIndicator { HorizontalAlignment = HorizontalAlignment.Left }, "loading-indicator"));
        column.Children.Add(SetId(new MaterialLoadingIndicator { IsContained = true, HorizontalAlignment = HorizontalAlignment.Left }, "contained-loading-indicator"));
        return Scroll(column);
    }
    private Control CreateCarousel()
    {
        var column = SceneColumn(); var narrow = false;
        var image = NativeLandscape();
        var carousel = SetId(new MaterialCarousel { PreferredItemWidth = 196, ItemSpacing = 8, Height = 220, MaxWidth = 1200, HorizontalAlignment = HorizontalAlignment.Left,
            ItemsSource = Enumerable.Range(1, 6).Select(index => new MaterialCarouselItem { Title = "Landscape " + index, Image = image, Content = "Image " + index }).ToArray() }, "carousel");
        carousel.ItemTemplate = new FuncDataTemplate<MaterialCarouselItem>((item, _) =>
        {
            var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Height = 220 };
            grid.Children.Add(new Image { Source = item!.Image, Stretch = Stretch.UniformToFill });
            var label = new Border { Padding = new Thickness(10), Child = Text(item.Title + "\n" + item.Content, MaterialTypeRole.BodySmall) };
            label.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerBrush")); Grid.SetRow(label, 1); grid.Children.Add(label);
            return grid;
        });
        column.Children.Add(Button("Narrow / wide", "carousel-layout-toggle", () => { narrow = !narrow; carousel.MaxWidth = narrow ? 260 : 1200; }, MaterialButtonVariant.Text));
        column.Children.Add(Text("Multi-browse · adaptive masks", MaterialTypeRole.TitleMedium)); column.Children.Add(carousel); return Scroll(column);
    }
    private static DrawingImage NativeLandscape()
    {
        // Direct port of AndroidReference/res/drawable/landscape.xml, including its256×256 viewport.
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.Parse("#1C6956")), Geometry = new RectangleGeometry(new Rect(0, 0, 256, 256)) });
        group.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.Parse("#FFFAD0")), Geometry = Geometry.Parse("M166,40a32,32 0,1 0,64 0a32,32 0,1 0,-64 0") });
        group.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.Parse("#22AFAE")), Geometry = Geometry.Parse("M0,220L60,120L140,210L235,145L256,190V256H0Z") });
        return new DrawingImage { Drawing = group };
    }
    private Control CreateNavigation()
    {
        var column = SceneColumn(); column.Children.Add(Text("Content navigation", MaterialTypeRole.TitleMedium));
        var bar = SetId(new MaterialNavigationBar(), "navigation");
        var tabs = SetId(new MaterialTabs { Variant = MaterialTabVariant.Primary }, "tabs");
        foreach (var (label, index) in new[] { ("Home", 0), ("Library", 1), ("Activity", 2), ("Topics", 3) })
        {
            bar.Items.Add(SetId(new MaterialNavigationItem { Content = label, Icon = Symbol(index == 0 ? "home" : index == 1 ? "book" : "star"),
                Badge = index is 1 or 2 ? new MaterialBadge { Count = index == 1 ? 3 : null } : null }, "navigation-" + index));
            if (index < 3) tabs.Items.Add(new MaterialNavigationItem { Content = label });
        }
        bar.SelectedIndex = tabs.SelectedIndex = 2;
        var synchronizing = false;
        bar.SelectionChanged += (_, _) => { if (synchronizing) return; synchronizing = true; try { tabs.SelectedIndex = Math.Min(bar.SelectedIndex, 2); } finally { synchronizing = false; } };
        tabs.SelectionChanged += (_, _) => { if (synchronizing) return; synchronizing = true; try { bar.SelectedIndex = tabs.SelectedIndex; } finally { synchronizing = false; } };
        column.Children.Add(bar); column.Children.Add(tabs); return Scroll(column);
    }
    private Control CreateOverlays()
    {
        var column = SceneColumn();
        MaterialButton? menuEntry = null;
        menuEntry = Button("Menu", "open-menu", () =>
        {
            var menu = new MaterialMenu { Variant = MaterialMenuVariant.LegacyDropdown };
            foreach (var label in new[] { "Edit", "Share", "Save" }) menu.Items.Add(new MaterialMenuItem { Content = label });
            menu.Show(Overlay, menuEntry!);
        });
        column.Children.Add(menuEntry);
        column.Children.Add(Button("Dialog", "open-dialog", () => new MaterialDialog { Title = "Edit draft", Content = "Apply changes?" }.Show(Overlay)));
        column.Children.Add(Button("Bottom sheet", "open-sheet", () =>
        {
            var body = new StackPanel();
            var title = Text("Bottom sheet", MaterialTypeRole.HeadlineSmall); title.Margin = new Thickness(24); body.Children.Add(title);
            var label = Text("Official default handle and transitions"); label.Margin = new Thickness(24); body.Children.Add(label); body.Children.Add(new Border { Height = 240 });
            new MaterialBottomSheet { Content = body }.Show(Overlay);
        }));
        column.Children.Add(Button("Side sheet · MDC Android", "open-side-sheet", () =>
        {
            var body = new TextBlock { Text = "Standard side information\nEditable draft\nIndependent content", FontSize = 18, LineHeight = 24, TextWrapping = TextWrapping.Wrap };
            var sheet = new MaterialSideSheet { Title = "Side sheet · MDC Android 1.14.0", Content = body, Padding = new Thickness(16, 0, 16, 16) };
            sheet.Loaded += (_, _) => { if (sheet.GetVisualDescendants().OfType<MaterialIconButton>().SingleOrDefault() is { } close) SetId(close, "side-sheet-close"); };
            sheet.Show(Overlay);
        }));
        var tooltipEntry = IconButton("save", "tooltip", "Save", () => { }); column.Children.Add(tooltipEntry);
        var tooltip = new MaterialTooltip { Content = "Save draft" }; _sceneLifetime.Add(tooltip.Attach(Overlay, tooltipEntry));
        return Scroll(column);
    }
}
