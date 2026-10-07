using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

/// <summary>Private presentation seam. One public framework callback per root, no independent timers.</summary>
internal static class MaterialRenderFrames
{
    private static readonly long Epoch = Stopwatch.GetTimestamp();
    private static readonly ConditionalWeakTable<TopLevel, FrameHub> Hubs = new();
    internal static TimeSpan Now => Stopwatch.GetElapsedTime(Epoch);
    internal static MaterialFrameLease Bind(Control owner, Func<MaterialFrame, bool> advance, bool ignoreOwnerEnabled = false) => new(owner, advance, ignoreOwnerEnabled);
    internal static FrameHub Hub(TopLevel root) => Hubs.GetValue(root, static value => new(value));

    internal sealed class FrameHub
    {
        private readonly WeakReference<TopLevel> _root;
        private readonly Action<TimeSpan> _callback;
        private readonly List<MaterialFrameLease> _clients = [];
        private readonly List<MaterialFrameLease> _snapshot = [];
        private bool _queued;
        private TimeSpan? _offset;

        internal FrameHub(TopLevel root)
        {
            _root = new(root);
            // The framework's one-shot callback cannot be cancelled. It must not retain a closed root.
            var weak = new WeakReference<FrameHub>(this);
            _callback = time => { if (weak.TryGetTarget(out var hub)) hub.Pulse(time); };
        }
        internal void Add(MaterialFrameLease lease)
        {
            if (!_clients.Contains(lease)) _clients.Add(lease);
            Queue();
        }
        internal void Remove(MaterialFrameLease lease) => _clients.Remove(lease);
        private void Queue()
        {
            if (_queued || _clients.Count == 0 || !_root.TryGetTarget(out var root)) return;
            _queued = true;
            root.RequestAnimationFrame(_callback);
        }
        private void Pulse(TimeSpan timestamp)
        {
            _queued = false;
            // Retain the framework's shared timestamps/deltas, mapping its epoch once to property-event time.
            _offset ??= Now - timestamp;
            timestamp += _offset.Value;
            _snapshot.Clear();
            _snapshot.AddRange(_clients);
            try
            {
                for (var i = 0; i < _snapshot.Count; i++) _snapshot[i].Pulse(timestamp);
            }
            finally
            {
                _snapshot.Clear();
                Queue();
            }
        }
    }
}

internal readonly record struct MaterialFrame(TimeSpan Elapsed, TimeSpan Delta, bool Rewound);

/// <summary>
/// Owns active time and enrollment, not state/focus/input or curve selection. Restart resets a finite
/// transition; SetTime rebases clock domains. Hidden/disabled/detached time is excluded without polling.
/// </summary>
internal sealed class MaterialFrameLease : IDisposable
{
    private readonly Control _owner;
    private readonly Func<MaterialFrame, bool> _advance;
    private readonly bool _ignoreOwnerEnabled;
    private readonly List<Visual> _ancestors = [];
    private MaterialRenderFrames.FrameHub? _hub;
    private TimeSpan? _authored;
    private TimeSpan _lastSource, _elapsed, _published;
    private bool _attached, _running, _eligible, _disposed, _publishing;
    internal TimeSpan Elapsed => _elapsed;
    internal bool IsRunning => _running;

    internal MaterialFrameLease(Control owner, Func<MaterialFrame, bool> advance, bool ignoreOwnerEnabled)
    {
        _owner = owner;
        _advance = advance;
        _ignoreOwnerEnabled = ignoreOwnerEnabled;
        _lastSource = MaterialRenderFrames.Now;
        owner.AttachedToVisualTree += Attached;
        owner.DetachedFromVisualTree += Detached;
        owner.PropertyChanged += OwnerChanged;
        if (owner.IsAttachedToVisualTree()) Attach();
    }
    internal void SetRunning(bool running)
    {
        if (_disposed || _running == running) return;
        Accumulate(SourceNow, false);
        _running = running;
        Synchronize();
    }
    internal void Restart()
    {
        _elapsed = _published = TimeSpan.Zero;
        _lastSource = SourceNow;
    }
    internal void SetTime(TimeSpan? time)
    {
        if (_disposed || time == _authored) return;
        var old = _authored;
        if (old.HasValue && time.HasValue)
        {
            _authored = time;
            var rewind = time.Value < old.Value;
            Accumulate(time.Value, rewind);
            Publish(rewind);
        }
        else
        {
            // Switching live/manual preserves the presented active phase and establishes a new baseline.
            Accumulate(SourceNow, false);
            _authored = time;
            _lastSource = SourceNow;
        }
        Synchronize();
    }
    internal void Sample()
    {
        if (_disposed) return;
        Accumulate(SourceNow, false);
        Publish(false);
    }
    private TimeSpan SourceNow => _authored ?? MaterialRenderFrames.Now;
    private void Accumulate(TimeSpan source, bool rewind)
    {
        if (rewind) _elapsed = _published = TimeSpan.Zero;
        else if (_eligible && source > _lastSource) _elapsed += source - _lastSource;
        // A live callback timestamp can precede the immediately preceding property event slightly.
        _lastSource = rewind || _authored.HasValue || source > _lastSource ? source : _lastSource;
    }
    private void Publish(bool rewind)
    {
        if (_publishing || !_attached || _disposed) return;
        _publishing = true;
        try
        {
            var delta = _elapsed - _published;
            _published = _elapsed;
            if (!_advance(new(_elapsed, delta, rewind))) SetRunning(false);
        }
        finally { _publishing = false; }
    }
    internal void Pulse(TimeSpan timestamp)
    {
        if (!_eligible || !_running || _authored.HasValue || _disposed) return;
        Accumulate(timestamp, false);
        Publish(false);
    }
    private void Synchronize()
    {
        var enabled = _ignoreOwnerEnabled || _owner.IsEffectivelyEnabled;
        _eligible = _attached && _running && _owner.IsEffectivelyVisible && enabled;
        if (_eligible && !_authored.HasValue) _hub?.Add(this);
        else _hub?.Remove(this);
    }
    private void VisibilityChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property != Visual.IsVisibleProperty && change.Property != InputElement.IsEffectivelyEnabledProperty) return;
        Accumulate(SourceNow, false);
        Synchronize();
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property != InputElement.IsEffectivelyEnabledProperty) return;
        Accumulate(SourceNow, false);
        Synchronize();
    }
    private void Attached(object? sender, VisualTreeAttachmentEventArgs args) => Attach();
    private void Attach()
    {
        _attached = true;
        _lastSource = SourceNow;
        for (Visual? visual = _owner; visual is not null; visual = visual.GetVisualParent())
        {
            visual.PropertyChanged += VisibilityChanged;
            _ancestors.Add(visual);
        }
        if (TopLevel.GetTopLevel(_owner) is { } root) _hub = MaterialRenderFrames.Hub(root);
        Synchronize();
    }
    private void Detached(object? sender, VisualTreeAttachmentEventArgs args)
    {
        Accumulate(SourceNow, false);
        _attached = false;
        _eligible = false;
        _hub?.Remove(this);
        _hub = null;
        foreach (var ancestor in _ancestors) ancestor.PropertyChanged -= VisibilityChanged;
        _ancestors.Clear();
    }
    public void Dispose()
    {
        if (_disposed) return;
        Detached(null, null!);
        _disposed = true;
        _owner.AttachedToVisualTree -= Attached;
        _owner.DetachedFromVisualTree -= Detached;
        _owner.PropertyChanged -= OwnerChanged;
    }
}
