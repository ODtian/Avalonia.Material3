using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;

namespace Avalonia.Material3.Controls;

/// <summary>A host-defined generic candidate or query token. Keys use ordinal identity.</summary>
public sealed record MaterialSearchToken(string Key, string Label);
public enum MaterialSearchMode { Bar, View, FilledAutocomplete, OutlinedAutocomplete }
public enum MaterialSearchViewPresentation { Docked, FullScreen }
public enum MaterialCandidateAction { AddToken, ReplaceText }
public enum MaterialQuerySubmissionReason { Programmatic, SubmitButton, Keyboard }

/// <summary>An immutable snapshot of an explicit search intent.</summary>
public sealed class MaterialQuerySubmittedEventArgs : RoutedEventArgs
{
    public string Text { get; }
    public IReadOnlyList<MaterialSearchToken> Tokens { get; }
    public MaterialSearchToken? SelectedToken { get; }
    public MaterialQuerySubmissionReason Reason { get; }
    internal MaterialQuerySubmittedEventArgs(MaterialSearch search, MaterialQuerySubmissionReason reason)
        : base(MaterialSearch.QuerySubmittedEvent)
    {
        Text = search.Text ?? "";
        Tokens = Array.AsReadOnly(search.Tokens.ToArray());
        SelectedToken = search.SelectedToken;
        Reason = reason;
    }
}

