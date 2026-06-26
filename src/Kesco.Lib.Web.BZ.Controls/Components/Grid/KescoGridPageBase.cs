using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Dapper;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.BZ.Controls.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid;

/// <summary>
/// Интерфейс обратного вызова, реализуемый <see cref="KescoGridPageBase{T}"/>.
/// Позволяет <see cref="KescoGrid{TEntity}"/> уведомлять страницу об изменении запроса
/// через каскадный параметр, без явной передачи <c>OnQueryChanged</c> в разметке.
/// </summary>
public interface IKescoGridDataLoader
{
    /// <summary>
    /// Вызывается гридом при изменении любого параметра запроса:
    /// поиска, сортировки, группировки, фильтрации, пагинации.
    /// </summary>
    Task OnQueryChangedAsync(KescoDataQuery query);

    /// <summary>
    /// Вызывается гридом при выборе пункта меню «Выгрузка в Excel».
    /// </summary>
    /// <param name="request">Параметры экспорта: режим, заголовок, список видимых колонок.</param>
    Task ExcelExportAsync(ExcelExportRequest request);

    /// <summary>
    /// Вызывается гридом при выборе пункта меню «Печать всех данных».
    /// Загружает все строки (без пагинации, с учётом группировки и состояния
    /// развёрнутости групп) и возвращает готовый HTML-документ для печати в iframe.
    /// </summary>
    /// <param name="columns">Видимые колонки в порядке отображения.</param>
    /// <param name="title">Заголовок грида.</param>
    /// <param name="filterDescription">Описание активных фильтров (или null).</param>
    /// <param name="groupDescription">Описание колонок группировки (или null).</param>
    Task<string> BuildPrintHtmlAsync(
        IReadOnlyList<KescoColumnMeta> columns, string title,
        string? filterDescription, string? groupDescription);

    /// <summary>
    /// Проверяет, развёрнуты ли ВСЕ группы на указанной глубине.
    /// Используется чипами панели группировки для отображения иконки переключателя
    /// (<see cref="Icons.Material.Filled.UnfoldLess"/> / <see cref="Icons.Material.Filled.UnfoldMore"/>).
    /// Возвращает false, если хотя бы одна группа свёрнута или группировка не активна.
    /// </summary>
    bool IsLevelFullyExpanded(int depth);

    /// <summary>
    /// Переключает состояние ВСЕХ групп на указанной глубине.
    /// При разворачивании — каскадно разворачивает родительские уровни (0..depth-1).
    /// При сворачивании — сворачивает только этот уровень (дочерние группы сохраняют
    /// своё индивидуальное состояние в <see cref="KescoDataQuery.ExpandedGroups"/>,
    /// но становятся невидимы через <see cref="KescoGroupingEngine.WalkTree"/>).
    /// После переключения сбрасывает страницу на 1 и перезагружает данные.
    /// </summary>
    Task ToggleLevelExpandedAsync(int depth);
}

/// <summary>
/// Базовый класс Blazor-страницы с серверным гридом <see cref="KescoGrid{TEntity}"/>.
/// Инкапсулирует инфраструктуру загрузки данных: плоский режим и режим группировки.
/// <para>
/// Страница-наследник должна:
/// <list type="bullet">
///   <item>Унаследоваться: <c>@inherits KescoGridPageBase&lt;МояСущность&gt;</c></item>
///   <item>Передать SQL-параметры в атрибуты <c>&lt;KescoGrid&gt;</c>:
///     <c>SelectSql</c>, <c>SearchColumns</c>, <c>DefaultOrder</c>, <c>EditDialogType</c></item>
///   <item>Присвоить свойство <see cref="Grid"/> через <c>@ref</c>:
///     <code>@ref="@(Grid = value)"</code> или объявить поле и переопределить свойство <see cref="Grid"/>.</item>
///   <item>Передать <c>DataLoader="this"</c> в параметры <c>&lt;KescoGrid&gt;</c></item>
/// </list>
/// </para>
/// </summary>
/// <typeparam name="T">Тип сущности — наследник <see cref="Entity"/>.</typeparam>
public abstract class KescoGridPageBase<T> : ComponentBase, IKescoGridDataLoader where T : Entity
{
    // ── Инжектируемые сервисы ────────────────────────────────────────────────────

    /// <summary>Менеджер подключения к БД — инжектируется автоматически.</summary>
    [Inject] protected DbManager Db { get; set; } = null!;

    /// <summary>Сервис уведомлений — инжектируется автоматически.</summary>
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    /// <summary>Сервис диалоговых окон — инжектируется автоматически.</summary>
    [Inject] protected IDialogService DialogService { get; set; } = null!;

