using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Themes;
using Avalonia.Material3.Tokens;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Gallery.Pages;

/// <summary>Package-only generic asynchronous host. Task ownership and pause policy live here, never in the library.</summary>
public sealed class ProgressFeedbackPage : StackPanel
{
    private readonly MaterialTheme _theme;
    private readonly Func<IProgress<double>, CancellationToken, Task> _operation;
    private CancellationTokenSource _lifetime = new();
    private bool _busy;
    private bool _failNext;
    private TimeSpan _time;
    public MaterialButton StartButton { get; } = new() { Content = "Start import" };
    public MaterialButton PauseButton { get; } = new() { Content = "Pause / resume", Variant = MaterialButtonVariant.Tonal, IsEnabled = false };
    public MaterialButton FailureButton { get; } = new() { Content = "Next task fails", IsToggle = true, Variant = MaterialButtonVariant.Outlined };
    public MaterialButton MotionButton { get; } = new() { Content = "Reduce motion", IsToggle = true, Variant = MaterialButtonVariant.Outlined };
    public MaterialButton StepButton { get; } = new() { Content = "Controlled time +250ms", Variant = MaterialButtonVariant.Outlined };
    public MaterialButton ClockButton { get; } = new() { Content = "Use live / controlled time", Variant = MaterialButtonVariant.Outlined };
    public MaterialButton ThemeButton { get; } = new() { Content = "Light / dark", Variant = MaterialButtonVariant.Text };
    public MaterialButton FontButton { get; } = new() { Content = "100 / 200% text", Variant = MaterialButtonVariant.Text };
    public TextBlock Result { get; } = new() { Text = "Idle — start a host-owned task", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    public IReadOnlyList<MaterialProgressIndicator> Indicators { get; }
    public Task CurrentOperation { get; private set; } = Task.CompletedTask;

    public ProgressFeedbackPage(MaterialTheme theme, Func<IProgress<double>, CancellationToken, Task>? operation = null)
    {
        _theme = theme;
        _operation = operation ?? DemoOperationAsync;
        Spacing = 12;
        Margin = new Thickness(24);
        Children.Add(Label("Progress & Expressive loading — 通用异步任务", "m3-title-large"));
        Children.Add(Label("The host reports values and outcomes. Pause freezes feedback; the built-in sample also pauses its work. Reduced motion keeps a static, accessible busy cue."));
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var action in new[] { StartButton, PauseButton, FailureButton, MotionButton, ClockButton, StepButton, ThemeButton, FontButton })
        {
            action.Margin = new Thickness(0, 0, 8, 8);
            AutomationProperties.SetName(action, action.Content?.ToString());
            actions.Children.Add(action);
        }
        foreach (var (control, id) in new (Control, string)[] { (StartButton, "StartButton"), (PauseButton, "PauseButton"), (FailureButton, "FailureButton"),
            (MotionButton, "MotionButton"), (ClockButton, "ClockButton"), (StepButton, "StepButton"), (ThemeButton, "ThemeButton"), (FontButton, "FontButton") })
            AutomationProperties.SetAutomationId(control, id);
        Children.Add(actions);
        Result.Classes.Add("m3-body-large");
        AutomationProperties.SetAutomationId(Result, "TaskResult");
        AutomationProperties.SetLiveSetting(Result, AutomationLiveSetting.Polite);
        Children.Add(Result);
        var indicators = new List<MaterialProgressIndicator>();
        void Add(string name, MaterialProgressIndicator indicator)
        {
            indicator.Status = MaterialProgressStatus.Idle;
            indicator.HorizontalAlignment = HorizontalAlignment.Left;
            AutomationProperties.SetName(indicator, name + " import progress");
            AutomationProperties.SetAutomationId(indicator, "Progress" + indicators.Count);
            Children.Add(Label(name, "m3-label-large"));
            Children.Add(indicator);
            indicators.Add(indicator);
        }
        foreach (var expressive in new[] { false, true })
        foreach (var unknown in new[] { false, true })
        {
            var prefix = (expressive ? "Expressive wave" : "Standard") + (unknown ? " — indeterminate" : " — determinate");
            Add(prefix + " linear", new MaterialLinearProgressIndicator { IsExpressive = expressive, IsIndeterminate = unknown });
            Add(prefix + " circular", new MaterialCircularProgressIndicator { IsExpressive = expressive, IsIndeterminate = unknown });
        }
        Add("Loading — seven-shape cycle", new MaterialLoadingIndicator());
        Add("Loading — progress-driven morph", new MaterialLoadingIndicator { IsIndeterminate = false });
        Add("Contained loading — cycle", new MaterialLoadingIndicator { IsContained = true });
        Add("Contained loading — progress-driven morph", new MaterialLoadingIndicator { IsContained = true, IsIndeterminate = false });
        Indicators = indicators.AsReadOnly();
        StartButton.Click += (_, _) => CurrentOperation = RunOperationAsync();
        PauseButton.Click += (_, _) =>
        {
            if (!_busy) return;
            var pause = Indicators[0].Status != MaterialProgressStatus.Paused;
            SetStatus(pause ? MaterialProgressStatus.Paused : MaterialProgressStatus.Running);
            UpdateResult();
        };
        FailureButton.Click += (_, _) => _failNext = FailureButton.IsChecked;
        MotionButton.IsChecked = theme.Motion.ReduceMotion;
        MotionButton.Click += (_, _) => theme.Motion = theme.Motion with { ReduceMotion = MotionButton.IsChecked };
        StepButton.Click += (_, _) =>
        {
            _time += TimeSpan.FromMilliseconds(250);
            foreach (var indicator in Indicators) indicator.AnimationTime = _time;
        };
        ClockButton.Click += (_, _) =>
        {
            var controlled = Indicators[0].AnimationTime is null;
            _time = TimeSpan.Zero;
            foreach (var indicator in Indicators) indicator.AnimationTime = controlled ? _time : null;
        };
        ThemeButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
                window.RequestedThemeVariant = window.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        };
        FontButton.Click += (_, _) => theme.Typography = theme.Typography with { Scale = theme.Typography.Scale == 1 ? 2 : 1 };
        DetachedFromVisualTree += (_, _) =>
        {
            _lifetime.Cancel();
            _busy = false;
            StartButton.IsEnabled = FailureButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            SetStatus(MaterialProgressStatus.Idle);
        };
        AttachedToVisualTree += (_, _) =>
        {
            if (!_lifetime.IsCancellationRequested) return;
            _lifetime.Dispose();
            _lifetime = new CancellationTokenSource();
        };
    }

    public async Task RunOperationAsync()
    {
        if (_busy) return;
        var lifetime = _lifetime;
        _busy = true;
        StartButton.IsEnabled = FailureButton.IsEnabled = false;
        PauseButton.IsEnabled = true;
        foreach (var indicator in Indicators) { indicator.Value = 0; indicator.ResultMessage = null; }
        SetStatus(MaterialProgressStatus.Running);
        UpdateResult();
        try
        {
            await _operation(new HostProgress(value =>
            {
                if (lifetime.IsCancellationRequested || !ReferenceEquals(lifetime, _lifetime)) return;
                foreach (var indicator in Indicators) indicator.Value = value;
                UpdateResult();
            }), lifetime.Token);
            if (lifetime.IsCancellationRequested || !ReferenceEquals(lifetime, _lifetime)) return;
            foreach (var indicator in Indicators) indicator.ResultMessage = "12 documents imported";
            SetStatus(MaterialProgressStatus.Completed);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (lifetime.IsCancellationRequested || !ReferenceEquals(lifetime, _lifetime)) return;
            foreach (var indicator in Indicators) indicator.ResultMessage = error.Message;
            SetStatus(MaterialProgressStatus.Failed);
        }
        finally
        {
            if (ReferenceEquals(lifetime, _lifetime) && !lifetime.IsCancellationRequested)
            {
                _busy = false;
                StartButton.IsEnabled = FailureButton.IsEnabled = true;
                PauseButton.IsEnabled = false;
                UpdateResult();
            }
        }
    }
    private async Task DemoOperationAsync(IProgress<double> reporter, CancellationToken token)
    {
        for (var i = 1; i <= 12; i++)
        {
            await Task.Delay(250, token);
            while (Indicators[0].Status == MaterialProgressStatus.Paused) await Task.Delay(50, token);
            if (_failNext && i == 6) throw new InvalidOperationException("Connection lost — retry the import");
            reporter.Report(i / 12d);
        }
    }
    private void SetStatus(MaterialProgressStatus status)
    {
        foreach (var indicator in Indicators) indicator.Status = status;
    }
    private void UpdateResult() => Result.Text = Indicators[0].StatusDescription;
    private static TextBlock Label(string text, string role = "m3-body-medium")
    {
        var label = new TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        label.Classes.Add(role);
        return label;
    }
    private sealed class HostProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value)
        {
            if (Dispatcher.UIThread.CheckAccess()) report(value); else Dispatcher.UIThread.Post(() => report(value));
        }
    }
}
