using Kesco.Lib.Web.BZ.Controls.Components.Grid.ColumnTypes;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>
/// Диалог настраиваемого (составного) фильтра.
/// Работает с глубокой копией входного дерева — оригинал не изменяется до нажатия «Применить».
/// «Применить» → возвращает изменённую копию через <c>DialogResult.Ok</c>.
/// «Сбросить»  → возвращает пустой корневой узел.
/// «Отмена»    → <c>DialogResult.Cancel</c>.
/// </summary>
public partial class KescoFilterDialog : ComponentBase
{
    // ── Параметры ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Каскадный параметр экземпляра диалога MudBlazor.
    /// Используется для закрытия диалога с результатом.
    /// </summary>
    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    /// <summary>
    /// Корневой узел дерева фильтра. Диалог работает с глубокой копией,
    /// не изменяя оригинал до подтверждения.
    /// </summary>
    [Parameter, EditorRequired]
    public KescoFilterGroupNode Root { get; set; } = null!;

    /// <summary>Список доступных для фильтрации колонок.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<KescoColumnMeta> Columns { get; set; } = [];

    /// <summary>Необязательные варианты значений на колонку (SqlName → список).</summary>
    [Parameter]
    public IReadOnlyDictionary<string, IReadOnlyList<KescoFilterOption>>? LookupOptions { get; set; }

    // ── Внутреннее состояние ───────────────────────────────────────────────────

    /// <summary>Глубокая копия входного корня — черновик редактирования.</summary>
    private KescoFilterGroupNode _draft = null!;

    /// <summary>Текстовое описание текущего черновика фильтра.</summary>
    private string _draftDescription = "";

    /// <summary>Резолвит отображаемое имя колонки по её SQL-имени.</summary>
    private Func<string, string> DisplayNameOf => sql =>
        Columns.FirstOrDefault(c => c.SqlName == sql)?.DisplayName ?? sql;

    // ── Жизненный цикл ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        // Создаём копию при первичной инициализации параметров.
        // Повторные изменения Root снаружи игнорируются — диалог изолирован.
        _draft ??= (KescoFilterGroupNode)Root.Clone();
        RecalcDescription();
    }

    // ── Обработчики ───────────────────────────────────────────────────────────

    /// <summary>
    /// Обрабатывает любые изменения в дереве черновика — вызывает перерисовку.
    /// </summary>
    private void OnDraftChanged()
    {
        RecalcDescription();
        StateHasChanged();
    }

    /// <summary>Пересчитывает текстовое описание черновика фильтра.</summary>
    private void RecalcDescription() =>
        _draftDescription = KescoFilterDescriptionBuilder.BuildText(_draft, DisplayNameOf) ?? "";

    /// <summary>
    /// Применяет фильтр: закрывает диалог и возвращает черновик как результат.
    /// </summary>
    private void Apply() => MudDialog?.Close(DialogResult.Ok(_draft));

    /// <summary>
    /// Сбрасывает фильтр: закрывает диалог и возвращает пустую группу.
    /// </summary>
    private void Reset() => MudDialog?.Close(DialogResult.Ok(new KescoFilterGroupNode()));

    /// <summary>Отменяет редактирование без изменений.</summary>
    private void Cancel() => MudDialog?.Cancel();
}
