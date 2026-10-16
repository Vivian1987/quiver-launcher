using System.Collections.ObjectModel;
using QuiverLauncher.Services;

namespace QuiverLauncher.ViewModels;

public sealed class LibraryFiltersViewModel(GameManager manager, LibraryViewModel library)
{
    public AppSettings Settings => library.Settings.Current;
    public ObservableCollection<TagDisplayFilterListItem> TagDisplayFilters => library.TagDisplayFilters;
    public void Save() => library.Settings.SaveCurrent();
    public void SetSort(string sort)
    {
        library.SortBy = sort;
        Settings.SortBy = sort;
        library.ApplySorting();
    }
    public void SetScope(AppListScope scope)
    {
        manager.SetListScope(scope, Settings);
        library.Settings.Refresh();
        library.RefreshContinue();
        library.ApplySorting();
    }
    public void Toggle(string filterId)
    {
        Settings.ActiveTagDisplayFilterId = string.Equals(Settings.ActiveTagDisplayFilterId, filterId, StringComparison.OrdinalIgnoreCase) ? null : filterId;
        Save();
        manager.ApplyTagDisplayFilter(Settings);
        library.RefreshFilters();
        library.RefreshContinue();
        library.ApplySorting();
    }
    /// <summary>The Rating filter's choices, in the order the App Catalog ranks them (see <see cref="BrowseText.RatingKey"/>).</summary>
    public static IReadOnlyList<(string Key, string Label)> Ratings { get; } =
        [("runs", "Runs well"), ("issues", "Has issues"), ("broken", "Doesn't run"), ("unrated", "Not rated yet")];
    public bool IsRatingActive(string rating) => Settings.ActiveRatingFilters.Contains(rating, StringComparer.OrdinalIgnoreCase);
    /// <summary>Turns one rating on or off; several can be on, and the library shows apps with any of them.</summary>
    public void ToggleRating(string rating)
    {
        Settings.EnsureInitialized();
        if (Settings.ActiveRatingFilters.RemoveAll(r => string.Equals(r, rating, StringComparison.OrdinalIgnoreCase)) == 0)
            Settings.ActiveRatingFilters.Add(rating);
        Save();
        manager.ApplyTagDisplayFilter(Settings);
        library.RefreshContinue();
        library.ApplySorting();
    }
    public void ToggleFiltersSection()
    {
        Settings.FiltersSectionCollapsed = !Settings.FiltersSectionCollapsed;
        Save();
    }
    public void Delete(string filterId)
    {
        var filter = Settings.TagDisplayFilters.FirstOrDefault(f => f.Id == filterId);
        if (filter == null) return;
        Settings.TagDisplayFilters.Remove(filter);
        if (string.Equals(Settings.ActiveTagDisplayFilterId, filterId, StringComparison.OrdinalIgnoreCase)) Settings.ActiveTagDisplayFilterId = null;
        Save();
        library.RefreshFilters();
        manager.ApplyTagDisplayFilter(Settings);
        library.ApplySorting();
    }
    public int IndexOf(string? filterId) => filterId == null ? -1 : Settings.TagDisplayFilters.FindIndex(f => string.Equals(f.Id, filterId, StringComparison.OrdinalIgnoreCase));
    public void PreviewMove(string filterId, int targetIndex)
    {
        Settings.EnsureInitialized();
        var fromIndex = IndexOf(filterId);
        if (fromIndex < 0 || fromIndex == targetIndex) return;
        TagDisplayFilterReorder.Move(Settings.TagDisplayFilters, fromIndex, targetIndex);
        var uiIndex = TagDisplayFilters.Select((item, index) => (item, index)).FirstOrDefault(x => string.Equals(x.item.Id, filterId, StringComparison.OrdinalIgnoreCase), (null!, -1)).index;
        if (uiIndex >= 0 && targetIndex >= 0 && targetIndex < TagDisplayFilters.Count) TagDisplayFilters.Move(uiIndex, targetIndex);
    }
}
