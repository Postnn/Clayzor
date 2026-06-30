using Kesco.Lib.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid;

public partial class KescoGrid<TEntity> where TEntity : class
{
    private string? _searchText;
    private bool _trayExpanded = false;
    private bool _filterTrayExpanded = false;
    private List<string> _groupColumns = [];
    private int _dragSourceIndex = -1;

    /// <summary>
    /// Состояние сортировки: <c>Column</c> содержит <see cref="KescoColumnMeta.SortName"/>
    /// (реальное выражение для ORDER BY), а не SqlName колонки.
    /// </summary>
    private List<SortColumn> _sortState = [];

    private bool _selectMode;
    private bool _selectAllChecked;
    private HashSet<int> _selectedIds = [];
    private KescoDataQuery _lastQuery = new();
    private Dictionary<string, bool> _openSubGroups = [];
    private bool _isExporting;

    /// <summary>
    /// Кеш: FullKey группы → ID дочерних сущностей.
    /// Заполняется лениво при первом взаимодействии с чекбоксом группы в grouped-режиме.
    /// Сбрасывается при деактивации режима выбора и при перезагрузке данных.
    /// </summary>
    private Dictionary<string, HashSet<int>> _groupChildIds = [];

    private Dictionary<string, ColumnFilter> _activeFilters = [];
    private int _pageNumber = 1;
    private int _pageSize;
    private int _dataKey;
    private const string ServiceEditColumnKey = "__kesco_edit";
    private const string SelectColumnKey = "__kesco_select";

    /// <summary>
    /// Срабатывает при регистрации или отмене регистрации колонки.
    /// <see cref="KescoColumn{TEntity}"/> подписывается и вызывает <c>StateHasChanged</c>,
    /// чтобы отобразить <c>DisplayName</c> после того, как <see cref="KescoColumnDef"/>
    /// завершит инициализацию и зарегистрирует метаданные.
    /// </summary>
    public event Action? ColumnsChanged;

    /// <summary>
    /// Срабатывает при открытии или закрытии панели группировки или фильтрации.
    /// <see cref="KescoColumn{TEntity}"/> подписывается и вызывает <c>StateHasChanged</c>,
    /// чтобы показать или скрыть кнопку меню (⋮) в заголовке колонки.
    /// </summary>
    public event Action? TrayStateChanged;

    /// <summary>
    /// Индекс по числовому идентификатору: ColumnId → <see cref="KescoColumnMeta"/>.
    /// Используется <see cref="KescoColumn{TEntity}"/> для поиска метаданных при построении заголовка.
    /// </summary>
    private readonly Dictionary<int, KescoColumnMeta> _columnById = new();

    /// <summary>
    /// Индекс по SQL-имени: SqlName → <see cref="KescoColumnMeta"/>.
    /// Используется для группировки, фильтрации, drag-and-drop и <see cref="IsGrouped"/>.
    /// </summary>
    private readonly Dictionary<string, KescoColumnMeta> _columnBySqlName = new();

    /// <summary>Порядок колонок в гриде (список ColumnId).</summary>
    private readonly List<int> _columnOrder = [];

    /// <summary>SQL-имена колонок, скрытых пользователем через диалог настройки.</summary>
    private readonly HashSet<string> _hiddenSqlNames = [];

    /// <summary>Флаг завершения первой фазы рендеринга (сбор CellTemplate).</summary>
    private bool _columnsReady;

    /// <summary>DotNetObjectReference для передачи в JS (insert-drag заголовков).</summary>
    private DotNetObjectReference<KescoGrid<TEntity>>? _dotnetRef;

    /// <summary>ColumnId → CellTemplate для динамического рендеринга колонок.</summary>
    private readonly Dictionary<int, object> _cellTemplates = [];

    private string _gridHeight
    {
        get
        {
            var trays = (_trayExpanded ? 1 : 0) + (_filterTrayExpanded ? 1 : 0);
            return trays switch
            {
                2 => "calc(100vh - 380px)",
                1 => "calc(100vh - 330px)",
                _ => "calc(100vh - 280px)",
            };
        }
    }

    // ── Parameters ───────────────────────────────────────────────────────────────
    /// <summary>Заголовок грида.</summary>
    [Parameter] public string Title { get; set; } = "Список";

    /// <summary>DOM-идентификатор корневого элемента грида.</summary>
    [Parameter] public string Id { get; set; } = "kesco-grid";

    /// <summary>Данные для отображения.</summary>
    [Parameter] public IEnumerable<TEntity> Items { get; set; } = [];

    /// <summary>Признак загрузки — управляет индикатором грида.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Показывать кнопку «Добавить» в тулбаре.</summary>
    [Parameter] public bool ShowAddButton { get; set; } = true;

    /// <summary>Количество строк на странице по умолчанию.</summary>
    [Parameter] public int PageSize { get; set; } = 50;

    /// <summary>
    /// Колонки грида — <c>KescoColumn</c> / <c>TemplateColumn</c> / <c>PropertyColumn</c>.
    /// Передаются внутрь <c>MudDataGrid.Columns</c>.
    /// </summary>
    [Parameter] public RenderFragment? Columns { get; set; }

