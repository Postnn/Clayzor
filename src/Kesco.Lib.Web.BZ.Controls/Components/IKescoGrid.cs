using Microsoft.AspNetCore.Components;

namespace Kesco.Lib.Web.BZ.Controls;

/// <summary>
/// Интерфейс регистрации метаданных колонок и настроек источника данных,
/// реализуемый <see cref="KescoGrid{TEntity}"/>.
/// Используется <see cref="KescoColumnDef"/> для регистрации колонок через каскадный параметр,
/// и <see cref="KescoGridPageBase{T}"/> для чтения SQL-настроек грида.
/// </summary>
public interface IKescoGrid
{
    /// <summary>Базовый SQL-запрос SELECT (без WHERE / ORDER BY).</summary>
    string SelectSql { get; }

    /// <summary>Выходные имена колонок для полнотекстового поиска.</summary>
    string[] SearchColumns { get; }

    /// <summary>Порядок сортировки по умолчанию.</summary>
    string DefaultOrder { get; }

    /// <summary>
    /// Тип компонента диалога редактирования.
    /// Диалог должен принимать параметр <c>Model</c> типа сущности.
    /// </summary>
    Type? EditDialogType { get; }

    /// <summary>
    /// Возвращает <c>true</c> если колонка в данный момент участвует в группировке.
    /// Используется для <c>Hidden</c> на колонках грида:
    /// <code>Hidden="@(_dataGrid?.IsGrouped("SqlName") ?? false)"</code>
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    bool IsGrouped(string sqlName);

    /// <summary>
    /// Переключает серверную сортировку по колонке: нет → ASC → DESC → нет.
    /// Вызывается из <see cref="KescoSortableColumnHeader"/> при клике на заголовок.
    /// </summary>
    /// <param name="column">SQL-имя колонки.</param>
    Task ToggleSort(string column);

    /// <summary>
    /// Возвращает фрагмент разметки с бейджем сортировки для колонки
    /// (номер приоритета + стрелка направления), либо пустой фрагмент если колонка не сортируется.
    /// Вызывается из <see cref="KescoSortableColumnHeader"/>.
    /// </summary>
    /// <param name="column">SQL-имя колонки.</param>
    RenderFragment GetSortBadge(string column);

    /// <summary>
    /// Регистрирует колонку в гриде.
    /// Вызывается из <see cref="KescoColumnDef.OnInitialized"/>.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки (выходное имя SELECT).</param>
    /// <param name="displayName">Отображаемое имя (для чипов в треях группировки и фильтрации).</param>
    /// <param name="groupable">Разрешить группировку по этой колонке.</param>
    /// <param name="filterable">Разрешить фильтрацию по этой колонке.</param>
    void RegisterColumn(string sqlName, string displayName, bool groupable, bool filterable);

    /// <summary>
    /// Отменяет регистрацию колонки.
    /// Вызывается из <see cref="KescoColumnDef.Dispose"/>.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    void UnregisterColumn(string sqlName);
}
