using Dapper;

namespace Kesco.Lib.Web.BZ.Controls;

/// <summary>
/// Параметры одной колонки сортировки: имя SQL-колонки и направление.
/// </summary>
/// <param name="Column">Имя SQL-колонки (русское название из БД).</param>
/// <param name="Desc">true — сортировка по убыванию, false — по возрастанию.</param>
public sealed record SortColumn(string Column, bool Desc);

/// <summary>
/// Тип данных колонки — определяет доступные операторы и поле ввода в диалоге фильтрации.
/// </summary>
public enum ColumnType
{
    /// <summary>Текстовая колонка — операторы Contains/Equals/StartsWith/EndsWith/NotEquals.</summary>
    Text,
    /// <summary>Числовая колонка — операторы Equals/NotEquals/GreaterThan/GreaterThanOrEqual/LessThan/LessThanOrEqual.</summary>
    Number,
    /// <summary>Булевая колонка — оператор Equals, значение Да/Нет.</summary>
    Boolean,
}

/// <summary>
/// Оператор сравнения для фильтра по колонке.
/// </summary>
public enum ColumnFilterOperator
{
    /// <summary>Содержит подстроку (LIKE).</summary>
    Contains,
    /// <summary>Равно.</summary>
    Equals,
    /// <summary>Начинается с.</summary>
    StartsWith,
    /// <summary>Заканчивается на.</summary>
    EndsWith,
    /// <summary>Больше (&gt;).</summary>
    GreaterThan,
    /// <summary>Больше или равно (&gt;=).</summary>
    GreaterThanOrEqual,
    /// <summary>Меньше (&lt;).</summary>
    LessThan,
    /// <summary>Меньше или равно (&lt;=).</summary>
    LessThanOrEqual,
    /// <summary>Не равно.</summary>
    NotEquals,
}

/// <summary>
/// Условие фильтрации по одной SQL-колонке.
/// </summary>
public sealed class ColumnFilter
{
    /// <summary>SQL-имя колонки (например, "НазваниеАнализа" или "a.НазваниеАнализа").</summary>
    public string Column { get; set; } = "";

    /// <summary>Имя Dapper-параметра для значения фильтра (без @, уникальное в запросе).</summary>
    public string ParamName { get; set; } = "";

    /// <summary>Значение фильтра. null или пустая строка — фильтр не активен.</summary>
    public object? Value { get; set; }

    /// <summary>Оператор сравнения.</summary>
    public ColumnFilterOperator Operator { get; set; } = ColumnFilterOperator.Contains;

    /// <summary>Возвращает true, если фильтр имеет значимое значение.</summary>
    public bool HasValue => Value is not null && Value.ToString() is { Length: > 0 };
}

/// <summary>
/// Текущее состояние запроса к данным (поиск, группировка, сортировка, фильтрация по колонкам).
/// Предоставляет методы <see cref="BuildOrderBy"/>, <see cref="BuildWhereClause"/>
/// и <see cref="BuildColumnFilterClause"/> для генерации фрагментов SQL.
/// </summary>
public sealed class KescoDataQuery
{
    /// <summary>Текст поискового запроса. null или пустая строка — поиск не активен.</summary>
    public string? SearchText { get; set; }

    /// <summary>Включена ли группировка данных.</summary>
    public bool GroupEnabled { get; set; }

    /// <summary>SQL-имена колонок, по которым выполняется группировка (в порядке приоритета).</summary>
    public List<string> GroupColumns { get; set; } = [];

    /// <summary>Набор развёрнутых групп (полные ключи через \u001F). Пустой = все свёрнуты.</summary>
    public HashSet<string> ExpandedGroups { get; set; } = [];

    /// <summary>Список колонок сортировки в порядке приоритета.</summary>
    public List<SortColumn> SortColumns { get; set; } = [];

    /// <summary>Номер текущей страницы (1-based).</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Размер страницы (количество записей).</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Общее количество записей, соответствующих запросу. Заполняется страницей после загрузки данных.</summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Условия фильтрации по отдельным колонкам. Ключ — SQL-имя колонки, значение — условие фильтра.
    /// Управляется страницей; KescoGrid не изменяет этот словарь.
    /// </summary>
    public Dictionary<string, ColumnFilter> ColumnFilters { get; set; } = [];

