using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using Kesco.Lib.Entities;
using Kesco.Lib.Web.BZ.Controls.Components.Grid;

namespace Kesco.Lib.Web.BZ.Controls.Services;

/// <summary>
/// Генератор HTML-документа для печати всех данных грида Kesco.
/// Строит самодостаточный HTML с инлайн-стилями (не зависит от app.css),
/// вставляемый в скрытый iframe перед window.print().
/// </summary>
public static class KescoGridPrintHtmlGenerator
{
    // ── Цвета и типографика (повторяют @media print из app.css) ──────────
    private const string FontFamily = "Verdana, Arial, sans-serif";

    private const string HeaderBg     = "#222222";
    private const string HeaderColor  = "#ffffff";
    private const string BodyColor    = "#000000";
    private const string CellBorder   = "1px solid #cccccc";
    private const string GroupBg      = "#e8e8e8";
    private const string GroupColor   = "#000000";

    /// <summary>
    /// Генерирует полный HTML-документ для печати.
    /// </summary>
    /// <param name="title">Заголовок грида (первая строка).</param>
    /// <param name="columns">Видимые колонки в порядке отображения.</param>
    /// <param name="rows">Строки данных (заголовки групп + строки детализации).</param>
    /// <param name="entityType">Тип сущности для маппинга SqlName → свойство.</param>
    /// <param name="expandedGroups">FullKey развёрнутых групп (свёрнутые — только заголовок).</param>
    /// <param name="filterDescription">Описание активных фильтров (или null).</param>
    /// <param name="groupDescription">Описание колонок группировки (или null).</param>
    public static string Build(
        string title,
        IReadOnlyList<KescoColumnMeta> columns,
        IReadOnlyList<IGridRow> rows,
        Type entityType,
        HashSet<string>? expandedGroups = null,
        string? filterDescription = null,
        string? groupDescription = null)
    {
        int colCount = columns.Count;
        if (colCount == 0) return "<html><body></body></html>";

        var propMap = BuildPropertyMap(entityType);
        var sb      = new StringBuilder();

        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        sb.Append("<style>");
        AppendPrintStyles(sb);
        sb.Append("</style></head><body>");

        // Корневой контейнер
        sb.Append("<div style=\"padding:8px;font-family:").Append(FontFamily).Append(";font-size:9pt;\">");

        // ── Заголовок ────────────────────────────────────────────────
        sb.Append("<div style=\"font-size:16pt;font-weight:bold;color:#000;margin-bottom:12px;\">")
          .Append(EscapeHtml(title)).Append("</div>");

        // ── Описание группировки (опционально) ──────────────────────
        if (!string.IsNullOrWhiteSpace(groupDescription))
        {
            sb.Append("<div style=\"font-size:8pt;color:#555;margin-bottom:4px;\">")
              .Append(EscapeHtml(groupDescription)).Append("</div>");
        }

        // ── Описание фильтра (опционально) ──────────────────────────
        if (!string.IsNullOrWhiteSpace(filterDescription))
        {
            sb.Append("<div style=\"font-size:8pt;color:#555;margin-bottom:4px;\">")
              .Append(EscapeHtml(filterDescription)).Append("</div>");
        }

        // ── Таблица ──────────────────────────────────────────────────
        sb.Append("<table style=\"width:100%;border-collapse:collapse;background:#fff;\">");

        // THEAD
        sb.Append("<thead><tr>");
        for (int c = 0; c < colCount; c++)
        {
            sb.Append("<th style=\"background:").Append(HeaderBg)
              .Append(";color:").Append(HeaderColor)
              .Append(";font-weight:bold;font-size:9pt;padding:6px 8px;text-align:center;")
              .Append("border-bottom:2px solid #000;")
              .Append("border-right:").Append(CellBorder)
              .Append(";-webkit-print-color-adjust:exact;print-color-adjust:exact;\">")
              .Append(EscapeHtml(columns[c].DisplayName))
              .Append("</th>");
        }
        sb.Append("</tr></thead>");

        // TBODY
        sb.Append("<tbody>");
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];

