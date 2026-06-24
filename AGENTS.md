# KescoBZ — Медицинские исследования

## Agent instructions
- Перед внесением изменений, если требования неоднозначны, задавай уточняющие вопросы через опросник (question tool)

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
| **KescoGrid\<T>** — грид с серверной пагинацией, поиском, сортировкой, группировкой, фильтрацией по колонкам. Конфигурация передаётся через параметры: `SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`, `DataLoader`, `ColumnMenuMode` | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **KescoGridPageBase\<T>** — базовый класс страниц с гридом. Читает конфигурацию SQL из `Grid` (IKescoGrid). Предоставляет `LoadData`, `ToggleGroup`, `OpenAddDialog`, `OnRowClicked`. Авто-вычисляет `FilterColumnTypes` | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **KescoColumn\<T>** — колонка грида с авто-заголовком. Получает Title/SortName/Drag&Drop из `KescoColumnDef` по `ColumnId`. Скрывается при группировке. Кнопка меню ⋮ для мобильных | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **KescoColumnDef** — невидимый регистратор метаданных колонки: `ColumnId` (EditorRequired), `SqlName`, `DisplayName`, `SortName`, `Groupable`, `Filterable` | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **KescoGroupHeader** — заголовок строки группы с иконкой раскрытия/сворачивания и количеством элементов | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **KescoDragState** — статическое хранилище SQL-имени перетаскиваемой колонки между dragstart и drop | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **KescoColumnFilterDialog** — диалог настройки фильтра по колонке с типо-зависимыми операторами | [docs/kesco-column-filter-dialog.md](docs/kesco-column-filter-dialog.md) |
| **KescoEditForm\<T>** — MudDialog с валидацией, сохранением, удалением | [docs/kesco-edit-form.md](docs/kesco-edit-form.md) |
| **KescoComboBox\<TItem>** — выпадающий список для `ILookupEntity` | [docs/kesco-combo-box.md](docs/kesco-combo-box.md) |
| **KescoErrorBar** — баннер ошибок БД с детализацией (SQL, параметры) | [docs/kesco-error-bar.md](docs/kesco-error-bar.md) |
| **ConfirmDialog** — диалог подтверждения | [docs/confirm-dialog.md](docs/confirm-dialog.md) |
| **ILookupEntity** — интерфейс справочной сущности (`int Id`, `string Name`) | [docs/entity-crud.md](docs/entity-crud.md) |
| **KescoTheme** — corporate theme (DarkNavy + Gold accent). Applied in MainLayout | — |

### Интерфейсы

| Интерфейс | Назначение |
|---|---|
| **IKescoGrid** — контракт KescoGrid: `SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`, `ColumnMenuMode`, `IsGrouped`, `ToggleSort`, `GetSortBadge`, `GetColumnMeta`, `AddGroupAsync`, `AddFilterAsync`, регистрация колонок | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **IKescoGridDataLoader** — контракт обратного вызова: `OnQueryChangedAsync(KescoDataQuery)`. Реализуется KescoGridPageBase, передаётся через `DataLoader="this"` | [docs/kesco-grid.md](docs/kesco-grid.md) |
| **KescoColumnMeta** — метаданные зарегистрированной колонки: `ColumnId`, `SqlName`, `DisplayName`, `SortName`, `Groupable`, `Filterable` | [docs/kesco-grid.md](docs/kesco-grid.md) |

### Services

| Сервис | Назначение |
|---|---|
| **KescoErrorService** (Scoped) — хранит состояние последней ошибки SQL, реализует `ISqlErrorHandler`. Используется `KescoErrorBar` |
| **ISqlErrorHandler** (DALC) — интерфейс, вызываемый `DbManager` при `SqlException`. Регистрируется в DI |

## Server-side grouping architecture

Группировка выполняется **на стороне SQL Server** двумя отдельными запросами (подход DevExpress Blazor Grid). Реализация — `KescoGroupingEngine` (статический класс в `Components/Grid/KescoGroupingEngine.cs`).

