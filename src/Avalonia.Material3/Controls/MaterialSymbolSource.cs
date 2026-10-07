using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avalonia.Material3.Controls;

// Immutable property defaults must not share a Control parent. The ordinary data-template
// seam creates a fresh symbol. Explicit caller ContentTemplate/IconTemplate still wins.
internal sealed record MaterialSymbolSource(string Name);
internal sealed class MaterialSymbolTemplate : IDataTemplate
{
    public static MaterialSymbolTemplate Instance { get; } = new();
    public bool Match(object? data) => data is MaterialSymbolSource;
    public Control? Build(object? data) => data is MaterialSymbolSource source ? new MaterialSymbol { Symbol = source.Name } : null;
}
