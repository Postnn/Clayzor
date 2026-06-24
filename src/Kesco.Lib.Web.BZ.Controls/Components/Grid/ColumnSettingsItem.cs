namespace Kesco.Lib.Web.BZ.Controls.Components.Grid;

/// <summary>
/// Элемент списка в диалоге настройки колонок KescoGrid.
/// </summary>
public class ColumnSettingsItem
{
    /// <summary>SQL-имя колонки (выходное имя из SELECT).</summary>
    public string SqlName { get; init; } = "";

    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Видимость колонки в гриде.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Переключатель заблокирован (колонка в группировке).</summary>
    public bool IsReadonly { get; set; }
}
