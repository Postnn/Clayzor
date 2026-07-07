using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

/// <summary>
/// Тесты URL-хелпера <see cref="KescoFilterUrlHelper"/>:
/// сжатие/восстановление дерева фильтра через DeflateStream + Base64Url.
/// </summary>
public class KescoFilterUrlHelperTests
{
    /// <summary>Дерево → Serialize → Deserialize → структура сохранена.</summary>
    [Fact]
    public void SerializeDeserialize_RoundTrip_StructurePreserved()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.Or };
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.Contains, Value = "test", Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ValueFilter { Column = "col2", Values = [1, 2], Negate = false });

        var url = KescoFilterUrlHelper.Serialize(root);
        Assert.NotNull(url);

        var restored = KescoFilterUrlHelper.Deserialize(url);
        Assert.NotNull(restored);
        Assert.Equal(LogicalOperator.Or, restored!.Logic);
        Assert.Equal(2, restored.Nodes.Count);
        Assert.IsType<ColumnFilter>(restored.Nodes[0]);
        Assert.IsType<ValueFilter>(restored.Nodes[1]);
    }

    /// <summary>Пустое дерево (нет дочерних узлов) → Serialize возвращает null.</summary>
    [Fact]
    public void Serialize_EmptyTree_ReturnsNull()
    {
        var empty = new KescoFilterGroupNode();
        var result = KescoFilterUrlHelper.Serialize(empty);
        Assert.Null(result);
    }

    /// <summary>null или пустая строка или невалидный Base64 → Deserialize возвращает null.</summary>
    [Fact]
    public void Deserialize_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(KescoFilterUrlHelper.Deserialize(null));
        Assert.Null(KescoFilterUrlHelper.Deserialize(""));
        Assert.Null(KescoFilterUrlHelper.Deserialize("invalid_base64!!!"));
    }
}
