using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Avalonia.Android;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Automation;
using Avalonia.Media;
using Avalonia.Material3.Controls;
using Avalonia.Media.TextFormatting;
using System.Security.Cryptography;
using SkiaSharp;
using Material3.ReferenceUi;

namespace Material3.ReferenceAndroid;

[Activity(Name = "org.pixivyou.m3avaloniareference.MainActivity", Label = "M3 Avalonia Reference", Theme = "@style/ReferenceTheme", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
    private string? _readyKey;
    private bool _readinessQueued;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        var intent = Intent;
        ReferenceApp.Configuration = new ReferenceConfiguration
        {
            Scene = intent?.GetStringExtra("scene") ?? "home", Dark = intent?.GetBooleanExtra("dark", false) ?? false,
            Palette = intent?.GetStringExtra("palette") ?? "classic", Locale = intent?.GetStringExtra("locale") ?? "en-US",
            ExpressiveButtons = intent?.GetBooleanExtra("expressiveButtons", true) ?? true,
            CheckboxM3 = intent?.GetBooleanExtra("checkboxM3", true) ?? true
        };
        try { ReferenceApp.Configuration.Validate(); }
        catch (ArgumentException exception) { Log.Error("M3AvaloniaReference", exception.Message); throw; }
        base.OnCreate(savedInstanceState);
        if (Content is ReferenceShell shell)
        {
            TopLevel.SetAutoSafeAreaPadding(shell, false);
            shell.AttachedToVisualTree += (_, _) => ConfigureInsets(shell);
            ConfigureInsets(shell);
            BackRequested += (_, args) => args.Handled = shell.RequestBack();
            shell.LayoutUpdated += (_, _) => QueueReadiness(shell);
        }
        var metrics = Resources?.DisplayMetrics;
        Log.Info("M3AvaloniaReference", $"scene={ReferenceApp.Configuration.Scene} dark={ReferenceApp.Configuration.Dark} palette={ReferenceApp.Configuration.Palette} locale={ReferenceApp.Configuration.Locale} density={metrics?.Density} fontScale={Resources?.Configuration?.FontScale} expressiveButtons={ReferenceApp.Configuration.ExpressiveButtons} checkboxM3={ReferenceApp.Configuration.CheckboxM3}");
    }
    private void ConfigureInsets(ReferenceShell shell)
    {
        if (TopLevel.GetTopLevel(shell) is not { InsetsManager: { } insets } root) return;
        insets.DisplayEdgeToEdgePreference = true;
        void Refresh()
        {
            // Android initializes its SurfaceView atscale1, then publishes the real density.
            // The framework already converts native inset pixels to DIP; reread after scaling changes.
            shell.SetSafeArea(insets.SafeAreaPadding);
            Log.Info("M3AvaloniaReference", $"insets rootScaling={root.RenderScaling} density={Resources?.DisplayMetrics?.Density} safeArea={insets.SafeAreaPadding} client={root.ClientSize} edgeToEdge={insets.DisplaysEdgeToEdge}");
        }
        Refresh(); insets.SafeAreaChanged += (_, _) => Refresh();
        root.ScalingChanged += (_, _) => Refresh();
    }
    private void QueueReadiness(ReferenceShell shell)
    {
        if (_readinessQueued || TopLevel.GetTopLevel(shell) is not { } root || root.ClientSize.Width < 100 || root.RenderScaling < (Resources?.DisplayMetrics?.Density ?? 1)) return;
        var clock = shell.GetVisualDescendants().OfType<MaterialClockDial>().FirstOrDefault();
        var key = $"{shell.Scene}/{shell.Dark}/{root.ClientSize}/{root.RenderScaling}/{clock?.ActivePart}/{clock?.Value}";
        if (key == _readyKey) return;
        _readinessQueued = true;
        root.RequestAnimationFrame(_ =>
        {
            _readinessQueued = false; _readyKey = key;
            Log.Info("M3AvaloniaReference", $"ready scene={shell.Scene} dark={shell.Dark} uptime={SystemClock.UptimeMillis()} rootScaling={root.RenderScaling} safeArea={root.InsetsManager?.SafeAreaPadding} client={root.ClientSize} hardwareAccelerated={Window?.DecorView.IsHardwareAccelerated}");
            foreach (var weight in new[] { FontWeight.Normal, FontWeight.Medium, FontWeight.Bold })
                if (FontManager.Current.TryGetGlyphTypeface(new Typeface(shell.MaterialTheme.Typography.FontFamily, FontStyle.Normal, weight), out var face))
                    Log.Info("M3AvaloniaReference", $"font requestedWeight={(int)weight} actualWeight={(int)face.Weight} family={face.FamilyName} simulations={face.FontSimulations}");
            if (shell.Scene == "buttons")
            {
                var systemMedium = SKTypeface.FromFamilyName("sans-serif", 500, 5, SKFontStyleSlant.Upright);
                if (systemMedium is not null)
                {
                    using var stream = systemMedium.OpenStream(out var index);
                    if (stream is not null)
                    {
                        using var data = SKData.Create(stream);
                        if (data is not null) Log.Info("M3AvaloniaReference", $"system-medium family={systemMedium.FamilyName} weight={systemMedium.FontStyle.Weight} width={systemMedium.FontStyle.Width} slant={systemMedium.FontStyle.Slant} glyphs={systemMedium.GlyphCount} ttc={index} sha={Convert.ToHexString(SHA256.HashData(data.ToArray()))}");
                    }
                }
            }
            if (clock is not null)
                foreach (var number in clock.Children.OfType<MaterialClockNumber>().Where(number => number.Value is 3 or 5))
                {
                    var typeface = new Typeface(number.FontFamily, number.FontStyle, number.FontWeight);
                    using var paragraph = new TextLayout(number.Content?.ToString() ?? string.Empty, typeface, number.FontSize, number.Foreground,
                        lineHeight: number.GetValue(TextBlock.LineHeightProperty), letterSpacing: number.LetterSpacing);
                    Log.Info("M3AvaloniaReference", $"clock-font value={number.Value} runs={string.Join(',', paragraph.TextLines.SelectMany(line => line.TextRuns).Select(run => run.GetType().Name))} paragraph={paragraph.Width},{paragraph.Height} baseline={paragraph.TextLines[0].Baseline} options={TextOptions.GetTextOptions(number)}");
                    if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphFace) && glyphFace.PlatformTypeface.TryGetStream(out var fontStream))
                        using (fontStream)
                            Log.Info("M3AvaloniaReference", $"clock-font-face value={number.Value} family={glyphFace.FamilyName} weight={glyphFace.Weight} stretch={glyphFace.Stretch} style={glyphFace.Style} simulations={glyphFace.FontSimulations} glyphs={glyphFace.GlyphCount} sha={Convert.ToHexString(SHA256.HashData(fontStream))}");
                    foreach (var visual in number.GetVisualDescendants().OfType<Control>().Prepend(number).Where(control => control == number || control.GetType().Name == "MaterialClockLabel"))
                        if (visual.TransformToVisual(root) is { } matrix)
                        {
                            var box = new Avalonia.Rect(visual.Bounds.Size).TransformToAABB(matrix); var density = root.RenderScaling;
                            Log.Info("M3AvaloniaReference", $"clock-layout part={clock.ActivePart} value={number.Value} visual={visual.GetType().Name} dip={box} physical={box.X * density},{box.Y * density},{box.Width * density},{box.Height * density} phase={box.X * density - Math.Floor(box.X * density)},{box.Y * density - Math.Floor(box.Y * density)}");
                        }
                }
            foreach (var control in shell.GetVisualDescendants().OfType<Control>())
            {
                var id = AutomationProperties.GetAutomationId(control);
                if (string.IsNullOrEmpty(id) || control.TransformToVisual(root) is not { } transform) continue;
                var bounds = new Avalonia.Rect(control.Bounds.Size).TransformToAABB(transform);
                Log.Info("M3AvaloniaReference", $"tag scene={shell.Scene} id={id} dip={bounds} physical={bounds.X * root.RenderScaling},{bounds.Y * root.RenderScaling},{bounds.Width * root.RenderScaling},{bounds.Height * root.RenderScaling} visible={control.IsEffectivelyVisible} enabled={control.IsEffectivelyEnabled}");
            }
        });
    }
    public override bool DispatchTouchEvent(MotionEvent? motion)
    {
        if (motion is not null && motion.ActionMasked is MotionEventActions.Down or MotionEventActions.Up or MotionEventActions.Cancel)
            Log.Info("M3AvaloniaReference", $"pointer action={(int)motion.ActionMasked} eventUptime={motion.EventTime} observedUptime={SystemClock.UptimeMillis()} x={motion.GetX()} y={motion.GetY()} rawX={motion.RawX} rawY={motion.RawY}");
        return base.DispatchTouchEvent(motion);
    }
}
