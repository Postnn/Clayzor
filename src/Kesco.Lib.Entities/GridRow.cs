namespace Kesco.Lib.Entities;

/// <summary>
/// Маркерный интерфейс для строки в плоском списке с группами.
/// Каждая строка в <see cref="GroupedPage{T}.Rows"/> реализует этот интерфейс.
/// </summary>
public interface IGridRow { }

/// <summary>
/// Заголовок группы в плоском списке.
/// Содержит ключ, название, количество элементов и состояние развёрнутости.
/// </summary>
public class GroupHeaderRow : IGridRow
{
    /// <summary>Отображаемое значение группы (например, название типа).</summary>
    public string DisplayValue { get; set; } = "";

    /// <summary>
    /// Полный ключ группы — конкатенация значений всех уровней группировки через \u001F (unit separator).
    /// Используется для идентификации группы в <c>HashSet</c> развёрнутых групп.
    /// </summary>
    public string FullKey { get; set; } = "";

    /// <summary>Общее количество строк детализации в этой группе (включая подгруппы).</summary>
    public int ItemCount { get; set; }

    /// <summary>Уровень вложенности: 0 — внешняя группа, 1 — первый уровень вложенности и т.д.</summary>
    public int Depth { get; set; }

    /// <summary>Развёрнута ли группа (показываются ли дочерние строки).</summary>
    public bool IsExpanded { get; set; }

    /// <summary>Значения группировки по уровням (для фильтрации детальных строк).</summary>
    public List<string> GroupKeys { get; set; } = [];
}

/// <summary>
/// Строка детализации — оборачивает сущность T.
/// </summary>
/// <typeparam name="T">Тип сущности (наследник Entity).</typeparam>
public class DetailRow<T> : IGridRow where T : Entity
{
    /// <summary>Сущность — строка данных.</summary>
    public T Item { get; set; } = default!;

    /// <summary>Полный ключ группы, к которой принадлежит строка.</summary>
    public string GroupKey { get; set; } = "";
}

/// <summary>
/// Результат запроса группированной страницы данных.
/// Содержит плоский список заголовков групп и строк детализации в порядке отображения,
/// а также общее эффективное количество строк для пагинации.
/// </summary>
/// <typeparam name="T">Тип сущности (наследник Entity).</typeparam>
public class GroupedPage<T> where T : Entity
{
    /// <summary>Плоский список строк для отображения: заголовки групп + строки детализации.</summary>
    public List<IGridRow> Rows { get; set; } = [];

    /// <summary>Общее количество эффективных строк (заголовки групп + все строки детализации) с учётом состояния развёрнутости.</summary>
    public int TotalEffectiveRows { get; set; }
}
