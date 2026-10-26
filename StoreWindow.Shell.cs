using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using KznStore.Store;

namespace KznStore.Views;

public sealed partial class StoreWindow
{
    private UIElement BuildDashboard()
    {
        var root = new Grid();
        root.SetResourceReference(Panel.BackgroundProperty, "CanvasBrush");
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(216) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sidebar = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebar.SetResourceReference(Panel.BackgroundProperty, "SidebarBrush");
        var divider = new Border { BorderThickness = new Thickness(0, 0, 1, 0), Child = sidebar };
        divider.SetResourceReference(Border.BorderBrushProperty, "DividerBrush");
        Grid.SetColumn(divider, 0); root.Children.Add(divider);

        var brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20, 22, 14, 24), VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(AppIcon(_catalog.Products.FirstOrDefault(p => p.Id == "kzn-notch") ?? StoreCatalog.Default.Products[0], 40));
        var brandName = Label("Kzn Store", 18); brandName.FontWeight = FontWeights.Bold; brandName.Margin = new Thickness(10, 0, 0, 0); brandName.VerticalAlignment = VerticalAlignment.Center; brand.Children.Add(brandName);
        sidebar.Children.Add(brand);

        var nav = new StackPanel { Margin = new Thickness(12, 0, 12, 12) }; Grid.SetRow(nav, 1); sidebar.Children.Add(nav);
        nav.Children.Add(NavItem("Overview", "today", "\uE80F"));
        nav.Children.Add(NavItem("Apps", "apps", "\uE71D"));
        nav.Children.Add(NavItem("Collections", "collections", "\uE8B7"));
        nav.Children.Add(NavItem("UpdatesNav", "updates", "\uE896"));
        nav.Children.Add(NavItem("Installed", "library", "\uE8F1"));
        nav.Children.Add(NavItem("SettingsNav", "settings", "\uE713"));

        var note = new StackPanel { Margin = new Thickness(14, 10, 14, 18) };
        var noteSurface = new Border { Child = note, CornerRadius = new CornerRadius(20), Padding = new Thickness(14) };
        noteSurface.SetResourceReference(Border.BackgroundProperty, "PreviewBrush");
        note.Children.Add(Label(_strings["AllInOne"], 15));
        var noteText = Label(_strings["SidebarSubtitle"], 12, true); noteText.Margin = new Thickness(0, 7, 0, 0); note.Children.Add(noteText);
        Grid.SetRow(noteSurface, 2); sidebar.Children.Add(noteSurface);

        var content = new Grid { Margin = new Thickness(24, 18, 24, 0) };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(content, 1); root.Children.Add(content);

        var toolbar = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var search = SearchControl(); toolbar.Children.Add(search);
        var controls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(controls, 1); toolbar.Children.Add(controls);
        var updates = Pill(_checkingUpdates ? "Checking" : "CheckUpdates", () => _ = CheckUpdatesAsync());
        updates.IsEnabled = !_checkingUpdates && _download is null; updates.Margin = new Thickness(14, 0, 8, 0); controls.Children.Add(updates);
        var import = Pill("Import", ImportCatalog); import.IsEnabled = _download is null; import.Margin = new Thickness(0, 0, 10, 0); controls.Children.Add(import);
        var languages = new ComboBox { Width = 96, MinHeight = 36, Padding = new Thickness(9, 5, 9, 5), VerticalAlignment = VerticalAlignment.Center, ItemsSource = new[] { "Deutsch", "English" }, SelectedIndex = _strings.Language == "de" ? 0 : 1 };
        AutomationProperties.SetName(languages, _strings["Language"]); controls.Children.Add(languages);
        languages.SelectionChanged += (_, _) => { _strings.Language = languages.SelectedIndex == 0 ? "de" : "en"; SavePreferences(); Build(); };
        content.Children.Add(toolbar);