/// <summary>Native text editing, host candidates, editable tokens and explicit query submission.</summary>
[TemplatePart("PART_Editor", typeof(MaterialTextField))]
[TemplatePart("PART_Candidates", typeof(ListBox))]
[TemplatePart("PART_Tokens", typeof(ItemsControl))]
[TemplatePart("PART_SubmitButton", typeof(Button))]
[TemplatePart("PART_AddButton", typeof(Button))]
[TemplatePart("PART_CloseButton", typeof(Button))]
[PseudoClasses(":bar", ":view", ":filledautocomplete", ":outlinedautocomplete", ":open", ":fullscreen", ":invalid")]
public class MaterialSearch : TemplatedControl
{
    public static readonly StyledProperty<object?> LeadingContentProperty = AvaloniaProperty.Register<MaterialSearch, object?>(nameof(LeadingContent));
    public static readonly StyledProperty<object?> TrailingContentProperty = AvaloniaProperty.Register<MaterialSearch, object?>(nameof(TrailingContent));
    public static readonly StyledProperty<IDataTemplate?> CandidateTemplateProperty = AvaloniaProperty.Register<MaterialSearch, IDataTemplate?>(nameof(CandidateTemplate));
    public static readonly StyledProperty<string> SubmitButtonLabelProperty = AvaloniaProperty.Register<MaterialSearch, string>(nameof(SubmitButtonLabel), "Submit query", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> AddButtonLabelProperty = AvaloniaProperty.Register<MaterialSearch, string>(nameof(AddButtonLabel), "Add token", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> ClearButtonLabelProperty = AvaloniaProperty.Register<MaterialSearch, string>(nameof(ClearButtonLabel), "Clear query", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<string> RemoveTokenLabelPrefixProperty = AvaloniaProperty.Register<MaterialSearch, string>(nameof(RemoveTokenLabelPrefix), "Remove", validate: value => !string.IsNullOrWhiteSpace(value));
    public object? LeadingContent { get => GetValue(LeadingContentProperty); set => SetValue(LeadingContentProperty, value); }
    public object? TrailingContent { get => GetValue(TrailingContentProperty); set => SetValue(TrailingContentProperty, value); }
    public IDataTemplate? CandidateTemplate { get => GetValue(CandidateTemplateProperty); set => SetValue(CandidateTemplateProperty, value); }
    public string SubmitButtonLabel { get => GetValue(SubmitButtonLabelProperty); set => SetValue(SubmitButtonLabelProperty, value); }
    public string AddButtonLabel { get => GetValue(AddButtonLabelProperty); set => SetValue(AddButtonLabelProperty, value); }
    public string ClearButtonLabel { get => GetValue(ClearButtonLabelProperty); set => SetValue(ClearButtonLabelProperty, value); }
    public string RemoveTokenLabelPrefix { get => GetValue(RemoveTokenLabelPrefixProperty); set => SetValue(RemoveTokenLabelPrefixProperty, value); }
    private readonly IDataTemplate _defaultCandidateTemplate = new FuncDataTemplate<MaterialSearchToken>((token, _) => new TextBlock { Text = token?.Label, TextWrapping = TextWrapping.Wrap });
    public static readonly StyledProperty<MaterialSearchViewPresentation> ViewPresentationProperty = AvaloniaProperty.Register<MaterialSearch, MaterialSearchViewPresentation>(nameof(ViewPresentation), validate: Enum.IsDefined);
    public static readonly StyledProperty<string> CloseButtonLabelProperty = AvaloniaProperty.Register<MaterialSearch, string>(nameof(CloseButtonLabel), "Close suggestions", validate: value => !string.IsNullOrWhiteSpace(value));
    public static readonly StyledProperty<double> CandidateMaxHeightProperty = AvaloniaProperty.Register<MaterialSearch, double>(nameof(CandidateMaxHeight), 240, validate: value => double.IsFinite(value) && value >= 48);
    public static readonly DirectProperty<MaterialSearch, double> HeaderHeightProperty = AvaloniaProperty.RegisterDirect<MaterialSearch, double>(nameof(HeaderHeight), search => search.HeaderHeight);
    public MaterialSearchViewPresentation ViewPresentation { get => GetValue(ViewPresentationProperty); set => SetValue(ViewPresentationProperty, value); }
    public string CloseButtonLabel { get => GetValue(CloseButtonLabelProperty); set => SetValue(CloseButtonLabelProperty, value); }
    public double CandidateMaxHeight { get => GetValue(CandidateMaxHeightProperty); set => SetValue(CandidateMaxHeightProperty, value); }
    public double HeaderHeight => _headerHeight;
    private double _headerHeight = 56;
    public static readonly StyledProperty<MaterialCandidateAction> CandidateActionProperty = AvaloniaProperty.Register<MaterialSearch, MaterialCandidateAction>(nameof(CandidateAction), validate: Enum.IsDefined);
    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<MaterialSearch, bool>(nameof(IsReadOnly));
    public MaterialCandidateAction CandidateAction { get => GetValue(CandidateActionProperty); set => SetValue(CandidateActionProperty, value); }
    public bool IsReadOnly { get => GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
    public static readonly StyledProperty<bool> IsErrorProperty = AvaloniaProperty.Register<MaterialSearch, bool>(nameof(IsError));
    public static readonly StyledProperty<string?> ErrorTextProperty = AvaloniaProperty.Register<MaterialSearch, string?>(nameof(ErrorText));
    public static readonly StyledProperty<string?> SupportingTextProperty = AvaloniaProperty.Register<MaterialSearch, string?>(nameof(SupportingText));
    public static readonly DirectProperty<MaterialSearch, bool> HasErrorProperty = AvaloniaProperty.RegisterDirect<MaterialSearch, bool>(nameof(HasError), search => search.HasError);
    public bool IsError { get => GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public string? SupportingText { get => GetValue(SupportingTextProperty); set => SetValue(SupportingTextProperty, value); }
    public bool HasError => _hasError;
    private bool _hasError;
    public static readonly DirectProperty<MaterialSearch, string?> EffectiveErrorTextProperty = AvaloniaProperty.RegisterDirect<MaterialSearch, string?>(nameof(EffectiveErrorText), search => search.EffectiveErrorText);
    public string? EffectiveErrorText => _effectiveErrorText;
    private string? _effectiveErrorText;
    public static readonly StyledProperty<MaterialSearchMode> ModeProperty = AvaloniaProperty.Register<MaterialSearch, MaterialSearchMode>(nameof(Mode), validate: Enum.IsDefined);
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<MaterialSearch, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<MaterialSearch, string>(nameof(Label), "Search", validate: value => !string.IsNullOrWhiteSpace(value));
    public MaterialSearchMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<MaterialSearch, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);
    public static readonly StyledProperty<IEnumerable<MaterialSearchToken>?> CandidatesProperty = AvaloniaProperty.Register<MaterialSearch, IEnumerable<MaterialSearchToken>?>(nameof(Candidates));
    public static readonly StyledProperty<IList<MaterialSearchToken>> TokensProperty = AvaloniaProperty.Register<MaterialSearch, IList<MaterialSearchToken>>(nameof(Tokens), Array.Empty<MaterialSearchToken>(), validate: value => value is not null);
    public static readonly StyledProperty<MaterialSearchToken?> SelectedCandidateProperty = AvaloniaProperty.Register<MaterialSearch, MaterialSearchToken?>(nameof(SelectedCandidate), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<MaterialSearchToken?> SelectedTokenProperty = AvaloniaProperty.Register<MaterialSearch, MaterialSearchToken?>(nameof(SelectedToken), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<MaterialSearch, MaterialTextField?> EditorProperty = AvaloniaProperty.RegisterDirect<MaterialSearch, MaterialTextField?>(nameof(Editor), search => search.Editor);
    public static readonly RoutedEvent<MaterialQuerySubmittedEventArgs> QuerySubmittedEvent = RoutedEvent.Register<MaterialSearch, MaterialQuerySubmittedEventArgs>(nameof(QuerySubmitted), RoutingStrategies.Bubble);
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public IEnumerable<MaterialSearchToken>? Candidates { get => GetValue(CandidatesProperty); set => SetValue(CandidatesProperty, value); }
    public IList<MaterialSearchToken> Tokens { get => GetValue(TokensProperty); set => SetValue(TokensProperty, value); }
    public MaterialSearchToken? SelectedCandidate { get => GetValue(SelectedCandidateProperty); set => SetValue(SelectedCandidateProperty, value); }
    public MaterialSearchToken? SelectedToken { get => GetValue(SelectedTokenProperty); set => SetValue(SelectedTokenProperty, value); }
    public MaterialTextField? Editor => _editor;
    public event EventHandler<MaterialQuerySubmittedEventArgs>? QuerySubmitted { add => AddHandler(QuerySubmittedEvent, value); remove => RemoveHandler(QuerySubmittedEvent, value); }
    private MaterialTextField? _editor;
    private Button? _submit;
    private Button? _add;
    private Button? _close;
    private ItemsControl? _tokens;
    private ListBox? _candidates;
    private bool _accepting;
    private INotifyCollectionChanged? _observedTokens;
    private bool _attached;
    private readonly MaterialMotionValue _expansion;
    private readonly MaterialMotionSettings _motion;
    internal double ExpansionProgress => _expansion.Value;
    internal bool IsFullscreenPresentation => Mode is MaterialSearchMode.Bar or MaterialSearchMode.View
        && ViewPresentation == MaterialSearchViewPresentation.FullScreen;
    internal event Action? ExpansionChanged;

    public MaterialSearch()
    {
        Tokens = new AvaloniaList<MaterialSearchToken>();
        _expansion = new(this, IsOpen ? 1 : 0, PaintExpansion);
        _motion = new(this, () => UpdateExpansion(true));
        UpdateStates();
        AddHandler(KeyDownEvent, SearchKeyDown, RoutingStrategies.Tunnel);
    }
    public void Open() { if (IsEffectivelyEnabled) { SetCurrentValue(IsOpenProperty, true); _editor?.Focus(); } }
    public void Close() { SetCurrentValue(IsOpenProperty, false); SetCurrentValue(SelectedCandidateProperty, null); _editor?.Focus(); }
    private void UpdateStates()
    {
        foreach (var mode in Enum.GetValues<MaterialSearchMode>())
            PseudoClasses.Set(":" + mode.ToString().ToLowerInvariant(), Mode == mode);
        PseudoClasses.Set(":open", IsOpen);
        PseudoClasses.Set(":fullpresentation", IsFullscreenPresentation);
        var fullScreen = IsOpen && (Mode is MaterialSearchMode.Bar or MaterialSearchMode.View) && ViewPresentation == MaterialSearchViewPresentation.FullScreen;
        PseudoClasses.Set(":fullscreen", fullScreen);
        UpdateExpansion(_attached);
    }
    private void PaintExpansion(double progress)
    {
        SetAndRaise(HeaderHeightProperty, ref _headerHeight, IsFullscreenPresentation ? 56 + 16 * Math.Clamp(progress, 0, 1) : 56);
        ExpansionChanged?.Invoke();
    }
    private void UpdateExpansion(bool animate)
    {
        if (_expansion is null || _motion is null) return;
        var target = IsOpen ? 1 : 0;
        if (!animate || !_attached || _motion.FastEffects.IsInstant || Mode is not (MaterialSearchMode.Bar or MaterialSearchMode.View))
        { _expansion.Snap(target); return; }
        var interrupted = _expansion.IsRunning && _expansion.Value is > 0 and < 1;
        _expansion.Tween(target, TimeSpan.FromMilliseconds(interrupted || !IsOpen ? 350 : 600),
            interrupted || !IsOpen ? new Avalonia.Animation.Easings.SplineEasing(0, 1, 0, 1) : new Avalonia.Animation.Easings.SplineEasing(.05, .7, .1, 1),
            interrupted ? TimeSpan.Zero : TimeSpan.FromMilliseconds(100));
    }
    private void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsEffectivelyEnabled || e.KeyModifiers != KeyModifiers.None) return;
        // The native presenter owns composition. Its Enter/arrows/Escape belong to the IME, not search intent.
        if (_editor?.IsFocused == true && _editor.GetVisualDescendants().OfType<TextPresenter>().Any(p => !string.IsNullOrEmpty(p.PreeditText))) return;
        if (_editor?.IsFocused == true && e.Key is Key.Down or Key.Up)
        {
            Open();
            if (_candidates?.ItemCount > 0)
            {
                var next = _candidates.SelectedIndex < 0 ? (e.Key == Key.Down ? 0 : _candidates.ItemCount - 1) : _candidates.SelectedIndex + (e.Key == Key.Down ? 1 : -1);
                _candidates.SelectedIndex = Math.Clamp(next, 0, _candidates.ItemCount - 1);
                _candidates.ScrollIntoView(_candidates.SelectedItem!);
            }
            e.Handled = true;
        }
        else if ((_editor?.IsFocused == true || _candidates?.IsKeyboardFocusWithin == true) && e.Key == Key.Enter)
        {
            if (IsOpen && SelectedCandidate is not null) AcceptCandidate();
            else SubmitQuery(MaterialQuerySubmissionReason.Keyboard);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && IsOpen) { Close(); e.Handled = true; }
    }
    private void CandidateReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left || !IsOpen || SelectedCandidate is null || _candidates is null) return;
        var item = _candidates.GetVisualDescendants().OfType<ListBoxItem>().FirstOrDefault(row => Equals(row.Content, SelectedCandidate) && new Rect(row.Bounds.Size).Contains(e.GetPosition(row)));
        if (item is not null) { AcceptCandidate(); e.Handled = true; }
    }
    protected override Type StyleKeyOverride => typeof(MaterialSearch);
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialSearchAutomationPeer(this);

    /// <summary>Adds a unique nonempty host token. Editing never submits a query.</summary>
    public bool AddToken(MaterialSearchToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!IsEffectivelyEnabled || IsReadOnly || Tokens.IsReadOnly || string.IsNullOrWhiteSpace(token.Key) || string.IsNullOrWhiteSpace(token.Label) || Tokens.Any(t => t.Key == token.Key)) return false;
        Tokens.Add(token);
        RefreshTokens();
        return true;
    }
    public bool RemoveToken(MaterialSearchToken token)
    {
        if (!IsEffectivelyEnabled || IsReadOnly || Tokens.IsReadOnly || !Tokens.Remove(token)) return false;
        NormalizeSelectedToken();
        RefreshTokens();
        _editor?.Focus();
        return true;
    }
    public bool AcceptCandidate()
    {
        if (!IsEffectivelyEnabled || IsReadOnly || SelectedCandidate is not { } candidate || Candidates?.Contains(candidate) != true || string.IsNullOrWhiteSpace(candidate.Key) || string.IsNullOrWhiteSpace(candidate.Label)) return false;
        if (CandidateAction == MaterialCandidateAction.AddToken && !AddToken(candidate)) return false;
        _accepting = true;
        try
        {
            _editor?.Focus();
            SetCurrentValue(TextProperty, CandidateAction == MaterialCandidateAction.ReplaceText ? candidate.Label : "");
            if (_editor is not null) _editor.CaretIndex = _editor.Text?.Length ?? 0;
            Close();
        }
        finally { _accepting = false; }
        return true;
    }
    public bool SubmitQuery(MaterialQuerySubmissionReason reason = MaterialQuerySubmissionReason.Programmatic)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (!IsEffectivelyEnabled || HasError) return false;
        RaiseEvent(new MaterialQuerySubmittedEventArgs(this, reason));
        return true;
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_submit is not null) _submit.Click -= SubmitClicked;
        if (_add is not null) _add.Click -= AddClicked;
        if (_close is not null) _close.Click -= CloseClicked;
        if (_candidates is not null) _candidates.PointerReleased -= CandidateReleased;
        if (_editor is not null) _editor.PropertyChanged -= EditorChanged;
        base.OnApplyTemplate(e);
        SetAndRaise(EditorProperty, ref _editor, e.NameScope.Find<MaterialTextField>("PART_Editor"));
        if (_editor is not null) _editor.PropertyChanged += EditorChanged;
        UpdateError();
        _submit = e.NameScope.Find<Button>("PART_SubmitButton");
        _add = e.NameScope.Find<Button>("PART_AddButton");
        _close = e.NameScope.Find<Button>("PART_CloseButton");
        if (_close is not null) _close.Click += CloseClicked;
        _tokens = e.NameScope.Find<ItemsControl>("PART_Tokens");
        _candidates = e.NameScope.Find<ListBox>("PART_Candidates");
        if (_candidates is not null)
        {
            _candidates.PointerReleased += CandidateReleased;
            _candidates.ItemTemplate = CandidateTemplate ?? _defaultCandidateTemplate;
        }
        if (_submit is not null) _submit.Click += SubmitClicked;
        if (_add is not null) _add.Click += AddClicked;
        RefreshTokens();
    }
    private void EditorChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialTextField.HasErrorProperty) UpdateError();
        if (e.Property == MaterialTextField.TextProperty && _editor?.IsFocused == true && !_accepting && !IsReadOnly)
            SetCurrentValue(IsOpenProperty, true);
    }
    private void UpdateError()
    {
        var bindingError = DataValidationErrors.GetErrors(this)?.Select(error => error is Exception exception ? exception.Message : error?.ToString()).FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
        SetAndRaise(EffectiveErrorTextProperty, ref _effectiveErrorText, !string.IsNullOrEmpty(ErrorText) ? ErrorText : bindingError);
        SetAndRaise(HasErrorProperty, ref _hasError, IsError || !string.IsNullOrEmpty(EffectiveErrorText) || DataValidationErrors.GetHasErrors(this) || _editor?.HasError == true);
        PseudoClasses.Set(":invalid", HasError);
    }
    private void CloseClicked(object? sender, RoutedEventArgs e) { e.Handled = true; Close(); }
    private void SubmitClicked(object? sender, RoutedEventArgs e) { e.Handled = true; SubmitQuery(MaterialQuerySubmissionReason.SubmitButton); }
    private void AddClicked(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!string.IsNullOrWhiteSpace(Text) && AddToken(new MaterialSearchToken(Text, Text)))
        {
            _editor?.Focus();
            SetCurrentValue(TextProperty, "");
        }
    }
    private void NormalizeSelectedToken()
    {
        if (SelectedToken is not null && !Tokens.Contains(SelectedToken)) SetCurrentValue(SelectedTokenProperty, null);
    }
    private void TokensChanged(object? sender, NotifyCollectionChangedEventArgs e) { NormalizeSelectedToken(); RefreshTokens(); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        UpdateExpansion(false);
        if (_observedTokens is not null) _observedTokens.CollectionChanged += TokensChanged;
        NormalizeSelectedToken();
        RefreshTokens();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observedTokens is not null) _observedTokens.CollectionChanged -= TokensChanged;
        _attached = false;
        base.OnDetachedFromVisualTree(e);
    }
    private void RefreshTokens()
    {
        if (_tokens is null) return;
        _tokens.ItemsSource = Tokens.ToArray();
        _tokens.ItemTemplate ??= new FuncDataTemplate<MaterialSearchToken>((token, _) =>
        {
            if (token is null) return null;
            var chip = new MaterialChip { ChipVariant = MaterialChipVariant.Input, Content = token.Label,
                DataContext = token, RemoveButtonLabel = RemoveTokenLabelPrefix + " " + token.Label, IsChecked = Equals(SelectedToken, token), IsRemovable = !IsReadOnly && !Tokens.IsReadOnly, HorizontalAlignment = Layout.HorizontalAlignment.Stretch };
            chip.Click += (_, _) => SetCurrentValue(SelectedTokenProperty, chip.IsChecked ? token : null);
            chip.RemovalRequested += (_, args) => { args.Handled = true; RemoveToken(token); };
            return chip;
        });
    }
    private void UpdateTokenSelection()
    {
        if (_tokens is null) return;
        foreach (var chip in _tokens.GetVisualDescendants().OfType<MaterialChip>())
        {
            if (chip.DataContext is MaterialSearchToken token)
            {
                chip.SetCurrentValue(MaterialChip.IsCheckedProperty, Equals(SelectedToken, token));
                chip.IsRemovable = !IsReadOnly && !Tokens.IsReadOnly;
                chip.RemoveButtonLabel = RemoveTokenLabelPrefix + " " + token.Label;
            }
        }
    }
    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        if (property == TextProperty) DataValidationErrors.SetError(this, error);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TokensProperty)
        {
            if (_observedTokens is not null) _observedTokens.CollectionChanged -= TokensChanged;
            _observedTokens = Tokens as INotifyCollectionChanged;
            if (_attached && _observedTokens is not null) _observedTokens.CollectionChanged += TokensChanged;
            NormalizeSelectedToken();
            RefreshTokens();
        }
        if (change.Property == SelectedTokenProperty) NormalizeSelectedToken();
        if (change.Property == SelectedTokenProperty || change.Property == IsReadOnlyProperty || change.Property == RemoveTokenLabelPrefixProperty) UpdateTokenSelection();
        if (change.Property == CandidateTemplateProperty && _candidates is not null) _candidates.ItemTemplate = CandidateTemplate ?? _defaultCandidateTemplate;
        if (change.Property == IsKeyboardFocusWithinProperty && !IsKeyboardFocusWithin)
        {
            // Dismiss without stealing focus back from another host control.
            SetCurrentValue(IsOpenProperty, false);
            SetCurrentValue(SelectedCandidateProperty, null);
        }
        if (change.Property == ModeProperty && Mode == MaterialSearchMode.View) SetCurrentValue(IsOpenProperty, true);
        if (change.Property == ModeProperty || change.Property == IsOpenProperty || change.Property == ViewPresentationProperty) UpdateStates();
        if (change.Property == IsErrorProperty || change.Property == ErrorTextProperty || change.Property == DataValidationErrors.ErrorsProperty || change.Property == DataValidationErrors.HasErrorsProperty) UpdateError();
    }
}
