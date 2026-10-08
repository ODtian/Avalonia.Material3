using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Material3.Controls;

// Modality keeps the real enabled/input/automation gate. This flag is presentation only.
internal sealed class MaterialModalPaintScope : AvaloniaObject, IDisposable
{
    internal static readonly AttachedProperty<bool> EnabledForPaintProperty =
        AvaloniaProperty.RegisterAttached<MaterialModalPaintScope, Control, bool>("EnabledForPaint");
    private static readonly ConditionalWeakTable<Control, MaterialModalPaintScope> Active = new();
    private const string PaintClass = ":modal-paint-enabled";
    private readonly Control _root;
    private readonly Dictionary<Control, Watch> _controls = [];
    private readonly Dictionary<Control, Watch> _ancestors = [];
    private bool _refreshing, _disposed;

    internal static bool IsEnabledForPaint(Control control) => control.IsEffectivelyEnabled || control.GetValue(EnabledForPaintProperty);

    internal MaterialModalPaintScope(Control root)
    {
        _root = root; Active.Add(root, this);
        root.LayoutUpdated += LayoutUpdated;
        Refresh(); // Set paint eligibility before the real gate changes.
        root.IsEnabled = false;
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
            var gated = Active.TryGetValue(ancestor, out _);
            covered |= gated;
            if (!gated && !ancestor.IsEnabled || ancestor is Button { Command: { } command } button && !command.CanExecute(button.CommandParameter))
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
        Active.Remove(_root); _root.IsEnabled = true;
        foreach (var pair in _controls) { pair.Value.Dispose(); Apply(pair.Key); }
        _controls.Clear();
        foreach (var watch in _ancestors.Values) watch.Dispose();
        _ancestors.Clear();
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