        var body = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (_wideLayout) body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(294) });
        Grid.SetRow(body, 1); content.Children.Add(body);

        var main = new Grid(); main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var productsScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 12, 8) };
        if (_products.Parent is ScrollViewer oldProductsScroll) oldProductsScroll.Content = null;
        productsScroll.Content = _products; main.Children.Add(productsScroll);
        var feedback = new StackPanel { Margin = new Thickness(0, 8, 12, 10) }; Grid.SetRow(feedback, 1); main.Children.Add(feedback);
        if (_progress.Parent is Panel oldProgress) oldProgress.Children.Remove(_progress); feedback.Children.Add(_progress);
        if (_status.Parent is Panel oldStatus) oldStatus.Children.Remove(_status);
        _status.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); _status.FontSize = 11; _status.Margin = new Thickness(2, 4, 0, 0);
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite); feedback.Children.Add(_status);
        if (_download is not null) feedback.Children.Add(Pill("Cancel", () => _download.Cancel()));
        _status.Text = _strings[_statusKey] + (_statusKey == "Error" ? _error : "");
        body.Children.Add(main);

        if (_wideLayout)
        {
            if (_rightRail.Parent is ScrollViewer oldRailScroll) oldRailScroll.Content = null;
            var railScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(12, 0, 0, 8) };
            railScroll.Content = _rightRail; Grid.SetColumn(railScroll, 1); body.Children.Add(railScroll);
        }
        return root;
    }

    private UIElement SearchControl()
    {
        if (_search.Parent is Panel old) old.Children.Remove(_search);
        _search.BorderThickness = new Thickness(0); _search.MinHeight = 42;
        _search.Padding = new Thickness(42, 9, 12, 9);
        AutomationProperties.SetName(_search, _strings["Search"]);
        var wrap = new Grid();
        wrap.Children.Add(_search);
        var glyph = new TextBlock { Text = "\uE721", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 16, Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        glyph.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); wrap.Children.Add(glyph);
        _searchHint = Label(_strings["Search"], 13, true); _searchHint.IsHitTestVisible = false; _searchHint.VerticalAlignment = VerticalAlignment.Center; _searchHint.Margin = new Thickness(42, 0, 0, 0);
        _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; wrap.Children.Add(_searchHint);
        var frame = new Border { Child = wrap, CornerRadius = new CornerRadius(16), ClipToBounds = true };
        frame.SetResourceReference(Border.BackgroundProperty, "ControlBrush"); return frame;
    }

    private Button NavItem(string key, string page, string glyph)
    {
        var button = new Button { MinHeight = 42, Margin = new Thickness(0, 3, 0, 3), Padding = new Thickness(14, 8, 12, 8), HorizontalContentAlignment = HorizontalAlignment.Left };
        button.BorderThickness = new Thickness(0); button.Background = Brushes.Transparent;
        button.SetResourceReference(Control.ForegroundProperty, _page == page ? "AccentBrush" : "TextBrush");
        if (_page == page) button.SetResourceReference(Control.BackgroundProperty, "SelectionWashBrush");
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 17, Width = 24, VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new TextBlock { Text = _strings[key], FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
        button.Content = content; AutomationProperties.SetName(button, _strings[key]);
        button.Click += (_, _) =>
        {
            _selectedProduct = null;
            if (page == "updates") { _page = page; Build(); _ = CheckUpdatesAsync(); }
            else Navigate(page);
        };
        return button;
    }

    private void RenderRightRail()
    {
        _rightRail.Children.Clear();
        var products = VisibleProducts();
        var trending = new StackPanel();
        foreach (var product in products.Take(4)) trending.Children.Add(TrendingRow(product));
        _rightRail.Children.Add(SectionHeader("TrendingApps", () => Navigate("apps")));
        _rightRail.Children.Add(Surface(trending, 22, 14));

        _rightRail.Children.Add(SectionHeader("Collections", () => Navigate("collections")));
        var collections = new StackPanel();
        collections.Children.Add(CollectionTile("CollectionAll", "CollectionAllDetail", "\uE71D", products.Count.ToString(), "apps"));
        collections.Children.Add(CollectionTile("CollectionInstalled", "CollectionInstalledDetail", "\uE8F1", InstalledProducts().Count.ToString(), "library"));
        if (_demoEnabled) collections.Children.Add(CollectionTile("CollectionDemo", "CollectionDemoDetail", "\uE8B7", "KZN Test", "demo"));
        _rightRail.Children.Add(Surface(collections, 22, 12));
    }

    private UIElement SectionHeader(string key, Action seeAll)
    {
        var row = new Grid { Margin = new Thickness(2, 0, 2, 9) }; row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = Label(_strings[key], 17); title.FontWeight = FontWeights.SemiBold; row.Children.Add(title);
        var all = new Button { Content = _strings["AllApps"], FontSize = 11, Padding = new Thickness(5, 2, 5, 2), MinHeight = 24, MinWidth = 40, Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
        all.SetResourceReference(Control.ForegroundProperty, "AccentBrush"); all.Click += (_, _) => seeAll(); Grid.SetColumn(all, 1); row.Children.Add(all);
        return row;
    }

    private UIElement TrendingRow(StoreProduct product)
    {
        var root = new StackPanel { Margin = new Thickness(0, 2, 0, 8) };
        var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) }); row.ColumnDefinitions.Add(new ColumnDefinition());
        row.Children.Add(AppIcon(product, 42));
        var info = new StackPanel { Margin = new Thickness(9, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(info, 1); row.Children.Add(info);
        var name = Label(product.Name, 13); name.FontWeight = FontWeights.SemiBold; info.Children.Add(name);
        var description = Label(product.Describe(_strings.Language), 10, true); description.MaxHeight = 28; description.TextTrimming = TextTrimming.CharacterEllipsis; info.Children.Add(description);
        var stars = Label("☆ " + Stars(product) + " · " + _strings[IsDemo(product) ? "DemoStars" : "Stars"], 10); stars.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush"); info.Children.Add(stars);
        root.Children.Add(row);
        var action = CompactAction(product); action.HorizontalAlignment = HorizontalAlignment.Right; action.Margin = new Thickness(0, 7, 0, 1); action.MinWidth = 82; action.MinHeight = 32; root.Children.Add(action);
        if (product != VisibleProducts().Take(4).LastOrDefault())
        { var line = new Border { Height = 1, Margin = new Thickness(0, 2, 0, 8) }; line.SetResourceReference(Border.BackgroundProperty, "DividerBrush"); root.Children.Add(line); }
        return root;
    }

    private Button CompactAction(StoreProduct product)
    {
        if (IsDemo(product))
        {
            var key = _demoInstalled ? "DemoOpen" : "Install";
            var demo = Pill(key, () => { _selectedProduct = product; Build(); }, true); ButtonIcon(demo, _strings[key], _demoInstalled ? "\uE768" : "\uE896"); return demo;
        }
        var button = ProductAction(product); button.HorizontalContentAlignment = HorizontalAlignment.Center;
        if (button.Content as string == _strings["Open"]) ButtonIcon(button, _strings["Open"], "\uE768");
        else if (button.Content as string == _strings["Install"] || button.Content as string == _strings["Update"]) ButtonIcon(button, button.Content.ToString()!, "\uE896");
        return button;
    }

    private Button CollectionTile(string titleKey, string detailKey, string glyph, string value, string page)
    {
        var button = new Button { HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 7), MinHeight = 58, BorderThickness = new Thickness(0) };
        button.SetResourceReference(Control.BackgroundProperty, "ControlBrush"); button.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) }); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var symbol = new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        symbol.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        var icon = new Border { Width = 32, Height = 32, CornerRadius = new CornerRadius(9), Child = symbol };
        icon.SetResourceReference(Border.BackgroundProperty, "SelectionWashBrush"); grid.Children.Add(icon);
        var copy = new StackPanel { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; copy.Children.Add(Label(_strings[titleKey], 12)); copy.Children.Add(Label(_strings[detailKey], 10, true)); Grid.SetColumn(copy, 1); grid.Children.Add(copy);
        var count = Label(value, 10, true); count.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(count, 2); grid.Children.Add(count);
        button.Content = grid; button.Click += (_, _) =>
        {
            if (page == "demo") { _search.Text = "KZN Test"; Navigate("apps"); }
            else Navigate(page);
        };
        return button;
    }

    private List<StoreProduct> VisibleProducts()
        => (_demoEnabled ? _catalog.Products.Concat(new[] { _demoProduct }) : _catalog.Products)
            .Where(p => (p.Name + " " + p.Describe(_strings.Language)).Contains(_search.Text, StringComparison.CurrentCultureIgnoreCase)).ToList();

    private List<StoreProduct> InstalledProducts()
        => VisibleProducts().Where(p => IsDemo(p) ? _demoInstalled : Installed(p) is not null).ToList();

    private void AddPageHeading(string title, string subtitle)
    {
        var heading = Label(title, 25); heading.FontWeight = FontWeights.Bold; heading.Margin = new Thickness(2, 3, 0, 2); _products.Children.Add(heading);
        var detail = Label(subtitle, 13, true); detail.Margin = new Thickness(3, 0, 0, 16); _products.Children.Add(detail);
    }

    private void RenderFeatureCategories()
    {
        var heading = Label(_strings["Categories"], 17); heading.FontWeight = FontWeights.SemiBold; heading.Margin = new Thickness(2, 3, 0, 9); _products.Children.Add(heading);
        var items = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 18) };
        foreach (var (key, glyph, color) in new[]
        {
            ("MusicFeature", "\uE189", "#E9569B"), ("FocusFeature", "\uE823", "#22A886"),
            ("TasksFeature", "\uE73A", "#1677FF"), ("GamingFeature", "\uE7FC", "#8B5CF6"), ("PluginsFeature", "\uE943", "#F08A4B")
        })
        {
            var tile = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            tile.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 15, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center });
            var caption = Label(_strings[key], 11); caption.VerticalAlignment = VerticalAlignment.Center; tile.Children.Add(caption);
            var card = Surface(tile, 16, 10); card.Margin = new Thickness(3); items.Children.Add(card);
        }
        _products.Children.Add(items);
    }

    private void RenderFeaturedCards(List<StoreProduct> products)
    {
        var heading = Label(_strings["FeaturedApps"], 18); heading.FontWeight = FontWeights.SemiBold; heading.Margin = new Thickness(2, 2, 0, 9); _products.Children.Add(heading);
        var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 18) };
        foreach (var product in products)
        {
            var cardContent = new StackPanel();
            var top = new Grid(); top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) }); top.ColumnDefinitions.Add(new ColumnDefinition());
            top.Children.Add(AppIcon(product, 54));
            var details = new StackPanel { Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(details, 1); top.Children.Add(details);
            var name = Label(product.Name, 14); name.FontWeight = FontWeights.SemiBold; details.Children.Add(name);
            details.Children.Add(Label(product.Describe(_strings.Language), 11, true));
            if (IsDemo(product)) details.Children.Add(Label(_strings["DemoBadge"], 10, true));
            cardContent.Children.Add(top);
            if (IsDemo(product) || GitHubCommunityClient.ValidRepository(product.Repository)) cardContent.Children.Add(CommunityButton(product));
            var actions = ProductActions(product); actions.Orientation = Orientation.Horizontal; actions.MinWidth = 0; actions.MaxWidth = double.PositiveInfinity;
            actions.HorizontalAlignment = HorizontalAlignment.Left; actions.Margin = new Thickness(0, 7, 0, 0); cardContent.Children.Add(actions);
            var card = Surface(cardContent, 21, 12); card.Margin = new Thickness(4); grid.Children.Add(card);
        }
        _products.Children.Add(grid);
    }

    private void RenderRows(List<StoreProduct> products)
    {
        var surfaceItems = new StackPanel(); _products.Children.Add(Surface(surfaceItems, 22, 14));
        foreach (var (product, index) in products.Select((item, i) => (item, i)))
        {
            var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) }); row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(AppIcon(product, 54));
            var copy = new StackPanel { Margin = new Thickness(7, 0, 0, 0) }; Grid.SetColumn(copy, 1); row.Children.Add(copy);
            var name = Pill("ViewApp", () => { _selectedProduct = product; Build(); }); name.Content = product.Name; name.FontSize = 16; name.Padding = new Thickness(0); name.MinHeight = 22; name.Background = Brushes.Transparent; name.SetResourceReference(Control.ForegroundProperty, "TextBrush"); copy.Children.Add(name);
            copy.Children.Add(Label(product.Describe(_strings.Language), 12, true));
            if (IsDemo(product)) copy.Children.Add(Label(_strings["DemoBadge"], 10, true));
            if (IsDemo(product) || GitHubCommunityClient.ValidRepository(product.Repository)) copy.Children.Add(CommunityButton(product));
            var actions = ProductActions(product); actions.Orientation = Orientation.Horizontal; actions.MinWidth = 0; actions.MaxWidth = double.PositiveInfinity;
            actions.HorizontalAlignment = HorizontalAlignment.Left; actions.Margin = new Thickness(0, 6, 0, 0); copy.Children.Add(actions);
            surfaceItems.Children.Add(row);
            if (index < products.Count - 1) { var line = new Border { Height = 1, Margin = new Thickness(62, 6, 0, 6) }; line.SetResourceReference(Border.BackgroundProperty, "DividerBrush"); surfaceItems.Children.Add(line); }
        }
    }

    private void RenderCollections()
    {
        AddPageHeading(_strings["Collections"], _strings["CollectionAllDetail"]);
        var cards = new UniformGrid { Columns = 2 };
        cards.Children.Add(CollectionTile("CollectionAll", "CollectionAllDetail", "\uE71D", VisibleProducts().Count.ToString(), "apps"));
        cards.Children.Add(CollectionTile("CollectionInstalled", "CollectionInstalledDetail", "\uE8F1", InstalledProducts().Count.ToString(), "library"));
        if (_demoEnabled) cards.Children.Add(CollectionTile("CollectionDemo", "CollectionDemoDetail", "\uE8B7", "KZN Test", "demo"));
        _products.Children.Add(Surface(cards, 22, 14));
    }

    private void RenderUpdates()
    {
        AddPageHeading(_strings["UpdatesNav"], _strings["UpdateDescription"]);
        var content = new StackPanel();
        content.Children.Add(AppIcon(_catalog.Products.FirstOrDefault(p => p.Id == "kzn-notch") ?? StoreCatalog.Default.Products[0], 64));
        var appName = Label("Kzn Notch", 17); appName.FontWeight = FontWeights.SemiBold; appName.Margin = new Thickness(0, 12, 0, 5); content.Children.Add(appName);
        content.Children.Add(Label(_status.Text, 13, true));
        var check = Pill(_checkingUpdates ? "Checking" : "CheckUpdates", () => _ = CheckUpdatesAsync(), true); check.HorizontalAlignment = HorizontalAlignment.Left; check.Margin = new Thickness(0, 14, 0, 0); content.Children.Add(check);
        _products.Children.Add(Surface(content, 22, 18));
    }

    private void RenderSettings()
    {
        AddPageHeading(_strings["StoreSettings"], _strings["StoreSettingsDetail"]);
        var settings = new StackPanel();
        var language = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 12) };
        language.Children.Add(Label(_strings["Language"], 14));
        var select = new ComboBox { Width = 130, MinHeight = 34, Padding = new Thickness(8), ItemsSource = new[] { "Deutsch", "English" }, SelectedIndex = _strings.Language == "de" ? 0 : 1, Margin = new Thickness(20, 0, 0, 0) };
        select.SelectionChanged += (_, _) => { _strings.Language = select.SelectedIndex == 0 ? "de" : "en"; SavePreferences(); Build(); }; language.Children.Add(select); settings.Children.Add(language);
        var catalogTitle = Label(_strings["Catalog"], 14); catalogTitle.FontWeight = FontWeights.SemiBold; catalogTitle.Margin = new Thickness(0, 8, 0, 4); settings.Children.Add(catalogTitle);
        settings.Children.Add(Label(_strings["CatalogNote"], 12, true));
        var import = Pill("Import", ImportCatalog); import.HorizontalAlignment = HorizontalAlignment.Left; import.Margin = new Thickness(0, 8, 0, 16); settings.Children.Add(import);
        var updateTitle = Label(_strings["UpdatesNav"], 14); updateTitle.FontWeight = FontWeights.SemiBold; settings.Children.Add(updateTitle);
        settings.Children.Add(Label(_strings["UpdateDescription"], 12, true));
        var check = Pill("CheckUpdates", () => _ = CheckUpdatesAsync()); check.HorizontalAlignment = HorizontalAlignment.Left; check.Margin = new Thickness(0, 8, 0, 14); settings.Children.Add(check);
        if (_demoEnabled)
        {
            var hide = Pill("HideDemo", () => { _demoCancellation?.Cancel(); _demoEnabled = false; _selectedProduct = null; SavePreferences(); Build(); }); hide.HorizontalAlignment = HorizontalAlignment.Left; settings.Children.Add(hide);
        }
        _products.Children.Add(Surface(settings, 22, 18));
    }
}
