using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using QuiverLauncher.Services;
using QuiverLauncher.ViewModels;
using QuiverLauncher.Views;

namespace QuiverLauncher.Tests;

public class LibraryFiltersViewTests
{
    [AvaloniaFact]
    public async Task Sidebar_sorts_saves_and_offers_one_add_filter_control()
    {
        var store = new Store();
        using var manager = new GameManager(store);
        using var library = new LibraryViewModel(manager, new SettingsViewModel(store));
        await using var session = new LauncherSession();
        var view = new LibraryFiltersView();
        var saves = 0;
        view.Configure(new LibraryFiltersViewModel(manager, library), session, (_, _) => Task.CompletedTask, () => saves++);
        var window = new Window { Content = view, Width = 260, Height = 700 };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var sort = view.FindControl<ComboBox>("SortByComboBox")!;
            var add = view.FindControl<Button>("AddDisplayFilterButton")!;

            view.SelectSort("LastPlayed");
            library.SortBy.Should().Be("LastPlayed");
            store.Current.SortBy.Should().Be("LastPlayed");
            saves.Should().Be(1);

            var controls = view.CollectNavigationControls();
            controls[0].Should().BeSameAs(sort);
            controls.Should().Contain(add);
            view.GetVisualDescendants().OfType<Button>().Should().NotContain(b => b.Content as string == "Add Filter");

            // Mobile keeps sorting in its header menu, so the drawer leaves the dropdown out.
            view.ApplyMobileLayout();
            Dispatcher.UIThread.RunJobs();
            view.CollectNavigationControls().Should().NotContain(sort);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task Rating_dropdown_ticks_ratings_and_filters_fold_away()
    {
        var store = new Store();
        using var manager = new GameManager(store);
        using var library = new LibraryViewModel(manager, new SettingsViewModel(store));
        await using var session = new LauncherSession();
        var view = new LibraryFiltersView();
        view.Configure(new LibraryFiltersViewModel(manager, library), session, (_, _) => Task.CompletedTask, () => { });
        var window = new Window { Content = view, Width = 260, Height = 900 };
        try
        {
            window.Show();
            view.RefreshSidebarFilterSelection();
            Dispatcher.UIThread.RunJobs();
            var dropdown = view.FindControl<Button>("RatingFilterButton")!;
            var summary = view.FindControl<TextBlock>("RatingFilterSummary")!;
            var items = ((MenuFlyout)dropdown.Flyout!).Items.OfType<MenuItem>().ToList();
            MenuItem Item(string rating) => items.Single(i => (string)i.Tag! == rating);
            void Tick(string rating) { Item(rating).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
            items.Select(i => i.Tag).Should().Equal("runs", "issues", "broken", "unrated");
            items.Should().OnlyContain(i => i.ToggleType == MenuItemToggleType.CheckBox && i.StaysOpenOnClick);
            summary.Text.Should().Be("Any rating");
            view.CollectNavigationControls().Should().Contain(dropdown);

            Tick("runs");
            Tick("issues");
            store.Current.ActiveRatingFilters.Should().Equal("runs", "issues");
            summary.Text.Should().Be("Runs well, Has issues");
            dropdown.Classes.Should().Contain("active");
            Item("runs").IsChecked.Should().BeTrue();
            Tick("runs");
            store.Current.ActiveRatingFilters.Should().Equal("issues");
            Item("runs").IsChecked.Should().BeFalse();
            summary.Text.Should().Be("Has issues");

            // FILTERS folds away the Rating dropdown and the tag filters under it.
            var filtersToggle = view.FindControl<Button>("FiltersSectionToggle")!;
            filtersToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            store.Current.FiltersSectionCollapsed.Should().BeTrue();
            view.FindControl<StackPanel>("FiltersSectionBody")!.IsVisible.Should().BeFalse();
            view.CollectNavigationControls().Should().Contain(filtersToggle).And.NotContain(dropdown);
            filtersToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            view.FindControl<StackPanel>("FiltersSectionBody")!.IsVisible.Should().BeTrue();
        }
        finally { window.Close(); }
    }

    private sealed class Store : ISettingsStore
    {
        public AppSettings Current { get; } = new() { FirstStartup = false, AppsPath = Path.Combine(Path.GetTempPath(), "quiver-filters-view", Guid.NewGuid().ToString("N")) };
        public AppSettings Load() => Current;
        public void Save(AppSettings settings) { }
    }
}
