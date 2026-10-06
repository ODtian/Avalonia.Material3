using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Avalonia.Material3.Controls;

/// <summary>A persistent page-action bar, distinct from destination navigation. Host supplies actions/FAB.</summary>
public class MaterialBottomAppBar : TemplatedControl
{
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<MaterialBottomAppBar, object?>(nameof(Actions));
    public static readonly StyledProperty<IDataTemplate?> ActionsTemplateProperty = AvaloniaProperty.Register<MaterialBottomAppBar, IDataTemplate?>(nameof(ActionsTemplate));
    public static readonly StyledProperty<object?> FloatingActionProperty = AvaloniaProperty.Register<MaterialBottomAppBar, object?>(nameof(FloatingAction));
    public static readonly StyledProperty<IDataTemplate?> FloatingActionTemplateProperty = AvaloniaProperty.Register<MaterialBottomAppBar, IDataTemplate?>(nameof(FloatingActionTemplate));
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public IDataTemplate? ActionsTemplate { get => GetValue(ActionsTemplateProperty); set => SetValue(ActionsTemplateProperty, value); }
    public object? FloatingAction { get => GetValue(FloatingActionProperty); set => SetValue(FloatingActionProperty, value); }
    public IDataTemplate? FloatingActionTemplate { get => GetValue(FloatingActionTemplateProperty); set => SetValue(FloatingActionTemplateProperty, value); }
    protected override Type StyleKeyOverride => typeof(MaterialBottomAppBar);
    protected override AutomationPeer OnCreateAutomationPeer() => new AppBarAutomationPeer(this);
}
