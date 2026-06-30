# 10. Интеграция в KescoGrid: данные и загрузка

Единый источник истины — дерево `KescoFilterGroupNode`. Прежний словарь
`_activeFilters`, неявно объединяемый через AND, упраздняется (становится деревом).
Эта задача — только **данные/загрузка**; панель и маршрутизация — задача 11.

## Состояние грида (`KescoGrid.Filtering.cs`, см. задачу 05)
- `private KescoFilterGroupNode _filterRoot = new();` (корень `Logic=And`).
- Синхронизировать `_filterRoot` → `query.CompositeFilter`.

## `KescoDataQuery.cs`
- `public KescoFilterGroupNode? CompositeFilter { get; set; }`.

## `IKescoGrid.cs`
```csharp
KescoFilterGroupNode? ActiveCompositeFilter { get; }
Task OpenCompositeFilterDialog();   // реализация UI — задача 11
```
(`AddFilterAsync(sqlName)` остаётся — открывает колоночный диалог и вставляет лист.)

## Пути загрузки (`KescoGridPageBase`, см. задачу 06)
- Заменить вызовы `_query.BuildColumnFilterClause(dp)` (≈7 мест: страница,
  группировка, экспорт, печать, выбранные, детали) на
  `KescoCompositeSqlBuilder.Build(_query.CompositeFilter, dp, knownColumns, columnNameMap)`
  (задача 07).
- `knownColumns` — множество `SqlName` зарегистрированных колонок (через `IKescoGrid`/метаданные).
- Объединение с поиском — существующим `CombineWhere`.

## Критерии
- [ ] Параллельного словаря-фильтра (AND по умолчанию) больше нет; истина — дерево.
- [ ] Все пути загрузки учитывают дерево; пустой корень = без фильтрации.
- [ ] `dotnet build` без ошибок; данные грузятся корректно.
