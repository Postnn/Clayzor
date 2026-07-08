
**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

# Clayzor — Медицинские исследования

## Agent instructions
- Перед внесением изменений, если требования неоднозначны, задавай уточняющие вопросы через опросник (question tool)
- Изменения в git (add, commit, push, etc.) — **только по прямому указанию**. Никогда не коммитить автоматически после завершения задачи.
- Актуализация AGENTS.md и документации (`docs/`) — **только по прямому указанию**. Не обновлять документацию в рамках других задач без явной просьбы.

## Stack
- .NET 10 Blazor Server (Interactive Server)
- MudBlazor 9.x — `Variant.Outlined` is the standard form input style
- MudBlazor.Extensions 9.x — дополнительные возможности MudBlazor (перетаскивание диалогов и др.)
- Dapper + Microsoft.Data.SqlClient 6.x (SQL Server)
- Windows Integrated Auth (Kerberos/NTLM via Negotiate)
- **SQL Server 2008 R2** — использовать только синтаксис, доступный в этой версии

## Build & Run
```
dotnet build Clayzor.sln
dotnet run --project src\Clayzor.App.Web.MedicalTests
```
Dev URL: `http://localhost:5010`

## Tests
```
dotnet test tests\Clayzor.Lib.Web.Controls.Tests
```
Тестовый проект: xUnit, `net10.0`. Покрывает SQL-билдер, модель фильтра, JSON/URL-сериализацию, описание и индикатор. См. `tests/Clayzor.Lib.Web.Controls.Tests/`.

Watch with hot reload (VS Code task also available):
```
dotnet watch run --project src\Clayzor.App.Web.MedicalTests
```

## Project dependency chain
```
Clayzor.Lib.DALC          ← Dapper, SqlClient
Clayzor.Lib.Entities       ← DALC + Dapper (column mapping)
Clayzor.Lib.Web.Settings   ← config bindings (no DB dep)
Clayzor.Lib.Web.Controls ← Entities + Web.Settings + MudBlazor (Razor Class Library)
Clayzor.App.Web.MedicalTests ← all of the above (web app entry point)
```

## Configuration — connection string priority

`BindClaySettings()` in `Clayzor.Lib.Web.Settings/ClayAppSettings.cs` merges:
1. `web.config` (XML) — loaded via `AddWebConfig()` alongside `appsettings.json`
2. `ClayApp:ConnectionString` (any source) → `ConnectionStrings:DefaultConnection` (fallback)

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

`App.razor` must also include Clayzor RCL JS scripts:
```html
<script src="_content/Clayzor.Lib.Web.Controls/js/clayColumnSettings.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridColumnDrag.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridPrint.js"></script>
<script src="_content/Clayzor.Lib.Web.Controls/js/clayGridExcel.js"></script>
```

## Database access pattern

**No repository layer, no ORM, no migrations.** All SQL lives in `Clayzor.Lib.Entities/SQLQueries.cs` as named constants.

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

## Shared components (Clayzor.Lib.Web.Controls)

