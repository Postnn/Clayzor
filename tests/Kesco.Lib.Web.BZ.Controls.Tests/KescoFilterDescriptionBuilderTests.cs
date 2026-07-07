using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

/// <summary>
/// Тесты построителя описаний <see cref="KescoFilterDescriptionBuilder"/>:
/// текстовое описание (<see cref="KescoFilterDescriptionBuilder.BuildText"/>),
/// сегменты (<see cref="KescoFilterDescriptionBuilder.BuildSegments"/>),
/// счётчик активных условий (<see cref="KescoFilterDescriptionBuilder.CountActiveLeaves"/>).
/// </summary>
public class KescoFilterDescriptionBuilderTests
{
    private static string GetDisplayName(string sql) => sql;

    /// <summary>BuildText для группы И — содержит имена колонок, операторы и значения.</summary>
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

    /// <summary>CountActiveLeaves: 2 листа, у одного второе условие → 3.</summary>
    [Fact]
    public void CountActiveLeaves_TwoLeavesOneWithSecondClause_Returns3()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "A", Operator = ColumnFilterOperator.Contains, Value = "x", Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ColumnFilter
        {
            Column = "B", Operator = ColumnFilterOperator.Contains, Value = "y",
            SecondOperator = ColumnFilterOperator.Equals, SecondValue = "z",
            Source = KescoFilterSource.CompositeDialog,
        });

        var count = KescoFilterDescriptionBuilder.CountActiveLeaves(root);
        Assert.Equal(3, count);
    }

    /// <summary>CountActiveLeaves: null и пустое дерево → 0.</summary>
    [Fact]
    public void CountActiveLeaves_Empty_ReturnsZero()
    {
        Assert.Equal(0, KescoFilterDescriptionBuilder.CountActiveLeaves(null));
        Assert.Equal(0, KescoFilterDescriptionBuilder.CountActiveLeaves(new KescoFilterGroupNode()));
    }

    /// <summary>CountActiveLeaves: ValueFilter с HasValue → 1.</summary>
    [Fact]
    public void CountActiveLeaves_ValueFilterWithValue_ReturnsOne()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter { Column = "col", Values = [1, 2] });

        var count = KescoFilterDescriptionBuilder.CountActiveLeaves(root);
        Assert.Equal(1, count);
    }

    /// <summary>BuildSegments для ColumnDialog — сегмент содержит Source и Column.</summary>
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
