using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Material3.Themes;

namespace StandaloneHost;

public partial class MainWindow : Window
{
    public Gallery.GalleryShell Shell { get; }
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Shell = new Gallery.GalleryShell(Application.Current!.Styles.OfType<MaterialTheme>().Single());
        Content = Shell;
        var scope = new NameScope();
        NameScope.SetNameScope(this, scope);
        scope.Register("ActionButton", Shell.ActionButton);
        scope.Register("ThemeButton", Shell.ThemeButton);
        scope.Register("ResultText", Shell.ResultText);
    }
}
