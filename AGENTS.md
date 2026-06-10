# KescoBZ — Медицинские исследования

## Stack
- .NET 10 Blazor Server (Interactive Server)
- MudBlazor 9.x — `Variant.Outlined` is the standard form input style
- MudBlazor.Extensions 9.x — дополнительные возможности MudBlazor (перетаскивание диалогов и др.)
- Dapper + Microsoft.Data.SqlClient 6.x (SQL Server)
- Windows Integrated Auth (Kerberos/NTLM via Negotiate)
- **SQL Server 2008 R2** — использовать только синтаксис, доступный в этой версии

## Build & Run
```
dotnet build KescoBZ.sln
dotnet run --project src\Kesco.App.Web.BZ.MedicalTests
```
Dev URL: `http://localhost:5010`

Watch with hot reload (VS Code task also available):
```
dotnet watch run --project src\Kesco.App.Web.BZ.MedicalTests
```

## Project dependency chain
```
Kesco.Lib.DALC          ← Dapper, SqlClient
Kesco.Lib.Entities       ← DALC + Dapper (column mapping)
Kesco.Lib.Web.Settings   ← config bindings (no DB dep)
Kesco.Lib.Web.BZ.Controls ← Entities + Web.Settings + MudBlazor (Razor Class Library)
Kesco.App.Web.BZ.MedicalTests ← all of the above (web app entry point)
```

## Configuration — connection string priority

`BindKescoSettings()` in `Kesco.Lib.Web.Settings/KescoAppSettings.cs` merges:
1. `web.config` (XML) — loaded via `AddWebConfig()` alongside `appsettings.json`
2. `KescoApp:ConnectionString` (any source) → `ConnectionStrings:DefaultConnection` (fallback)

Both `appsettings.json` and `web.config` participate; the merge order is standard .NET config layering (last source wins for keys). Local overrides go in `appsettings.Development.json` (gitignored).

`Program.cs` registers MudBlazor and MudBlazor.Extensions:
```csharp
builder.Services.AddMudServices();
builder.Services.AddMudExtensions(cfg => cfg.WithDefaultDialogOptions(d => d.DragMode = MudDialogDragMode.Simple));
```

`App.razor` must include MudBlazor.Extensions CSS and JS:
```html
<link href="_content/MudBlazor.Extensions/mudBlazorExtensions.min.css" rel="stylesheet" />
<script src="_content/MudBlazor.Extensions/js/mudBlazorExtensions.js"></script>
```

## Database access pattern

**No repository layer, no ORM, no migrations.** All SQL lives in `Kesco.Lib.Entities/SQLQueries.cs` as named constants.

### SQL constant naming convention
- `SELECT_{DataName}` — запросы на выборку данных
- `INSERT_{TableName}` — добавление записей
- `UPDATE_{TableName}` — обновление записей
- `DELETE_{TableName}` — удаление записей
- `SP_{Name}` — хранимые процедуры
- `FN_{Name}` — пользовательские функции

### Rules
- Queries use `DbManager` (scoped per-request) injected via `@inject DbManager Db`
- Raw SQL for queries: `Db.QueryAsync<T>(SQLQueries.CONST_NAME)` — `QueryAsync` defaults to `CommandType.Text`
- Raw SQL for commands: `Db.ExecuteAsync(SQLQueries.CONST_NAME, entity, commandType: CommandType.Text)` — **must pass `commandType: CommandType.Text`** because `ExecuteAsync` defaults to `CommandType.StoredProcedure`
- Stored procedures: `Db.QueryStoredProcAsync<T>(name, params)`
- All database access must go through `DbManager` methods — no direct `SqlConnection` or Dapper calls in pages or other assemblies
- Column names in SQL are **Russian**: `КодМедицинскогоАнализа`, `НазваниеАнализа`, etc.
- Entity properties map to Russian columns via `[Column(MedA.Имя)]` referencing constants from `ColumnNames.cs` — каждое имя колонки определено ровно один раз
- Every entity class must be registered in `DapperColumnMapper.Initialize()`
- `@using System.Data` required in `_Imports.razor` when using `CommandType.Text`

