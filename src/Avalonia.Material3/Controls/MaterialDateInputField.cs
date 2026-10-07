using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// The source always supplies a supporting lambda, including its empty16-DIP box.
internal sealed class MaterialDateInputField : MaterialTextField
{
    internal MaterialDateInputField() => PseudoClasses.Set(":date-input",true);
    protected override Type StyleKeyOverride => typeof(MaterialTextField);
}