1. **Запрос групповых агрегатов** — `GROUP BY` + `COUNT(*)`, возвращает уникальные значения группировки и количество записей
2. **Запрос детальных строк** — выборка конкретных записей с `ROW_NUMBER()` и фильтром по значениям группы

### Модель данных
- `IGridRow` — маркерный интерфейс строки в плоском списке (`Kesco.Lib.Entities/GridRow.cs`)
- `GroupHeaderRow` — заголовок группы: `FullKey`, `DisplayValue`, `ItemCount`, `Depth`, `IsExpanded`
- `DetailRow<T>` — обёртка сущности: `Item`, `GroupKey`, `Depth`
- `GroupedPage<T>` — результат запроса: `Rows` (плоский список) + `TotalEffectiveRows`
- `KescoDataQuery.ExpandedGroups` — `HashSet<string>` полных ключей развёрнутых групп (разделитель `\u001F`)

### Рендеринг
- **Плоская модель**: заголовки групп и строки детализации передаются как единый `IEnumerable<IGridRow>`
- Колонки используют компонент `<KescoColumn>` с проверкой типа в `CellTemplate`:
  ```razor
  <KescoColumn TEntity="IGridRow" ColumnId="2">
      <CellTemplate>
          @if (context.Item is GroupHeaderRow header)
          {
              <KescoGroupHeader Header="header" OnToggle="ToggleGroup" />
          }
          else if (context.Item is DetailRow<MyEntity> detail)
          {
              <MudText Style="@($"padding-left:{(detail.Depth + 1) * 16}px")">@detail.Item.Id</MudText>
          }
      </CellTemplate>
  </KescoColumn>
  ```
- `KescoGroupHeader` — встроенный компонент для отображения иконки раскрытия/сворачивания и количества элементов
- `KescoColumn` автоматически получает Title (DisplayName), строит HeaderTemplate с drag&drop и серверной сортировкой, скрывает колонку при группировке
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
- Колонки, участвующие в группировке, скрываются в гриде автоматически — `KescoColumn` вычисляет `Hidden` через `IsGrouped(SqlName)` из `KescoColumnMeta`
- Иконка раскрытия/сворачивания и название группы отображаются в первой колонке (Код) через `KescoGroupHeader`

### Имена колонок в WHERE и GROUP BY
- `SearchColumns` передаются как выходные имена (например, `"НазваниеАнализа"`, `"TestTypeName"`)
- SQL-пагинация через `ROW_NUMBER()` оборачивает SELECT в подзапрос `FROM (SELECT ...) _src`, где выходные имена колонок видны напрямую — алиасы таблиц не нужны
- `GroupColumns` содержат те же выходные имена — они напрямую используются в `GROUP BY`
- `KescoGridPageBase` читает `SearchColumns`, `SelectSql`, `DefaultOrder` из `Grid` (реализация `IKescoGrid`) — **abstract-свойства не нужны**, вся конфигурация передаётся через параметры `<KescoGrid>`

### Порядок сортировки в групповых запросах
- Групповой агрегатный запрос учитывает направление сортировки из `SortColumns`:
  ```sql
  ORDER BY TestTypeName DESC, Порядок ASC
  ```
- Детальные строки внутри группы сортируются по колонкам, НЕ участвующим в группировке
- **Запрещено** пересортировывать список агрегатов после получения из БД (`aggregates.OrderBy(...)`) — это уничтожает порядок, заданный `ORDER BY` в SQL. Синтетические родительские узлы строятся непосредственно внутри цикла `foreach (var gr in groupRows)` до листового узла, поэтому при обходе `aggregates` (без `.OrderBy`) порядок родитель-перед-детьми всегда соблюдается

## Server-side column filtering

Фильтрация по колонкам выполняется **на стороне SQL Server** через `BuildColumnFilterClause`.
UI — панель фильтров (filter tray) с drag-and-drop заголовков и диалогом `KescoColumnFilterDialog` для настройки условий.

