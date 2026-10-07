using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Avalonia.Material3.Controls;

// The stock numeric recipe uses TextBox's SelectedText edit operation, so its
// native undo/redo, selection and IME infrastructure continues to own edits.
internal sealed class MaterialDateInputEditing
{
    private readonly MaterialDatePicker _owner;
    private readonly MaterialTextField _field;
    private int _generation;
    private CancellationTokenSource? _pasteRequest;
    internal MaterialDateInputEditing(MaterialDatePicker owner, MaterialTextField field)
    {
        _owner=owner; _field=field;
        field.AddHandler(InputElement.TextInputEvent, Input, RoutingStrategies.Tunnel);
        field.AddHandler(InputElement.KeyDownEvent, Delete, RoutingStrategies.Tunnel);
        field.PastingFromClipboard += Paste;
        field.AttachedToVisualTree += (_,_)=>CancelPaste();
        field.DetachedFromVisualTree += (_,_)=>CancelPaste();
        field.PropertyChanged += (_,change)=>
        {
            if(change.Property==InputElement.IsEffectivelyEnabledProperty&&!field.IsEffectivelyEnabled ||
                change.Property==TextBox.IsReadOnlyProperty&&field.IsReadOnly)CancelPaste();
        };
    }
    private bool Enabled => _owner.InputFormat is null && _field.IsEffectivelyEnabled && !_field.IsReadOnly;
    private void Input(object? sender, TextInputEventArgs args)
    {
        if(!Enabled || args.Handled || args.Text is null)return;
        args.Handled=true; Insert(args.Text);
    }
    private void Insert(string text)
    {
        if(text.Any(c=>!char.IsDigit(c)))return;
        var current=_field.Text??"";
        var start=Math.Min(_field.SelectionStart,_field.SelectionEnd);
        var end=Math.Max(_field.SelectionStart,_field.SelectionEnd);
        var prefix=new string(current[..start].Where(char.IsDigit).ToArray());
        var suffix=new string(current[end..].Where(char.IsDigit).ToArray());
        var digits=prefix+text+suffix;
        if(digits.Length>8)return;
        Edit(digits,prefix.Length+text.Length);
    }
    private void Delete(object? sender, KeyEventArgs args)
    {
        if(!Enabled || args.Handled || args.KeyModifiers!=KeyModifiers.None || args.Key is not (Key.Back or Key.Delete))return;
        var current=_field.Text??"";
        var start=Math.Min(_field.SelectionStart,_field.SelectionEnd);
        var end=Math.Max(_field.SelectionStart,_field.SelectionEnd);
        var prefix=new string(current[..start].Where(char.IsDigit).ToArray());
        var suffix=new string(current[end..].Where(char.IsDigit).ToArray());
        if(start==end)
        {
            if(args.Key==Key.Back && prefix.Length>0)prefix=prefix[..^1];
            else if(args.Key==Key.Delete && suffix.Length>0)suffix=suffix[1..];
            else return;
        }
        args.Handled=true; Edit(prefix+suffix,prefix.Length);
    }
    private void Edit(string digits,int caretDigits)
    {
        var pattern=_owner.EffectiveInputPattern;
        var formatted=Format(pattern,digits);
        if(_field.Text==formatted)return;
        _field.SelectAll(); _field.SelectedText=formatted;
        _field.CaretIndex=Format(pattern,digits[..caretDigits]).Length;
    }
    private async void Paste(object? sender,RoutedEventArgs args)
    {
        if(!Enabled || args.Handled)return;
        var root=TopLevel.GetTopLevel(_field);
        if(root?.Clipboard is not { } clipboard)return;
        args.Handled=true;
        CancelPaste();
        using var request=new CancellationTokenSource();_pasteRequest=request;
        var generation=_generation;var session=_owner.Session;
        var originalText=_field.Text;var start=_field.SelectionStart;var end=_field.SelectionEnd;
        var pattern=_owner.EffectiveInputPattern;
        string? text;
        try { text=await clipboard.TryGetTextAsync().WaitAsync(request.Token); }
        catch(Exception error) when(error is NotSupportedException or System.IO.IOException or System.Runtime.InteropServices.ExternalException or System.ComponentModel.Win32Exception or OperationCanceledException) { return; }
        finally { if(_pasteRequest==request)_pasteRequest=null; }
        if(text is not null && generation==_generation && session==_owner.Session && Enabled &&
            _field.Text==originalText&&_field.SelectionStart==start&&_field.SelectionEnd==end&&
            TopLevel.GetTopLevel(_field)==root && _owner.EffectiveInputPattern==pattern)Insert(text);
    }
    private void CancelPaste()
    {
        _generation++;_pasteRequest?.Cancel();_pasteRequest?.Dispose();_pasteRequest=null;
    }
    private static string Format(string pattern,string digits)
    {
        var output=new System.Text.StringBuilder(); var index=0;
        foreach(var c in pattern)
        {
            if(c is 'd' or 'M' or 'y') {if(index>=digits.Length)break;output.Append(digits[index++]);}
            else if(index>0)output.Append(c);
        }
        return output.ToString();
    }
}
