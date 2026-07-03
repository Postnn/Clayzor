# Фильтр по уникальному значению (Excel-style) — мастер-план

Цель: добавить в `KescoGrid` фильтрацию колонки по набору уникальных значений
(как автофильтр Excel): выпадающий список галочек в заголовке колонки, поверх
существующего дерева фильтра (`KescoFilterGroupNode` / `IKescoFilterNode` /
`ColumnFilter` / `KescoCompositeSqlBuilder`). Переиспользуем существующие типы,
диалоги (`IDialogService` + `IMudDialogInstance`), Dapper-параметры и белый
список колонок из реестра. Новые сущности — только там, где их нет.

## Как читать этот план (та же схема, что и в `promts/00_README.md`)
- Одна задача за подход. В начале сессии дать агенту **этот файл (00) + текущий
  промпт (Vn)**. Сказать: «сделай только задачу Vn, потом остановись».
- Перед кодом — план изменений (что и где правит).
- Рефакторинги/добавления модели — **behavior-preserving**: старое поведение не
  ломаем, только расширяем. Перед задачей — git-коммит (точка отката).
- Конец задачи = `dotnet build` без ошибок + чек-лист промпта + коммит.
- Если у агента нет доступа к репозиторию — вставлять вместе с промптом реальные
  файлы, на которые он ссылается (иначе выдумает API).

## Соглашения (повторять агенту)
- Значения в SQL — только Dapper-параметрами; имя колонки — только из реестра
  (белый список `BuildKnownColumns()` / `knownColumns`).
- Каждый компонент: `.razor` + `.razor.cs` (логика не в разметке).
- Диалоги — через `IDialogService` + `IMudDialogInstance` (как
  `KescoColumnFilterDialog`), не через `@bind-Visible`.
- В одной колонке одновременно активен **либо** фильтр по условию
  (`ColumnFilter`, `Source=ColumnDialog`), **либо** фильтр по значению
  (`ValueFilter`) — они взаимоисключающие (треб. 8, 9).

## Статус
- **V1 — Модель узла `ValueFilter` — СДЕЛАНО, закоммичено.**
- **V2 — SQL для `ValueFilter` в `KescoCompositeSqlBuilder` — СДЕЛАНО, закоммичено.**
- **V3 — Метаданные колонки: `AllowValueFilter`, подписи bool — СДЕЛАНО, закоммичено.**
- **V4 — Источник уникальных значений: `LoadDistinctValuesAsync` — СДЕЛАНО.**

## Порядок выполнения (одна задача за подход)

| №  | Задача | Тип | Покрывает требования | Зависит от |
|----|--------|-----|----------------------|------------|
| V1 | Модель узла `ValueFilter` (лист дерева фильтра) | модель | 5, 6, 14 | — |
| V2 | SQL для `ValueFilter` в `KescoCompositeSqlBuilder` (IN / NOT IN, NULL/пустышки, инверсия) | фича | 6, 14 | V1 |
| V3 | Метаданные колонки: `AllowValueFilter`, подписи bool | модель | 1, 15 | — |
| V4 | Источник уникальных значений: `LoadDistinctValuesAsync` (лениво, cap 100, контекст) | фича | 2, 3, 10 | V1, V3 |
| V5 | Пресет оператора в `KescoColumnFilterDialog` (`InitialOperator`) | enabler | 7 | — |
| V6 | Компонент диалога значения `KescoColumnValueFilterDialog` | фича | 3–10, 12, 15 | V1, V4, V5 |
| V7 | Интеграция в заголовок: значок слева, подсветка, маршрутизация применения | фича | 1, 7–9, 11, 13, 14 | V3, V6, Filtering |
| V8 | (позже) Панель/чипы/описание для печати; тумблер в диалоге настройки колонок | опц. | — | V7 |

Логика порядка: сперва **модель** (V1) и её **SQL** (V2) на чистую проверяемую
базу; параллельно **метаданные** (V3); затем **данные** (V4) и мелкий
**enabler** (V5); после — **UI-компонент** (V6) и **сборка в заголовок** (V7);
опции/панель — в конце (V8).

## Ключевые существующие точки интеграции (агенту не выдумывать)
- Дерево фильтра: `Filter/IKescoFilterNode.cs`, `Filter/KescoFilterGroupNode.cs`,
  лист `ColumnFilter` в `KescoDataQuery.cs`.
- Построение SQL: `Filter/KescoCompositeSqlBuilder.cs` (метод `BuildLeaf`),
  `KescoDataQuery.BuildSingleClause`.
- Реестр/метаданные: `IKescoGrid.cs` (`KescoColumnMeta`, `RegisterColumn`),
  `KescoColumnDef.razor`.
- Заголовок колонки: `KescoColumn.razor` (`HeaderTemplate`).
- Состояние и применение фильтра: `KescoGrid.Filtering.cs`
  (`_filterRoot`, `OpenFilterDialog`, `ColumnDialogLeaves`, `NotifyQueryChanged`).
- Данные/загрузка: `KescoGridPageBase.cs` (`LoadFlatData`,
  `BuildCompositeFilterClause`, `BuildKnownColumns`, `Db` = `DbManager`),
  контракт `IKescoGridDataLoader`.
- Типы колонок: `ColumnTypes/ColumnTypeRegistry.cs`, `ColumnTypeDescriptor`,
  `BooleanColumnType`.
