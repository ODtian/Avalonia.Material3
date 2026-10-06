using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Avalonia.Material3.Themes;

/// <summary>Default wrapping label projection; replace ContentTemplate for structured labels.</summary>
public sealed class NavigationLabelTemplate : IDataTemplate
{
    public bool Match(object? data) => true;
    public Control Build(object? data) => data is Control control ? control : new TextBlock { Text = data?.ToString(), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, VerticalAlignment = Layout.VerticalAlignment.Center };
}