### Модель данных
- `ColumnType` — тип данных колонки: `Text` (10 операторов: Contains/NotContains/Equals/NotEquals/StartsWith/NotStartsWith/EndsWith/NotEndsWith/IsEmpty/IsNotEmpty), `Number` (равенство + сравнения >/</>=/<=), `Boolean` (Equals)
- `ColumnFilterOperator` — оператор сравнения: `Contains`, `NotContains`, `Equals`, `NotEquals`, `StartsWith`, `NotStartsWith`, `EndsWith`, `NotEndsWith`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `IsEmpty`, `IsNotEmpty`
- `LogicalOperator` — `And` / `Or` для объединения двух условий на одной колонке
- `ColumnFilter` — условие фильтра: `Column`, `ParamName`, `Operator`, `Value` + опциональные `LogicalOperator`, `SecondOperator`, `SecondValue`, `SecondParamName` (до двух условий на колонку)
- `KescoDataQuery.ColumnFilters` — `Dictionary<string, ColumnFilter>` — ключ = SQL-имя колонки

### SQL-генерация
- `KescoDataQuery.BuildColumnFilterClause(DynamicParameters parameters, Dictionary<string, string>? columnNameMap)` — генерирует WHERE-фрагмент (`col LIKE @p` / `col = @p` / `col > @p` и т.д.) и добавляет параметры в `DynamicParameters`
- `columnNameMap` — опциональный маппинг имён (например, `"TestTypeName"` → `"t.ТипМедицинскогоАнализа"`) для плоского режима, где имена колонок в SELECT отличаются от подзапросного режима

### Filter tray
- Панель включается кнопкой `FilterAlt` (`ShowFilterTray="true"`), скрыта по умолчанию (`_filterTrayExpanded = false`)
- Добавление фильтра: перетаскивание заголовка колонки на панель → открывается `KescoColumnFilterDialog`
- Редактирование: клик по чипу фильтра → повторно открывается диалог с текущими значениями
- Удаление: клик по × на чипе
- При выключении панели все фильтры сбрасываются, данные перезагружаются
- Чип показывает читаемое описание: `«Название содержит «грипп»»` или для двух условий `«Название: содержит «грипп» И не содержит «ковид»»` (через `KescoColumnFilterDialog.GetFilterDescription`)
- Filter tray не конфликтует с grouping tray — оба могут быть открыты одновременно

### Интеграция на странице (через KescoGridPageBase\<T>)
Конфигурация SQL передаётся через параметры `<KescoGrid>`:
```razor
<KescoGrid TEntity="IGridRow"
           DataLoader="this"
           SelectSql="@SQLQueries.SELECT_МоиЗаписи"
           SearchColumns="@(new[]{"НазваниеАнализа","TestTypeName"})"
           DefaultOrder="Порядок, НазваниеАнализа"
           ... >
```
`KescoGridPageBase<T>` автоматически читает `SelectSql`, `SearchColumns`, `DefaultOrder` из `IKescoGrid`, строит WHERE через `BuildWhereClause`/`BuildColumnFilterClause` и вызывает `Entity.GetPagedAsync`/`Entity.GetCountAsync`. В плоском и группированном режимах `SearchColumns` одни и те же — используются выходные имена колонок (видимые в подзапросе `ROW_NUMBER()`).

`FilterColumnTypes` вычисляется автоматически через рефлексию по `[Column]`-атрибутам и C#-типам свойств сущности.
Страница просто передаёт `FilterColumnTypes="@FilterColumnTypes"` в `<KescoGrid>`. Маппинг: `bool` → `Boolean`, числовые типы → `Number`, остальные → `Text`.

