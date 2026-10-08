using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

internal interface IMaterialInputScope
{
    void RefreshInputScope();
}

// A library-owned parent separates synthetic input eligibility from the child's authored
// IsEnabled/core/command. It measures and arranges as an ordinary transparent decorator.
internal sealed class MaterialInputScope : Decorator, IMaterialInputScope
{
    internal MaterialInputScope(Control? child) { Child = child; UseLayoutRounding = false; }
    protected override bool IsEnabledCore => base.IsEnabledCore && MaterialModalPaintScope.IsInputScopeOpen(this);
    void IMaterialInputScope.RefreshInputScope() => UpdateIsEffectivelyEnabled();
}
