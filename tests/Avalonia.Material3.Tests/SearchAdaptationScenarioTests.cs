using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SearchAdaptationScenarioTests
{
    [AvaloniaTheory]
    [InlineData(MaterialChipVariant.Assist)]
    [InlineData(MaterialChipVariant.Filter)]
    [InlineData(MaterialChipVariant.Input)]
    [InlineData(MaterialChipVariant.Suggestion)]
    public void Long_bilingual_chips_wrap_at_200_percent_and_keep_full_accessible_names(MaterialChipVariant variant)
    {
        const string label = "很长的中英文混排标签 — Complete bilingual document collection label without loss of accessible content";
        var chip = new MaterialChip { ChipVariant = variant, Content = label };
        using var host = new SearchScenarioHost(chip, 320, 900);
        host.Theme.Typography = new MaterialTypography { Scale = 2 };
        host.Capture();
        Assert.True(chip.Bounds.Width <= 296);
        Assert.True(chip.Bounds.Height >= 120);
        Assert.Equal(label, ControlAutomationPeer.CreatePeerForElement(chip)!.GetName());
        var before = chip.Bounds.Size;
        chip.Focus(NavigationMethod.Tab);
        host.Capture();
        Assert.Equal(before, chip.Bounds.Size);
        chip.IsEnabled = false;
        host.Press(PhysicalKey.Space);
        Assert.False(chip.IsChecked);
    }

    [AvaloniaTheory]
    [InlineData(MaterialSearchMode.Bar)]
    [InlineData(MaterialSearchMode.View)]
    [InlineData(MaterialSearchMode.FilledAutocomplete)]
    [InlineData(MaterialSearchMode.OutlinedAutocomplete)]
    public void Existing_search_forms_follow_live_semantic_colors_full_body_typography_and_mode_switches(MaterialSearchMode mode)
    {
        var search = new MaterialSearch { Mode = mode, Text = "中文 Atlas" };
        using var host = new SearchScenarioHost(search, 320, 800);
        var editor = search.Editor;
        host.Theme.LightColorScheme = MaterialColorScheme.Light with { SurfaceContainerHigh = Color.Parse("#FFEEDD"), OnSurface = Color.Parse("#123456") };
        host.Theme.DarkColorScheme = MaterialColorScheme.Dark with { SurfaceContainerHigh = Color.Parse("#334455"), OnSurface = Color.Parse("#FEDCBA") };
        host.Theme.Typography = new MaterialTypography { BodyLarge = new MaterialTypeStyle(19, 29, 0.7, FontWeight.Bold) { FontFamily = new FontFamily("Arial") }, Scale = 2 };
        host.Capture();
        Assert.Equal(38, editor!.FontSize);
        Assert.Equal(58, editor.LineHeight);
        Assert.Equal(1.4, editor.LetterSpacing);
        Assert.Equal(FontWeight.Bold, editor.FontWeight);
        Assert.Equal(Color.Parse("#123456"), Assert.IsAssignableFrom<ISolidColorBrush>(editor.Foreground).Color);
        host.Window.RequestedThemeVariant = ThemeVariant.Dark;
        host.Capture();
        Assert.Equal(Color.Parse("#FEDCBA"), Assert.IsAssignableFrom<ISolidColorBrush>(editor.Foreground).Color);
        search.Mode = MaterialSearchMode.Bar;
        search.Close();
        Assert.Same(editor, search.Editor);
        Assert.Equal("中文 Atlas", editor.Text);
        Assert.Equal(Color.Parse("#334455"), Assert.IsAssignableFrom<ISolidColorBrush>(search.Background).Color);
        host.Theme.Shapes = host.Theme.Shapes with { CornerFull = 17 };
        Assert.Equal(new CornerRadius(17), search.CornerRadius);
    }

    [AvaloniaFact]
    public void Host_can_replace_search_template_using_public_parts_while_retaining_native_editing_and_submission()
    {
        var search = new MaterialSearch { Text = "original" };
        using var host = new SearchScenarioHost(search);
        search.Template = new FuncControlTemplate<MaterialSearch>((owner, names) =>
        {
            var editor = new MaterialTextField { Label = "Custom search", ShowClearButton = true };
            editor.Bind(TextBox.TextProperty, new Binding(nameof(MaterialSearch.Text)) { Source = owner, Mode = BindingMode.TwoWay });
            names.Register("PART_Editor", editor);
            var submit = new MaterialButton { Content = "Custom submit" };
            names.Register("PART_SubmitButton", submit);
            return new StackPanel { Children = { editor, submit } };
        });
        host.Capture();
        search.Editor!.Focus();
        host.Press(PhysicalKey.A, RawInputModifiers.Control);
        host.Window.KeyTextInput("new 中文");
        Assert.Equal("new 中文", search.Text);
        MaterialQuerySubmittedEventArgs? result = null;
        search.QuerySubmitted += (_, args) => result = args;
        var submit = search.GetVisualDescendants().OfType<MaterialButton>().Single(button => Equals(button.Content, "Custom submit"));
        host.Click(submit);
        Assert.Equal("new 中文", result!.Text);
        Assert.Equal(MaterialQuerySubmissionReason.SubmitButton, result.Reason);
    }

    [AvaloniaFact]
    public void Many_candidates_scroll_to_the_keyboard_selected_full_label_in_a_bounded_viewport()
    {
        var candidates = Enumerable.Range(0, 30).Select(i => new MaterialSearchToken(i.ToString(), $"Document {i} / 文档完整候选标签")).ToArray();
        var search = new MaterialSearch { Candidates = candidates, CandidateMaxHeight = 144 };
        using var host = new SearchScenarioHost(search);
        search.Editor!.Focus();
        for (var i = 0; i < 30; i++) { host.Press(PhysicalKey.ArrowDown); host.Capture(); }
        Assert.Equal("Document 29 / 文档完整候选标签", search.SelectedCandidate!.Label);
        var list = search.GetVisualDescendants().OfType<ListBox>().Single();
        Assert.True(list.Bounds.Height <= 144);
        Assert.Contains(list.GetVisualDescendants().OfType<ScrollViewer>(), viewer => viewer.Offset.Y > 0);
        host.Press(PhysicalKey.Enter);
        Assert.Equal("29", Assert.Single(search.Tokens).Key);
    }

    [AvaloniaFact]
    public void Leaving_search_dismisses_suggestions_without_trapping_focus_or_submitting()
    {
        var search = new MaterialSearch { Text = "kept" };
        using var host = new SearchScenarioHost(search);
        var next = new MaterialButton { Content = "Next host action" };
        Assert.IsType<StackPanel>(host.Window.Content).Children.Add(next);
        host.Capture();
        var submitted = 0;
        var activated = 0;
        search.QuerySubmitted += (_, _) => submitted++;
        next.Click += (_, _) => activated++;
        search.Open();
        next.Focus(NavigationMethod.Tab);
        Assert.False(search.IsOpen);
        Assert.True(next.IsFocused);
        host.Press(PhysicalKey.Enter);
        Assert.Equal(0, submitted);
        Assert.Equal(1, activated);
        Assert.Equal("kept", search.Text);
    }

    [AvaloniaFact]
    public void Duplicate_keys_stale_candidates_and_read_only_token_collections_do_not_change_the_query()
    {
        var search = new MaterialSearch { Text = "kept" };
        var token = new MaterialSearchToken("a", "Atlas");
        Assert.True(search.AddToken(token));
        Assert.False(search.AddToken(new MaterialSearchToken("a", "Different label")));
        search.SelectedCandidate = token;
        Assert.False(search.AcceptCandidate());
        search.Tokens = Array.AsReadOnly(new[] { token });
        Assert.False(search.AddToken(new MaterialSearchToken("b", "Bilingual")));
        Assert.False(search.RemoveToken(token));
        Assert.Equal("kept", search.Text);
        Assert.Single(search.Tokens);
        Assert.Throws<ArgumentOutOfRangeException>(() => search.SubmitQuery((MaterialQuerySubmissionReason)100));
    }

    [AvaloniaFact]
    public void Host_selection_cannot_point_to_a_token_outside_the_query_and_reattachment_resynchronizes_external_tokens()
    {
        var token = new MaterialSearchToken("atlas", "Atlas / 图集");
        var tokens = new AvaloniaList<MaterialSearchToken> { token };
        var search = new MaterialSearch { Tokens = tokens };
        using var host = new SearchScenarioHost(search);
        search.SelectedToken = new MaterialSearchToken("missing", "Missing");
        Assert.Null(search.SelectedToken);
        search.SelectedToken = token;
        var root = Assert.IsType<StackPanel>(host.Window.Content);
        root.Children.Remove(search);
        tokens.Clear();
        root.Children.Add(search);
        host.Capture();
        Assert.Null(search.SelectedToken);
        Assert.Empty(search.GetVisualDescendants().OfType<MaterialChip>());
    }
}
