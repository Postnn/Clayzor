namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>
/// Узел дерева составного фильтра: лист (<see cref="ColumnFilter"/>) или группа (<see cref="KescoFilterGroupNode"/>).
/// </summary>
public interface IKescoFilterNode
{
    /// <summary>Рекурсивное глубокое копирование узла. Правка копии не трогает оригинал.</summary>
    IKescoFilterNode Clone();
}
