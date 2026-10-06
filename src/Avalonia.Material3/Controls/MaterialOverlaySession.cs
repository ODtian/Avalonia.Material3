using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

public enum MaterialOverlayCloseReason { Confirmed, Cancelled, Escape, Back, LightDismiss, HostDetached, AnchorDetached }

/// <summary>A host-owned payload and the way the overlay finished. Cancellation never supplies a payload.</summary>
public sealed record MaterialOverlayResult(MaterialOverlayCloseReason Reason, object? Value = null);

/// <summary>One presentation in a host's LIFO stack. Mutate only on the UI thread.</summary>
public sealed class MaterialOverlaySession
{
    private readonly MaterialOverlayHost _host;
    private readonly TaskCompletionSource<MaterialOverlayResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal MaterialOverlaySession(MaterialOverlayHost host, Control content, MaterialOverlayOptions options, Control? returnFocus, MaterialOverlayLayer layer)
    { _host = host; Content = content; Options = options; ReturnFocus = returnFocus; Layer = layer; }
    public MaterialOverlayOptions Options { get; }
    public event EventHandler<MaterialOverlayClosingEventArgs>? Closing;
    private bool _requestingClose;
    internal bool CanFinish(MaterialOverlayResult result)
    {
        if (_requestingClose) return false;
        _requestingClose = true;
        try
        {
            var args = new MaterialOverlayClosingEventArgs(result);
            Closing?.Invoke(this, args);
            return !args.Cancel;
        }
        finally { _requestingClose = false; }
    }
    public Control Content { get; }
    public Task<MaterialOverlayResult> Completion => _completion.Task;
    public bool IsOpen => !_completion.Task.IsCompleted;
    public bool Close(object? value = null) => _host.Finish(this, new(MaterialOverlayCloseReason.Confirmed, value));
    public bool Dismiss(MaterialOverlayCloseReason reason = MaterialOverlayCloseReason.Cancelled)
    {
        if (reason is MaterialOverlayCloseReason.Confirmed or MaterialOverlayCloseReason.HostDetached or MaterialOverlayCloseReason.AnchorDetached || !Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));
        return _host.Finish(this, new(reason));
    }
    internal EventHandler<VisualTreeAttachmentEventArgs>? AnchorDetachedHandler { get; set; }
    internal Control? ReturnFocus { get; }
    internal MaterialOverlayLayer Layer { get; }
    internal event EventHandler? Completed;
    /// <summary>Raised on the UI thread after removal and focus restoration. Completion is guaranteed even if a callback throws.</summary>
    public event EventHandler<MaterialOverlayResult>? Closed;
    internal void Complete(MaterialOverlayResult result)
    {
        try { Completed?.Invoke(this, EventArgs.Empty); }
        finally { _completion.TrySetResult(result); }
        Closed?.Invoke(this, result);
    }
}
