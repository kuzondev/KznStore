using KznStore.Views;

namespace KznStore;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kzn Store");
        var window = new StoreWindow(dataDirectory, enableDemo: e.Args.Contains("--store-demo"));
        MainWindow = window;
        window.Show();
    }
}
