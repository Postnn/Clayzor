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

    /// <summary>Есть ли в дереве хотя бы один узел составного фильтра (не лист ColumnDialog).</summary>
    private bool HasComposite =>
        _filterRoot.Nodes.Any(n => n is not ColumnFilter cf || cf.Source != KescoFilterSource.ColumnDialog);

    /// <summary>
    /// Включает/выключает панель фильтрации. Настроенный фильтр при сворачивании
    /// панели сохраняется — сброс выполняется только явной кнопкой очистки.
    /// </summary>
    private Task ToggleFilterTray()
    {
        _filterTrayExpanded = !_filterTrayExpanded;
        TrayStateChanged?.Invoke();
        StateHasChanged();
        return Task.CompletedTask;
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

        if (HasComposite)
            await OpenCompositeFilterDialog(BuildTreeWithColumnAnded(draggedSqlName));
        else
            await OpenFilterDialog(draggedSqlName, cm.DisplayName);
    }

    /// <summary>
    /// Строит копию дерева фильтра с новым условием по колонке, приклеенным через И
    /// на верхнем уровне (сужает выборку). Если корень был <c>ИЛИ</c> — оборачивает
    /// старое дерево и новый лист в новый корень <c>И</c>, чтобы не расширить фильтр.
    /// </summary>
    private KescoFilterGroupNode BuildTreeWithColumnAnded(string sqlName)
    {
        var clone = (KescoFilterGroupNode)_filterRoot.Clone();
        var meta  = _columnBySqlName[sqlName];
        var leaf  = new ColumnFilter
        {
            Column   = sqlName,
            Operator = meta.Type.DefaultOperator,
            Source   = KescoFilterSource.CompositeDialog,
            IsNew    = true,
        };

        if (clone.Nodes.Count == 0 || clone.Logic == LogicalOperator.And)
        {
            clone.Logic = LogicalOperator.And;
            clone.Nodes.Add(leaf);
            return clone;
        }

        return new KescoFilterGroupNode
        {
            Logic = LogicalOperator.And,
            Nodes = { clone, leaf },
        };
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

    /// <summary>Полностью очищает дерево фильтра и перезагружает данные.</summary>
    private async Task ClearAllFilters()
    {
        _filterRoot = new();
        _pageNumber = 1;
        await NotifyQueryChanged();
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
    /// Строит читаемое описание всего дерева фильтра для экспорта/печати.
    /// </summary>
    private string? BuildFilterDescription()
        => KescoFilterDescriptionBuilder.BuildText(
            _filterRoot,
            sqlName => _columnBySqlName.TryGetValue(sqlName, out var m) ? m.DisplayName : sqlName);

    /// <summary>
    /// Строит список кликабельных сегментов из всего дерева фильтра для панели.
    /// </summary>
    private IReadOnlyList<FilterSegment> BuildFilterSegments()
        => KescoFilterDescriptionBuilder.BuildSegments(
            _filterRoot,
            sqlName => _columnBySqlName.TryGetValue(sqlName, out var m) ? m.DisplayName : sqlName);

    /// <inheritdoc cref="IKescoGrid.ActiveCompositeFilter"/>
    public KescoFilterGroupNode? ActiveCompositeFilter => _filterRoot;

    /// <inheritdoc cref="IKescoGrid.OpenCompositeFilterDialog"/>
    public async Task OpenCompositeFilterDialog() => await OpenCompositeFilterDialog(null);

    /// <summary>
    /// Открывает диалог настраиваемого фильтра. Если передан <paramref name="seedRoot"/>
    /// (например, кандидат-дерево с добавленным перетаскиванием колонки условием) —
    /// диалог открывается на нём вместо действующего <see cref="_filterRoot"/>;
    /// отмена диалога не меняет действующий фильтр.
    /// </summary>
    private async Task OpenCompositeFilterDialog(KescoFilterGroupNode? seedRoot)
    {
        // Фильтруемые колонки — только зарегистрированные Filterable в текущем порядке
        var filterableCols = ((IKescoGrid)this).GetVisibleColumns()
            .Where(c => c.Filterable)
            .ToList();

        // Если нет ни одной Filterable колонки — берём все зарегистрированные
        if (filterableCols.Count == 0)
            filterableCols = _columnBySqlName.Values.Where(c => c.Filterable).ToList();

        var parameters = new DialogParameters<KescoFilterDialog>
        {
            { x => x.Root,         seedRoot ?? _filterRoot },
            { x => x.Columns,      (IReadOnlyList<KescoColumnMeta>)filterableCols },
            { x => x.LookupOptions, FilterLookupOptions },
        };
        var options = new DialogOptionsEx
        {
            MaxWidth  = MaxWidth.Small,
            FullWidth = false,
            CloseOnEscapeKey = true,
            DragMode  = MudDialogDragMode.Simple,
        };
        var dialog = await DialogService.ShowExAsync<KescoFilterDialog>(
            "Настраиваемый фильтр", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is KescoFilterGroupNode newRoot)
        {
            _filterRoot = newRoot;
            _pageNumber = 1;
            await NotifyQueryChanged();
        }
    }
}
