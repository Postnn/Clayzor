using Dapper;
using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

public class KescoCompositeSqlBuilderTests
{
    private static ISet<string> Known(string[] cols) => new HashSet<string>(cols);

    // ── 1.1 Группа И с двумя условиями ────────────────────────────────────

    [Fact]
    public void Build_AndGroup_ReturnsParenthesizedAnd()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.And };
        root.Nodes.Add(new ColumnFilter { Column = "col1", Operator = ColumnFilterOperator.Contains, Value = "test", Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ColumnFilter { Column = "col2", Operator = ColumnFilterOperator.Equals, Value = 42, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col1", "col2"]));

        Assert.NotNull(result);
        Assert.StartsWith("(", result);
        Assert.Contains(" AND ", result);
        Assert.EndsWith(")", result);
    }

    // ── 1.2 Группа ИЛИ с двумя условиями ──────────────────────────────────

    [Fact]
    public void Build_OrGroup_ReturnsParenthesizedOr()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.Or };
        root.Nodes.Add(new ColumnFilter { Column = "A", Operator = ColumnFilterOperator.Equals, Value = 1, Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ColumnFilter { Column = "B", Operator = ColumnFilterOperator.Equals, Value = 2, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["A", "B"]));

        Assert.NotNull(result);
        Assert.Contains(" OR ", result);
    }

    // ── 1.3 Вложенная группа ──────────────────────────────────────────────

    [Fact]
    public void Build_NestedGroup_ReturnsNestedBrackets()
    {
        var inner = new KescoFilterGroupNode { Logic = LogicalOperator.Or };
        inner.Nodes.Add(new ColumnFilter { Column = "A", Operator = ColumnFilterOperator.Equals, Value = 1, Source = KescoFilterSource.CompositeDialog });
        inner.Nodes.Add(new ColumnFilter { Column = "B", Operator = ColumnFilterOperator.Equals, Value = 2, Source = KescoFilterSource.CompositeDialog });

        var root = new KescoFilterGroupNode { Logic = LogicalOperator.And };
        root.Nodes.Add(inner);
        root.Nodes.Add(new ColumnFilter { Column = "C", Operator = ColumnFilterOperator.Equals, Value = 3, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["A", "B", "C"]));

        Assert.NotNull(result);
        // ((A = @p0 OR B = @p1) AND C = @p2)
        Assert.StartsWith("((", result);
        Assert.Contains(" AND ", result);
        Assert.Contains(" OR ", result);
    }

    // ── 1.4 Пустой корень ─────────────────────────────────────────────────

    [Fact]
    public void Build_EmptyRoot_ReturnsNull()
    {
        var root = new KescoFilterGroupNode();
        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["A"]));
        Assert.Null(result);
    }

    [Fact]
    public void Build_NullRoot_ReturnsNull()
    {
        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(null, dp, Known(["A"]));
        Assert.Null(result);
    }

    // ── 1.5 Лист с неизвестной колонкой ───────────────────────────────────

    [Fact]
    public void Build_UnknownColumn_LeafDropped()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.And };
        root.Nodes.Add(new ColumnFilter { Column = "known", Operator = ColumnFilterOperator.Equals, Value = 1, Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ColumnFilter { Column = "unknown", Operator = ColumnFilterOperator.Equals, Value = "bad", Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["known"]));

        Assert.NotNull(result);
        Assert.DoesNotContain("unknown", result);
        Assert.Contains("known", result);
        // Только одно условие → без скобок
        Assert.DoesNotContain("(", result);
    }

    // ── 1.6 Значения параметризуются ──────────────────────────────────────

    [Fact]
    public void Build_ValueParameterized()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.Equals, Value = "hello", Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("@p0", result);
        Assert.DoesNotContain("hello", result);
    }

    // ── 1.7 Повтор колонки → уникальные имена параметров ──────────────────

    [Fact]
    public void Build_DuplicateColumn_UniqueParamNames()
    {
        var root = new KescoFilterGroupNode { Logic = LogicalOperator.And };
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.Equals, Value = 1, Source = KescoFilterSource.CompositeDialog });
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.NotEquals, Value = 2, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("@p0", result);
        Assert.Contains("@p1", result);
        Assert.NotEqual("@p0", "@p1");
    }

    // ── 1.8 IsNull ────────────────────────────────────────────────────────

    [Fact]
    public void Build_IsNull_NoParameter()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.IsNull, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("IS NULL", result);
    }

    // ── 1.9 IsNotNull ─────────────────────────────────────────────────────

    [Fact]
    public void Build_IsNotNull_NoParameter()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.IsNotNull, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("IS NOT NULL", result);
    }

    // ── 1.10 Contains ─────────────────────────────────────────────────────

    [Fact]
    public void Build_Contains_LikeWithEscape()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.Contains, Value = "test", Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("LIKE @p0", result);
        Assert.Contains("ESCAPE '\\'", result);
    }

    // ── 1.11 SQL-инъекция ─────────────────────────────────────────────────

    [Fact]
    public void Build_SqlInjection_ValueInParameterNotInSql()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "col", Operator = ColumnFilterOperator.Equals, Value = "x' OR 1=1 --", Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.DoesNotContain("OR 1=1", result);
        Assert.DoesNotContain("--", result);
    }

    // ── 1.12 ValueFilter IN ───────────────────────────────────────────────

    [Fact]
    public void Build_ValueFilter_IN_GeneratesInClause()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter
        {
            Column = "col",
            Values = [1, 2, 3],
            Negate = false,
            BlankChecked = false,
        });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("IN (", result);
        Assert.Contains("@p0", result);
        Assert.Contains("@p1", result);
        Assert.Contains("@p2", result);
    }

    // ── 1.13 ValueFilter NOT IN + BlankChecked ────────────────────────────

    [Fact]
    public void Build_ValueFilter_NotIn_BlankCheckedFalse()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter
        {
            Column = "col",
            Values = ["a", "b"],
            Negate = true,
            BlankChecked = false,
        });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("NOT IN (", result);
        // Negate+!Blank: добавляется IS NOT NULL и <> '' для строк
        Assert.Contains("IS NOT NULL", result);
    }

    // ── 1.14 ValueFilter Negate=false BlankChecked=true ───────────────────

    [Fact]
    public void Build_ValueFilter_BlankChecked_OrLogic()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter
        {
            Column = "col",
            Values = [1],
            Negate = false,
            BlankChecked = true,
        });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains(" OR ", result); // Blank → OR logic
        Assert.Contains("IS NULL", result);
    }

    // ── 1.15 Date параметризуется как DateTime ───────────────────────────

    [Fact]
    public void Build_Date_AddedAsParameter()
    {
        var root = new KescoFilterGroupNode();
        var date = new DateTime(2025, 3, 15);
        root.Nodes.Add(new ColumnFilter { Column = "dt", Operator = ColumnFilterOperator.Equals, Value = date, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["dt"]));

        Assert.NotNull(result);
        Assert.Contains("@p0", result);
    }

    // ── 1.16 Decimal параметризуется ──────────────────────────────────────

    [Fact]
    public void Build_Decimal_AddedAsParameter()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ColumnFilter { Column = "price", Operator = ColumnFilterOperator.GreaterThan, Value = 99.95m, Source = KescoFilterSource.CompositeDialog });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["price"]));

        Assert.NotNull(result);
        Assert.Contains("@p0", result);
        Assert.Contains(">", result);
    }
}
