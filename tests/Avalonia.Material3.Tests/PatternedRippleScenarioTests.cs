using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Material3.Controls;
using Avalonia.Material3.Tokens;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Runtime.InteropServices;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Material3.Tests;

public class PatternedRippleScenarioTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_patterned_press_paints_white_sparkles_and_a_soft_radial_wave(bool controlOverride)
    {
        var button = new MaterialButton { Width = 240, Height = 48, Content = "", Background = new SolidColorBrush(Color.Parse("#202020")),
            Foreground = Brushes.Red, CornerRadius = new CornerRadius(16) };
        using var host = new GeometryHost(button, 240, 48);
        host.Theme.Motion = new MaterialMotion();
        if (controlOverride) MaterialRipple.SetStyle(button, MaterialRippleStyle.Patterned);
        else host.Theme.RippleStyle = MaterialRippleStyle.Patterned;
        host.Render();
        using var bitmap = new RenderTargetBitmap(new PixelSize(240,48), new Vector(96,96));
        using var pixels = new WriteableBitmap(new PixelSize(240,48), new Vector(96,96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        Color[] Capture()
        {
            bitmap.Render(button); using var storage = pixels.Lock(); bitmap.CopyPixels(storage);
            var result = new Color[200*32];
            for (var y=0;y<32;y++) for(var x=0;x<200;x++)
            {
                var offset=(y+8)*storage.RowBytes+(x+20)*4;
                result[y*200+x]=Color.FromRgb(Marshal.ReadByte(storage.Address,offset+2),Marshal.ReadByte(storage.Address,offset+1),Marshal.ReadByte(storage.Address,offset));
            }
            return result;
        }
        var rest = Capture();
        var origin = new Point(40, 24); host.Window.MouseDown(origin, MouseButton.Left);
        var maxSparkles = 0; var maxIntensities = 0;
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); TimeSpan? started=null;
        void Sample(TimeSpan time)
        {
            started??=time; var frame=Capture(); var sparkles=0; var reds=new HashSet<byte>();
            for(var i=0;i<frame.Length;i++)
            {
                var color=frame[i]; if(color.G>rest[i].G+2 && color.B>rest[i].B+2) sparkles++;
                if(color.G<=32 && color.B<=32) reds.Add(color.R);
            }
            maxSparkles=Math.Max(maxSparkles,sparkles); maxIntensities=Math.Max(maxIntensities,reds.Count);
            if(time-started.Value>=TimeSpan.FromMilliseconds(450)) observed.TrySetResult();
            else host.Window.RequestAnimationFrame(Sample);
        }
        host.Window.RequestAnimationFrame(Sample); host.Render(); await observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(maxSparkles > 5, $"White procedural sparkle pixels must brighten green and blue over the red wave ({maxSparkles}).");
        Assert.True(maxIntensities > 5, "The radial wave must paint several gradient intensities.");
        host.Window.MouseUp(origin, MouseButton.Left);
    }

    [AvaloniaTheory]
    [InlineData(MaterialRippleStyle.Solid)]
    [InlineData(MaterialRippleStyle.Patterned)]
    public async Task Short_release_finishes_the_selected_recipe_and_preserves_its_shape_clip(MaterialRippleStyle style)
    {
        var button = new MaterialButton { Width=180, Height=48, Content="", Background=Brushes.White, Foreground=Brushes.Black, CornerRadius=new(16) };
        using var host=new GeometryHost(button,180,48);
        host.Theme.Motion=new MaterialMotion(); host.Theme.RippleStyle=style;
        host.Theme.States=new MaterialStates { HoverStateLayerOpacity=0, FocusStateLayerOpacity=0 };
        host.Render(); var outside=host.Pixel(5,5);
        using var bitmap=new RenderTargetBitmap(new PixelSize(180,48),new Vector(96,96));
        using var pixels=new WriteableBitmap(new PixelSize(180,48),new Vector(96,96),PixelFormat.Bgra8888,AlphaFormat.Premul);
        host.Window.MouseDown(new Point(40,24),MouseButton.Left); host.Window.MouseUp(new Point(40,24),MouseButton.Left);
        var samples=new List<(double Time,byte Blue)>();
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); TimeSpan? started=null;
        void Sample(TimeSpan time)
        {
            started??=time; bitmap.Render(button); using var storage=pixels.Lock(); bitmap.CopyPixels(storage);
            samples.Add(((time-started.Value).TotalMilliseconds,Marshal.ReadByte(storage.Address,24*storage.RowBytes+90*4)));
            if(time-started.Value>=TimeSpan.FromMilliseconds(900)) done.TrySetResult(); else host.Window.RequestAnimationFrame(Sample);
        }
        host.Window.RequestAnimationFrame(Sample); host.Render(); await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if(style==MaterialRippleStyle.Patterned) Assert.Contains(samples,s=>s.Time is >=400 and <550 && s.Blue<254);
        else Assert.All(samples.Where(s=>s.Time>=400),s=>Assert.Equal(255,s.Blue));
        Assert.Equal(255,samples[^1].Blue); Assert.Equal(outside,host.Pixel(5,5));
    }

    [AvaloniaTheory]
    [InlineData(MaterialRippleStyle.Solid)]
    [InlineData(MaterialRippleStyle.Patterned)]
    public async Task Rapid_native_input_style_changes_and_reattachment_keep_ripple_lifetimes_bounded(MaterialRippleStyle style)
    {
        var button=new MaterialButton { Width=180, Height=48,Content="",Background=new SolidColorBrush(Color.Parse("#202020")),Foreground=Brushes.Red };
        using var host=new GeometryHost(button,180,48); host.Theme.Motion=new MaterialMotion(); host.Theme.RippleStyle=style;
        host.Theme.States=new MaterialStates {HoverStateLayerOpacity=0,FocusStateLayerOpacity=0}; host.Render();
        var rest=host.Pixel(90,24); var point=new Point(40,24); var actions=0; button.Click+=(_,_)=>actions++;
        for(var press=0;press<4;press++) {host.Window.MouseDown(point,MouseButton.Left); host.Window.MouseUp(point,MouseButton.Left);}
        Assert.Equal(4,actions); await Task.Delay(30); host.Render(); Assert.NotEqual(rest,host.Pixel(90,24));
        MaterialRipple.SetStyle(button,style==MaterialRippleStyle.Solid?MaterialRippleStyle.Patterned:MaterialRippleStyle.Solid); host.Render();
        Assert.Equal(rest,host.Pixel(90,24));
        MaterialRipple.SetStyle(button,null);
        Color CurrentCenter()
        {
            using var bitmap=new RenderTargetBitmap(new PixelSize(180,48),new Vector(96,96)); bitmap.Render(button);
            using var pixels=new WriteableBitmap(new PixelSize(180,48),new Vector(96,96),PixelFormat.Bgra8888,AlphaFormat.Premul);
            using var storage=pixels.Lock();bitmap.CopyPixels(storage); var offset=24*storage.RowBytes+90*4;
            return Color.FromRgb(Marshal.ReadByte(storage.Address,offset+2),Marshal.ReadByte(storage.Address,offset+1),Marshal.ReadByte(storage.Address,offset));
        }
        host.Window.MouseDown(point,MouseButton.Left); host.Window.MouseUp(point,MouseButton.Left); host.Render(); var beforeHidden=CurrentCenter();
        button.IsVisible=false; await Task.Delay(500); button.IsVisible=true; var resumed=CurrentCenter();
        Assert.InRange(Math.Abs(beforeHidden.R-resumed.R),0,2); Assert.InRange(Math.Abs(beforeHidden.G-resumed.G),0,2);
        host.Window.MouseUp(point,MouseButton.Left);
        host.Window.MouseDown(point,MouseButton.Left); button.IsEnabled=false; button.IsEnabled=true; host.Render();
        Assert.Equal(rest,host.Pixel(90,24)); host.Window.MouseUp(point,MouseButton.Left);
        var mount=(Border)host.Window.Content!;
        host.Window.MouseDown(point,MouseButton.Left); mount.Child=null; host.Render();
        mount.Child=button; host.Render(); Assert.Equal(rest,host.Pixel(90,24)); host.Window.MouseUp(point,MouseButton.Left);
        host.Theme.Motion=host.Theme.Motion with {ReduceMotion=true}; host.Render();
        host.Window.MouseDown(point,MouseButton.Left); host.Render(); Assert.NotEqual(rest,host.Pixel(90,24));
        host.Window.MouseUp(point,MouseButton.Left); host.Render(); Assert.Equal(rest,host.Pixel(90,24));
        button.Focus(NavigationMethod.Tab); var beforeKeyboard=actions;
        host.Window.KeyPress(Key.Space,RawInputModifiers.None,PhysicalKey.Space," "); host.Render(); Assert.NotEqual(rest,host.Pixel(90,24));
        host.Window.KeyRelease(Key.Space,RawInputModifiers.None,PhysicalKey.Space," "); host.Render(); Assert.Equal(rest,host.Pixel(90,24)); Assert.Equal(beforeKeyboard+1,actions);
    }

    [AvaloniaFact]
    public async Task Patterned_noise_quantization_keeps_its_physical_pixel_grid_at_double_dpi()
    {
        var button=new MaterialButton {Width=240,Height=48,Content="",Background=new SolidColorBrush(Color.Parse("#202020")),Foreground=Brushes.Red};
        using var host=new GeometryHost(button,240,48); host.Theme.Motion=new MaterialMotion(); host.Theme.RippleStyle=MaterialRippleStyle.Patterned; host.Render();
        var face=button.GetVisualDescendants().OfType<Border>().Single(b=>b.Name=="Container");
        var offset=GeometryHost.Box(face,host.Window).Left*2;
        using var bitmap=new RenderTargetBitmap(new PixelSize(480,96),new Vector(192,192));
        using var pixels=new WriteableBitmap(new PixelSize(480,96),new Vector(192,192),PixelFormat.Bgra8888,AlphaFormat.Premul);
        var bestEdges=0; var bestAligned=0;
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); TimeSpan? started=null;
        host.Window.MouseDown(new Point(40,24),MouseButton.Left);
        void Sample(TimeSpan time)
        {
            started??=time; bitmap.Render(button); using var storage=pixels.Lock(); bitmap.CopyPixels(storage);
            var edges=0; var aligned=0;
            for(var y=24;y<72;y++) for(var x=40;x<440;x++)
            {
                var left=Marshal.ReadByte(storage.Address,y*storage.RowBytes+(x-1)*4+1);
                var right=Marshal.ReadByte(storage.Address,y*storage.RowBytes+x*4+1);
                if(Math.Abs(left-right)<4 || left>100 || right>100) continue;
                edges++;
                if(Math.Floor((x-.5-offset)/2.1)!=Math.Floor((x+.5-offset)/2.1)) aligned++;
            }
            if(edges>bestEdges) {bestEdges=edges;bestAligned=aligned;}
            if(time-started.Value>=TimeSpan.FromMilliseconds(350)) done.TrySetResult(); else host.Window.RequestAnimationFrame(Sample);
        }
        host.Window.RequestAnimationFrame(Sample); host.Render(); await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(bestEdges>20,$"Expected visible sparkle-grid edges ({bestEdges}).");
        Assert.True(bestAligned>=bestEdges*.95,$"Physical2.1px grid alignment {bestAligned}/{bestEdges}.");
        host.Window.MouseUp(new Point(40,24),MouseButton.Left);
    }
}
