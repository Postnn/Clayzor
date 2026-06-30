using Microsoft.JSInterop;
using MudBlazor;

namespace Kesco.Lib.Web.BZ.Controls.Components.Grid;

public partial class KescoGrid<TEntity> where TEntity : class
{
    /// <summary>Флаг выполнения операции экспорта/печати (показывает спиннер).</summary>
    private bool _isExporting;

    /// <summary>Состояние раскрытия подгрупп меню групповых операций: label → isOpen.</summary>
    private Dictionary<string, bool> _openSubGroups = [];

    private void ToggleSubGroup(string label)
    {
        if (_openSubGroups.TryGetValue(label, out var isOpen))
            _openSubGroups[label] = !isOpen;
        else
            _openSubGroups[label] = true;
    }

    private bool IsSubGroupOpen(string label)
        => _openSubGroups.TryGetValue(label, out var isOpen) && isOpen;

    private async Task PrintCurrentPageInternal()
        => await JS.InvokeVoidAsync("kescoGridPrint.printCurrentPage", Id);

    private async Task PrintAllInternal()
    {
        if (DataLoader is null) return;
        var spinnerId = Id + "-print-spinner";
        _ = JS.InvokeVoidAsync("kescoGridPrint.showSpinner", spinnerId);
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            var html = await DataLoader.BuildPrintHtmlAsync(
                columns, Title, BuildFilterDescription(), BuildGroupDescription());
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            await JS.InvokeAsync<object>("kescoGridPrint.printHtml", html);
        }
        catch (Exception ex)
        {
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            Snackbar.Add($"Ошибка печати: {ex.Message}", Severity.Error);
        }
    }

    private async Task PrintSelectedInternal()
    {
        if (DataLoader is null || _selectedIds.Count == 0) return;
        var spinnerId = Id + "-print-spinner";
        _ = JS.InvokeVoidAsync("kescoGridPrint.showSpinner", spinnerId);
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            var html = await DataLoader.BuildPrintHtmlForSelectedAsync(
                columns, Title, _selectedIds.ToList(),
                BuildFilterDescription(), BuildGroupDescription());
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            await JS.InvokeAsync<object>("kescoGridPrint.printHtml", html);
        }
        catch (Exception ex)
        {
            await JS.InvokeVoidAsync("kescoGridPrint.hideSpinner", spinnerId);
            Snackbar.Add($"Ошибка печати: {ex.Message}", Severity.Error);
        }
    }

    private async Task ExcelCurrentPageInternal()
    {
        if (DataLoader is null) return;
        _isExporting = true;
        StateHasChanged();
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            await DataLoader.ExcelExportAsync(new ExcelExportRequest
            {
                Mode = ExcelExportMode.CurrentPage,
                Title = Title,
                VisibleColumns = columns,
                FilterDescription = BuildFilterDescription(),
                GroupDescription = BuildGroupDescription(),
            });
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private async Task ExcelAllInternal()
    {
        if (DataLoader is null) return;
        _isExporting = true;
        StateHasChanged();
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            await DataLoader.ExcelExportAsync(new ExcelExportRequest
            {
                Mode = ExcelExportMode.All,
                Title = Title,
                VisibleColumns = columns,
                FilterDescription = BuildFilterDescription(),
                GroupDescription = BuildGroupDescription(),
            });
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private async Task ExcelSelectedInternal()
    {
        if (DataLoader is null || _selectedIds.Count == 0) return;
        _isExporting = true;
        StateHasChanged();
        try
        {
            var columns = ((IKescoGrid)this).GetVisibleColumns();
            await DataLoader.ExcelExportAsync(new ExcelExportRequest
            {
                Mode = ExcelExportMode.Selected,
                Title = Title,
                VisibleColumns = columns,
                SelectedIds = _selectedIds.ToList(),
                FilterDescription = BuildFilterDescription(),
                GroupDescription = BuildGroupDescription(),
            });
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }
}