### SQL Server 2008 R2 pagination
- **Запрещено** использовать `OFFSET/FETCH` (требует SQL Server 2012+). Вместо этого — `ROW_NUMBER()`:
  ```sql
  SELECT * FROM (
      SELECT _src.*, ROW_NUMBER() OVER (ORDER BY {orderBy}) AS _rn
      FROM ({selectSql}) _src
  ) _p WHERE _rn BETWEEN @__start AND @__end
  ```
- Параметры: `@__start = (pageNumber - 1) * pageSize + 1`, `@__end = pageNumber * pageSize`
- Реализовано в `Entity.GetPagedAsync<T>()` — статические врапперы сущностей вызывают его через `Entity.GetPagedAsync<MyEntity>(...)`

## Entity CRUD & Lookup pattern
→ [docs/entity-crud.md](docs/entity-crud.md)

## Adding a new entity
→ [docs/adding-new-entity.md](docs/adding-new-entity.md)

## Shared components (Kesco.Lib.Web.BZ.Controls)

| Компонент | Документация |
|---|---|
| **KescoDataList\<T>** — грид с серверной пагинацией, поиском, сортировкой, группировкой | [docs/kesco-data-list.md](docs/kesco-data-list.md) |
| **KescoEditForm\<T>** — MudDialog с валидацией, сохранением, удалением | [docs/kesco-edit-form.md](docs/kesco-edit-form.md) |
| **KescoComboBox\<TItem>** — выпадающий список для `ILookupEntity` | [docs/kesco-combo-box.md](docs/kesco-combo-box.md) |
| **KescoErrorBar** — баннер ошибок БД с детализацией (SQL, параметры) | [docs/kesco-error-bar.md](docs/kesco-error-bar.md) |
| **ConfirmDialog** — диалог подтверждения | [docs/confirm-dialog.md](docs/confirm-dialog.md) |
| **ILookupEntity** — интерфейс справочной сущности (`int Id`, `string Name`) | [docs/entity-crud.md](docs/entity-crud.md) |
| **KescoTheme** — corporate theme (DarkNavy + Gold accent). Applied in MainLayout | — |

### Services

| Сервис | Назначение |
|---|---|
| **KescoErrorService** (Scoped) — хранит состояние последней ошибки SQL, реализует `ISqlErrorHandler`. Используется `KescoErrorBar` |
| **ISqlErrorHandler** (DALC) — интерфейс, вызываемый `DbManager` при `SqlException`. Регистрируется в DI |

## Server-side grouping architecture

Группировка выполняется **на стороне SQL Server** двумя отдельными запросами (подход DevExpress Blazor Grid):

1. **Запрос групповых агрегатов** — `GROUP BY` + `COUNT(*)`, возвращает уникальные значения группировки и количество записей
2. **Запрос детальных строк** — выборка конкретных записей с `ROW_NUMBER()` и фильтром по значениям группы

### Модель данных
- `IGridRow` — маркерный интерфейс строки в плоском списке (`Kesco.Lib.Entities/GridRow.cs`)
- `GroupHeaderRow` — заголовок группы: `FullKey`, `DisplayValue`, `ItemCount`, `Depth`, `IsExpanded`
- `DetailRow<T>` — обёртка сущности: `Item`, `GroupKey`
- `GroupedPage<T>` — результат запроса: `Rows` (плоский список) + `TotalEffectiveRows`
- `KescoDataQuery.ExpandedGroups` — `HashSet<string>` полных ключей развёрнутых групп (разделитель `\u001F`)

### Рендеринг
- **Плоская модель**: заголовки групп и строки детализации передаются как единый `IEnumerable<IGridRow>`
- Колонки грида используют `TemplateColumn T="IGridRow"` с проверкой типа в `CellTemplate`:
  ```razor
  @if (context.Item is GroupHeaderRow header) { /* рендер заголовка */ }
  else if (context.Item is DetailRow<MyEntity> detail) { /* рендер данных */ }
  ```
