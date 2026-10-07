using Avalonia.Controls;

namespace Avalonia.Material3.Controls;

// TimeInput has one supporting caption, with a two-line minimum.
internal sealed class MaterialTimeSupportingText : TextBlock
{
    public static readonly StyledProperty<MaterialTimeInputField?> OwnerProperty =
        AvaloniaProperty.Register<MaterialTimeSupportingText, MaterialTimeInputField?>(nameof(Owner));
    public MaterialTimeInputField? Owner { get=>GetValue(OwnerProperty);set=>SetValue(OwnerProperty,value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if(change.Property==OwnerProperty)
        {
            if(change.OldValue is MaterialTimeInputField old)old.PropertyChanged-=OwnerChanged;
            if(Owner is { } owner)owner.PropertyChanged+=OwnerChanged;
            Update();
        }
        if(change.Property==LineHeightProperty)MinHeight=2*(double.IsFinite(LineHeight)?LineHeight:16);
    }
    private void OwnerChanged(object? sender,AvaloniaPropertyChangedEventArgs change)
    {
        if(change.Property==MaterialTextField.LabelProperty || change.Property==MaterialTextField.EffectiveSupportingTextProperty
            || change.Property==MaterialTextField.HasErrorProperty)Update();
    }
    private void Update()
    {
        Text=Owner?.EffectiveSupportingText??Owner?.Label;
        MaterialPickerSupport.Resource(this,ForegroundProperty,Owner?.HasError==true?"ErrorBrush":"OnSurfaceVariantBrush");
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if(Owner is { } owner)owner.PropertyChanged-=OwnerChanged;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if(Owner is { } owner){owner.PropertyChanged-=OwnerChanged;owner.PropertyChanged+=OwnerChanged;}
        Update();
    }
}
