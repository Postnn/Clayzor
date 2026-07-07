using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

public class FilterModelTests
{
    // ── 2.1 KescoFilterGroupNode.Clone() — глубокая копия ─────────────────

    [Fact]
    public void GroupNode_Clone_DeepCopy()
    {
        var leaf = new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.Equals, Value = "test" };
        var original = new KescoFilterGroupNode { Logic = LogicalOperator.Or };
        original.Nodes.Add(leaf);

        var clone = (KescoFilterGroupNode)original.Clone();

        // Структура идентична
        Assert.Equal(LogicalOperator.Or, clone.Logic);
        Assert.Single(clone.Nodes);
        Assert.IsType<ColumnFilter>(clone.Nodes[0]);

        // Правка копии не трогает оригинал
        clone.Nodes.Clear();
        Assert.Single(original.Nodes);
    }

    // ── 2.2 ColumnFilter.Clone() — поля скопированы ──────────────────────

    [Fact]
    public void ColumnFilter_Clone_CopiesAllFields()
    {
        var original = new ColumnFilter
        {
            Column = "TestCol",
            Operator = ColumnFilterOperator.Contains,
            Value = "hello",
            Source = KescoFilterSource.CompositeDialog,
            LogicalOperator = LogicalOperator.Or,
            SecondOperator = ColumnFilterOperator.Equals,
            SecondValue = 42,
            ParamName = "x",
            SecondParamName = "y",
            IsNew = true,
        };

        var clone = (ColumnFilter)original.Clone();

        Assert.Equal("TestCol", clone.Column);
        Assert.Equal(ColumnFilterOperator.Contains, clone.Operator);
        Assert.Equal("hello", clone.Value);
        Assert.Equal(KescoFilterSource.CompositeDialog, clone.Source);
        Assert.Equal(LogicalOperator.Or, clone.LogicalOperator);
        Assert.Equal(ColumnFilterOperator.Equals, clone.SecondOperator);
        Assert.Equal(42, clone.SecondValue);
        // IsNew не копируется
        Assert.False(clone.IsNew);
    }

    // ── 2.3 ValueFilter.Clone() — независимый список Values ──────────────

    [Fact]
    public void ValueFilter_Clone_IndependentValuesList()
    {
        var original = new ValueFilter
        {
            Column = "col",
            Values = [1, 2, 3],
            Negate = true,
            BlankChecked = true,
        };

        var clone = (ValueFilter)original.Clone();

        Assert.Equal("col", clone.Column);
        Assert.True(clone.Negate);
        Assert.True(clone.BlankChecked);
        Assert.Equal(3, clone.Values.Count);

        // Правка копии не трогает оригинал
        clone.Values.Clear();
        Assert.Equal(3, original.Values.Count);
    }
}
