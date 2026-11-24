using Clayzor.Lib.Web.Controls.Components.Grid;

namespace Clayzor.ThemeStudio.Components;

internal sealed class StudioReviewDataLoader(IReadOnlyDictionary<string, IReadOnlyList<object?>> values) : IClayGridDataLoader
{
    public Task OnQueryChangedAsync(ClayDataQuery query) => Task.CompletedTask;
    public Task ExcelExportAsync(ExcelExportRequest request) => Task.CompletedTask;
    public Task<string> BuildPrintHtmlAsync(IReadOnlyList<ClayColumnMeta> columns, string title,
        string? filterDescription, string? groupDescription) => Task.FromResult(string.Empty);
    public Task<string> BuildPrintHtmlForCurrentPageAsync(IReadOnlyList<ClayColumnMeta> columns, string title,
        string? filterDescription, string? groupDescription) => Task.FromResult(string.Empty);
    public bool IsLevelFullyExpanded(int depth) => false;
    public Task ToggleLevelExpandedAsync(int depth) => Task.CompletedTask;
    public Task<Dictionary<string, HashSet<int>>> LoadGroupChildIdsAsync(IReadOnlyList<string> groupFullKeys, ClayDataQuery query) =>
        Task.FromResult(new Dictionary<string, HashSet<int>>());

    public Task<DistinctValuesResult> LoadDistinctValuesAsync(string sqlName, ClayDataQuery query, int limit = 100)
    {
        var items = values.GetValueOrDefault(sqlName, []);
        return Task.FromResult(new DistinctValuesResult { Values = items, TotalDistinct = items.Count });
    }

    public Task<string> BuildPrintHtmlForSelectedAsync(IReadOnlyList<ClayColumnMeta> columns, string title,
        IReadOnlyList<int> selectedIds, string? filterDescription, string? groupDescription) => Task.FromResult(string.Empty);
}
