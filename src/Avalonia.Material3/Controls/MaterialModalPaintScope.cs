using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Styling;
using Avalonia.Data;

namespace Avalonia.Material3.Controls;

// Temporary presentation gates keep real input/automation disabled and preserve authored paint.
internal sealed class MaterialModalPaintScope : AvaloniaObject, IDisposable
{
    internal static readonly AttachedProperty<bool> EnabledForPaintProperty =
        AvaloniaProperty.RegisterAttached<MaterialModalPaintScope, Control, bool>("EnabledForPaint");
    private static readonly ConditionalWeakTable<Control, MaterialModalPaintScope> Active = new();
    private const string PaintClass = ":modal-paint-enabled";
    private readonly Control _root;
    private readonly bool _rootCoreEnabled;
    private readonly IDisposable _inputBinding;
    private readonly Dictionary<Control, Watch> _controls = [];
    private readonly Dictionary<Control, Watch> _ancestors = [];
    private bool _refreshing, _disposed;

    internal static bool IsEnabledForPaint(Control control) => control.IsEffectivelyEnabled || control.GetValue(EnabledForPaintProperty);
    internal static Selector DisabledPaint(Selector? selector) => selector.Class(":disabled").Not(value => value.Class(PaintClass));
    internal static Selector EnabledPaint(Selector? selector) => selector.Not(value => DisabledPaint(value));

    internal static void SetInputEnabled(ref MaterialModalPaintScope? scope, Control root, bool enabled)
    {
        if (enabled) { scope?.Dispose(); scope = null; }
        else scope ??= new(root);
    }

    internal MaterialModalPaintScope(Control root)
    {
        _root = root; _rootCoreEnabled = AuthoredEnabledCore(root);
        Active.Add(root, this);
        root.LayoutUpdated += LayoutUpdated;
        Refresh(); // Set paint eligibility before the real gate changes.
        _inputBinding = root.Bind(InputElement.IsEnabledProperty, DisabledInput.Instance, BindingPriority.Animation);
    }
    private void LayoutUpdated(object? sender, EventArgs args) => Refresh();
    private void Refresh()
    {
        if (_refreshing || _disposed) return;
        _refreshing = true;
        try
        {
            var ancestors = _root.GetVisualAncestors().OfType<Control>().ToHashSet();
            foreach (var removed in _ancestors.Keys.Where(control => !ancestors.Contains(control)).ToArray())
            { _ancestors[removed].Dispose(); _ancestors.Remove(removed); }
            foreach (var ancestor in ancestors)
                if (!_ancestors.ContainsKey(ancestor)) _ancestors.Add(ancestor, new Watch(this, ancestor));
            var current = _root.GetVisualDescendants().OfType<Control>().Prepend(_root).ToHashSet();
            foreach (var removed in _controls.Keys.Where(control => !current.Contains(control)).ToArray())
            { _controls[removed].Dispose(); _controls.Remove(removed); Apply(removed); }
            foreach (var control in current)
            {
                if (!_controls.ContainsKey(control)) _controls.Add(control, new Watch(this, control));
                Apply(control);
            }
        }
        finally { _refreshing = false; }
    }
    private static void Apply(Control control)
    {
        var covered = false; var authoredEnabled = true;
        for (Visual? visual = control; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is not Control ancestor) continue;
            var gated = Active.TryGetValue(ancestor, out var scope);
            covered |= gated;
            if (gated ? !scope!._rootCoreEnabled || !ancestor.GetBaseValue(InputElement.IsEnabledProperty).GetValueOrDefault(true) : !AuthoredEnabledCore(ancestor))
                authoredEnabled = false;
        }
        var enabled = covered && authoredEnabled;
        if (control.GetValue(EnabledForPaintProperty) != enabled) control.SetValue(EnabledForPaintProperty, enabled);
        ((IPseudoClasses)control.Classes).Set(PaintClass, enabled);
    }
    private void Changed(Control control, AvaloniaPropertyChangedEventArgs change)
    {
        if (_disposed || _refreshing) return;
        if (change.Property == InputElement.IsEffectivelyEnabledProperty) Apply(control);
        else if (change.Property == InputElement.IsEnabledProperty || change.Property == Button.CommandProperty || change.Property == Button.CommandParameterProperty)
            Refresh();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _root.LayoutUpdated -= LayoutUpdated;
        Active.Remove(_root); _inputBinding.Dispose();
        foreach (var pair in _controls) { pair.Value.Dispose(); Apply(pair.Key); }
        _controls.Clear();
        foreach (var watch in _ancestors.Values) watch.Dispose();
        _ancestors.Clear();
    }
    // Avalonia's documented protected virtual enabled contract includes caller subclasses.
    // Runtime-generated callvirt preserves that dispatch and is supported by NativeAOT.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_IsEnabledCore")]
    private static extern bool AuthoredEnabledCore(InputElement control);
    private sealed class DisabledInput : IObservable<bool>, IDisposable
    {
        internal static readonly DisabledInput Instance = new();
        public IDisposable Subscribe(IObserver<bool> observer) { observer.OnNext(false); return this; }
        public void Dispose() { }
    }
    private sealed class Watch : IDisposable
    {
        private readonly MaterialModalPaintScope _scope;
        private readonly Control _control;
        private ICommand? _command;
        internal Watch(MaterialModalPaintScope scope, Control control)
        { _scope = scope; _control = control; control.PropertyChanged += Changed; UpdateCommand(); }
        private void Changed(object? sender, AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == Button.CommandProperty) UpdateCommand();
            _scope.Changed(_control, change);
        }
        private void UpdateCommand()
        {
            if (_command is not null) _command.CanExecuteChanged -= CanExecuteChanged;
            _command = (_control as Button)?.Command;
            if (_command is not null) _command.CanExecuteChanged += CanExecuteChanged;
        }
        private void CanExecuteChanged(object? sender, EventArgs args) => _scope.Refresh();
        public void Dispose()
        { _control.PropertyChanged -= Changed; if (_command is not null) _command.CanExecuteChanged -= CanExecuteChanged; }
    }
}
