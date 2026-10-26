using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using KznStore.Core;
using KznStore.Store;

namespace KznStore.Views;

public sealed partial class StoreWindow
{
    private Border Surface(UIElement child, double radius = 24, double padding = 20)
    {
        var border = new Border { Child = child, CornerRadius = new CornerRadius(radius), Padding = new Thickness(padding) };
        border.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush"); return border;
    }
    private Button Pill(string key, Action click, bool primary = false)
    {
        var button = Action(key, click); button.MinHeight = 36; button.MinWidth = 86; button.FontWeight = FontWeights.SemiBold;
        button.BorderThickness = new Thickness(0); button.SetResourceReference(Control.BackgroundProperty, primary ? "PrimaryActionBrush" : "ControlBrush");
        button.SetResourceReference(Control.ForegroundProperty, primary ? "OnAccentBrush" : "AccentBrush"); return button;
    }
    private FrameworkElement AppIcon(StoreProduct product, double size)
    {
        UIElement child;
        if (product.Id == "kzn-notch")
        {
            var mark = new Grid { Width = size, Height = size };
            mark.Children.Add(new System.Windows.Shapes.Ellipse { Width = size * .76, Height = size * .43, Stroke = Brushes.White, StrokeThickness = size * .08, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            mark.Children.Add(new System.Windows.Shapes.Ellipse { Width = size * .12, Height = size * .12, Fill = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, RenderTransform = new TranslateTransform(size * .19, 0) });
            child = mark;
        }
        else { var initial = Label(product.Name[..1], size * .45); initial.HorizontalAlignment = HorizontalAlignment.Center; initial.VerticalAlignment = VerticalAlignment.Center; child = initial; }
        return new Border { Child = child, Width = size, Height = size, CornerRadius = new CornerRadius(size * .23), Background = product.Id == "kzn-notch" ? new SolidColorBrush(Color.FromRgb(8, 10, 14)) : (Brush)FindResource("PreviewBrush"), ClipToBounds = true };
    }
    private void Navigate(string page)
    {
        _page = page; _library = page == "library"; _selectedProduct = null; Build();
        if (SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast)
            _products.BeginAnimation(OpacityProperty, new DoubleAnimation(.45, 1, TimeSpan.FromMilliseconds(160)));
    }
    private void Build()
    {
        if (_closed) return;
        _building = true;
        Content = BuildDashboard();
        _building = false;
        RenderProducts();
        if (_wideLayout) RenderRightRail();
    }
    private Windows.ApplicationModel.Package? Installed(StoreProduct product)
    {
        try { return new Windows.Management.Deployment.PackageManager().FindPackagesForUser("").FirstOrDefault(x => x.Id.Name == product.PackageName && x.Id.Publisher == product.Publisher); }
        catch (Exception ex) { StoreLog.Error("Store installed packages", ex); return null; }
    }
    private string Stars(StoreProduct product)
    {
        if (IsDemo(product)) return _demoStars.ToString("N0", CultureInfo.GetCultureInfo(_strings.Language == "de" ? "de-DE" : "en-US"));
        if (_community.TryGetValue(product.Repository, out var community)) return community is null ? _strings["OfflineStars"] : community.Stars.ToString("N0", CultureInfo.GetCultureInfo(_strings.Language == "de" ? "de-DE" : "en-US"));
        return _refreshingCommunity ? _strings["LoadingStars"] : _strings["OfflineStars"];
    }
    private Button CommunityButton(StoreProduct product)
    {
        var button = Pill("Stars", () => OpenRepository(product)); button.Content = "☆ " + Stars(product) + " · " + _strings[IsDemo(product) ? "DemoStars" : "Stars"];
        button.ToolTip = _strings[IsDemo(product) ? "DemoOnly" : "StarsHint"]; button.Background = Brushes.Transparent; button.HorizontalAlignment = HorizontalAlignment.Left; button.Padding = new Thickness(0, 5, 0, 5); return button;
    }
    private void OpenRepository(StoreProduct product)
    {
        if (IsDemo(product)) { OpenDemoSource(); return; }
        if (!GitHubCommunityClient.ValidRepository(product.Repository)) return;
        try { Process.Start(new ProcessStartInfo("https://github.com/" + product.Repository) { UseShellExecute = true }); }
        catch (Exception ex) { _status.Text = _strings["Error"] + ex.Message; }
    }
    private Button ProductAction(StoreProduct product)
    {
        var installed = Installed(product); var version = installed?.Id.Version;
        var installedVersion = version is { } v ? new Version(v.Major, v.Minor, v.Build, v.Revision) : null;
        var hasUpdate = installedVersion is not null && Version.Parse(product.Version) > installedVersion;
        var key = installed is not null && (!hasUpdate || string.IsNullOrEmpty(product.DownloadUrl)) ? "Open" : string.IsNullOrEmpty(product.DownloadUrl) ? "Unavailable" : hasUpdate ? "Update" : "Install";
        var button = Pill(key, () => { if (key == "Open") Open(product, installed!); else _ = InstallAsync(product); }, key is "Install" or "Update");
        button.IsEnabled = _download is null && key != "Unavailable"; return button;
    }
    private FrameworkElement Feature(StoreProduct product, bool detail = false)
    {
        var grid = new Grid { MinHeight = detail ? 184 : 232, ClipToBounds = true };
        grid.ColumnDefinitions.Add(new() { Width = new GridLength(1.15, GridUnitType.Star) }); grid.ColumnDefinitions.Add(new());
        grid.Background = new LinearGradientBrush(new GradientStopCollection
        {
            new GradientStop(Color.FromRgb(7, 18, 44), 0), new GradientStop(Color.FromRgb(17, 54, 112), .58), new GradientStop(Color.FromRgb(12, 104, 200), 1)
        }, 0);
        var glow = new System.Windows.Shapes.Ellipse { Width = 360, Height = 280, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0), Opacity = .66,
            Fill = new RadialGradientBrush(Color.FromArgb(220, 88, 188, 255), Color.FromArgb(10, 38, 120, 236)) };
        Grid.SetColumn(glow, 1); grid.Children.Add(glow);
        var orbit = new System.Windows.Shapes.Ellipse { Width = 250, Height = 250, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 46, 0), Stroke = new SolidColorBrush(Color.FromArgb(90, 204, 231, 255)), StrokeThickness = 1.5 };
        Grid.SetColumn(orbit, 1); grid.Children.Add(orbit);
        var copy = new StackPanel { Margin = new Thickness(24), VerticalAlignment = VerticalAlignment.Center }; grid.Children.Add(copy);
        var kicker = new TextBlock { Text = _strings[detail ? "Preview" : "Featured"], Foreground = new SolidColorBrush(Color.FromRgb(163, 185, 219)), FontSize = 11, FontWeight = FontWeights.SemiBold }; copy.Children.Add(kicker);
        copy.Children.Add(new TextBlock { Text = _strings["HeroTitle"], Foreground = Brushes.White, FontSize = 28, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 10) });
        copy.Children.Add(new TextBlock { Text = _strings["HeroSubtitle"], Foreground = new SolidColorBrush(Color.FromRgb(198, 210, 229)), FontSize = 13, TextWrapping = TextWrapping.Wrap });
        if (!detail) { var discover = Pill("ViewApp", () => { _selectedProduct = product; Build(); }); discover.Margin = new Thickness(0, 16, 0, 0); discover.HorizontalAlignment = HorizontalAlignment.Left; copy.Children.Add(discover); }
        var preview = new Grid { Margin = new Thickness(10, 16, 22, 16), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(preview, 1); grid.Children.Add(preview);
        var island = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };
        island.Children.Add(new TextBlock { Text = "Kzn Notch", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 13 });
        island.Children.Add(new TextBlock { Text = _strings.Language == "de" ? "Zeit für deinen Fokus." : "Time to find your focus.", Foreground = new SolidColorBrush(Color.FromRgb(172, 177, 189)), FontSize = 12, Margin = new Thickness(0, 12, 0, 8), TextWrapping = TextWrapping.Wrap });
        island.Children.Add(new TextBlock { Text = "25:00", Foreground = Brushes.White, FontSize = 35, FontWeight = FontWeights.Medium, HorizontalAlignment = HorizontalAlignment.Center });
        island.Children.Add(new TextBlock { Text = "♫     ◷     ☑     ♧", Foreground = new SolidColorBrush(Color.FromRgb(163, 185, 219)), FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) });
        preview.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(6, 8, 13)), CornerRadius = new CornerRadius(22), Child = island, MaxWidth = 270 });
        return new Border { Child = grid, CornerRadius = new CornerRadius(30), Margin = new Thickness(0, 2, 0, 20), ClipToBounds = true };
    }
    private void RenderProducts()
    {
        _products.Children.Clear();
        if (_selectedProduct is { } selected) { RenderDetail(selected); return; }
        if (_page == "settings") { RenderSettings(); return; }
        if (_page == "updates") { RenderUpdates(); return; }
        if (_page == "collections") { RenderCollections(); return; }
        var products = VisibleProducts().Where(p => _page != "library" || (IsDemo(p) ? _demoInstalled : Installed(p) is not null)).ToList();
        if (products.Count == 0) { _products.Children.Add(Label(_strings["Empty"], 15, true)); return; }
        if (_page == "today" && _search.Text.Length == 0)
        {
            var date = CultureInfo.GetCultureInfo(_strings.Language == "de" ? "de-DE" : "en-US");
            var heading = Label(DateTime.Now.ToString("dddd, d. MMMM", date).ToUpper(date), 10, true); heading.FontWeight = FontWeights.SemiBold; heading.Margin = new Thickness(3, 0, 0, 3); _products.Children.Add(heading);
            var welcome = Label(_strings["Welcome"], 27); welcome.FontWeight = FontWeights.Bold; _products.Children.Add(welcome);
            var subtitle = Label(_strings["WelcomeSubtitle"], 13, true); subtitle.Margin = new Thickness(0, 2, 0, 15); _products.Children.Add(subtitle);
            if (products.FirstOrDefault(p => p.Id == "kzn-notch") is { } feature) _products.Children.Add(Feature(feature));
            RenderFeatureCategories();
            RenderFeaturedCards(products);
            return;
        }
        AddPageHeading(_page == "library" ? _strings["Library"] : _strings["AllApps"], _strings["Subtitle"]);
        RenderRows(products);
    }
    private void RenderDetail(StoreProduct product)
    {
        var header = new Grid { Margin = new Thickness(0, 0, 0, 22) }; header.ColumnDefinitions.Add(new() { Width = new GridLength(108) }); header.ColumnDefinitions.Add(new()); header.Children.Add(AppIcon(product, 90));
        var title = new StackPanel(); Grid.SetColumn(title, 1); header.Children.Add(title); title.Children.Add(Label(product.Name, 27)); title.Children.Add(Label(product.Describe(_strings.Language), 14, true));
        var action = ProductActions(product); action.HorizontalAlignment = HorizontalAlignment.Left; action.Margin = new Thickness(0, 12, 0, 0); title.Children.Add(action); _products.Children.Add(header);
        var info = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 24) };
        foreach (var (label, value) in new[] { (_strings[IsDemo(product) ? "DemoStars" : "Stars"], Stars(product)), (_strings["Version"], product.Version), (_strings["Platform"], "Windows 11") })
        { var cell = new StackPanel { Margin = new Thickness(10) }; cell.Children.Add(Label(label.ToUpperInvariant(), 11, true)); var number = Label(value, 20); number.FontWeight = FontWeights.SemiBold; cell.Children.Add(number); info.Children.Add(cell); }
        _products.Children.Add(Surface(info, 18, 6)); var spacer = new Border { Height = 22 }; _products.Children.Add(spacer);
        if (product.Id == "kzn-notch") _products.Children.Add(Feature(product, true));
        var about = Label(_strings["AboutApp"], 22); about.FontWeight = FontWeights.Bold; _products.Children.Add(about);
        var description = Label(product.Describe(_strings.Language), 15); description.Margin = new Thickness(0, 10, 0, 12); _products.Children.Add(description);
        if (IsDemo(product))
        {
            _products.Children.Add(Label(_strings["DemoOnly"], 13, true));
            var hide = Pill("HideDemo", () => { _demoCancellation?.Cancel(); _demoEnabled = false; _selectedProduct = null; SavePreferences(); Build(); });
            hide.HorizontalAlignment = HorizontalAlignment.Left; hide.Margin = new Thickness(0, 16, 0, 16); _products.Children.Add(hide);
        }
        if (GitHubCommunityClient.ValidRepository(product.Repository))
        { _products.Children.Add(CommunityButton(product)); _products.Children.Add(Label(_strings["StarsHint"], 12, true)); var source = Pill("Source", () => OpenRepository(product)); source.HorizontalAlignment = HorizontalAlignment.Left; source.HorizontalContentAlignment = HorizontalAlignment.Center; source.MinHeight = 36; source.Tag = "GitHubBrand"; source.ToolTip = _strings[IsDemo(product) ? "DemoSource" : "Source"]; GitHubButtonIcon(source, "GitHub"); source.Margin = new Thickness(0, 12, 0, 12); _products.Children.Add(source); }
    }
    private async Task RefreshCommunityAsync(bool force = false)
    {
        if (_refreshingCommunity || _closed || !force && DateTimeOffset.Now - _lastCommunityRefresh < TimeSpan.FromMinutes(10)) return;
        _refreshingCommunity = true; RenderProducts(); if (_wideLayout) RenderRightRail();
        try
        {
            using var gate = new SemaphoreSlim(4);
            await Task.WhenAll(_catalog.Products.Select(p => p.Repository).Where(GitHubCommunityClient.ValidRepository).Distinct(StringComparer.OrdinalIgnoreCase).Select(async repository =>
            { await gate.WaitAsync(_lifetime.Token); try { _community[repository] = await _communityLoader(repository, _lifetime.Token); } finally { gate.Release(); } }));
            _lastCommunityRefresh = DateTimeOffset.Now;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StoreLog.Error("Store GitHub community", ex); }
        finally { _refreshingCommunity = false; if (!_closed) { RenderProducts(); if (_wideLayout) RenderRightRail(); } }
    }
    private async Task CheckUpdatesAsync()
    {
        if (_checkingUpdates) return; _checkingUpdates = true; Build();
        try { await Task.WhenAll(RefreshCommunityAsync(true), _catalogRefreshChecker!(_strings.Language)); }
        catch (Exception ex) { _statusKey = "Error"; _error = ex.Message; }
        finally { _checkingUpdates = false; Build(); }
    }
}
