namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>
/// Группа условий составного фильтра с логикой И/ИЛИ.
/// Может содержать листы (<see cref="ColumnFilter"/>) и вложенные группы.
/// </summary>
public sealed class KescoFilterGroupNode : IKescoFilterNode
{
    /// <summary>Логический оператор, применяемый к прямым дочерним узлам.</summary>
    public LogicalOperator Logic { get; set; } = LogicalOperator.And;

    /// <summary>Дочерние узлы: листы (<see cref="ColumnFilter"/>) и/или вложенные группы.</summary>
    public List<IKescoFilterNode> Nodes { get; set; } = new();

    /// <summary>Рекурсивное глубокое копирование группы и всех дочерних узлов.</summary>
    public IKescoFilterNode Clone() => new KescoFilterGroupNode
    {
        Logic = Logic,
        Nodes = Nodes.Select(n => n.Clone()).ToList()
    };
}
