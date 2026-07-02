using System.Text;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>
/// Кликабельный сегмент в панели фильтра.
/// Каждый сегмент соответствует одному условию (клаузе) листового узла дерева.
/// </summary>
public sealed class FilterSegment
{
    /// <summary>Отображаемый текст сегмента (например, «Название содержит «грипп»»).</summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// Источник листа. Определяет маршрутизацию клика:
    /// <see cref="KescoFilterSource.ColumnDialog"/> → диалог колонки;
    /// <see cref="KescoFilterSource.CompositeDialog"/> → составной диалог.
    /// </summary>
    public KescoFilterSource Source { get; init; }

    /// <summary>SQL-имя колонки листа (для маршрутизации к диалогу колонки).</summary>
    public string Column { get; init; } = "";
}

/// <summary>
/// Статический построитель текстового описания дерева фильтра.
/// Используется для кликабельных сегментов в панели и для строки описания (экспорт/печать).
/// </summary>
public static class KescoFilterDescriptionBuilder
{
    /// <summary>
    /// Строит плоский список кликабельных сегментов из дерева фильтра.
    /// Каждый сегмент соответствует одному условию (клаузе) листового узла.
    /// </summary>
    /// <param name="root">Корень дерева.</param>
    /// <param name="getDisplayName">Функция получения отображаемого имени по SQL-имени колонки.</param>
    /// <returns>Список сегментов в порядке обхода дерева.</returns>
    public static IReadOnlyList<FilterSegment> BuildSegments(
        KescoFilterGroupNode? root,
        Func<string, string> getDisplayName)
    {
        if (root is null || root.Nodes.Count == 0) return [];
        var result = new List<FilterSegment>();
        CollectSegments(root, getDisplayName, result);
        return result;
    }

    /// <summary>
    /// Строит полное текстовое описание дерева для экспорта/печати.
    /// Группы оборачиваются скобками; И/ИЛИ вставляются между соседними условиями.
    /// </summary>
    /// <param name="root">Корень дерева.</param>
    /// <param name="getDisplayName">Функция получения отображаемого имени по SQL-имени колонки.</param>
    /// <returns>Строка описания или null если дерево пустое.</returns>
    public static string? BuildText(
        KescoFilterGroupNode? root,
        Func<string, string> getDisplayName)
    {
        if (root is null || root.Nodes.Count == 0) return null;
        var text = BuildGroupText(root, getDisplayName, isRoot: true);
        return string.IsNullOrWhiteSpace(text) ? null : $"Фильтр: {text}";
    }

    /// <summary>Текст одного листа с обоими условиями (клаузами), напр.
    /// «Тип: содержит «прот» И содержит «моч»».</summary>
    public static string DescribeLeaf(ColumnFilter leaf, Func<string, string> getDisplayName)
        => BuildLeafText(leaf, getDisplayName);

    // ── Внутренние методы ─────────────────────────────────────────────────────

    private static void CollectSegments(
        KescoFilterGroupNode group,
        Func<string, string> getDisplayName,
        List<FilterSegment> result)
    {
        foreach (var node in group.Nodes)
        {
            if (node is ColumnFilter leaf)
                AddLeafSegments(leaf, getDisplayName, result);
            else if (node is KescoFilterGroupNode nested)
                CollectSegments(nested, getDisplayName, result);
        }
    }

    private static void AddLeafSegments(
        ColumnFilter leaf,
        Func<string, string> getDisplayName,
        List<FilterSegment> result)
    {
        var dn = getDisplayName(leaf.Column);

        // Первое условие
        var text1 = $"{dn}: {DescribeClause(leaf.Operator, leaf.Value)}";
        result.Add(new FilterSegment { Text = text1, Source = leaf.Source, Column = leaf.Column });

        // Второе условие (если есть)
        if (leaf.HasSecondClause)
        {
            var logic = leaf.LogicalOperator == LogicalOperator.Or ? "ИЛИ" : "И";
            var text2 = $"{logic} {DescribeClause(leaf.SecondOperator, leaf.SecondValue)}";
            result.Add(new FilterSegment { Text = text2, Source = leaf.Source, Column = leaf.Column });
        }
    }

    private static string BuildGroupText(
        KescoFilterGroupNode group,
        Func<string, string> getDisplayName,
        bool isRoot)
    {
        var logic = group.Logic == LogicalOperator.Or ? " ИЛИ " : " И ";
        var parts = new List<string>();

        foreach (var node in group.Nodes)
        {
            string part;
            if (node is ColumnFilter leaf)
                part = BuildLeafText(leaf, getDisplayName);
            else if (node is KescoFilterGroupNode nested)
                part = BuildGroupText(nested, getDisplayName, isRoot: false);
            else
                continue;

            if (!string.IsNullOrEmpty(part))
                parts.Add(part);
        }

        if (parts.Count == 0) return "";
        var joined = string.Join(logic, parts);
        return isRoot || parts.Count <= 1 ? joined : $"({joined})";
    }

    private static string BuildLeafText(ColumnFilter leaf, Func<string, string> getDisplayName)
    {
        var dn = getDisplayName(leaf.Column);
        var first = $"{dn}: {DescribeClause(leaf.Operator, leaf.Value)}";
        if (!leaf.HasSecondClause) return first;

        var logic = leaf.LogicalOperator == LogicalOperator.Or ? "ИЛИ" : "И";
        var second = DescribeClause(leaf.SecondOperator, leaf.SecondValue);
        return $"{first} {logic} {second}";
    }

    private static string DescribeClause(ColumnFilterOperator op, object? value)
    {
        var fmt = FormatValue(value);
        return op switch
        {
            ColumnFilterOperator.Contains           => $"содержит «{fmt}»",
            ColumnFilterOperator.NotContains        => $"не содержит «{fmt}»",
            ColumnFilterOperator.Equals             => $"= «{fmt}»",
            ColumnFilterOperator.NotEquals          => $"≠ «{fmt}»",
            ColumnFilterOperator.StartsWith         => $"начинается с «{fmt}»",
            ColumnFilterOperator.NotStartsWith      => $"не начинается с «{fmt}»",
            ColumnFilterOperator.EndsWith           => $"заканчивается на «{fmt}»",
            ColumnFilterOperator.NotEndsWith        => $"не заканчивается на «{fmt}»",
            ColumnFilterOperator.GreaterThan        => $"> «{fmt}»",
            ColumnFilterOperator.GreaterThanOrEqual => $"≥ «{fmt}»",
            ColumnFilterOperator.LessThan           => $"< «{fmt}»",
            ColumnFilterOperator.LessThanOrEqual    => $"≤ «{fmt}»",
            ColumnFilterOperator.IsEmpty            => "пустая строка",
            ColumnFilterOperator.IsNotEmpty         => "не пустая строка",
            ColumnFilterOperator.IsNull             => "пусто (NULL)",
            ColumnFilterOperator.IsNotNull          => "не пусто (NOT NULL)",
            _                                       => $"= «{fmt}»",
        };
    }

    private static string FormatValue(object? value)
        => value is DateTime dt ? dt.ToString("dd.MM.yyyy") : value?.ToString() ?? "";
}