    /// <summary>
    /// Метаданные колонок — <see cref="KescoColumnDef"/> компоненты.
    /// Рендерятся вне грида через <c>CascadingValue</c> для регистрации метаданных.
    /// </summary>
    [Parameter] public RenderFragment? ColumnDefs { get; set; }

    /// <summary>Событие нажатия кнопки «Добавить».</summary>
    [Parameter] public EventCallback OnAdd { get; set; }

    /// <summary>
    /// Текст уведомления после успешного сохранения записи через сервисную колонку.
    /// </summary>
    [Parameter] public string EditSuccessMessage { get; set; } = "Запись обновлена";

    /// <summary>Событие изменения параметров запроса (поиск, сортировка, пагинация и т.д.).</summary>
    [Parameter] public EventCallback<KescoDataQuery> OnQueryChanged { get; set; }

    /// <summary>Общее количество записей — используется для пагинации.</summary>
    [Parameter] public int TotalCount { get; set; }

    /// <summary>
    /// Текущий номер страницы. Передаётся со страницы для синхронизации
    /// пагинатора при внешних изменениях (например, авто-переход в <c>ToggleGroup</c>).
    /// </summary>
    [Parameter] public int PageNumber { get; set; } = 1;

    /// <summary>Показывать панель пагинации.</summary>
    [Parameter] public bool ShowPagination { get; set; } = true;

    /// <summary>Разрешить перетаскивание колонок.</summary>
    [Parameter] public bool AllowColumnReorder { get; set; } = true;

    /// <summary>
    /// Тип данных для каждой фильтруемой колонки: ключ — SQL-имя, значение — <see cref="ColumnType"/>.
    /// Передаётся из <see cref="KescoGridPageBase{T}.FilterColumnTypes"/>.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, ColumnType> FilterColumnTypes { get; set; }
        = new Dictionary<string, ColumnType>();

    /// <summary>
    /// Необязательный источник вариантов для выпадающего списка значений в диалоге фильтра.
    /// Ключ — SQL-имя колонки, значение — список вариантов (<see cref="KescoFilterOption"/>).
    /// Передаётся со страницы, которая может переопределить <see cref="KescoGridPageBase{T}.FilterLookupOptions"/>.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, IReadOnlyList<KescoFilterOption>>? FilterLookupOptions { get; set; }

    /// <summary>Базовый SQL-запрос SELECT (без WHERE / ORDER BY).</summary>
    [Parameter] public string SelectSql { get; set; } = string.Empty;

    /// <summary>Выходные имена колонок SELECT для полнотекстового поиска.</summary>
    [Parameter] public string[] SearchColumns { get; set; } = [];

    /// <summary>Порядок сортировки по умолчанию.</summary>
    [Parameter] public string DefaultOrder { get; set; } = string.Empty;

    /// <summary>
    /// Тип компонента диалога редактирования.
    /// Диалог должен принимать параметр <c>Model</c> типа сущности.
    /// </summary>
    [Parameter] public Type? EditDialogType { get; set; }

    /// <summary>
    /// Загрузчик данных страницы. Передаётся как <c>DataLoader="this"</c> со страницы,
    /// наследующей <see cref="KescoGridPageBase{T}"/>.
    /// Если задан, <see cref="NotifyQueryChanged"/> вызывает его вместо <see cref="OnQueryChanged"/>.
    /// </summary>
    [Parameter] public IKescoGridDataLoader? DataLoader { get; set; }

    /// <summary>
    /// Режим отображения кнопки меню (⋮) в заголовках колонок.
    /// <c>Hidden</c> — всегда скрыта, <c>Always</c> — всегда видна,
    /// <c>Mobile</c> — только на мобильных (≤960px, по умолчанию).
    /// </summary>
    [Parameter] public ColumnMenuMode ColumnMenuMode { get; set; } = ColumnMenuMode.Mobile;

    /// <summary>Показывать кнопку выбора записей (чекбоксы).</summary>
    [Parameter] public bool SelectVisible { get; set; }

    /// <summary>Показывать группу «Печать» в меню групповых операций.</summary>
    [Parameter] public bool ShowPrint { get; set; }

    /// <summary>Показывать группу «Выгрузка в Excel» в меню групповых операций.</summary>
    [Parameter] public bool ShowExcel { get; set; }

    /// <summary>
    /// Кастомные группы операций для меню групповых операций.
    /// Каждая группа рендерится как подменю со своими операциями.
    /// Обработчики (<see cref="BatchOperation.OnExecute"/>) реализуются в приложении.
    /// </summary>
    [Parameter] public IReadOnlyList<BatchOperationGroup>? CustomBatchGroups { get; set; }

    /// <summary>SQL-имена колонок текущей группировки.</summary>
    public IReadOnlyList<string> GroupColumns => _groupColumns.AsReadOnly();

    /// <summary>ID выбранных сущностей (персистентно между страницами).</summary>
    public IReadOnlyCollection<int> SelectedIds => _selectedIds;

