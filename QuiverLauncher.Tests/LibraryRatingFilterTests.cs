using System.Net;
using System.Text.Json;
using FluentAssertions;
using QuiverLauncher.Core.Models;
using QuiverLauncher.Models;
using QuiverLauncher.Services;
using QuiverLauncher.ViewModels;

namespace QuiverLauncher.Tests;

/// <summary>The Library's Rating filter narrows the list by what players report on quiverlauncher.com.</summary>
public class LibraryRatingFilterTests
{
    [Theory]
    [InlineData(0, 0, 0, "unrated")]
    [InlineData(3, 0, 0, "runs")]
    [InlineData(1, 2, 0, "issues")]
    [InlineData(0, 1, 2, "broken")]
    // A tie takes the more careful verdict, as the catalog shows it.
    [InlineData(2, 2, 0, "issues")]
    public void Rating_key_follows_the_catalog_verdict(int runs, int issues, int broken, string expected) =>
        BrowseText.RatingKey(new QuiverCatalogApp { Recommended = runs, ReportIssues = issues, ReportBroken = broken })
            .Should().Be(expected);

    [Fact]
    public void An_app_outside_the_catalog_is_not_rated_yet() => BrowseText.RatingKey(null).Should().Be("unrated");

    [Fact]
    public async Task Filter_shows_apps_with_any_chosen_rating_once_the_listing_arrives()
    {
        using var launcher = new Launcher();
        var token = TestContext.Current.CancellationToken;
        var site = launcher.Network.Site;
        site["/release-status?limit=100"] = Page([Status("good"), Status("rough"), Status("bad"), Status("fresh")]);
        site["/apps?limit=100&sort=added"] = Page([Listed("good", 3, 0, 0), Listed("rough", 1, 2, 0), Listed("bad", 0, 0, 2), Listed("fresh", 0, 0, 0)]);
        GameInfo App(string name) => new() { Name = name, Repository = $"owner/{name}", FolderName = name, Status = GameStatus.Installed };
        var apps = new[] { App("good"), App("rough"), App("bad"), App("fresh"), App("manual") };
        var settings = launcher.Store.Current;
        launcher.Manager.SetCatalogAppsAndFilter([.. apps], settings);

        // Before the listing loads, nothing has a rating yet.
        settings.ActiveRatingFilters = ["runs", "issues"];
        launcher.Manager.ApplyTagDisplayFilter(settings);
        launcher.Manager.Games.Should().BeEmpty();

        await launcher.Manager.CatalogReleases.RefreshAsync(apps, token);
        await launcher.Manager.SyncCatalogDetailsAsync(apps, token);
        Names().Should().BeEquivalentTo(["good", "rough"]);

        // An app added by hand has no catalog entry, so it counts as not rated yet.
        settings.ActiveRatingFilters = ["unrated"];
        launcher.Manager.ApplyTagDisplayFilter(settings);
        Names().Should().BeEquivalentTo(["fresh", "manual"]);

        settings.ActiveRatingFilters = ["broken"];
        launcher.Manager.ApplyTagDisplayFilter(settings);
        Names().Should().Equal("bad");

        settings.ActiveRatingFilters.Clear();
        launcher.Manager.ApplyTagDisplayFilter(settings);
        launcher.Manager.Games.Should().HaveCount(5);

        IEnumerable<string> Names() => launcher.Manager.Games.Select(g => g.Name);
    }

    [Fact]
    public void Toggling_ratings_and_the_filters_section_saves_and_combines_ratings()
    {
        using var launcher = new Launcher();
        using var library = new LibraryViewModel(launcher.Manager, new SettingsViewModel(launcher.Store));
        var filters = new LibraryFiltersViewModel(launcher.Manager, library);
        filters.ToggleRating("runs");
        filters.ToggleRating("unrated");
        filters.IsRatingActive("runs").Should().BeTrue();
        filters.ToggleRating("runs");
        filters.IsRatingActive("runs").Should().BeFalse();
        filters.ToggleFiltersSection();

        var saved = new FileSettingsStore(launcher.SettingsPath).Current;
        saved.ActiveRatingFilters.Should().Equal("unrated");
        saved.FiltersSectionCollapsed.Should().BeTrue();
    }

    private static string Page(object[] items) => JsonSerializer.Serialize(new { items, nextCursor = (string?)null, isDone = true });

    private static object Status(string slug) => new
    {
        id = "id-" + slug, slug, provider = "github", repository = $"owner/{slug}",
        verified = new { version = "v1.0", rolling = false }, latestUpstream = new { version = "v1.0" },
    };

    private static object Listed(string slug, int runs, int issues, int broken) => new
    {
        slug, name = slug, recommended = runs, reportIssues = issues, reportBroken = broken,
        launcher = new { folderName = slug },
    };

    /// <summary>quiverlauncher.com answers by path and query after /api/v1; anything else is a 404.</summary>
    private sealed class Network : HttpMessageHandler
    {
        public Dictionary<string, string> Site { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            string? body = null;
            if (uri.AbsolutePath.StartsWith("/api/v1/"))
                Site.TryGetValue(uri.PathAndQuery["/api/v1".Length..], out body);
            return Task.FromResult(body == null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    /// <summary>A launcher with its own data folder whose only network is <see cref="Network"/>.</summary>
    private sealed class Launcher : IDisposable
    {
        private readonly string? _previousRoot = QuiverLauncherPaths.OverrideUserDataRoot;
        private readonly ISettingsStore _previousSettings = SettingsStoreProvider.Default;
        private readonly string _root = Path.Combine(Path.GetTempPath(), "quiver-rating-filter", Guid.NewGuid().ToString("N"));
        public Network Network { get; } = new();
        public string SettingsPath => Path.Combine(_root, "settings.json");
        public FileSettingsStore Store { get; }
        public GameManager Manager { get; }

        public Launcher()
        {
            Directory.CreateDirectory(_root);
            QuiverLauncherPaths.OverrideUserDataRoot = _root;
            Store = new FileSettingsStore(SettingsPath);
            Store.Current.FirstStartup = false;
            Store.Current.AppsPath = Path.Combine(_root, "Apps");
            Store.Save(Store.Current);
            SettingsStoreProvider.Default = Store;
            Manager = new GameManager(Store, new HttpClient(Network), new AppCatalogService(dataDirectory: _root));
        }

        public void Dispose()
        {
            Manager.Dispose();
            SettingsStoreProvider.Default = _previousSettings;
            QuiverLauncherPaths.OverrideUserDataRoot = _previousRoot;
            TestFixtures.CleanupDirectory(_root);
        }
    }
}
