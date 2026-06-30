using Kesco.Lib.Web.BZ.Controls.Components.Grid.ColumnTypes;
using Microsoft.AspNetCore.Components;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>
/// Редактор одного листового условия составного фильтра (<see cref="ColumnFilter"/>, <c>Source=CompositeDialog</c>).
/// Отображает выбор колонки, оператора и редактор значения через <see cref="KescoFilterValueEditor"/>.
/// </summary>
public partial class KescoFilterExpression : ComponentBase
{
    // ── Параметры ──────────────────────────────────────────────────────────────

    /// <summary>Редактируемый листовой узел фильтра.</summary>
    [Parameter, EditorRequired]
    public ColumnFilter Node { get; set; } = null!;

    /// <summary>Список доступных для фильтрации колонок.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<KescoColumnMeta> Columns { get; set; } = [];

    /// <summary>Необязательные варианты значений на колонку (SqlName → список).</summary>
    [Parameter]
    public IReadOnlyDictionary<string, IReadOnlyList<KescoFilterOption>>? LookupOptions { get; set; }

    /// <summary>Вызывается при любом изменении условия — родитель вызывает StateHasChanged.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>Вызывается при нажатии кнопки удаления условия.</summary>
    [Parameter]
    public EventCallback OnRemove { get; set; }

    // ── Внутреннее состояние ───────────────────────────────────────────────────

    /// <summary>Дескриптор типа выбранной колонки; null если колонка не выбрана.</summary>
    private ColumnTypeDescriptor? _descriptor;

    /// <summary>Доступные операторы для текущей колонки.</summary>
    private IReadOnlyList<ColumnFilterOperator> _availableOperators = [];

    /// <summary>Варианты lookup для текущей колонки (если заданы).</summary>
    private IReadOnlyList<KescoFilterOption>? _options;

    // ── Жизненный цикл ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        RefreshDescriptor(Node.Column);
    }

    // ── Обработчики ───────────────────────────────────────────────────────────

    /// <summary>
    /// Обрабатывает смену колонки: обновляет дескриптор и сбрасывает оператор/значение
    /// на значения по умолчанию для нового типа.
    /// </summary>
    private async Task OnColumnChanged(string sqlName)
    {
        Node.Column = sqlName;
        RefreshDescriptor(sqlName);

        // Сбрасываем оператор и значение при смене колонки
        Node.Operator = _descriptor?.DefaultOperator ?? ColumnFilterOperator.Contains;
        Node.Value    = null;

        await OnChanged.InvokeAsync();
    }

    /// <summary>Обрабатывает смену оператора.</summary>
    private async Task OnOperatorChanged(ColumnFilterOperator op)
    {
        Node.Operator = op;
        await OnChanged.InvokeAsync();
    }

    /// <summary>Обрабатывает изменение значения из <see cref="KescoFilterValueEditor"/>.</summary>
    private async Task OnValueChanged(object? value)
    {
        Node.Value = value;
        await OnChanged.InvokeAsync();
    }

    // ── Вспомогательные ───────────────────────────────────────────────────────

    /// <summary>
    /// Обновляет <see cref="_descriptor"/>, <see cref="_availableOperators"/> и <see cref="_options"/>
    /// по SQL-имени колонки.
    /// </summary>
    private void RefreshDescriptor(string sqlName)
    {
        var col = Columns.FirstOrDefault(c => c.SqlName == sqlName);
        _descriptor        = col?.Type;
        _availableOperators = _descriptor is not null
            ? _descriptor.Operators
            : [];
        _options = LookupOptions?.GetValueOrDefault(sqlName);
    }
}