    /// <summary>JS interop — для скачивания файлов и других операций.</summary>
    [Inject] protected IJSRuntime JS { get; set; } = null!;

    // ── Ссылка на грид — устанавливается через @ref="_dataGrid" ─────────────────

    /// <summary>
    /// Ссылка на грид через интерфейс <see cref="IKescoGrid"/> для чтения SQL-параметров.
    /// Заполняется автоматически при установке <see cref="Grid"/> на странице-наследнике:
    /// <code>
    /// protected override IKescoGrid? Grid
    /// {
    ///     get => _dataGrid;
    ///     set => _dataGrid = value;   // _dataGrid объявлен как KescoGrid&lt;IKescoGridRow&gt;
    /// }
    /// </code>
    /// Или проще — страница переопределяет свойство <see cref="Grid"/> через <c>@ref</c>-поле.
    /// </summary>
    protected virtual IKescoGrid? Grid { get; set; }

    // ── Общее состояние страницы ─────────────────────────────────────────────────

    /// <summary>
    /// Текущее состояние запроса к данным.
    /// Обновляется в <see cref="IKescoGridDataLoader.OnQueryChangedAsync"/> при каждом
    /// взаимодействии с гридом.
    /// </summary>
    protected KescoDataQuery _query = new();

    /// <summary>Строки текущей страницы (заголовки групп + строки детализации).</summary>
    protected List<IKescoGridRow> _rows = [];

    /// <summary>Признак загрузки данных — управляет индикатором в <see cref="KescoGrid{TEntity}"/>.</summary>
    protected bool _loading = true;

    /// <summary>
    /// Корни дерева групп — кешируются после успешной загрузки групповых данных
    /// в <see cref="LoadGroupedData"/>. Используется для получения всех FullKey
    /// групп по глубине (переключатели «развернуть/свернуть все» на чипах трея).
    /// null — данные не загружены или активен плоский режим.
    /// </summary>
    private List<GridGroupNode>? _groupTreeRoots;

    /// <summary>
    /// Кеш: глубина → список FullKey всех групп на этой глубине.
    /// Сбрасывается при каждой перезагрузке групповых данных.
    /// </summary>
    private Dictionary<int, List<string>>? _groupKeysByDepth;

    // ── Настройки уведомлений — могут быть переопределены на странице ─────────────

    /// <summary>
    /// Текст уведомления после успешного добавления записи.
    /// Может быть переопределён на странице.
    /// </summary>
    protected virtual string AddSuccessMessage => "Запись добавлена";

    /// <summary>
    /// Текст уведомления после успешного сохранения записи.
    /// Может быть переопределён на странице.
    /// </summary>
    protected virtual string SaveSuccessMessage => "Запись обновлена";

    // ── Типы колонок для фильтрации — вычисляются автоматически ─────────────────

    /// <summary>
    /// Типы данных фильтруемых колонок, автоматически определённые по <see cref="ColumnAttribute"/>
    /// и C#-типам свойств сущности <typeparamref name="T"/>.
    /// Маппинг: SQL-имя колонки → <see cref="ColumnType"/> (Text / Number / Boolean).
    /// <para>
    /// SQL-имя берётся из <c>[Column("...")]</c>-атрибута, либо из имени свойства если атрибут отсутствует
    /// (так работает, например, <c>TestTypeName</c> — алиас из JOIN без <c>[Column]</c>).
    /// </para>
    /// Может быть переопределено на странице для нестандартного маппинга.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, ColumnType> FilterColumnTypes => _inferredColumnTypes;

    // Кеш вычисляется один раз для каждого конкретного T при инициализации класса
    private static readonly IReadOnlyDictionary<string, ColumnType> _inferredColumnTypes
        = InferFilterColumnTypes();

    /// <summary>
    /// Кеш маппинга SQL-имя колонки → <see cref="PropertyInfo"/> свойства сущности <typeparamref name="T"/>.
    /// Используется при построении групповых заголовков для Excel-экспорта всех данных —
    /// читает значения групповых колонок из свойств сущности по их SQL-именам.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, PropertyInfo> _propertyMap
        = BuildPropertyMap();

