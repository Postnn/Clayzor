using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid;

public partial class KescoGrid<TEntity> where TEntity : class
{
    /// <summary>Активные фильтры по колонкам: SqlName → ColumnFilter.</summary>
    private Dictionary<string, ColumnFilter> _activeFilters = [];

    /// <summary>Флаг раскрытия панели фильтрации.</summary>
    private bool _filterTrayExpanded = false;

    /// <summary>
    /// Включает/выключает панель фильтрации.
    /// При выключении сбрасывает все активные фильтры и перезагружает данные.
    /// </summary>
    private async Task ToggleFilterTray()
    {
        _filterTrayExpanded = !_filterTrayExpanded;
        TrayStateChanged?.Invoke();
        if (!_filterTrayExpanded)
        {
            _activeFilters.Clear();
            _pageNumber = 1;
            await NotifyQueryChanged();
        }
        else
        {
            StateHasChanged();
        }
    }

    private void OnFilterTrayDragOver(DragEventArgs e)
    {
        e.DataTransfer.DropEffect = "move";
    }

    private async Task OnFilterTrayDrop(DragEventArgs e)
    {
        var draggedSqlName = KescoDragState.DraggedColumn;
        KescoDragState.DraggedColumn = null;

        if (string.IsNullOrEmpty(draggedSqlName))
            return;
        if (!_columnBySqlName.TryGetValue(draggedSqlName, out var cm) || !cm.Filterable)
            return;

        await OpenFilterDialog(draggedSqlName, cm.DisplayName);
    }

    /// <summary>
    /// Открывает диалог настройки фильтра для указанной колонки.
    /// При подтверждении сохраняет фильтр и перезагружает данные.
    /// </summary>
    private async Task OpenFilterDialog(string sqlName, string displayName)
    {
        var colType = FilterColumnTypes.TryGetValue(sqlName, out var t) ? t : ColumnType.Text;
        _activeFilters.TryGetValue(sqlName, out var existing);

        var parameters = new DialogParameters<KescoColumnFilterDialog>
        {
            { x => x.ColumnDisplayName, displayName },
            { x => x.ColumnSqlName,     sqlName },
            { x => x.ColumnType,        colType },
            { x => x.ExistingFilter,    existing },
            { x => x.LookupOptions,     FilterLookupOptions?.GetValueOrDefault(sqlName) },
        };
        var options = new DialogOptionsEx
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            DragMode = MudDialogDragMode.Simple,
        };
        var dialog = await DialogService.ShowExAsync<KescoColumnFilterDialog>(
            $"Фильтр: {displayName}", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is ColumnFilter colFilter)
        {
            _activeFilters[sqlName] = colFilter;
            _pageNumber = 1;
            await NotifyQueryChanged();
        }
    }

    private async Task RemoveFilter(string sqlName)
    {
        _activeFilters.Remove(sqlName);
        _pageNumber = 1;
        await NotifyQueryChanged();
    }

    /// <summary>
    /// Строит читаемое описание активных фильтров для экспорта/печати.
    /// </summary>
    private string? BuildFilterDescription()
    {
        if (_activeFilters.Count == 0) return null;
        var parts = new List<string>();
        foreach (var kv in _activeFilters)
        {
            var sqlName     = kv.Key;
            var filter      = kv.Value;
            var displayName = _columnBySqlName.TryGetValue(sqlName, out var m) ? m.DisplayName : sqlName;
            parts.Add(KescoColumnFilterDialog.GetFilterDescription(filter, displayName));
        }
        return $"Фильтр: {string.Join("; ", parts)}";
    }
}
