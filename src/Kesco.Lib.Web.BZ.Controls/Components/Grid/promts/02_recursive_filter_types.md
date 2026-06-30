# 02. Рекурсивные типы фильтра (дерево групп И/ИЛИ)

## Цель
Ввести древовидную модель составного фильтра поверх существующего `ColumnFilter`.
Лист дерева — `ColumnFilter` (одна колонка, до двух условий). Узел-группа —
новый тип с логикой `И`/`ИЛИ` и списком дочерних узлов. Произвольная вложенность.

## Новые файлы

### `Components/Grid/Filter/IKescoFilterNode.cs`
```csharp
namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>Узел дерева составного фильтра: лист (ColumnFilter) или группа.</summary>
public interface IKescoFilterNode
{
    IKescoFilterNode Clone();
}
```

### `Components/Grid/Filter/KescoFilterGroupNode.cs`
```csharp
namespace Kesco.Lib.Web.BZ.Controls.Components.Grid.Filter;

/// <summary>Группа условий с логикой И/ИЛИ. Может содержать листы и вложенные группы.</summary>
public sealed class KescoFilterGroupNode : IKescoFilterNode
{
    public LogicalOperator Logic { get; set; } = LogicalOperator.And;   // переиспользуем существующий enum
    public List<IKescoFilterNode> Nodes { get; set; } = new();

    public IKescoFilterNode Clone() => new KescoFilterGroupNode
    {
        Logic = Logic,
        Nodes = Nodes.Select(n => n.Clone()).ToList()
    };
}
```

## Правка существующего `ColumnFilter` (в `KescoDataQuery.cs`)

Сделать `ColumnFilter` листом дерева. Изменения **не ломают** текущий
словарный режим (`Dictionary<string, ColumnFilter>`):

1. `public sealed class ColumnFilter : IKescoFilterNode`.
2. Добавить происхождение (для маршрутизации редактирования — см. промпт 05):
   ```csharp
   public KescoFilterSource Source { get; set; } = KescoFilterSource.ColumnDialog;
   ```
   где новый enum (рядом с `ColumnFilter`):
   ```csharp
   public enum KescoFilterSource { ColumnDialog, CompositeDialog }
   ```
   `SourceField` не нужен — у `ColumnFilter` уже есть `Column` (SqlName).
3. Реализовать `Clone()` — копировать оба условия и `Source`:
   ```csharp
   public IKescoFilterNode Clone() => new ColumnFilter
   {
       Column = Column, ParamName = ParamName, Operator = Operator, Value = Value,
       LogicalOperator = LogicalOperator,
       SecondParamName = SecondParamName, SecondOperator = SecondOperator, SecondValue = SecondValue,
       Source = Source
   };
   ```

> Лист `ColumnFilter` способен нести до двух условий по одной колонке (И/ИЛИ) —
> это ровно то, что выдаёт `KescoColumnFilterDialog`. Условия, созданные диалогом
> настраиваемого фильтра, — одноклаузные `ColumnFilter` (`HasSecondClause = false`)
> с `Source = CompositeDialog`.

## Критерии
- [ ] Существующая колоночная фильтрация (словарь + `BuildColumnFilterClause`) продолжает работать.
- [ ] `KescoFilterGroupNode.Clone()` рекурсивно копирует поддерево; правка копии не трогает оригинал.
- [ ] `ColumnFilter.Clone()` копирует оба условия и `Source`.
- [ ] `dotnet build` без ошибок.
