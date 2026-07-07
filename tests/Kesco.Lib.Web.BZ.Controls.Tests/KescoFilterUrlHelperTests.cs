using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

public class KescoFilterUrlHelperTests
{
    // ── 6.1 Round-trip ────────────────────────────────────────────────────

    [Fact]
    public void SerializeDeserialize_RoundTrip_StructurePreserved()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.Or };
        root.Nodes.Add(new ColumnFilter
        {
            Column = "col",
            Operator = ColumnFilterOperator.Contains,
            Value = "test",
            Source = KescoFilterSource.CompositeDialog,
        });
        root.Nodes.Add(new ValueFilter
        {
            Column = "col2",
            Values = [1, 2],
            Negate = false,
        });

        var url = KescoFilterUrlHelper.Serialize(root);
        Assert.NotNull(url);

        var restored = KescoFilterUrlHelper.Deserialize(url);
        Assert.NotNull(restored);
        Assert.Equal(LogicalOperator.Or, restored!.Logic);
        Assert.Equal(2, restored.Nodes.Count);
        Assert.IsType<ColumnFilter>(restored.Nodes[0]);
        Assert.IsType<ValueFilter>(restored.Nodes[1]);
    }

    // ── 6.2 Пустое дерево → null ──────────────────────────────────────────

    [Fact]
    public void Serialize_EmptyTree_ReturnsNull()
    {
        var empty = new KescoFilterGroupNode();
        var result = KescoFilterUrlHelper.Serialize(empty);
        Assert.Null(result);
    }

    // ── 6.3 null → null ───────────────────────────────────────────────────

    [Fact]
    public void Deserialize_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(KescoFilterUrlHelper.Deserialize(null));
        Assert.Null(KescoFilterUrlHelper.Deserialize(""));
        Assert.Null(KescoFilterUrlHelper.Deserialize("invalid_base64!!!"));
    }
}
