# Инструкции для агента — репозиторий Clayzor

Это суперпроект с git-сабмодулями (`src/*`, `tests/*`). Сабмодули по умолчанию
находятся в состоянии detached HEAD.

## Правило: detached HEAD в сабмодуле

Перед коммитом внутри сабмодуля всегда проверяй `git status`.

Если первая строка — `HEAD detached ...` и в этом состоянии есть локальный
коммит, который нужно сохранить на ветке:

1. Сначала убедись, что ветка `main` — предок текущего коммита
   (текущий коммит просто "впереди" `main`):
   `git merge-base --is-ancestor main HEAD` (код возврата 0 = да).
2. Если да — прикрепи HEAD к ветке и подвинь её на текущий коммит:
   `git switch -C main`
3. Если нет (ветка `main` разошлась или указывает на посторонний коммит) —
   НЕ выполняй `switch -C` (он перезапишет указатель `main`). Остановись
   и спроси пользователя, что делать.

Если `git status` показывает `On branch main` — HEAD уже на ветке,
ничего прикреплять не нужно.

## Сопутствующее

- После коммитов в сабмодулях отдельно фиксируй обновлённые ссылки на них
  в суперпроекте (`git add src tests` → commit) — иначе возникает рассинхрон.
- Файлы сборки .NET (`bin/`, `obj/`) не коммить: они в `.gitignore` каждого
  сабмодуля.
 
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
- Актуализация AGENTS.md и документации — **только по прямому указанию**. Не обновлять документацию в рамках других задач без явной просьбы.

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

`BindClaySettings()` in `Clayzor.Lib.Web.Settings/ClayAppSettings.cs`:
1. Биндит секцию `ClayApp` из конфигурации (`appsettings.json` + `web.config`)
2. Если `ConnectionString` пуста — читает имя из `ClayGrid:Dynamic:ConnectionStringName` (по умолчанию `"DefaultConnection"`)
3. Читает строку подключения с этим именем из `web.config` через `System.Configuration.ConfigurationManager.OpenMappedExeConfiguration().ConnectionStrings`
4. `URI_help_clayGrid` читается из `web.config` через `ConfigurationManager.AppSettings` (ключ `URI_help_clayGrid`)
5. `DictionaryConnectionString` — аналогично п.3 с именем `"DictionaryConnection"`

Оба `appsettings.json` и `web.config` участвуют в конфигурации; `web.config` — основной источник для `connectionStrings` и `appSettings`. Локальные переопределения — в `appsettings.Development.json` (gitignored).

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

## Map / где что искать