            if (row is GroupHeaderRow gh)
            {
                AppendGroupRow(sb, gh, colCount);
            }
            else if (row is IDetailRow detailRow)
            {
                AppendDetailRow(sb, detailRow, columns, propMap);
            }
        }
        sb.Append("</tbody>");

        sb.Append("</table>");
        sb.Append("</div>");
        sb.Append("</body></html>");

        return sb.ToString();
    }

    // ── Групповая строка ────────────────────────────────────────────────

    private static void AppendGroupRow(StringBuilder sb, GroupHeaderRow gh, int colCount)
    {
        int indentPx = gh.Depth * 16;
        sb.Append("<tr style=\"background:").Append(GroupBg)
          .Append(";border-top:2px solid #000;\">");
        sb.Append("<td colspan=\"").Append(colCount)
          .Append("\" style=\"font-weight:bold;color:").Append(GroupColor)
          .Append(";font-size:9pt;padding:5px 8px 5px ").Append(8 + indentPx)
          .Append("px;border-bottom:").Append(CellBorder)
          .Append(";border-right:").Append(CellBorder)
          .Append(";-webkit-print-color-adjust:exact;print-color-adjust:exact;\">")
          .Append(EscapeHtml(gh.DisplayValue))
          .Append(" (").Append(gh.ItemCount).Append(" шт.)")
          .Append("</td>");
        sb.Append("</tr>");
    }

    // ── Строка детализации ─────────────────────────────────────────────

    private static void AppendDetailRow(
        StringBuilder sb, IDetailRow detailRow,
        IReadOnlyList<KescoColumnMeta> columns,
        Dictionary<string, PropertyInfo> propMap)
    {
        var entity = detailRow.Item;
        if (entity is null) return;

        sb.Append("<tr style=\"page-break-inside:avoid;\">");

        for (int c = 0; c < columns.Count; c++)
        {
            var sqlName = columns[c].SqlName;
            string cellValue = "";

            if (propMap.TryGetValue(sqlName, out var prop))
            {
                var value = prop.GetValue(entity);
                cellValue = FormatCellValue(value, prop.PropertyType);
            }

            sb.Append("<td style=\"font-size:9pt;padding:4px 8px;color:").Append(BodyColor)
              .Append(";border-bottom:").Append(CellBorder)
              .Append(";border-right:").Append(CellBorder)
              .Append(";-webkit-print-color-adjust:exact;print-color-adjust:exact;\">")
              .Append(EscapeHtml(cellValue))
              .Append("</td>");
        }
        sb.Append("</tr>");
    }

    // ── Форматирование значения ячейки ──────────────────────────────────

    private static string FormatCellValue(object? value, Type propertyType)
    {
        if (value is null || value == DBNull.Value)
            return "";

        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (type == typeof(bool))
            return (bool)value ? "Да" : "Нет";
        if (type == typeof(DateTime))
            return ((DateTime)value).ToString("dd.MM.yyyy");
        if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
            return ((IFormattable)value).ToString("N2", null);

        return value.ToString() ?? "";
    }

    // ── CSS для @page и @media print внутри iframe ─────────────────────

    private static void AppendPrintStyles(StringBuilder sb)
    {
        sb.Append("@page{size:landscape;margin:15mm}");
        sb.Append("@media print{");
        sb.Append("body{background:#fff;margin:0;font-family:").Append(FontFamily).Append(";font-size:9pt;}");
        sb.Append("table{width:100%;border-collapse:collapse;}");
        sb.Append("thead{display:table-header-group;}");
        sb.Append("tr{page-break-inside:avoid;}");
        sb.Append("}");
    }

    // ── HTML-экранирование ──────────────────────────────────────────────

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return System.Net.WebUtility.HtmlEncode(text);
    }

    // ── Маппинг SqlName → PropertyInfo через [Column] атрибуты ─────────

    private static Dictionary<string, PropertyInfo> BuildPropertyMap(Type entityType)
    {
        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var sqlName = colAttr?.Name ?? prop.Name;
            map[sqlName] = prop;
        }
        return map;
    }
}
