using System.Text.Json;
using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

public class KescoFilterJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static string Serialize(IKescoFilterNode node)
        => JsonSerializer.Serialize(node, Options);

    private static IKescoFilterNode? Deserialize(string json)
        => JsonSerializer.Deserialize<IKescoFilterNode>(json, Options);

    // ── 5.1 Группа + лист → round-trip ────────────────────────────────────

    [Fact]
    public void RoundTrip_GroupWithLeaf_StructurePreserved()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.And };
        root.Nodes.Add(new ColumnFilter
        {
            Column = "col",
            Operator = ColumnFilterOperator.Contains,
            Value = "test",
            Source = KescoFilterSource.CompositeDialog,
        });

        var json = Serialize(root);
        var restored = Deserialize(json);

        Assert.NotNull(restored);
        var group = Assert.IsType<KescoFilterGroupNode>(restored);
        Assert.Equal(LogicalOperator.And, group.Logic);
        Assert.Single(group.Nodes);
        var leaf = Assert.IsType<ColumnFilter>(group.Nodes[0]);
        Assert.Equal("col", leaf.Column);
        Assert.Equal(ColumnFilterOperator.Contains, leaf.Operator);
        Assert.Equal("test", leaf.Value);
    }

    // ── 5.2 ParamName не сериализуется ────────────────────────────────────

    [Fact]
    public void Serialize_ParamName_NotInJson()
    {
        var leaf = new ColumnFilter
        {
            Column = "col",
            Operator = ColumnFilterOperator.Equals,
            Value = 1,
            ParamName = "secret",
            SecondParamName = "also_secret",
            Source = KescoFilterSource.CompositeDialog,
        };

        var json = Serialize(leaf);
        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("paramName", json);
        Assert.DoesNotContain("secondParamName", json);
    }

    // ── 5.3 IsNew не сериализуется ────────────────────────────────────────

    [Fact]
    public void Serialize_IsNew_NotInJson()
    {
        var leaf = new ColumnFilter
        {
            Column = "col",
            Operator = ColumnFilterOperator.Equals,
            Value = 1,
            IsNew = true,
        };

        var json = Serialize(leaf);
        Assert.DoesNotContain("isNew", json);
    }

    // ── 5.4 ValueFilter round-trip ────────────────────────────────────────

    [Fact]
    public void RoundTrip_ValueFilter_PreservesValues()
    {
        var vf = new ValueFilter
        {
            Column = "col",
            Values = ["a", "b", "c"],
            Negate = true,
            BlankChecked = true,
        };

        var json = Serialize(vf);
        var restored = Deserialize(json);

        Assert.NotNull(restored);
        var restoredVf = Assert.IsType<ValueFilter>(restored);
        Assert.Equal("col", restoredVf.Column);
        Assert.True(restoredVf.Negate);
        Assert.True(restoredVf.BlankChecked);
        Assert.Equal(3, restoredVf.Values.Count);
    }

    // ── 5.5 SecondClause round-trip ───────────────────────────────────────

    [Fact]
    public void RoundTrip_SecondClause_Preserved()
    {
        var leaf = new ColumnFilter
        {
            Column = "col",
            Operator = ColumnFilterOperator.GreaterThan,
            Value = 10,
            LogicalOperator = LogicalOperator.And,
            SecondOperator = ColumnFilterOperator.LessThan,
            SecondValue = 100,
            Source = KescoFilterSource.CompositeDialog,
        };

        var json = Serialize(leaf);
        var restored = Deserialize(json);

        Assert.NotNull(restored);
        var restoredLeaf = Assert.IsType<ColumnFilter>(restored);
        Assert.Equal(ColumnFilterOperator.GreaterThan, restoredLeaf.Operator);
        Assert.Equal(ColumnFilterOperator.LessThan, restoredLeaf.SecondOperator);
        Assert.Equal(100, restoredLeaf.SecondValue);
    }
}
