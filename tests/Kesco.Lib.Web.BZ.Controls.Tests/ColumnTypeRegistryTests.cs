using Kesco.Lib.Web.BZ.Controls.Components.Grid;
using Kesco.Lib.Web.BZ.Controls.Components.Grid.ColumnTypes;

namespace Kesco.Lib.Web.BZ.Controls.Tests;

/// <summary>
/// Тесты реестра типов колонок <see cref="ColumnTypeRegistry"/>:
/// маппинг CLR-типов на дескрипторы (<see cref="ColumnTypeDescriptor"/>).
/// </summary>
public class ColumnTypeRegistryTests
{
    /// <summary>typeof(string) → <see cref="ColumnType.Text"/>.</summary>
    [Fact]
    public void FromClr_String_ReturnsText()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(string));
        Assert.Equal(ColumnType.Text, descriptor.Kind);
    }

    /// <summary>typeof(int) → <see cref="ColumnType.Number"/>.</summary>
    [Fact]
    public void FromClr_Int_ReturnsNumber()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(int));
        Assert.Equal(ColumnType.Number, descriptor.Kind);
    }

    /// <summary>typeof(int?) — Nullable-обёртка → <see cref="ColumnType.Number"/>.</summary>
    [Fact]
    public void FromClr_NullableInt_ReturnsNumber()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(int?));
        Assert.Equal(ColumnType.Number, descriptor.Kind);
    }

    /// <summary>typeof(decimal) → <see cref="ColumnType.Decimal"/>.</summary>
    [Fact]
    public void FromClr_Decimal_ReturnsDecimal()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(decimal));
        Assert.Equal(ColumnType.Decimal, descriptor.Kind);
    }

    /// <summary>typeof(DateTime) → <see cref="ColumnType.Date"/>.</summary>
    [Fact]
    public void FromClr_DateTime_ReturnsDate()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(DateTime));
        Assert.Equal(ColumnType.Date, descriptor.Kind);
    }

    /// <summary>typeof(bool) → <see cref="ColumnType.Boolean"/>.</summary>
    [Fact]
    public void FromClr_Bool_ReturnsBoolean()
    {
        var descriptor = ColumnTypeRegistry.FromClr(typeof(bool));
        Assert.Equal(ColumnType.Boolean, descriptor.Kind);
    }
}