    private int _totalPages => _pageSize > 0 && TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / _pageSize) : 1;

    // ── Lifecycle ────────────────────────────────────────────────────────────────
    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        _pageSize = PageSize;
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        _pageNumber = PageNumber;
        if (TotalCount > 0 && _pageNumber > _totalPages)
            _pageNumber = _totalPages;
        if (_selectMode)
            _selectAllChecked = ComputeSelectAllState();
    }

    private bool _loadingChildIds;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _columnsReady = true;
            _dataKey++;
            StateHasChanged();
        }
        else if (_columnsReady && !string.IsNullOrEmpty(Id))
        {
            // Реинициализируем drag после каждого ре-рендера динамических колонок.
            _dotnetRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("kescoGridColumnDrag.init", Id, _dotnetRef);

            // Eager load group-child IDs so group checkboxes reflect selection state
            if (_selectMode && DataLoader is not null && !_loadingChildIds)
            {
                var missingKeys = new List<string>();
                foreach (var row in Items ?? [])
                {
                    if (row is GroupHeaderRow gh && !_groupChildIds.ContainsKey(gh.FullKey))
                        missingKeys.Add(gh.FullKey);
                }
                if (missingKeys.Count > 0)
                {
                    _loadingChildIds = true;
                    try
                    {
                        await LoadChildIdsForGroupsAsync(missingKeys);
                        StateHasChanged();
                    }
                    finally
                    {
                        _loadingChildIds = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Вызывается из JS после завершения drag-and-drop заголовка колонки.
    /// Обновляет <see cref="_columnOrder"/> через insert (не swap).
    /// </summary>
    /// <param name="srcSql">SQL-имя перетаскиваемой колонки.</param>
    /// <param name="targetSql">SQL-имя целевой колонки (относительно которой вставляем).</param>
    /// <param name="insertBefore"><c>true</c> — вставить перед <paramref name="targetSql"/>, <c>false</c> — после.</param>
    /// <summary>
    /// Вызывается из JS при начале drag заголовка колонки.
    /// Устанавливает <see cref="KescoDragState.DraggedColumn"/> для tray-drop обработчиков.
    /// </summary>
    [JSInvokable]
    public void SetDraggedColumn(string? sqlName)
        => KescoDragState.DraggedColumn = sqlName;

    [JSInvokable]
    public void OnColumnDrop(string srcSql, string targetSql, bool insertBefore)
    {
        if (!_columnBySqlName.TryGetValue(srcSql, out var srcMeta)) return;
        if (!_columnBySqlName.TryGetValue(targetSql, out var tgtMeta)) return;

        var srcId = srcMeta.ColumnId;
        var tgtId = tgtMeta.ColumnId;

        var srcIdx = _columnOrder.IndexOf(srcId);
        var tgtIdx = _columnOrder.IndexOf(tgtId);
        if (srcIdx < 0 || tgtIdx < 0 || srcIdx == tgtIdx) return;

        _columnOrder.RemoveAt(srcIdx);
        // После удаления источника пересчитываем целевой индекс
        tgtIdx = _columnOrder.IndexOf(tgtId);
        var insertAt = insertBefore ? tgtIdx : tgtIdx + 1;
        insertAt = Math.Clamp(insertAt, 0, _columnOrder.Count);
        _columnOrder.Insert(insertAt, srcId);

        _dataKey++;
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(Id))
        {
            try { await JS.InvokeVoidAsync("kescoGridColumnDrag.dispose", Id); } catch { }
        }
        _dotnetRef?.Dispose();
    }

    private void ToggleSelectMode()
    {
        _selectMode = !_selectMode;
        if (!_selectMode)
        {
            _selectedIds.Clear();
            _groupChildIds.Clear();
            _selectAllChecked = false;
        }
        _dataKey++;
        StateHasChanged();
    }

    /// <summary>
    /// true — некоторые (но не все) сущности на текущей странице выбраны.
    /// Учитывает как DetailRow, так и дочерние сущности GroupHeaderRow.
    /// </summary>
    private bool IsHeaderIndeterminate()
    {
        if (Items is null) return false;
        bool anySelected = false;
        bool allSelected = true;
        bool anyItem = false;

        foreach (var row in Items)
        {
            if (row is IDetailRow dr && dr.Item is Entity e)
            {
                anyItem = true;
                if (_selectedIds.Contains(e.Id)) anySelected = true;
                else allSelected = false;
            }
            else if (row is GroupHeaderRow gh)
            {
                var childIds = GetChildIdsForGroup(gh.FullKey);
                if (childIds is not null && childIds.Count > 0)
                {
                    anyItem = true;
                    bool groupAllSelected = true;
                    foreach (var id in childIds)
                    {
                        if (_selectedIds.Contains(id)) anySelected = true;
                        else groupAllSelected = false;
                    }
                    if (!groupAllSelected) allSelected = false;
                }
            }
        }

        return anyItem && anySelected && !allSelected;
    }

    /// <summary>
    /// Вычисляет состояние чекбокса в заголовке: все ли сущности на текущей странице выбраны.
    /// Учитывает как DetailRow, так и дочерние сущности GroupHeaderRow.
    /// </summary>
    private bool ComputeSelectAllState()
    {
        if (Items is null) return false;
        bool anyItem = false;

        foreach (var row in Items)
        {
            if (row is IDetailRow dr && dr.Item is Entity e)
            {
                anyItem = true;
                if (!_selectedIds.Contains(e.Id)) return false;
            }
            else if (row is GroupHeaderRow gh)
            {
                var childIds = GetChildIdsForGroup(gh.FullKey);
                if (childIds is not null && childIds.Count > 0)
                {
                    anyItem = true;
                    foreach (var id in childIds)
                        if (!_selectedIds.Contains(id)) return false;
                }
            }
        }

        return anyItem;
    }

    /// <summary>
    /// Tri-state чекбокса для группового заголовка.
    /// Все потомки выбраны → (true, false), ни одного → (false, false), часть → (false, true).
    /// </summary>
    private (bool Checked, bool Indeterminate) ComputeGroupCheckState(GroupHeaderRow gh)
    {
        var childIds = GetChildIdsForGroup(gh.FullKey);
        if (childIds is null || childIds.Count == 0)
            return (false, false);

        int selectedCount = 0;
        foreach (var id in childIds)
            if (_selectedIds.Contains(id))
                selectedCount++;

        if (selectedCount == 0) return (false, false);
        if (selectedCount == childIds.Count) return (true, false);
        return (false, true);
    }

    /// <summary>
    /// Возвращает ID всех дочерних сущностей группы (из кеша или null).
    /// При первом обращении кеш заполняется через DataLoader.
    /// </summary>
    private HashSet<int>? GetChildIdsForGroup(string fullKey)
    {
        if (_groupChildIds.TryGetValue(fullKey, out var ids))
            return ids;
        return null;
    }

    /// <summary>
    /// Ленивая загрузка ID дочерних сущностей группы через DataLoader.
    /// Вызывается при первом клике по чекбоксу группы.
    /// </summary>
    private async Task LoadChildIdsForGroupsAsync(List<string> fullKeys)
    {
        if (DataLoader is null || fullKeys.Count == 0) return;
        var batch = await DataLoader.LoadGroupChildIdsAsync(fullKeys, _lastQuery);
        foreach (var kv in batch)
            _groupChildIds[kv.Key] = kv.Value;
    }

    /// <summary>
    /// Сбрасывает кеш дочерних ID групп (вызывается перед перезагрузкой данных).
    /// </summary>
    private void ClearGroupChildCache()
    {
        if (_selectMode)
            _groupChildIds.Clear();
    }

    /// <summary>
    /// Обработчик клика по tri-state иконке в заголовке колонки выбора.
    /// Если есть выделенные (хотя бы один) → снимаем всё, иначе → выделяем всё.
    /// Обрабатывает как DetailRow, так и GroupHeaderRow (через дочерние ID).
    /// </summary>
    private async Task OnHeaderTriToggle()
    {
        bool anySelected = IsHeaderIndeterminate() || _selectAllChecked;
        _selectAllChecked = !anySelected;

        // Убедимся, что дочерние ID всех видимых групп загружены
        var missingKeys = new List<string>();
        foreach (var row in Items ?? [])
        {
            if (row is GroupHeaderRow gh && GetChildIdsForGroup(gh.FullKey) is null)
                missingKeys.Add(gh.FullKey);
        }
        if (missingKeys.Count > 0)
            await LoadChildIdsForGroupsAsync(missingKeys);

        foreach (var row in Items ?? [])
        {
            if (row is IDetailRow dr && dr.Item is Entity entity)
            {
                if (!anySelected) _selectedIds.Add(entity.Id);
                else _selectedIds.Remove(entity.Id);
            }
            else if (row is GroupHeaderRow gh)
            {
                var childIds = GetChildIdsForGroup(gh.FullKey);
                if (childIds is not null)
                {
                    foreach (var id in childIds)
                    {
                        if (!anySelected) _selectedIds.Add(id);
                        else _selectedIds.Remove(id);
                    }
                }
            }
        }

        StateHasChanged();
    }

    // ── Selection event handlers ─────────────────────────────────────────────────

    /// <summary>
    /// Обработчик клика по чекбоксу строки детализации — добавляет/удаляет ID сущности.
    /// </summary>
    private async Task OnRowSelectAsync(int entityId, bool selected)
    {
        if (selected)
            _selectedIds.Add(entityId);
        else
            _selectedIds.Remove(entityId);
        _selectAllChecked = ComputeSelectAllState();
        StateHasChanged();
    }

    /// <summary>
    /// Обработчик клика по tri-state иконке группы.
    /// Если все потомки выбраны → снимаем всё; иначе → выделяем всё.
    /// </summary>
    private async Task OnGroupTriToggle(GroupHeaderRow gh)
    {
        var childIds = GetChildIdsForGroup(gh.FullKey);
        if (childIds is null)
        {
            await LoadChildIdsForGroupsAsync([gh.FullKey]);
            childIds = GetChildIdsForGroup(gh.FullKey);
        }
        if (childIds is null || childIds.Count == 0) return;

        bool allSelected = true;
        foreach (var id in childIds)
        {
            if (!_selectedIds.Contains(id)) { allSelected = false; break; }
        }

        foreach (var id in childIds)
        {
            if (allSelected) _selectedIds.Remove(id);
            else _selectedIds.Add(id);
        }
        _selectAllChecked = ComputeSelectAllState();
        StateHasChanged();
    }

    /// <summary>
    /// Обработчик клика по чекбоксу группы — выделяет/снимает всех потомков.
    /// При первом обращении лениво загружает ID дочерних сущностей через DataLoader.
    /// </summary>
    private async Task OnGroupSelectAsync(GroupHeaderRow gh, bool selected)
    {
        var childIds = GetChildIdsForGroup(gh.FullKey);
        if (childIds is null)
        {
            // Ленивая загрузка при первом клике
            await LoadChildIdsForGroupsAsync([gh.FullKey]);
            childIds = GetChildIdsForGroup(gh.FullKey);
        }
        if (childIds is null) return;

        foreach (var id in childIds)
        {
            if (selected)
                _selectedIds.Add(id);
            else
                _selectedIds.Remove(id);
        }
        _selectAllChecked = ComputeSelectAllState();
        StateHasChanged();
    }

    private void ToggleSubGroup(string label)
    {
        if (_openSubGroups.TryGetValue(label, out var isOpen))
            _openSubGroups[label] = !isOpen;
        else
            _openSubGroups[label] = true;
    }

    private bool IsSubGroupOpen(string label)
        => _openSubGroups.TryGetValue(label, out var isOpen) && isOpen;

    private async Task PrintCurrentPageInternal()    => await JS.InvokeVoidAsync("kescoGridPrint.printCurrentPage", Id);

    private async Task PrintAllInternal()
    {
        if (DataLoader is null) return;
        // JS-спиннер — не трогает Blazor-рендер, грид не перерендеривается
        var spinnerId = Id + "-print-spinner";
        _ = JS.InvokeVoidAsync("kescoGridPrint.showSpinner", spinnerId);
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            var html = await DataLoader.BuildPrintHtmlAsync(
                columns, Title, BuildFilterDescription(), BuildGroupDescription());
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            await JS.InvokeAsync<object>("kescoGridPrint.printHtml", html);
        }
        catch (Exception ex)
        {
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            Snackbar.Add($"Ошибка печати: {ex.Message}", Severity.Error);
        }
    }

    private async Task PrintSelectedInternal()
    {
        if (DataLoader is null || _selectedIds.Count == 0) return;
        var spinnerId = Id + "-print-spinner";
        _ = JS.InvokeVoidAsync("kescoGridPrint.showSpinner", spinnerId);
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            var html = await DataLoader.BuildPrintHtmlForSelectedAsync(
                columns, Title, _selectedIds.ToList(),
                BuildFilterDescription(), BuildGroupDescription());
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            await JS.InvokeAsync<object>("kescoGridPrint.printHtml", html);
        }
        catch (Exception ex)
        {
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            Snackbar.Add($"Ошибка печати: {ex.Message}", Severity.Error);
        }
    }
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

    private string? BuildGroupDescription()
    {
        if (_groupColumns.Count == 0) return null;
        var names = _groupColumns
            .Select(c => _columnBySqlName.TryGetValue(c, out var m) ? m.DisplayName : c);
        return $"Группировка: {string.Join(" → ", names)}";
    }

    private async Task ExcelCurrentPageInternal()
    {
        if (DataLoader is null) return;
        _isExporting = true;
        StateHasChanged();
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            await DataLoader.ExcelExportAsync(new ExcelExportRequest
            {
                Mode = ExcelExportMode.CurrentPage,
                Title = Title,
                VisibleColumns = columns,
                FilterDescription = BuildFilterDescription(),
                GroupDescription = BuildGroupDescription(),
            });
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private async Task ExcelAllInternal()
    {
        if (DataLoader is null) return;
        _isExporting = true;
        StateHasChanged();
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            await DataLoader.ExcelExportAsync(new ExcelExportRequest
            {
                Mode = ExcelExportMode.All,
                Title = Title,
                VisibleColumns = columns,
                FilterDescription = BuildFilterDescription(),
                GroupDescription = BuildGroupDescription(),
            });
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private async Task ExcelSelectedInternal()
    {
        if (DataLoader is null || _selectedIds.Count == 0) return;
        _isExporting = true;
        StateHasChanged();
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            await DataLoader.ExcelExportAsync(new ExcelExportRequest
            {
                Mode = ExcelExportMode.Selected,
                Title = Title,
                VisibleColumns = columns,
                SelectedIds = _selectedIds.ToList(),
                FilterDescription = BuildFilterDescription(),
                GroupDescription = BuildGroupDescription(),
            });
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private bool _menuVisible(KescoColumnMeta meta) =>
        meta is not null
        && ColumnMenuMode != ColumnMenuMode.Hidden
        && ((_trayExpanded && meta.Groupable)
            || (_filterTrayExpanded && meta.Filterable));

    private void HandleSortClick(string sqlName) => _ = ToggleSort(sqlName);

    /// <summary>
    /// Открывает диалог редактирования для строки детализации.
    /// Вызывается из сервисной колонки с иконкой карандаша.
    /// После успешного сохранения показывает уведомление и перезагружает данные.
    /// </summary>
    private async Task HandleEditClick(IDetailRow detail)
    {
        if (EditDialogType is null) return;
        var parameters = new DialogParameters { ["Model"] = detail.Item };
        var options = new DialogOptionsEx { MaxWidth = MaxWidth.Small, FullWidth = true, DragMode = MudDialogDragMode.Simple };
        var dialog = await DialogService.ShowExAsync(EditDialogType, string.Empty, parameters, options);
        if (!(await dialog.Result)?.Canceled ?? false)
        {
            Snackbar.Add(EditSuccessMessage, Severity.Success);
            await NotifyQueryChanged();
        }
    }

    private List<int> _columnOrderSnapshot = [];

    private async Task OpenColumnSettings()
    {
        _columnOrderSnapshot = [.._columnOrder];

        var items = _columnBySqlName.Values
            .OrderBy(m => _columnOrder.IndexOf(m.ColumnId))
            .Select(m => new ColumnSettingsItem
            {
                SqlName     = m.SqlName,
                DisplayName = m.DisplayName,
                IsVisible   = !_hiddenSqlNames.Contains(m.SqlName),
                IsReadonly  = IsGrouped(m.SqlName)
            })
            .ToList();

        for (int i = 0; i < _sortState.Count; i++)
        {
            var sc = _sortState[i];
            var match = items.FirstOrDefault(it =>
                _columnBySqlName.TryGetValue(it.SqlName, out var m) && m.SortName == sc.Column);
            if (match is not null)
            {
                match.SortPriority = i + 1;
                match.IsSortDesc   = sc.Desc;
            }
        }

        var parameters = new DialogParameters<KescoColumnSettingsDialog> { { x => x.Items, items } };
        var options = new DialogOptionsEx
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            DragMode = MudDialogDragMode.Simple,
        };
        var dialog = await DialogService.ShowExAsync<KescoColumnSettingsDialog>("Настройка колонок", parameters, options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled && result.Data is List<ColumnSettingsItem> updatedItems)
        {
            _hiddenSqlNames.Clear();
            _columnOrder.Clear();
            foreach (var item in updatedItems)
            {
                if (!item.IsVisible)
                    _hiddenSqlNames.Add(item.SqlName);
                if (_columnBySqlName.TryGetValue(item.SqlName, out var meta2))
                    _columnOrder.Add(meta2.ColumnId);
            }

            _sortState.Clear();
            foreach (var item in updatedItems.Where(i => i.SortPriority > 0).OrderBy(i => i.SortPriority))
            {
                var sortName = _columnBySqlName.TryGetValue(item.SqlName, out var meta3) ? meta3.SortName : item.SqlName;
                _sortState.Add(new SortColumn(sortName, item.IsSortDesc));
            }

            _pageNumber = 1;
            await NotifyQueryChanged();
        }
        else
        {
            _columnOrder.Clear();
            _columnOrder.AddRange(_columnOrderSnapshot);
            _dataKey++;
            StateHasChanged();
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает порядок группировки для указанной SQL-колонки.
    /// Меньшее значение = внешний уровень группировки.
    /// </summary>
    public int GetGroupByOrder(string sqlColumn)
    {
        var idx = _groupColumns.IndexOf(sqlColumn);
        return idx < 0 ? 0 : idx;
    }

    /// <summary>
    /// Переключает сортировку по колонке: нет → ASC → DESC → нет.
    /// Принимает SqlName; реальное выражение ORDER BY берётся из
    /// <see cref="KescoColumnMeta.SortName"/> зарегистрированной колонки.
    /// </summary>
    public async Task ToggleSort(string sqlName)
    {
        // Резолвим имя для ORDER BY: если зарегистрирован SortName — используем его
        var sortName = _columnBySqlName.TryGetValue(sqlName, out var meta)
            ? meta.SortName
            : sqlName;

        var idx = _sortState.FindIndex(s => s.Column == sortName);
        if (idx >= 0)
        {
            if (_sortState[idx].Desc)
                _sortState.RemoveAt(idx);
            else
                _sortState[idx] = _sortState[idx] with { Desc = true };
        }
        else
        {
            _sortState.Insert(0, new SortColumn(sortName, false));
            if (_sortState.Count > 2)
                _sortState.RemoveAt(2);
        }

        _pageNumber = 1;
        await NotifyQueryChanged();
        StateHasChanged();
    }

    /// <summary>
    /// Возвращает бейдж сортировки для колонки: номер приоритета + стрелка направления.
    /// Принимает SqlName; поиск в состоянии сортировки ведётся по резолвленному SortName.
    /// </summary>
    public RenderFragment GetSortBadge(string sqlName) => builder =>
    {
        var sortName = _columnBySqlName.TryGetValue(sqlName, out var meta)
            ? meta.SortName
            : sqlName;

        var idx = _sortState.FindIndex(s => s.Column == sortName);
        if (idx < 0) return;
        var arrow = _sortState[idx].Desc ? "\u2193" : "\u2191";
        var label = (idx + 1).ToString() + arrow;
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "chip-sort-badge");
        builder.AddContent(2, label);
        builder.CloseElement();
    };

    // ── Grouping tray ────────────────────────────────────────────────────────────
    private async Task ToggleTray()
    {
        _trayExpanded = !_trayExpanded;
        TrayStateChanged?.Invoke();
        if (!_trayExpanded)
        {
            _groupColumns.Clear();
            _pageNumber = 1;
            await NotifyQueryChanged();
        }
        else
        {
            StateHasChanged();
        }
    }

    private async Task OnSearchTextChanged(string? value)
    {
        _searchText = value;
        _pageNumber = 1;
        await NotifyQueryChanged();
    }

    private async Task AddGroupColumn(string column)
    {
        if (_groupColumns.Contains(column))
            return;
        _groupColumns.Add(column);
        _pageNumber = 1;
        await NotifyQueryChanged();
    }

    private async Task RemoveGroupColumn(string column)
    {
        _groupColumns.Remove(column);
        _pageNumber = 1;
        await NotifyQueryChanged();
    }

    /// <summary>
    /// Переключает развёрнутость всех групп на заданной глубине через DataLoader.
    /// </summary>
    private async Task ToggleLevel(int depth)
    {
        if (DataLoader is not null)
        {
            await DataLoader.ToggleLevelExpandedAsync(depth);
            StateHasChanged();
        }
    }

    private void OnChipDragStart(DragEventArgs e, int index)
    {
        e.DataTransfer.EffectAllowed = "move";
        _dragSourceIndex = index;
        KescoDragState.DraggedColumn = _groupColumns[index];
    }

    private void OnChipDragEnd()
    {
        _dragSourceIndex = -1;
        KescoDragState.DraggedColumn = null;
    }

    private void OnTrayDragOver(DragEventArgs e)
    {
        e.DataTransfer.DropEffect = "move";
    }

    private async Task OnTrayDrop(DragEventArgs e, int targetIndex)
    {
        var draggedData = KescoDragState.DraggedColumn;
        KescoDragState.DraggedColumn = null;

        if (!string.IsNullOrEmpty(draggedData)
            && _columnBySqlName.TryGetValue(draggedData, out var m) && m.Groupable
            && !_groupColumns.Contains(draggedData))
        {
            _groupColumns.Add(draggedData);
            _dragSourceIndex = -1;
            _pageNumber = 1;
            StateHasChanged();
            await NotifyQueryChanged();
            return;
        }

        if (_dragSourceIndex < 0 || _dragSourceIndex >= _groupColumns.Count)
            return;

        var item = _groupColumns[_dragSourceIndex];
        _groupColumns.RemoveAt(_dragSourceIndex);

        if (targetIndex < 0 || targetIndex >= _groupColumns.Count + 1)
            _groupColumns.Add(item);
        else if (targetIndex > _dragSourceIndex)
            _groupColumns.Insert(targetIndex - 1, item);
        else
            _groupColumns.Insert(targetIndex, item);

        _dragSourceIndex = -1;
        _pageNumber = 1;
        StateHasChanged();
        await NotifyQueryChanged();
    }

    // ── Pagination ───────────────────────────────────────────────────────────────
    private async Task OnPageSizeChangedAsync(int value)
    {
        _pageSize = value;
        _pageNumber = 1;
        await NotifyQueryChanged();
    }

    /// <summary>
    /// Сбрасывает номер страницы на 1 и инициирует перезагрузку данных.
    /// </summary>
    public async Task RefreshAsync()
    {
        _pageNumber = 1;
        await NotifyQueryChanged();
    }

    private async Task GoToFirstPageAsync()
    {
        if (_pageNumber <= 1) return;
        _pageNumber = 1;
        await NotifyQueryChanged();
    }

    private async Task GoToPrevPageAsync()
    {
        if (_pageNumber <= 1) return;
        _pageNumber--;
        await NotifyQueryChanged();
    }

    private async Task GoToNextPageAsync()
    {
        if (_pageNumber >= _totalPages) return;
        _pageNumber++;
        await NotifyQueryChanged();
    }

    private async Task GoToLastPageAsync()
    {
        if (_pageNumber >= _totalPages) return;
        _pageNumber = _totalPages;
        await NotifyQueryChanged();
    }

    // ── Filter tray ──────────────────────────────────────────────────────────────
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

    // ── Core notification ────────────────────────────────────────────────────────
    private async Task NotifyQueryChanged()
    {
        _dataKey++;
        ClearGroupChildCache();
        var query = new KescoDataQuery
        {
            SearchText    = _searchText,
            GroupEnabled  = _groupColumns.Count > 0,
            GroupColumns  = _groupColumns.ToList(),
            SortColumns   = _sortState.ToList(),
            PageNumber    = _pageNumber,
            PageSize      = _pageSize,
            ColumnFilters = _activeFilters.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        // Если изменилась суть запроса (фильтр, поиск, группировка, сортировка) —
        // сбрасываем выделение, т.к. набор данных поменялся.
        // При смене только номера страницы или размера страницы — выделение сохраняется.
        if (_selectMode && _lastQuery.PageNumber != 0)
        {
            var essenceChanged =
                _lastQuery.SearchText    != query.SearchText ||
                !_lastQuery.GroupColumns.SequenceEqual(query.GroupColumns) ||
                !_lastQuery.SortColumns.SequenceEqual(query.SortColumns) ||
                _lastQuery.ColumnFilters.Count != query.ColumnFilters.Count ||
                !_lastQuery.ColumnFilters.Keys.SequenceEqual(query.ColumnFilters.Keys);
            if (essenceChanged)
            {
                _selectedIds.Clear();
                _selectAllChecked = false;
            }
        }

        _lastQuery = query;
        if (DataLoader is not null)
            await DataLoader.OnQueryChangedAsync(query);
        else
            await OnQueryChanged.InvokeAsync(query);
    }

    // ── IKescoGrid — реализация интерфейса ───────────────────────────────────────

    event Action? IKescoGrid.ColumnsChanged
    {
        add    => ColumnsChanged += value;
        remove => ColumnsChanged -= value;
    }

    event Action? IKescoGrid.TrayStateChanged
    {
        add    => TrayStateChanged += value;
        remove => TrayStateChanged -= value;
    }

    string IKescoGrid.SelectSql        => SelectSql;
    string[] IKescoGrid.SearchColumns  => SearchColumns;
    string IKescoGrid.DefaultOrder     => DefaultOrder;
    Type? IKescoGrid.EditDialogType    => EditDialogType;

    /// <summary>
    /// Возвращает <c>true</c> если колонка участвует в текущей группировке.
    /// </summary>
    public bool IsGrouped(string sqlName)      => _groupColumns.Contains(sqlName);
    bool IKescoGrid.IsGrouped(string sqlName)  => IsGrouped(sqlName);

    Task IKescoGrid.ToggleSort(string sqlName)             => ToggleSort(sqlName);
    RenderFragment IKescoGrid.GetSortBadge(string sqlName) => GetSortBadge(sqlName);

    /// <summary>
    /// Возвращает метаданные колонки по её SQL-имени, либо <c>null</c>.
    /// </summary>
    public KescoColumnMeta? GetColumnMeta(string sqlName)
        => _columnBySqlName.TryGetValue(sqlName, out var m) ? m : null;

    KescoColumnMeta? IKescoGrid.GetColumnMeta(string sqlName) => GetColumnMeta(sqlName);

    /// <summary>
    /// Возвращает метаданные колонки по числовому <c>ColumnId</c>, либо <c>null</c>.
    /// </summary>
    public KescoColumnMeta? GetColumnMetaById(int columnId)
        => _columnById.TryGetValue(columnId, out var m) ? m : null;

    KescoColumnMeta? IKescoGrid.GetColumnMetaById(int columnId) => GetColumnMetaById(columnId);

    /// <summary>
    /// Регистрирует колонку. Вызывается из <see cref="KescoColumnDef"/> при инициализации.
    /// Поддерживает два индекса: по <paramref name="columnId"/> и по <paramref name="sqlName"/>.
    /// </summary>
    void IKescoGrid.RegisterColumn(int columnId, string sqlName, string displayName, bool groupable, bool filterable, string? sortName)
    {
        if (string.IsNullOrEmpty(sqlName)) return;
        var colType = FilterColumnTypes.TryGetValue(sqlName, out var t) ? t : ColumnType.Text;
        var meta = new KescoColumnMeta
        {
            ColumnId    = columnId,
            SqlName     = sqlName,
            DisplayName = displayName,
            SortName    = string.IsNullOrEmpty(sortName) ? sqlName : sortName,
            Groupable   = groupable,
            Filterable  = filterable,
            Type        = ColumnTypes.ColumnTypeRegistry.FromKind(colType),
        };
        _columnById[columnId]     = meta;
        _columnBySqlName[sqlName] = meta;
        ColumnsChanged?.Invoke();
    }

    /// <summary>
    /// Отменяет регистрацию колонки при уничтожении <see cref="KescoColumnDef"/>.
    /// Удаляет из обоих индексов.
    /// </summary>
    void IKescoGrid.UnregisterColumn(int columnId, string sqlName)
    {
        _columnById.Remove(columnId);
        _columnBySqlName.Remove(sqlName);
        ColumnsChanged?.Invoke();
    }

    ColumnMenuMode IKescoGrid.ColumnMenuMode => ColumnMenuMode;

    bool IKescoGrid.IsGroupingTrayExpanded => _trayExpanded;
    bool IKescoGrid.IsFilterTrayExpanded   => _filterTrayExpanded;

    Task IKescoGrid.AddGroupAsync(string sqlName) => AddGroupColumn(sqlName);

    Task IKescoGrid.AddFilterAsync(string sqlName)
        => OpenFilterDialog(sqlName, _columnBySqlName.TryGetValue(sqlName, out var m) ? m.DisplayName : sqlName);

    void IKescoGrid.RegisterColumnInOrder(int columnId)
    {
        if (!_columnOrder.Contains(columnId))
            _columnOrder.Add(columnId);
    }

    void IKescoGrid.RegisterCellTemplate(int columnId, object template)
        => _cellTemplates[columnId] = template;

    bool IKescoGrid.IsColumnHidden(string sqlName) => _hiddenSqlNames.Contains(sqlName);

    IReadOnlyList<KescoColumnMeta> IKescoGrid.GetVisibleColumns()
        => _columnOrder
            .Select(id => _columnById.GetValueOrDefault(id))
            .Where(meta => meta is not null
                && !_hiddenSqlNames.Contains(meta.SqlName)
                && !IsGrouped(meta.SqlName))
            .ToList()!;
}
