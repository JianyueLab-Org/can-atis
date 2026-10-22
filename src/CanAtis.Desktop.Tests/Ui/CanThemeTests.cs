using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using CanAtis.Ui.Controls;
using CanAtis.Ui.Dialogs;
using CanAtis.Ui.Windows;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(CanAtis.Tests.Ui.CanThemeTestApplication))]

namespace CanAtis.Tests.Ui;

public static class CanThemeTestApplication
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
        .UseReactiveUI();
}

public class CanThemeTests
{
    [AvaloniaFact]
    public void ApplicationLoadsCanTokensAndProductionWindows()
    {
        Assert.True(Application.Current!.TryGetResource("Brush.Bg.App", null, out var background));
        Assert.Equal(Color.Parse("#141518"), Assert.IsType<SolidColorBrush>(background).Color);
        var main = new MainWindow();
        var settings = new SettingsDialog();
        var configuration = new AtisConfigurationWindow();
        main.Show();
        settings.Show();
        configuration.Show();
        Assert.True(main.IsVisible);
        Assert.True(settings.IsVisible);
        Assert.True(configuration.IsVisible);
        configuration.Close();
        settings.Close();
        main.Close();
    }

    [AvaloniaFact]
    public void StationNavigationCanSwitchBetweenLeftAndTop()
    {
        var window = new MainWindow();
        var tabs = window.FindControl<CustomTabControl>("AtisStations")!;
        tabs.IsVisible = true;
        window.Show();
        tabs.ApplyTemplate();
        var navigation = tabs.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(control => control.Name == "StationNavigation");
        Assert.Equal(Dock.Left, DockPanel.GetDock(navigation));
        var verticalPanel = Assert.IsType<StackPanel>(tabs.ItemsPanel.Build());
        Assert.Equal(Orientation.Vertical, verticalPanel.Orientation);
        tabs.Classes.Add("top");
        Assert.Equal(Dock.Top, DockPanel.GetDock(navigation));
        Assert.True(double.IsNaN(navigation.Width));
        var horizontalPanel = Assert.IsType<StackPanel>(tabs.ItemsPanel.Build());
        Assert.Equal(Orientation.Horizontal, horizontalPanel.Orientation);
        tabs.Classes.Remove("top");
        Assert.Equal(Dock.Left, DockPanel.GetDock(navigation));
        window.Close();
    }
}
