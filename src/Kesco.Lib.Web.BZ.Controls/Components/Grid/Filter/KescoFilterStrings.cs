namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>
/// Единый источник всех строковых констант для UI фильтра.
/// Никакого хардкода русских строк в .razor/.cs — только ссылки на этот класс.
/// </summary>
public static class KescoFilterStrings
{
    // ── ToggleGroup ────────────────────────────────────────────────────────

    public const string And = "И";
    public const string Or = "ИЛИ";

    // ── KescoFilterGroup ───────────────────────────────────────────────────

    public const string AddCondition    = "добавить условие";
    public const string AddGroup        = "добавить группу";
    public const string DeleteGroup     = "Удалить группу";
    public const string DeleteCondition = "Удалить условие";
    public const string DeleteValueFilter = "Удалить фильтр по значению";
    public const string ColumnDialogInfo  = "Редактируется в диалоге колонки:";
    public const string ValueFilterInfo   = "Фильтр по значению:";

    // ── KescoFilterExpression ──────────────────────────────────────────────

    public const string FieldLabel     = "Поле";
    public const string ConditionLabel = "Условие";

    // ── KescoFilterDialog ──────────────────────────────────────────────────

    public const string DialogTitle = "Настраиваемый фильтр";
    public const string Reset       = "Сбросить";
    public const string Cancel      = "Отмена";
    public const string Apply       = "Применить";

    // ── KescoGrid toolbar ──────────────────────────────────────────────────

    public const string ShowFilters = "Фильтровать";
    public const string HideFilters = "Скрыть фильтры";
}