| Компонент | Документация |
|---|---|
| **ClayGrid\<T>** — грид с серверной пагинацией, поиском, сортировкой, группировкой, фильтрацией по колонкам. Разметка в `ClayGrid.razor`, логика в 9 partial class-файлах (1 основной + 8 по темам, см. «Codebehind-структура» ниже). При `EditDialogType != null` автоматически добавляет сервисную колонку (первой) с иконкой карандаша для открытия диалога редактирования. Конфигурация передаётся через параметры: `SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`, `DataLoader`, `ColumnMenuMode`. `OnGroupToggle` — страница подписывается на раскрытие/сворачивание групп (грид сам рендерит `ClayGroupHeader` в вычисленной хост-колонке `GroupRowHostKey`)| [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayGridPageBase\<T>** — базовый класс страниц с гридом в 5 partial-файлах (1 основной + 4 по темам, см. «Codebehind-структура ClayGridPageBase» ниже). Читает конфигурацию SQL из `Grid` (IClayGrid). Предоставляет `LoadData`, `ToggleGroup`, `OpenAddDialog`. Авто-вычисляет `FilterColumnTypes` | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayColumn\<T>** — колонка грида с авто-заголовком. Получает Title/SortName/Drag&Drop из `ClayColumnDef` по `ColumnId`. Скрывается при группировке. Кнопка меню ⋮ для мобильных | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayColumnDef** — невидимый регистратор метаданных колонки: `ColumnId` (EditorRequired), `SqlName`, `DisplayName`, `SortName`, `Groupable`, `Filterable`, `AllowValueFilter`, `BoolTrueLabel`, `BoolFalseLabel` | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayGroupHeader** — заголовок строки группы с иконкой раскрытия/сворачивания и количеством элементов. Рендерится гридом автоматически в вычисленной хост-колонке (`GroupRowHostKey`) — страница напрямую компонент не вызывает, только подписывается на `OnGroupToggle` | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayDragState** — статическое хранилище SQL-имени перетаскиваемой колонки между dragstart и drop | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayGridPrintHtmlGenerator** — статический генератор HTML для печати всех данных грида. Генерирует HTML с теми же MudBlazor CSS-классами (`.mud-table`, `.mud-table-cell`, `.group-header-cell`) и встраивает полный `@media print` CSS — визуально идентичен печати текущей страницы | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayGridPrintStyles** — символы для печатных форм: иконки групп (`▸`/`▾` — аналог MudBlazor ChevronRight/ExpandMore), булевы значения (`✓`/`✗` — аналог CheckCircle/Cancel) | — |
| **ClayColumnFilterDialog** — диалог настройки фильтра по колонке с типо-зависимыми операторами. Использует `ClayFilterValueEditor` для редакторов значений. Параметр `InitialOperator` позволяет задать начальный оператор для нового фильтра | [docs/clay-column-filter-dialog.md](docs/clay-column-filter-dialog.md) |
| **ClayColumnValueFilterDialog** — диалог фильтра по уникальным значениям (Excel-style): кастомные чекбоксы (как в гриде), «выделить все» (tri-state), контекстные условия через `MudMenu`, ленивая загрузка, обработка порога 100, взаимоисключение с фильтром по условию. Возвращает `ValueFilter`, `Cleared`, `OpenConditionRequest` или `RemoveCondition` | [docs/clay-grid.md](docs/clay-grid.md) |
| **OpenConditionRequest** — record для маршрутизации из диалога значений в форму условия с пресетом оператора | — |
| **ClayFilterOption** — класс варианта для выпадающего списка значения фильтра: `Value` (object?), `Label` (string) | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayFilterValueEditor** — единый редактор значения фильтра по типу колонки (Text/Number/Decimal/Date/Boolean/lookup). Скрывается при операторах без значения. Переиспользуется в `ClayColumnFilterDialog` и `ClayFilterDialog` | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayFilterOperatorLabels** — статический хелпер: читаемые русские метки операторов фильтрации. Переиспользуется в `ClayColumnFilterDialog` и `ClayFilterDialog` | — |
| **ClayFilterStrings** — единый источник строковых констант UI фильтра (заголовки, кнопки, подписи). Устраняет хардкод русских строк в разметке диалогов | — |
| **ClayFilterJsonConverter** — `JsonConverter<IClayFilterNode>` с дискриминатором `$type` (`"group"`/`"column"`/`"value"`). Транзиентные поля (`ParamName`, `SecondParamName`, `ParamPrefix`, computed-свойства) исключены через `[JsonIgnore]` | — |
| **ClayFilterUrlHelper** — статический хелпер: дерево → JSON → DeflateStream → Base64Url (и обратно). Query-параметр `filter`. Используется `ClayGridPageBase` для восстановления фильтра при первой загрузке | — |
| **ClayFilterExpression** — редактор одного листового условия составного фильтра. Компактная однорядная раскладка (`flex-wrap:wrap`, `Dense="true"`): Поле / Условие / Значение / ✕. Автофокус на «Значение» после смены колонки/оператора (через `@key` ремоунт + `AutoFocus`). При `Node.IsNew` (условие добавлено перетаскиванием) сразу фокусирует значение | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayFilterGroup** — рекурсивный узел-группа составного фильтра с `MudToggleGroup` И/ИЛИ, кнопками добавления условия/группы (в одной строке с переключателем) и удаления. Корневая группа (`IsRoot=true`) рендерится плоско (без рамки/отступа). Не-корневые — компактно (`gap:6px`, `border-left:2px`). `GetLeafDescription` делегирует в `ClayFilterDescriptionBuilder.DescribeLeaf` (оба клауза). Условия `ColumnDialog` отображаются read-only с кнопкой удаления (крестик) | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayFilterDialog** — диалог настраиваемого (составного) фильтра. Фиксированная высота через скоупленный `ContentClass` (`width:600px; height:min(460px,80vh); overflow:hidden; flex column`). Описание — фиксировано сверху (`flex:0 0 auto`), дерево условий — единственная прокручиваемая зона (`flex:1; overflow-y:auto; min-height:0`). `DragMode=Simple` (перетаскивается). Всегда рендерит корневую группу, работает с глубокой копией дерева, возвращает результат через `DialogResult.Ok(ClayFilterGroupNode)` | [docs/clay-grid.md](docs/clay-grid.md) |
| **FilterSegment** — кликабельный сегмент в панели фильтра: `Text`, `Source` (ColumnDialog/CompositeDialog/ValueFilter), `Column` (маршрутизация клика) | — |
| **DistinctValuesResult** — результат `LoadDistinctValuesAsync`: `Values` (IReadOnlyList<object?>), `Capped` (> лимита 100), `HasBlanks` (есть NULL/пустые), `TotalDistinct` (полное число) | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayFilterDescriptionBuilder** — статический построитель: `BuildSegments(root, getDisplayName, getColumnMeta?)` → список кликабельных сегментов; `BuildText(root, getDisplayName, getColumnMeta?)` → строка описания для экспорта/печати; `DescribeLeaf(leaf, getDisplayName)` → текст одного ColumnFilter с обоими клаузами; `DescribeValueFilter(vf, getDisplayName, getColumnMeta?)` → текст фильтра по значению («одно из [v1, v2]» / «кроме [...]»). V8: поддержка `ValueFilter` в сегментах и тексте, форматирование через `ColumnTypeDescriptor.Format` и `BoolTrueLabel`/`BoolFalseLabel` | — |
| **ClayColumnSettingsDialog** — диалог настройки порядка, видимости, сортировки и фильтра по значению колонок (jQuery UI Sortable drag-and-drop с авто-прокруткой). Параметр `ShowSorting` (default `true`) — скрывает секцию сортировки для режима печати/экспорта. V8: sticky-заголовок с иконками `Visibility`/`Checklist`, переключатель `AllowValueFilter` (только при `ShowSorting`). Валидация: нельзя применить с нулём видимых колонок | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayColumnSettingsPromptDialog** — лёгкий диалог с тремя исходами перед печатью/экспортом: «Выбрать колонки» (→ диалог настройки), «Как на странице» (→ текущий вид), «Отмена». Параметр `ContextLabel` — контекст операции | — |
| **ClayEditForm\<T>** — MudDialog с валидацией, сохранением, удалением | [docs/clay-edit-form.md](docs/clay-edit-form.md) |
| **ClayComboBox\<TItem>** — выпадающий список для `ILookupEntity`. Рендерит `MudSelect` с `Variant="Variant.Outlined"`, `Margin="Margin.Dense"`, `Dense="true"` и `PopoverClass="clay-combo-popover"`. CSS-правила `.clay-combo-popover` (overflow, max-height, line-height, font-size) живут в `app.css` | [docs/clay-combo-box.md](docs/clay-combo-box.md) |
| **ClayErrorBar** — баннер ошибок БД с детализацией (SQL, параметры) | [docs/clay-error-bar.md](docs/clay-error-bar.md) |
| **ClayButton** — обёртка `MudTooltip` + `MudIconButton` с авто-сбросом тултипа после клика. Заменяет пару `<MudTooltip><MudIconButton/></MudTooltip>` | — |
| **ClayMenu** — обёртка `MudMenu` с авто-построением кнопки-активатора (опциональный тултип, сброс тултипа после клика). Заменяет `<MudMenu><ActivatorContent><MudTooltip><MudIconButton/></MudTooltip></ActivatorContent></MudMenu>` | — |
| **ClayCheckbox** — контролируемый (controlled) чекбокс с tri-state поддержкой (`State`: `true`/`false`/`null`). Кастомный `<span>`-глиф (16×16, CSS-галочка border-rotate). Используется для выбора записей в гриде и фильтра по значению | — |
| **ConfirmDialog** — диалог подтверждения | [docs/confirm-dialog.md](docs/confirm-dialog.md) |
| **ILookupEntity** — интерфейс справочной сущности (`int Id`, `string Name`) | [docs/entity-crud.md](docs/entity-crud.md) |
| **ClayTheme** — corporate theme (DarkNavy + Gold accent). PaletteLight references `ClayColors.*` (single source of truth for brand hex values). Typography references CSS variables `--clay-font-family` (Verdana) and `--clay-font-size` (0.8rem). Applied in MainLayout. **Important**: palette values must be C# hex literals, NOT `var(...)` — MudBlazor parses them via `MudColor.Parse()` | — |
| **ClayColors** — public C# constants (`public const string`) for every brand color. Single source of truth — shared by `ClayTheme.cs` and (via `--mud-palette-*` variables) by `app.css`. See STYLE_RULES.md §1 (Variant A) | — |

### Интерфейсы

| Интерфейс | Назначение |
|---|---|
| **IClayGrid** — контракт ClayGrid: `SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`, `ColumnMenuMode`, `IsGrouped`, `ToggleSort`, `GetSortBadge`, `GetColumnMeta`, `AddGroupAsync`, `AddFilterAsync`, `IsValueFilterAvailable`, `IsValueFilterActive`, `OpenValueFilterDialog` (V7), регистрация колонок | [docs/clay-grid.md](docs/clay-grid.md) |
| **IClayGridDataLoader** — контракт обратного вызова: `OnQueryChangedAsync(ClayDataQuery)`, `ExcelExportAsync(ExcelExportRequest)`, `BuildPrintHtmlAsync(columns, title, filterDescription, groupDescription)`, `BuildPrintHtmlForCurrentPageAsync(columns, title, filterDescription, groupDescription)`, `BuildPrintHtmlForSelectedAsync(...)`, `LoadDistinctValuesAsync(sqlName, query, limit)` — загрузка уникальных значений колонки для Excel-style фильтра. Реализуется ClayGridPageBase, передаётся через `DataLoader="this"` | [docs/clay-grid.md](docs/clay-grid.md) |
| **ClayColumnMeta** — метаданные зарегистрированной колонки: `ColumnId`, `SqlName`, `DisplayName`, `SortName`, `Groupable`, `Filterable`, `AllowValueFilter`, `BoolTrueLabel`, `BoolFalseLabel`, `Type` | [docs/clay-grid.md](docs/clay-grid.md) |

### Services

| Сервис | Назначение |
|---|---|
| **ClayErrorService** (Scoped) — хранит состояние последней ошибки SQL, реализует `ISqlErrorHandler`. Используется `ClayErrorBar` |
| **ISqlErrorHandler** (DALC) — интерфейс, вызываемый `DbManager` при `SqlException`. Регистрируется в DI |

### Codebehind-структура ClayGrid

После рефакторинга (задачи 04–05 мастер-плана) логика `ClayGrid<TEntity>` разнесена по partial-файлам. Все файлы объявляют `public partial class ClayGrid<TEntity> where TEntity : class` в namespace `Clayzor.Lib.Web.Controls.Components.Grid`. Базовый класс (`ComponentBase`) и реализуемые интерфейсы (`IClayGrid`, `IDisposable`, `IAsyncDisposable`) — только в основном файле.

| Файл | Строк | Содержание |
|---|---|---|
| `ClayGrid.razor` | ~640 | Разметка грида (MudDataGrid, тулбар, панели, колонки) |
| `ClayGrid.razor.cs` | ~540 | Основа: интерфейсы, параметры, поля (`_lastQuery`, `_columnById`, `_columnBySqlName`, `_columnOrder`, `_hiddenSqlNames`), инициализация, регистрация колонок, `RegisterCellTemplate`, `NotifyQueryChanged`, высота грида, `DisposeAsync`, `ColumnMenuMode`, `OpenColumnSettings`, `BuildColumnSettingsItems` (переиспользуется для печати/экспорта) |
| `ClayGrid.Search.cs` | 18 | `_searchText`, `DebounceTimer`, обработчики поиска |
| `ClayGrid.Sorting.cs` | 66 | `_sortState`, `ToggleSort`, `HandleSortClick`, `GetSortBadge` |
| `ClayGrid.Grouping.cs` | ~250 | `OnGroupToggle` (параметр-событие), `GroupRowHostKey` (авто-выбор хост-колонки для заголовка группы), `IsGroupRowHost`, `_groupColumns`, `_trayExpanded`, `AddGroupColumn`, `RemoveGroupColumn`, `OnChipDragStart/End`, `OnTrayDragOver/Drop`, `GroupColumns`, `OnGroupTriToggle`, `OnHeaderTriToggle`, `_groupChildIds` |
| `ClayGrid.Filtering.cs` | ~420 | `_filterRoot`, `HasComposite`, `ValueFilterLeaves` (V12), `_valueFilterDisabledColumns` (V8), `OpenFilterDialog` (+`initialOperator`), `OpenValueFilterDialog` (V7), `ApplyValueFilter`, `RemoveValueFilter`, `DescribeValueFilter` (V8), `BuildCurrentQuery`, `OpenCompositeFilterDialog`, чипы, фильтр-трей |
| `ClayGrid.DragDrop.cs` | 86 | `_dragSourceIndex`, drag-and-drop чипов группировки (перемещение/перестановка в трее) |
| `ClayGrid.Selection.cs` | 113 | `_selectMode`, `_selectAllChecked`, `_selectedIds`, `OnRowSelectAsync`, `SelectAllAsync`, `DeselectAllAsync`, `ToggleSelectMode`, персистентность выделения |
| `ClayGrid.ExportMenu.cs` | ~240 | `_isExporting`, `_openSubGroups`, `ToggleSubGroup`, `ResolveExportColumnsAsync` (prompt → настройка/как на странице/null), `Print{CurrentPage,Selected,All}Internal` (через `BuildPrintHtmlForCurrentPageAsync` / `BuildPrintHtmlAsync` / `BuildPrintHtmlForSelectedAsync`), `Excel{CurrentPage,Selected,All}Internal` |
| `ClayGrid.Paging.cs` | 59 | `_pageSize`, `OnPageSizeChanged`, `PrevPage`, `NextPage`, `LastPage` |

**Правила модификации:**
- Новые поля/методы добавлять в соответствующий тематический файл, а не в `ClayGrid.razor.cs`
- `ClayGrid.Filtering.cs` будет переписан задачами 10–11 (переход на дерево фильтра), поэтому изолирован
- При добавлении using — в тот файл, где используется тип
- Базовый класс и интерфейсы — только в `ClayGrid.razor.cs`

### Codebehind-структура ClayGridPageBase

После рефакторинга (задача 06 мастер-плана) логика `ClayGridPageBase<T>` разнесена по partial-файлам. Все файлы объявляют `public abstract partial class ClayGridPageBase<T> where T : Entity` в namespace `Clayzor.Lib.Web.Controls.Components.Grid`. Базовый класс (`ComponentBase`) и реализуемые интерфейсы (`IClayGridDataLoader`) — только в основном файле.

| Файл | Строк | Содержание |
|---|---|---|
| `ClayGridPageBase.cs` | ~530 | Ядро: `[Inject]`-сервисы, `Grid`, `LoadData`, `LoadFlatData`, `LoadGroupedData`, `LoadDistinctValuesAsync` (V4), `CloneFilterTreeWithoutColumn`, `CheckHasBlanksAsync`, `ToggleGroup`, `OpenAddDialog`, `IClayGridDataLoader` |
| `ClayGridPageBase.ColumnTypes.cs` | 83 | Вывод типов колонок: `_idColumnName`, `_propertyMap`, `_inferredColumnTypes`, `FilterColumnTypes`, `GetIdColumnName`, `BuildPropertyMap`, `InferFilterColumnTypes`, `MapClrTypeToColumnType` |
| `ClayGridPageBase.Export.Excel.cs` | 208 | Экспорт в Excel: `IClayGridDataLoader.ExcelExportAsync`, `BuildAllRowsForExcel`, `BuildAllGroupedRowsForExcel`, `BuildExportRows`, `CollectCounts`, `SanitizeFileName` |
| `ClayGridPageBase.Export.Print.cs` | 89 | Печать всех данных: `BuildAllRowsForPrint`, `BuildAllFlatRowsForPrint`, `BuildAllGroupedRowsForPrint` |
| `ClayGridPageBase.Export.Selected.cs` | 225 | Экспорт/печать выбранных: `BuildPrintHtmlForSelectedAsync`, `BuildAllRowsForSelected`, `BuildAllFlatRowsForSelected`, `BuildAllGroupedRowsForSelected`, `GetGroupKeysByDepth`, `CollectKeysByDepth` |

**Правила модификации:**
- Новые поля/методы добавлять в соответствующий тематический файл, а не в `ClayGridPageBase.cs`
- При добавлении using — в тот файл, где используется тип
- Базовый класс и интерфейсы — только в `ClayGridPageBase.cs`

## Server-side grouping architecture

Группировка выполняется **на стороне SQL Server** двумя отдельными запросами (подход DevExpress Blazor Grid). Реализация — `ClayGroupingEngine` (статический класс в `Components/Grid/ClayGroupingEngine.cs`).

1. **Запрос групповых агрегатов** — `GROUP BY` + `COUNT(*)`, возвращает уникальные значения группировки и количество записей
2. **Запрос детальных строк** — выборка конкретных записей с `ROW_NUMBER()` и фильтром по значениям группы

### Модель данных
- `IClayGridRow` — маркерный интерфейс строки в плоском списке (`Clayzor.Lib.Web.Controls/Components/Grid/ClayGridRow.cs`)
- `GroupHeaderRow` — заголовок группы: `FullKey`, `DisplayValue`, `ItemCount`, `Depth`, `IsExpanded`
- `DetailRow<T>` — обёртка сущности: `Item`, `GroupKey`, `Depth`
- `GroupedPage<T>` — результат запроса: `Rows` (плоский список) + `TotalEffectiveRows`
- `ClayDataQuery.ExpandedGroups` — `HashSet<string>` полных ключей развёрнутых групп (разделитель `\u001F`)

### Рендеринг
- **Плоская модель**: заголовки групп и строки детализации передаются как единый `IEnumerable<IClayGridRow>`
- `ClayGrid` сам решает, какая колонка хостит `<ClayGroupHeader>` для строк `GroupHeaderRow`
  (`GroupRowHostKey`/`IsGroupRowHost` в `ClayGrid.Grouping.cs`: колонка редактирования, если есть,
  иначе первая видимая колонка данных, не скрытая группировкой). Страница подписывается только через
  `OnGroupToggle="ToggleGroup"` на теге `<ClayGrid>` — вручную вставлять `<ClayGroupHeader>` в
  `CellTemplate` не нужно:
  ```razor
  <ClayGrid TEntity="IClayGridRow" ... OnGroupToggle="ToggleGroup">
      ...
      <ClayColumn TEntity="IClayGridRow" ColumnId="2">
          <CellTemplate>
              @if (context.Item is DetailRow<MyEntity> detail)
              {
                  <MudText Style="@($"padding-left:{(detail.Depth + 1) * 16}px")">@detail.Item.Id</MudText>
              }
          </CellTemplate>
      </ClayColumn>
  </ClayGrid>
  ```
- `ClayGroupHeader` — встроенный компонент для отображения иконки раскрытия/сворачивания и количества элементов, вызывается гридом автоматически
- `ClayColumn` автоматически получает Title (DisplayName), строит HeaderTemplate с drag&drop и серверной сортировкой, скрывает колонку при группировке
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
- Колонки, участвующие в группировке, скрываются в гриде автоматически — `ClayColumn` вычисляет `Hidden` через `IsGrouped(SqlName)` из `ClayColumnMeta`
- Иконка раскрытия/сворачивания и название группы отображаются в первой колонке (Код) через `ClayGroupHeader`

### Имена колонок в WHERE и GROUP BY
- `SearchColumns` передаются как выходные имена (например, `"НазваниеАнализа"`, `"TestTypeName"`)
- SQL-пагинация через `ROW_NUMBER()` оборачивает SELECT в подзапрос `FROM (SELECT ...) _src`, где выходные имена колонок видны напрямую — алиасы таблиц не нужны
- `GroupColumns` содержат те же выходные имена — они напрямую используются в `GROUP BY`
- `ClayGridPageBase` читает `SearchColumns`, `SelectSql`, `DefaultOrder` из `Grid` (реализация `IClayGrid`) — **abstract-свойства не нужны**, вся конфигурация передаётся через параметры `<ClayGrid>`

### Порядок сортировки в групповых запросах
- Групповой агрегатный запрос учитывает направление сортировки из `SortColumns`:
  ```sql
  ORDER BY TestTypeName DESC, Порядок ASC
  ```
- Детальные строки внутри группы сортируются по колонкам, НЕ участвующим в группировке
- **Запрещено** пересортировывать список агрегатов после получения из БД (`aggregates.OrderBy(...)`) — это уничтожает порядок, заданный `ORDER BY` в SQL. Синтетические родительские узлы строятся непосредственно внутри цикла `foreach (var gr in groupRows)` до листового узла, поэтому при обходе `aggregates` (без `.OrderBy`) порядок родитель-перед-детьми всегда соблюдается

## Server-side column filtering

Фильтрация по колонкам выполняется **на стороне SQL Server** через `ClayCompositeSqlBuilder.Build`.
Единый источник истины — дерево `ClayFilterGroupNode` (см. «Типы составного фильтра» ниже).
UI — панель фильтров (filter tray) с drag-and-drop заголовков и диалогом `ClayColumnFilterDialog` для настройки условий;
диалог составного фильтра `ClayFilterDialog` (задача 11 — панель и маршрутизация).

### Модель данных
- `ColumnType` — тип данных колонки: `Text` (10 операторов: Contains/NotContains/Equals/NotEquals/StartsWith/NotStartsWith/EndsWith/NotEndsWith/IsEmpty/IsNotEmpty), `Number` (равенство + сравнения >/</>=/<=), `Boolean` (Equals)
- `ColumnFilterOperator` — оператор сравнения: `Contains`, `NotContains`, `Equals`, `NotEquals`, `StartsWith`, `NotStartsWith`, `EndsWith`, `NotEndsWith`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `IsEmpty`, `IsNotEmpty`, `IsNull`, `IsNotNull`
- `LogicalOperator` — `And` / `Or` для объединения узлов в группе
- `ColumnFilter` — условие фильтра: `Column`, `ParamName`, `Operator`, `Value` + опциональные `LogicalOperator`, `SecondOperator`, `SecondValue`, `SecondParamName` (до двух условий на колонку). Реализует `IClayFilterNode`. Свойство `Source` (`ClayFilterSource`) — происхождение: `ColumnDialog` или `CompositeDialog`
- `ClayFilterGroupNode` — группа И/ИЛИ (`LogicalOperator Logic` + `List<IClayFilterNode> Nodes`). Реализует `IClayFilterNode`
- `ClayDataQuery.CompositeFilter` — `ClayFilterGroupNode?` — единый источник истины фильтрации. Заменил `ColumnFilters` (словарь помечен `[Obsolete]`)
- `ClayGrid._filterRoot` — приватный корень дерева фильтра в гриде. Колоночные фильтры — листья с `Source=ColumnDialog`. Составной фильтр — `Source=CompositeDialog`

### SQL-генерация
- `ClayCompositeSqlBuilder.Build(root, parameters, knownColumns, columnNameMap?)` — рекурсивно обходит дерево `ClayFilterGroupNode` и возвращает фрагмент WHERE (без слова WHERE). Безопасность: имя колонки — только из белого списка `knownColumns`; значения — только Dapper-параметры
- `ClayGridPageBase.BuildCompositeFilterClause(CompositeFilter?, dp, columnNameMap?)` — обёртка над `ClayCompositeSqlBuilder.Build`, добавляет параметры в `DynamicParameters`. Используется во всех путях загрузки (страница, группировка, экспорт, печать, выбранные)
- `ClayGridPageBase.BuildKnownColumns()` — возвращает `ISet<string>` из ключей `_inferredColumnTypes`
- `ClayDataQuery.BuildColumnFilterClause` и `BuildSingleClause` — помечены `[Obsolete]` (заменены на `ClayCompositeSqlBuilder`). `BuildSingleClause` сделан `internal`

### Типы данных фильтрации (`ColumnType`)
- `Text` — строки: Contains, NotContains, Equals, NotEquals, StartsWith, NotStartsWith, EndsWith, NotEndsWith, IsEmpty, IsNotEmpty
- `Number` — целые числа (int/long/short/byte): Equals, NotEquals, сравнения >/</>=/<=, IsNull, IsNotNull
- `Decimal` — дробные (decimal/double/float): те же что Number, редактор `MudNumericField<decimal?>`
- `Date` — даты (DateTime/DateTimeOffset/DateOnly): сравнения + IsNull/IsNotNull, редактор `MudDatePicker`
- `Boolean` — булевы: Equals, IsNull, IsNotNull
- `ClayGridPageBase.MapClrTypeToColumnType` — делегирует в `ColumnTypeRegistry.FromClr(type).Kind`
- `ClayGridPageBase.FilterLookupOptions` — необязательный virtual-словарь (SqlName → список `ClayFilterOption`) для выпадающего выбора значений в диалоге фильтра. Страница может переопределить. Грид пробрасывает в `ClayColumnFilterDialog.LookupOptions`

### Дескрипторы типов колонок (`Components/Grid/ColumnTypes/`)
- `ColumnTypeDescriptor` — абстрактный базовый класс: `Kind`, `ClrType`, `Operators`, `DefaultOperator`, `OperatorTakesValue(op)`, `Parse(string?)`, `Format(object?)`, `ToParameter(object?)`. Единая точка типозависимого поведения
- `TextColumnType`, `NumberColumnType`, `DecimalColumnType`, `BooleanColumnType`, `DateColumnType` — конкретные дескрипторы
- `ColumnTypeRegistry` — `FromClr(Type)` (CLR→дескриптор), `FromKind(ColumnType)` (enum→дескриптор), синглтоны
- `ClayColumnMeta.Type` — дескриптор, заполняемый при регистрации колонки; единственный источник операторов/парсинга/формата
- `ClayColumnFilterDialog` получает операторы и DefaultOperator из дескриптора, парсинг/формат — через `_descriptor.Parse/Format`

### Типы составного фильтра (`Components/Grid/Filter/`)
- `IClayFilterNode` — интерфейс узла дерева фильтра: `Clone()` (рекурсивное глубокое копирование)
- `ClayFilterGroupNode` — группа И/ИЛИ (`LogicalOperator Logic` + `List<IClayFilterNode> Nodes` + рекурсивный `Clone()`). Переиспользует существующий `LogicalOperator`
- `ColumnFilter` реализует `IClayFilterNode` — листовой узел дерева. `ColumnFilter.Source` (`ClayFilterSource`) — происхождение: `ColumnDialog` (диалог колонки) или `CompositeDialog` (настраиваемый фильтр). `IsNew` — транзиентный UI-флаг (`[JsonIgnore]`, не копируется в `Clone()`): свежедобавленное перетаскиванием условие → автофокус на «Значение»
- `ValueFilter` реализует `IClayFilterNode` — листовой узел дерева: фильтрация по набору выбранных значений (Excel-style). Поля: `Column`, `Values`, `Negate` (IN/NOT IN), `BlankChecked` (NULL/пустые строки), `ParamPrefix`. `HasValue` → `Values.Count > 0 || BlankChecked`. В одной колонке одновременно активен либо `ColumnFilter`, либо `ValueFilter` — они взаимоисключающие. Диалог настройки — `ClayColumnValueFilterDialog`, открывается через `OpenConditionRequest`
- `ClayCompositeSqlBuilder` — статический SQL-билдер. `Build(root, parameters, knownColumns, columnNameMap?)` рекурсивно обходит дерево `ClayFilterGroupNode` и возвращает фрагмент WHERE (без слова WHERE). `BuildLeaf` для `ColumnFilter`, `BuildValueLeaf` для `ValueFilter` (IN/NOT IN с учётом Negate×BlankChecked, 6 комбинаций). Безопасность: имя колонки — только из белого списка `knownColumns`; значения — только Dapper-параметры; уникальные имена параметров через сквозной счётчик (`p0, p1, …`). Листовые узлы с неизвестной колонкой отбрасываются

### Filter tray
- Панель включается кнопкой `FilterAlt` (`ShowFilterTray="true"`), скрыта по умолчанию (`_filterTrayExpanded = false`). Кнопка появляется автоматически при наличии хотя бы одного `ClayColumnDef` с `Filterable="true"`
- Иконка `FilterList` в левой части панели (`filter-tray-icon`) — кликабельный `ClayButton`, открывает `OpenCompositeFilterDialog()` → `ClayFilterDialog`
- `ToggleFilterTray()` — **не сбрасывает** фильтр при сворачивании панели. Сброс только явной кнопкой «Очистить фильтр» (`ClearAllFilters()` — обнуляет `_filterRoot` целиком)
- **Два взаимоисключающих режима** отображения чипов (свойство `HasComposite` — любой узел не-ColumnDialog):
  - **Есть составные условия** (`HasComposite == true`) → единый текстовый чип со строкой `BuildFilterDescription()` (весь фильтр одним текстом). Клик по чипу → `OpenCompositeFilterDialog()`. Крестик → `ClearAllFilters()`. Чипов колонок нет
  - **Нет составных условий** (`HasComposite == false`) → чипы по листьям `ColumnDialog` (один чип на колонку, сегменты кликабельны → `OpenFilterDialog`)
- **Перетаскивание колонки на панель** (`OnFilterTrayDrop`):
  - Нет составных условий → `OpenFilterDialog(sqlName)` (диалог колонки), лист `Source=ColumnDialog`
  - Есть составные условия → `BuildTreeWithColumnAnded(sqlName)` строит копию дерева с новым условием через `И` на верхнем уровне (если корень `ИЛИ` — оборачивает в `И(Старое, Новое)`), открывает диалог на `seedRoot`. Отмена не меняет действующий фильтр. У нового листа `IsNew=true` → автофокус на «Значение»
- Удаление колоночного фильтра: × на чипе → `RemoveFilter(sqlName)`. Колоночные условия также можно удалить из формы настраиваемого фильтра (крестик в `ClayFilterGroup`)
- Сегменты/описание строятся через `ClayFilterDescriptionBuilder`: `BuildSegments(root, getDisplayName)` → `IReadOnlyList<FilterSegment>` (для колоночных чипов); `BuildText(root, getDisplayName)` → строка для составного чипа и экспорта/печати
- **Печатная шапка** (`.clay-grid-print-descriptions`) скрыта на экране (`display:none`), видна только при печати (`@media print { display:block }`) — дублирования текста фильтра на экране нет
- Filter tray не конфликтует с grouping tray — оба могут быть открыты одновременно
- **Бейдж активных условий**: `ClayFilterDescriptionBuilder.CountActiveLeaves(root)` рекурсивно подсчитывает активные условия: `ColumnFilter` с `HasValue` (+1 если `HasSecondClause`), `ValueFilter` с `HasValue` (+1). Счётчик отображается через `MudBadge` на кнопке `FilterAlt` (скрывается при 0)

### Сериализация и URL-персистенция фильтра
- **`ClayFilterJsonConverter : JsonConverter<IClayFilterNode>`** — полиморфная JSON-сериализация дерева фильтра с дискриминатором `$type`: `"group"` → `ClayFilterGroupNode`, `"column"` → `ColumnFilter`, `"value"` → `ValueFilter`. Транзиентные поля (`ParamName`, `SecondParamName`, `ParamPrefix`, computed-свойства) помечены `[JsonIgnore]`. `object? Value` сериализуется как есть, десериализуется через `JsonElement` → ближайший CLR-тип. Атрибут `[JsonConverter]` на интерфейсе `IClayFilterNode`
- **`ClayFilterUrlHelper`** — статический хелпер: дерево → JSON → `DeflateStream` → Base64Url (и обратно). Query-параметр: `filter`
- **Восстановление при загрузке**: `ClayGridPageBase.OnAfterRenderAsync(firstRender)` читает параметр `filter` из URL, десериализует через `ClayFilterUrlHelper.Deserialize()` и вызывает `Grid.RestoreFilter(root)`. `ClayGrid.RestoreFilter()` заменяет `_filterRoot` и вызывает `NotifyQueryChanged()`

### Локализация фильтра
- **`ClayFilterStrings`** — единый источник всех строковых констант UI фильтра (заголовки, кнопки, подписи). Заменяет хардкод русских строк в `ClayFilterGroup.razor`, `ClayFilterDialog.razor`, `ClayFilterExpression.razor` и тулбаре `ClayGrid.razor`

### Интеграция на странице (через ClayGridPageBase\<T>)
Конфигурация SQL передаётся через параметры `<ClayGrid>`:
```razor
<ClayGrid TEntity="IClayGridRow"
           DataLoader="this"
           SelectSql="@SQLQueries.SELECT_МоиЗаписи"
           SearchColumns="@(new[]{"НазваниеАнализа","TestTypeName"})"
           DefaultOrder="Порядок, НазваниеАнализа"
           ... >
```
`ClayGridPageBase<T>` автоматически читает `SelectSql`, `SearchColumns`, `DefaultOrder` из `IClayGrid`, строит WHERE через `BuildWhereClause`/`BuildCompositeFilterClause` и вызывает `Entity.GetPagedAsync`/`Entity.GetCountAsync`. Во всех путях загрузки (страница, группировка, экспорт, печать, выбранные) фильтрация идёт через единый вызов `BuildCompositeFilterClause(_query.CompositeFilter, dp)`, который делегирует в `ClayCompositeSqlBuilder.Build`. В плоском и группированном режимах `SearchColumns` одни и те же — используются выходные имена колонок (видимые в подзапросе `ROW_NUMBER()`).

`FilterColumnTypes` вычисляется автоматически через рефлексию по `[Column]`-атрибутам и C#-типам свойств сущности.
Страница просто передаёт `FilterColumnTypes="@FilterColumnTypes"` в `<ClayGrid>`. Маппинг: `bool` → `Boolean`, числовые типы → `Number`, остальные → `Text`.

## Typography & Fonts

- Font face: **Verdana** + fallback `Arial, sans-serif` — defined via CSS variable `--clay-font-family`
- Font size: **0.8rem** (~13pt) — defined via CSS variable `--clay-font-size` (single token, NO size variants)
- MudBlazor typography (`ClayTheme.cs`) — all levels (Default, Body1, Body2, Subtitle1, Subtitle2, Caption, Overline) set `FontSize = "var(--clay-font-size)"`. H4/H5/H6/Button keep their own sizes (heading/button scale)
- MudBlazor input controls use `font-size: var(--clay-font-size) !important` (CSS rules in `app.css` for `.mud-input-control input`, `.mud-input-slot`, `.mud-input-label-outlined`, and `.mud-input-outlined-border legend`)
- MudBlazor outlined labels use `transform: scale(0.75)` by default — overridden in `app.css` via `scale(1)` with `!important` on both `.mud-input.mud-shrink ~ .mud-input-label-outlined` and `.mud-input-control:focus-within .mud-input-label-outlined`. Key detail: `mud-shrink` class is on `div.mud-input`, NOT on the `<label>` — label is a sibling AFTER `.mud-input`, so the selector uses `~` (general sibling combinator). Focused empty fields have NO `mud-shrink` class, hence the separate `:focus-within` selector.
- Legend notch in outlined border is sized via `font-size: var(--clay-font-size) !important` on `.mud-input-outlined-border legend` (both `.mud-shrink` and `:focus-within` variants)
- No external font CDN dependencies (Google Fonts Inter removed)

## Style enforcement (STYLE_RULES.md, `promts/_done/STYLE_PROMPTS.md`)

Пошаговые промты для внедрения единого стиля — `promts/_done/STYLE_PROMPTS.md` (Промт 0–6). Выполнены.
Закон стиля — `STYLE_RULES.md`.

**All visual styling (color, font, background, border, shadow, radius) lives ONLY in:**
- `Clayzor.App.Web.MedicalTests/wwwroot/css/app.css` — CSS classes and `:root` tokens
- `Clayzor.Lib.Web.Controls/Themes/ClayTheme.cs` — MudBlazor theme palette
- `Clayzor.Lib.Web.Controls/Themes/ClayColors.cs` — single source of brand hex values

### Architecture (Variant A)
`ClayColors.cs` (C# hex literals) → `ClayTheme.cs` (MudBlazor palette) → MudBlazor emits `--mud-palette-*` CSS variables → `app.css` aliases them as `--clay-*` variables. Dark mode adapts automatically.

### Build-time enforcement
- **`build/StyleGuard.targets`** — MSBuild inline C# task (`BeforeTargets="CoreCompile"`). Scans `**/*.razor` and `**/*.cs` for visual inline styles (`color:`, `background`, `border`, `font-*`, hex colors, `rgba(`). Build FAILS on violations with file/line/error details.
- White-listed files (excluded from scanning): `app.css`, `ClayTheme.cs`, `ClayGridPrintStyles.cs`, `ClayGridPrintHtmlGenerator.cs`, `ClayGridExcelGenerator.cs`
- **Allowed in `style=`/`Style=`**: structural properties only — `display`, `flex`, `gap`, `width`, `padding`, `margin`, `cursor`, `overflow`, `text-overflow`, `position`, `z-index`, `opacity`, `white-space`, etc.
- **Prohibited in `style=`/`Style=`**: `color`, `background*`, `border*`, `box-shadow`, `font-*`, `fill`, `stroke`, `letter-spacing`, `text-transform`, hex colors, `rgb(`/`rgba(` with color values
- All `<style>` blocks in `.razor` must be moved to `app.css`

### Before completing any UI task (checklist)
- No visual `style=`/`Style=` in changed `.razor`
- No hex colors/font strings in `.cs` outside white-list
- Colors use `var(--mud-palette-*)` (adaptive) or `var(--clay-*)` (brand aliases)
- New visual patterns → CSS class in `app.css`
- Layout → MudBlazor utility classes (`d-flex`, `gap-*`, `pa-*`)
- `dotnet build` passes (StyleGuard checks active)

## Key conventions
- All Razor markup and user-visible text is **Russian**
- No CI/CD, no linter configuration
- Unit tests: `tests/Clayzor.Lib.Web.Controls.Tests/` (xUnit, 39 тестов — SQL-билдер, модель, сериализация)
- **Использовать готовые компоненты из `src\Clayzor.Lib.Web.Controls`** при разработке форм (ClayEditForm, ClayComboBox, ConfirmDialog, ClayColumnFilterDialog, ClayErrorBar). Проверять наличие подходящего компонента перед использованием MudBlazor-компонентов напрямую.
- **Кнопки с тултипом**: всегда использовать `<ClayButton>` вместо пары `<MudTooltip><MudIconButton/></MudTooltip>`. `ClayButton` автоматически сбрасывает тултип после клика, предотвращая «залипание» при открытии диалогов.
- **Выпадающие меню**: всегда использовать `<ClayMenu>` вместо `<MudMenu>` с `<ActivatorContent>`, `<MudTooltip>` и `<MudIconButton>`. `ClayMenu` автоматически строит кнопку-активатор с опциональным тултипом и сбрасывает тултип после клика, аналогично `ClayButton`.
- **Кастомные чекбоксы**: всегда использовать `<ClayCheckbox>` (controlled: `State` + `OnToggle`) вместо inline `<span>`-глифов 16×16. Поддерживает tri-state (`null` = indeterminate).
- `[Column]` attributes use exact Russian database column names — do not translate
- SQL queries reference tables by Russian names (e.g. `МедицинскиеАнализы`, `МедицинскиеАнализыТипы`)
- Each SQL constant must be documented with `///` XML doc and `--` inline SQL comments for every column
- Every public/protected class, method, property, and field must have a `/// <summary>` XML doc comment
- Database column names are defined exactly once in `ColumnNames.cs` and referenced from `[Column]` attributes (SQLQueries constants are exempt)
- Data loading goes in `OnAfterRenderAsync(bool firstRender)` with `if (firstRender)` guard, **not** in `OnInitializedAsync` — avoids double-load from Blazor prerendering
- Sorting, searching, grouping, and **pagination** for data grids must be performed on SQL Server side (not in-memory)
- Sort column headers call `async Task ToggleSort(string column)` via `@onclick="@(async () => { if (_dataGrid is not null) await _dataGrid.ToggleSort("SqlColumn"); })"` — `_dataGrid` is set via `@ref` on `ClayGrid`. Chips in the grouping tray use `@onclick="async () => await ToggleSort(col)"` inside ClayGrid itself. **Do not call `ToggleSort` as fire-and-forget void** — Blazor will not await the data reload
- `appsettings.Development.json` is gitignored — use it for local connection strings
- All modal/dialog windows must be draggable
- Data grid header row must be fixed (not scroll with data) — `ClayGrid` does this automatically
- При вызове `Db.ExecuteAsync()` с сырым SQL обязательно передавать `commandType: CommandType.Text` — по умолчанию `ExecuteAsync` использует `CommandType.StoredProcedure`
- `DapperColumnMapper` делает fallback на имя свойства, если `[Column]`-атрибут не совпал с колонкой результата — это позволяет использовать SQL-алиасы (`SELECT КодТипа AS Id`) даже при наличии `[Column("КодТипа")]` на свойстве `Id`
- **OnQueryChanged**: обновлять свойства `_query`, а не переприсваивать объект — иначе `TotalCount` сбрасывается в 0 при async-рендере. `ExpandedGroups` управляется страницей и **не перезаписывается** из query. `CompositeFilter` копируется из query целиком (дерево)
- **ClayGridPageBase**: SQL-конфигурация передаётся через параметры `<ClayGrid>` (`SelectSql`, `SearchColumns`, `DefaultOrder`, `EditDialogType`), а не через abstract-свойства. База читает их из `Grid?.SelectSql` и т.д. Страница переопределяет `protected override IClayGrid? Grid => _dataGrid;` и передаёт `DataLoader="this"`
- **Запрещено** подставлять значения параметров в SQL-строку — все параметры передаются через Dapper (`@param`). Параметры пагинации передаются как `@__start`/`@__end` через `DynamicParameters`
- **Обработка ошибок БД**: `DbManager` автоматически перехватывает `SqlException` и вызывает `ISqlErrorHandler.HandleSqlError()`. Страницам **не нужно** вызывать `ErrorService.Report()` вручную — только `try/finally` для `_loading = false`. Баннер `ClayErrorBar` в `MainLayout` показывает ошибку со строкой подключения, SQL и параметрами
- **Grouping tray**: заголовки колонок генерируются автоматически через `<ClayColumn>` — drag-and-drop (`draggable="true"` с `data-col-sql`,
  `ClayDragState.DraggedColumn` устанавливается через JS→C# `SetDraggedColumn`) и серверная сортировка (`@onclick` → `Grid.ToggleSort(query.SqlName)`) встроены в компонент.
  При перетаскивании на панель группировки колонка добавляется автоматически. Сортировка по сгруппированным колонкам разрешена (клик по чипу в трее).
  Каждый чип имеет переключатель `UnfoldMore`/`UnfoldLess` — разворачивает/сворачивает ВСЕ группы этого уровня (с каскадом вверх: разворачивание уровня N разворачивает и уровни 0..N−1). Кнопка `MoreVert` (⋮) справа — контекстное меню с пунктом «Фильтровать» (аналог меню заголовка колонки, вызывает `OpenFilterDialog`).
  Панель скрыта по умолчанию (`_trayExpanded = false`) и открывается кнопкой `AccountTree` в тулбаре.
  Кнопка группировки появляется автоматически при наличии хотя бы одного `ClayColumnDef` с `Groupable="true"`.
  Кнопки тулбара (группировка, фильтрация, добавить, выбрать, групповые операции) используют `MudIconButton` с CSS-классами `grouping-toggle-btn` /
  `filter-toggle-btn` / `toolbar-add-btn` / `toolbar-select-btn` / `toolbar-batch-btn` и тултипами — не `MudButton Variant.Filled`
- **Per-level expand/collapse**: `IClayGridDataLoader.IsLevelFullyExpanded(int depth)` / `ToggleLevelExpandedAsync(int depth)` — массовое управление группами. Дерево групп кешируется в `ClayGridPageBase._groupTreeRoots`. `IsLevelFullyExpanded` проверяет, что все FullKey данной глубины присутствуют в `ExpandedGroups`. `ToggleLevelExpandedAsync` при разворачивании каскадно добавляет в `ExpandedGroups` ключи уровней 0..depth, при сворачивании — удаляет ключи только depth. Взаимодействие с индивидуальным `ToggleGroup`: ручное сворачивание одной группы делает `IsLevelFullyExpanded = false`.
- **Column registration**: метаданные колонок регистрируются через `<ClayColumnDef SqlName="..." DisplayName="..." Groupable="true" Filterable="true" />`
  внутри `<ColumnDefs>`, а не через параметры `ShowGroupingTray`/`AvailableGroupColumns`/`AvailableFilterColumns`.
  `ClayGrid` реализует `IClayGrid` и получает регистрацию через каскадный параметр.
- **Column reorder**: перетаскивание колонок в заголовке реализовано кастомным JS (`clayGridColumnDrag.js`) с **insert**-семантикой (вставка перед/после). Заменяет MudBlazor `DragDropColumnReordering`. `ClayColumn` и динамические колонки ClayGrid устанавливают `DragAndDropEnabled="false"`. Заголовки содержат `data-col-sql` атрибут. JS инициализируется через `clayGridColumnDrag.init(gridId, dotnetRef)` в `OnAfterRenderAsync`. `SetDraggedColumn` вызывается из JS на dragstart — устанавливает `ClayDragState.DraggedColumn` для tray-drop. `OnColumnDrop` применяет insert-перемещение в `_columnOrder` с последующим `_dataKey++`. Cleanup — `clayGridColumnDrag.dispose(gridId)` в `DisposeAsync`.
- **Grouping tray toggle**: кнопка `AccountTree` включает/выключает трей. При выключении (`_trayExpanded = false`) очищает `_groupColumns` и перезагружает данные в плоском режиме — колонки возвращаются в грид. Кнопка появляется только при наличии хотя бы одного `ClayColumnDef` с `Groupable="true"`
- **Filter tray toggle**: кнопка `FilterAlt` включает/выключает трей фильтрации. При выключении (`_filterTrayExpanded = false`) очищает `_activeFilters` и перезагружает данные без фильтров. Оба трея (группировка + фильтрация) могут быть открыты одновременно — высота грида уменьшается соответственно
- **Tray borders & icons**: панели группировки (`.grouping-tray`) и фильтрации (`.filter-tray`) имеют идентичное оформление. Фон, границы и иконки используют MudBlazor-переменные (`--mud-palette-*`) для авто-адаптации к светлой/тёмной теме. `border-left: 3px solid var(--mud-palette-primary)`, `border-bottom: 2px solid var(--lh-gold)`, фон `var(--mud-palette-background-gray)`. Иконки: по умолчанию `var(--mud-palette-text-secondary)` + opacity 0.45; при наличии чипов в трее (`:has(.xxx-chip)`) — `var(--mud-palette-primary)` + opacity 1. При наведении/перетаскивании `border-left-color` меняется на `var(--lh-gold)`. CSS определён в `wwwroot/css/app.css`
- **Batch operations**: стандартные операции включаются флагами `ShowPrint` / `ShowExcel` без написания кода на странице. Кастомные — через `CustomBatchGroups` (модели `BatchOperationGroup` / `BatchOperation`, обработчики `Func<Task>? OnExecute` реализуются в приложении). Меню открывается кнопкой `PlaylistAddCheck` при `SelectVisible="true"`
- **Selection**: режим выбора включается кнопкой `CheckBox` в тулбаре. Добавляет сервисную колонку с кастомными чекбоксами (16×16px, белый фон, классическая CSS-галочка border-rotate, жирный квадрат для indeterminate). Выделение хранится как `HashSet<int> _selectedIds` (ID сущностей) и **персистентно** между страницами и при смене размера страницы. Сбрасывается только при изменении сути запроса: поиск, группировка, сортировка, фильтры. Галка в заголовке управляет и строками детализации, и группами (через `GroupHeaderRow` → ленивая загрузка дочерних ID → `_groupChildIds`). При выключении режима выбора `_selectAllChecked` сбрасывается. Групповые чекбоксы поддерживают tri-state (checked/unchecked/indeterminate)
- **Print**: все три режима печати (текущая страница / все данные / выбранные) проходят через серверную HTML-генерацию: `BuildPrintHtmlForCurrentPageAsync` / `BuildPrintHtmlAsync` / `BuildPrintHtmlForSelectedAsync` → `ClayGridPrintHtmlGenerator.Build()` → `clayGridPrint.printHtml(html)` (рендерит HTML в скрытый iframe, печатает iframe, удаляет после `afterprint`). `clayGridPrint.printCurrentPage()` удалён — печать текущей страницы больше не использует `window.print()` на DOM грида. **HTML использует те же MudBlazor CSS-классы** (`.mud-table`, `.mud-table-cell`, `.group-header-cell`) и встраивает полный `@media print` CSS — результат визуально идентичен экранному гриду. Групповые строки выводятся без иконок разворота/выбора — только имя группы и количество. Грид **не затрагивается**. На время загрузки — спиннер (`_isExporting`). Печать «Выбранных» — реализована через `BuildAllRowsForSelected` (группированный режим: C# interleaving с `SelectedItemCount` для каждого `GroupHeaderRow`). **Перед каждой операцией** — `ResolveExportColumnsAsync` → prompt `ClayColumnSettingsPromptDialog` (3 исхода): «Выбрать колонки» (диалог `ClayColumnSettingsDialog` с `ShowSorting=false`), «Как на странице» (`GetVisibleColumns()`), «Отмена»
- **Excel export**: экспорт в Excel реализован через `ClayGridExcelGenerator` (генератор .xlsx на сервере, ClosedXML) и `clayGridExcel.js` (скачивание на клиенте через Blob URL). Генератор создаёт стилизованную книгу в цветах Clayzor (navy `#05164D`, gold `#FFAD00`, серый stripe `#F2F4F7`) со шрифтом Verdana. Поддерживает заголовок грида, описания фильтров/группировки, групповые строки с Excel Outline (вложенные группы через стек — все уровни имеют +/- контролы), авто-ширину колонок. Групповые строки выводятся без иконок выбора. На время экспорта рядом с заголовком грида показывается `MudProgressCircular` (Size.Small, Indeterminate) — флаг `_isExporting` управляется в `try/finally`. Экспорт «Всех данных» — полностью реализован через `BuildAllRowsForExcel()`: в плоском режиме один запрос `SELECT * FROM _src`, в режиме группировки — два запроса (GROUP BY для агрегатов + плоский SELECT всех строк, групповая структура строится в C# детектированием смены ключа). Экспорт «Выбранных» — реализован через `BuildAllRowsForSelected`. **Перед каждой операцией** — `ResolveExportColumnsAsync` (тот же prompt, что и для печати)
- **Column settings**: кнопка `ViewColumn` в тулбаре открывает `ClayColumnSettingsDialog` — диалог настройки порядка, видимости и сортировки колонок. Drag-and-drop реализован как jQuery UI Sortable с авто-прокруткой (ghost на `position:fixed`, placeholder в DOM, авто-скролл при приближении к краям контейнера). Сортировка: клик по названию колонки циклически переключает ASC(↑) → DESC(↓) → нет, до 2 колонок одновременно. Бейдж `1↑`/`2↓` (`.chip-sort-badge`) — золотой фон, navy текст, идентичен бейджу трея группировки. Область сортировки (`.sort-toggle-area`) изолирована от drag-and-drop через JS `closest()`. Кнопки сброса внизу диалога: `ClearAll` (сброс сортировки), `RestartAlt` (восстановление исходного порядка, видимости и сортировки). JS в `clayColumnSettings.js` (RCL), результат передаётся в C# через `[JSInvokable] OnJsDrop`. Стили чипов — `.column-settings-chip` (navy, gold accent hover), ghost — `.column-settings-ghost`, placeholder — `.column-settings-placeholder`. Видимость колонок применяется к гриду (через `_hiddenSqlNames` + `ClayColumn.Hidden`). Переключатель заблокирован для сгруппированных колонок. Порядок колонок из диалога применяется к гриду через двухфазный рендеринг (сбор CellTemplate → динамические TemplateColumns по `_columnOrder`). `_columnOrder` всегда синхронизирован с DOM (обновляется через `OnColumnDrop` из кастомного JS), дополнительное чтение DOM не требуется. При применении сортировка синхронизируется обратно в `_sortState` грида (через `SortName`), вызывается `NotifyQueryChanged()` для перезагрузки данных. При отмене диалога порядок, видимость и сортировка восстанавливаются из snapshot. Параметр `Id` задаёт DOM-id корневого элемента грида (используется `clayGridColumnDrag.init`). **Режим без сортировки** (`ShowSorting=false`): sort-toggle-area не кликабельна, бейдж и кнопка сброса сортировки скрыты. Используется при выборе колонок для печати/экспорта. Валидация: кнопка «Применить» блокируется если все колонки скрыты (Snackbar «Должна быть видна хотя бы одна колонка»)
- **Grid height**: вычисляется динамически через `_gridHeight`: `calc(100vh - 280px)` без треев, `calc(100vh - 330px)` с одним треем, `calc(100vh - 380px)` с двумя. Заголовок грида фиксирован (`FixedHeader="true"`)
- **Responsive layout**: тулбар и пагинация обёрнуты в `<div>` с `flex-wrap` — элементы переносятся на узких экранах. Внутренние группы используют `MudStack` для вертикального центрирования
- **Mobile menu**: колонки грида имеют кнопку `⋮` для доступа к группировке и фильтрации без drag-and-drop. Режим управляется параметром `ColumnMenuMode` (по умолчанию `Mobile` — только ≤960px). Пункты меню показываются только когда соответствующая панель (группировка/фильтрация) активирована
- **Navigation drawer**: `DrawerVariant.Responsive` — десктоп (persistent, сдвигает контент), мобильные (temporary overlay). Кнопка-гамбургер меняет иконку `Menu` ↔ `MenuOpen`. AppBar `z-index: 1301` всегда выше overlay. Overlay начинается ниже AppBar + gold underline (`top: 51px`)
- `MudThemeProvider` в `MainLayout` использует `ObserveSystemDarkModeChange="false"` — отключает автоматическое переключение тёмной/светлой темы при изменении системных настроек ОС
- `Program.cs` регистрирует `ClayErrorService` как Scoped и как `ISqlErrorHandler`, передаёт `ISqlErrorHandler` в конструктор `DbManager`