- **Запрещено** использовать MudBlazor `GroupBy`/`Groupable`/`GroupExpanded`/`GroupTemplate` — группировка управляется сервером
- `SortMode` на MudDataGrid **не задаётся** — порядок строк определяется серверным SQL

### Пагинация с группами
- Каждый заголовок группы = 1 эффективная строка, каждая строка детализации = 1
- `TotalCount` = общее эффективное количество строк (а не сырых записей)
- При сворачивании/разворачивании группы количество видимых строк меняется, страница пересчитывается
- При разворачивании последней группы на странице, если её детали не влезают — автоматический переход на следующую страницу

### Многоуровневая группировка
- SQL: `GROUP BY Col1, Col2, ...` — возвращает листовые агрегаты
- C#: синтетические родительские узлы создаются из листовых, `ItemCount` родителя = сумма дочерних
- `ComputeParentCounts()` рекурсивно вычисляет `ItemCount` для всех промежуточных уровней
- Уровень вложенности: `Depth` (0 = внешний), отступ заголовка: `Depth * 16px`. Строки детализации отступают на `(Depth + 1) * 16px` — на один уровень глубже родительской группы
- **`ItemCount` учитывается только для листовых узлов** (`Children.Count == 0`) — родительские узлы не имеют собственных строк детализации, их «строки» = дочерние группы
- При сворачивании группы, если текущая страница становится за пределами `maxPage = ceil(TotalCount / PageSize)`, происходит автоматический возврат на `maxPage`

### Отображение колонок
- Колонки, участвующие в группировке, скрываются в гриде через `Hidden` на `TemplateColumn`:
  ```razor
  Hidden="@(_dataGrid?.GroupColumns.Contains("SqlColName") ?? false)"
  ```
- Иконка раскрытия/сворачивания и название группы отображаются в первой колонке (Код)

### Групповой WHERE
- В подзапросах `FROM (SELECT ...) _g` / `FROM (SELECT ...) _src` видны только выходные имена колонок (не табличные алиасы)
- Для grouped-режима `BuildWhereClause` должен использовать выходные имена колонок: `"НазваниеАнализа"`, `"TestTypeName"` (не `"a.НазваниеАнализа"`, `"t.ТипМедицинскогоАнализа"`)
- В плоском режиме WHERE напрямую в SELECT, табличные алиасы допустимы: `"a.НазваниеАнализа"`

### `GroupExprMap` — сопоставление SQL-имён колонок
- В странице определяется словарь, сопоставляющий имена колонок из `GroupColumns` с их выходными именами в подзапросе:
  ```csharp
  private static readonly Dictionary<string, string> GroupExprMap = new()
  {
      ["TestTypeName"] = "TestTypeName",
      ["КодМедицинскогоАнализа"] = "КодМедицинскогоАнализа",
      ["НазваниеАнализа"] = "НазваниеАнализа",
      ["Порядок"] = "Порядок"
  };
  ```

### Порядок сортировки в групповых запросах
- Групповой агрегатный запрос учитывает направление сортировки из `SortColumns`:
  ```sql
  ORDER BY TestTypeName DESC, Порядок ASC
  ```
- Детальные строки внутри группы сортируются по колонкам, НЕ участвующим в группировке
- **Запрещено** пересортировывать список агрегатов после получения из БД (`aggregates.OrderBy(...)`) — это уничтожает порядок, заданный `ORDER BY` в SQL. Синтетические родительские узлы строятся непосредственно внутри цикла `foreach (var gr in groupRows)` до листового узла, поэтому при обходе `aggregates` (без `.OrderBy`) порядок родитель-перед-детьми всегда соблюдается

