using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.ColumnTypes;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

public class ColumnTypeRegistryTests
{
    // ── 3.1 string → Text ─────────────────────────────────────────────────

    [Fact]
    public void FromClr_String_ReturnsText()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(string));
        Assert.Equal(ColumnType.Text, descriptor.Kind);
    }

    // ── 3.2 int → Number ──────────────────────────────────────────────────

    [Fact]
    public void FromClr_Int_ReturnsNumber()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(int));
        Assert.Equal(ColumnType.Number, descriptor.Kind);
    }

    // ── 3.3 int? → Number ─────────────────────────────────────────────────

    [Fact]
    public void FromClr_NullableInt_ReturnsNumber()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(int?));
        Assert.Equal(ColumnType.Number, descriptor.Kind);
    }

    // ── 3.4 decimal → Decimal ─────────────────────────────────────────────

    [Fact]
    public void FromClr_Decimal_ReturnsDecimal()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(decimal));
        Assert.Equal(ColumnType.Decimal, descriptor.Kind);
    }

    // ── 3.5 DateTime → Date ───────────────────────────────────────────────

    [Fact]
    public void FromClr_DateTime_ReturnsDate()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(DateTime));
        Assert.Equal(ColumnType.Date, descriptor.Kind);
    }

    // ── 3.6 bool → Boolean ────────────────────────────────────────────────

    [Fact]
    public void FromClr_Bool_ReturnsBoolean()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(bool));
        Assert.Equal(ColumnType.Boolean, descriptor.Kind);
    }
}