    /// <summary>
    /// Строит фрагмент WHERE для фильтрации по колонкам из <see cref="ColumnFilters"/>.
    /// Возвращает null, если нет активных фильтров.
    /// Параметры добавляются в <paramref name="parameters"/> через Dapper <c>DynamicParameters</c>.
    /// </summary>
    /// <param name="parameters">Объект DynamicParameters, в который добавляются параметры фильтра.</param>
    /// <param name="columnNameMap">
    /// Необязательный маппинг SQL-имён колонок: ключ — имя из <see cref="ColumnFilter.Column"/>,
    /// значение — имя для подстановки в SQL-выражение.
    /// Используется в плоском режиме, где имена колонок отличаются от подзапросного режима
    /// (например, <c>"TestTypeName"</c> → <c>"t.ТипМедицинскогоАнализа"</c>).
    /// </param>
    /// <returns>Строка для вставки в WHERE (без ключевого слова WHERE), либо null.</returns>
    public string? BuildColumnFilterClause(DynamicParameters parameters,
        Dictionary<string, string>? columnNameMap = null)
    {
        var parts = new List<string>();
        foreach (var cf in ColumnFilters.Values)
        {
            if (!cf.HasValue) continue;
            // Применяем маппинг имён если задан
            var colName = columnNameMap is not null && columnNameMap.TryGetValue(cf.Column, out var mapped)
                ? mapped
                : cf.Column;
            string expr;
            switch (cf.Operator)
            {
                case ColumnFilterOperator.Contains:
                    parameters.Add(cf.ParamName, $"%{cf.Value}%");
                    expr = $"{colName} LIKE @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.StartsWith:
                    parameters.Add(cf.ParamName, $"{cf.Value}%");
                    expr = $"{colName} LIKE @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.EndsWith:
                    parameters.Add(cf.ParamName, $"%{cf.Value}");
                    expr = $"{colName} LIKE @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.Equals:
                    parameters.Add(cf.ParamName, cf.Value);
                    expr = $"{colName} = @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.NotEquals:
                    parameters.Add(cf.ParamName, cf.Value);
                    expr = $"{colName} <> @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.GreaterThan:
                    parameters.Add(cf.ParamName, cf.Value);
                    expr = $"{colName} > @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.GreaterThanOrEqual:
                    parameters.Add(cf.ParamName, cf.Value);
                    expr = $"{colName} >= @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.LessThan:
                    parameters.Add(cf.ParamName, cf.Value);
                    expr = $"{colName} < @{cf.ParamName}";
                    break;
                case ColumnFilterOperator.LessThanOrEqual:
                    parameters.Add(cf.ParamName, cf.Value);
                    expr = $"{colName} <= @{cf.ParamName}";
                    break;
                default:
                    parameters.Add(cf.ParamName, cf.Value);
                    expr = $"{colName} = @{cf.ParamName}";
                    break;
            }
            parts.Add(expr);
        }
        return parts.Count > 0 ? string.Join(" AND ", parts) : null;
    }

    /// <summary>
    /// Строит фрагмент ORDER BY с учётом группировки и сортировки.
    /// Если группировка включена, её колонка идёт первой.
    /// Если сортировка не задана, используется <paramref name="defaultOrder"/>.
    /// </summary>
    /// <param name="defaultOrder">Порядок сортировки по умолчанию, например "Порядок, НазваниеАнализа".</param>
    /// <returns>Строка для вставки в ORDER BY.</returns>
    public string BuildOrderBy(string defaultOrder)
    {
        var clauses = new List<string>();

        if (GroupEnabled && GroupColumns.Count > 0)
        {
            foreach (var gc in GroupColumns)
            {
                var sortCol = SortColumns.Find(s => s.Column == gc);
                if (sortCol is not null)
                    clauses.Add($"{gc} {(sortCol.Desc ? "DESC" : "ASC")}");
                else
                    clauses.Add(gc);
            }
        }

        if (SortColumns.Count == 0)
        {
            foreach (var col in defaultOrder.Split(", ", StringSplitOptions.RemoveEmptyEntries))
            {
                if (GroupEnabled && GroupColumns.Contains(col))
                    continue;
                clauses.Add(col);
            }
        }
        else
        {
            foreach (var s in SortColumns)
            {
                if (GroupEnabled && GroupColumns.Contains(s.Column))
                    continue;
                clauses.Add($"{s.Column} {(s.Desc ? "DESC" : "ASC")}");
            }
        }

        return string.Join(", ", clauses);
    }

    /// <summary>
    /// Строит фрагмент WHERE с LIKE-поиском по указанным колонкам.
    /// Возвращает null, если текст поиска пуст.
    /// Использует параметр @search, значение подставляется через Dapper.
    /// </summary>
    /// <param name="searchColumns">Имена SQL-колонок для поиска, например "a.НазваниеАнализа", "t.ТипМедицинскогоАнализа".</param>
    /// <returns>Строка для вставки в WHERE, либо null.</returns>
    public string? BuildWhereClause(params string[] searchColumns)
    {
        if (string.IsNullOrWhiteSpace(SearchText) || searchColumns.Length == 0)
            return null;

        return string.Join(" OR ", searchColumns.Select(c => $"{c} LIKE @search"));
    }

    /// <summary>
    /// Объединяет два WHERE-фрагмента через AND.
    /// Если оба null — возвращает null.
    /// Если один null — возвращает другой без обёртки в скобки.
    /// Если оба не null — возвращает <c>({a}) AND ({b})</c>.
    /// </summary>
    /// <param name="a">Первый WHERE-фрагмент (например, из <see cref="BuildWhereClause"/>).</param>
    /// <param name="b">Второй WHERE-фрагмент (например, из <see cref="BuildColumnFilterClause"/>).</param>
    public static string? CombineWhere(string? a, string? b)
    {
        if (a is null && b is null) return null;
        if (a is null) return b;
        if (b is null) return a;
        return $"({a}) AND ({b})";
    }
}
