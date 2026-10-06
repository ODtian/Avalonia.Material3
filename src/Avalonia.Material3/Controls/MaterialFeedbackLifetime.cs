using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// Owns one generation only. Covered timeout/cancellation requests wait for top ownership;
// rejected closes never complete a task or execute an action behind a newer presentation.
internal sealed class MaterialFeedbackLifetime : IDisposable
{
    private readonly MaterialOverlayHost _host;
    private readonly MaterialOverlaySession _session;
    private readonly Control? _anchor;
    private readonly bool _allowDisabledAnchor;
    private readonly ITimer? _timer;
    private CancellationTokenRegistration _registration;
    private readonly CancellationToken _cancellationToken;
    private bool _disposed;
    private bool _ending;
    public bool IsEnding => _ending || _cancellationToken.IsCancellationRequested;
    public event EventHandler? StateChanged;
    public MaterialFeedbackLifetime(MaterialOverlayHost host, MaterialOverlaySession session, TimeSpan? duration,
        TimeProvider timeProvider, CancellationToken cancellationToken, Control? anchor = null, bool allowDisabledAnchor = false)
    {
        _host = host; _session = session; _anchor = anchor;
        _cancellationToken = cancellationToken;
        _allowDisabledAnchor = allowDisabledAnchor;
        host.PropertyChanged += HostChanged;
        if (anchor is not null) host.LayoutUpdated += CheckAnchor;
        session.Completed += SessionCompleted;
        if (duration is { } delay && delay != Timeout.InfiniteTimeSpan)
            _timer = timeProvider.CreateTimer(_ => Dispatcher.UIThread.Post(RequestDismiss), null, delay, Timeout.InfiniteTimeSpan);
        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(RequestDismiss));
    }
    private void SessionCompleted(object? sender, EventArgs e) => Dispose();
    private void HostChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialOverlayHost.OpenCountProperty)
            Dispatcher.UIThread.Post(() => { if (!_disposed) { StateChanged?.Invoke(this, EventArgs.Empty); if (IsEnding) TryDismiss(); } });
    }
    private void CheckAnchor(object? sender, EventArgs e)
    {
        if (_anchor is { } anchor && (!anchor.IsEffectivelyVisible || !_allowDisabledAnchor &&
            anchor.GetVisualAncestors().Prepend(anchor).TakeWhile(visual => visual != _host).OfType<InputElement>()
                .Any(element => element is not MaterialOverlayContentPresenter && element is not MaterialOverlayLayer && !element.IsEnabled))) RequestDismiss();
    }
    public void RequestDismiss()
    {
        if (_disposed) return;
        _ending = true; StateChanged?.Invoke(this, EventArgs.Empty); TryDismiss();
    }
    private void TryDismiss()
    {
        if (!_disposed && IsEnding && _session.IsTop) _session.Dismiss();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose(); _registration.Dispose();
        _session.Completed -= SessionCompleted;
        _host.PropertyChanged -= HostChanged; _host.LayoutUpdated -= CheckAnchor;
    }
}