| Что | Где |
|---|---|
| Глобальные правила (Think/Simplicity/Surgical/Goal-Driven), обзор решения, стек, сборка, тесты, цепочка зависимостей, конфигурация, key conventions | **/AGENTS.md** (этот файл) |
| Доступ к БД, SQL-конвенции, пагинация SQL Server 2008 R2 | **[src/Clayzor.Lib.DALC/AGENTS.md](src/Clayzor.Lib.DALC/AGENTS.md)** |
| Entity CRUD & Lookup, добавление новых сущностей | **[src/Clayzor.Lib.Entities/AGENTS.md](src/Clayzor.Lib.Entities/AGENTS.md)** |
| Документация по Entity, CRUD, ILookupEntity | [src/Clayzor.Lib.Entities/docs/entity-crud.md](src/Clayzor.Lib.Entities/docs/entity-crud.md) |
| Воркфлоу добавления новой сущности | [src/Clayzor.Lib.Entities/docs/adding-new-entity.md](src/Clayzor.Lib.Entities/docs/adding-new-entity.md) |
| Роль сборки настроек/конфигурации | **[src/Clayzor.Lib.Web.Settings/AGENTS.md](src/Clayzor.Lib.Web.Settings/AGENTS.md)** |
| Shared-компоненты (ClayGrid, ClayComboBox, ClayEditForm, ...), интерфейсы, сервисы, серверная группировка, фильтрация, фильтр-трей, сериализация, локализация | **[src/Clayzor.Lib.Web.Controls/AGENTS.md](src/Clayzor.Lib.Web.Controls/AGENTS.md)** |
| Документация ClayGrid | [src/Clayzor.Lib.Web.Controls/docs/clay-grid.md](src/Clayzor.Lib.Web.Controls/docs/clay-grid.md) |
| Документация ClayComboBox | [src/Clayzor.Lib.Web.Controls/docs/clay-combo-box.md](src/Clayzor.Lib.Web.Controls/docs/clay-combo-box.md) |
| Документация ClayEditForm | [src/Clayzor.Lib.Web.Controls/docs/clay-edit-form.md](src/Clayzor.Lib.Web.Controls/docs/clay-edit-form.md) |
| Документация ClayErrorBar | [src/Clayzor.Lib.Web.Controls/docs/clay-error-bar.md](src/Clayzor.Lib.Web.Controls/docs/clay-error-bar.md) |
| Документация ClayColumnFilterDialog | [src/Clayzor.Lib.Web.Controls/docs/clay-column-filter-dialog.md](src/Clayzor.Lib.Web.Controls/docs/clay-column-filter-dialog.md) |
| Документация ConfirmDialog | [src/Clayzor.Lib.Web.Controls/docs/confirm-dialog.md](src/Clayzor.Lib.Web.Controls/docs/confirm-dialog.md) |
| Динамический режим ClayGrid — план реализации (G0–G14, выполнены) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/_readme_grid_dynamic.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/_readme_grid_dynamic.md) |
| Динамический режим ClayGrid — багфиксы (GF1–GF16) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GF0_README_dynamic_fixes.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GF0_README_dynamic_fixes.md) |
| Динамический режим ClayGrid — группировка (GG1–GG9) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GG0_README_dynamic_grouping.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GG0_README_dynamic_grouping.md) |
| Динамический режим ClayGrid — снятие потолка уровней (GN1–GN4) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GN0_README_grouping_levels.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GN0_README_grouping_levels.md) |
| Динамический режим ClayGrid — печать и Excel (GE1–GE6) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GE0_README_dynamic_export.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/_done/GE0_README_dynamic_export.md) |
| ClayGrid — багфиксы UX (GB1–GB11) | [src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GB0_README_grid_ux_fixes.md](src/Clayzor.Lib.Web.Controls/Components/Grid/promts/GB0_README_grid_ux_fixes.md) |
| DynamicGrid: SQL-схема (dev/test стенд) | [scripts/dynamic-grid/schema.sql](scripts/dynamic-grid/schema.sql) |
| Типографика, шрифты, style enforcement (Variant A, build-time, checklist) | **[src/Clayzor.App.Web.MedicalTests/AGENTS.md](src/Clayzor.App.Web.MedicalTests/AGENTS.md)** |
| Закон стиля (исходник) | [STYLE_RULES.md](STYLE_RULES.md) |
| Промты внедрения стиля (выполнены) | [promts/_done/STYLE_PROMPTS.md](promts/_done/STYLE_PROMPTS.md) |

## Key conventions
- All Razor markup and user-visible text is **Russian**
- No CI/CD, no linter configuration
- Unit tests: `tests/Clayzor.Lib.Web.Controls.Tests/` (xUnit, 39 тестов — SQL-билдер, модель, сериализация)
- **Использовать готовые компоненты из `src\Clayzor.Lib.Web.Controls`** при разработке форм (ClayEditForm, ClayComboBox, ConfirmDialog, ClayColumnFilterDialog, ClayErrorBar). Проверять наличие подходящего компонента перед использованием MudBlazor-компонентов напрямую.
- **Кнопки с тултипом**: всегда использовать `<ClayButton>` вместо пары `<MudTooltip><MudIconButton/></MudTooltip>`. `ClayButton` автоматически сбрасывает тултип после клика, предотвращая «залипание» при открытии диалогов. При задании `Href` рендерится как ссылка (`<a>`) — для кнопок, открывающих внешние страницы.
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
  Кнопки тулбара (настройка колонок, группировка, фильтрация, добавить, выбрать, групповые операции, документация) используют `ClayButton` с CSS-классами `toolbar-columns-btn` /
  `grouping-toggle-btn` / `filter-toggle-btn` / `toolbar-add-btn` / `toolbar-select-btn` / `toolbar-batch-btn` / `toolbar-help-btn` и тултипами — не `MudButton Variant.Filled`
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
