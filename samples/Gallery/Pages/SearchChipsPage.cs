using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gallery.Pages;

/// <summary>Generic package-only search/token scenario. No persistence, network or product history.</summary>
public sealed class SearchChipsPage : StackPanel
{
    private static readonly MaterialSearchToken Atlas = new("atlas", "Atlas / 图集");
    private static readonly MaterialSearchToken OpenAccess = new("open", "Open access / 开放");
    private static readonly MaterialSearchToken Long = new("mixed", "很长的中英文混排标签 — Bilingual accessible document collection with a complete descriptive label");
    private static readonly MaterialSearchToken[] Catalog = [Atlas, OpenAccess, Long];
    public MaterialSearch QuerySearch { get; } = new() { Label = "Find documents / 查找文档", SupportingText = "Down chooses a candidate; Enter accepts it. Search explicitly submits all tokens and remaining text.", Candidates = Catalog };
    public MaterialChip OpenAccessFilter { get; } = new() { ChipVariant = MaterialChipVariant.Filter, Content = OpenAccess.Label };
    public MaterialButton ThemeButton { get; } = new() { Content = "Light / Dark", Variant = MaterialButtonVariant.Tonal };
    public MaterialButton FontButton { get; } = new() { Content = "100% / 200%", Variant = MaterialButtonVariant.Tonal };
    public MaterialButton ErrorButton { get; } = new() { Content = "Candidate error", Variant = MaterialButtonVariant.Text };
    public MaterialButton EnabledButton { get; } = new() { Content = "Enabled / Disabled", Variant = MaterialButtonVariant.Text };
    public MaterialButton ModeButton { get; } = new() { Content = "Docked / Full screen", Variant = MaterialButtonVariant.Text };
    public TextBlock Result { get; } = new() { Text = "Waiting for explicit query", TextWrapping = TextWrapping.Wrap };
    public int SubmissionCount { get; private set; }
    public MaterialQuerySubmittedEventArgs? LastQuery { get; private set; }
    public IReadOnlyList<MaterialSearch> SearchForms { get; }
    public IReadOnlyList<MaterialChip> ChipForms { get; }

