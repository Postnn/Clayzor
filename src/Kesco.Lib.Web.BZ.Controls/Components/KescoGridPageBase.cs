using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Dapper;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kesco.Lib.Web.BZ.Controls;

/// <summary>
/// Базовый класс Blazor-страницы с серверным гридом <see cref="KescoGrid{TEntity}"/>.
/// Инкапсулирует инфраструктуру загрузки данных: плоский режим и режим группировки.
/// <para>
/// Страница-наследник должна:
/// <list type="bullet">
///   <item>Унаследоваться: <c>@inherits KescoGridPageBase&lt;МояСущность&gt;</c></item>
///   <item>Реализовать шесть abstract/virtual свойств (см. ниже)</item>
///   <item>В обработчике <c>OnQueryChanged</c> вызвать <see cref="OnQueryChangedBase"/>:
///     <code>private Task OnQueryChanged(KescoDataQuery q) =&gt; OnQueryChangedBase(q);</code>
///   </item>
/// </list>
/// </para>
/// </summary>
/// <typeparam name="T">Тип сущности — наследник <see cref="Entity"/>.</typeparam>
public abstract class KescoGridPageBase<T> : ComponentBase where T : Entity
{
    // ── Инжектируемые сервисы ────────────────────────────────────────────────────

    /// <summary>Менеджер подключения к БД — инжектируется автоматически.</summary>
    [Inject] protected DbManager Db { get; set; } = null!;

    /// <summary>Сервис уведомлений — инжектируется автоматически.</summary>
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    // ── Общее состояние страницы ─────────────────────────────────────────────────

    /// <summary>
    /// Текущее состояние запроса к данным.
    /// Обновляется в <see cref="OnQueryChangedBase"/> при каждом взаимодействии с гридом.
    /// </summary>
    protected KescoDataQuery _query = new();

    /// <summary>Строки текущей страницы (заголовки групп + строки детализации).</summary>
    protected List<IGridRow> _rows = [];

    /// <summary>Признак загрузки данных — управляет индикатором в <see cref="KescoGrid{TEntity}"/>.</summary>
    protected bool _loading = true;

    // ── Настройки источника данных — задаются в производном классе ──────────────

    /// <summary>
    /// Базовый SQL-запрос SELECT (без WHERE / ORDER BY).
    /// Пример: <c>SQLQueries.SELECT_МедицинскиеАнализы</c>
    /// </summary>
    protected abstract string SelectSql { get; }

    /// <summary>
    /// Колонки полнотекстового поиска — выходные имена колонок SELECT без алиасов таблиц.
    /// WHERE строится поверх подзапроса <c>SELECT * FROM ({SelectSql}) _q</c>,
    /// поэтому алиасы <c>a.</c>/<c>t.</c> не нужны ни в плоском, ни в группированном режиме.
    /// Пример: <c>["НазваниеАнализа", "TestTypeName"]</c>
    /// </summary>
    protected abstract string[] SearchColumns { get; }

    /// <summary>
    /// Порядок сортировки по умолчанию (когда пользователь не задал сортировку).
    /// Пример: <c>"Порядок, НазваниеАнализа"</c>
    /// </summary>
    protected abstract string DefaultOrder { get; }

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

    // ── Инфраструктура (не переопределяются на странице) ────────────────────────

    /// <summary>
    /// Стандартный обработчик события <c>OnQueryChanged</c> грида.
    /// Копирует все поля из <paramref name="query"/> в <see cref="_query"/> и запускает загрузку.
    /// </summary>
    protected async Task OnQueryChangedBase(KescoDataQuery query)
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

    /// <summary>
    /// Загружает данные при первом рендере.
    /// Guard <c>firstRender</c> предотвращает двойную загрузку при Blazor prerendering.
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

    // ── Загрузка данных (реализованы здесь, не переопределяются) ────────────────

    /// <summary>
    /// Плоский режим: загружает страницу записей с учётом поиска и фильтрации по колонкам.
    /// WHERE применяется поверх подзапроса <c>SELECT * FROM ({SelectSql}) _q</c> —
    /// алиасы таблиц не нужны, используются выходные имена колонок.
    /// </summary>
    private async Task LoadFlatData()
    {
        var searchWhere    = _query.BuildWhereClause(SearchColumns);
        var orderBy        = _query.BuildOrderBy(DefaultOrder);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);

        _query.TotalCount = await Entity.GetCountAsync<T>(Db, SelectSql, where, dp);
        var items         = await Entity.GetPagedAsync<T>(Db, SelectSql, where, orderBy, dp, _query.PageNumber, _query.PageSize);
        _rows             = items.Select(i => (IGridRow)new DetailRow<T> { Item = i }).ToList();
    }

    /// <summary>
    /// Режим группировки: строит агрегатный SQL, формирует дерево групп через
    /// <see cref="KescoGroupingEngine"/>, загружает видимые на текущей странице
    /// детальные строки и собирает итоговый плоский список <see cref="_rows"/>.
    /// WHERE применяется поверх подзапроса — имена колонок едины с плоским режимом.
    /// </summary>
    private async Task LoadGroupedData()
    {
        var searchWhere    = _query.BuildWhereClause(SearchColumns);
        var orderBy        = _query.BuildOrderBy(DefaultOrder);
        var dp             = new DynamicParameters();
        dp.Add("search", $"%{_query.SearchText}%");
        var colFilterWhere = _query.BuildColumnFilterClause(dp);
        var where          = KescoDataQuery.CombineWhere(searchWhere, colFilterWhere);

        // GroupColumns — это выходные имена колонок SELECT, они же используются в GROUP BY
        var exprs = _query.GroupColumns.ToList();

        // Шаг 1: агрегатный запрос GROUP BY
        var groupSql  = KescoGroupingEngine.BuildGroupAggregateSql(SelectSql, exprs, where, _query.SortColumns);
        var groupRows = await Db.QueryAsync<GridGroupRow>(groupSql, dp);

        // Шаг 2: строим дерево групп
        var aggregates = KescoGroupingEngine.BuildAggregates(groupRows);
        var roots      = KescoGroupingEngine.BuildTree(aggregates);
        KescoGroupingEngine.ComputeParentCounts(roots);

        // Шаг 3: страничная разметка
        int totalEffective = roots.Sum(r => KescoGroupingEngine.ComputeEffectiveRows(r, _query.ExpandedGroups));
        int pageStart      = (_query.PageNumber - 1) * _query.PageSize + 1;
        int pageEnd        = _query.PageNumber * _query.PageSize;
        var layout         = new List<GridLayoutItem>();
        int cur            = 1;
        KescoGroupingEngine.WalkTree(roots, _query.ExpandedGroups, pageStart, pageEnd, ref cur, layout);

        // Шаг 4: загружаем детальные строки для раскрытых групп
        var newRows     = new List<IGridRow>();
        var detailOrder = KescoGroupingEngine.BuildDetailOrder(orderBy, _query.GroupColumns, DefaultOrder);

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

                var sql   = KescoGroupingEngine.BuildDetailPageSql(SelectSql, detailWhere, detailOrder);
                var rows  = await Db.QueryAsync<T>(sql, detailParams);
                newRows.AddRange(rows.Select(i => new DetailRow<T> { Item = i, Depth = ag.Depth }));
            }
        }

        _rows             = newRows;
        _query.TotalCount = totalEffective;
    }
}