## Key conventions
- All Razor markup and user-visible text is **Russian**
- No tests exist in this repo
- No CI/CD, no linter configuration
- `[Column]` attributes use exact Russian database column names — do not translate
- SQL queries reference tables by Russian names (e.g. `МедицинскиеАнализы`, `МедицинскиеАнализыТипы`)
- Each SQL constant must be documented with `///` XML doc and `--` inline SQL comments for every column
- Every public/protected class, method, property, and field must have a `/// <summary>` XML doc comment
- Database column names are defined exactly once in `ColumnNames.cs` and referenced from `[Column]` attributes (SQLQueries constants are exempt)
- Data loading goes in `OnAfterRenderAsync(bool firstRender)` with `if (firstRender)` guard, **not** in `OnInitializedAsync` — avoids double-load from Blazor prerendering
- Sorting, searching, grouping, and **pagination** for data grids must be performed on SQL Server side (not in-memory)
- Sort column headers call `async Task ToggleSort(string column)` via `@onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("SqlColumn"); })"` — `_dataGrid` is set via `@ref` on `KescoDataList`. Chips in the grouping tray use `@onclick="async () => await ToggleSort(col)"` inside KescoDataList itself. **Do not call `ToggleSort` as fire-and-forget void** — Blazor will not await the data reload
- `appsettings.Development.json` is gitignored — use it for local connection strings
- All modal/dialog windows must be draggable
- Data grid header row must be fixed (not scroll with data) — `KescoDataList` does this automatically
- При вызове `Db.ExecuteAsync()` с сырым SQL обязательно передавать `commandType: CommandType.Text` — по умолчанию `ExecuteAsync` использует `CommandType.StoredProcedure`
- `DapperColumnMapper` делает fallback на имя свойства, если `[Column]`-атрибут не совпал с колонкой результата — это позволяет использовать SQL-алиасы (`SELECT КодТипа AS Id`) даже при наличии `[Column("КодТипа")]` на свойстве `Id`
- **OnQueryChanged**: обновлять свойства `_query`, а не переприсваивать объект — иначе `TotalCount` сбрасывается в 0 при async-рендере. `ExpandedGroups` управляется страницей и **не перезаписывается** из query
- **Запрещено** подставлять значения параметров в SQL-строку — все параметры передаются через Dapper (`@param`). Параметры пагинации передаются как `@__start`/`@__end` через `DynamicParameters`
- **Обработка ошибок БД**: `DbManager` автоматически перехватывает `SqlException` и вызывает `ISqlErrorHandler.HandleSqlError()`. Страницам **не нужно** вызывать `ErrorService.Report()` вручную — только `try/finally` для `_loading = false`. Баннер `KescoErrorBar` в `MainLayout` показывает ошибку со строкой подключения, SQL и параметрами
- **Grouping tray**: заголовки колонок должны быть `draggable="true"` с `@ondragstart`,
  устанавливающим `KescoDragState.DraggedColumn`. При перетаскивании на панель группировки колонка
  добавляется автоматически. Сортировка по сгруппированным колонкам разрешена (клик по чипу в трее).
  Панель скрыта по умолчанию (`_trayExpanded = false`) и открывается кнопкой `AccountTree` в тулбаре.
  Кнопки тулбара (группировка, добавить) используют `MudIconButton` с CSS-классами `grouping-toggle-btn` /
  `toolbar-add-btn` и тултипами — не `MudButton Variant.Filled`
- **Grouping tray toggle**: кнопка `AccountTree` включает/выключает трей. При выключении (`_trayExpanded = false`) очищает `_groupColumns` и перезагружает данные в плоском режиме — колонки возвращаются в грид
- **Grid height**: вычисляется динамически через `_gridHeight`: `calc(100vh - 280px)` без трея, `calc(100vh - 330px)` с треем. Заголовок грида фиксирован (`FixedHeader="true"`)
- **Responsive layout**: тулбар и пагинация обёрнуты в `<div>` с `flex-wrap` — элементы переносятся на узких экранах. Внутренние группы используют `MudStack` для вертикального центрирования
- **Navigation drawer**: `DrawerVariant.Responsive` — десктоп (persistent, сдвигает контент), мобильные (temporary overlay). Кнопка-гамбургер меняет иконку `Menu` ↔ `MenuOpen`. AppBar `z-index: 1301` всегда выше overlay. Overlay начинается ниже AppBar + gold underline (`top: 51px`)
- `Program.cs` регистрирует `KescoErrorService` как Scoped и как `ISqlErrorHandler`, передаёт `ISqlErrorHandler` в конструктор `DbManager`
