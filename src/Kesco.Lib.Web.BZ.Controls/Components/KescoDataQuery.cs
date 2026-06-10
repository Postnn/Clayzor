namespace Kesco.Lib.Web.BZ.Controls;

/// <summary>
/// Параметры одной колонки сортировки: имя SQL-колонки и направление.
/// </summary>
/// <param name="Column">Имя SQL-колонки (русское название из БД).</param>
/// <param name="Desc">true — сортировка по убыванию, false — по возрастанию.</param>
public sealed record SortColumn(string Column, bool Desc);

/// <summary>
/// Текущее состояние запроса к данным (поиск, группировка, сортировка).
/// Предоставляет методы <see cref="BuildOrderBy"/> и <see cref="BuildWhereClause"/>
/// для генерации фрагментов SQL на стороне страницы.
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
}
