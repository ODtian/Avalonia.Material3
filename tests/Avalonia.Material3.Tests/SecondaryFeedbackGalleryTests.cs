using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gallery.Pages;
using Xunit;

namespace Avalonia.Material3.Tests;

public class SecondaryFeedbackGalleryTests
{
    [AvaloniaFact]
    public void Gallery_menu_and_snackbar_actions_change_only_the_named_host_result()
    {
        using var host = new FeedbackHost();
        var page = new SecondaryFeedbackPage(host.Theme);
        host.Window.Content = page; host.Render();
        host.Click(page.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "Standard menu"));
        host.Key(Key.Enter);
        Assert.Equal("Menu: copied", page.Result.Text);
        host.Click(page.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "Show Snackbar"));
        host.Click(page.Snackbar.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "Undo"));
        Assert.Equal("Snackbar: undo performed", page.Result.Text);
        Assert.Equal(0, page.Overlay.OpenCount);
    }
}