## Key conventions
- All Razor markup and user-visible text is **Russian**
- No tests exist in this repo
- No CI/CD, no linter configuration
- **Использовать готовые компоненты из `src\Kesco.Lib.Web.BZ.Controls`** при разработке форм (KescoEditForm, KescoComboBox, ConfirmDialog, KescoColumnFilterDialog, KescoErrorBar). Проверять наличие подходящего компонента перед использованием MudBlazor-компонентов напрямую.
- `[Column]` attributes use exact Russian database column names — do not translate
- SQL queries reference tables by Russian names (e.g. `МедицинскиеАнализы`, `МедицинскиеАнализыТипы`)
- Each SQL constant must be documented with `///` XML doc and `--` inline SQL comments for every column
- Every public/protected class, method, property, and field must have a `/// <summary>` XML doc comment
- Database column names are defined exactly once in `ColumnNames.cs` and referenced from `[Column]` attributes (SQLQueries constants are exempt)
- Data loading goes in `OnAfterRenderAsync(bool firstRender)` with `if (firstRender)` guard, **not** in `OnInitializedAsync` — avoids double-load from Blazor prerendering
- Sorting, searching, grouping, and **pagination** for data grids must be performed on SQL Server side (not in-memory)
- Sort column headers call `async Task ToggleSort(string column)` via `@onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("SqlColumn"); })"` — `_dataGrid` is set via `@ref` on `KescoGrid`. Chips in the grouping tray use `@onclick="async () => await ToggleSort(col)"` inside KescoGrid itself. **Do not call `ToggleSort` as fire-and-forget void** — Blazor will not await the data reload
- `appsettings.Development.json` is gitignored — use it for local connection strings
- All modal/dialog windows must be draggable
- Data grid header row must be fixed (not scroll with data) — `KescoGrid` does this automatically
- При вызове `Db.ExecuteAsync()` с сырым SQL обязательно передавать `commandType: CommandType.Text` — по умолчанию `ExecuteAsync` использует `CommandType.StoredProcedure`
- `DapperColumnMapper` делает fallback на имя свойства, если `[Column]`-атрибут не совпал с колонкой результата — это позволяет использовать SQL-алиасы (`SELECT КодТипа AS Id`) даже при наличии `[Column("КодТипа")]` на свойстве `Id`
- **OnQueryChanged**: обновлять свойства `_query`, а не переприсваивать объект — иначе `TotalCount` сбрасывается в 0 при async-рендере. `ExpandedGroups` управляется страницей и **не перезаписывается** из query. `ColumnFilters` копируется из query целиком (словарь)
- **KescoGridPageBase**: SQL-конфигурация передаётся через параметры `<KescoGrid>` (`SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`), а не через abstract-свойства. База читает их из `Grid?.SelectSql` и т.д. Страница переопределяет `protected override IKescoGrid? Grid => _dataGrid;` и передаёт `DataLoader="this"`
- **Запрещено** подставлять значения параметров в SQL-строку — все параметры передаются через Dapper (`@param`). Параметры пагинации передаются как `@__start`/`@__end` через `DynamicParameters`
- **Обработка ошибок БД**: `DbManager` автоматически перехватывает `SqlException` и вызывает `ISqlErrorHandler.HandleSqlError()`. Страницам **не нужно** вызывать `ErrorService.Report()` вручную — только `try/finally` для `_loading = false`. Баннер `KescoErrorBar` в `MainLayout` показывает ошибку со строкой подключения, SQL и параметрами
- **Grouping tray**: заголовки колонок генерируются автоматически через `<KescoColumn>` — drag-and-drop (`draggable="true"` с `@ondragstart`,
  устанавливающим `KescoDragState.DraggedColumn`) и серверная сортировка (`@onclick` → `Grid.ToggleSort(query.SqlName)`) встроены в компонент.
  При перетаскивании на панель группировки колонка добавляется автоматически. Сортировка по сгруппированным колонкам разрешена (клик по чипу в трее).
  Панель скрыта по умолчанию (`_trayExpanded = false`) и открывается кнопкой `AccountTree` в тулбаре.
  Кнопка группировки появляется автоматически при наличии хотя бы одного `KescoColumnDef` с `Groupable="true"`.
  Кнопки тулбара (группировка, фильтрация, добавить, выбрать, групповые операции) используют `MudIconButton` с CSS-классами `grouping-toggle-btn` /
  `filter-toggle-btn` / `toolbar-add-btn` / `toolbar-select-btn` / `toolbar-batch-btn` и тултипами — не `MudButton Variant.Filled`
