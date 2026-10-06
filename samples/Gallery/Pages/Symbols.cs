using Avalonia.Material3.Controls;

namespace Gallery.Pages;

// Each call returns a fresh visual. Template slots keep caller ContentTemplate ownership.
internal static class Symbols
{
    public static MaterialSymbol Create(string name, double size = 24, bool filled = false) =>
        new() { Symbol = name, Size = size, Filled = filled };
}
