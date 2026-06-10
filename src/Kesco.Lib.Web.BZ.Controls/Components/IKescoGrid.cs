namespace Kesco.Lib.Web.BZ.Controls;

/// <summary>
/// Интерфейс регистрации метаданных колонок, реализуемый <see cref="KescoGrid{TEntity}"/>.
/// Используется <see cref="KescoColumnDef"/> для передачи настроек группируемости
/// через каскадный параметр без привязки к generic-типу грида.
/// </summary>
public interface IKescoGrid
{
    /// <summary>
    /// Регистрирует колонку в гриде.
    /// Вызывается из <see cref="KescoColumnDef.OnInitialized"/>.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки (выходное имя SELECT).</param>
    /// <param name="displayName">Отображаемое имя (для чипа в трее группировки).</param>
    /// <param name="groupable">Разрешить группировку по этой колонке.</param>
    void RegisterColumn(string sqlName, string displayName, bool groupable);

    /// <summary>
    /// Отменяет регистрацию колонки.
    /// Вызывается из <see cref="KescoColumnDef.Dispose"/>.
    /// </summary>
    /// <param name="sqlName">SQL-имя колонки.</param>
    void UnregisterColumn(string sqlName);
}
