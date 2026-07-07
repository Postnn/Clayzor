using Dapper;
using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

/// <summary>
/// Тесты SQL-билдера <see cref="KescoCompositeSqlBuilder"/> и <see cref="KescoDataQuery.BuildSingleClause"/>.
/// Покрытие: группы И/ИЛИ, вложенность, белый список колонок, параметризация,
/// уникальные имена параметров, операторы без значений, LIKE/ESCAPE, ValueFilter (IN/NOT IN/BlankChecked).
/// </summary>
public class KescoCompositeSqlBuilderTests
{
    private static ISet<string> Known(string[] cols) => new HashSet<string>(cols);

    /// <summary>Группа И с двумя условиями — скобки и AND.</summary>
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

    /// <summary>Группа ИЛИ с двумя условиями — скобки и OR.</summary>
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

    /// <summary>Вложенная группа И(ИЛИ(a,b), c) — двойные скобки.</summary>
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
        Assert.StartsWith("((", result);
        Assert.Contains(" AND ", result);
        Assert.Contains(" OR ", result);
    }

    /// <summary>Пустой корень (без дочерних узлов) → null.</summary>
    [Fact]
    public void Build_EmptyRoot_ReturnsNull()
    {
        var root = new KescoFilterGroupNode();
        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["A"]));
        Assert.Null(result);
    }

    /// <summary>null-корень → null.</summary>
    [Fact]
    public void Build_NullRoot_ReturnsNull()
    {
        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(null, dp, Known(["A"]));
        Assert.Null(result);
    }

    /// <summary>Колонка вне белого списка — лист молча отбрасывается.</summary>
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
        Assert.DoesNotContain("(", result);
    }

    /// <summary>Значение уходит в Dapper-параметр, а не в SQL-текст.</summary>
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

    /// <summary>Два листа на одной колонке — уникальные имена параметров p0, p1.</summary>
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
    }

    /// <summary>Оператор IsNull — SQL содержит IS NULL без параметра.</summary>
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

    /// <summary>Оператор IsNotNull — SQL содержит IS NOT NULL без параметра.</summary>
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

    /// <summary>Contains → LIKE @p0 ESCAPE '\' с обрамлением %value%.</summary>
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

    /// <summary>SQL-инъекция в значении — значение в параметре, не в SQL-тексте.</summary>
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

    /// <summary>ValueFilter в режиме IN — SQL содержит IN (@p0, @p1, @p2).</summary>
    [Fact]
    public void Build_ValueFilter_IN_GeneratesInClause()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter { Column = "col", Values = [1, 2, 3], Negate = false, BlankChecked = false });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("IN (", result);
        Assert.Contains("@p0", result);
        Assert.Contains("@p1", result);
        Assert.Contains("@p2", result);
    }

    /// <summary>ValueFilter NOT IN без BlankChecked — добавляется IS NOT NULL.</summary>
    [Fact]
    public void Build_ValueFilter_NotIn_BlankCheckedFalse()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter { Column = "col", Values = ["a", "b"], Negate = true, BlankChecked = false });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains("NOT IN (", result);
        Assert.Contains("IS NOT NULL", result);
    }

    /// <summary>ValueFilter с BlankChecked=true — OR-логика, IS NULL.</summary>
    [Fact]
    public void Build_ValueFilter_BlankChecked_OrLogic()
    {
        var root = new KescoFilterGroupNode();
        root.Nodes.Add(new ValueFilter { Column = "col", Values = [1], Negate = false, BlankChecked = true });

        var dp = new DynamicParameters();
        var result = KescoCompositeSqlBuilder.Build(root, dp, Known(["col"]));

        Assert.NotNull(result);
        Assert.Contains(" OR ", result);
        Assert.Contains("IS NULL", result);
    }

    /// <summary>Дата параметризуется как DateTime через Dapper.</summary>
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

    /// <summary>Decimal параметризуется через Dapper.</summary>
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