    /// <summary>
    /// Строит словарь SQL-имя колонки → <see cref="PropertyInfo"/> через рефлексию
    /// по <see cref="ColumnAttribute"/> и свойствам <typeparamref name="T"/>.
    /// </summary>
    private static Dictionary<string, PropertyInfo> BuildPropertyMap()
    {
        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            map[colAttr?.Name ?? prop.Name] = prop;
        }
        return map;
    }

    /// <summary>
    /// Определяет типы колонок для фильтрации через рефлексию по свойствам <typeparamref name="T"/>.
    /// </summary>
    private static Dictionary<string, ColumnType> InferFilterColumnTypes()
    {
        var result = new Dictionary<string, ColumnType>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var sqlName = colAttr?.Name ?? prop.Name;
            result[sqlName] = MapClrTypeToColumnType(prop.PropertyType);
        }
        return result;
    }

    /// <summary>
    /// Приводит C#-тип свойства к <see cref="ColumnType"/> для диалога фильтрации.
    /// <c>Nullable&lt;T&gt;</c> обрабатывается через <see cref="Nullable.GetUnderlyingType"/>.
    /// </summary>
    private static ColumnType MapClrTypeToColumnType(Type clrType)
    {
        var t = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (t == typeof(bool))    return ColumnType.Boolean;
        if (t == typeof(int)    || t == typeof(long)    ||
            t == typeof(short)  || t == typeof(byte)    ||
            t == typeof(decimal)|| t == typeof(float)   ||
            t == typeof(double) || t == typeof(uint)    ||
            t == typeof(ulong)  || t == typeof(ushort))
            return ColumnType.Number;
        return ColumnType.Text;
    }

    // ── IKescoGridDataLoader — вызывается гридом при изменении запроса ───────────

    /// <summary>
    /// Реализация <see cref="IKescoGridDataLoader.OnQueryChangedAsync"/>.
    /// Копирует все поля из <paramref name="query"/> в <see cref="_query"/> и запускает загрузку.
    /// Вызывается автоматически гридом через каскадный параметр.
    /// </summary>
    async Task IKescoGridDataLoader.OnQueryChangedAsync(KescoDataQuery query)
    {
        _query.SearchText    = query.SearchText;
        _query.GroupEnabled  = query.GroupEnabled;
        _query.GroupColumns  = query.GroupColumns;
        _query.SortColumns   = query.SortColumns;
        _query.PageNumber    = query.PageNumber;
        _query.PageSize      = query.PageSize;
        _query.ColumnFilters = query.ColumnFilters;
        await LoadData();
    }

    async Task IKescoGridDataLoader.ExcelExportAsync(ExcelExportRequest request)
    {
        try
        {
            var columns = request.VisibleColumns;
            if (columns.Count == 0) return;

            List<IKescoGridRow> rowsToExport = request.Mode switch
            {
                ExcelExportMode.CurrentPage => await BuildExportRows(),
                ExcelExportMode.Selected   => _rows, // TODO: отфильтровать по SelectedItems
                ExcelExportMode.All        => await BuildAllRowsForExcel(),
                _ => _rows
            };

            if (rowsToExport.Count == 0)
            {
                Snackbar.Add("Нет данных для выгрузки", Severity.Warning);
                return;
            }

            var bytes = KescoGridExcelGenerator.ExportToExcel(
                request.Title, columns, rowsToExport, typeof(T), _query.ExpandedGroups,
                request.FilterDescription, request.GroupDescription);

            var base64   = Convert.ToBase64String(bytes);
            var fileName = $"{SanitizeFileName(request.Title)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            await JS.InvokeVoidAsync("kescoGridExcel.downloadFile", fileName, base64);
            Snackbar.Add($"Файл «{fileName}» выгружен", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка выгрузки: {ex.Message}", Severity.Error);
        }
    }

    async Task<string> IKescoGridDataLoader.BuildPrintHtmlAsync(
        IReadOnlyList<KescoColumnMeta> columns, string title,
        string? filterDescription, string? groupDescription)
    {
        var rows = await BuildAllRowsForPrint();
        return KescoGridPrintHtmlGenerator.Build(
            title, columns, rows, typeof(T), _query.ExpandedGroups,
            filterDescription, groupDescription);
    }

    /// <summary>
    /// Загружает ВСЕ строки без пагинации, соответствующие текущему запросу
    /// (поиск + фильтры), с учётом группировки и состояния развёрнутости групп.
    /// НЕ модифицирует <see cref="_rows"/> — возвращает новый список.
    /// </summary>
    private async Task<List<IKescoGridRow>> BuildAllRowsForPrint()
    {
        if (_query.GroupEnabled && _query.GroupColumns.Count > 0)
            return await BuildAllGroupedRowsForPrint();
        return await BuildAllFlatRowsForPrint();
    }

    /// <summary>
    /// Плоский режим: все строки без ROW_NUMBER() и пагинации.
    /// </summary>
    private async Task<List<IKescoGridRow>> BuildAllFlatRowsForPrint()
    {
        var selectSql     = Grid?.SelectSql     ?? string.Empty;
        var searchColumns = Grid?.SearchColumns ?? [];
        var defaultOrder  = Grid?.DefaultOrder  ?? string.Empty;

        var searchWhere    = _query.BuildWhereClause(searchColumns);
        var orderBy        = _query.BuildOrderBy(defaultOrder);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);

        var sql = $"SELECT * FROM ({selectSql}) _src";
        if (!string.IsNullOrWhiteSpace(where))
            sql += $" WHERE {where}";
        if (!string.IsNullOrWhiteSpace(orderBy))
            sql += $" ORDER BY {orderBy}";

        var items = await Db.QueryAsync<T>(sql, dp);
        return items.Select(i => (IKescoGridRow)new DetailRow<T> { Item = i }).ToList();
    }

    /// <summary>
    /// Режим группировки: GROUP BY для всего дерева, WalkTree без страничных
    /// границ (pageStart=1, pageEnd=int.MaxValue), все detail-строки развёрнутых групп.
    /// </summary>
    private async Task<List<IKescoGridRow>> BuildAllGroupedRowsForPrint()
    {
        var selectSql     = Grid?.SelectSql     ?? string.Empty;
        var searchColumns = Grid?.SearchColumns ?? [];
        var defaultOrder  = Grid?.DefaultOrder  ?? string.Empty;

        var searchWhere    = _query.BuildWhereClause(searchColumns);
        var orderBy        = _query.BuildOrderBy(defaultOrder);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);

        var exprs = _query.GroupColumns.ToList();

        var groupSql  = KescoGroupingEngine.BuildGroupAggregateSql(selectSql, exprs, where, _query.SortColumns);
        var groupRows = await Db.QueryAsync<GridGroupRow>(groupSql, dp);

        var aggregates = KescoGroupingEngine.BuildAggregates(groupRows);
        var roots      = KescoGroupingEngine.BuildTree(aggregates);
        KescoGroupingEngine.ComputeParentCounts(roots);
        foreach (var r in roots)
            KescoGroupingEngine.ComputeEffectiveRows(r, _query.ExpandedGroups);

        var layout = new List<GridLayoutItem>();
        int cur    = 1;
        KescoGroupingEngine.WalkTree(roots, _query.ExpandedGroups, 1, int.MaxValue, ref cur, layout);

        var result      = new List<IKescoGridRow>();
        var detailOrder = KescoGroupingEngine.BuildDetailOrder(orderBy, _query.GroupColumns, defaultOrder);

        foreach (var item in layout)
        {
            if (item.Header is not null)
                result.Add(item.Header);

            if (item.HasDetailRange && item.Aggregate is not null)
            {
                var ag           = item.Aggregate;
                var detailParams = new DynamicParameters();
                detailParams.AddDynamicParams(dp);

                var keyParts = ag.RawKeys
                    .Select((k, i) => { detailParams.Add($"dk{i}", k); return $"{exprs[i]} = @dk{i}"; })
                    .ToList();
                var detailWhere = KescoDataQuery.CombineWhere(where, string.Join(" AND ", keyParts));

                detailParams.Add("__start", item.DetailStart);
                detailParams.Add("__end",   item.DetailEnd);

                var sql  = KescoGroupingEngine.BuildDetailPageSql(selectSql, detailWhere, detailOrder);
                var rows = await Db.QueryAsync<T>(sql, detailParams);
                result.AddRange(rows.Select(i => new DetailRow<T> { Item = i, Depth = ag.Depth }));
            }
        }

        return result;
    }

    /// <summary>
    /// Загружает ВСЕ строки для экспорта в Excel.
    /// Плоский режим — один запрос (без пагинации).
    /// Группировка — запрос агрегатов (GROUP BY) + один запрос всех строк,
    /// групповая структура строится в C# по отсортированным данным.
    /// </summary>
    private async Task<List<IKescoGridRow>> BuildAllRowsForExcel()
    {
        if (_query.GroupEnabled && _query.GroupColumns.Count > 0)
            return await BuildAllGroupedRowsForExcel();
        return await BuildAllFlatRowsForPrint();
    }

    /// <summary>
    /// Режим группировки для Excel-экспорта всех данных.
    /// Делает ровно 2 SQL-запроса:
    /// 1. GROUP BY для агрегатов (количество записей в каждой группе)
    /// 2. SELECT * без пагинации, отсортированный по групповым колонкам + детальный порядок.
    /// Групповые заголовки (<see cref="GroupHeaderRow"/>) строятся в C# путём
    /// однопроходного детектирования смены группового ключа.
    /// В отличие от <see cref="BuildAllGroupedRowsForPrint"/> не делает N запросов
    /// на каждую листовую группу.
    /// </summary>
    private async Task<List<IKescoGridRow>> BuildAllGroupedRowsForExcel()
    {
        var selectSql     = Grid?.SelectSql     ?? string.Empty;
        var searchColumns = Grid?.SearchColumns ?? [];
        var defaultOrder  = Grid?.DefaultOrder  ?? string.Empty;

        var searchWhere    = _query.BuildWhereClause(searchColumns);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);

        var groupCols = _query.GroupColumns.ToList();

        // ── Запрос 1: GROUP BY агрегаты ─────────────────────────────
        var groupSql  = KescoGroupingEngine.BuildGroupAggregateSql(
            selectSql, groupCols, where, _query.SortColumns);
        var groupRows = await Db.QueryAsync<GridGroupRow>(groupSql, dp);

        var aggregates = KescoGroupingEngine.BuildAggregates(groupRows);
        var roots      = KescoGroupingEngine.BuildTree(aggregates);
        KescoGroupingEngine.ComputeParentCounts(roots);

        // FullKey → ItemCount (DFS по дереву — листовые и родительские узлы)
        var countLookup = new Dictionary<string, int>();
        CollectCounts(roots, countLookup);

        // ── Запрос 2: все строки одним запросом ─────────────────────
        // BuildOrderBy с GroupEnabled=true ставит групповые колонки первыми
        // (с учётом направления сортировки) — данные приходят уже сгруппированными
        var orderBy = _query.BuildOrderBy(defaultOrder);
        var flatSql = $"SELECT * FROM ({selectSql}) _src";
        if (!string.IsNullOrWhiteSpace(where))
            flatSql += $" WHERE {where}";
        if (!string.IsNullOrWhiteSpace(orderBy))
            flatSql += $" ORDER BY {orderBy}";

        var items = await Db.QueryAsync<T>(flatSql, dp);

        // ── C# interleaving: групповые заголовки + строки ───────────
        var result     = new List<IKescoGridRow>();
        string?[]? previousKeys = null;

        foreach (var item in items)
        {
            // Извлекаем значения групповых колонок из свойств сущности
            var currentKeys = groupCols
                .Select(c => _propertyMap.TryGetValue(c, out var p)
                    ? p.GetValue(item)?.ToString()
                    : null)
                .ToArray();

            // Находим первый уровень, где групповой ключ изменился
            int firstDiff = 0;
            if (previousKeys is not null)
            {
                while (firstDiff < previousKeys.Length
                       && firstDiff < currentKeys.Length
                       && string.Equals(previousKeys[firstDiff], currentKeys[firstDiff]))
                    firstDiff++;
            }

            // Эмитим заголовки для новых или изменившихся групп
            for (int depth = firstDiff; depth < groupCols.Count; depth++)
            {
                var keys         = currentKeys.Take(depth + 1).ToList();
                var displayValue = keys[depth] ?? "(пусто)";
                var fullKey      = string.Join("", keys);

                result.Add(new GroupHeaderRow
                {
                    DisplayValue = displayValue,
                    FullKey      = fullKey,
                    ItemCount    = countLookup.TryGetValue(fullKey, out var cnt) ? cnt : 0,
                    Depth        = depth,
                    GroupKeys    = keys!,
                });
            }

            result.Add(new DetailRow<T> { Item = item });
            previousKeys = currentKeys;
        }

        return result;
    }

    /// <summary>
    /// Рекурсивно собирает <c>FullKey → ItemCount</c> из дерева групп
    /// (<see cref="GridGroupNode"/>), включая родительские и листовые узлы.
    /// </summary>
    private static void CollectCounts(List<GridGroupNode> nodes, Dictionary<string, int> lookup)
    {
        foreach (var node in nodes)
        {
            lookup[node.Aggregate.FullKey] = node.Aggregate.ItemCount;
            CollectCounts(node.Children, lookup);
        }
    }

    /// <summary>
    /// Строит список строк для экспорта. Если активна группировка — для каждой
    /// развёрнутой группы загружает ВСЕ детальные строки (игнорируя пагинацию),
    /// а не только те, что уместились на текущей странице.
    /// На грид это не влияет — данные загружаются отдельным запросом.
    /// </summary>
    private async Task<List<IKescoGridRow>> BuildExportRows()
    {
        if (!_query.GroupEnabled || _query.GroupColumns.Count == 0)
            return _rows;

        var result      = new List<IKescoGridRow>();
        var expandedSet = _query.ExpandedGroups;

        var selectSql     = Grid?.SelectSql     ?? "";
        var defaultOrder  = Grid?.DefaultOrder  ?? "";
        var searchColumns = Grid?.SearchColumns ?? [];

        var searchWhere    = _query.BuildWhereClause(searchColumns);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);
        var detailOrder    = KescoGroupingEngine.BuildDetailOrder(
            _query.BuildOrderBy(defaultOrder), _query.GroupColumns, defaultOrder);

        var groupCols = _query.GroupColumns;

        foreach (var row in _rows)
        {
            if (row is GroupHeaderRow header)
            {
                result.Add(header);

                // Детальные строки загружаются только для конечных групп
                // (количество ключей == количество колонок группировки).
                // Промежуточные группы выводятся только как заголовки.
                if (header.GroupKeys.Count == groupCols.Count)
                {
                    var detailParams = new DynamicParameters();
                    detailParams.AddDynamicParams(dp);

                    for (int i = 0; i < header.GroupKeys.Count && i < groupCols.Count; i++)
                    {
                        detailParams.Add($"dk{i}", header.GroupKeys[i]);
                    }

                    var keyParts = new List<string>();
                    for (int i = 0; i < header.GroupKeys.Count && i < groupCols.Count; i++)
                        keyParts.Add($"{groupCols[i]} = @dk{i}");

                    var detailWhere = KescoDataQuery.CombineWhere(where,
                        string.Join(" AND ", keyParts));

                    // Чистый SQL без ROW_NUMBER() / BETWEEN — без пагинационной обёртки
                    var sql = $"SELECT * FROM ({selectSql}) _src";
                    if (!string.IsNullOrWhiteSpace(detailWhere))
                        sql += $" WHERE {detailWhere}";
                    if (!string.IsNullOrWhiteSpace(detailOrder))
                        sql += $" ORDER BY {detailOrder}";

                    var items = await Db.QueryAsync<T>(sql, detailParams);
                    result.AddRange(items.Select(item => new DetailRow<T>
                    {
                        Item  = item,
                        Depth = header.Depth
                    }));
                }
            }
        }

        return result;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
    }

    // ── Per-level expand/collapse (переключатели на чипах трея группировки) ────

    /// <summary>
    /// Возвращает словарь глубина → список FullKey всех групп на этой глубине.
    /// Строится рекурсивным обходом кешированного дерева групп (<see cref="_groupTreeRoots"/>).
    /// </summary>
    private Dictionary<int, List<string>> GetGroupKeysByDepth()
    {
        if (_groupKeysByDepth is not null) return _groupKeysByDepth;
        _groupKeysByDepth = new Dictionary<int, List<string>>();
        if (_groupTreeRoots is not null)
            CollectKeysByDepth(_groupTreeRoots, _groupKeysByDepth);
        return _groupKeysByDepth;
    }

    /// <summary>
    /// Рекурсивно собирает FullKey групп из дерева, группируя их по глубине.
    /// </summary>
    private static void CollectKeysByDepth(
        List<GridGroupNode> nodes, Dictionary<int, List<string>> result)
    {
        foreach (var node in nodes)
        {
            var d = node.Aggregate.Depth;
            if (!result.ContainsKey(d)) result[d] = [];
            result[d].Add(node.Aggregate.FullKey);
            CollectKeysByDepth(node.Children, result);
        }
    }

    /// <summary>
    /// Проверяет, развёрнуты ли ВСЕ группы на указанной глубине.
    /// </summary>
    bool IKescoGridDataLoader.IsLevelFullyExpanded(int depth)
    {
        var map = GetGroupKeysByDepth();
        return map.TryGetValue(depth, out var keys) && keys.Count > 0
            && keys.All(k => _query.ExpandedGroups.Contains(k));
    }

    /// <summary>
    /// Переключает состояние ВСЕХ групп на указанной глубине.
    /// При разворачивании — каскадно разворачивает родительские уровни (0..depth-1).
    /// При сворачивании — сворачивает только этот уровень.
    /// Сбрасывает страницу на 1 и перезагружает данные.
    /// </summary>
    async Task IKescoGridDataLoader.ToggleLevelExpandedAsync(int depth)
    {
        var map = GetGroupKeysByDepth();
        if (!map.TryGetValue(depth, out var keys) || keys.Count == 0) return;

        bool allExpanded = keys.All(k => _query.ExpandedGroups.Contains(k));

        if (allExpanded)
        {
            foreach (var k in keys) _query.ExpandedGroups.Remove(k);
        }
        else
        {
            // Каскад вверх: разворачиваем все родительские уровни
            for (int d = 0; d <= depth; d++)
                if (map.TryGetValue(d, out var levelKeys))
                    foreach (var k in levelKeys) _query.ExpandedGroups.Add(k);
        }

        _query.PageNumber = 1;
        await LoadData();
    }

    // ── Инфраструктура (не переопределяются на странице) ────────────────────────

    /// <summary>
    /// Загружает данные при первом рендере.
    /// Guard <c>firstRender</c> предотвращает двойную загрузку при Blazor prerendering.
    /// К этому моменту <see cref="Grid"/> уже установлен через <c>@ref</c>.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadData();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Диспетчер загрузки: вызывает <see cref="LoadGroupedData"/> или <see cref="LoadFlatData"/>
    /// в зависимости от состояния группировки в <see cref="_query"/>.
    /// Управляет <see cref="_loading"/> и вызывает <c>StateHasChanged</c> по завершении.
    /// </summary>
    protected async Task LoadData()
    {
        _loading = true;
        try
        {
            if (_query.GroupEnabled && _query.GroupColumns.Count > 0)
                await LoadGroupedData();
            else
                await LoadFlatData();
        }
        catch
        {
            // DbManager автоматически передаёт SqlException в ISqlErrorHandler → KescoErrorService.
        }
        finally
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Обрабатывает раскрытие/сворачивание группы в гриде.
    /// При разворачивании последней группы на странице автоматически переходит вперёд.
    /// При сворачивании группы, если страница вышла за пределы — возвращается на последнюю.
    /// </summary>
    protected async Task ToggleGroup(GroupHeaderRow header)
    {
        var wasExpanded = _query.ExpandedGroups.Contains(header.FullKey);
        if (wasExpanded)
            _query.ExpandedGroups.Remove(header.FullKey);
        else
            _query.ExpandedGroups.Add(header.FullKey);

        await LoadData();

        if (!wasExpanded)
        {
            var expandedHeader = _rows.OfType<GroupHeaderRow>()
                .FirstOrDefault(h => h.FullKey == header.FullKey);
            if (expandedHeader is not null)
            {
                var headerIdx = _rows.IndexOf(expandedHeader);
                if (headerIdx >= 0 && headerIdx == _rows.Count - 1 && header.ItemCount > 0)
                {
                    _query.PageNumber++;
                    await LoadData();
                }
            }
        }
        else if (_query.TotalCount > 0)
        {
            int maxPage = (int)Math.Ceiling((double)_query.TotalCount / _query.PageSize);
            if (_query.PageNumber > maxPage)
            {
                _query.PageNumber = maxPage;
                await LoadData();
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    // ── Диалоги редактирования ───────────────────────────────────────────────────

    /// <summary>
    /// Открывает диалог добавления новой записи типа <typeparamref name="T"/>.
    /// Тип диалога берётся из параметра <c>EditDialogType</c> грида.
    /// После подтверждения показывает уведомление и перезагружает данные.
    /// </summary>
    protected async Task OpenAddDialog()
    {
        var dialogType = Grid?.EditDialogType;
        if (dialogType is null) return;

        var parameters = new DialogParameters { ["Model"] = Activator.CreateInstance<T>() };
        var options = new DialogOptionsEx { MaxWidth = MaxWidth.Small, FullWidth = true, DragMode = MudDialogDragMode.Simple };
        var dialog = await DialogService.ShowExAsync(dialogType, string.Empty, parameters, options);
        if (!(await dialog.Result)?.Canceled ?? false)
        {
            Snackbar.Add(AddSuccessMessage, Severity.Success);
            await LoadData();
        }
    }

    /// <summary>
    /// Обрабатывает клик по строке грида:
    /// если строка — заголовок группы, переключает её раскрытие/сворачивание;
    /// если строка — детальная запись, открывает диалог редактирования.
    /// Тип диалога берётся из параметра <c>EditDialogType</c> грида.
    /// После подтверждения редактирования показывает уведомление и перезагружает данные.
    /// </summary>
    protected async Task OnRowClicked(DataGridRowClickEventArgs<IKescoGridRow> args)
    {
        if (args.Item is GroupHeaderRow header)
        {
            await ToggleGroup(header);
            return;
        }

        if (args.Item is DetailRow<T> detail)
        {
            var dialogType = Grid?.EditDialogType;
            if (dialogType is null) return;

            var parameters = new DialogParameters { ["Model"] = detail.Item };
            var options = new DialogOptionsEx { MaxWidth = MaxWidth.Small, FullWidth = true, DragMode = MudDialogDragMode.Simple };
            var dialog = await DialogService.ShowExAsync(dialogType, string.Empty, parameters, options);
            if (!(await dialog.Result)?.Canceled ?? false)
                Snackbar.Add(SaveSuccessMessage, Severity.Success);
            await LoadData();
        }
    }

    // ── Загрузка данных (реализованы здесь, не переопределяются) ────────────────

    /// <summary>
    /// Плоский режим: загружает страницу записей с учётом поиска и фильтрации по колонкам.
    /// SQL-параметры читаются из <see cref="Grid"/>.
    /// </summary>
    private async Task LoadFlatData()
    {
        _groupTreeRoots   = null;
        _groupKeysByDepth = null;

        var selectSql     = Grid?.SelectSql     ?? string.Empty;
        var searchColumns = Grid?.SearchColumns ?? [];
        var defaultOrder  = Grid?.DefaultOrder  ?? string.Empty;

        var searchWhere    = _query.BuildWhereClause(searchColumns);
        var orderBy        = _query.BuildOrderBy(defaultOrder);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);

        _query.TotalCount = await Entity.GetCountAsync<T>(Db, selectSql, where, dp);
        var items         = await Entity.GetPagedAsync<T>(Db, selectSql, where, orderBy, dp, _query.PageNumber, _query.PageSize);
        _rows             = items.Select(i => (IKescoGridRow)new DetailRow<T> { Item = i }).ToList();
    }

    /// <summary>
    /// Режим группировки: строит агрегатный SQL, формирует дерево групп через
    /// <see cref="KescoGroupingEngine"/>, загружает видимые на текущей странице
    /// детальные строки и собирает итоговый плоский список <see cref="_rows"/>.
    /// SQL-параметры читаются из <see cref="Grid"/>.
    /// </summary>
    private async Task LoadGroupedData()
    {
        var selectSql     = Grid?.SelectSql     ?? string.Empty;
        var searchColumns = Grid?.SearchColumns ?? [];
        var defaultOrder  = Grid?.DefaultOrder  ?? string.Empty;

        var searchWhere    = _query.BuildWhereClause(searchColumns);
        var orderBy        = _query.BuildOrderBy(defaultOrder);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);

        // GroupColumns — это выходные имена колонок SELECT, они же используются в GROUP BY
        var exprs = _query.GroupColumns.ToList();

        // Шаг 1: агрегатный запрос GROUP BY
        var groupSql  = KescoGroupingEngine.BuildGroupAggregateSql(selectSql, exprs, where, _query.SortColumns);
        var groupRows = await Db.QueryAsync<GridGroupRow>(groupSql, dp);

        // Шаг 2: строим дерево групп
        var aggregates = KescoGroupingEngine.BuildAggregates(groupRows);
        var roots      = KescoGroupingEngine.BuildTree(aggregates);
        KescoGroupingEngine.ComputeParentCounts(roots);
        _groupTreeRoots    = roots;
        _groupKeysByDepth  = null;

        // Шаг 3: страничная разметка
        int totalEffective = roots.Sum(r => KescoGroupingEngine.ComputeEffectiveRows(r, _query.ExpandedGroups));
        int pageStart      = (_query.PageNumber - 1) * _query.PageSize + 1;
        int pageEnd        = _query.PageNumber * _query.PageSize;
        var layout         = new List<GridLayoutItem>();
        int cur            = 1;
        KescoGroupingEngine.WalkTree(roots, _query.ExpandedGroups, pageStart, pageEnd, ref cur, layout);

        // Шаг 4: загружаем детальные строки для раскрытых групп
        var newRows     = new List<IKescoGridRow>();
        var detailOrder = KescoGroupingEngine.BuildDetailOrder(orderBy, _query.GroupColumns, defaultOrder);

        foreach (var item in layout)
        {
            if (item.Header is not null)
                newRows.Add(item.Header);

            if (item.HasDetailRange && item.Aggregate is not null)
            {
                var ag           = item.Aggregate;
                var detailParams = new DynamicParameters();
                detailParams.AddDynamicParams(dp);

                var keyParts = ag.RawKeys
                    .Select((k, i) => { detailParams.Add($"dk{i}", k); return $"{exprs[i]} = @dk{i}"; })
                    .ToList();
                var detailWhere = KescoDataQuery.CombineWhere(where, string.Join(" AND ", keyParts));

                detailParams.Add("__start", item.DetailStart);
                detailParams.Add("__end",   item.DetailEnd);

                var sql   = KescoGroupingEngine.BuildDetailPageSql(selectSql, detailWhere, detailOrder);
                var rows  = await Db.QueryAsync<T>(sql, detailParams);
                newRows.AddRange(rows.Select(i => new DetailRow<T> { Item = i, Depth = ag.Depth }));
            }
        }

        _rows             = newRows;
        _query.TotalCount = totalEffective;
    }
}
