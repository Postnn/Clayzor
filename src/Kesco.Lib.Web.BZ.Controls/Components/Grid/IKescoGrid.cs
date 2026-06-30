using Microsoft.AspNetCore.Components;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid;

/// <summary>
/// Метаданные колонки, зарегистрированной через <see cref="KescoColumnDef"/>.
/// </summary>
public sealed class KescoColumnMeta
{
    /// <summary>
    /// Числовой идентификатор колонки — связь <see cref="KescoColumnDef"/> ↔ <see cref="KescoColumn{TEntity}"/>.
    /// </summary>
    public int ColumnId { get; init; }

    /// <summary>SQL-имя колонки — выходное имя из SELECT. Идентификатор для группировки, фильтрации, drag.</summary>
    public string SqlName { get; init; } = "";

    /// <summary>Отображаемое имя для заголовков и треев.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>
    /// Имя колонки для ORDER BY. Если не задано при регистрации, равно <see cref="SqlName"/>.
    /// </summary>
    public string SortName { get; init; } = "";

    /// <summary>Разрешена ли группировка по колонке.</summary>
    public bool Groupable { get; init; }

    /// <summary>Разрешена ли фильтрация по колонке.</summary>
    public bool Filterable { get; init; }

    /// <summary>Дескриптор типа — единый источник операторов, парсинга и формата.</summary>
    public ColumnTypes.ColumnTypeDescriptor Type { get; init; } = null!;
}

/// <summary>
/// Интерфейс регистрации метаданных колонок и настроек источника данных,
/// реализуемый <see cref="KescoGrid{TEntity}"/>.
/// Используется:
/// <list type="bullet">
///   <item><see cref="KescoColumnDef"/> — регистрация метаданных через каскадный параметр</item>
///   <item><see cref="KescoColumn{TEntity}"/> — поиск метаданных по числовому <c>ColumnId</c> для построения заголовка</item>
///   <item><see cref="KescoGridPageBase{T}"/> — чтение SQL-настроек грида</item>
/// </list>
/// </summary>
public interface IKescoGrid
{
    /// <summary>
    /// Срабатывает при регистрации или отмене регистрации любой колонки.
    /// <see cref="KescoColumn{TEntity}"/> подписывается на это событие и вызывает
    /// <c>StateHasChanged</c>, чтобы отобразить <c>DisplayName</c> после того,
    /// как <see cref="KescoColumnDef"/> зарегистрирует метаданные.
    /// </summary>
    event Action? ColumnsChanged;

    /// <summary>
    /// Срабатывает при открытии или закрытии панели группировки или фильтрации.
    /// <see cref="KescoColumn{TEntity}"/> подписывается и вызывает <c>StateHasChanged</c>,
    /// чтобы показать или скрыть кнопку меню (⋮) в заголовке колонки.
    /// </summary>
    event Action? TrayStateChanged;

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
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    bool IsGrouped(string sqlName);

    /// <summary>
    /// Переключает серверную сортировку по колонке: нет → ASC → DESC → нет.
    /// Принимает <c>sqlName</c> колонки; реальное имя для ORDER BY резолвится
    /// из <see cref="KescoColumnMeta.SortName"/>.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки (идентификатор).</param>
    Task ToggleSort(string sqlName);

    /// <summary>
    /// Возвращает фрагмент разметки с бейджем сортировки для колонки
    /// (номер приоритета + стрелка направления), либо пустой фрагмент если колонка не сортируется.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    RenderFragment GetSortBadge(string sqlName);

    /// <summary>
    /// Возвращает метаданные колонки по её <c>SqlName</c>.
    /// Используется для drag-and-drop, группировки и фильтрации.
    /// Возвращает <c>null</c>, если колонка не зарегистрирована.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    KescoColumnMeta? GetColumnMeta(string sqlName);

    /// <summary>
    /// Возвращает метаданные колонки по её числовому <c>ColumnId</c>.
    /// Используется компонентом <see cref="KescoColumn{TEntity}"/> для получения
    /// <c>DisplayName</c>, <c>SqlName</c> и <c>SortName</c> при построении заголовка.
    /// Возвращает <c>null</c>, если колонка не зарегистрирована.
    /// </summary>
    /// <param name="columnId">Числовой идентификатор колонки.</param>
    KescoColumnMeta? GetColumnMetaById(int columnId);

    /// <summary>
    /// Регистрирует колонку в гриде.
    /// Вызывается из <see cref="KescoColumnDef.OnInitialized"/>.
    /// </summary>
    /// <param name="columnId">Числовой идентификатор — связь с <see cref="KescoColumn{TEntity}"/>.</param>
    /// <param name="sqlName">SQL-имя (выходное имя SELECT) — идентификатор для SQL-операций.</param>
    /// <param name="displayName">Отображаемое имя (для заголовков, чипов в треях).</param>
    /// <param name="groupable">Разрешить группировку по этой колонке.</param>
    /// <param name="filterable">Разрешить фильтрацию по этой колонке.</param>
    /// <param name="sortName">
    /// Имя для ORDER BY. Если <c>null</c> — используется <paramref name="sqlName"/>.
    /// </param>
    void RegisterColumn(int columnId, string sqlName, string displayName, bool groupable, bool filterable, string? sortName = null);

    /// <summary>
    /// Отменяет регистрацию колонки.
    /// Вызывается из <see cref="KescoColumnDef.Dispose"/>.
    /// </summary>
    /// <param name="columnId">Числовой идентификатор колонки.</param>
    /// <param name="sqlName">SQL-имя колонки.</param>
    void UnregisterColumn(int columnId, string sqlName);

    /// <summary>
    /// Режим отображения кнопки меню (⋮) в заголовках колонок.
    /// </summary>
    ColumnMenuMode ColumnMenuMode { get; }

    /// <summary>
    /// Открыта ли панель группировки в данный момент.
    /// </summary>
    bool IsGroupingTrayExpanded { get; }

    /// <summary>
    /// Открыта ли панель фильтрации в данный момент.
    /// </summary>
    bool IsFilterTrayExpanded { get; }

    /// <summary>
    /// Добавляет колонку в трей группировки (альтернатива drag-and-drop для мобильных).
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    Task AddGroupAsync(string sqlName);

    /// <summary>
    /// Открывает диалог фильтрации для колонки (альтернатива drag-and-drop для мобильных).
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    Task AddFilterAsync(string sqlName);

    /// <summary>
    /// Регистрирует колонку в порядке отображения.
    /// Вызывается из <see cref="KescoColumn{TEntity}"/> при инициализации.
    /// </summary>
    /// <param name="columnId">Числовой идентификатор колонки.</param>
    void RegisterColumnInOrder(int columnId);

    /// <summary>
    /// Возвращает <c>true</c> если колонка скрыта пользователем через диалог настройки колонок.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    bool IsColumnHidden(string sqlName);

    /// <summary>
    /// Возвращает метаданные видимых колонок в порядке отображения (без скрытых).
    /// </summary>
    IReadOnlyList<KescoColumnMeta> GetVisibleColumns();

    /// <summary>
    /// Регистрирует CellTemplate колонки для динамического рендеринга.
    /// Вызывается из <see cref="KescoColumn{TEntity}"/> при инициализации.
    /// </summary>
    /// <param name="columnId">Числовой идентификатор колонки.</param>
    /// <param name="template">Шаблон содержимого ячейки (приводится к RenderFragment при использовании).</param>
    void RegisterCellTemplate(int columnId, object template);
}