    public SearchChipsPage(MaterialTheme? theme = null)
    {
        Margin = new Thickness(16);
        Spacing = 8;
        theme ??= Application.Current?.Styles.OfType<MaterialTheme>().FirstOrDefault();
        AddHeading("Search and editable query chips / 搜索与词条", true);
        AddText("Native selection, undo and IME remain in the editor. Choosing, adding, selecting or removing tokens is NOT a search request.");
        var controls = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var button in new[] { ThemeButton, FontButton, ErrorButton, EnabledButton, ModeButton })
        {
            button.MaxWidth = 260;
            button.ContentTemplate = new FuncDataTemplate<object>((content, _) => new TextBlock { Text = content?.ToString(), TextWrapping = TextWrapping.Wrap });
            controls.Children.Add(button);
        }
        Children.Add(controls);
        Id(ThemeButton, "ThemeButton"); Id(FontButton, "FontButton"); Id(ErrorButton, "ErrorButton"); Id(EnabledButton, "EnabledButton"); Id(ModeButton, "ModeButton");
        ThemeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        };
        FontButton.IsEnabled = theme is not null;
        FontButton.Click += (_, _) => { if (theme is not null) theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 2 : 1 }; };
        ErrorButton.Click += (_, _) => QuerySearch.ErrorText = QuerySearch.HasError ? null : "Candidate source unavailable / 候选不可用. Correct or retry before submitting.";
        EnabledButton.Click += (_, _) => QuerySearch.IsEnabled = !QuerySearch.IsEnabled;
        ModeButton.Click += (_, _) =>
        {
            QuerySearch.ViewPresentation = QuerySearch.ViewPresentation == MaterialSearchViewPresentation.Docked ? MaterialSearchViewPresentation.FullScreen : MaterialSearchViewPresentation.Docked;
            QuerySearch.Open();
        };
        Id(QuerySearch, "QuerySearch"); Id(OpenAccessFilter, "OpenAccessFilter"); Id(Result, "QueryResult");
        QuerySearch.PropertyChanged += (_, args) =>
        {
            if (args.Property == MaterialSearch.EditorProperty && QuerySearch.Editor is { } editor) Id(editor, "QueryEditor");
            if (args.Property == MaterialSearch.TextProperty)
                QuerySearch.Candidates = string.IsNullOrWhiteSpace(QuerySearch.Text) ? Catalog : Catalog.Where(token => token.Label.Contains(QuerySearch.Text, StringComparison.OrdinalIgnoreCase)).ToArray();
        };
        QuerySearch.GotFocus += (_, args) => { if (ReferenceEquals(args.Source, QuerySearch.Editor)) QuerySearch.BringIntoView(); };
        QuerySearch.QuerySubmitted += (_, args) =>
        {
            LastQuery = args;
            SubmissionCount++;
            Result.Text = $"Submitted {SubmissionCount} ({args.Reason}): [{string.Join(" | ", args.Tokens.Select(token => token.Label))}] + {args.Text}";
        };
        OpenAccessFilter.Click += (_, _) =>
        {
            if (OpenAccessFilter.IsChecked) QuerySearch.AddToken(OpenAccess);
            else QuerySearch.RemoveToken(OpenAccess);
        };
        Children.Add(QuerySearch);
        Children.Add(OpenAccessFilter);
        Children.Add(Result);

        AddHeading("Pinned search forms / 搜索形态");
        var forms = new List<MaterialSearch>();
        foreach (var mode in Enum.GetValues<MaterialSearchMode>())
        {
            AddText(mode.ToString());
            var form = new MaterialSearch { Mode = mode, Label = mode + " / 示例", Candidates = Catalog,
                CandidateAction = mode is MaterialSearchMode.FilledAutocomplete or MaterialSearchMode.OutlinedAutocomplete ? MaterialCandidateAction.ReplaceText : MaterialCandidateAction.AddToken,
                SupportingText = "Host owns candidates and validation; selection does not execute a query." };
            Id(form, "Form" + mode);
            forms.Add(form);
            Children.Add(form);
        }
        AddText("Full-screen search view chrome — host gives this view a bounded viewport (not a modal dialog).");
        var full = new MaterialSearch { Mode = MaterialSearchMode.View, ViewPresentation = MaterialSearchViewPresentation.FullScreen,
            Label = "Full-screen view / 全屏搜索", Height = 360, CandidateMaxHeight = 600, Candidates = Catalog };
        forms.Add(full); Children.Add(full);
        SearchForms = forms.AsReadOnly();
        var outlined = forms.Single(form => form.Mode == MaterialSearchMode.OutlinedAutocomplete);
        outlined.CandidateTemplate = new FuncDataTemplate<MaterialSearchToken>((token, _) => new TextBlock { Text = token?.Label + " / Preview", TextWrapping = TextWrapping.Wrap });
        AddText("Read-only saved query (submit remains available) and disabled retained query");
        Children.Add(new MaterialSearch { Label = "Saved query", Text = "中文 Atlas", IsReadOnly = true });
        Children.Add(new MaterialSearch { Label = "Disabled query", Text = "Retained / 保留", IsEnabled = false });

        AddHeading("Assist, filter, input, suggestion / Chip variants");
        var chips = new List<MaterialChip>();
        foreach (var variant in Enum.GetValues<MaterialChipVariant>())
        {
            AddText(variant + ": flat, elevated (except input), selected, disabled and long mixed label");
            foreach (var state in new[] { "Flat", "Elevated", "Selected", "Disabled", "Long label" })
            {
                var chip = new MaterialChip { ChipVariant = variant, Content = state == "Long label" ? Long.Label : variant + " / " + state,
                    IsElevated = state == "Elevated", IsChecked = state == "Selected" && variant is MaterialChipVariant.Input or MaterialChipVariant.Filter,
                    IsEnabled = state != "Disabled", LeadingIcon = Symbols.Create("add", 18),
                    RemoveButtonLabel = "Remove " + variant + " " + state, HorizontalAlignment = HorizontalAlignment.Stretch };
                if (variant == MaterialChipVariant.Input && state == "Flat") chip.Avatar = new TextBlock { Text = "A" };
                chip.Click += (_, _) =>
                {
                    if (variant == MaterialChipVariant.Suggestion) QuerySearch.AddToken(Long);
                    else if (variant == MaterialChipVariant.Assist) Result.Text = "Assist action activated; query not submitted";
                };
                chip.RemovalRequested += (_, args) => { args.Handled = true; chip.IsVisible = false; Result.Text = "Input chip removed; query not submitted"; };
                chips.Add(chip); Children.Add(chip);
            }
        }
        ChipForms = chips.AsReadOnly();
        AddText("Keyboard: Tab / Shift+Tab, Space on chips, Down/Up in candidates, Enter to accept (not submit), Escape to dismiss. Remove is a separate named action. Full labels remain accessible when wrapping.");
    }
    private static void Id(Control control, string id) => AutomationProperties.SetAutomationId(control, id);
    private void AddText(string text) => Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
    private void AddHeading(string text, bool main = false)
    {
        var heading = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        heading.Classes.Add(main ? "m3-title-large" : "m3-title-medium");
        Children.Add(heading);
    }
}
