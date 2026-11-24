using Clayzor.Lib.Web.Controls.Components.Tree.DataSources;
using Clayzor.Lib.Web.Controls.Components.Tree.Models;

namespace Clayzor.ThemeStudio.Components;

internal sealed class StudioTreeData : IClayTreeDataSource
{
    public Task<ClayTreeLoadResult> LoadLevelAsync(ClayTreeLoadRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<ClayTreeNode> nodes = request.Parent?.Id switch
        {
            null =>
            [
                new() { Id = "1", RawId = 1, Text = "Склад", HasChildren = true },
                new() { Id = "2", RawId = 2, Text = "Ремонт" },
            ],
            "1" => [new() { Id = "3", RawId = 3, Text = "Стеллаж А", Level = 1 }],
            _ => [],
        };
        return Task.FromResult(new ClayTreeLoadResult(nodes));
    }
}
