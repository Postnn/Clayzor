using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

public class KescoFilterDescriptionBuilderTests
{
    private static string GetDisplayName(string sql) => sql;

    // ── 4.1 BuildText — группа И с двумя листьями ─────────────────────────

    [Fact]
    public void BuildText_AndGroup_ContainsAndWithDescriptions()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.And };
        root.Nodes.Add(new ColumnFilter { Column = "Кол1", Operator = ColumnFilterOperator.Contains, Value = "v1", Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ColumnFilter { Column = "Кол2", Operator = ColumnFilterOperator.Equals, Value = "v2", Source = KescoFilterSource.CompositeDialog });

        var text = KescoFilterDescriptionBuilder.BuildText(root, GetDisplayName);

        Assert.NotNull(text);
        Assert.Contains("Кол1", text);
        Assert.Contains("Кол2", text);
        Assert.Contains("содержит", text);
        Assert.Contains("v1", text);
    }

    // ── 4.2 CountActiveLeaves — 2 листа + SecondClause ────────────────────

    [Fact]
    public void CountActiveLeaves_TwoLeavesOneWithSecondClause_Returns3()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "A", Operator = ColumnFilterOperator.Contains, Value = "x", Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ColumnFilter
        {
            Column = "B",
            Operator = ColumnFilterOperator.Contains,
            Value = "y",
            SecondOperator = ColumnFilterOperator.Equals,
            SecondValue = "z",
            Source = KescoFilterSource.CompositeDialog,
        });

        var count = KescoFilterDescriptionBuilder.CountActiveLeaves(root);
        Assert.Equal(3, count); // 1 (A, одно условие) + 2 (B, два условия) = 3
    }

    // ── 4.3 CountActiveLeaves — пустое дерево ─────────────────────────────

    [Fact]
    public void CountActiveLeaves_Empty_ReturnsZero()
    {
        Assert.Equal(0, KescoFilterDescriptionBuilder.CountActiveLeaves(null));
        Assert.Equal(0, KescoFilterDescriptionBuilder.CountActiveLeaves(new KescoFilterGroupNode()));
    }

    // ── 4.4 CountActiveLeaves — ValueFilter ───────────────────────────────

    [Fact]
    public void CountActiveLeaves_ValueFilterWithValue_ReturnsOne()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter { Column = "col", Values = [1, 2] });

        var count = KescoFilterDescriptionBuilder.CountActiveLeaves(root);
        Assert.Equal(1, count);
    }

    // ── 4.5 BuildSegments — Source в сегменте ─────────────────────────────

    [Fact]
    public void BuildSegments_ColumnDialog_SegmentHasColumnSource()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.Contains, Value = "test", Source = KescoFilterSource.ColumnDialog });

        var segments = KescoFilterDescriptionBuilder.BuildSegments(root, GetDisplayName);

        Assert.Single(segments);
        Assert.Equal(KescoFilterSource.ColumnDialog, segments[0].Source);
        Assert.Equal("col", segments[0].Column);
    }
}
