using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Package-only dialog/overlay consumption demo. No application-specific domain state.</summary>
public sealed class DialogsPage : UserControl
{
    public MaterialOverlayHost Overlay { get; }
    public MaterialButton BasicButton { get; } = Action("Edit basic", "BasicEntry");
    public MaterialButton FullScreenButton { get; } = Action("Edit full screen", "FullScreenEntry");
    public MaterialButton LongButton { get; } = Action("Long content", "LongEntry");
    public MaterialButton NestedButton { get; } = Action("Nested confirmation", "NestedEntry");
    public MaterialButton PopupButton { get; } = Action("Generic anchored overlay", "PopupEntry");
    public TextBlock Result { get; } = new() { Name = "DialogResult", Text = "Waiting", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    public MaterialDialog? ActiveDialog { get; private set; }
    public MaterialTextField? Editor { get; private set; }
    public MaterialOverlaySession? LastSession { get; private set; }

    public DialogsPage(MaterialTheme theme, MaterialOverlayHost? windowOverlayHost = null)
    {
        Overlay = windowOverlayHost ?? new MaterialOverlayHost { Name = "DialogOverlay" };
        var mode = Action("Light / dark", "DialogTheme");
        var font = Action("100 / 200% text", "DialogFont");
        var back = Action("Host back", "DialogBack");
        mode.Click += (_, _) => { if (TopLevel.GetTopLevel(this) is Window window) window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark; };
        font.Click += (_, _) => theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 2 : 1 };
        back.Click += (_, _) => Overlay.RequestBack();
        BasicButton.Click += (_, _) => Edit(MaterialDialogMode.Basic, BasicButton);
        FullScreenButton.Click += (_, _) => Edit(MaterialDialogMode.FullScreen, FullScreenButton);
        LongButton.Click += (_, _) => Present(new MaterialDialog
        {
            Title = "Review long content / 长内容", Icon = Symbols.Create("info"), ConfirmText = "Accept",
            Content = string.Join("\n", Enumerable.Repeat("Generic reference text / 中文混排. Resize the window, scale text, or scroll while actions remain reachable.", 60))
        }, LongButton);
        NestedButton.Click += (_, _) =>
        {
            var child = Action("Open nested", "NestedLauncher");
            var modalBack = Action("Host back", "ModalBack");
            modalBack.Click += (_, _) => Overlay.RequestBack();
            var outer = new MaterialDialog { Title = "Outer dialog", Content = new StackPanel { Children = { child, modalBack } }, ConfirmText = "Finish" };
            child.Click += (_, _) => Present(new MaterialDialog { Title = "Nested confirmation", Content = "Only the top presentation receives dismissal and focus.", ConfirmText = "Confirm", ConfirmResult = "Nested confirmed" }, child);
            Present(outer, NestedButton);
        };
        PopupButton.Click += (_, _) =>
        {
            var action = Action("Choose sample", "PopupAction");
            var surface = new Border { CornerRadius = new CornerRadius(12), Padding = new Thickness(8), Child = action };
            surface.Bind(Border.BackgroundProperty, new DynamicResourceExtension("M3.SurfaceContainerBrush"));
            var session = Overlay.Show(surface, new MaterialOverlayOptions { IsModal = false, Placement = MaterialOverlayPlacement.Anchor, Anchor = PopupButton, ReturnFocus = PopupButton, CloseOnLightDismiss = true, Margin = new Thickness(8) });
            action.Click += (_, _) => session.Close("Sample selected");
            Observe(session);
        };
        AutomationProperties.SetLiveSetting(Result, AutomationLiveSetting.Polite);
        var entries = new WrapPanel { Children = { BasicButton, FullScreenButton, LongButton, NestedButton, PopupButton } };
        var pageContent = new ScrollViewer { Content = new StackPanel
        {
            Margin = new Thickness(24), Spacing = 16, Children =
            {
                new TextBlock { Text = "M3-12 Dialogs and reusable overlays", Classes = { "m3-headline-small" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new TextBlock { Text = "Edit → validate → confirm/cancel → result → entry focus. Escape/Back dismiss only the top. Basic and full-screen surfaces share the generic host contract.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                entries, new WrapPanel { Children = { mode, font, back } }, Result,
                new TextBlock { Text = "Keyboard: Tab / Shift+Tab stay in a modal, Enter/Space activate the focused action. A native editor owns its text and IME; Enter does not implicitly save an editing form. Long content is independently scrollable. Theme/font buttons apply on the next presentation; window resizing also applies while open.", TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            }
        } };
        if (windowOverlayHost is null) { Overlay.Content = pageContent; Content = Overlay; }
        else Content = pageContent;
    }
    private void Edit(MaterialDialogMode mode, Control entry)
    {
        var editor = new MaterialTextField { Name = "DialogEditor", Label = "Display name", Text = "Sample", SupportingText = "Enter a non-empty name", Variant = MaterialTextFieldVariant.Outlined };
        Editor = editor;
        var dialog = new MaterialDialog { Mode = mode, Title = "Edit details", Icon = Symbols.Create("star"), ConfirmText = "Save", Content = editor };
        dialog.Confirming += (_, args) =>
        {
            args.Cancel = string.IsNullOrWhiteSpace(editor.Text);
            editor.IsError = args.Cancel;
            editor.ErrorText = args.Cancel ? "A name is required" : null;
            args.Value = editor.Text;
            if (args.Cancel) editor.Focus();
        };
        Present(dialog, entry, editor);
    }
    private void Present(MaterialDialog dialog, Control entry, Control? initialFocus = null)
    {
        ActiveDialog = dialog;
        Observe(dialog.Show(Overlay, new MaterialOverlayOptions
        {
            InitialFocus = initialFocus, ReturnFocus = entry,
            Placement = dialog.Mode == MaterialDialogMode.FullScreen ? MaterialOverlayPlacement.FullScreen : MaterialOverlayPlacement.Center,
            Margin = dialog.Mode == MaterialDialogMode.FullScreen ? default : new Thickness(24)
        }));
    }
    private void Observe(MaterialOverlaySession session)
    {
        LastSession = session;
        session.Closed += (_, result) => Result.Text = result.Value is null ? result.Reason.ToString() : $"{result.Reason}: {result.Value}";
    }
    private static MaterialButton Action(string label, string id) => new() { Content = label, Name = id, Margin = new Thickness(4), Variant = MaterialButtonVariant.Text };
}