- **Column registration**: метаданные колонок регистрируются через `<KescoColumnDef SqlName="..." DisplayName="..." Groupable="true" Filterable="true" />`
  внутри `<ColumnDefs>`, а не через параметры `ShowGroupingTray`/`AvailableGroupColumns`/`AvailableFilterColumns`.
  `KescoGrid` реализует `IKescoGrid` и получает регистрацию через каскадный параметр.
- **Column reorder**: `KescoColumn` **обязан** устанавливать `DragAndDropEnabled="true"` на своём `TemplateColumn`. Без этого флага MudBlazor `MudDropContainer` не распознаёт колонку как участника `DragDropColumnReordering` — заголовок перетаскивается, но не встаёт на новое место (dragstart срабатывает от вложенного `draggable="true"` div, а не от `MudDynamicDropItem`).
- **Grouping tray toggle**: кнопка `AccountTree` включает/выключает трей. При выключении (`_trayExpanded = false`) очищает `_groupColumns` и перезагружает данные в плоском режиме — колонки возвращаются в грид. Кнопка появляется только при наличии хотя бы одного `KescoColumnDef` с `Groupable="true"`
- **Filter tray toggle**: кнопка `FilterAlt` включает/выключает трей фильтрации. При выключении (`_filterTrayExpanded = false`) очищает `_activeFilters` и перезагружает данные без фильтров. Оба трея (группировка + фильтрация) могут быть открыты одновременно — высота грида уменьшается соответственно
- **Tray borders & icons**: панели группировки (`.grouping-tray`) и фильтрации (`.filter-tray`) имеют идентичное оформление. Фон, границы и иконки используют MudBlazor-переменные (`--mud-palette-*`) для авто-адаптации к светлой/тёмной теме. `border-left: 3px solid var(--mud-palette-primary)`, `border-bottom: 2px solid var(--lh-gold)`, фон `var(--mud-palette-background-gray)`. Иконки: по умолчанию `var(--mud-palette-text-secondary)` + opacity 0.45; при наличии чипов в трее (`:has(.xxx-chip)`) — `var(--mud-palette-primary)` + opacity 1. При наведении/перетаскивании `border-left-color` меняется на `var(--lh-gold)`. CSS определён в `wwwroot/css/app.css`
- **Grid height**: вычисляется динамически через `_gridHeight`: `calc(100vh - 280px)` без треев, `calc(100vh - 330px)` с одним треем, `calc(100vh - 380px)` с двумя. Заголовок грида фиксирован (`FixedHeader="true"`)
- **Responsive layout**: тулбар и пагинация обёрнуты в `<div>` с `flex-wrap` — элементы переносятся на узких экранах. Внутренние группы используют `MudStack` для вертикального центрирования
- **Mobile menu**: колонки грида имеют кнопку `⋮` для доступа к группировке и фильтрации без drag-and-drop. Режим управляется параметром `ColumnMenuMode` (по умолчанию `Mobile` — только ≤960px). Пункты меню показываются только когда соответствующая панель (группировка/фильтрация) активирована
- **Navigation drawer**: `DrawerVariant.Responsive` — десктоп (persistent, сдвигает контент), мобильные (temporary overlay). Кнопка-гамбургер меняет иконку `Menu` ↔ `MenuOpen`. AppBar `z-index: 1301` всегда выше overlay. Overlay начинается ниже AppBar + gold underline (`top: 51px`)
- `Program.cs` регистрирует `KescoErrorService` как Scoped и как `ISqlErrorHandler`, передаёт `ISqlErrorHandler` в конструктор `DbManager`
