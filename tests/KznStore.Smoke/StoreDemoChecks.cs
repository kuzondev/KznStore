using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using KznStore.Store;
using KznStore.Views;

namespace KznStore.Smoke;

internal static class StoreDemoChecks
{
    public static async Task RunAsync(string root, Action<bool, string> check, Action<FrameworkElement, string> capture)
    {
        var dir = Path.Combine(root, "store-demo");
        var window = new StoreWindow(dir, (_, _) => Task.FromResult<GitHubCommunity?>(null), enableDemo: true);
        try
        {
            window.Show(); await Task.Delay(120);
            var search = Descendants<TextBox>(window).Single(); search.Text = "KZN Test"; await Task.Delay(50);
            check(Descendants<TextBlock>(window).Any(x => x.Text.Contains("DEMO")), "KZN Test ist eindeutig als lokale Demo gekennzeichnet");
            var install = Find(window, "DemoInstall"); var source = Find(window, "DemoSource");
            check(source.TransformToAncestor(window).Transform(new Point()).Y > install.TransformToAncestor(window).Transform(new Point()).Y,
                "GitHub-Quellcodebutton liegt unter dem Installieren-Button");
            capture((FrameworkElement)window.Content, Path.Combine(root, "store-demo-ready.png"));
            var filesBefore = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).OrderBy(x => x).ToArray();
            install.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(400);
            check(!Find(window, "DemoInstall").IsEnabled && Descendants<ProgressBar>(window).Any(x => x.IsVisible && x.Value > 0), "Demo-Installation zeigt sichtbaren Fortschritt und verhindert Doppelklicks");
            capture((FrameworkElement)window.Content, Path.Combine(root, "store-demo-installing.png"));
            await Task.Delay(3000);
            check(Descendants<Button>(window).Any(x => AutomationProperties.GetAutomationId(x) == "DemoReset")
                && filesBefore.SequenceEqual(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).OrderBy(x => x)),
                "Demo-Installation endet erfolgreich ohne zusätzliche Installationsdateien");
            capture((FrameworkElement)window.Content, Path.Combine(root, "store-demo-installed.png"));
            Find(window, "DemoReset").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(50);
            check(Find(window, "DemoInstall").IsEnabled, "Demo kann für weitere Versuche zurückgesetzt werden");
        }
        finally { window.Close(); }
        var again = new StoreWindow(dir, (_, _) => Task.FromResult<GitHubCommunity?>(null));
        try { again.Show(); await Task.Delay(50); check(Descendants<Button>(again).Any(x => x.Content as string == "KZN Test"), "Demo-Aktivierung bleibt im persönlichen Store-Profil erhalten"); }
        finally { again.Close(); }
        var normal = new StoreWindow(Path.Combine(root, "store-without-demo"), (_, _) => Task.FromResult<GitHubCommunity?>(null));
        try { normal.Show(); await Task.Delay(50); check(!Descendants<Button>(normal).Any(x => x.Content as string == "KZN Test"), "Neue Store-Profile zeigen das private Testitem nicht an"); }
        finally { normal.Close(); }
    }
    private static Button Find(Window window, string id) => Descendants<Button>(window).Single(x => AutomationProperties.GetAutomationId(x) == id);
    private static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); if (child is T typed) yield return typed;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
