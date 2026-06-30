using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid;

public partial class KescoGrid<TEntity> where TEntity : class
{
    /// <summary>
    /// Корень дерева фильтра — единственный источник истины.
    /// Объединяет колоночные фильтры (<c>Source=ColumnDialog</c>)
    /// и условия составного фильтра (<c>Source=CompositeDialog</c>).
    /// </summary>
    private KescoFilterGroupNode _filterRoot = new();

    /// <summary>Флаг раскрытия панели фильтрации.</summary>
    private bool _filterTrayExpanded = false;

    /// <summary>
    /// Вспомогательный доступ к листьям дерева с <c>Source=ColumnDialog</c>
    /// для отображения чипов в панели фильтрации.
    /// </summary>
    private IEnumerable<ColumnFilter> ColumnDialogLeaves =>
        _filterRoot.Nodes.OfType<ColumnFilter>()
                         .Where(f => f.Source == KescoFilterSource.ColumnDialog);

    /// <summary>
    /// Включает/выключает панель фильтрации.
    /// При выключении сбрасывает всё дерево фильтра и перезагружает данные.
    /// </summary>
    private async Task ToggleFilterTray()
    {
        _filterTrayExpanded = !_filterTrayExpanded;
        TrayStateChanged?.Invoke();
        if (!_filterTrayExpanded)
        {
            _filterRoot = new();
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
    /// Результат вставляется в <see cref="_filterRoot"/> как лист с <c>Source=ColumnDialog</c>.
    /// </summary>
    private async Task OpenFilterDialog(string sqlName, string displayName)
    {
        var colType  = FilterColumnTypes.TryGetValue(sqlName, out var t) ? t : ColumnType.Text;
        // Ищем существующий лист ColumnDialog для этой колонки
        var existing = _filterRoot.Nodes
            .OfType<ColumnFilter>()
            .FirstOrDefault(f => f.Column == sqlName && f.Source == KescoFilterSource.ColumnDialog);

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
            colFilter.Source = KescoFilterSource.ColumnDialog;
            // Заменяем существующий или добавляем новый лист
            if (existing is not null)
            {
                var idx = _filterRoot.Nodes.IndexOf(existing);
                _filterRoot.Nodes[idx] = colFilter;
            }
            else
            {
                _filterRoot.Nodes.Add(colFilter);
            }
            _pageNumber = 1;
            await NotifyQueryChanged();
        }
    }

    /// <summary>Удаляет листовой фильтр колонки из дерева.</summary>
    private async Task RemoveFilter(string sqlName)
    {
        var leaf = _filterRoot.Nodes
            .OfType<ColumnFilter>()
            .FirstOrDefault(f => f.Column == sqlName && f.Source == KescoFilterSource.ColumnDialog);
        if (leaf is not null)
        {
            _filterRoot.Nodes.Remove(leaf);
            _pageNumber = 1;
            await NotifyQueryChanged();
        }
    }

    /// <summary>
    /// Строит читаемое описание активных фильтров для экспорта/печати.
    /// Учитывает листья с <c>Source=ColumnDialog</c> в корне дерева.
    /// </summary>
    private string? BuildFilterDescription()
    {
        var leaves = ColumnDialogLeaves.ToList();
        if (leaves.Count == 0) return null;
        var parts = leaves.Select(f =>
        {
            var dn = _columnBySqlName.TryGetValue(f.Column, out var m) ? m.DisplayName : f.Column;
            return KescoColumnFilterDialog.GetFilterDescription(f, dn);
        });
        return $"Фильтр: {string.Join("; ", parts)}";
    }

    /// <inheritdoc cref="IKescoGrid.ActiveCompositeFilter"/>
    public KescoFilterGroupNode? ActiveCompositeFilter => _filterRoot;

    /// <inheritdoc cref="IKescoGrid.OpenCompositeFilterDialog"/>
    public Task OpenCompositeFilterDialog()
    {
        // UI-реализация — задача 11
        return Task.CompletedTask;
    }
}
