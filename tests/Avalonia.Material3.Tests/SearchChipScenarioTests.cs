using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SearchChipScenarioTests
{
    [AvaloniaFact]
    public void Two_way_query_binding_reports_setter_errors_and_recovers_after_native_correction()
    {
        var search = new MaterialSearch();
        var model = new TextFieldScenarioTests.CodeModel();
        search.Bind(MaterialSearch.TextProperty, new Binding(nameof(model.Value)) { Source = model, Mode = BindingMode.TwoWay });
        using var host = new SearchScenarioHost(search);
        search.Editor!.Focus();
        host.Window.KeyTextInput("ab");
        Assert.True(search.HasError);
        Assert.False(search.SubmitQuery());
        Assert.Equal("Code is too short", search.EffectiveErrorText);
        host.Window.KeyTextInput("c");
        Assert.False(search.HasError);
        Assert.Equal("abc", model.Value);
        Assert.True(search.SubmitQuery());
    }

    [AvaloniaFact]
    public void Repeated_editor_arrows_navigate_more_than_one_candidate_and_pointer_release_outside_cancels()
    {
        var first = new MaterialSearchToken("a", "Atlas");
        var second = new MaterialSearchToken("b", "Bilingual / 双语");
        var search = new MaterialSearch { Candidates = new[] { first, second } };
        using var host = new SearchScenarioHost(search);
        search.Editor!.Focus();
        host.Press(PhysicalKey.ArrowDown);
        host.Press(PhysicalKey.ArrowDown);
        Assert.Equal(second, search.SelectedCandidate);
        host.Press(PhysicalKey.ArrowUp);
        Assert.Equal(first, search.SelectedCandidate);
        host.Capture();
        var item = host.Window.GetVisualDescendants().OfType<ListBoxItem>().First();
        var point = item.TranslatePoint(new Point(20, 20), host.Window)!.Value;
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(new Point(1, 1), MouseButton.Left);
        Assert.Empty(search.Tokens);
        host.Click(item);
        Assert.Equal(first, Assert.Single(search.Tokens));
    }

    [AvaloniaFact]
    public void Binding_validation_on_outer_search_contract_is_presented_and_blocks_explicit_submission()
    {
        var search = new MaterialSearch { Text = "kept", SupportingText = "Query helper" };
        using var host = new SearchScenarioHost(search);
        DataValidationErrors.SetErrors(search, new[] { "Host binding rejected query" });
        Assert.True(search.HasError);
        Assert.False(search.SubmitQuery());
        Assert.Contains("Host binding rejected query", ControlAutomationPeer.CreatePeerForElement(search.Editor!)!.GetHelpText());
        DataValidationErrors.ClearErrors(search);
        Assert.False(search.HasError);
        Assert.True(search.SubmitQuery());
        Assert.Equal("kept", search.Text);
    }

    [AvaloniaFact]
    public void Input_avatar_and_trailing_slot_are_present_and_disabled_chips_use_OnSurface_alpha_not_whole_control_opacity()
    {
        var chip = new MaterialChip { ChipVariant = MaterialChipVariant.Input, Content = "Atlas",
            Avatar = new TextBlock { Text = "A" }, TrailingIcon = new TextBlock { Text = "detail" } };
        using var host = new SearchScenarioHost(chip);
        Assert.Contains(chip.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "A" && text.IsEffectivelyVisible);
        Assert.Contains(chip.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "detail" && text.IsEffectivelyVisible);
        Assert.Equal(32, chip.GetVisualDescendants().OfType<Border>().Single(b => b.BorderThickness == new Thickness(1)).Bounds.Height);
        host.Theme.LightColorScheme = MaterialColorScheme.Light with { OnSurface = Color.Parse("#123456"), OnSurfaceVariant = Color.Parse("#ABCDEF") };
        chip.IsEnabled = false;
        host.Capture();
        Assert.Equal(Color.Parse("#123456"), Assert.IsAssignableFrom<ISolidColorBrush>(chip.Foreground).Color);
        Assert.Equal(0.38, chip.GetVisualDescendants().OfType<ContentPresenter>().Single(p => Equals(p.Content, "Atlas")).Opacity, 2);
        Assert.Equal(1, chip.Opacity);
    }

    [AvaloniaTheory]
    [InlineData(MaterialChipVariant.Assist)]
    [InlineData(MaterialChipVariant.Filter)]
    [InlineData(MaterialChipVariant.Input)]
    [InlineData(MaterialChipVariant.Suggestion)]
    public void Chip_visuals_use_32_DIP_containers_48_DIP_targets_and_live_complete_label_typography(MaterialChipVariant variant)
    {
        var chip = new MaterialChip { ChipVariant = variant, Content = "Atlas" };
        using var host = new SearchScenarioHost(chip);
        var container = chip.GetVisualDescendants().OfType<Border>().Single(b => b.BorderThickness == new Thickness(1));
        Assert.Equal(32, container.Bounds.Height);
        Assert.True(chip.Bounds.Height >= 48);
        host.Theme.Typography = new MaterialTypography { LabelLarge = new MaterialTypeStyle(18, 26, 0.7, FontWeight.Bold) { FontFamily = new FontFamily("Arial") }, Scale = 2 };
        host.Capture();
        Assert.Equal(36, chip.FontSize);
        Assert.Equal(new FontFamily("Arial"), chip.FontFamily);
        Assert.Equal(FontWeight.Bold, chip.FontWeight);
        Assert.Equal(1.4, chip.LetterSpacing);
        Assert.Equal(52, TextBlock.GetLineHeight(chip));
        Assert.True(chip.Bounds.Height > 48);
    }

    [AvaloniaFact]
    public void Host_candidate_templates_slots_and_localized_actions_preserve_native_input_and_explicit_intent()
    {
        var slot = new MaterialButton { Content = "Scope" };
        var atlas = new MaterialSearchToken("a", "Atlas");
        var search = new MaterialSearch { Candidates = new[] { atlas }, TrailingContent = slot,
            SubmitButtonLabel = "执行查询", AddButtonLabel = "添加词条", ClearButtonLabel = "清空查询",
            CandidateTemplate = new FuncDataTemplate<MaterialSearchToken>((token, _) => new TextBlock { Text = "Preview / 预览 " + token!.Label, TextWrapping = TextWrapping.Wrap }) };
        using var host = new SearchScenarioHost(search);
        search.Editor!.Focus();
        host.Window.KeyTextInput("kept");
        slot.Focus(NavigationMethod.Tab);
        host.Press(PhysicalKey.Space);
        host.Window.KeyTextInput(" ");
        Assert.Equal("kept", search.Text);
        search.Open();
        host.Capture();
        Assert.Contains(host.Window.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Preview / 预览 Atlas");
        var submit = host.Window.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "执行查询");
        MaterialQuerySubmittedEventArgs? result = null;
        search.QuerySubmitted += (_, args) => result = args;
        host.Click(submit);
        Assert.Equal(MaterialQuerySubmissionReason.SubmitButton, result!.Reason);
        Assert.Equal("kept", result.Text);
    }

    [AvaloniaFact]
    public void Automation_names_roles_expansion_and_candidate_selection_are_available_without_parsing_display_text()
    {
        var atlas = new MaterialSearchToken("atlas", "完整 bilingual Atlas 候选标签");
        var search = new MaterialSearch { Mode = MaterialSearchMode.FilledAutocomplete, Label = "Find documents", Candidates = new[] { atlas } };
        using var host = new SearchScenarioHost(search);
        var peer = ControlAutomationPeer.CreatePeerForElement(search)!;
        Assert.Equal("Find documents", peer.GetName());
        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());
        var expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer.GetProvider<IExpandCollapseProvider>());
        expand.Expand();
        host.Capture();
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        var item = host.Window.GetVisualDescendants().OfType<ListBoxItem>().Single();
        var candidatePeer = ControlAutomationPeer.CreatePeerForElement(item)!;
        Assert.Equal(atlas.Label, candidatePeer.GetName());
        var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(candidatePeer.GetProvider<ISelectionItemProvider>());
        selection.Select();
        Assert.Equal(atlas, search.SelectedCandidate);
        Assert.True(selection.IsSelected);
        expand.Collapse();
        Assert.False(search.IsOpen);
        Assert.True(search.Editor!.IsFocused);
    }

    [AvaloniaFact]
    public void Token_selection_keeps_keyboard_focus_and_named_removal_returns_to_editor_without_submit()
    {
        var search = new MaterialSearch();
        var atlas = new MaterialSearchToken("atlas", "Atlas / 图集");
        search.Tokens.Add(atlas);
        using var host = new SearchScenarioHost(search);
        var chip = host.Window.GetVisualDescendants().OfType<MaterialChip>().Single();
        chip.Focus(NavigationMethod.Tab);
        host.Press(PhysicalKey.Space);
        host.Capture();
        Assert.Equal(atlas, search.SelectedToken);
        Assert.True(chip.IsFocused);
        host.Press(PhysicalKey.Tab);
        var remove = host.Window.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Remove Atlas / 图集");
        Assert.True(remove.IsFocused);
        host.Press(PhysicalKey.Enter);
        Assert.Empty(search.Tokens);
        Assert.True(search.Editor!.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(MaterialSearchViewPresentation.Docked, 56)]
    [InlineData(MaterialSearchViewPresentation.FullScreen, 72)]
    public void Search_view_uses_pinned_header_shape_and_can_be_closed_without_losing_native_editing(MaterialSearchViewPresentation presentation, double height)
    {
        var search = new MaterialSearch { Mode = MaterialSearchMode.View, ViewPresentation = presentation,
            Candidates = new[] { new MaterialSearchToken("a", "Atlas / 图集") }, CloseButtonLabel = "Back to query" };
        using var host = new SearchScenarioHost(search);
        Assert.True(search.IsOpen);
        Assert.Equal(height, search.HeaderHeight);
        Assert.Equal(presentation == MaterialSearchViewPresentation.FullScreen ? new CornerRadius(0) : new CornerRadius(28), search.CornerRadius);
        search.Editor!.Focus();
        host.Window.KeyTextInput("kept / 保留");
        var close = host.Window.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Back to query");
        host.Click(close);
        Assert.False(search.IsOpen);
        Assert.Equal("kept / 保留", search.Editor.Text);
        Assert.True(search.Editor.IsFocused);
        host.Press(PhysicalKey.A, RawInputModifiers.Control);
        host.Window.KeyTextInput("edited / 已编辑");
        Assert.Equal("edited / 已编辑", search.Text);
    }

    [AvaloniaFact]
    public void Native_preedit_enter_and_arrows_do_not_accept_candidates_or_submit_uncommitted_text()
    {
        var search = new MaterialSearch { Candidates = new[] { new MaterialSearchToken("a", "Atlas") } };
        using var host = new SearchScenarioHost(search);
        var submitted = 0;
        search.QuerySubmitted += (_, _) => submitted++;
        search.Editor!.Focus();
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        search.Editor.RaiseEvent(request);
        var client = Assert.IsAssignableFrom<TextInputMethodClient>(request.Client);
        client.SetPreeditText("zhong", 5);
        host.Press(PhysicalKey.ArrowDown);
        host.Press(PhysicalKey.Enter);
        Assert.True(search.Editor.IsFocused);
        Assert.Null(search.SelectedCandidate);
        Assert.Equal(0, submitted);
        Assert.Empty(search.Tokens);
        client.SetPreeditText(null);
        host.Window.KeyTextInput("中");
        host.Press(PhysicalKey.Enter);
        Assert.Equal(1, submitted);
        Assert.Equal("中", search.Text);
    }

    [AvaloniaFact]
    public void Autocomplete_can_replace_native_text_without_adding_tokens_or_implicitly_submitting()
    {
        var chosen = new MaterialSearchToken("atlas", "完整 Atlas label");
        var search = new MaterialSearch { Mode = MaterialSearchMode.OutlinedAutocomplete,
            CandidateAction = MaterialCandidateAction.ReplaceText, Candidates = new[] { chosen } };
        using var host = new SearchScenarioHost(search);
        var submitted = 0;
        search.QuerySubmitted += (_, _) => submitted++;
        search.Editor!.Focus();
        host.Window.KeyTextInput("At");
        Assert.True(search.IsOpen);
        host.Press(PhysicalKey.ArrowDown);
        host.Press(PhysicalKey.Enter);
        Assert.Equal("完整 Atlas label", search.Editor.Text);
        Assert.Empty(search.Tokens);
        Assert.False(search.IsOpen);
        Assert.Equal(0, submitted);
        search.IsReadOnly = true;
        search.SelectedCandidate = chosen;
        Assert.False(search.AcceptCandidate());
        Assert.False(search.AddToken(chosen));
        Assert.True(search.SubmitQuery()); // Read-only is not disabled: a saved query can still be submitted.
    }

    [AvaloniaFact]
    public void Host_errors_and_native_validation_block_submit_without_discarding_edits_or_tokens()
    {
        var search = new MaterialSearch { Label = "Find / 查找", SupportingText = "Choose terms", ErrorText = "候选 unavailable" };
        using var host = new SearchScenarioHost(search);
        var submitted = 0;
        search.QuerySubmitted += (_, _) => submitted++;
        search.Editor!.Focus();
        host.Window.KeyTextInput("保留 draft");
        Assert.True(search.HasError);
        Assert.False(search.SubmitQuery());
        host.Press(PhysicalKey.Enter);
        Assert.Equal(0, submitted);
        var peer = ControlAutomationPeer.CreatePeerForElement(search.Editor)!;
        Assert.Equal("Find / 查找", peer.GetName());
        Assert.Contains("候选 unavailable", peer.GetHelpText());
        Assert.Equal("Invalid", peer.GetItemStatus());
        search.ErrorText = null;
        DataValidationErrors.SetErrors(search.Editor, new[] { "Invalid binding" });
        Assert.True(search.HasError);
        Assert.False(search.SubmitQuery());
        DataValidationErrors.ClearErrors(search.Editor);
        Assert.False(search.HasError);
        Assert.True(search.SubmitQuery());
        Assert.Equal("保留 draft", search.Text);
        Assert.Equal(1, submitted);
        search.IsEnabled = false;
        Assert.False(search.SubmitQuery());
        Assert.False(search.AddToken(new MaterialSearchToken("blocked", "Blocked")));
    }

    [AvaloniaTheory]
    [InlineData(MaterialSearchMode.Bar)]
    [InlineData(MaterialSearchMode.View)]
    [InlineData(MaterialSearchMode.FilledAutocomplete)]
    [InlineData(MaterialSearchMode.OutlinedAutocomplete)]
    public void Every_search_form_exposes_candidates_keyboard_acceptance_and_separate_submission(MaterialSearchMode mode)
    {
        var atlas = new MaterialSearchToken("atlas", "中文 Atlas candidate");
        var search = new MaterialSearch { Mode = mode, Candidates = new[] { atlas }, Label = "Find / 查找" };
        using var host = new SearchScenarioHost(search);
        var submissions = new List<MaterialQuerySubmittedEventArgs>();
        search.QuerySubmitted += (_, args) => submissions.Add(args);
        search.Editor!.Focus();
        host.Window.KeyTextInput("At");
        host.Press(PhysicalKey.ArrowDown);
        Assert.True(search.IsOpen);
        Assert.Equal(atlas, search.SelectedCandidate);
        host.Press(PhysicalKey.Enter); // Candidate choice, deliberately NOT query submission.
        Assert.Single(search.Tokens);
        Assert.Empty(submissions);
        Assert.True(search.Editor.IsFocused);
        host.Press(PhysicalKey.Enter); // No active choice: explicit search intent.
        Assert.Equal(MaterialQuerySubmissionReason.Keyboard, Assert.Single(submissions).Reason);
        Assert.Equal(mode == MaterialSearchMode.OutlinedAutocomplete ? MaterialTextFieldVariant.Outlined : MaterialTextFieldVariant.Filled, search.Editor.Variant);
        search.Open();
        host.Press(PhysicalKey.Escape);
        Assert.False(search.IsOpen);
        Assert.Single(search.Tokens);
    }

    [AvaloniaFact]
    public void Editing_accepting_selecting_and_removing_tokens_does_not_submit_until_explicit_intent()
    {
        var atlas = new MaterialSearchToken("atlas", "图集 Atlas");
        var search = new MaterialSearch { Candidates = new[] { atlas } };
        using var host = new SearchScenarioHost(search);
        MaterialQuerySubmittedEventArgs? submitted = null;
        search.QuerySubmitted += (_, args) => submitted = args;
        search.Editor!.Focus();
        host.Window.KeyTextInput("中文 draft");
        Assert.Equal("中文 draft", search.Text);
        search.SelectedCandidate = atlas;
        Assert.True(search.AcceptCandidate());
        Assert.Equal(new[] { atlas }, search.Tokens);
        Assert.Null(submitted);
        Assert.True(search.AddToken(new MaterialSearchToken("open", "Open access / 开放")));
        search.SelectedToken = atlas;
        Assert.True(search.RemoveToken(atlas));
        Assert.Null(search.SelectedToken);
        search.Editor.Focus();
        host.Window.KeyTextInput(" final");
        Assert.True(search.SubmitQuery());
        Assert.Equal(" final", submitted!.Text);
        Assert.Equal("Open access / 开放", Assert.Single(submitted.Tokens).Label);
        search.Tokens.Clear();
        Assert.Single(submitted.Tokens); // Submission is an immutable snapshot, not a live collection.
    }

    [AvaloniaFact]
    public void Input_chip_has_distinct_named_remove_action_without_toggling_or_invoking_primary_action()
    {
        var chip = new MaterialChip { ChipVariant = MaterialChipVariant.Input, Content = "长标签 bilingual Atlas", RemoveButtonLabel = "Remove Atlas" };
        using var host = new SearchScenarioHost(chip);
        var removed = 0;
        var activated = 0;
        chip.RemovalRequested += (_, _) => removed++;
        chip.Click += (_, _) => activated++;
        var remove = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "Remove Atlas");
        Assert.True(remove.Bounds.Width >= 48 && remove.Bounds.Height >= 48);
        host.Click(remove);
        Assert.Equal(1, removed);
        Assert.Equal(0, activated);
        Assert.False(chip.IsChecked);
        chip.IsEnabled = false;
        host.Click(remove);
        Assert.Equal(1, removed);
    }

    [AvaloniaFact]
    public void Filter_chip_keyboard_activation_commits_selection_before_host_action()
    {
        var chip = new MaterialChip { ChipVariant = MaterialChipVariant.Filter, Content = "中文 Atlas" };
        using var host = new SearchScenarioHost(chip);
        bool? observed = null;
        chip.Click += (_, _) => observed = chip.IsChecked;
        chip.Focus();
        host.Press(PhysicalKey.Space);
        Assert.True(observed);
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(ControlAutomationPeer.CreatePeerForElement(chip));
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        toggle.Toggle();
        Assert.False(chip.IsChecked);
    }
}

internal sealed class SearchScenarioHost : IDisposable
{
    public MaterialTheme Theme { get; } = new() { Motion = new MaterialMotion { ReduceMotion = true } };
    public Window Window { get; }
    public SearchScenarioHost(Control content, double width = 400, double height = 600)
    {
        Application.Current!.Styles.Add(Theme);
        Window = new Window { Width = width, Height = height, RequestedThemeVariant = ThemeVariant.Light,
            Content = new StackPanel { Margin = new Thickness(12), Children = { content } } };
        Window.Show();
        Capture();
    }
    public void Press(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPressQwerty(key, modifiers);
        Window.KeyReleaseQwerty(key, modifiers);
    }
    public void Click(Control control)
    {
        control.BringIntoView();
        Capture();
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
        Window.MouseDown(point, MouseButton.Left);
        Window.MouseUp(point, MouseButton.Left);
        Capture();
    }
    public void Capture() { using var bitmap = Window.CaptureRenderedFrame(); }
    public void Dispose() { Window.Close(); Application.Current!.Styles.Remove(Theme); }
}
