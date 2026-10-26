using System.Windows.Controls;
using System.Windows.Automation;
using KznStore.Store;

namespace KznStore.Views;

public sealed partial class StoreWindow
{
    // Opt-in local preview only. Imported catalogs cannot create simulated products.
    private readonly StoreProduct _demoProduct = new()
    {
        Id = "local-kzn-test", Name = "KZN Test", Version = "1.0.0.0", PackageName = "Kzn.LocalPreview", Publisher = "Kzn",
        Description = new() { ["de"] = "Dein Testplatz für den Kzn Store. Probiere den Installationsablauf aus.", ["en"] = "Your Kzn Store playground. Try the installation flow." }
    };
    private readonly int _demoStars = Random.Shared.Next(80, 8000);
    private bool _demoEnabled, _demoInstalled, _demoInstalling;
    private int _demoProgress;
    private CancellationTokenSource? _demoCancellation;
    private bool IsDemo(StoreProduct product) => ReferenceEquals(product, _demoProduct);

    private void ButtonIcon(Button button, string label, string glyph)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        content.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 13, Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        button.Content = content; AutomationProperties.SetName(button, label);
    }
    private void GitHubButtonIcon(Button button, string label)
    {
        button.SetResourceReference(Control.BackgroundProperty, "GitHubButtonBrush");
        button.SetResourceReference(Control.ForegroundProperty, "GitHubButtonForegroundBrush");
        button.SetResourceReference(Control.BorderBrushProperty, "GitHubButtonBrush");
        var content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        // A small vector Octocat mark; unlike the Fluent code glyph it reads as the GitHub brand.
        var mark = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M12 .297a12 12 0 0 0-3.797 23.4c.6.111.82-.261.82-.577v-2.234c-3.338.726-4.043-1.416-4.043-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.09-.745.083-.73.083-.73 1.205.085 1.84 1.237 1.84 1.237 1.07 1.835 2.807 1.305 3.492.998.108-.776.418-1.305.762-1.605-2.665-.3-5.467-1.333-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.536-1.524.117-3.176 0 0 1.008-.323 3.301 1.23a11.5 11.5 0 0 1 6 0c2.291-1.553 3.297-1.23 3.297-1.23.655 1.652.243 2.873.12 3.176.77.84 1.233 1.91 1.233 3.221 0 4.61-2.807 5.628-5.48 5.921.43.372.814 1.102.814 2.222v3.293c0 .319.216.694.825.576A12.001 12.001 0 0 0 12 .297"),
            Width = 15, Height = 15, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 7, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        mark.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "GitHubButtonForegroundBrush");
        content.Children.Add(mark);
        content.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        button.Content = content; button.HorizontalContentAlignment = HorizontalAlignment.Center;
        AutomationProperties.SetName(button, label);
    }
    private StackPanel ProductActions(StoreProduct product)
    {
        var actions = new StackPanel { MinWidth = 120, MaxWidth = 180 };
        Button primary;
        if (IsDemo(product))
        {
            var key = _demoInstalling ? "DemoInstalling" : _demoInstalled ? "DemoOpen" : "Install";
            primary = Pill(key, () => { if (_demoInstalled) { _statusKey = "DemoComplete"; _status.Text = _strings[_statusKey]; } else _ = SimulateDemoAsync(); }, !_demoInstalled);
            primary.IsEnabled = !_demoInstalling;
            primary.HorizontalContentAlignment = HorizontalAlignment.Center;
            AutomationProperties.SetAutomationId(primary, "DemoInstall");
            ButtonIcon(primary, _demoInstalling ? $"{_strings[key]} {_demoProgress}%" : _strings[key], _demoInstalled ? "\uE768" : "\uE896");
        }
        else
        {
            primary = ProductAction(product);
            primary.HorizontalContentAlignment = HorizontalAlignment.Center;
            if (primary.Content as string == _strings["Install"] || primary.Content as string == _strings["Update"])
                ButtonIcon(primary, primary.Content.ToString()!, "\uE896");
            else if (primary.Content as string == _strings["Open"])
                ButtonIcon(primary, primary.Content.ToString()!, "\uE768");
        }
        actions.Children.Add(primary);
        if (IsDemo(product) || GitHubCommunityClient.ValidRepository(product.Repository))
        {
            var source = Pill("GitHub", () => OpenRepository(product)); source.Margin = actions.Orientation == Orientation.Horizontal ? new Thickness(5, 0, 0, 0) : new Thickness(0, 5, 0, 0); source.FontSize = 12;
            source.MinHeight = primary.MinHeight; source.HorizontalContentAlignment = HorizontalAlignment.Center;
            source.Tag = "GitHubBrand";
            source.ToolTip = _strings[IsDemo(product) ? "DemoSource" : "Source"];
            GitHubButtonIcon(source, "GitHub");
            if (IsDemo(product)) AutomationProperties.SetAutomationId(source, "DemoSource");
            actions.Children.Add(source);
        }
        if (IsDemo(product) && _demoInstalling)
        {
            actions.Children.Add(new ProgressBar { Value = _demoProgress, Minimum = 0, Maximum = 100, Height = 4, Margin = new Thickness(4, 4, 4, 6) });
            var cancel = Pill("Cancel", () => _demoCancellation?.Cancel()); cancel.FontSize = 11; cancel.MinHeight = 26; actions.Children.Add(cancel);
        }
        if (IsDemo(product) && _demoInstalled)
        {
            var reset = Pill("DemoReset", () => { _demoInstalled = false; _demoProgress = 0; _statusKey = "DemoOnly"; Build(); });
            reset.FontSize = 11; reset.MinHeight = 26; reset.Background = Brushes.Transparent;
            AutomationProperties.SetAutomationId(reset, "DemoReset"); actions.Children.Add(reset);
        }
        return actions;
    }
    private async Task SimulateDemoAsync()
    {
        if (_demoInstalling || _demoInstalled) return;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _demoCancellation = cancellation; _demoInstalling = true; _demoProgress = 0; _statusKey = "DemoOnly";
        _status.Text = _strings[_statusKey]; RenderProducts();
        try
        {
            for (int step = 1; step <= 20; step++)
            {
                await Task.Delay(130, cancellation.Token);
                _demoProgress = step * 5; if (!_closed) RenderProducts();
            }
            _demoInstalled = true; _statusKey = "DemoComplete";
        }
        catch (OperationCanceledException) { _demoProgress = 0; _statusKey = "Cancelled"; }
        finally
        {
            _demoInstalling = false; _demoCancellation = null;
            if (!_closed) { _status.Text = _strings[_statusKey]; RenderProducts(); }
        }
    }
    private void OpenDemoSource()
    {
        const string url = "https://github.com/kuzondev/KznNotch/blob/main/src/KznStore/StoreWindow.Demo.cs";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { _status.Text = _strings["Error"] + ex.Message; }
    }
}
